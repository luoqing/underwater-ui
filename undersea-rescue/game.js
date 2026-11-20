import { createScore } from "./rescue-score.js";
import lines from "./rescue-lines.json";
import { playVoice, stopVoice } from "./rescue-voice.js";
import { swimKeys } from "./dive-controls.js";
import { species } from "./reef-life.js";
import { makeOcean } from "./ocean.js";
import cover from "./assets/reef-color-reference.jpg";
const $ = (id) => document.getElementById(id);
document.body.style.setProperty("--cover", `url("${cover}")`);
const state = { story: null },
  buttons = [...document.querySelectorAll("[data-story]")];
const clamp = (v, lo, hi) => Math.max(lo, Math.min(hi, v));
let ocean,
  loading,
  last = 0,
  ctx,
  score,
  sound = true,
  session = 0;
const ease = (a, b, t) => a + (b - a) * (1 - Math.exp(-t));
function tone(start, end, delay = 0.01, length = 0.55) {
  if (!ctx || !sound) return;
  const at = ctx.currentTime + delay,
    osc = ctx.createOscillator(),
    gain = ctx.createGain();
  osc.type = "sine";
  osc.frequency.setValueAtTime(start, at);
  osc.frequency.exponentialRampToValueAtTime(end, at + length);
  gain.gain.setValueAtTime(0.001, at);
  gain.gain.exponentialRampToValueAtTime(0.13, at + 0.05);
  gain.gain.exponentialRampToValueAtTime(0.001, at + length);
  osc.connect(gain);
  gain.connect(ctx.destination);
  osc.start(at);
  osc.stop(at + length + 0.01);
}
function call(real) {
  tone(real ? 185 : 242, real ? 118 : 238, 0.01, real ? 0.82 : 0.24);
  tone(
    real ? 150 : 242,
    real ? 102 : 238,
    real ? 1.02 : 0.43,
    real ? 0.66 : 0.24,
  );
}
let breathAt = 0,
  breathing = null;
function breathe() {
  if (!sound || !ctx || ctx.state !== "running" || ctx.currentTime < breathAt)
    return;
  const now = ctx.currentTime;
  breathAt = now + 5.4;
  const length = (ctx.sampleRate * 2.45) | 0,
    buffer = ctx.createBuffer(1, length, ctx.sampleRate),
    samples = buffer.getChannelData(0);
  for (let i = 0; i < length; i++) samples[i] = Math.random() * 2 - 1;
  const source = ctx.createBufferSource(),
    filter = ctx.createBiquadFilter(),
    gain = ctx.createGain();
  source.buffer = buffer;
  filter.type = "bandpass";
  filter.frequency.setValueAtTime(520, now);
  filter.frequency.exponentialRampToValueAtTime(210, now + 2.4);
  filter.Q.value = 0.55;
  gain.gain.setValueAtTime(0.0001, now);
  gain.gain.exponentialRampToValueAtTime(0.04, now + 0.58);
  gain.gain.exponentialRampToValueAtTime(0.002, now + 1.35);
  gain.gain.exponentialRampToValueAtTime(0.024, now + 1.72);
  gain.gain.exponentialRampToValueAtTime(0.0001, now + 2.4);
  source.connect(filter);
  filter.connect(gain);
  gain.connect(ctx.destination);
  source.start();
  breathing = source;
}
function stopBreath() {
  if (breathing) {
    breathing.stop();
    breathing = null;
  }
  breathAt = 0;
}
function audioStart() {
  try {
    ctx ??= new AudioContext();
    score ??= createScore(ctx);
    if (sound) ctx.resume().catch(() => {});
  } catch {}
}
$("audio").onclick = () => {
  sound = !sound;
  $("audio").setAttribute("aria-pressed", String(sound));
  $("audio").title = sound ? "关闭声音" : "打开声音";
  if (sound) {
    $("panel").classList.remove("spoken");
    audioStart();
    if (state.line && (!state.roaming || state.route))
      speakLine(state.line);
  } else {
    stopVoice();
    score?.stop();
    ctx?.suspend().catch(() => {});
    if (state.line) showReplies(state.line, false);
  }
};
function message(t) {
  $("feedback").textContent = t;
}
function page(phase, title, copy) {
  stopVoice();
  state.line = null;
  captionStart = null;
  $("skip-line").hidden = true;
  $("panel").classList.remove("watch-release", "dialogue", "spoken");
  $("controls").hidden = false;
  $("phase").textContent = phase;
  $("objective").textContent = title;
  $("narrative").textContent = copy;
  $("controls").replaceChildren();
  message("");
}
function button(label, fn, primary = false) {
  const b = document.createElement("button");
  b.textContent = label;
  b.className = primary ? "primary" : "";
  b.onclick = fn;
  $("controls").append(b);
  return b;
}
function range(label, value, min, max, onChange, unit = "") {
  const row = document.createElement("label");
  row.className = "slider";
  const input = document.createElement("input");
  input.type = "range";
  input.min = min;
  input.max = max;
  input.value = value;
  const out = document.createElement("span");
  out.className = "readout";
  out.textContent = value + unit;
  input.oninput = () => {
    out.textContent = input.value + unit;
    onChange(Number(input.value));
  };
  row.append(label, input, out);
  $("controls").append(row);
}
function meter(name, l, r) {
  const row = document.createElement("div");
  row.className = "meter";
  row.innerHTML =
    '<span></span><span class="track"><i class="band"></i><i class="needle"></i></span><output></output>';
  row.firstChild.textContent = name;
  $("controls").append(row);
  const band = row.querySelector(".band"),
    needle = row.querySelector(".needle"),
    out = row.querySelector("output");
  return (v, text, lo = l, hi = r) => {
    band.style.left = lo + "%";
    band.style.width = hi - lo + "%";
    needle.style.left = clamp(v, 0, 100) + "%";
    out.textContent = text;
  };
}
const legs = {
  calf: "寻找幼鲸泡泡",
  gate1: "带泡泡游出背流湾",
  gate2: "带泡泡穿过峡口",
  lee: "沿礁肩带幼鲸绕开潮沟",
  mother: "游向母鲸",
  cage: "潜向沙坡沉笼",
};
function startLeg(leg) {
  stopVoice();
  state.line = null;
  state.leg = leg;
  state.step = "navigate";
  $("panel").hidden = true;
  $("dive-hud").hidden = false;
  $("route-guide").textContent = legs[leg];
}
function arrive() {
  releaseStick();
  const leg = state.leg;
  state.leg = null;
  ocean.clearKeys();
  $("dive-hud").hidden = true;
  $("panel").hidden = false;
  if (leg === "gate1") return crossing();
  if (leg === "lee") return startLeg("gate2");
  if (leg === "gate2") return startLeg("mother");
  conversation(leg === "calf" ? "lost" : leg);
}
// Start times follow the pauses in the seven recorded Mandarin lines.
const cueStarts = {
  lost: [[2.5,"刚才"],[4.2,"我一转弯"]],
  rhythm: [[1.3,"先是"],[4.45,"最后"]],
  trust: [[1.9,"我要是"],[3.55,"你等等"]],
  mother: [[2.5,"慢慢游"],[5.4,"小翼"]],
  cage: [[1.27,"我的右"],[3.21,"先别拉"],[4.6,"能把"]],
  space: [[1.35,"你把"],[3.75,"我就能"]],
  clear: [[1.1,"你看"],[3.25,"泡泡"]]
};
let captionStart = null;
function updateCaption() {
  if (captionStart === null || !state.line) return;
  const id = state.line, line = lines[id][1], elapsed = ctx.currentTime - captionStart;
  let from = 0, to = line.length;
  for (const [at, word] of cueStarts[id]) {
    const index = line.indexOf(word);
    if (elapsed < at) { to = index; break; }
    from = index;
  }
  const text = line.slice(from,to);
  if ($("narrative").textContent !== text) $("narrative").textContent = text;
}
function showReplies(id, heard) {
  if (state.line !== id) return;
  captionStart = null;
  $("panel").classList.toggle("spoken", heard);
  $("skip-line").hidden = true;
  $("controls").hidden = false;
  if (!heard) $("narrative").textContent = lines[id][1];
}
async function speakLine(id) {
  try { await ctx?.resume(); } catch { showReplies(id,false); return; }
  if (state.line !== id || !sound || $("panel").classList.contains("spoken")) return;
  playVoice(ctx,id,(at)=>{
    captionStart = at;
    $("panel").classList.remove("spoken");
    $("controls").hidden = true;
    $("skip-line").hidden = false;
    updateCaption();
  },heard=>showReplies(id,heard));
}
$("skip-line").onclick = () => { stopVoice(); showReplies(state.line,true); };
function conversation(id) {
  const [name, line] = lines[id];
  state.step = "talk";
  page("",name.split(" · ").pop(),line);
  $("panel").classList.add("dialogue");
  state.line = id;
  if (sound) {
    $("controls").hidden = true;
    $("skip-line").hidden = false;
    $("narrative").textContent = line.slice(0,line.indexOf(cueStarts[id][0][1]));
    speakLine(id);
  }
  if (id === "lost") {
    button("怎么唱的？", () => conversation("rhythm"), true);
    button("我陪你找", () => conversation("trust"));
  } else if (id === "rhythm" || id === "trust")
    button(id === "trust" ? "一起听" : "听鲸歌", briefingWhale, true);
  else if (id === "mother")
    button(
      "我去找小翼",
      () => {
        state.story = "manta";
        startLeg("cage");
      },
      true,
    );
  else if (id === "cage") {
    button("哪边能动？", () => conversation("space"), true);
    button("看看笼架", rigging);
  } else if (id === "space") button("一起抬", rigging, true);
  else if (id === "clear") button("回海图", exitDive, true);
  const replay = button("重听", () => {
    $("panel").classList.remove("spoken");
    if (!sound) $("audio").click();
    else {
      audioStart();
      speakLine(id);
    }
  });
  replay.classList.add("icon");
  replay.setAttribute("aria-label","重听对白");
  replay.title = "重听对白";
  replay.replaceChildren($("replay-icon").content.cloneNode(true));
}
function briefingWhale() {
  state.step = "tune";
  page(
    "水听器 · 接收",
    "听清鲸歌",
    "调到信号最强处。",
  );
  const signal = meter("信号", 82, 100);
  function adjust(v) {
    state.freq = v;
    const strength = Math.max(0, 100 - Math.abs(v - state.targetHz) * 3.4);
    signal(strength, Math.round(strength) + "%");
    if (strength > 82) tone(160, 127, 0.01, 0.2);
  }
  range("调谐", state.freq, 40, 100, adjust, " Hz");
  adjust(state.freq);
  button(
    "锁定呼声",
    () => {
      if (Math.abs(state.freq - state.targetHz) > 5)
        return message("杂音还很重。向信号变强的一侧微调。");
      call(true);
      bearings();
    },
    true,
  );
}
function bearings() {
  state.step = "listen";
  page(
    "水听器 · 辨向",
    "哪边是母鲸？",
    "试听两侧，找出一长一短、音调渐低的呼唤。",
  );
  const choose = button(
    "向此处领航",
    () => {
      if (state.channel === undefined) return message("先选一侧水听器试听。");
      if (state.channel !== state.right)
        return message("幼鲸没有跟来。这个方向只有等长的短音，再听听另一侧。");
      state.gate = 0;
      crossing();
    },
    true,
  );
  for (const [i, name] of ["西侧", "东侧"].entries()) {
    const b = button("试听" + name, () => {
      state.channel = i;
      call(i === state.right);
      message(
        name +
          "录音　" +
          (i === state.right
            ? "━━━━ ↘　━━ ↘　（一长一短）"
            : "━━ →　━━ →　（等长短音）"),
      );
    });
    $("controls").insertBefore(b, choose);
  }
}
function crossing() {
  state.step = "cross";
  const second = state.gate === 1;
  page(
    "护航 · " + (second ? "峡口" : "背流湾"),
    second ? "越过最后一道潮沟" : "带幼鲸离开背流湾",
    second
      ? state.route
        ? "等流速回落，或带泡泡沿右侧礁肩绕行。"
        : "峡口更窄。观察漂沙和流速表，等横流回落到绿色区间再出发。"
      : "等指针进入浅绿区。",
  );
  state.limit = second ? 29 : 42;
  state.showFlow = meter("横流", 0, state.limit);
  button(
    "发出领航信号",
    () => {
      if (state.flow > state.limit) {
        state.push = state.tide > 0 ? 1 : -1;
        tone(120, 88);
        message("横流把幼鲸推回了湾内。等水流转弱后再试。");
        return;
      }
      tone(200, 340);
      if (state.route) {
        state.escorting = true;
        state.gate++;
        startLeg(second ? "gate2" : "gate1");
        return;
      }
      state.step = "swim";
      state.goal = second ? 1 : 0.48;
      state.gate++;
      state.targetLane = second ? -2.25 : 2.25;
      page(
        "护航 · 操舵",
        "带泡泡沿缓流穿过峡口",
        "幼鲸会跟随你的航向。用方向键或舵杆调整左右位置，抵消横流漂移，让白色指针进入绿色航道。",
      );
      state.showLane = meter("航道", 0, 0);
      range("舵杆", 50, 0, 100, (v) => (state.steer = (v - 50) / 50), "%");
    },
    true,
  );
  if (second && state.route) {
    button("沿礁肩绕行", () => {
      state.gate++;
      startLeg("lee");
    });
  }
}
function rigging() {
  state.step = "rig";
  page(
    "打捞 · 勘察",
    "托稳笼架",
    "右侧陷进沙里了，从哪里抬？",
  );
  button("抬起左侧", () => {
    state.tilt = 0.2;
    message("左边翘起，右脚却陷得更深。需要分散拉力。");
  });
  button(
    "托住两端",
    () => {
      state.tilt = 0;
      ballast();
    },
    true,
  );
  button("抬起右侧", () => {
    state.tilt = -0.18;
    message("笼架向一侧倾斜了。试试把力分到两端。");
  });
}
function ballast() {
  state.step = "lift";
  page(
    "打捞 · 托举横梁",
    "两端一起慢慢抬",
    "把指针稳在浅绿区。",
  );
  state.showLoad = meter("力度", 42, 66);
  range("托举力道", state.valve, 0, 100, (v) => (state.valve = v), "%");

}
function finish() {
  state.step = "done";
  const wh = state.story === "whale";
  call(wh);
  if (state.route) return conversation(wh ? "mother" : "clear");
  page(
    "潜航记录 · 救援完成",
    wh ? "母鲸接住了那声回应" : "沙地重新有了动静",
    wh
      ? "幼鲸游到母鲸身侧。两道背影穿过峡口，水听器里终于有了重叠的呼声。"
      : "笼架悬停在海床上方。蝠鲼从横梁下滑出，绕过沙坡游向深水。",
  );
  button("进入另一项救援", () => start(wh ? "manta" : "whale"), true);
  button("重玩本次潜航", () => start(state.story));
  button("返回海图", exitDive);
}
function observe(index) {
  state.step = "observe";
  state.specimen = index;
  const fish = species[index];
  page("近距观测 · " + fish.latin, fish.name, fish.note);
  for (const [i, f] of species.entries())
    button(f.name, () => observe(i), i === index);
}
let hintTimer;
function switchSwim(on, inPlace = true) {
  releaseStick();
  if (on) stopVoice();
  state.roaming = on;
  held.clear();
  $("panel").hidden = on;
  clearTimeout(hintTimer);
  $("swim-tip").hidden = !on;
  if (on) hintTimer = setTimeout(() => { $("swim-tip").hidden = true; }, 4000);
  $("dive-hud").hidden = !on;
  $("route-guide").hidden = !state.route;
  $("face-target").hidden = !state.route;
  $("roam-toggle").hidden = state.story === "roam" || state.route;
  const roamLabel = on ? "返回任务" : "自由潜水";
  $("roam-toggle").title = roamLabel;
  $("roam-toggle").setAttribute("aria-label",roamLabel);
  if (on) ocean.enterWater(inPlace);
  else {
    stopBreath();
    ocean.leaveWater();
  }
}
$("face-target").onclick = () => ocean.faceWaypoint(state.leg);
$("roam-toggle").onclick = () => switchSwim(!state.roaming);
const stick = $("swim-stick");
const grip = stick.firstElementChild;
let stickPointer = null;
function releaseStick() {
  const pointer = stickPointer;
  stickPointer = null;
  if (pointer !== null && stick.hasPointerCapture(pointer))
    stick.releasePointerCapture(pointer);
  grip.style.transform = "";
  ocean?.swimStick(0, 0);
}
function steerStick(event) {
  if (event.pointerId !== stickPointer) return;
  const rect = stick.getBoundingClientRect();
  const limit = (rect.width - grip.offsetWidth) / 2;
  if (limit <= 0) return;
  let x = (event.clientX - rect.left - rect.width / 2) / limit;
  let y = (rect.top + rect.height / 2 - event.clientY) / limit;
  const distance = Math.hypot(x, y);
  x /= Math.max(1, distance);
  y /= Math.max(1, distance);
  grip.style.transform = "translate(" + x * limit + "px," + -y * limit + "px)";
  const deadZone = 0.09;
  const strength =
    distance <= deadZone
      ? 0
      : (Math.min(distance, 1) - deadZone) /
        ((1 - deadZone) * Math.min(distance, 1));
  ocean?.swimStick(x * strength, y * strength);
}
stick.onpointerdown = (e) => {
  if (stickPointer !== null) return;
  e.preventDefault();
  stickPointer = e.pointerId;
  stick.setPointerCapture(e.pointerId);
  steerStick(e);
};
stick.onpointermove = steerStick;
stick.onpointerup =
  stick.onpointercancel =
  stick.onlostpointercapture =
    (e) => {
      if (e.pointerId === stickPointer) releaseStick();
    };
for (const btn of document.querySelectorAll("[data-key]")) {
  btn.onpointerdown = (e) => {
    e.preventDefault();
    btn.setPointerCapture(e.pointerId);
    ocean?.swimKey(btn.dataset.key, true);
  };
  btn.onpointerup =
    btn.onpointercancel =
    btn.onlostpointercapture =
      () => ocean?.swimKey(btn.dataset.key, false);
}
function exitDive() {
  score?.stop();
  releaseStick();
  if (location.hash && location.protocol.startsWith("http"))
    history.replaceState(null, "", location.pathname + location.search);
  stopVoice();
  state.line = null;
  stopBreath();
  ocean?.leaveWater();
  state.roaming = false;
  held.clear();
  session++;
  state.story = null;
  $("menu").hidden = false;
  $("dive").hidden = true;
  $("load").textContent = "选择救援或自由观察";
  buttons[0].focus();
}
$("exit").onclick = exitDive;
async function start(story, route = false) {
  if (story === "route") {
    story = "whale";
    route = true;
  }
  stopVoice();
  const id = ++session;
  audioStart();
  buttons.forEach((b) => (b.disabled = true));
  $("load").textContent = "正在装载峡谷与动物模型…";
  try {
    loading ??= makeOcean($("view"));
    ocean = await loading;
    if (session !== id) return;
    Object.assign(state, {
      story,
      route,
      leg: null,
      gate: 0,
      escorting: false,
      step: "",
      time: 0,
      travel: 0,
      escape: 0,
      lift: 0,
      tide: 0,
      flow: 0,
      push: 0,
      tilt: 0,
      freq: 42,
      targetHz: 65 + Math.floor(Math.random() * 25),
      lane: 0,
      steer: 0,
      right: Math.random() < 0.5 ? 0 : 1,
      channel: undefined,
      valve: 5,
      verticalSpeed: 0,
    });
    $("menu").hidden = true;
    $("dive").hidden = false;
    ocean.reset();
    if (!route && story !== "roam")
      story === "observe"
        ? observe(0)
        : story === "whale"
          ? briefingWhale()
          : rigging();
    switchSwim(story === "roam" || route, false);
    if (route) startLeg("calf");
    last = performance.now();
    requestAnimationFrame((now) => frame(now, id));
  } catch (err) {
    console.error(err);
    loading = null;
    state.story = null;
    $("dive").hidden = true;
    $("menu").hidden = false;
    $("load").textContent = "海域载入失败。请用支持 WebGL 2 的浏览器重新打开。";
  } finally {
    buttons.forEach((b) => (b.disabled = false));
  }
}
function frame(now, id) {
  if (session !== id || !state.story) return;
  const dt = Math.min(Math.max(0, (now - last) / 1000), 0.1);
  last = now;
  if (state.route || !state.roaming) tickMission(dt);
  if (sound && !document.hidden) score?.update(state);
  updateCaption();
  const read = ocean.render(state, dt);
  if (state.roaming) {
    breathe();
    if (state.leg) {
      const guide = $("route-guide");
      const label = legs[state.leg] + "　" + read.bearing + "　" + Math.round(read.range) + " 米";
      if (label !== guide.textContent) guide.textContent = label;
      if (read.arrived) arrive();
    }
    if (!$("dive-hud").hidden) {
      const depth = $("dive-readout");
      const label = "↓ " + read.depth + " m";
      if (depth.textContent !== label) depth.textContent = label;
    }
  }
  requestAnimationFrame((t) => frame(t, id));
}
function tickMission(dt) {
  state.time += dt;
  state.tide = Math.sin(state.time * (state.gate === 1 ? 0.94 : 0.78) + 0.53);
  state.flow = Math.abs(state.tide) * 100;
  state.push = ease(state.push, 0, dt * 2);
  if (state.step === "cross") {
    state.showFlow(state.flow, (state.flow / 68).toFixed(1) + " 节");
  }
  if (state.step === "swim") {
    const direction = held.has("ArrowLeft")
      ? -1
      : held.has("ArrowRight")
        ? 1
        : state.steer;
    state.lane = clamp(
      state.lane + dt * (direction * 2.8 + state.tide * 0.35),
      -4,
      4,
    );
    const aligned = Math.abs(state.lane - state.targetLane) < 1.05;
    const end = aligned ? state.goal : state.goal - 0.025;
    state.travel = Math.max(
      state.travel,
      Math.min(end, state.travel + dt * 0.108),
    );
    const screen = (x) => (x + 4) * 12.5;
    state.showLane(
      screen(state.lane),
      aligned
        ? "已对准"
        : state.lane < state.targetLane
          ? "向右调整"
          : "向左调整",
      screen(state.targetLane - 1.05),
      screen(state.targetLane + 1.05),
    );
    if (state.travel >= state.goal) {
      state.steer = 0;
      if (state.gate === 2) finish();
      else crossing();
    }
  }
  if (state.step === "lift") {
    const center = 29 + 43 * state.lift + 6 * Math.sin(state.time * 0.75),
      ok = Math.abs(state.valve - center) <= 9;
    state.verticalSpeed = ok ? ease(state.verticalSpeed, 0.12, dt * 2.2) : 0;
    state.lift = clamp(state.lift + state.verticalSpeed * dt, 0, 1);
    state.showLoad(
      state.valve,
      (ok ? "上升 " : "稳住 ") + Math.round(state.lift * 100) + "%",
      center - 9,
      center + 9,
    );
    if (state.lift >= 1) {
      state.step = "escape";
      page("打捞 · 观察", "通道打开了", "稳住横梁。蝠鲼正在离开笼架。");
      $("panel").classList.add("watch-release");
    }
  }
  if (state.step === "escape") {
    state.escape = Math.min(1, state.escape + dt / 8.5);
    if (state.escape >= 1) finish();
  }
}
const held = new Set();
addEventListener("keydown", (e) => {
  if (
    state.story &&
    !e.metaKey &&
    !e.ctrlKey &&
    !e.altKey &&
    e.target.tagName !== "INPUT"
  ) {
    if (state.roaming && (!state.route || state.leg) && swimKeys.has(e.code)) {
      e.preventDefault();
      ocean.swimKey(e.code, true);
      return;
    }
    if (!state.roaming && ["KeyW", "KeyA", "KeyS", "KeyD"].includes(e.code)) {
      e.preventDefault();
      switchSwim(true);
      ocean.swimKey(e.code, true);
      return;
    }
    if (state.roaming && e.code === "Escape") {
      state.story === "roam" || state.route ? exitDive() : switchSwim(false);
      return;
    }
  }
  if (
    state.step === "swim" &&
    state.story &&
    e.target.tagName !== "INPUT" &&
    ["ArrowLeft", "ArrowRight"].includes(e.key)
  ) {
    e.preventDefault();
    held.add(e.key);
  }
});
addEventListener("keyup", (e) => {
  held.delete(e.key);
  ocean?.swimKey(e.code, false);
});
addEventListener("blur", () => {
  held.clear();
  releaseStick();
  ocean?.clearKeys();
});
document.addEventListener("visibilitychange", () => {
  if (document.hidden) {
    score?.stop();
    held.clear();
    releaseStick();
    ocean?.clearKeys();
  }
});
buttons.forEach((b) => (b.onclick = () => start(b.dataset.story)));
$("load").textContent = "选择救援或自由观察";

if (location.hash === "#dive") start("roam");
if (location.hash === "#atlas") start("observe");

if (location.hash === "#rescue") start("route");
