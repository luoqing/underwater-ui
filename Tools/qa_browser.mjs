// Interactive, isolated Chromium checks for the Unity canvas. Commands are JSON lines.
import fs from 'node:fs';
import os from 'node:os';
import path from 'node:path';
import { spawn } from 'node:child_process';
import { createInterface } from 'node:readline';
import { setTimeout as delay } from 'node:timers/promises';

const executable = process.argv[2];
const output = path.resolve(process.argv[3]);
fs.mkdirSync(output, { recursive: true });
const profile = fs.mkdtempSync(path.join(os.tmpdir(), 'underwater-bugfix-qa-'));
const log = fs.openSync(path.join(output, 'chromium.log'), 'w');
const chrome = spawn(executable, ['--headless=new', '--no-sandbox', '--disable-dev-shm-usage',
  '--use-angle=swiftshader', '--enable-unsafe-swiftshader', '--no-proxy-server', '--no-first-run',
  '--disable-background-timer-throttling', '--disable-renderer-backgrounding',
  '--remote-debugging-port=0', `--user-data-dir=${profile}`, 'about:blank'],
{ stdio: ['ignore', log, log] });
let socket;
let sequence = 0;
let records = [];
const pending = new Map();
const input = createInterface({ input: process.stdin });
function send(method, params = {}) {
  const id = ++sequence;
  return new Promise((resolve, reject) => {
    const timer = setTimeout(() => { pending.delete(id); reject(new Error(`Timeout: ${method}`)); }, 120000);
    pending.set(id, { resolve, reject, timer });
    socket.send(JSON.stringify({ id, method, params }));
  });
}
async function state() {
  const result = await send('Runtime.evaluate', { expression: `JSON.stringify({
    ready:document.querySelector('#unity-loading-bar')?.style.display==='none',
    progress:document.querySelector('#unity-progress-bar-full')?.style.width,
    url:location.href,width:innerWidth,height:innerHeight,
    canvas:document.querySelector('canvas')?{width:document.querySelector('canvas').width,height:document.querySelector('canvas').height}:null,
    resources:performance.getEntriesByType('resource').filter(r=>r.name.includes('/Build/')).map(r=>({name:r.name,transferSize:r.transferSize,duration:r.duration}))})`, returnByValue: true });
  return JSON.parse(result.result.value);
}
async function click(x, y, count = 1) {
  await send('Input.dispatchMouseEvent', { type: 'mouseMoved', x, y });
  await send('Input.dispatchMouseEvent', { type: 'mousePressed', x, y, button: 'left', buttons: 1, clickCount: count });
  await delay(750);
  await send('Input.dispatchMouseEvent', { type: 'mouseReleased', x, y, button: 'left', clickCount: count });
}
async function command(c) {
  if (c.action === 'navigate') {
    records = [];
    if (c.width) await send('Emulation.setDeviceMetricsOverride', { width: c.width, height: c.height, deviceScaleFactor: 1, mobile: false });
    await send('Page.navigate', { url: c.url });
    for (let i = 0; i < 240; i++) {
      await delay(1000);
      if ((await state()).ready) return state();
    }
    throw new Error('Unity loader did not complete');
  }
  if (c.action === 'resize') return send('Emulation.setDeviceMetricsOverride', { width: c.width, height: c.height, deviceScaleFactor: 1, mobile: false });
  if (c.action === 'wheel') {
    await send('Input.dispatchMouseEvent', { type: 'mouseWheel', x: c.x, y: c.y, deltaX: 0, deltaY: c.deltaY });
    await delay(c.wait ?? 1000);
    return state();
  }
  if (c.action === 'click') { await click(c.x, c.y, c.count ?? 1); await delay(c.wait ?? 500); return state(); }
  if (c.action === 'key') {
    const code = c.key === ' ' ? 'Space' : `Key${c.key.toUpperCase()}`;
    const virtual = c.key.toUpperCase().charCodeAt(0);
    await send('Input.dispatchKeyEvent', { type: 'keyDown', key: c.key, code, windowsVirtualKeyCode: virtual });
    await delay(c.ms);
    await send('Input.dispatchKeyEvent', { type: 'keyUp', key: c.key, code, windowsVirtualKeyCode: virtual });
    return state();
  }
  if (c.action === 'hold') {
    await send('Input.dispatchMouseEvent', { type: 'mouseMoved', x: c.x, y: c.y });
    await send('Input.dispatchMouseEvent', { type: 'mousePressed', x: c.x, y: c.y, button: 'left', buttons: 1, clickCount: 1 });
    await delay(c.ms);
    await send('Input.dispatchMouseEvent', { type: 'mouseReleased', x: c.x, y: c.y, button: 'left', clickCount: 1 });
    return state();
  }
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
  if (c.action === 'shot') {
    if (!/^[a-zA-Z0-9_-]+$/.test(c.name)) throw new Error('Invalid screenshot name');
    const shot = await send('Page.captureScreenshot', { format: 'png' });
    const file = path.join(output, c.name + '.png');
    fs.writeFileSync(file, Buffer.from(shot.data, 'base64'));
    const snapshot = { state: await state(), records };
    fs.writeFileSync(path.join(output, c.name + '.json'), JSON.stringify(snapshot, null, 2));
    return { file, errors: records.filter(r=>r.type === 'exception' || r.type === 'error') };
  }
  if (c.action === 'state') return { state: await state(), records };
  if (c.action === 'wait') { await delay(c.ms); return state(); }
  if (c.action === 'close') { input.close(); return { closed: true }; }
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
  socket = new WebSocket(targets.find(t=>t.type === 'page').webSocketDebuggerUrl);
  await new Promise((resolve, reject)=>{ socket.onopen = resolve; socket.onerror = reject; });
  socket.onmessage = e => {
    const m = JSON.parse(e.data);
    if (m.id && pending.has(m.id)) {
      const p = pending.get(m.id); pending.delete(m.id); clearTimeout(p.timer);
      m.error ? p.reject(new Error(JSON.stringify(m.error))) : p.resolve(m.result);
    } else if (m.method === 'Runtime.consoleAPICalled') records.push({ type: m.params.type, text: m.params.args.map(a=>a.value ?? a.description).join(' ') });
    else if (m.method === 'Runtime.exceptionThrown') records.push({ type: 'exception', detail: m.params.exceptionDetails });
    else if (m.method === 'Network.loadingFailed') records.push({ type: 'networkFailure', detail: m.params });
  };
  await send('Page.enable'); await send('Runtime.enable'); await send('Network.enable');
  console.log(JSON.stringify({ ready: true }));
  for await (const line of input) {
    try { const c = JSON.parse(line); console.log(JSON.stringify({ ok: true, action: c.action, result: await command(c) })); if (c.action === 'close') break; }
    catch (e) { console.log(JSON.stringify({ ok: false, error: e.message })); }
  }
} finally {
  if (socket?.readyState === WebSocket.OPEN) {
    try { await send('Browser.close'); } catch {}
  }
  for (const p of pending.values()) clearTimeout(p.timer);
  socket?.close(); chrome.kill('SIGTERM');
}
