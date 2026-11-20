import * as T from "three";
import stoneSkin from "./assets/sandstone.jpg";
import acroporaSkin from "./assets/acropora.jpg";
import brainSkin from "./assets/brain.jpg";
import folioseSkin from "./assets/foliose.jpg";

const tau = Math.PI * 2,
  vec = (x, y, z) => new T.Vector3(x, y, z);
function seeded(seed) {
  return () =>
    (seed = (Math.imul(1664525, seed) + 1013904223) >>> 0) / 4294967296;
}
const rng = seeded(737148);
let distant = false;
const between = (lo, hi) => lo + (hi - lo) * rng();

class Surface {
  positions = [];
  uvs = [];
  indices = [];
  colors = [];
  flex = [];
  sites = [];
  vertex(p, u, v, color, flex = 0) {
    const id = this.positions.length / 3;
    this.positions.push(p.x, p.y, p.z);
    this.uvs.push(u, v);
    this.colors.push(color.r, color.g, color.b);
    this.flex.push(flex);
    return id;
  }
  grid(rows, cols, sample) {
    const first = this.positions.length / 3;
    for (let j = 0; j <= rows; j++)
      for (let i = 0; i <= cols; i++)
        this.vertex(...sample(i / cols, j / rows));
    for (let j = 0; j < rows; j++)
      for (let i = 0; i < cols; i++) {
        const a = first + j * (cols + 1) + i;
        this.indices.push(
          a,
          a + cols + 1,
          a + 1,
          a + 1,
          a + cols + 1,
          a + cols + 2,
        );
      }
  }
  branch(points, radii, base, tip = base, flex = 0, sides = 7) {
    if (distant) sides = Math.min(4, sides);
    const path = new T.CatmullRomCurve3(points),
      steps = distant ? 2 : Math.max(3, points.length),
      frames = path.computeFrenetFrames(steps, false);
    this.grid(steps, sides, (u, v) => {
      const i = Math.round(v * steps),
        a = u * tau,
        at = v * (radii.length - 1),
        lo = Math.floor(at),
        r = T.MathUtils.lerp(
          radii[lo],
          radii[Math.min(lo + 1, radii.length - 1)],
          at - lo,
        );
      const p = path
        .getPointAt(v)
        .addScaledVector(frames.normals[i], r * Math.cos(a))
        .addScaledVector(frames.binormals[i], r * Math.sin(a));
      return [p, u, v * 1.7, base.clone().lerp(tip, v ** 2), flex * v * v];
    });
  }
  finish() {
    const geo = new T.BufferGeometry();
    geo.setAttribute(
      "position",
      new T.Float32BufferAttribute(this.positions, 3),
    );
    geo.setAttribute("uv", new T.Float32BufferAttribute(this.uvs, 2));
    geo.setAttribute("color", new T.Float32BufferAttribute(this.colors, 3));
    geo.setAttribute("flex", new T.Float32BufferAttribute(this.flex, 1));
    geo.setIndex(this.indices);
    geo.computeVertexNormals();
    return geo;
  }
}
const color = (h) => new T.Color(h);
function staghorn() {
  const s = new Surface(),
    base = color(0xf1a26d),
    tip = color(0xc184ff);
  function grow(p, dir, length, r, depth) {
    const end = p.clone().addScaledVector(dir, length),
      mid = p.clone().lerp(end, 0.52);
    mid.x += between(-0.05, 0.05);
    s.branch(
      [p, mid, end],
      [r, r * 0.79, depth ? r * 0.55 : 0.003],
      base,
      tip,
      0,
      7,
    );
    if (!depth) {
      s.sites.push(end);
      return;
    }
    for (let i = 0; i < (depth === 1 ? 3 : 2); i++) {
      const a = between(0, tau),
        v = dir
          .clone()
          .multiplyScalar(0.51)
          .add(vec(Math.cos(a) * 0.68, between(0.2, 0.9), Math.sin(a) * 0.68))
          .normalize();
      grow(
        p.clone().lerp(end, between(0.53, 0.94)),
        v,
        length * between(0.46, 0.75),
        r * 0.62,
        depth - 1,
      );
    }
  }
  for (let i = 0; i < (distant ? 7 : 12); i++) {
    const a = i * 2.4,
      r = between(0.02, 0.32);
    grow(
      vec(r * Math.cos(a), 0, r * Math.sin(a)),
      vec(
        Math.cos(a) * 0.65,
        between(0.5, 1.1),
        Math.sin(a) * 0.65,
      ).normalize(),
      between(0.36, 0.72),
      between(0.045, 0.082),
      distant ? 2 : 3,
    );
  }
  return s;
}
function plates() {
  const s = new Surface(),
    base = color(0xfaa78e),
    rim = color(0xfff1c9);
  for (let j = 0; j < (distant ? 5 : 10); j++) {
    const a = j * 2.4,
      r = (1 - j / 14) * between(0.55, 0.9),
      x = 0.35 * Math.cos(a),
      z = 0.34 * Math.sin(a),
      h = 0.12 + j * 0.077;
    s.grid(distant ? 3 : 8, distant ? 20 : 48, (u, v) => {
      const ang = u * tau,
        edge =
          1 + 0.12 * Math.sin(5 * ang + a) + 0.047 * Math.sin(11 * ang + a);
      const y =
        h +
        v * v * (0.16 + 0.11 * Math.sin(ang * 4 + a)) +
        0.12 * v * Math.cos(ang + a);
      const p = vec(
        x + r * v * edge * Math.cos(ang),
        y,
        z + r * v * edge * Math.sin(ang),
      );
      if (v === 1 && Math.round(u * 48) % 6 === 0) s.sites.push(p.clone());
      return [p, p.x * 2, p.z * 2, base.clone().lerp(rim, v ** 13)];
    });
  }
  return s;
}
function brain(tissue) {
  const s = new Surface();
  function sample(u, v) {
    const a = u * tau,
      b = v * Math.PI * 0.57;
    const rad = 0.77 * (1 + 0.055 * Math.sin(a * 5 + 0.5) * Math.sin(b * 2));
    return vec(
      rad * Math.sin(b) * Math.cos(a),
      0.59 * Math.cos(b) + 0.15,
      rad * 0.8 * Math.sin(b) * Math.sin(a),
    );
  }
  s.grid(distant ? 8 : 36, distant ? 20 : 80, (u, v) => [
    sample(u, v),
    u * 4.8,
    v * 1.6,
    color(0xffe58c),
  ]);
  if (!distant) {
    // Seat mouths in the dark valleys of the same mirrored atlas used on the skeleton.
    const mirror = (n) => 1 - Math.abs(1 - (n % 2));
    for (let row = 2; row < 44; row++) {
      const v0 = row / 51,
        count = Math.ceil(139 * Math.sin(v0 * Math.PI * 0.57));
      for (let k = 0; k < count; k++) {
        const u = (k + 0.5 + 0.32 * Math.sin(k * 16.3 + row * 2.7)) / count;
        const v = v0 + 0.005 * Math.sin(k * 2.6 - row * 4.1);
        const x = Math.round(mirror(u * 4.8) * (tissue.width - 1));
        const y = Math.round((1 - mirror(v * 1.6)) * (tissue.height - 1));
        const offset = (y * tissue.width + x) * 4;
        const green = tissue.data[offset + 1];
        if (green >= 119) continue;
        // Interpolate the same triangle as the visible colony, including between UV rows.
        const col = u * 80,
          rowV = v * 36,
          x0 = Math.floor(col),
          y0 = Math.floor(rowV);
        const du = col - x0,
          dv = rowV - y0;
        const upper = du + dv > 1;
        const point = sample(
          (x0 + Number(upper)) / 80,
          (y0 + Number(upper)) / 36,
        )
          .multiplyScalar(Math.abs(1 - du - dv))
          .addScaledVector(sample((x0 + 1) / 80, y0 / 36), upper ? 1 - dv : du)
          .addScaledVector(sample(x0 / 80, (y0 + 1) / 36), upper ? 1 - du : dv);
        point.pigment = new T.Color().setRGB(
          tissue.data[offset] / 255,
          green / 255,
          tissue.data[offset + 2] / 255,
          T.SRGBColorSpace,
        );
        s.sites.push(point);
      }
    }
  }
  return s;
}
function seaFan(source) {
  let m;
  source.scene.traverse((n) => {
    if (n.isMesh) m = n;
  });
  const g = m.geometry,
    p = g.attributes.position,
    flex = [],
    sites = [];
  for (let i = 0; i < p.count; i++) {
    const y = Math.max(0, p.getY(i));
    flex.push(y * y * 0.061);
    if (i % 67 === 0 && p.getZ(i) > 0.008 && y > 0.4)
      sites.push(vec(p.getX(i), p.getY(i), p.getZ(i)));
  }
  g.setAttribute("flex", new T.Float32BufferAttribute(flex, 1));
  return { finish: () => g, sites, map: m.material.map };
}
function softColony(anemone) {
  const s = new Surface(),
    n = anemone ? (distant ? 46 : 120) : distant ? 11 : 38,
    root = color(anemone ? 0xec8f50 : 0xd96a9c),
    tip = color(anemone ? 0xffca84 : 0xffbacc);
  for (let i = 0; i < n; i++) {
    const a = i * 2.39996,
      r = 0.61 * Math.sqrt((i + 0.5) / n),
      p = vec(r * Math.cos(a), 0.02, r * Math.sin(a));
    const h =
      (anemone ? 0.32 : 0.63) * (1.25 - 0.55 * r) + between(-0.09, 0.11);
    const end = p.clone().add(vec(0.15 * Math.cos(a), h, 0.15 * Math.sin(a)));
    if (anemone) {
      const bend = vec(0.056 * Math.sin(a * 4), 0, 0.055 * Math.cos(a * 3));
      const top = end
        .clone()
        .add(vec(0.035 * Math.cos(a), 0.028, 0.035 * Math.sin(a)));
      s.branch(
        [
          p,
          p.clone().lerp(end, 0.42).add(bend),
          p.clone().lerp(end, 0.73).sub(bend),
          end,
          top,
        ],
        [0.022, 0.027, 0.025, 0.044, 0.002],
        root,
        tip,
        0.13,
        7,
      );
      continue;
    }
    s.branch(
      [
        p,
        p
          .clone()
          .lerp(end, 0.55)
          .add(vec(0.04, 0.04, -0.05)),
        end,
      ],
      [
        anemone ? 0.03 : 0.041,
        anemone ? 0.041 : 0.036,
        anemone ? 0.001 : 0.022,
      ],
      root,
      tip,
      0.085,
      6,
    );

    for (let k = 0; k < 8; k++) {
      const b = (k * tau) / 8 + a,
        q = end.clone().add(vec(0.13 * Math.cos(b), 0.08, 0.13 * Math.sin(b)));
      s.branch(
        [
          end,
          end
            .clone()
            .lerp(q, 0.7)
            .add(vec(0, 0.035, 0)),
          q,
        ],
        [0.014, 0.013, 0.002],
        tip,
        tip,
        0.1,
        4,
      );
      if (!distant) {
        // Paired pinnules follow each of the eight soft-coral tentacles.
        const side = vec(-Math.sin(b), 0, Math.cos(b));
        for (let row = 1; row <= 4; row++) {
          const v = row / 5,
            root = end.clone().lerp(q, v);
          root.y += 0.034 * Math.sin(Math.PI * v);
          for (const sign of [-1, 1]) {
            s.grid(1, 1, (u, w) => {
              const leaf = root
                .clone()
                .addScaledVector(side, sign * w * (0.029 - 0.012 * v));
              leaf.x +=
                Math.cos(b) * (0.014 * w + (u - 0.5) * (0.009 - 0.008 * w));
              leaf.z +=
                Math.sin(b) * (0.014 * w + (u - 0.5) * (0.009 - 0.008 * w));
              leaf.y += 0.014 * w;
              return [leaf, u, w, tip];
            });
          }
        }
      }
    }
    s.sites.push(end);
  }
  for (let i = 0; i < s.flex.length; i++)
    s.flex[i] = Math.max(0, s.positions[3 * i + 1]) ** 1.5 * 0.17;
  return s;
}
function polyp() {
  const s = new Surface();
  // A low mantle with an elongated oral slit and two irregular rows of soft lobes.
  const radii = [0.53, 0.43, 0.32, 0.2, 0.1, 0.038, 0.002];
  const heights = [-0.065, 0.014, 0.073, 0.1, 0.079, 0.041, 0.032];
  s.grid(6, 28, (u, v) => {
    const i = Math.round(v * 6),
      a = u * tau;
    const pleat = 0.5 + 0.5 * Math.cos(12 * a + 0.65 * Math.sin(3 * a));
    const r = radii[i] * (0.93 + 0.07 * pleat);
    const tone = [0.57, 0.7, 0.9, 1, 0.77, 0.4, 0.25][i];
    return [
      vec(
        r * Math.cos(a),
        heights[i] + 0.011 * pleat,
        r * Math.sin(a) * (0.6 + 0.16 * (1 - v)),
      ),
      u,
      v,
      color(0xc9b28b).multiplyScalar(tone),
    ];
  });
  for (let i = 0; i < 14; i++) {
    const a = (i * tau) / 14 + 0.092 * Math.sin(i * 3.1);
    const reach = 0.11 + 0.13 * (0.5 + 0.5 * Math.cos(i * 3.78));
    const h = 0.13 + 0.13 * (0.5 + 0.5 * Math.sin(i * 2.41));
    const point = (r, y, twist) =>
      vec(r * Math.cos(a + twist), y, 0.7 * r * Math.sin(a + twist));
    s.branch(
      [
        point(0.235, 0.068, 0),
        point(0.27, 0.096, 0.015),
        point(0.31, h, 0.045),
        point(0.29 + reach, h + 0.027, 0.11),
        point(0.31 + reach, h + 0.01, 0.17),
      ],
      [0.057, 0.052, 0.038, 0.021, 0.009],
      color(0xb49d76),
      color(0xd4c69c),
      0.068,
      6,
    );
  }
  return s.finish();
}
function movingMaterial(mat, clock, water, currentOnly = false) {
  water(mat);
  const prior = mat.onBeforeCompile;
  mat.onBeforeCompile = (shader) => {
    prior(shader);
    shader.fragmentShader = shader.fragmentShader.replace(
      "#include <opaque_fragment>",
      "outgoingLight+=diffuseColor.rgb*.19;\n#include <opaque_fragment>",
    );
    shader.uniforms.coralClock = clock;
    shader.vertexShader =
      "attribute float flex; uniform float coralClock;\n" + shader.vertexShader;
    shader.vertexShader = shader.vertexShader.replace(
      "#include <begin_vertex>",
      `#include <begin_vertex>
    vec3 colony=instanceMatrix[3].xyz;
    float sway=sin(coralClock*1.38+colony.z*.57+position.y*.81);
    float surge=.42*flow+.24*sin(coralClock*.55+colony.x*.68)+.59*sway;
    ${
      currentOnly
        ? `vec3 drift=vec3(.81*surge,0.,.36*cos(coralClock*.92+colony.z*.7));
    vec3 localDrift=vec3(dot(normalize(instanceMatrix[0].xyz),drift),
      dot(normalize(instanceMatrix[1].xyz),drift),dot(normalize(instanceMatrix[2].xyz),drift));
    transformed += flex*(localDrift+vec3(.09*sin(coralClock*1.65+position.x*8.+colony.x*3.),-.13*abs(sway),0.));`
        : `transformed.x+=flex*(surge+.27*sin(coralClock*2.1+position.z*5.));
    transformed.z+=flex*.52*cos(coralClock*.92+colony.z*.7);
    `
    } `,
    );
  };
  mat.customProgramCacheKey = () =>
    currentOnly ? "tentacles-tide" : "reef-tide";
  return mat;
}

export async function coralGarden(
  scene,
  ground,
  water,
  fanSource,
  fanFarSource,
  mobile,
) {
  const tex = new T.TextureLoader();
  const maps = await Promise.all(
    [acroporaSkin, folioseSkin, brainSkin].map((u) => tex.loadAsync(u)),
  );
  for (const t of maps) {
    t.colorSpace = T.SRGBColorSpace;
    t.wrapS = t.wrapT = T.MirroredRepeatWrapping;
    t.anisotropy = 4;
  }
  const sampler = document.createElement("canvas");
  sampler.width = maps[2].image.width;
  sampler.height = maps[2].image.height;
  const ctx = sampler.getContext("2d");
  ctx.drawImage(maps[2].image, 0, 0);
  const tissue = ctx.getImageData(0, 0, sampler.width, sampler.height);
  const clock = { value: 0 },
    kinds = [
      staghorn(),
      plates(),
      brain(tissue),
      seaFan(fanSource),
      softColony(false),
      softColony(true),
    ];
  const mats = kinds.map((_, i) =>
    movingMaterial(
      new T.MeshStandardMaterial({
        side: T.DoubleSide,
        vertexColors: i !== 3,
        roughness: 0.78,
        map: i === 3 ? kinds[i].map : maps[i] || null,
        bumpMap: maps[i] || null,
        bumpScale: i === 2 ? 0.036 : 0.012,
      }),
      clock,
      water,
    ),
  );
  const fanSkin = mats[3].onBeforeCompile;
  mats[3].onBeforeCompile = (shader) => {
    fanSkin(shader);
    shader.fragmentShader = shader.fragmentShader.replace(
      "#include <color_fragment>",
      `#include <color_fragment>
      float tissue=dot(diffuseColor.rgb,vec3(.35,.45,.20));
      diffuseColor.rgb=mix(diffuseColor.rgb,vec3(.72,.043,.24)*(.32+1.8*sqrt(tissue)),.60);
    `,
    );
  };
  mats[3].customProgramCacheKey = () => "rose-seafan";
  const plateSkin = mats[1].onBeforeCompile;
  mats[1].onBeforeCompile = (shader) => {
    plateSkin(shader);
    shader.fragmentShader = shader.fragmentShader.replace(
      "#include <color_fragment>",
      `#include <color_fragment>
      float tissue=dot(diffuseColor.rgb,vec3(.35,.50,.15));
      diffuseColor.rgb=mix(diffuseColor.rgb,vec3(1.,.20,.063)*(.17+1.65*sqrt(tissue)),.64);
    `,
    );
  };
  mats[1].customProgramCacheKey = () => "salmon-foliose";
  const batches = kinds.map(() => []),
    colliders = [],
    colonies = [],
    homes = [];
  const tmp = new T.Object3D();
  function plant(i, x, z, scale, angle = between(0, tau)) {
    const y = ground(x, z);
    if (y > 8.4) return;
    tmp.position.set(x, y - 0.04 * scale, z);
    tmp.rotation.set(0, angle, 0);
    tmp.scale.setScalar(scale);
    tmp.updateMatrix();
    const matrix = tmp.matrix.clone();
    batches[i].push(matrix);
    const radius = [0.77, 0.8, 0.71, 0.69, 0.61, 0.58][i] * scale;
    const center = vec(
      x,
      y + scale * [0.42, 0.34, 0.26, 0.64, 0.34, 0.19][i],
      z,
    );
    colliders.push({
      center,
      radius,
      height: scale * [0.57, 0.47, 0.42, 0.75, 0.45, 0.28][i],
    });
    colonies.push({ kind: i, center, matrix, sites: kinds[i].sites });
    if (i === 5) homes.push(center);
  }
  // Colonies grow in thickets; the middle of the winding sand channel stays open.
  for (let bank = 0; bank < 2; bank++)
    for (let patch = 0; patch < 21; patch++) {
      const z = 10 - patch * 2.85 + between(-0.8, 0.8),
        x =
          (bank ? 1 : -1) *
          (3.6 + 0.9 * Math.sin(z * 0.23) + between(-0.4, 1.5));
      const dominant = [0, 1, 2, 3, 4][(patch * 3 + bank) % 5];
      for (let n = 0; n < 3; n++) {
        const a = between(0, tau),
          r = between(0.12, 1.62),
          kind = n < 2 ? dominant : Math.floor(between(0, 5));
        plant(
          kind,
          x + r * Math.cos(a),
          z + r * Math.sin(a),
          between(0.46, 1.1) * (n === 0 ? 1.08 : 1),
          kind === 3 ? between(-0.62, 0.67) : undefined,
        );
      }
    }
  for (const [x, z] of [
    [-2.4, 2.8],
    [2.8, -7.7],
    [-3.4, -20.1],
    [3.3, -39],
  ])
    plant(5, x, z, 1.3);
  // Distinct silhouettes in the first view at both sides of the entry.
  plant(3, -2.38, 4.2, 1.4, 0.15);
  plant(1, 2.92, 4.05, 1.04);
  plant(0, 2.55, 1, 1.42);
  plant(2, -3.2, 0.1, 1.25);
  plant(2, -1.64, 5.35, 0.94);
  distant = true;
  const low = [
    staghorn(),
    plates(),
    brain(),
    seaFan(fanFarSource),
    softColony(false),
    softColony(true),
  ];
  distant = false;
  const lods = batches.map((items, i) => {
    const pair = [kinds[i], low[i]].map((s, k) => {
      const m = new T.InstancedMesh(s.finish(), mats[i], items.length);
      m.receiveShadow = true;
      m.castShadow = k === 0;
      m.frustumCulled = false;
      scene.add(m);
      return m;
    });
    return { items, pair };
  });
  const at = new T.Vector3();
  const frustum = new T.Frustum(),
    vp = new T.Matrix4(),
    oldVP = new T.Matrix4(),
    sphere = new T.Sphere();
  function visibleReefs(camera) {
    const eye = camera.position;
    camera.updateMatrixWorld();
    vp.multiplyMatrices(camera.projectionMatrix, camera.matrixWorldInverse);
    if (vp.equals(oldVP)) return;
    oldVP.copy(vp);
    frustum.setFromProjectionMatrix(vp);
    for (const { items, pair } of lods) {
      const counts = [0, 0];
      for (const matrix of items) {
        at.setFromMatrixPosition(matrix);
        const d = at.distanceToSquared(eye);
        sphere.center.copy(at);
        sphere.radius = 2.5;
        if (d > (mobile ? 27 : 38) ** 2 || !frustum.intersectsSphere(sphere))
          continue;
        const k = d < (mobile ? 2.8 : 7.6) ** 2 ? 0 : 1;
        pair[k].setMatrixAt(counts[k]++, matrix);
      }
      pair.forEach((m, k) => {
        m.count = counts[k];
        m.instanceMatrix.needsUpdate = true;
      });
    }
  }
  // Low encrusted bases fasten the separate branches to their substrate.
  const stone = await tex.loadAsync(stoneSkin);
  stone.colorSpace = T.SRGBColorSpace;
  stone.wrapS = stone.wrapT = T.MirroredRepeatWrapping;
  const baseG = new T.IcosahedronGeometry(1, 2);
  const pts = baseG.attributes.position;
  for (let i = 0; i < pts.count; i++) {
    const x = pts.getX(i),
      y = pts.getY(i),
      z = pts.getZ(i);
    const noise = 1 + 0.1 * Math.sin(x * 8 + z * 3) * Math.cos(z * 6 - y * 2);
    pts.setXYZ(i, x * noise, y * noise, z * noise);
  }
  baseG.computeVertexNormals();
  const baseMat = new T.MeshStandardMaterial({
    map: stone,
    color: 0xada183,
    roughness: 0.97,
  });
  water(baseMat);
  const bases = new T.InstancedMesh(baseG, baseMat, colonies.length);
  colonies.forEach((c, i) => {
    tmp.matrix.copy(c.matrix);
    tmp.matrix.decompose(tmp.position, tmp.quaternion, tmp.scale);
    const k = tmp.scale.x;
    tmp.scale.set(0.68 * k, 0.22 * k, 0.61 * k);
    tmp.position.y -= 0.06 * k;
    tmp.updateMatrix();
    bases.setMatrixAt(i, tmp.matrix);
  });
  bases.receiveShadow = true;
  scene.add(bases);
  const micro = new T.InstancedMesh(
    polyp(),
    movingMaterial(
      new T.MeshStandardMaterial({
        vertexColors: true,
        roughness: 0.91,
        side: T.DoubleSide,
      }),
      clock,
      water,
      true,
    ),
    200,
  );
  const pigment = new T.Color(),
    neutral = color(0xbda97b);
  micro.frustumCulled = false;
  scene.add(micro);
  const lastEye = vec(Infinity, 0, 0);
  function update(t, camera) {
    const eye = camera.position;
    clock.value = t;
    visibleReefs(camera);
    if (eye.distanceToSquared(lastEye) < 0.009) return;
    lastEye.copy(eye);
    const nearby = colonies
      .filter(
        (c) =>
          c.kind < 3 &&
          c.sites.length &&
          c.center.distanceToSquared(eye) < (mobile ? 1.9 : 19),
      )
      .sort(
        (a, b) =>
          a.center.distanceToSquared(eye) - b.center.distanceToSquared(eye),
      )
      .slice(0, 7);
    let i = 0;
    for (const c of nearby) {
      const sites = c.sites;
      const step =
        Math.max(1, Math.floor(sites.length / 25));
      for (
        let j = 0;
        j < sites.length && i < (mobile ? 38 : 200);
        j += step
      ) {
        const site = sites[j];
        tmp.position.copy(site).applyMatrix4(c.matrix);
        const n =
          c.kind === 2
            ? vec(
                site.x / 0.77 ** 2,
                (site.y - 0.15) / 0.59 ** 2,
                site.z / 0.62 ** 2,
              )
                .normalize()
                .transformDirection(c.matrix)
            : vec(0, 1, 0);
        tmp.position.addScaledVector(n, -0.00028);
        tmp.quaternion.setFromUnitVectors(vec(0, 1, 0), n);
        tmp.rotateY(site.z * 23 + site.x * 11);
        const variation = Math.sin(site.x * 73 + site.z * 38);
        const width = (c.kind === 2 ? 0.018 : 0.012) * (1 + 0.36 * variation);
        tmp.scale.set(
          width,
          width * (0.73 + 0.51 * Math.sin(site.z * 61 + site.x * 45)),
          width,
        );
        tmp.updateMatrix();
        micro.setColorAt(
          i,
          site.pigment
            ? pigment.copy(site.pigment).lerp(neutral, 0.48)
            : neutral,
        );
        micro.setMatrixAt(i++, tmp.matrix);
      }
    }
    if (micro.instanceColor) micro.instanceColor.needsUpdate = true;
    micro.count = i;
    micro.instanceMatrix.needsUpdate = true;
  }
  return { update, homes, colliders };
}
