import { moonJelly } from "./moon-jelly.js";
import * as T from "three";

export function reefAnimals(scene, actor, sources, garden, reef) {
  const turtle = actor(sources.turtle, 1.48),
    flippers = ["FrontFlipper_L", "FrontFlipper_R"].map((name) =>
      turtle.getObjectByName(name),
    );
  const clowns = garden.homes.flatMap((home) =>
    Array.from({ length: 3 }, (_, i) => ({
      home,
      i,
      node: actor(sources.clown, 0.21 + i * 0.04),
    })),
  );
  const lions = Array.from({ length: 3 }, (_, i) => ({
    node: actor(sources.lion, 0.44 + i * 0.045),
    i,
  }));
  const model = moonJelly();
  const jellies = Array.from({ length: 10 }, () => {
    const j = model.clone();
    scene.add(j);
    return j;
  });
  const jellyBounds = jellies.map((j) => ({ center: j.position, radius: 1 }));
  const anchor = turtle.position;
  const clownMaterial = clowns[0].node.children[0].getObjectByProperty(
    "isMesh",
    true,
  ).material;
  const water = clownMaterial.onBeforeCompile;
  clownMaterial.onBeforeCompile = (shader) => {
    water(shader);
    shader.vertexShader = shader.vertexShader.replace(
      "#include <begin_vertex>",
      `#include <begin_vertex>
      float tail=clamp(.43-position.x,0.,1.);
      transformed.z+=.095*tail*tail*sin(tideTime*10.+modelMatrix[3].z*2.+position.x*4.);`,
    );
  };
  clownMaterial.customProgramCacheKey = () => "anemonefish-swim";
  function update(t) {
    const stroke = t * 2.0;
    const a = t * 0.18 - 0.024 * Math.cos(stroke);
    const effort = 0.5 + 0.5 * Math.sin(0.66 * t);
    anchor.set(
      -0.65 + 2.3 * Math.sin(a),
      2.6 + 0.46 * Math.sin(a * 0.75),
      -3.6 + 2.5 * Math.cos(a),
    );
    turtle.rotation.set(
      0.04 * Math.sin(a),
      Math.atan2(2.3 * Math.cos(a), -2.5 * Math.sin(a)),
      0.055 * Math.cos(a),
    );
    flippers.forEach((f, i) => {
      if (f) {
        const sign = i ? -1 : 1;
        f.rotation.y = sign * (0.1 + (0.12 + 0.42 * effort) * Math.sin(stroke));
        f.rotation.z = sign * (0.06 + 0.19 * effort * Math.cos(stroke));
      }
    });
    reef.separate(anchor, 0.48);
    for (const { node, i, home } of clowns) {
      const tempo = t * (0.61 + i * 0.055) + i * 2.2 + home.z * 0.15;
      const b = tempo + 0.31 * Math.sin(tempo * 2);
      const radius = 0.46 + 0.16 * Math.sin(tempo);
      node.position.copy(home);
      node.position.x += radius * Math.sin(b);
      node.position.z += radius * 0.73 * Math.cos(b);
      node.position.y += 0.4 + 0.12 * Math.sin(2 * b);
      node.rotation.set(
        0,
        Math.atan2(0.73 * Math.sin(b), Math.cos(b)),
        0.06 * Math.sin(b * 3),
      );
    }
    for (const { node, i } of lions) {
      const b = t * 0.22 + 0.32 * Math.sin(t * 0.49 + i) + i;
      node.position.set(
        (i % 2 ? -1 : 1) * (2.35 + 0.38 * Math.sin(b)),
        1.18 + 0.14 * Math.sin(b * 0.84),
        2.7 - 10 * i + 0.52 * Math.cos(b),
      );
      node.rotation.y = Math.atan2(
        0.52 * Math.sin(b),
        (i % 2 ? -1 : 1) * 0.38 * Math.cos(b),
      );
      reef.separate(node.position, 0.22);
      node.traverse((m) => {
        if (m.morphTargetInfluences) {
          m.morphTargetInfluences[0] = Math.max(0, Math.sin(t * 2 + i)) * 0.58;
          m.morphTargetInfluences[1] = Math.max(0, -Math.sin(t * 2 + i)) * 0.58;
        }
      });
    }
    jellies.forEach((j, i) => {
      const b = i * 2.4 + t * 0.16,
        phase = (t * (0.51 + i * 0.009) + i * 0.173) % 1,
        pulse =
          phase < 0.24
            ? T.MathUtils.smoothstep(phase, 0, 0.24)
            : 1 - T.MathUtils.smoothstep(phase, 0.24, 0.93);
      j.position.set(
        2.5 * Math.sin(i * 2.4) + Math.sin(b) * 0.71,
        2.65 +
          (i % 4) * 1.07 +
          0.38 * Math.sin(b * 0.78) +
          0.09 * Math.sin(phase * 2 * Math.PI - 0.9),
        -13 - (i % 6) * 1.78 + 0.52 * Math.cos(b),
      );
      j.rotation.set(0.11 * Math.sin(b), b, 0.13 * Math.cos(b));
      const size = 0.62 + (i % 5) * 0.14;
      jellyBounds[i].radius = 0.54 * size;
      j.scale.set(
        size * (1 - 0.12 * pulse),
        size * (1 + 0.14 * pulse),
        size * (1 - 0.12 * pulse),
      );
    });
  }
  return { update, turtle, jellyBounds, jellies, clowns, lions, jelly: jellies[3], clown: clowns[2].node, lion: lions[0].node };
}
