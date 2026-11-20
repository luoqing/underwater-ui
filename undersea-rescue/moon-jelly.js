import * as T from "three";
import { mergeGeometries } from "three/addons/utils/BufferGeometryUtils.js";

// One shared translucent bell, oral-arm mesh and fringe for the entire bay.
export function moonJelly() {
  const tau = Math.PI * 2;
  const profile = Array.from({ length: 23 }, (_, k) => {
    const a = (k / 22) * 1.68;
    return new T.Vector2(0.45 * Math.sin(a), 0.07 + 0.225 * Math.cos(a));
  }).reverse();
  const dome = new T.LatheGeometry(profile, 72);
  const shell = new T.MeshStandardMaterial({
    color: 0xc5dce8,
    roughness: 0.47,
    transparent: true,
    opacity: 0.47,
    depthWrite: false,
    side: T.DoubleSide,
    forceSinglePass: true,
  });
  shell.onBeforeCompile = (s) => {
    s.fragmentShader = s.fragmentShader.replace(
      "#include <opaque_fragment>",
      `float rim=pow(1.-abs(dot(normalize(normal),normalize(vViewPosition))),2.);
       diffuseColor.a *= .30+.68*rim;
       outgoingLight += vec3(.055,.075,.09)*rim;
       #include <opaque_fragment>`,
    );
  };
  const inner = [],
    pink = new T.Color(0xd5a6c0),
    milky = new T.Color(0xc7c6da);
  function tint(g, c) {
    const colors = [];
    for (let i = 0; i < g.attributes.position.count; i++)
      colors.push(c.r, c.g, c.b);
    g.setAttribute("color", new T.Float32BufferAttribute(colors, 3));
    return g;
  }
  for (let k = 0; k < 4; k++) {
    const phi = (k * tau) / 4;
    const points = [];
    for (let i = 0; i <= 20; i++) {
      const a = -2.32 + (4.64 * i) / 20;
      const x = 0.096 + 0.055 * Math.cos(a),
        z = 0.049 * Math.sin(a);
      points.push(
        new T.Vector3(
          x * Math.cos(phi) - z * Math.sin(phi),
          0.186 - 0.018 * Math.cos(a),
          x * Math.sin(phi) + z * Math.cos(phi),
        ),
      );
    }
    inner.push(
      tint(
        new T.TubeGeometry(new T.CatmullRomCurve3(points), 24, 0.009, 6, false),
        pink,
      ),
    );
    const arm = new T.PlaneGeometry(1, 1, 5, 17);
    const pos = arm.attributes.position;
    for (let i = 0; i < pos.count; i++) {
      const v = 0.5 - pos.getY(i),
        side = pos.getX(i),
        angle = phi + 0.8 * v;
      const width =
        0.047 * (1 - 0.8 * v) * side * (1 + 0.34 * Math.sin(v * 44 + k));
      const rad = 0.038 + v * 0.052 + 0.022 * Math.sin(8 * v + k);
      pos.setXYZ(
        i,
        rad * Math.cos(angle) - width * Math.sin(angle),
        0.13 - v * (0.33 + 0.035 * (k % 2)) + 0.011 * Math.sin(44 * v) * side,
        rad * Math.sin(angle) + width * Math.cos(angle),
      );
    }
    arm.computeVertexNormals();
    inner.push(tint(arm, milky));
  }
  const anatomy = mergeGeometries(inner);
  inner.forEach((g) => g.dispose());
  const body = new T.Mesh(
    anatomy,
    new T.MeshStandardMaterial({
      vertexColors: true,
      roughness: 0.82,
      transparent: true,
      opacity: 0.69,
      side: T.DoubleSide,
      forceSinglePass: true,
      depthWrite: false,
    }),
  );
  const threads = [];
  for (let i = 0; i < 120; i++) {
    const a = (tau * i) / 120,
      r = 0.446,
      h = 0.046 + 0.032 * (0.5 + 0.5 * Math.sin(i * 3.1));
    threads.push(
      r * Math.cos(a),
      0.048,
      r * Math.sin(a),
      (r - 0.009) * Math.cos(a + 0.008),
      0.048 - h,
      (r - 0.009) * Math.sin(a + 0.008),
    );
  }
  const fringe = new T.BufferGeometry();
  fringe.setAttribute("position", new T.Float32BufferAttribute(threads, 3));
  const group = new T.Group();
  group.add(
    body,
    new T.LineSegments(
      fringe,
      new T.LineBasicMaterial({
        color: 0xcad8e3,
        transparent: true,
        opacity: 0.37,
        depthWrite: false,
      }),
    ),
    new T.Mesh(dome, shell),
  );
  return group;
}
