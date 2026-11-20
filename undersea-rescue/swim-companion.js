import * as T from "three";

export function swimCompanion(host, camera, reef, colonies, animals, onSelect) {
  const caster = new T.Raycaster(), pointer = new T.Vector2();
  const specs = new Map(animals.map(a => [a.node, a]));
  const proxy = new T.Object3D(), matrix = new T.Matrix4();
  const position = new T.Vector3(), rotation = new T.Quaternion();
  const basePosition = new T.Vector3(), baseRotation = new T.Quaternion();
  const goal = new T.Vector3(), delta = new T.Vector3(), forward = new T.Vector3(), angles = new T.Euler(0,0,0,"YXZ");
  let current, node, instance, depth, returning = false, applied = false;
  function point(x, y) {
    const rect = host.getBoundingClientRect();
    pointer.set(2*(x-rect.left)/rect.width-1, 1-2*(y-rect.top)/rect.height);
  }
  function restore() {
    if (!applied) return;
    if (instance == null) {
      node.position.copy(basePosition);
      node.quaternion.copy(baseRotation);
    }
    applied = false;
  }
  function stop(immediate = false) {
    if (!current) return;
    if (!returning) onSelect(null);
    returning = true;
    if (immediate) {
      restore();
      current = null;
    }
  }
  function toggle(x, y, allowed) {
    if (current && !returning) { stop(); return; }
    point(x, y);
    caster.setFromCamera(pointer, camera);
    const roots = animals.filter(a => a.node.visible && allowed(a)).map(a => a.node);
    const hit = caster.intersectObjects(roots, true)[0];
    if (!hit || reef.blocked(caster.ray, hit.distance)) return;
    let root = hit.object;
    while (root && !specs.has(root)) root = root.parent;
    if (!root) return;
    stop(true);
    current = specs.get(root);
    instance = hit.instanceId;
    node = root;
    if (instance != null) {
      root.getMatrixAt(instance, matrix);
      matrix.decompose(proxy.position, proxy.quaternion, proxy.scale);
      node = proxy;
    }
    position.copy(node.position);
    rotation.copy(node.quaternion);
    depth = T.MathUtils.clamp(camera.position.distanceTo(position), current.radius + 1.2, 10 + current.radius);
    returning = false;
    onSelect(current.name);
  }
  function update(dt) {
    if (!current) return;
    if (instance != null) {
      current.node.getMatrixAt(instance, matrix);
      matrix.decompose(node.position, node.quaternion, node.scale);
    }
    basePosition.copy(node.position);
    baseRotation.copy(node.quaternion);
    if (returning) {
      const mix = 1 - Math.exp(-dt*2.6);
      position.lerp(basePosition,mix);
      rotation.slerp(baseRotation,mix);
      if (position.distanceToSquared(basePosition)<.009 && rotation.angleTo(baseRotation)<.12) {
        current = null;
        return;
      }
    } else {
      caster.setFromCamera(pointer,camera);
      caster.ray.at(depth,goal);
      delta.subVectors(goal,position);
      const distance = delta.length();
      if (distance>.04) {
        delta.divideScalar(distance);
        const step = Math.min(distance, dt*current.speed);
        if (current.axis !== "drift") {
          const rise = Math.asin(T.MathUtils.clamp(delta.y,-.65,.65));
          const foreZ = current.axis === "z", yaw = foreZ ? Math.atan2(delta.x,delta.z) : Math.atan2(-delta.z,delta.x);
          const reversed = current.axis === "-x";
          angles.set(foreZ ? -rise : 0, reversed ? yaw+Math.PI : yaw, foreZ ? 0 : reversed ? -rise : rise,"YXZ");
          rotation.rotateTowards(newRotation.setFromEuler(angles), dt*(current.speed>2 ? 1 : 2.4));
          forward.set(foreZ ? 0 : reversed ? -1 : 1,0,foreZ ? 1 : 0).applyQuaternion(rotation);
          position.addScaledVector(forward,step*(.18+.82*Math.max(0,forward.dot(delta))));
        } else {
          rotation.slerp(baseRotation,1-Math.exp(-dt*2));
          position.addScaledVector(delta,step);
        }
      }
      const r = current.radius;
      position.x = T.MathUtils.clamp(position.x,-13,13);
      position.z = T.MathUtils.clamp(position.z,-54,13);
      position.y = T.MathUtils.clamp(position.y,-.25,9.7-r);
      reef.separate(position,r);
      for (const c of colonies) {
        delta.subVectors(position,c.center);
        const rx = c.radius+r, ry = c.height+r;
        const length = Math.hypot(delta.x/rx,delta.y/ry,delta.z/rx);
        if (length<1) {
          if (length<.001) position.y=c.center.y+ry;
          else position.copy(c.center).addScaledVector(delta,1/length);
        }
      }
    }
    node.position.copy(position);
    node.quaternion.copy(rotation);
    if (instance != null) {
      node.updateMatrix();
      current.node.setMatrixAt(instance,node.matrix);
      current.node.instanceMatrix.needsUpdate=true;
    }
    applied=true;
  }
  const newRotation = new T.Quaternion();
  return { point, toggle, update, restore, stop, get active() { return !!current && !returning; } };
}
