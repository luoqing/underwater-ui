// Build compact index-only meshes. All UVs, joint weights and textures stay on the original vertices.
import { readFile, writeFile } from "node:fs/promises";
import { MeshoptSimplifier } from "meshoptimizer";
await MeshoptSimplifier.ready;
const targets = {
  "tidal-canyon": 0.21,
  whale: 0.14,
  manta: 0.15,
  turtle: 0.22,
  clownfish: 0.28,
  "moon-jelly": 0.16,
  lionfish: 0.3,
  "reef-garden": 0.19,
  "reef-shelf": 0.19,
  "coral-colony": 0.2,
  "sea-fan": 0.3,
  "sea-fan-distant": 0.25,
  "horse-mackerel": 0.16,
  anthias:.19, "blue-tang":.22, threadfin:.28, "moon-wrasse":.25,
};
let offset = 0;
const catalog = {},
  chunks = [];
for (const [name, ratio] of Object.entries(targets)) {
  const raw = await readFile(
    new URL("../assets/" + name + ".glb", import.meta.url),
  );
  const length = raw.readUInt32LE(12),
    doc = JSON.parse(raw.subarray(20, 20 + length));
  function readAccessor(id) {
    const a = doc.accessors[id],
      v = doc.bufferViews[a.bufferView];
    const Type = {
      5126: Float32Array,
      5125: Uint32Array,
      5123: Uint16Array,
      5121: Uint8Array,
    }[a.componentType];
    const dim = a.type === "VEC3" ? 3 : 1;
    const stride =
      (v.byteStride || Type.BYTES_PER_ELEMENT * dim) / Type.BYTES_PER_ELEMENT;
    const values = new Type(
      raw.buffer,
      raw.byteOffset + 28 + length + (v.byteOffset || 0) + (a.byteOffset || 0),
      (a.count - 1) * stride + dim,
    );
    if (stride === dim) return values;
    const packed = new Type(a.count * dim);
    for (let i = 0; i < a.count; i++)
      packed.set(values.subarray(i * stride, i * stride + dim), i * dim);
    return packed;
  }
  catalog[name] = {};
  for (const [meshId, m] of doc.meshes.entries())
    for (const [primId, p] of m.primitives.entries()) {
      const pos = readAccessor(p.attributes.POSITION),
        idx = new Uint32Array(readAccessor(p.indices));
      const [low] = MeshoptSimplifier.simplify(
        idx,
        pos,
        3,
        3 * Math.floor((idx.length * ratio) / 3),
        0.04,
      );
      catalog[name][meshId + "/" + primId] = [offset, low.length];
      chunks.push(Buffer.from(low.buffer, low.byteOffset, low.byteLength));
      offset += low.byteLength;
    }
}
const json = Buffer.from(JSON.stringify(catalog)),
  header = Buffer.alloc(4 + Math.ceil(json.length / 4) * 4, 32);
header.writeUInt32LE(header.length - 4, 0);
json.copy(header, 4);
await writeFile(
  new URL("../assets/mobile-lod.bin", import.meta.url),
  Buffer.concat([header, ...chunks]),
);
