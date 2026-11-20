import * as T from "three";
import file from "./assets/mobile-lod.bin";
export async function mobileDetail(sources) {
  const data = await (await fetch(file)).arrayBuffer();
  const header = new DataView(data).getUint32(0, true);
  const catalog = JSON.parse(
    new TextDecoder().decode(new Uint8Array(data, 4, header)),
  );
  const meshes = new WeakMap();
  for (const [name, source] of Object.entries(sources)) {
    source.scene.traverse((mesh) => {
      if (!mesh.isMesh) return;
      const { meshes: id, primitives: primitive } =
        source.parser.associations.get(mesh);
      const row = catalog[name]?.[id + "/" + (primitive ?? 0)];
      if (!row) return;
      const high = mesh.geometry,
        low = new T.BufferGeometry();
      low.attributes = { ...high.attributes };
      low.morphAttributes = high.morphAttributes;
      low.morphTargetsRelative = high.morphTargetsRelative;
      low.boundingBox = high.boundingBox;
      low.boundingSphere = high.boundingSphere;
      low.setIndex(
        new T.BufferAttribute(
          new Uint32Array(data, 4 + header + row[0], row[1]),
          1,
        ),
      );
      meshes.set(high, low);
    });
  }
  return meshes;
}
