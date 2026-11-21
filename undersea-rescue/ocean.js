import { rescueSub, berth } from "./echo-sub.js";
import subFile from "./assets/echo-sub.glb";
import { swimCompanion } from "./swim-companion.js";
import { whaleClearance } from "./whale-clearance.js";
import { whaleMotion } from "./whale-swim.js";
import blueTangFile from "./assets/blue-tang.glb";
import kelpFile from "./assets/kelp-cluster.glb";
import { plantKelp } from "./kelp.js";
import { mobileDetail } from "./mobile-detail.js";
import * as T from "three";
import { createDiver } from "./dive-controls.js";
import { coralGarden } from "./coral-garden.js";
import { reefAnimals } from "./reef-animals.js";
import { legacyReefs } from "./legacy-reefs.js";
import legacyGarden from "./assets/reef-garden.glb";
import legacyShelf from "./assets/reef-shelf.glb";
import fanFile from "./assets/sea-fan.glb";
import fanFarFile from "./assets/sea-fan-distant.glb";
import anthiasFile from "./assets/anthias.glb";
import butterflyFile from "./assets/threadfin.glb";
import turtleFile from "./assets/turtle.glb";
import clownFile from "./assets/clownfish.glb";
import coralFile from "./assets/coral-colony.glb";
import lionFile from "./assets/lionfish.glb";
import surfaceFile from "./assets/surface.jpg";
import { obstacles } from "./collision.js";
import { makeShoal } from "./shoal.js";
import { reefLife, atlas, floorHeight } from "./reef-life.js";
import grouperFile from "./assets/spotted-grouper.glb";
import wrasseFile from "./assets/moon-wrasse.glb";
import flounderFile from "./assets/olive-flounder.glb";
import fishFile from "./assets/horse-mackerel.glb";
import { GLTFLoader } from "three/addons/loaders/GLTFLoader.js";
import { mergeGeometries } from "three/addons/utils/BufferGeometryUtils.js";
import { clone } from "three/addons/utils/SkeletonUtils.js";
import terrainFile from "./assets/tidal-canyon.glb";
import whaleFile from "./assets/whale.glb";
import mantaFile from "./assets/manta.glb";

const loader = new GLTFLoader();
const waveTime = { value: 0 },
  flow = { value: 0 };
const surfaceNoise = `
float sw(vec2 p,float t){
 return sin(p.x*1.7+p.y*.65+t*.9+.75*sin(p.y*1.32-t*.5))*.5
      + sin(p.y*2.3-p.x*.42-t*.64+sin(p.x*1.2+t*.42))*.32
      + sin(p.x*4.1+p.y*2.4+t*.69)*.18;
}`;

function seaMaterial(mat, kelp = false, rock = false) {
  mat.onBeforeCompile = (shader) => {
    shader.uniforms.tideTime = waveTime;
    shader.uniforms.flow = flow;
    shader.vertexShader =
      "varying vec3 waterPos; uniform float tideTime; uniform float flow;\n" +
      shader.vertexShader;
    shader.vertexShader = shader.vertexShader.replace(
      "#include <begin_vertex>",
      `#include <begin_vertex>
  ${kelp ? "transformed.x += uv.y*uv.y*(.38*sin(tideTime*1.4+position.z*.8)+flow*.65);" : ""}
  waterPos=(modelMatrix*vec4(transformed,1.)).xyz;`,
    );
    shader.vertexShader = shader.vertexShader.replace(
      "#include <project_vertex>",
      "#include <project_vertex>\n waterPos=transpose(mat3(viewMatrix))*mvPosition.xyz+cameraPosition;",
    );
    shader.fragmentShader =
      "varying vec3 waterPos; uniform float tideTime;\n" +
      surfaceNoise +
      "\n" +
      shader.fragmentShader;
    shader.fragmentShader = shader.fragmentShader.replace(
      "#include <color_fragment>",
      `#include <color_fragment>
    vec2 p=(waterPos.xz+waterPos.y*vec2(-.48,.44))*.89;
    float wave=sw(p,tideTime*.48);
    float ridge=1.-smoothstep(.015,.23+fwidth(wave),abs(wave));
    float litPatch=.50+.50*sin(p.x*.53+sin(p.y*.62));
    diffuseColor.rgb *= .92+ridge*(.17+litPatch*.27);
    diffuseColor.rgb += vec3(.035,.056,.043)*ridge*litPatch;
    diffuseColor.rgb *= exp(-max(0.,9.-waterPos.y)*vec3(.016,.007,.005));
    ${rock ? "diffuseColor.rgb *= .94+.035*sin(waterPos.y*19.+1.7*sin(waterPos.x*1.8+waterPos.z*.7)) + .045*sw(p*5.,0.);" : ""}`,
    );
  };
  mat.customProgramCacheKey = () => `sea-${kelp}-${rock}`;
}

function salvageRig() {
  const halfWidth = 2.2, halfDepth = 2.05;
  // The lowest rail is set into the uneven sand. The underside stays open for release.
  const bottom = -0.82, top = 1.55;
  const pieces = [];
  function bar(from, to, radius) {
    const a = new T.Vector3(...from), b = new T.Vector3(...to);
    const axis = b.clone().sub(a);
    const geometry = new T.CylinderGeometry(radius, radius, axis.length(), 8);
    geometry.applyQuaternion(new T.Quaternion().setFromUnitVectors(new T.Vector3(0, 1, 0), axis.normalize()));
    geometry.translate(...a.lerp(b, 0.5).toArray());
    pieces.push(geometry);
  }
  for (const x of [-halfWidth, halfWidth]) {
    for (const z of [-halfDepth, halfDepth]) bar([x,bottom,z], [x,top,z], 0.065);
    for (const y of [bottom, 0.25, top]) bar([x,y,-halfDepth], [x,y,halfDepth], 0.048);
    for (let i = 1; i < 9; i++) {
      const z = halfDepth * (2 * i / 9 - 1);
      bar([x,bottom,z], [x,top,z], 0.03);
    }
  }
  for (const z of [-halfDepth, halfDepth]) {
    for (const y of [bottom, 0.25, top]) bar([-halfWidth,y,z], [halfWidth,y,z], 0.048);
    for (let i = 1; i < 10; i++) {
      const x = halfWidth * (i / 5 - 1);
      bar([x,bottom,z], [x,top,z], 0.03);
    }
  }
  for (let i = 1; i < 10; i++) {
    const x = halfWidth * (i / 5 - 1);
    bar([x,top,-halfDepth], [x,top,halfDepth], 0.025);
  }
  const cage = new T.Mesh(mergeGeometries(pieces), new T.MeshStandardMaterial({
    color: 0x634b3d, metalness: 0.24, roughness: 0.87
  }));
  cage.castShadow = cage.receiveShadow = true;
  return cage;
}

export async function makeOcean(host, onCompanion) {
  const renderer = new T.WebGLRenderer({
    antialias: true,
    powerPreference: "high-performance",
  });
  const touchScreen = matchMedia(
    "(any-pointer: coarse), (max-width: 650px)",
  ).matches;
  renderer.setPixelRatio(Math.min(devicePixelRatio, touchScreen ? 1.2 : 1.5));
  renderer.toneMapping = T.ACESFilmicToneMapping;
  renderer.toneMappingExposure = 1.04;
  renderer.shadowMap.enabled = true;
  if (touchScreen) renderer.shadowMap.autoUpdate = false;
  let lastShadow = -Infinity;
  renderer.shadowMap.type = T.PCFSoftShadowMap;
  const meter = document.getElementById("render-stats");
  const showStats = new URLSearchParams(location.search).get("stats") === "1";
  let frameTimes = [],
    lastFrame = 0,
    lastReport = 0;
  function recordFrame() {
    if (!showStats) return;
    const now = performance.now();
    if (lastFrame) frameTimes.push(now - lastFrame);
    lastFrame = now;
    if (now - lastReport < 4000 || frameTimes.length < 2) return;
    const span = frameTimes.reduce((sum, ms) => sum + ms, 0);
    const fps = ((1000 * frameTimes.length) / span).toFixed(1);
    frameTimes.sort((a, b) => a - b);
    const p95 = frameTimes[Math.ceil(frameTimes.length * 0.95) - 1].toFixed(1);
    const { calls, triangles } = renderer.info.render;
    meter.hidden = false;
    meter.textContent =
      fps + " FPS · P95 " + p95 + " ms · " + calls + " 次绘制";
    Object.assign(meter.dataset, {
      fps,
      p95,
      calls,
      triangles,
      frames: frameTimes.length,
      ms: Math.round(span),
    });
    lastReport = now;
    frameTimes = [];
  }
  host.replaceChildren(renderer.domElement);
  const scene = new T.Scene();
  // Leave the near reef clear and blend the far canyon into open water.
  scene.background = new T.Color(0x087faf);
  scene.fog = new T.Fog(scene.background, 8, 56);
  const diveLamp = new T.PointLight(0xffead0, 31, 9, 2);
  scene.add(diveLamp);
  const camera = new T.PerspectiveCamera(54, 1, 0.08, 105);
  const envW = 128,
    envH = 64,
    sky = new Float32Array(envW * envH * 4);
  for (let y = 0; y < envH; y++)
    for (let x = 0; x < envW; x++) {
      const h = Math.max(0, Math.sin((y / envH - 0.5) * Math.PI)),
        spark = Math.exp(-((x - 83) ** 2 / 45 + (y - 49) ** 2 / 8));
      sky.set(
        [
          0.033 + 0.24 * h + 3 * spark,
          0.09 + 0.55 * h + 2.8 * spark,
          0.11 + 0.67 * h + 2.1 * spark,
          1,
        ],
        4 * (y * envW + x),
      );
    }
  const env = new T.DataTexture(sky, envW, envH, T.RGBAFormat, T.FloatType);
  env.mapping = T.EquirectangularReflectionMapping;
  env.needsUpdate = true;
  const pmrem = new T.PMREMGenerator(renderer);
  scene.environment = pmrem.fromEquirectangular(env).texture;
  scene.environmentIntensity = 0.45;
  pmrem.dispose();
  env.dispose();
  scene.add(new T.HemisphereLight(0xd9f4f8, 0x78918c, 1.62));
  const sun = new T.DirectionalLight(0xffefdc, 2.7);
  sun.position.set(-11, 23, 1);
  sun.target.position.set(1, 0, -12);
  scene.add(sun, sun.target);
  sun.castShadow = true;
  sun.shadow.mapSize.setScalar(touchScreen ? 1024 : 2048);
  Object.assign(sun.shadow.camera, {
    left: -19,
    right: 19,
    top: 23,
    bottom: -23,
    near: 0.5,
    far: 65,
  });
  sun.shadow.camera.updateProjectionMatrix();
  sun.shadow.normalBias = 0.028;
  sun.shadow.bias = -0.00015;
  const [
    canyon,
    whale,
    manta,
    mackerel,
    grouper,
    wrasse,
    flounder,
    turtleSource,
    clownSource,
    coralSource,
    lionSource,
    bankSource,
    shelfSource,
    fanSource,
    fanFarSource,
    anthiasSource,
    butterflySource,
    tangSource,
    kelpSource,
    subSource,
  ] = await Promise.all(
    [
      terrainFile,
      whaleFile,
      mantaFile,
      fishFile,
      grouperFile,
      wrasseFile,
      flounderFile,
      turtleFile,
      clownFile,
      coralFile,
      lionFile,
      legacyGarden,
      legacyShelf,
      fanFile,
      fanFarFile,
      anthiasFile,
      butterflyFile,
      blueTangFile,
      kelpFile,
      subFile,
    ].map((url) => loader.loadAsync(url)),
  );
  const mobileMeshes = touchScreen
    ? await mobileDetail({
        "tidal-canyon": canyon,
        whale,
        manta,
        "horse-mackerel": mackerel,
        turtle: turtleSource,
        clownfish: clownSource,
        lionfish: lionSource,
        "reef-garden": bankSource,
        "reef-shelf": shelfSource,
        "coral-colony": coralSource,
        "sea-fan": fanSource,
        "sea-fan-distant": fanFarSource,
      })
    : new WeakMap();
  for (const source of [
    whale,
    manta,
    turtleSource,
    clownSource,
    lionSource,
    fanSource,
    fanFarSource,
  ]) {
    source.scene.traverse((m) => {
      if (m.isMesh && mobileMeshes.has(m.geometry))
        m.geometry = mobileMeshes.get(m.geometry);
    });
  }
  canyon.scene.traverse((m) => {
    if (!m.isMesh) return;
    const name = m.material.name;
    m.receiveShadow = true;
    m.castShadow = name !== "Rippled sand";
    if (m.material.map)
      m.material.map.anisotropy = Math.min(
        8,
        renderer.capabilities.getMaxAnisotropy(),
      );
    seaMaterial(
      m.material,
      name === "Kelp ribbons",
      name === "Eroded sandstone",
    );
  });
  scene.add(canyon.scene);
  const surfaceMap = await new T.TextureLoader().loadAsync(surfaceFile);
  surfaceMap.colorSpace = T.SRGBColorSpace;
  surfaceMap.wrapS = surfaceMap.wrapT = T.MirroredRepeatWrapping;
  const water = new T.Mesh(
    new T.PlaneGeometry(140, 140, 120, 120),
    new T.ShaderMaterial({
      side: T.DoubleSide,
      uniforms: { time: waveTime, surfaceMap: { value: surfaceMap }, openWater: { value: scene.fog.color } },
      vertexShader: `varying vec3 p;uniform float time;void main(){p=(modelMatrix*vec4(position,1.)).xyz;p.y+=.12*sin(p.x*.7+time)+.1*cos(p.z*.8-time*.6);gl_Position=projectionMatrix*viewMatrix*vec4(p,1.);}`,
      fragmentShader:
        `varying vec3 p;uniform float time;uniform sampler2D surfaceMap;uniform vec3 openWater;` +
        surfaceNoise +
        `void main(){vec2 q=p.xz*.64;float w=sw(q,time)+.23*sw(q*2.7,time*.84)+.11*sw(q*6.4,time*.91);
 vec3 n=normalize(vec3((sw(q+vec2(.08,0.),time)-sw(q,time))*2.4,1.,(sw(q+vec2(0.,.08),time)-sw(q,time))*2.4));vec3 eye=normalize(p-cameraPosition);
 float sky=smoothstep(.44,.82,eye.y+.28*w);float glint=pow(max(0.,dot(normalize(n+vec3(-.34,.1,-.25)),normalize(vec3(-.45,.78,-.42)-eye*.25))),18.);
 float sun=exp(-.065*length(p.xz-vec2(-6.,-12.)));vec3 c=mix(vec3(.009,.17,.32),vec3(.40,.75,.78),sky)+vec3(.02,.27,.41)*(.52+.44*w)+vec3(.91,.88,.59)*glint*sun;vec2 rippleUV=p.xz*.040+vec2(w*.012,sw(q*.54,time*.6)*.016);vec3 refraction=texture2D(surfaceMap,rippleUV).rgb;c=mix(c,refraction*vec3(.46,.88,1.12)*(.77+.34*sky)+vec3(.0,.045,.12),.64);float dist=distance(cameraPosition,p);c=mix(c,openWater,smoothstep(10.,56.,dist));gl_FragColor=vec4(c,1.);
 #include <colorspace_fragment>
 }`,
    }),
  );
  water.rotation.x = -Math.PI / 2;
  water.position.set(0, 10, -15);
  scene.add(water);
  const beams = new T.BufferGeometry(),
    bp = [],
    bu = [];
  for (let i = 0; i < 18; i++) {
    const x = -16 + ((i * 7) % 31),
      z = 9 - ((i * 11) % 57),
      width = 0.5 + (i % 4) * 0.43;
    for (const [u, v] of [
      [0, 0],
      [1, 0],
      [1, 1],
      [0, 0],
      [1, 1],
      [0, 1],
    ]) {
      bp.push(x + u * width * (1 + v) + v * 4.7, 10 - v * 10, z - v * 2.5);
      bu.push(u, v);
    }
  }
  beams.setAttribute("position", new T.Float32BufferAttribute(bp, 3));
  beams.setAttribute("uv", new T.Float32BufferAttribute(bu, 2));
  scene.add(
    new T.Mesh(
      beams,
      new T.ShaderMaterial({
        uniforms: { time: waveTime },
        transparent: true,
        depthWrite: false,
        side: T.DoubleSide,
        blending: T.AdditiveBlending,
        vertexShader:
          "varying vec2 v;void main(){v=uv;gl_Position=projectionMatrix*modelViewMatrix*vec4(position,1.);}",
        fragmentShader:
          "varying vec2 v;uniform float time;void main(){float a=pow(sin(3.14159*v.x),2.)*pow(1.-v.y,.9)*(.74+.26*sin(v.x*9.+time*.52));gl_FragColor=vec4(.68,.88,.76,.087*a);}",
      }),
    ),
  );
  const dustPos = new Float32Array(1300 * 3);
  for (let i = 0; i < dustPos.length; i += 3) {
    dustPos[i] = Math.random() * 38 - 19;
    dustPos[i + 1] = Math.random() * 10;
    dustPos[i + 2] = Math.random() * 67 - 52;
  }
  const dustGeo = new T.BufferGeometry();
  dustGeo.setAttribute("position", new T.BufferAttribute(dustPos, 3));
  const dust = new T.Points(
    dustGeo,
    new T.PointsMaterial({
      color: 0xb1d3c6,
      transparent: true,
      opacity: 0.38,
      size: 0.023,
      depthWrite: false,
    }),
  );
  scene.add(dust);
  const solid = [];
  canyon.scene.traverse((m) => {
    if (!m.isMesh || m.material.name === "Kelp ribbons") return;
    if (m.material.name === "Rippled sand" && mobileMeshes.has(m.geometry))
      m.geometry = mobileMeshes.get(m.geometry);
    solid.push(m);
  });
  const reef = legacyReefs(
      scene,
      obstacles(solid),
      { garden: bankSource, shelf: shelfSource, cluster: coralSource },
      seaMaterial,
      mobileMeshes,
    ),
    school = makeShoal(
      { mackerel, anthias: anthiasSource, tang: tangSource },
      reef,
      mobileMeshes,
      seaMaterial,
    );
  scene.add(...school.batches);
  plantKelp(scene, reef, kelpSource, seaMaterial);
  canyon.scene.traverse((m) => {
    if (m.isMesh && mobileMeshes.has(m.geometry))
      m.geometry = mobileMeshes.get(m.geometry);
  });
  const residents = reefLife(
    [mackerel, grouper, wrasse, flounder, anthiasSource, butterflySource],
    scene,
    reef,
    seaMaterial,
  );
  const eels = new T.InstancedMesh(
    mergeGeometries([
      new T.TubeGeometry(
        new T.CatmullRomCurve3([
          new T.Vector3(0, 0, 0),
          new T.Vector3(0.04, 0.4, 0),
          new T.Vector3(0.16, 0.64, 0),
          new T.Vector3(0.22, 0.65, 0.02),
        ]),
        10,
        0.026,
        6,
        false,
      ),
      new T.SphereGeometry(1, 12, 10)
        .scale(0.04, 0.027, 0.027)
        .translate(0.222, 0.65, 0.02),
    ]),
    new T.MeshStandardMaterial({ color: 0xb7975d }),
    32,
  );
  eels.frustumCulled = false;
  scene.add(eels);
  eels.material.onBeforeCompile = (shader) => {
    shader.vertexShader =
      "varying vec3 bodyP;\n" +
      shader.vertexShader.replace(
        "#include <begin_vertex>",
        "#include <begin_vertex>\n bodyP=position;",
      );
    shader.fragmentShader =
      "varying vec3 bodyP;\n" +
      shader.fragmentShader.replace(
        "#include <color_fragment>",
        "#include <color_fragment>\n float spots=sin(bodyP.y*97.+bodyP.x*107.)*cos(bodyP.z*114.-bodyP.y*45.);diffuseColor.rgb*=mix(.22,1.,smoothstep(.55,.77,1.-spots));float eye=length((bodyP.xy-vec2(.233,.657))*vec2(1.,1.1));diffuseColor.rgb*=mix(.02,1.,smoothstep(.004,.007,eye));",
      );
  };

  function actor(source, size) {
    const model = clone(source.scene);
    const box = new T.Box3().setFromObject(model),
      dim = box.getSize(new T.Vector3()),
      center = box.getCenter(new T.Vector3());
    model.traverse((m) => {
      if (m.isMesh) {
        m.castShadow = true;
        m.receiveShadow = true;
        seaMaterial(m.material);
      }
    });
    model.position.sub(center);
    const pivot = new T.Group();
    pivot.add(model);
    pivot.scale.setScalar(size / Math.max(dim.x, dim.y, dim.z));
    const group = new T.Group();
    group.add(pivot);
    scene.add(group);
    return group;
  }
  const calf = actor(whale, 5.7),
    mother = actor(whale, 10),
    ray = actor(manta, 3.45),
    rig = salvageRig();
  scene.add(rig);
  const whalePair = whaleClearance(calf, mother);
  // The dark dorsal surface faces local -X. Level the disc, then roll about its nose axis.
  ray.children[0].rotation.set(Math.PI, 0, 1.25);
  ray.traverse((m) => {
    if (!m.isMesh) return;
    const baseLight = m.material.onBeforeCompile;
    m.material.onBeforeCompile = (shader) => {
      baseLight(shader);
      shader.vertexShader = shader.vertexShader.replace(
        "#include <begin_vertex>",
        "#include <begin_vertex>\n transformed.x+=pow(abs(position.z)*2.,2.)*.065*sin(tideTime*2.45-abs(position.z)*4.0);",
      );
    };
  });
  const garden = await coralGarden(
    scene,
    reef.ground,
    seaMaterial,
    fanSource,
    fanFarSource,
    touchScreen,
  );
  const fauna = reefAnimals(
    scene,
    actor,
    {
      turtle: turtleSource,
      clown: clownSource,
      lion: lionSource,
    },
    garden,
    reef,
  );
  const eel = new T.Object3D();
  const atlasAnimals = {
    jelly: fauna.jelly, mother, calf, turtle: fauna.turtle,
    clown: fauna.clown, lion: fauna.lion, ray, tang: school.featuredTang, eel,
  };
  const diver = createDiver();
  let diveActive = false;

  const whales = [calf, mother].map((animal, i) => {
    const path = new T.CatmullRomCurve3(
      [
        [1, -18],
        [4.4, -23],
        [4, -33],
        [1.7, -40],
        [-2.1, -39],
        [-3.7, -31],
        [-3.6, -23],
        [-1.8, -18.5],
      ].map(
        ([x, z], k) =>
          new T.Vector3(
            x + (i ? 0 : 1.2),
            (i ? 5.5 : 4.25) + 0.55 * Math.sin((k * Math.PI) / 4),
            z,
          ),
      ),
      true,
    );
    // Patrol in a smaller home range during rescue. Arc length keeps the
    // whale advancing at the same pace through the ends of the oval.
    const patrol = new T.CatmullRomCurve3(
      Array.from({ length: 12 }, (_, k) => {
        const a = (k / 12) * Math.PI * 2;
        return new T.Vector3(
          (i ? 3.7 : 2.1) * Math.sin(a),
          (i ? 4.95 : 4.05) + .28 * Math.sin(a + .2),
          (i ? -32 : -5.9) + (i ? 8 : 3.5) * Math.cos(a),
        );
      }), true,
    );
    return {
      animal,
      patrol,
      patrolLength: patrol.getLength(),
      path,
      length: path.getLength(),
      motion: whaleMotion(animal, i, seaMaterial),
    };
  });
  const direction = new T.Vector3(),
    future = new T.Vector3();
  function cruise(w, path, fraction) {
    const u = fraction % 1;
    path.getPointAt(u, w.animal.position);
    path.getTangentAt(u, direction);
    path.getTangentAt((u + .032) % 1, future);
    const yaw = Math.atan2(direction.x, direction.z);
    const bank = -.18 * Math.sin(Math.atan2(future.x, future.z) - yaw);
    w.animal.rotation.set(-Math.asin(direction.y), yaw, bank, "YXZ");
  }
  function motherFlank(target) {
    const h = mother.rotation.y;
    target.copy(mother.position);
    target.x += shot.flank * 5.35 * Math.cos(h) - 3.35 * Math.sin(h);
    target.z -= shot.flank * 5.35 * Math.sin(h) + 3.35 * Math.cos(h);
    target.y -= .75;
  }
  const gates = {
    song: new T.Vector3(-2.4, 3.4, 2.2),
    ridge: new T.Vector3(1.7, 3.9, -40.5),
    lee: new T.Vector3(3.6, 4.6, -17.4),
    gate1: new T.Vector3(0.8, 3.1, -12),
    gate2: new T.Vector3(-0.7, 3.3, -22),
  };
  const cageTarget = new T.Vector3(0.4, reef.ground(.4, -45.8) + 1.45, -45.8);
  const ripple = new T.Mesh(
    new T.RingGeometry(.95, 1, 64),
    new T.MeshBasicMaterial({color:0xc3f5ef, transparent:true, opacity:0, depthWrite:false, side:T.DoubleSide})
  );
  scene.add(ripple);
  const sub = rescueSub(subSource.scene, scene);
  Object.assign(gates, {
    hydro: new T.Vector3(-.2, 4.1, -35),
    fork: new T.Vector3(-.2, 4.1, -35),
    shoalPass: new T.Vector3(-2.15, 5.55, -39.8),
    reefPass: new T.Vector3(2.65, 2.05, -39.6),
    armDock: new T.Vector3(cageTarget.x, cageTarget.y+.12, cageTarget.z+3.48),
  });
  const routeCalf = new T.Vector3();
  // Wait beyond the crossing so a diver following the tail can enter the trigger.
  const lookouts = Object.fromEntries(["gate1", "lee", "gate2"].map(leg =>
    [leg, gates[leg].clone().add(new T.Vector3(0, 0.35, -3.8))]));
  lookouts.mother = mother.position;
  const rendezvous = new T.Vector3();
  let loopPhase = 0, guideLeg = null;
  let calfWaiting = false;
  function targetFor(leg) {
    if (leg === "calf") return calf.position;
    if (leg === "mother") return mother.position;
    if (leg === "cage") return cageTarget;
    return gates[leg];
  }
  let observerHeading = 0;
  const tmp = new T.Object3D(),
    look = new T.Vector3(),
    pos = new T.Vector3(),
    aim = new T.Vector3();
  const shot = {
    mode: null,
    weight: 0,
    active: false,
    from: new T.Vector3(),
    fromRotation: new T.Quaternion(),
    last: new T.Vector3(),
    lastRotation: new T.Quaternion(),
    offset: new T.Vector3(),
    anchor: new T.Vector3(),
  };
  const candidate = new T.Vector3(),
    center = new T.Vector3(),
    desiredEye = new T.Vector3();
  function composeCamera(mode, dt) {
    const changed = mode !== shot.mode;
    if (changed) {
      shot.from.copy(shot.last);
      shot.fromRotation.copy(shot.lastRotation);
      shot.weight = shot.active ? 1 : 0;
      shot.mode = mode;
    }
    if (mode === "reunion" || mode === "release") {
      const reunion = mode === "reunion";
      if (reunion) center.copy(mother.position).lerp(calf.position, 0.38);
      else {
        // Frame the raised cage and the whole exit lane from the same camera position.
        center.set(cageTarget.x - .9, cageTarget.y + 1.15, cageTarget.z - 3.6);
      }
      if (changed) {
        // Choose a clear sightline down the canyon once, then keep the same side of the action.
        shot.anchor.copy(center);
        const radius = (reunion ? 14.5 : 12) * Math.max(1, 1 / camera.aspect);
        const h = mother.rotation.y;
        const nearSide =
          Math.sign(
            (calf.position.x - mother.position.x) * Math.cos(h) -
              (calf.position.z - mother.position.z) * Math.sin(h),
          ) || 1;
        const angles = reunion
          ? [
              h + nearSide * 1.35,
              h - nearSide * 1.35,
              h + nearSide * 0.95,
              h - nearSide * 0.95,
              0.22,
              2.94,
            ]
          : [-.55, .55, -.32, .32, -.7, .7, 2.8];
        let best = -Infinity;
        for (const angle of angles) {
          candidate
            .set(
              Math.sin(angle) * radius,
              reunion ? 3.1 : 5.5,
              Math.cos(angle) * radius,
            )
            .add(center);
          if (reunion) {
            // Look down the open channel; its high side walls can hide the calf.
            candidate.x = T.MathUtils.clamp(candidate.x, -4.5, 4.5);
            const dx = candidate.x - center.x;
            const dz = Math.sqrt(Math.max(0, radius * radius - dx * dx));
            candidate.z = center.z + Math.sign(Math.cos(angle)) * dz;
          }
          reef.cameraArm(center, candidate, 0.33);
          const clearance = candidate.distanceTo(center);
          if (clearance > best) {
            shot.offset.subVectors(candidate, center);
            best = clearance;
          }
          if (clearance > 0.94 * radius) break;
        }
        if (reunion)
          shot.flank =
            Math.sign(
              shot.offset.x * Math.cos(h) - shot.offset.z * Math.sin(h),
            ) || nearSide;
      }
      if (reunion) {
        shot.anchor.lerp(center, 1 - Math.exp(-dt * .64));
        desiredEye.copy(shot.anchor).add(shot.offset);
        desiredEye.x = T.MathUtils.clamp(desiredEye.x, -4.5, 4.5);
        desiredEye.y = Math.max(desiredEye.y, 7.8);
      } else desiredEye.copy(center).add(shot.offset);
      reef.cameraArm(center, desiredEye, 0.32);
      camera.position.copy(desiredEye);
      camera.lookAt(reunion ? shot.anchor : center);
    }
    if (shot.weight > 0) {
      shot.weight = Math.max(0, shot.weight - dt / 1.65);
      const w = T.MathUtils.smoothstep(shot.weight, 0, 1);
      camera.position.lerp(shot.from, w);
      camera.quaternion.slerp(shot.fromRotation, w);
    }
    if (diveActive && (mode || shot.weight > 0)) {
      for (const bubble of fauna.jellyBounds) {
        direction.subVectors(camera.position, bubble.center);
        const d = direction.length(),
          safe = bubble.radius + 0.34;
        if (d < safe)
          camera.position
            .copy(bubble.center)
            .addScaledVector(
              d > 0.001 ? direction.divideScalar(d) : direction.set(0, 1, 0),
              safe,
            );
      }
    }
    shot.last.copy(camera.position);
    shot.lastRotation.copy(camera.quaternion);
    shot.active = true;
  }
  const companions = [];
  function addAnimal(node,name,radius,speed,axis="x") {
    companions.push({node,name,radius,speed,axis});
  }
  for (const a of residents.actors) addAnimal(a.fish,atlas[a.index].name,.12,.95);
  school.batches.forEach((b,i)=>addAnimal(b,["金色拟花鮨","蓝倒吊","日本竹荚鱼"][i],.12,1.2));
  for (const j of fauna.jellies) addAnimal(j,"海月水母",.42,.35,"drift");
  for (const f of fauna.clowns) addAnimal(f.node,"小丑鱼",.12,.85);
  for (const f of fauna.lions) addAnimal(f.node,"狮子鱼",.18,.55);
  addAnimal(fauna.turtle,"海龟",.46,1.2,"z");
  addAnimal(calf,"幼鲸 · 泡泡",1.35,2.9,"z");
  addAnimal(mother,"鲸鱼妈妈",2.25,3.1,"z");
  addAnimal(ray,"蝠鲼 · 小翼",.85,1.75,"-x");
  const companion = swimCompanion(host,camera,reef,garden.colliders,companions,onCompanion);
  let trappedRay = false, canSwimTogether = false, onRoute = false, firstTap = null, press = null;
  let viewPointer = null;
  let orbitFree = false,
    az = 0,
    pitch = 0,
    drag = null,
    snap = true,
    clock = 0,
    lastSpecimen = -1;
  host.onpointerdown = (e) => {
    if (viewPointer !== null || e.button !== 0) return;
    press = {x:e.clientX,y:e.clientY,time:e.timeStamp};
    viewPointer = e.pointerId;
    host.setPointerCapture(e.pointerId);
    drag = [e.clientX, e.clientY];
  };
  host.onpointermove = (e) => {
    if (press && e.pointerId === viewPointer && Math.hypot(e.clientX-press.x,e.clientY-press.y)>10) {
      press=null;
      firstTap=null;
    }
    if (companion.active && (e.pointerType === "mouse" || viewPointer === e.pointerId)) {
      companion.point(e.clientX,e.clientY);
      return;
    }
    if (!drag || viewPointer !== e.pointerId) return;
    if (diveActive) {
      diver.turn(e.clientX - drag[0], e.clientY - drag[1]);
      drag = [e.clientX, e.clientY];
      return;
    }
    az -= (e.clientX - drag[0]) * 0.006;
    if (!orbitFree) az = T.MathUtils.clamp(az, -1.35, 1.35);
    pitch = T.MathUtils.clamp(
      pitch + (e.clientY - drag[1]) * 0.004,
      -0.42,
      0.58,
    );
    drag = [e.clientX, e.clientY];
  };
  host.onpointerup =
    host.onpointercancel =
    host.onlostpointercapture =
      (e) => {
        if (viewPointer === e.pointerId) {
          if (e.type === "pointerup" && press && canSwimTogether && e.timeStamp-press.time<380) {
            const close = firstTap && firstTap.kind===e.pointerType && e.timeStamp-firstTap.time<400 &&
              Math.hypot(e.clientX-firstTap.x,e.clientY-firstTap.y)<24;
            if (close) {
              companion.toggle(e.clientX,e.clientY, a => !(trappedRay && a.node === ray) && !(onRoute && (a.node === calf || a.node === mother)));
              firstTap=null;
            } else firstTap={x:e.clientX,y:e.clientY,time:e.timeStamp,kind:e.pointerType};
          } else firstTap=null;
          press=null;
          drag = null;
          viewPointer = null;
        }
      };
  let framing = null;
  function frameSubject(following) {
    if (framing === following) return;
    framing = following;
    const w = host.clientWidth,
      h = host.clientHeight;
    if (following && w > 0 && h > 0 && (touchScreen || w <= 650 || h <= 510)) {
      const landscape = w > h;
      camera.setViewOffset(
        w,
        h,
        landscape ? -w * 0.205 : 0,
        h * (landscape ? 0.045 : 0.12),
        w,
        h,
      );
    } else camera.clearViewOffset();
  }
  function resize() {
    const w = host.clientWidth,
      h = host.clientHeight;
    if (!w || !h) return;
    renderer.setSize(w, h, false);
    framing = null;
    camera.aspect = w / h;
    camera.updateProjectionMatrix();
  }
  new ResizeObserver(resize).observe(host);
  return {
    swimStick(x, y) {
      diver.setStick(x, y);
    },
    swimKey(code, down) {
      diver.press(code, down);
    },
    clearKeys() {
      diver.clear();
    },
    faceWaypoint(leg) {
      if (leg) diver.face(lookouts[leg] ? routeCalf : targetFor(leg));
    },
    enterWater(inPlace) {
      diveActive = true;
      diver.reset(
        inPlace ? camera.position : null,
        inPlace ? camera.getWorldDirection(direction) : null,
      );
    },
    boardSub() { diver.reset(berth, new T.Vector3(1,-.03,.20).normalize()); },
    diverPosition() { return diver.position.toArray(); },
    restoreEscort(leg) {
      routeCalf.copy(diver.position);
      if (lookouts[leg]) {
        const toward = leg === "mother" ? whales[1].patrol.getPointAt(.09) : lookouts[leg];
        direction.subVectors(toward, routeCalf).clampLength(0, 4.8);
        routeCalf.add(direction);
        reef.separate(routeCalf, 1.75);
      } else routeCalf.x -= 2.8;
    },
    restoreDiver(position) {
      diver.reset(new T.Vector3(...position), new T.Vector3(0, 0, -1));
    },
    leaveWater() {
      companion.stop(true);
      firstTap = null;
      diveActive = false;
      diver.clear();
      drag = null;
      snap = true;
    },
    reset() {
      companion.stop(true);
      firstTap = null;
      frameTimes = [];
      lastFrame = 0;
      lastReport = performance.now();
      meter.hidden = true;
      diveActive = false;
      drag = null;
      az = 0;
      pitch = 0;
      snap = true;
      clock = 0;
      calfWaiting = false;
      guideLeg = null;
      lastSpecimen = -1;
      for (const w of whales) w.motion.reset();
      whalePair.reset();
      shot.mode = null;
      shot.weight = 0;
      shot.active = false;
      shot.flank = -1;
      school.reset();
      resize();
    },
    render(s, dt) {
      companion.restore();
      onRoute = s.route;
      trappedRay = (s.route || s.story === "manta") && s.escape < 1;
      canSwimTogether = !s.boat && (s.story === "observe" || s.story === "roam" || (diveActive && (!s.route || s.leg)));
      if (!canSwimTogether) { companion.stop(true); firstTap=null; }
      clock += dt;
      const t = clock,
        wh = s.story === "whale",
        journey = s.travel || 0,
        base = -3 - journey * 21;
      if (touchScreen && (t < lastShadow || t - lastShadow > 0.45)) {
        renderer.shadowMap.needsUpdate = true;
        lastShadow = t;
      }
      for (const w of whales) w.motion.advance(dt);
      waveTime.value = t;
      flow.value = diveActive ? Math.sin(t * 0.78 + 0.53) : s.tide;
      const observatory = s.story === "observe" && !diveActive;
      const subject = observatory ? atlas[s.specimen] : null;
      calf.visible = s.route ? s.leg !== "song" : diveActive || subject?.key === "calf";
      mother.visible = s.route ? (s.leg === "mother" || s.line === "mother" || s.step === "still" || s.line === "knock" || !wh) : diveActive || subject?.key === "mother";
      rig.visible = s.route && s.foundCage;
      ray.visible = s.route ? rig.visible : diveActive || subject?.key === "ray";
      orbitFree = observatory;
      const sceneShot =
        s.line === "mother" || s.step === "still" || s.line === "knock" || (s.step === "done" && wh)
          ? "reunion"
          : !wh &&
              (s.step === "escape" || s.step === "lower" ||
                s.line === "clear" ||
                (s.step === "lift" && s.lift > 0.65) ||
                (s.step === "done" && s.lift >= 1))
            ? "release"
            : null;
      frameSubject(!sceneShot && !diveActive);
      residents.update(t, dt, observatory ? s.specimen : null);

      const activeGate = gates[s.leg];
      calf.position.set(
        (s.lane || 0) + 0.28 * Math.sin(t * 0.52) + s.push,
        3.6 + 0.24 * Math.sin(t * 0.91),
        base,
      );
      calf.rotation.set(
        0.05 * Math.sin(t),
        -1.12 + 0.16 * Math.sin(t * 0.44) - journey * 1.56,
        0.055 * Math.cos(t * 0.71),
      );
      rig.position.set(cageTarget.x, cageTarget.y - 1.33 + 3.3 * s.lift, cageTarget.z);
      rig.rotation.z = -0.09 * (1 - s.lift) + s.tilt;
      const freed = T.MathUtils.smoothstep(s.escape, 0, 1);
      ray.position.set(
        cageTarget.x - 2.1 * freed * freed,
        cageTarget.y - .82 +
          0.046 * Math.sin(t * 0.8) +
          1.35 * T.MathUtils.smoothstep(freed, 0.52, 1),
        cageTarget.z - 7.5 * freed - 1.8 * s.lowering,
      );
      ray.rotation.set(
        0.02 * Math.cos(t),
        -Math.PI / 2 + Math.atan2(4.2 * freed, 7.5),
        0.037 * Math.sin(t * 1.4),
      );
      // Keep the wing envelope below the lifting crossbar until the ray exits.
      if (!wh && s.escape < 0.16)
        ray.position.y = Math.min(ray.position.y, rig.position.y + 1.4 - 0.52);
      reef.separate(ray.position, 0.48);
      const z = wh ? base : cageTarget.z;
      if (s.route) {
        const adult = whales[1], young = whales[0];
        cruise(adult, adult.patrol, adult.motion.distance / adult.patrolLength + .09);
        if (s.boat) mother.position.z+=12*T.MathUtils.smoothstep(s.time-s.boardSince,0,7);
        reef.separate(mother.position, 2.3);
        if (!s.escorting) {
          cruise(young, young.patrol, .76 * young.motion.distance / young.patrolLength);
          reef.separate(calf.position, 1.75);
          routeCalf.copy(calf.position);
        } else {
          let pace = 1;
          const leg = s.step === "cross" ? "gate1" : s.leg;
          const lookout = lookouts[leg];
          if (lookout) {
            if (guideLeg !== leg) calfWaiting = false;
            guideLeg = leg;
            const gap = routeCalf.distanceTo(diver.position);
            const ahead = routeCalf.distanceToSquared(lookout) < diver.position.distanceToSquared(lookout);
            const atLookout = leg !== "mother" && routeCalf.distanceToSquared(lookout) < 2.8 ** 2;
            const circling = atLookout || s.step === "cross" || (ahead && gap > (calfWaiting ? 5.4 : 7.2));
            if (circling) {
              // Stay within sight and swim an arc back towards a diver who has stopped.
              direction.subVectors(lookout, diver.position).clampLength(0, 6.2);
              rendezvous.copy(diver.position).add(direction);
              if (atLookout && gap < 7.2) rendezvous.copy(lookout);
              if (!calfWaiting) loopPhase = Math.atan2(routeCalf.x - rendezvous.x, routeCalf.z - rendezvous.z);
              loopPhase += dt * .85;
              future.copy(rendezvous);
              future.x += 2.1 * Math.sin(loopPhase + .9);
              future.z += 2.1 * Math.cos(loopPhase + .9);
            } else if (leg === "mother") {
              motherFlank(future);
            } else {
              future.copy(lookout);
            }
            future.y = T.MathUtils.clamp(future.y + .15 * Math.sin(t * .85), 2.8, 7.5);
            calfWaiting = circling;
            pace = circling ? .9 : .78;
          } else if (wh && !["talk","still"].includes(s.step)) {
            camera.getWorldDirection(direction);
            future.copy(diver.position);
            future.x += 2.8 * direction.z - 1.7 * direction.x;
            future.z -= 2.8 * direction.x + 1.7 * direction.z;
            future.y = T.MathUtils.clamp(future.y + 0.4, 2.5, 8);
          } else {
            motherFlank(future);
          }
          young.motion.follow(routeCalf, future, dt, pace);
          reef.separate(routeCalf, 1.75);
          calf.position.copy(routeCalf);
        }
      } else if (diveActive || observatory) {
        for (const [i, w] of whales.entries()) {
          cruise(w, w.path, w.motion.distance / w.length + (i ? .34 : .2));
        }
        {
          const b = t * 0.205;
          ray.position.set(
            1.3 + 2.24 * Math.sin(b),
            3.25 + 0.49 * Math.cos(b),
            -11.2 + 2.5 * Math.cos(b),
          );
          ray.rotation.y = Math.atan2(-2.5 * Math.sin(b), -2.24 * Math.cos(b));
        }
        reef.separate(calf.position, 1.8);
        reef.separate(mother.position, 2.3);
        reef.separate(ray.position, 0.68);
      }
      if (wh && !diveActive) {
        const adult = whales[1], young = whales[0];
        cruise(adult, adult.patrol, adult.motion.distance / adult.patrolLength + .09);
        reef.separate(mother.position, 2.3);
        if (s.step === "done") {
          motherFlank(future);
          if (young.motion.previous) calf.position.copy(young.motion.previous);
          young.motion.follow(calf.position, future, dt);
        } else if (s.step === "swim" && young.motion.previous) {
          future.copy(calf.position);
          calf.position.copy(young.motion.previous);
          young.motion.follow(calf.position, future, dt);
        } else {
          cruise(young, young.patrol, .76 * young.motion.distance / young.patrolLength);
          calf.position.z += base + 5.9;
        }
        reef.separate(calf.position, 1.75);
      }
      fauna.update(t);
      if (s.boat) for (let i=0;i<3;i++) {
        const j=fauna.jellies[i], phase=t*.58+i*Math.PI*.7;
        j.position.set(-2.2+1.4*Math.sin(phase),5.3+.45*Math.cos(phase),-38-i*1.4);
        fauna.jellyBounds[i].center.copy(j.position);
      }
      if (!s.boat) sub.hide();
      const large = diveActive
        ? [
            ...(calf.visible ? [{ center: calf.position, radius: 2.55 }] : []),
            ...(mother.visible ? [{ center: mother.position, radius: 4.3 }] : []),
            ...(ray.visible ? [{ center: ray.position, radius: 1.6 }] : []),
            { center: fauna.turtle.position, radius: 0.59 },
            ...fauna.jellyBounds,
          ]
        : observatory
          ? []
          : wh
            ? [
                { center: calf.position, radius: 2.55 },
                { center: mother.position, radius: 4.3 },
              ]
            : [
                { center: ray.position, radius: 1.65 },
                { center: rig.position, radius: 2.8 },
              ];
      if (diveActive && rig.visible)
        large.push({ center: rig.position, radius: s.boat ? 2.0 : 2.65 });
      school.update(t, dt, s.tide, large, camera.position, subject?.key === "tang");
      companion.update(dt);
      whalePair.update(dt, reef);
      if (s.route && calf.visible) routeCalf.copy(calf.position);
      if (wh || diveActive || observatory) for (const w of whales) w.motion.pose(dt);
      for (let i = 0; i < 32; i++) {
        const x = 3.8 + Math.sin(i * 44.7) * 1.05,
          ez = 3.2 + Math.cos(i * 9.5) * 1.8;
        tmp.position.set(x, floorHeight(x, ez) - 0.11, ez);
        tmp.rotation.set(0, 0.22 * i, 0.1 * Math.sin(t + i));
        tmp.scale.set(1, wh || subject?.key === "eel" ? 0.87 : 0.38, 1);
        tmp.updateMatrix();
        eels.setMatrixAt(i, tmp.matrix);
        if (i === 0 && subject?.key === "eel") {
          eel.position.set(.08,.32,0).applyMatrix4(tmp.matrix);
          eel.rotation.copy(tmp.rotation);
        }
      }
      eels.instanceMatrix.needsUpdate = true;
      dust.position.x = Math.sin(t * 0.17) * 0.7;
      dust.position.y = Math.sin(t * 0.24) * 0.24;
      ripple.visible = s.route && (s.leg === "song" || s.leg === "ridge");
      if (ripple.visible) {
        const cycle = ((t - s.legStart) % 3) / 3;
        ripple.position.copy(targetFor(s.leg));
        ripple.quaternion.copy(camera.quaternion);
        ripple.scale.setScalar(.3 + cycle * 1.25);
        ripple.material.opacity = .38 * Math.sin(cycle * Math.PI);
      }
      if (diveActive) {
        const info = diver.move(dt, camera, reef, garden.colliders, large, s.boat ? .9 : .27);
        if (s.leg) {
          direction.subVectors(targetFor(s.leg), camera.position);
          info.arrived = activeGate
            ? direction.lengthSq() < 2.5 ** 2
            : direction.length() <= (s.leg === "mother" ? 5.8 : 3.6);
          // A follower can pass the waypoint to either side of the whale. Count the
          // channel opening as a passage, even when the exact point is missed.
          if (activeGate && lookouts[s.leg] && direction.z >= 0 &&
              Math.abs(direction.x) < 4.3 && Math.abs(direction.y) < 2.8) {
            info.arrived = true;
          }
          info.range = direction.length();
          if (lookouts[s.leg]) {
            direction.subVectors(routeCalf, camera.position);
            info.calfWaiting = calfWaiting;
          }
          camera.getWorldDirection(future);
          const angle = Math.atan2(
            direction.x * future.z - direction.z * future.x,
            direction.x * future.x + direction.z * future.z,
          );
          info.aligned = Math.abs(angle)<=.35;
          info.signalPan = -Math.sin(angle);
          if (s.leg === "hydro") info.arrived=false;
          if (s.leg === "armDock") info.arrived=info.range<1.65;
          if (s.leg === "shoalPass" || s.leg === "reefPass") info.arrived=info.range<1.9;
          info.bearing =
            Math.abs(angle) > 2.35
              ? "在身后"
              : angle > 0.35
                ? "向左"
                : angle < -0.35
                  ? "向右"
                  : direction.y < (s.boat ? -.85 : -2)
                    ? "前方偏下"
                    : direction.y > (s.boat ? .85 : 2)
                      ? "前方偏上"
                      : "正前方";
        }
        if (s.boat) sub.drive(camera, diver, s, cageTarget, reef, info);
        composeCamera(sceneShot, dt);
        garden.update(t, camera);
        diveLamp.intensity = T.MathUtils.damp(
          diveLamp.intensity,
          9,
          3,
          dt,
        );
        diveLamp.position.copy(camera.position);
        diveLamp.position.y += 0.55;
        renderer.render(scene, camera);
        recordFrame();
        return info;
      }
      const distance = (camera.aspect < 1 ? 12 : 8.3) + (wh ? 1.6 : 0),
        angle = 0.19 + az;
      pos.set(
        (s.lane || 0) * 0.65 + distance * Math.sin(angle),
        (wh ? 3.8 : 3.35) + distance * Math.sin(pitch),
        z + distance * Math.cos(angle),
      );
      pos.y = T.MathUtils.clamp(pos.y, 0.9, 8.8);
      aim.set(
        wh ? (s.lane || 0) * 0.7 : 0.45,
        wh ? 3.05 : 1.2 + s.lift * 0.42,
        z - 1,
      );
      if (observatory) {
        const id = s.specimen,
          fish = subject.key ? atlasAnimals[subject.key] : residents.featured[id];
        if (lastSpecimen !== id) {
          snap = true;
          az = 0;
          pitch = 0;
          lastSpecimen = id;
        }
        if (snap) observerHeading = fish.rotation.y;
        const headingError = fish.rotation.y - observerHeading;
        observerHeading +=
          Math.atan2(Math.sin(headingError), Math.cos(headingError)) *
          (1 - Math.exp(-dt * 2.4));
        const subjectAngle = az + (subject.key === "jelly" ? 0 : observerHeading) + (subject.angle ?? 0.2);
        const range = subject.length * (camera.aspect < 1 ? 2.35 : 1.4);
        const tilt = id === 3
          ? T.MathUtils.clamp(.92 + pitch, .42, 1.42)
          : (subject.tilt ?? .09) + pitch;
        aim.copy(fish.position);
        aim.y += id === 3 ? .012 : -range * .06;
        pos.set(
          fish.position.x + range * Math.sin(subjectAngle) * Math.cos(tilt),
          fish.position.y + range * Math.sin(tilt),
          fish.position.z + range * Math.cos(subjectAngle) * Math.cos(tilt),
        );
      }
      reef.cameraArm(aim, pos, observatory ? 0.055 : 0.32);
      if (snap || observatory) {
        camera.position.copy(pos);
        look.copy(aim);
        snap = false;
      } else {
        camera.position.lerp(pos, 1 - Math.exp(-dt * 3));
        look.lerp(aim, 1 - Math.exp(-dt * 3));
      }
      if (!observatory) reef.cameraArm(look, camera.position, .28);
      if (observatory && s.specimen === 3) {
        camera.position.y = Math.max(camera.position.y, reef.ground(camera.position.x, camera.position.z) + .16);
      }
      camera.lookAt(look);
      composeCamera(sceneShot, dt);
      garden.update(t, camera);
      diveLamp.intensity = observatory ? 0.65 : 22;
      diveLamp.position.copy(camera.position);
      diveLamp.position.y += 0.55;
      renderer.render(scene, camera);
      recordFrame();
    },
  };
}
