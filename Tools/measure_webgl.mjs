// Isolated Chromium smoke test. Software WebGL results are not iGPU benchmarks.
// Usage: node Tools/measure_webgl.mjs <url> <output-prefix> <chrome-path> [--repeat]
import fs from 'node:fs';
import os from 'node:os';
import path from 'node:path';
import { spawn } from 'node:child_process';
import { setTimeout as delay } from 'node:timers/promises';

const [url, output, executable, repeat] = process.argv.slice(2);
if (!url || !output || !executable) throw new Error('Expected URL, output prefix and Chromium executable');
const profile = fs.mkdtempSync(path.join(os.tmpdir(), 'underwater-review-'));
const chromeLog = fs.openSync(`${output}.chrome.log`, 'w');
const chrome = spawn(executable, [
  '--headless=new', '--no-sandbox', '--disable-dev-shm-usage',
  '--use-angle=swiftshader', '--enable-unsafe-swiftshader',
  '--no-proxy-server', '--no-first-run', '--remote-debugging-port=0',
  `--user-data-dir=${profile}`, 'about:blank'
], { stdio: ['ignore', chromeLog, chromeLog] });
const report = { url, softwareGPU: true, viewport: { width: 960, height: 540 }, console: [], errors: [], failedRequests: [], responses: [] };
let socket;
let timer;
let outputPrefix = output;
const pending = new Map();
let seq = 0;
function send(method, params = {}) {
  const id = ++seq;
  return new Promise((resolve, reject) => {
    const timeout = setTimeout(() => { pending.delete(id); reject(new Error(`CDP timeout: ${method}`)); }, 45000);
    pending.set(id, { resolve, reject, timeout });
    socket.send(JSON.stringify({ id, method, params }));
  });
}
async function run() {
  const portFile = path.join(profile, 'DevToolsActivePort');
  for (let i = 0; !fs.existsSync(portFile); i++) {
    if (i > 150 || chrome.exitCode != null) throw new Error('Chromium failed to start');
    await delay(200);
  }
  const port = fs.readFileSync(portFile, 'utf8').split('\n')[0];
  const targets = await (await fetch(`http://127.0.0.1:${port}/json/list`)).json();
  socket = new WebSocket(targets.find(t => t.type === 'page').webSocketDebuggerUrl);
  await new Promise((resolve, reject) => { socket.onopen = resolve; socket.onerror = reject; });
  socket.onmessage = event => {
    const m = JSON.parse(event.data);
    if (m.id && pending.has(m.id)) {
      const p = pending.get(m.id);
      pending.delete(m.id);
      clearTimeout(p.timeout);
      if (m.error) p.reject(new Error(JSON.stringify(m.error))); else p.resolve(m.result);
    } else if (m.method === 'Runtime.consoleAPICalled') {
      report.console.push({ type: m.params.type, text: m.params.args.map(a => a.value ?? a.description).join(' ') });
    } else if (m.method === 'Runtime.exceptionThrown') report.errors.push(m.params.exceptionDetails);
    else if (m.method === 'Network.loadingFailed') report.failedRequests.push(m.params);
    else if (m.method === 'Network.responseReceived') {
      const r = m.params.response;
      report.responses.push({ url: r.url, status: r.status, mimeType: r.mimeType, headers: r.headers, fromDiskCache: r.fromDiskCache });
    }
  };
  await send('Page.enable');
  await send('Runtime.enable');
  await send('Network.enable');
  await send('Emulation.setDeviceMetricsOverride', { ...report.viewport, deviceScaleFactor: 1, mobile: false });
  await measure();
  if (repeat === '--repeat') {
    fs.writeFileSync(`${outputPrefix}.json`, JSON.stringify(report, null, 2));
    outputPrefix = `${output}-warm`;
    report.console = [];
    report.errors = [];
    report.failedRequests = [];
    report.responses = [];
    delete report.state;
    delete report.resources;
    report.warm = true;
    await measure();
  }
}
async function measure() {
  await send('Page.navigate', { url });
  for (let i = 0; i < 120; i++) {
    await delay(1000);
    const result = await send('Runtime.evaluate', { expression: `JSON.stringify({ready:document.querySelector('#unity-loading-bar')?.style.display==='none',progress:document.querySelector('#unity-progress-bar-full')?.style.width,elapsed:performance.now(),metrics:window.underwaterLoadMetrics,canvas:document.querySelector('canvas')?{width:document.querySelector('canvas').width,height:document.querySelector('canvas').height}:null})`, returnByValue: true });
    report.state = JSON.parse(result.result.value);
    if (report.state.ready) break;
  }
  // The loader promise can resolve while Unity's splash screen is still showing.
  // Capture later and visually verify the scene; do not label loader readiness as playable time.
  if (!report.warm) await delay(10000);
  const resources = await send('Runtime.evaluate', { expression: `JSON.stringify(performance.getEntriesByType('resource').map(r=>({name:r.name,duration:r.duration,transferSize:r.transferSize,encodedBodySize:r.encodedBodySize,decodedBodySize:r.decodedBodySize})))`, returnByValue: true });
  report.resources = JSON.parse(resources.result.value);
  // The cold run supplies the visual smoke test. Warm runs measure cache/loader
  // behavior without another expensive software-GPU screenshot.
  if (!report.warm) {
    const screenshot = await send('Page.captureScreenshot', { format: 'png' });
    fs.writeFileSync(`${outputPrefix}.png`, Buffer.from(screenshot.data, 'base64'));
  }
  if (!report.state.ready) throw new Error('Unity did not become ready before timeout');
}
try {
  await Promise.race([run(), new Promise((_, reject) => { timer = setTimeout(() => reject(new Error('Overall timeout')), 240000); })]);
} catch (error) {
  report.failure = error.message;
  process.exitCode = 1;
} finally {
  clearTimeout(timer);
  for (const p of pending.values()) clearTimeout(p.timeout);
  socket?.close();
  chrome.kill('SIGTERM');
  fs.writeFileSync(`${outputPrefix}.json`, JSON.stringify(report, null, 2));
  console.log(JSON.stringify({ output: outputPrefix, state: report.state, failure: report.failure, exceptions: report.errors.length, failedRequests: report.failedRequests.length }));
}
