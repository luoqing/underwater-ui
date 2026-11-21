import * as T from "three";
import { outsideBall } from "./collision.js";

export const swimKeys = new Set([
  "KeyW",
  "KeyA",
  "KeyS",
  "KeyD",
  "KeyQ",
  "KeyE",
  "Space",
  "ShiftLeft",
  "ShiftRight",
  "ArrowUp",
  "ArrowDown",
  "ArrowLeft",
  "ArrowRight",
]);
const clamp = T.MathUtils.clamp;
export function createDiver() {
  const stick = new T.Vector2();
  const keys = new Set(),
    velocity = new T.Vector3(),
    desired = new T.Vector3(),
    fwd = new T.Vector3(),
    side = new T.Vector3(),
    offset = new T.Vector3();
  const position = new T.Vector3(),
    origin = new T.Vector3();
  let yaw = 0,
    pitch = -0.15,
    travel = 0;
  function reset(p, direction) {
    position.copy(p || new T.Vector3(-0.1, 1.82, 8.35));
    velocity.set(0, 0, 0);
    keys.clear();
    stick.set(0, 0);
    travel = 0;
    yaw = direction ? Math.atan2(-direction.x, -direction.z) : 0.115;
    pitch = direction ? Math.asin(clamp(direction.y, -1, 1)) : -0.085;
  }
  function turn(dx, dy) {
    yaw -= dx * 0.004;
    pitch = clamp(pitch - dy * 0.0035, -1.35, 1.35);
  }
  function press(code, down) {
    if (down) keys.add(code);
    else keys.delete(code);
  }
  function move(dt, camera, reef, colonies, animals, radius=.27, vessel=null) {
    if (keys.has("ArrowLeft")) yaw += dt * 1.35;
    if (keys.has("ArrowRight")) yaw -= dt * 1.35;
    if (keys.has("ArrowUp")) pitch = clamp(pitch + dt * 0.85, -1.35, 1.35);
    if (keys.has("ArrowDown")) pitch = clamp(pitch - dt * 0.85, -1.35, 1.35);
    const piloting = !!vessel;
    const helm = stick.x + Number(keys.has("KeyD")) - Number(keys.has("KeyA"));
    if (piloting) yaw -= helm * vessel.turn * dt;
    fwd.set(
      -Math.sin(yaw) * Math.cos(pitch),
      Math.sin(pitch),
      -Math.cos(yaw) * Math.cos(pitch),
    );
    side.set(Math.cos(yaw), 0, -Math.sin(yaw));
    desired
      .copy(fwd)
      .multiplyScalar(
        stick.y + Number(keys.has("KeyW")) - Number(keys.has("KeyS")),
      )
      .addScaledVector(
        side,
        piloting ? 0 : helm,
      );
    desired.y +=
      Number(keys.has("Space") || keys.has("KeyE")) - Number(keys.has("KeyQ"));
    desired
      .clampLength(0, 1)
      .multiplyScalar(
        keys.has("ShiftLeft") || keys.has("ShiftRight") ? (vessel ? vessel.thrust*1.4 : 4.9) : (vessel?.thrust ?? 2.65),
      );
    velocity.lerp(desired, 1 - Math.exp(-dt * 5.6));
    origin.copy(position);
    const steps = Math.max(1, Math.ceil((velocity.length() * dt) / 0.1));
    for (let i = 0; i < steps; i++) {
      position.addScaledVector(velocity, dt / steps);
      position.x = clamp(position.x, -78, 78);
      position.z = clamp(position.z, -117, 76);
      position.y = clamp(position.y, -48, 9.2);
      for (const c of colonies) {
        const rx = c.radius + radius,
          ry = c.height + radius;
        offset.subVectors(position, c.center);
        if (
          Math.abs(offset.x) > rx ||
          Math.abs(offset.z) > rx ||
          Math.abs(offset.y) > ry
        )
          continue;
        const k = Math.sqrt(
          (offset.x ** 2 + offset.z ** 2) / rx ** 2 + offset.y ** 2 / ry ** 2,
        );
        if (k < 1) {
          if (k < 0.001) position.y = c.center.y + ry;
          else position.copy(c.center).addScaledVector(offset, 1 / k);
        }
      }
      reef.separate(position, radius, velocity);
      if (radius>.3) position.y=Math.max(position.y,reef.ground(position.x,position.z)+radius);
      for (const a of animals)
        outsideBall(position, a.center, a.radius + radius + .07, velocity, offset);
    }
    travel += origin.distanceTo(position);
    camera.position.copy(position);
    camera.up.set(0, 1, 0);
    camera.lookAt(offset.copy(position).add(fwd));
    return {
      depth: (10 - position.y).toFixed(1),
      speed: velocity.length(),
      distance: travel.toFixed(1),
      zone:
        position.y < -18 ? "深水海盆" : position.z > 32 || position.z < -70 || Math.abs(position.x)>41 ? "外礁斜坡" : position.z > 15 ? "海龟浅滩" : position.z > -9 ? "珊瑚花园" : position.z > -25 ? "水母湾" : "鲸游峡口",
    };
  }
  return {
    position,
    throttle(){return clamp(stick.y+Number(keys.has("KeyW"))-Number(keys.has("KeyS")),-1,1)},
    reset,
    move,
    turn,
    press,
    setStick(x, y) {
      stick.set(x, y);
    },
    face(point) {
      offset.subVectors(point, position).normalize();
      yaw = Math.atan2(-offset.x, -offset.z);
      pitch = clamp(Math.asin(clamp(offset.y, -1, 1)), -1.35, 1.35);
    },
    clear() {
      keys.clear();
      stick.set(0, 0);
      velocity.set(0, 0, 0);
    },
  };
}
