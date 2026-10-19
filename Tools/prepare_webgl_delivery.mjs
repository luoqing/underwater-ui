// Package an existing Unity build without changing any decoded asset bytes.
import { cp, mkdir, readFile, readdir, realpath, stat, writeFile } from 'node:fs/promises';
import path from 'node:path';
import { pathToFileURL } from 'node:url';
import { createHash } from 'node:crypto';
import { gzipSync, gunzipSync } from 'node:zlib';

const sha256 = bytes => createHash('sha256').update(bytes).digest('hex');

export async function prepareDelivery(input, output) {
  input = await realpath(input);
  output = path.join(await realpath(path.dirname(path.resolve(output))), path.basename(output));
  if (output === input || output.startsWith(input + path.sep)) {
    throw new Error('Output must be outside the input build');
  }
  let html = await readFile(path.join(input, 'index.html'), 'utf8');
  if (!/\bvar buildUrl = "Build";/.test(html)) throw new Error('Unsupported Unity template');
  const assets = [];
  for (const [field, suffix] of Object.entries({
    loaderUrl: 'loader.js', dataUrl: 'data', frameworkUrl: 'framework.js', codeUrl: 'wasm',
  })) {
    const pattern = new RegExp(`\\b${field}\\s*[:=]\\s*buildUrl \\+ "(/[^"\\r\\n]+)"`, 'g');
    const matches = [...html.matchAll(pattern)];
    if (matches.length !== 1) throw new Error(`Expected one ${field}`);
    const source = matches[0][1].slice(1);
    if (!/^[a-zA-Z0-9_.-]+$/.test(source) ||
        !source.endsWith('.' + suffix) && !source.endsWith('.' + suffix + '.gz')) {
      throw new Error(`Unsafe or unsupported asset: ${source}`);
    }
    const sourcePath = await realpath(path.join(input, 'Build', source));
    if (path.dirname(sourcePath) !== path.join(input, 'Build')) throw new Error('Asset escapes Build');
    if (!(await stat(sourcePath)).isFile()) throw new Error('Asset is not a file');
    assets.push({ source, sourcePath, suffix, match: matches[0][0] });
  }
  // Never overwrite a previous build or preview.
  await mkdir(output);
  for (const entry of await readdir(input)) {
    if (entry !== 'index.html' && entry !== 'Build') {
      await cp(path.join(input, entry), path.join(output, entry), { recursive: true });
    }
  }
  await mkdir(path.join(output, 'Build'));
  for (const entry of await readdir(path.join(input, 'Build'))) {
    if (!assets.some(asset => asset.source === entry)) {
      await cp(path.join(input, 'Build', entry), path.join(output, 'Build', entry), { recursive: true });
    }
  }
  const manifest = [];
  for (const asset of assets) {
    const original = await readFile(asset.sourcePath);
    const decoded = asset.source.endsWith('.gz') ? gunzipSync(original) : original;
    const hash = sha256(decoded);
    const compressed = asset.suffix !== 'loader.js';
    const delivered = compressed ? gzipSync(decoded, { level: 9 }) : decoded;
    const restored = compressed ? gunzipSync(delivered) : delivered;
    if (!restored.equals(decoded)) throw new Error(`Round-trip mismatch: ${asset.source}`);
    const filename = `${hash.slice(0, 32)}.${asset.suffix}${compressed ? '.gz' : ''}`;
    await writeFile(path.join(output, 'Build', filename), delivered, { flag: 'wx' });
    html = html.replace(asset.match, asset.match.replace('/' + asset.source, '/' + filename));
    manifest.push({ source: asset.source, filename, decodedBytes: decoded.length,
      transferBytes: delivered.length, sha256: hash, byteIdenticalAfterDecode: true });
  }
  await writeFile(path.join(output, 'index.html'), html, { flag: 'wx' });
  await writeFile(path.join(output, 'delivery-manifest.json'), JSON.stringify(manifest, null, 2) + '\n');
  return manifest;
}

if (process.argv[1] && import.meta.url === pathToFileURL(path.resolve(process.argv[1])).href) {
  if (process.argv.length !== 4) throw new Error('Usage: node prepare_webgl_delivery.mjs INPUT OUTPUT');
  console.log(JSON.stringify(await prepareDelivery(process.argv[2], process.argv[3]), null, 2));
}
