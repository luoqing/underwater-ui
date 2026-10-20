// Isolated browser gameplay capture; JSON-lines commands on stdin.
// node Tools/qa_browser_video.mjs /path/to/chrome /path/to/output
// {action:'navigate',url,width:1440,height:900,waitForUnity:true}
// {action:'recordStart',name:'adventure',quality:85}; {action:'recordStop'}
// {action:'click'|'doubleClick',x,y,holdMs:50,delayMs:120,wait:500}
// {action:'hold',x,y,ms}; {action:'drag',x,y,toX,toY,ms}; {action:'key',key:'w',ms}
// {action:'shot',name}; {action:'resize',width,height}; {action:'wait',ms}
// {action:'state'}; {action:'close'}; {action:'mark',text:'Chapter title'}
// Records real JPEG frames and CDP timestamps, then encodes variable-frame-rate
// MP4 with ffmpeg (override executable using FFMPEG). No motion interpolation.
// Video is silent; original frames, timing manifest and action log are retained.
import fs from 'node:fs';
import os from 'node:os';
import path from 'node:path';
import { spawn } from 'node:child_process';
import { createInterface } from 'node:readline';
import { setTimeout as delay } from 'node:timers/promises';

const executable = process.argv[2];
const output = path.resolve(process.argv[3]);
fs.mkdirSync(output, { recursive: true });
const profile = fs.mkdtempSync(path.join(os.tmpdir(), 'underwater-video-qa-'));
const log = fs.openSync(path.join(output, 'chromium.log'), 'w');
const chrome = spawn(executable, ['--headless=new', '--no-sandbox', '--disable-dev-shm-usage',
  '--use-angle=swiftshader', '--enable-unsafe-swiftshader', '--no-proxy-server', '--no-first-run',
  '--disable-background-timer-throttling', '--disable-renderer-backgrounding',
  '--remote-debugging-port=0', `--user-data-dir=${profile}`, 'about:blank'],
{ stdio: ['ignore', log, log] });
let socket, recording;
let sequence = 0;
let records = [];
const pending = new Map();
const input = createInterface({ input: process.stdin });
function actionLog(value) {
  fs.appendFileSync(path.join(output, 'actions.jsonl'), JSON.stringify({
    time: new Date().toISOString(), recording: recording?.name,
    elapsedMs: recording ? Date.now() - recording.started : undefined, ...value }) + '\n');
}
function send(method, params = {}) {
  const id = ++sequence;
  return new Promise((resolve, reject) => {
    const timer = setTimeout(() => { pending.delete(id); reject(new Error(`Timeout: ${method}`)); }, 120000);
    pending.set(id, { resolve, reject, timer });
    socket.send(JSON.stringify({ id, method, params }));
  });
}
function safeName(name) {
  if (!/^[a-zA-Z0-9_-]+$/.test(name)) throw new Error('Invalid output name');
  return name;
}
async function state() {
  const result = await send('Runtime.evaluate', { expression: `JSON.stringify({
    ready:document.querySelector('#unity-loading-bar')?.style.display==='none',
    progress:document.querySelector('#unity-progress-bar-full')?.style.width,
    url:location.href,width:innerWidth,height:innerHeight,
    canvas:document.querySelector('canvas')?{width:document.querySelector('canvas').width,height:document.querySelector('canvas').height}:null,
    media:Array.from(document.querySelectorAll('video')).map(v=>({src:v.currentSrc,duration:Number.isFinite(v.duration)?v.duration:null,
      readyState:v.readyState,currentTime:v.currentTime,error:v.error?{code:v.error.code,message:v.error.message}:null,
      tracks:Array.from(v.querySelectorAll('track')).map(t=>({readyState:t.readyState,mode:t.track.mode}))})),
    resources:performance.getEntriesByType('resource').filter(r=>r.name.includes('/Build/')).map(r=>({name:r.name,transferSize:r.transferSize,duration:r.duration}))})`, returnByValue: true });
  return JSON.parse(result.result.value);
}
async function mouseClick(x, y, count = 1, holdMs = 750) {
  await send('Input.dispatchMouseEvent', { type: 'mouseMoved', x, y });
  await send('Input.dispatchMouseEvent', { type: 'mousePressed', x, y, button: 'left', buttons: 1, clickCount: count });
  await delay(holdMs);
  await send('Input.dispatchMouseEvent', { type: 'mouseReleased', x, y, button: 'left', clickCount: count });
}
async function doubleClick(c) {
  const dispatches = [];
  const dispatch = params => dispatches.push(send('Input.dispatchMouseEvent', params)
    .then(() => null, error => error));
  try {
    dispatch({ type: 'mouseMoved', x: c.x, y: c.y });
    for (let count = 1; count <= 2; count++) {
      dispatch({ type: 'mousePressed', x: c.x, y: c.y, button: 'left', buttons: 1, clickCount: count });
      await delay(c.holdMs ?? 50);
      dispatch({ type: 'mouseReleased', x: c.x, y: c.y, button: 'left', buttons: 0, clickCount: count });
      if (count === 1) await delay(c.delayMs ?? 120);
    }
    const error = (await Promise.all(dispatches)).find(Boolean);
    if (error) throw error;
  } catch (error) {
    await send('Input.dispatchMouseEvent', { type: 'mouseReleased', x: c.x, y: c.y, button: 'left', buttons: 0 }).catch(() => {});
    throw error;
  }
}
async function stopRecording() {
  if (!recording) throw new Error('No active recording');
  await send('Page.stopScreencast');
  const capture = recording;
  capture.stopped = Date.now();
  recording = undefined;
  fs.writeFileSync(path.join(capture.directory, 'timing.json'), JSON.stringify(capture, null, 2));
  if (!capture.frames.length) throw new Error('Recording contains no frames');
  // CDP timestamps are seconds since epoch. Keep stalls as held images, not a
  // falsely smooth or accelerated clip. First image covers any initial delay.
  const rows = ['ffconcat version 1.0'];
  capture.frames.forEach((frame, index) => {
    const start = index === 0 ? capture.started : frame.timestamp * 1000;
    const next = capture.frames[index + 1];
    const end = next ? next.timestamp * 1000 : capture.stopped;
    rows.push(`file '${frame.file}'`, `duration ${Math.max(.001, (end - start) / 1000).toFixed(6)}`);
  });
  rows.push(`file '${capture.frames.at(-1).file}'`);
  const manifest = path.join(capture.directory, 'frames.ffconcat');
  fs.writeFileSync(manifest, rows.join('\n') + '\n');
  const video = path.join(output, capture.name + '.mp4');
  const encoderLog = fs.openSync(path.join(capture.directory, 'ffmpeg.log'), 'w');
  const encoder = spawn(process.env.FFMPEG || 'ffmpeg', ['-y', '-f', 'concat', '-safe', '0', '-i', manifest,
    '-vsync', 'vfr', '-vf', 'pad=ceil(iw/2)*2:ceil(ih/2)*2', '-c:v', 'libx264', '-preset', 'fast',
    '-crf', '20', '-pix_fmt', 'yuv420p', '-movflags', '+faststart', video], { stdio: ['ignore', encoderLog, encoderLog] });
  try {
    await new Promise((resolve, reject) => {
      encoder.on('error', reject);
      encoder.on('exit', code => code === 0 ? resolve() : reject(new Error(`ffmpeg exited ${code}; see ${capture.directory}/ffmpeg.log`)));
    });
  } finally { fs.closeSync(encoderLog); }
  return { video, frames: capture.frames.length, durationSeconds: (capture.stopped - capture.started) / 1000,
    timing: path.join(capture.directory, 'timing.json') };
}
async function command(c) {
  if (c.action === 'recordStart') {
    if (recording) throw new Error('Recording already active');
    const name = safeName(c.name);
    const directory = path.join(output, name + '-frames');
    fs.mkdirSync(directory); // Never overwrite a prior recording's evidence.
    recording = { name, directory, started: Date.now(), frames: [] };
    await send('Page.startScreencast', { format: 'jpeg', quality: c.quality ?? 85, everyNthFrame: 1 });
    return { recording: name, directory };
  }
  if (c.action === 'recordStop') return stopRecording();
  if (c.action === 'mark') return { marker: c.text };
  if (c.action === 'navigate') {
    records = [];
    if (c.width) await send('Emulation.setDeviceMetricsOverride', { width: c.width, height: c.height, deviceScaleFactor: 1, mobile: false });
    await send('Page.navigate', { url: c.url });
    if (c.waitForUnity === false) { await delay(c.wait ?? 1000); return state(); }
    for (let i = 0; i < 240; i++) {
      await delay(1000);
      if ((await state()).ready) return state();
    }
    throw new Error('Unity loader did not complete');
  }
  if (c.action === 'resize') return send('Emulation.setDeviceMetricsOverride', { width: c.width, height: c.height, deviceScaleFactor: 1, mobile: false });
  if (c.action === 'click' || c.action === 'doubleClick') {
    if (c.action === 'doubleClick') await doubleClick(c);
    else await mouseClick(c.x, c.y, 1, c.holdMs ?? 750);
    await delay(c.wait ?? 500);
    return state();
  }
  if (c.action === 'key') {
    const code = c.key === ' ' ? 'Space' : `Key${c.key.toUpperCase()}`;
    const virtual = c.key.toUpperCase().charCodeAt(0);
    await send('Input.dispatchKeyEvent', { type: 'keyDown', key: c.key, code, windowsVirtualKeyCode: virtual });
    await delay(c.ms);
    await send('Input.dispatchKeyEvent', { type: 'keyUp', key: c.key, code, windowsVirtualKeyCode: virtual });
    return state();
  }
  if (c.action === 'hold') { await mouseClick(c.x, c.y, 1, c.ms); return state(); }
  if (c.action === 'drag') {
    await send('Input.dispatchMouseEvent', { type: 'mouseMoved', x: c.x, y: c.y });
    await send('Input.dispatchMouseEvent', { type: 'mousePressed', x: c.x, y: c.y, button: 'left', buttons: 1, clickCount: 1 });
    for (let i = 1; i <= 20; i++) {
      await send('Input.dispatchMouseEvent', { type: 'mouseMoved', x: c.x + (c.toX - c.x) * i / 20,
        y: c.y + (c.toY - c.y) * i / 20, buttons: 1 });
      await delay((c.ms ?? 1000) / 20);
    }
    await send('Input.dispatchMouseEvent', { type: 'mouseReleased', x: c.toX, y: c.toY, button: 'left', clickCount: 1 });
    return state();
  }
  if (c.action === 'wheel') {
    await send('Input.dispatchMouseEvent', { type: 'mouseWheel', x: c.x, y: c.y, deltaX: 0, deltaY: c.deltaY });
    await delay(c.wait ?? 1000); return state();
  }
  if (c.action === 'shot') {
    const name = safeName(c.name);
    const shot = await send('Page.captureScreenshot', { format: 'png' });
    const file = path.join(output, name + '.png');
    fs.writeFileSync(file, Buffer.from(shot.data, 'base64'));
    fs.writeFileSync(path.join(output, name + '.json'), JSON.stringify({ state: await state(), records }, null, 2));
    return { file, errors: records.filter(r => ['exception', 'error', 'networkFailure'].includes(r.type)) };
  }
  if (c.action === 'state') return { state: await state(), records };
  if (c.action === 'wait') { await delay(c.ms); return state(); }
  if (c.action === 'close') { if (recording) await stopRecording(); input.close(); return { closed: true }; }
  throw new Error('Unknown action');
}
try {
  const portFile = path.join(profile, 'DevToolsActivePort');
  for (let i = 0; !fs.existsSync(portFile); i++) {
    if (i > 150 || chrome.exitCode !== null) throw new Error('Chromium failed to start');
    await delay(200);
  }
  const port = fs.readFileSync(portFile, 'utf8').split('\n')[0];
  const targets = await (await fetch(`http://127.0.0.1:${port}/json/list`)).json();
  socket = new WebSocket(targets.find(t => t.type === 'page').webSocketDebuggerUrl);
  await new Promise((resolve, reject) => { socket.onopen = resolve; socket.onerror = reject; });
  socket.onmessage = e => {
    const m = JSON.parse(e.data);
    if (m.id && pending.has(m.id)) {
      const p = pending.get(m.id); pending.delete(m.id); clearTimeout(p.timer);
      m.error ? p.reject(new Error(JSON.stringify(m.error))) : p.resolve(m.result);
    } else if (m.method === 'Page.screencastFrame') {
      if (recording) {
        const file = `frame-${String(recording.frames.length).padStart(6, '0')}.jpg`;
        fs.writeFileSync(path.join(recording.directory, file), Buffer.from(m.params.data, 'base64'));
        recording.frames.push({ file, timestamp: m.params.metadata.timestamp, received: Date.now() });
      }
      send('Page.screencastFrameAck', { sessionId: m.params.sessionId }).catch(error => actionLog({ captureError: error.message }));
    } else if (m.method === 'Runtime.consoleAPICalled') records.push({ type: m.params.type, text: m.params.args.map(a => a.value ?? a.description).join(' ') });
    else if (m.method === 'Runtime.exceptionThrown') records.push({ type: 'exception', detail: m.params.exceptionDetails });
    else if (m.method === 'Network.loadingFailed') records.push({ type: 'networkFailure', detail: m.params });
  };
  await send('Page.enable'); await send('Runtime.enable'); await send('Network.enable');
  console.log(JSON.stringify({ ready: true, profile, output }));
  for await (const line of input) {
    let c;
    try {
      c = JSON.parse(line); actionLog({ phase: 'start', command: c });
      const result = await command(c);
      actionLog({ phase: 'end', action: c.action, result });
      console.log(JSON.stringify({ ok: true, action: c.action, result }));
      if (c.action === 'close') break;
    } catch (error) {
      actionLog({ phase: 'error', action: c?.action, error: error.message });
      console.log(JSON.stringify({ ok: false, error: error.message }));
    }
  }
} finally {
  if (recording) { try { await stopRecording(); } catch (error) { actionLog({ captureError: error.message }); } }
  if (socket?.readyState === WebSocket.OPEN) { try { await send('Browser.close'); } catch {} }
  for (const p of pending.values()) clearTimeout(p.timer);
  socket?.close(); chrome.kill('SIGTERM'); fs.closeSync(log);
}
