import * as T from "three";

export const species = [
  {
    name: "日本竹荚鱼",
    latin: "Trachurus japonicus",
    zone: "中层鱼群",
    length: 0.44,
    note: "看一尾，再看它旁边的同伴。它们结队巡游，头部很稳，摆幅从身体后半部传到深叉形的尾鳍。",
  },
  {
    name: "赤点石斑鱼",
    latin: "Epinephelus akaara",
    zone: "砂岩礁脚",
    length: 0.54,
    note: "在礁脚多看几秒。石斑鱼会守在掩体旁，偶尔摆动圆尾，短距离换一个位置。",
  },
  {
    name: "新月锦鱼",
    latin: "Thalassoma lunare",
    zone: "礁缘浅水",
    length: 0.31,
    note: "留意鳃盖后方的一对胸鳍。锦鱼沿礁缘用胸鳍划水，转弯时再借尾部调整方向。",
  },
  {
    name: "牙鲆",
    latin: "Paralichthys olivaceus",
    zone: "沙坡底层",
    length: 0.57,
    note: "从斜上方看，它的两只眼都朝向你。离底时尾部上下摆动，长鳍边缘起伏，滑行后再伏回沙面。",
  },
  {
    name: "金色拟花鮨",
    latin: "Pseudanthias squamipinnis",
    zone: "珊瑚上方",
    length: 0.18,
    note: "看珊瑚上方那群金橙色的小鱼。有的短促前冲，有的放慢，仍在同一片礁头附近觅食。",
  },
  {
    name: "丝蝴蝶鱼",
    latin: "Chaetodon auriga",
    zone: "礁坪边缘",
    length: 0.24,
    note: "留意同行的另一尾。它们沿礁坪缓游，减速时低头探看，胸鳍轻轻划动，背鳍后方拖着一根细长的丝。",
  },
];

export function floorHeight(x, z) {
  return (
    0.25 * Math.sin(x * 0.6 + Math.sin(z * 0.29)) +
    0.18 * Math.cos(z * 0.48) +
    0.055 * Math.sin(z * 4 + x * 1.15 + 1.5 * Math.sin(x * 0.43))
  );
}

// Distances are metres. Each species has its own home range and cruising pace.
const gaits = [
  { rx: 1.45, rz: 0.86, rise: 0.13, speed: 0.47, beat: 2.0, bend: 0.105 },
  { rx: 0.56, rz: 0.38, rise: 0.08, speed: 0.12, beat: 0.85, bend: 0.085 },
  { rx: 0.96, rz: 0.62, rise: 0.22, speed: 0.34, beat: 1.75, bend: 0.036 },
  { rx: 0.46, rz: 0.31, rise: 0, speed: 0.36, beat: 1.35, bend: 0.045 },
  { rx: 0.68, rz: 0.43, rise: 0.12, speed: 0.19, beat: 2.25, bend: 0.103 },
  { rx: 0.56, rz: 0.4, rise: 0.1, speed: 0.15, beat: 1.45, bend: 0.057 },
];
function home(index, n) {
  const group = Math.floor(n / 9);
  return [
    [0.45 - 0.34 * n, 2.55 + 0.09 * Math.sin(n * 3.1), 0.85 - 0.3 * n],
    [-4.1 - (n % 2) * 0.3, 1.08, 0.5 - n * 7],
    [2.75, 1.65, -0.5 - n * 3.4],
    [0.85 + n * 1.3, 0, 1.2 - n * 7],
    [
      -1.05 + Math.sin(n * 2.4) * 0.92,
      2.2 + group * 0.28 + 0.35 * Math.cos(n * 4.3),
      4.4 - group * 6.6 + Math.cos(n * 3.7) * 0.72,
    ],
    [1.15 + (n % 2) * 0.35, 1.32 + (n % 2) * 0.16, 3.3 - Math.floor(n / 2) * 8],
  ][index];
}

function animateFlesh(material, index, phase, effort, turn) {
  const original = material.onBeforeCompile;
  const g = gaits[index];
  material.onBeforeCompile = (shader) => {
    original(shader);
    Object.assign(shader.uniforms, {
      swimPhase: phase,
      swimEffort: effort,
      swimTurn: turn,
    });
    shader.vertexShader =
      `
      uniform float swimPhase, swimEffort, swimTurn;
      ${index === 5 || index === 2 ? "attribute float _pectoral;" : ""}
      const float fishLength = ${species[index].length.toFixed(3)};
      float bend(float x) {
        float tail = clamp(.50-x/fishLength,0.,1.);
        return fishLength * pow(tail,2.25) * (${g.bend} * swimEffort * sin(swimPhase - 4.3*tail) + .14*swimTurn);
      }
    ` + shader.vertexShader;
    shader.vertexShader = shader.vertexShader.replace(
      "#include <beginnormal_vertex>",
      `#include <beginnormal_vertex>
       objectNormal.x -= objectNormal.${index === 3 ? "y" : "z"} * (bend(position.x+.0005)-bend(position.x-.0005))/.001;`,
    );
    shader.vertexShader = shader.vertexShader.replace(
      "#include <begin_vertex>",
      `#include <begin_vertex>
       transformed.${index === 3 ? "y" : "z"} += bend(position.x);
       ${index === 5 || index === 2 ? "transformed.z += .009*_pectoral*sin(swimPhase*1.4); transformed.y += .003*abs(_pectoral)*cos(swimPhase*1.4);" : ""}
       ${index === 3 ? "transformed.y += .006 * pow(clamp(abs(position.z)/.105,0.,1.),2.) * swimEffort * sin(swimPhase*1.6+position.x*27.);" : ""}`,
    );
  };
  material.customProgramCacheKey = () => "reef-swim-" + index;
}

export function reefLife(sources, scene, reef, light) {
  const actors = [],
    featured = [];
  sources.forEach((source, index) => {
    const g = gaits[index];
    const points = Array.from({ length: 12 }, (_, k) => {
      const a = (k * Math.PI) / 6;
      return new T.Vector3(
        g.rx * Math.sin(a),
        g.rise * Math.sin(2 * a + 0.3),
        g.rz * Math.cos(a) * (1 + 0.17 * Math.sin(a)),
      );
    });
    const path = new T.CatmullRomCurve3(points, true);
    for (let n = 0; n < [4, 4, 7, 3, 27, 6][index]; n++) {
      const fish = source.scene.clone(true);
      const phase = { value: n * 2.4 },
        effort = { value: 1 },
        turn = { value: 0 };
      fish.rotation.order = "YXZ";
      fish.traverse((m) => {
        if (!m.isMesh) return;
        m.material = m.material.clone();
        m.castShadow = m.receiveShadow = true;
        light(m.material);
        animateFlesh(m.material, index, phase, effort, turn);
      });
      actors.push({
        fish,
        index,
        n,
        path,
        loop: path.getLength(),
        origin: new T.Vector3(...home(index, n)),
        distance:
          ((index === 5
            ? Math.floor(n / 2) * 0.23
            : index === 0
              ? 0.013 * n
              : n * 0.163) %
            1) *
          path.getLength(),
        phase,
        effort,
        turn,
      });
      scene.add(fish);
      if (!n) featured[index] = fish;
    }
  });
  const tangent = new T.Vector3(),
    ahead = new T.Vector3();
  let previousSpecimen = null,
    flatfishStart = -Infinity;
  function update(t, dt, specimen) {
    if (specimen === 3 && previousSpecimen !== 3) flatfishStart = t;
    previousSpecimen = specimen;
    for (const actor of actors) {
      const { fish, index, n, path, loop, origin, phase, effort, turn } = actor;
      fish.visible = index !== 0 || specimen !== null;
      const g = gaits[index];
      const cycle =
        0.5 +
        0.5 *
          Math.sin(
            t * (0.58 + index * 0.045) +
              (index === 0
                ? 0.12 * n
                : index === 5
                  ? Math.floor(n / 2) * 2.39
                  : n * 2.39),
          );
      // Flatfish settle between scoots. Groupers hold position longer than midwater fish.
      const lift =
        index === 3 && n === 0 && specimen === 3
          ? Math.max(0, 1 - T.MathUtils.smoothstep(t - flatfishStart, 2.0, 3.8))
          : 0;
      const drive =
        index === 3
          ? Math.max(lift, T.MathUtils.smoothstep(cycle, 0.29, 0.85))
          : index === 1
            ? 0.035 + 1.85 * cycle ** 6
            : index === 4
              ? 0.46 + 1.16 * cycle ** 4
              : index === 5
                ? 0.17 + 0.83 * T.MathUtils.smoothstep(cycle, 0.2, 0.86)
                : 0.69 + 0.31 * cycle;
      const speed = g.speed * drive;
      actor.distance = (actor.distance + speed * dt) % loop;
      const u = actor.distance / loop;
      path.getPointAt(u, fish.position).add(origin);
      path.getTangentAt(u, tangent);
      path.getTangentAt((u + 0.037) % 1, ahead);
      const yaw = Math.atan2(-tangent.z, tangent.x);
      const next = Math.atan2(-ahead.z, ahead.x);
      turn.value = T.MathUtils.clamp(Math.sin(next - yaw) * drive, -0.65, 0.65);
      effort.value = 0.18 + 0.82 * drive;
      phase.value += dt * 2 * Math.PI * g.beat * (0.26 + 0.74 * drive);
      fish.rotation.set(
        index === 3 ? 0 : -0.29 * turn.value,
        yaw,
        Math.asin(tangent.y) * drive,
        "YXZ",
      );
      if (index === 5) fish.rotation.z -= 0.38 * (1 - drive);
      if (index === 3) {
        fish.position.y =
          floorHeight(fish.position.x, fish.position.z) + 0.023 + 0.115 * drive;
        const { x, z } = fish.position,
          dx = tangent.x * 0.08,
          dz = tangent.z * 0.08;
        fish.rotation.z = Math.atan2(
          floorHeight(x + dx, z + dz) - floorHeight(x - dx, z - dz),
          0.16,
        );
        fish.rotation.x = -Math.atan2(
          floorHeight(x - dz, z + dx) - floorHeight(x + dz, z - dx),
          0.16,
        );
      }
      reef.separate(
        fish.position,
        index === 3 ? 0.032 : species[index].length * 0.32,
      );
    }
  }
  return { update, featured };
}
