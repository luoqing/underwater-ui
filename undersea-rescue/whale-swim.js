import * as T from "three";

const clamp = T.MathUtils.clamp;
const wrap = (a) => Math.atan2(Math.sin(a), Math.cos(a));

export function whaleMotion(animal, index, waterMaterial) {
  const joints = ["WhaleBody", "WhaleTailBase", "WhaleTailTip"].map((name) =>
    animal.getObjectByName(name),
  );
  const fins = { value: new T.Vector2() };
  animal.traverse((mesh) => {
    if (!mesh.isMesh) return;
    const material = mesh.material.clone();
    waterMaterial(material);
    const water = material.onBeforeCompile;
    material.onBeforeCompile = (shader) => {
      water(shader);
      shader.uniforms.finAngle = fins;
      shader.vertexShader =
        "uniform vec2 finAngle;\n" +
        shader.vertexShader.replace(
          "#include <begin_vertex>",
          `#include <begin_vertex>
        float side = sign(position.x);
        float span = smoothstep(.135,.32,abs(position.x)) * smoothstep(-.14,-.064,position.z) * (1.-smoothstep(.105,.19,position.z));
        float a = span * (finAngle.x + side * finAngle.y);
        vec2 arm = vec2(abs(position.x) - .135, position.y + .085);
        transformed.x = side * (.135 + cos(a)*arm.x - sin(a)*arm.y);
        transformed.y = -.085 + sin(a)*arm.x + cos(a)*arm.y;`,
        );
    };
    material.customProgramCacheKey = () => "whale-pectoral";
    mesh.material = material;
  });
  const motion = {
    phase: index * 2.3,
    elapsed: 0,
    distance: 0,
    speed: 0,
    heading: null,
    previous: null,
    reset() {
      this.previous = null;
      this.heading = null;
      this.speed = 0;
      this.elapsed = 0;
      this.phase = index * 2.3;
      this.distance = 0;
    },
    advance(dt) {
      this.elapsed += dt;
      // A short series of propulsive strokes followed by a longer glide.
      const cycle =
        0.5 + 0.5 * Math.cos(this.elapsed * (index ? 0.54 : 0.69) + index);
      this.effort = T.MathUtils.smoothstep(cycle, 0.15, 0.8);
      this.phase += dt * (index ? 2.65 : 3.25) * (0.63 + 0.37 * this.effort);
      this.stride =
        0.88 +
        0.1 * this.effort -
        0.18 * this.effort * Math.cos(this.phase * 0.999);
      this.distance += dt * (index ? 2.85 : 2.55) * this.stride;
    },
    follow(position, target, dt, pace = 1) {
      const dx = target.x - position.x,
        dz = target.z - position.z,
        gap = Math.hypot(dx, dz);
      let heading = this.heading ?? animal.rotation.y;
      const turn = wrap(Math.atan2(dx, dz) - heading);
      heading += clamp(turn, -1.15 * dt, 1.15 * dt);
      const speed =
        Math.min(gap * 1.08, 4.65) *
        (0.2 + 0.8 * Math.max(0, Math.cos(turn))) *
        this.stride * pace;
      this.speed = T.MathUtils.damp(this.speed, speed, 1.5, dt);
      position.x += Math.sin(heading) * this.speed * dt;
      position.z += Math.cos(heading) * this.speed * dt;
      position.y += clamp((target.y - position.y) * 0.78, -0.8, 0.8) * dt;
      animal.rotation.set(
        -Math.atan2(clamp(target.y - position.y, -0.7, 0.7), 3),
        heading,
        clamp(-turn * 0.12, -0.15, 0.15),
        "YXZ",
      );
    },
    pose(dt) {
      const yaw = animal.rotation.y;
      const turn =
        this.heading === null || dt <= 0
          ? 0
          : clamp(wrap(yaw - this.heading) / dt, -1, 1);
      const speed =
        this.previous && dt > 0
          ? this.previous.distanceTo(animal.position) / dt
          : 0;
      this.heading = yaw;
      (this.previous ||= new T.Vector3()).copy(animal.position);
      const strength =
        (0.2 + 0.8 * this.effort) *
        (0.45 + 0.55 * T.MathUtils.smoothstep(speed, 0.2, 2.4));
      joints.forEach((joint, i) => {
        if (!joint) return;
        joint.rotation.x =
          [0.045, 0.27, 0.37][i] *
          strength *
          Math.sin(this.phase - [0.0, 0.48, 1.08][i]);
        joint.rotation.z = [0, 0.033, 0.05][i] * turn;
      });
      fins.value.set(
        0.04 + 0.065 * Math.sin(this.phase * 0.51) * strength,
        T.MathUtils.damp(fins.value.y, turn * 0.35, 2.4, dt),
      );
    },
  };
  return motion;
}
