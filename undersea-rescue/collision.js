import { DoubleSide, Ray, Vector3 } from "three";
import { MeshBVH } from "three-mesh-bvh";

// The canyon GLB stores both meshes directly in world metres.
export function obstacles(meshes) {
  const trees = meshes.map((m) => new MeshBVH(m.geometry, { maxLeafTris: 12 }));
  const hit = { point: new Vector3(), distance: 0, faceIndex: 0 },
    normal = new Vector3(),
    ray = new Ray();
  function separate(point, radius, velocity) {
    for (const tree of trees) {
      if (
        !tree.closestPointToPoint(point, hit, 0, radius) ||
        hit.distance >= radius
      )
        continue;
      normal.subVectors(point, hit.point).normalize();
      if (normal.lengthSq() < 0.01) normal.set(0, 1, 0);
      point.addScaledVector(normal, radius - hit.distance + 0.002);
      if (velocity)
        velocity.addScaledVector(normal, -Math.min(0, velocity.dot(normal)));
    }
  }
  function cameraArm(origin, desired, radius) {
    ray.direction.subVectors(desired, origin);
    let distance = ray.direction.length();
    ray.direction.normalize();
    ray.origin.copy(origin);
    for (const tree of trees) {
      const h = tree.raycastFirst(ray, DoubleSide, 0.001, distance);
      if (h) distance = Math.max(0.1, h.distance - radius);
    }
    desired.copy(origin).addScaledVector(ray.direction, distance);
    separate(desired, radius);
  }
  function ground(x, z) {
    ray.origin.set(x, 16, z);
    ray.direction.set(0, -1, 0);
    let y = -55;
    for (const tree of trees) {
      const h = tree.raycastFirst(ray, DoubleSide, 0, 80);
      if (h) y = Math.max(y, h.point.y);
    }
    return y;
  }
  function blocked(line, distance) {
    return trees.some(tree => tree.raycastFirst(line, DoubleSide, 0.001, distance - 0.015));
  }
  return { separate, cameraArm, ground, blocked };
}

export function outsideBall(point, center, radius, velocity, offset) {
  offset.subVectors(point, center);
  const d = offset.length();
  if (d >= radius) return;
  if (d < 0.0001) offset.set(1, 0, 0);
  else offset.divideScalar(d);
  point.addScaledVector(offset, radius - d);
  velocity.addScaledVector(offset, -Math.min(0, velocity.dot(offset)));
}
