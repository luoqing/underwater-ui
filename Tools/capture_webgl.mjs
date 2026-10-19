import fs from 'node:fs';
import os from 'node:os';
import path from 'node:path';
import { spawn } from 'node:child_process';
import { setTimeout as delay } from 'node:timers/promises';

const [url, output, executable, waitSeconds = '18'] = process.argv.slice(2);
if (!url || !output || !executable) {
  throw new Error('Expected URL, output PNG, and Chromium executable');
}

const profile = fs.mkdtempSync(path.join(os.tmpdir(), 'ocean-capture-'));
const chromeLog = fs.openSync(`${output}.chrome.log`, 'w');
const chrome = spawn(executable, [
  '--no-sandbox', '--disable-dev-shm-usage', '--no-proxy-server',
  '--use-angle=swiftshader', '--enable-webgl', '--ignore-gpu-blocklist',
  '--enable-unsafe-swiftshader', '--remote-debugging-port=0',
  '--window-size=1440,900', '--hide-scrollbars', '--no-first-run',
  '--disable-background-timer-throttling', '--disable-renderer-backgrounding',
  '--disable-backgrounding-occluded-windows',
  `--user-data-dir=${profile}`, 'about:blank'
], { stdio: ['ignore', chromeLog, chromeLog] });

let socket;
let sequence = 0;
const pending = new Map();

function send(method, params = {}, timeoutMs = 120000) {
  const id = ++sequence;
  return new Promise((resolve, reject) => {
    const timeout = setTimeout(() => {
      pending.delete(id);
      reject(new Error(`CDP timeout: ${method}`));
    }, timeoutMs);
    pending.set(id, { resolve, reject, timeout });
    socket.send(JSON.stringify({ id, method, params }));
  });
}

try {
  const portFile = path.join(profile, 'DevToolsActivePort');
  for (let i = 0; !fs.existsSync(portFile); i++) {
    if (i > 150 || chrome.exitCode != null) throw new Error('Chromium failed to start');
    await delay(200);
  }
  const port = fs.readFileSync(portFile, 'utf8').split('\n')[0];
  const targets = await (await fetch(`http://127.0.0.1:${port}/json/list`)).json();
  socket = new WebSocket(targets.find(target => target.type === 'page').webSocketDebuggerUrl);
  await new Promise((resolve, reject) => {
    socket.onopen = resolve;
    socket.onerror = reject;
  });
  socket.onmessage = event => {
    const message = JSON.parse(event.data);
    if (!message.id || !pending.has(message.id)) return;
    const request = pending.get(message.id);
    pending.delete(message.id);
    clearTimeout(request.timeout);
    if (message.error) request.reject(new Error(JSON.stringify(message.error)));
    else request.resolve(message.result);
  };

  await send('Page.enable');
  await send('Runtime.enable');
  await send('Emulation.setDeviceMetricsOverride', {
    width: 1440,
    height: 900,
    deviceScaleFactor: 1,
    mobile: false
  });
  await send('Page.navigate', { url });

  let state;
  for (let i = 0; i < 180; i++) {
    await delay(1000);
    const result = await send('Runtime.evaluate', {
      expression: `JSON.stringify({ready:document.querySelector('#unity-loading-bar')?.style.display==='none',progress:document.querySelector('#unity-progress-bar-full')?.style.width})`,
      returnByValue: true
    });
    state = JSON.parse(result.result.value);
    if (state.ready) break;
  }
  if (!state?.ready) throw new Error('Unity did not become ready');

  await delay(Number(waitSeconds) * 1000);
  const screenshot = await send('Page.captureScreenshot', {
    format: 'png',
    fromSurface: true,
    captureBeyondViewport: false
  }, 180000);
  fs.writeFileSync(output, Buffer.from(screenshot.data, 'base64'));
  console.log(JSON.stringify({ output, state }));
} finally {
  for (const request of pending.values()) clearTimeout(request.timeout);
  socket?.close();
  chrome.kill('SIGTERM');
}
