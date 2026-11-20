import * as T from "three";
import { legacyPigment } from "./reef-pigment.js";
import { MeshBVH } from "three-mesh-bvh";

// All dimensions below refer to metres in the new canyon. Keep the original GLB UVs,
// normal maps and relief, then place its different models in overlapping formations.
export function legacyReefs(scene, reef, sources, light, mobileMeshes) {
  const objects = [],
    ray = new T.Ray(),
    local = new T.Vector3(),
    normal = new T.Vector3();
  const hit = { point: new T.Vector3(), distance: 0, faceIndex: 0 };
  function plant(source, layout) {
    let model;
    source.scene.traverse((m) => {
      if (m.isMesh) model = m;
    });
    const g = model.geometry;
    g.computeBoundingBox();
    const bounds = g.boundingBox,
      extent = bounds.getSize(new T.Vector3()),
      width = Math.max(extent.x, extent.y, extent.z);
    const tree = new MeshBVH(g, { maxLeafTris: 16 });
    light(model.material);
    for (const [x, z, span, az, embed, tint] of layout) {
      const scale = span / width,
        mesh = new T.Mesh(mobileMeshes.get(g) || g, model.material.clone());
      mesh.material.onBeforeCompile = model.material.onBeforeCompile;
      mesh.material.customProgramCacheKey =
        model.material.customProgramCacheKey;
      const palette = [0xe97143, 0xcd78c4, 0xef9f40, 0xb295ee, 0xdd7165];
      legacyPigment(
        mesh.material,
        bounds,
        palette[objects.length % palette.length],
        objects.length * 0.81,
        light,
      );
      mesh.position.set(x, reef.ground(x, z) - scale * bounds.min.y - embed, z);
      mesh.scale.setScalar(scale);
      mesh.rotation.y = az;
      mesh.updateMatrixWorld();
      mesh.castShadow = mesh.receiveShadow = true;
      scene.add(mesh);
      const box = bounds.clone().applyMatrix4(mesh.matrixWorld);
      objects.push({
        tree,
        mesh,
        scale,
        box,
        inverse: mesh.matrixWorld.clone().invert(),
      });
    }
  }
  const reefs = [],
    shelves = [],
    clusters = [];
  for (let i = 0; i < 6; i++) {
    const z = 5 - i * 9.6,
      side = i % 2 ? 1 : -1;
    reefs.push([
      side * (5.8 + 0.65 * Math.sin(i)),
      z,
      5.4 + (i % 3) * 0.6,
      i * 1.48,
      0.84,
      0xfff4e8,
    ]);
    shelves.push(
      [-side * 5.1, z - 1.3, 4.3 + 0.45 * (i % 3), i * 2.63, 0.56, 0xfff2e7],
      [side * 4.4, z - 3.1, 3.3, i * 1.15, 0.34, 0xf6edff],
    );
    clusters.push(
      [
        side * 3.45,
        z - 0.8,
        2.1,
        i * 1.29,
        0.36,
        [0xffba97, 0xdcb4ef, 0xffcf9a][i % 3],
      ],
      [-side * 3.8, z - 3.0, 1.85, i * 2.51, 0.24, 0xffcba8],
    );
  }
  // Isolated reef heads break up the sand flats while leaving a swim-through on both sides.
  shelves.push(
    [2.6, -8.6, 2.9, 0.57, 0.33, 0xffe1c5],
    [1.5, -18.6, 3.5, 2.68, 0.44, 0xe9eedb],
    [-0.85, -42, 3.9, 1.7, 0.54, 0xf6ddd5],
  );
  plant(sources.garden, reefs);
  plant(sources.shelf, shelves);
  plant(sources.cluster, clusters);
  function ground(x, z) {
    let y = reef.ground(x, z);
    for (const o of objects) {
      if (
        x < o.box.min.x ||
        x > o.box.max.x ||
        z < o.box.min.z ||
        z > o.box.max.z
      )
        continue;
      ray.origin.set(x, 16, z).applyMatrix4(o.inverse);
      ray.direction.set(0, -1, 0);
      const h = o.tree.raycastFirst(ray, T.DoubleSide);
      if (h) y = Math.max(y, 16 - h.distance * o.scale);
    }
    return y;
  }
  function separate(p, r, v) {
    reef.separate(p, r, v);
    for (const o of objects) {
      if (o.box.distanceToPoint(p) > r) continue;
      local.copy(p).applyMatrix4(o.inverse);
      if (
        !o.tree.closestPointToPoint(local, hit, 0, r / o.scale) ||
        hit.distance * o.scale >= r
      )
        continue;
      normal
        .subVectors(local, hit.point)
        .transformDirection(o.mesh.matrixWorld);
      p.addScaledVector(normal, r - hit.distance * o.scale + 0.002);
      if (v) v.addScaledVector(normal, -Math.min(0, v.dot(normal)));
    }
  }
  function cameraArm(a, b, r) {
    reef.cameraArm(a, b, r);
    let dist = a.distanceTo(b);
    const direction = normal.subVectors(b, a).normalize();
    for (const o of objects) {
      ray.origin.copy(a);
      ray.direction.copy(direction);
      if (!ray.intersectsBox(o.box)) continue;
      ray.applyMatrix4(o.inverse);
      const h = o.tree.raycastFirst(ray, T.DoubleSide, 0.001, dist / o.scale);
      if (h) dist = Math.max(0.075, h.distance * o.scale - r);
    }
    b.copy(a).addScaledVector(direction, dist);
    separate(b, r);
  }
  function blocked(line, distance) {
    if (reef.blocked(line, distance)) return true;
    return objects.some(o => {
      if (!line.intersectsBox(o.box)) return false;
      ray.copy(line).applyMatrix4(o.inverse);
      return !!o.tree.raycastFirst(ray, T.DoubleSide, .001, (distance-.015)/o.scale);
    });
  }
  return { ground, separate, cameraArm, blocked };
}
