import test from 'node:test';
import assert from 'node:assert/strict';
import { mkdtemp, mkdir, readFile, writeFile, rm } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import path from 'node:path';
import { gunzipSync, gzipSync } from 'node:zlib';
import { prepareDelivery } from './prepare_webgl_delivery.mjs';

async function fixture(t, compressed = false) {
  const root = await mkdtemp(path.join(tmpdir(), 'webgl-delivery-test-'));
  t.after(() => rm(root, { recursive: true, force: true }));
  const input = path.join(root, 'original');
  await mkdir(path.join(input, 'Build'), { recursive: true });
  await mkdir(path.join(input, 'StreamingAssets'));
  await writeFile(path.join(input, 'StreamingAssets', 'animal.bin'), 'unchanged animal');
  let html = 'var buildUrl = "Build";\n';
  for (const [field, suffix] of Object.entries({ loaderUrl: 'loader.js', dataUrl: 'data',
    frameworkUrl: 'framework.js', codeUrl: 'wasm' })) {
    const bytes = Buffer.from(`Exact ${suffix} contents\0`.repeat(100));
    const zipped = compressed && suffix !== 'loader.js';
    const name = `game.${suffix}${zipped ? '.gz' : ''}`;
    html += `${field}: buildUrl + "/${name}",\n`;
    await writeFile(path.join(input, 'Build', name), zipped ? gzipSync(bytes) : bytes);
  }
  html += 'config.devicePixelRatio = 2; // quality unchanged\n';
  await writeFile(path.join(input, 'index.html'), html);
  return { input, output: path.join(root, 'preview'), html };
}

for (const compressed of [false, true]) {
  test(`lossless packaging, compressed input=${compressed}`, async t => {
    const { input, output, html } = await fixture(t, compressed);
    const manifest = await prepareDelivery(input, output);
    let expected = html;
    for (const asset of manifest) {
      const before = await readFile(path.join(input, 'Build', asset.source));
      const after = await readFile(path.join(output, 'Build', asset.filename));
      assert.deepEqual(asset.filename.endsWith('.gz') ? gunzipSync(after) : after,
        asset.source.endsWith('.gz') ? gunzipSync(before) : before);
      assert.match(asset.filename, /^[a-f0-9]{32}\./);
      expected = expected.replace('/' + asset.source, '/' + asset.filename);
    }
    assert.equal(await readFile(path.join(output, 'index.html'), 'utf8'), expected);
    assert.equal(await readFile(path.join(input, 'index.html'), 'utf8'), html);
    assert.equal(await readFile(path.join(output, 'StreamingAssets', 'animal.bin'), 'utf8'), 'unchanged animal');
    await assert.rejects(prepareDelivery(input, output), { code: 'EEXIST' });
  });
}

test('reject nested output and unsafe/missing references', async t => {
  const { input, output, html } = await fixture(t);
  await assert.rejects(prepareDelivery(input, path.join(input, 'preview')), /outside/);
  await writeFile(path.join(input, 'index.html'), html.replace('/game.data', '/../game.data'));
  await assert.rejects(prepareDelivery(input, output), /Unsafe/);
  await writeFile(path.join(input, 'index.html'), html.replace('dataUrl:', 'missing:'));
  await assert.rejects(prepareDelivery(input, output), /Expected one dataUrl/);
});
