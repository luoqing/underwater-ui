import * as T from "three";
import { outsideBall } from "./collision.js";

function schoolBatch(source, count, length, rotate, light, mobileMeshes) {
  const specimen = source.scene.getObjectByProperty("isMesh", true);
  const geometry = (
    mobileMeshes.get(specimen.geometry) || specimen.geometry
  ).clone();
  geometry.morphAttributes = {};
  if (rotate) geometry.rotateY(Math.PI / 2);
  geometry.center();
  geometry.computeBoundingBox();
  const k = length / (geometry.boundingBox.max.x - geometry.boundingBox.min.x);
  geometry.scale(k, k, k);
  const material = specimen.material.clone();
  light(material);
  const illuminate = material.onBeforeCompile;
  material.onBeforeCompile = (shader) => {
    illuminate(shader);
    shader.vertexShader = shader.vertexShader.replace(
      "#include <begin_vertex>",
      `#include <begin_vertex>
       float rear=clamp(.5-position.x/${length},0.,1.);
       transformed.z+=${length}*.105*rear*rear*sin(tideTime*7.+instanceMatrix[3].z+4.*position.x/${length});`,
    );
  };
  material.customProgramCacheKey = () => "reef-school-" + length;
  const batch = new T.InstancedMesh(geometry, material, count);
  batch.frustumCulled = false;
  batch.receiveShadow = true;
  return batch;
}
export function makeShoal(sources, reef, mobileMeshes, light) {
  const batches = [
    schoolBatch(sources.anthias, 17, 0.16, false, light, mobileMeshes),
    schoolBatch(sources.tang, 34, 0.245, false, light, mobileMeshes),
    schoolBatch(sources.mackerel, 17, 0.4, false, light, mobileMeshes),
  ];
  const featuredTang = new T.Object3D();
  const object = new T.Object3D(),
    desired = new T.Vector3(),
    delta = new T.Vector3(),
    target = new T.Vector3(),
    aim = new T.Vector3();
  const animals = Array.from({ length: 68 }, (_, i) => ({
    group: Math.floor(i / 17),
    phase: i * 2.39996,
    scale: 0.76 + ((i * 31) % 67) / 100,
    position: new T.Vector3(),
    velocity: new T.Vector3(),
    spawned: false,
  }));
  function update(t, dt, flow, large, eye, observingTang) {
    for (let i = 0; i < animals.length; i++) {
      const a = animals[i],
        g = a.group,
        q = a.phase;
      const orbit = t * (0.19 + g * 0.018) + g * 1.74;
      const silver = g === 2;
      const centersX = [-1.2, 1.05, 1.2, -1.8],
        centersZ = [3.8, 2.2, -22, -13];
      const radiusX = silver ? 4.4 : 1.25,
        radiusZ = silver ? 2.1 : 0.83;
      target.set(
        centersX[g] + radiusX * Math.sin(orbit) + Math.sin(q) * 1.08,
        (silver ? 4.2 : 1.86) +
          0.42 * Math.cos(q * 2.6) +
          0.18 * Math.sin(orbit * 1.7),
        centersZ[g] + radiusZ * Math.cos(orbit) + 0.74 * Math.cos(q * 1.07),
      );
      if (!a.spawned) {
        a.position.copy(target);
        a.velocity.set(Math.cos(orbit), 0, -0.25 * Math.sin(orbit));
        a.spawned = true;
      }
      desired.subVectors(target, a.position).multiplyScalar(1.6);
      desired.x += Math.cos(orbit) * radiusX * (0.19 + g * 0.018) + flow * 0.12;
      desired.z -= radiusZ * (0.19 + g * 0.018) * Math.sin(orbit);
      for (let j = 0; j < animals.length; j++) {
        if (i === j || !animals[j].spawned) continue;
        delta.subVectors(a.position, animals[j].position);
        const d = delta.length();
        if (d > 0 && d < 0.43)
          desired.addScaledVector(delta, ((0.43 - d) * 3.8) / d);
      }
      for (const b of large) {
        delta.subVectors(a.position, b.center);
        const d = delta.length();
        if (d > 0 && d < b.radius + 1)
          desired.addScaledVector(delta, ((b.radius + 1 - d) * 3) / d);
      }
      a.velocity.lerp(desired.clampLength(0.16, 2.1), 1 - Math.exp(-dt * 2.8));
      a.position.addScaledVector(a.velocity, dt);
      for (const b of large)
        outsideBall(
          a.position,
          b.center,
          b.radius + 0.19 * a.scale,
          a.velocity,
          delta,
        );
      if (!(observingTang && i === 17)) outsideBall(a.position, eye, 0.76, a.velocity, delta);
      reef.separate(a.position, 0.26 * a.scale, a.velocity);
      object.position.copy(a.position);
      aim.copy(a.velocity).normalize();
      object.rotation.order = "YXZ";
      object.rotation.set(
        0,
        Math.atan2(-aim.z, aim.x),
        Math.asin(T.MathUtils.clamp(aim.y, -0.6, 0.6)),
      );
      object.scale.setScalar(a.scale);
      object.updateMatrix();
      if (i === 17) {
        featuredTang.position.copy(object.position);
        featuredTang.rotation.copy(object.rotation);
      }
      batches[g === 3 ? 1 : g].setMatrixAt(
        (i % 17) + (g === 3 ? 17 : 0),
        object.matrix,
      );
    }
    for (const batch of batches) batch.instanceMatrix.needsUpdate = true;
  }
  return {
    batches,
    featuredTang,
    update,
    reset() {
      for (const a of animals) a.spawned = false;
    },
  };
}
