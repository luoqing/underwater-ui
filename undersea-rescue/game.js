import fleetPreview from "./assets/fleet-cabin-cards.jpg";
import { fleet } from "./sub-fleet.js";
import { boatLegs } from "./echo-sub.js";
import { whaleCall, whaleCalling, quietWhale } from "./whale-call.js";
import { createScore } from "./rescue-score.js";
import lines from "./rescue-lines.json";
import { playVoice, stopVoice } from "./rescue-voice.js";
import { swimKeys } from "./dive-controls.js";
import { atlas } from "./reef-life.js";
import { makeOcean } from "./ocean.js";
import { seaChart } from "./sea-chart.js";
import cover from "./assets/reef-color-reference.jpg";
const $ = (id) => document.getElementById(id);
document.body.style.setProperty("--cover", `url("${cover}")`);
const state = { story: null },
  buttons = [...document.querySelectorAll("[data-story]")];
const clamp = (v, lo, hi) => Math.max(lo, Math.min(hi, v));
const picker=$("vessels");
picker.style.setProperty("--fleet-preview", `url("${fleetPreview}")`);
let vesselId="echo";
try {
  const saved=localStorage.getItem("undersea-rescue.vessel");
  if (Object.hasOwn(fleet,saved)) vesselId=saved;
} catch {}
for (const [id,v] of Object.entries(fleet)) {
  const card=document.createElement("label"), input=document.createElement("input");
  input.type="radio"; input.name="vessel"; input.value=id; input.checked=id===vesselId;
  const image=document.createElement("span"), title=document.createElement("strong"), feature=document.createElement("small");
  image.className="sub-preview "+id; image.setAttribute("aria-hidden","true");
  title.textContent=v.name; feature.textContent=v.feature;
  card.append(input,image,title,feature); picker.append(card);
  input.onchange=()=>{
    vesselId=id;
    try {localStorage.setItem("undersea-rescue.vessel",id);} catch {}
  };
}
let ocean,
  loading,
  last = 0,
  ctx,
  score,
  sound = true,
  session = 0;
const ease = (a, b, t) => a + (b - a) * (1 - Math.exp(-t));
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
    state.nextSong = state.time;
    if (state.line && (!state.roaming || state.route))
      speakLine(state.line);
  } else {
    quietWhale();
    stopVoice();state.crewSpeaking=false;
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
  song: "沿声纹驶向礁口",
  calf: "靠近那头小鲸",
  gate1: "跟着泡泡驶向峡口",
  gate2: "顺着缓流穿过去",
  lee: "贴着右侧礁壁绕行",
  mother: "靠近鲸鱼妈妈",
  hydro: "停艇听声 · 左右拨杆转舵",
  fork: "开到水母群前选路",
  shoalPass: "从两只水母之间穿过",
  reefPass: "下潜，沿右侧沙坡绕行",
  armDock: "靠近笼子，听小翼说",
  ridge: "循着敲击声绕过礁肩",
  cage: "靠近横梁下的小翼",
};
const saveKey = "undersea-rescue.journey.v1";
function checkpoint() {
  try {
    const data = JSON.parse(localStorage.getItem(saveKey));
    if (data && Object.hasOwn(legs, data.leg) && Array.isArray(data.position)
      && data.position.length === 3 && data.position.every(Number.isFinite)) return data;
  } catch {}
  return null;
}
function refreshMenu() {
  $("load").textContent = "";
}
function startLeg(leg) {
  quietWhale();
  stopVoice();
  state.line = null;
  state.leg = leg;
  state.step = "navigate";
  state.nextSong = state.time + .3;
  state.legStart = state.time;
  state.lockTime = 0;
  if (leg === "hydro") state.nextPing=state.time+.25;
  if (leg === "armDock") state.foundCage=true;
  if (leg !== "song" && !state.chapterPreview) {
    try { localStorage.setItem(saveKey, JSON.stringify({leg, position:ocean.diverPosition(), gate:state.gate})); } catch {}
  }
  $("panel").hidden = true;
  $("swim-tip").hidden = true;
  $("dive-hud").hidden = false;
  $("route-guide").textContent = legs[leg];
  $("face-target").disabled = state.boat && leg !== "hydro";
  $("face-target").title = state.boat ? (leg==="hydro" ? "重听敲击" : "航向提示") : "转向当前救援目标";
  $("face-target").setAttribute("aria-description",$("face-target").title);

}
function arrive() {
  const leg = state.leg;
  if (leg === "hydro") return startLeg("fork");
  if (leg === "shoalPass" || leg === "reefPass") return startLeg("armDock");
  if (leg === "lee") return startLeg("gate2");
  if (leg === "gate2") return startLeg("mother");
  releaseStick();
  state.leg = null;
  ocean.clearKeys();
  $("dive-hud").hidden = true;
  $("panel").hidden = false;
  quietWhale();
  if (leg === "fork") {
    state.step="choosePass";
    page("", "从哪边去救小翼？", "上面有水母，右边的沙坡水流更缓。");
    button("上层 · 水母间隙",()=>startLeg("shoalPass"),true);
    button("下层 · 贴礁绕行",()=>startLeg("reefPass"));
    return;
  }
  if (leg === "armDock") return conversation("cage");
  if (leg === "song") return startLeg("calf");
  if (leg === "ridge") { state.foundCage = true; return startLeg("cage"); }
  if (leg === "gate1") return crossing();
  conversation(leg === "calf" ? "lost" : leg);
}
// Start times follow the pauses in the seven recorded Mandarin lines.
const cueStarts = {
  lost: [[2.5,"刚才"],[4.2,"我一转弯"]],
  rhythm: [[1.3,"先是"],[4.45,"最后"]],
  trust: [[1.9,"我要是"],[3.55,"你等等"]],
  mother: [[2.5,"慢慢游"]],
  knock: [],
  cage: [[1.87,"能帮我"]],
  space: [[1.89,"我就能"]],
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
  if (id === "mother") {
    state.line = null;
    startReunionPause();
    return;
  }
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
  page("",name,line);
  $("panel").classList.add("dialogue");
  state.line = id;
  if (sound) {
    $("controls").hidden = true;
    $("skip-line").hidden = false;
    $("narrative").textContent = cueStarts[id].length ? line.slice(0,line.indexOf(cueStarts[id][0][1])) : line;
    speakLine(id);
  }
  if (id === "mother" && !sound) state.mutedMotherUntil = state.time + 5.2;
  if (id === "lost") {
    button("陪它找找", () => {
      state.escorting = true;
      startLeg("gate1");
    }, true);
  } else if (id === "knock") {
    button("辨听敲击声", () => {
      state.story="manta"; state.reunionSince=state.time;
      startLeg("hydro");
    },true);
  }
  else if (id === "cage") {
    if (state.boat) {
      button("皮医生，准备出舱",()=>crewTransit(false),true);
    } else {
      button("怎么救你？", () => conversation("space"), true);
      button("看看笼架",rigging);
    }
  } else if (id === "space") button("一起抬", state.boat ? armControl : rigging, true);
  else if (id === "clear") {
    button("一起潜水", () => {
      state.foundCage = false;
      state.story = "roam";
      state.route = false;
      if (state.boat) ocean.leaveDock();
      state.leg = state.line = null;
      state.step = "";
      switchSwim(true, false, true);
    }, true);
    button("返回首页", exitDive);
  }
  if (id === "mother") return;
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
function startReunionPause() {
  state.step = "still";
  state.line = null;
  state.stillSince = state.time;
  state.knocked = false;
  $("panel").hidden = true;
}
function crossing() {
  state.step = "cross";
  page("", "峡口的水正往旁边推", "等指针落进浅绿区，带泡泡游过去。");
  state.limit = 32;
  state.showFlow = meter("横流", 0, state.limit);
  button("趁缓流通过", () => {
    if (state.flow > state.limit) {
      state.push = Math.sign(state.tide);
      return message("泡泡退了半步。等水慢一点。");
    }
    startLeg("gate2");
  }, true);
  button("沿礁肩绕行", () => startLeg("lee"));
}
const medicLines={
  depart:"你稳住艇，我去小翼身边看看。",
  ready:"这里看得清！你来抬，我帮你看着。",
  safe:"尾巴也出来啦，可以放下了。",
  home:"我回来啦！咱们跟上小翼吧。"
};
function medicLine(id){
  page("","皮医生",medicLines[id]);
  $("panel").classList.add("dialogue");
  state.crewSpeaking=sound;
  if(sound)playVoice(ctx,id,()=>{},()=>{state.crewSpeaking=false});
}
function crewTransit(returning){
  state.step=returning?"crewBack":"crewOut";
  state.crewTime=0;
  if(returning){
    page("","","");
    $("panel").hidden=true;
  }else medicLine("depart");
}
function armControl() {
  medicLine("ready");
  $("panel").hidden=false;
  state.step="arm";
  state.armReach=0;
  const grip=button("按住机械臂 · 松手暂停",()=>{},true);
  grip.style.touchAction="none";
  const down=()=>{state.armHeld=true;grip.setAttribute("aria-pressed","true");};
  const up=()=>{state.armHeld=false;grip.setAttribute("aria-pressed","false");};
  up();
  grip.onpointerdown=e=>{e.preventDefault();grip.setPointerCapture(e.pointerId);down();};
  grip.onpointerup=grip.onpointercancel=grip.onlostpointercapture=up;
  grip.onkeydown=e=>{if(["Space","Enter"].includes(e.code)){e.preventDefault();down();}};
  grip.onkeyup=up;grip.onblur=up;
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
  if(!state.chapterPreview)try { localStorage.removeItem(saveKey); } catch {}
  conversation("clear");
}
function observe(index, exhibit = null) {
  state.step = "observe";
  state.specimen = index;
  const entry = atlas[index];
  state.exhibit=exhibit;
  const notes={
    echo:"听潮号能听出鲸歌的方向。转到艇尾，看看并排转动的两只螺旋桨。",
    swift:"尖艇首划开海水，竖尾帮它拐弯。拖动转一圈，找找尾巴里的螺旋桨。",
    stone:"宽宽的鲸尾下藏着双桨。拖动看看它的尾巴。"
  };
  page("",exhibit?fleet[exhibit].name:entry.name,exhibit?notes[exhibit]:entry.note);
  const picker = document.createElement("select");
  picker.setAttribute("aria-label", "选择观察的海洋动物");
  for (const [i, animal] of atlas.entries()) picker.add(new Option(animal.name, String(i)));
  if(exhibit)picker.prepend(new Option("看动物","",true,true));
  picker.value = exhibit?"":String(index);
  picker.onchange = () => {
    observe(Number(picker.value));
    $("controls").querySelector("select").focus({ preventScroll: true });
  };
  const vessels=document.createElement("select");
  vessels.setAttribute("aria-label","选择观察的潜艇");
  const prompt=new Option("看潜艇","");prompt.disabled=true;vessels.add(prompt);
  for(const [id,boat] of Object.entries(fleet))vessels.add(new Option(boat.name,id));
  vessels.value=exhibit || "";
  vessels.onchange=()=>{
    observe(index,vessels.value);
    $("controls").querySelector('[aria-label="选择观察的潜艇"]').focus({preventScroll:true});
  };
  $("controls").append(picker,vessels);
}
let hintTimer, companionTimer;
function switchSwim(on, inPlace = true, keepPosition = false) {
  releaseStick();
  $("open-chart").hidden=!on;
  if(!on)showChart(false);
  else if(ocean)chart.prepare(ocean.chartFix(null).ground);
  $("dive-hud").classList.toggle("at-helm",!!state.boat);
  document.querySelector(".keyboard-guide").innerHTML=state.boat ? "W 推进 · S 倒车 · A 左舵 · D 右舵<br>E 上浮 · Q 下潜" : "W 前进 · S 后退 · A 左移 · D 右移<br>E 上浮 · Q 下潜";
  $("swim-tip").firstElementChild.textContent=state.boat ? "左键拖动环视 · 双击看前方" : "拖动转向 · 双击动物同游";
  $("swim-tip").lastElementChild.textContent=state.boat ? "左手驾艇 · 右侧拖动环视 · 双点看前方" : "左手摇杆游动 · 右手划动转向 · 双点动物同游";
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
  if (on) { if (!keepPosition) ocean.enterWater(inPlace, state.story==="roam"); }
  else {
    stopBreath();
    ocean.leaveWater();
  }
}
$("face-target").onclick = () => {
  if (state.leg === "hydro") state.nextPing=state.time;
  else if (!state.boat) ocean.faceWaypoint(state.leg);
};
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
const chart=seaChart($("chart-canvas"));
let nextFix=0;
function showChart(open){
  $("sea-chart").hidden=!open;
  $("open-chart").setAttribute("aria-expanded",String(open));
  $("open-chart").title=open ? "收起海图" : "打开海图";
  $("open-chart").setAttribute("aria-label",$("open-chart").title);
  if(open && ocean)chart.draw(ocean.chartFix(state.leg));
}
$("open-chart").onclick=()=>{if(state.roaming)showChart($("sea-chart").hidden)};
function exitDive() {
  showChart(false);
  quietWhale();
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
  refreshMenu();
  showFleet(false);
  buttons[0].focus();
}
$("exit").onclick = exitDive;
async function start(story, route = false) {
  const chapter=new URLSearchParams(location.search).get("chapter");
  if (story === "route") {
    story = "whale";
    route = true;
  }
  stopVoice();
  const id = ++session;
  audioStart();
  $("sail").disabled=$("back-menu").disabled=true;
  buttons.forEach((b) => (b.disabled = true));
  $("load").textContent = "正在装载峡谷与动物模型…";
  try {
    loading ??= makeOcean($("view"), name => {
      if (name && state.story === "observe" && !state.roaming) switchSwim(true);
      if (name) {
        clearTimeout(hintTimer);
        $("swim-tip").hidden = true;
      }
      const status = $("companion-status");
      clearTimeout(companionTimer);
      status.hidden = !name;
      status.textContent = name ? name + ((navigator.maxTouchPoints > 0 || matchMedia("(pointer: coarse)").matches) ? "同游中 · 再双点退出" : "同游中 · 再双击退出") : "";
      $("view").dataset.companion = name || "";
      if (name) companionTimer = setTimeout(() => { status.hidden = true; }, 2800);
    });
    ocean = await loading;
    if (session !== id) return;
    Object.assign(state, {
      story,exhibit:null,
      boat:route, vessel:fleet[vesselId], reunionSince:-8, armHeld:false, armReach:0, pingTime:-10,
      route, crewTime:0, crewSpeaking:false,
      chapterPreview:route && ["airlock","approach"].includes(chapter),
      leg: null,
      gate: 0,
      escorting: false,
      step: "",
      time: 0,
      travel: 0,
      escape: 0,
      lowering: 0,
      lift: 0,
      tide: 0,
      flow: 0,
      push: 0,
      tilt: 0,
      foundCage: false,
      mutedMotherUntil: 0,
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
    ocean.selectVessel(vesselId);
    if (story === "observe") observe(0);
    switchSwim(story === "roam" || route, false);
    if (route) {
      const saved = state.chapterPreview ? {leg:"armDock",position:ocean.dockApproach(chapter==="approach")} : checkpoint();
      if (saved) {
        if (saved.leg==="ridge") saved.leg="fork";
        if (saved.leg==="cage") saved.leg="armDock";
        if (boatLegs.has(saved.leg)) state.story="manta";
        ocean.restoreDiver(saved.position);
        if(chapter==="approach")ocean.faceWaypoint("armDock");
        state.escorting = !["song","calf"].includes(saved.leg);
        if (state.escorting) ocean.restoreEscort(saved.leg);

        state.gate = saved.gate || 0;
      }
      startLeg(saved?.leg || "song");
    }
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
    $("sail").disabled=$("back-menu").disabled=false;
    buttons.forEach((b) => (b.disabled = false));
  }
}
function frame(now, id) {
  if (session !== id || !state.story) return;
  const dt = Math.min(Math.max(0, (now - last) / 1000), 0.1);
  last = now;
  if (state.route || !state.roaming) tickMission(dt);
  updateCaption();
  const read = ocean.render(state, dt);
  if (sound && !document.hidden) score?.update(state, read, captionStart !== null || state.crewSpeaking || whaleCalling());
  if (state.roaming) {
    if (!state.boat) breathe(); else if (breathing) stopBreath();
    if (state.leg) {
      const guide = $("route-guide");
      const listening = state.leg === "song" || state.leg === "calf" || state.leg === "mother";
      if (listening && sound && !document.hidden && state.time >= state.nextSong) {
        whaleCall(ctx, read.signalPan, read.range);
        state.nextSong = state.time + 12;
      }
      const firstListen = state.leg === "song" && state.time - state.legStart < 3;
      const label = firstListen ? state.vessel.name+" · 礁后传来一声鲸鸣…" :
        state.leg === "song" ? ((navigator.maxTouchPoints > 0 || matchMedia("(pointer: coarse)").matches) ? "推摇杆，驶向声纹" : "按 W，驶向声纹") + " · " + read.bearing :
        (read.calfWaiting ? "泡泡游回来接你" : legs[state.leg]) + " · " + read.bearing;
      let text=label;
      if (state.leg==="hydro") {
        if (state.time>=state.nextPing) {
          state.pingTime=state.time; state.nextPing=state.time+2.8;
          if (sound) score?.signal(read.signalPan,read.aligned ? .19 : .10);
        }
        const still=read.speed<.3;
        state.lockTime=still && read.aligned ? state.lockTime+dt : 0;
        text=still ? (read.aligned ? "正前方 · 听清三声" : "转动艇首听声 · "+read.bearing)+"　↻" : "松开摇杆，听三声敲击";
        if (state.lockTime>=1.2 && state.time-state.pingTime>=.78 && state.time-state.pingTime<1.5) read.arrived=true;
      }
      if (text !== guide.textContent) guide.textContent=text;
      if(state.leg!=="armDock" && (read.cageInReach || (state.leg!=="hydro" && read.pastFork))){
        startLeg("armDock");
        if(read.atCage)arrive();
      }else if(read.arrived)arrive();
    }
    if (!$("dive-hud").hidden) {
      const depth = $("dive-readout");
      const label = read.depth+" m";
      if(!$("sea-chart").hidden && now>=nextFix){chart.draw(ocean.chartFix(state.leg));nextFix=now+100;}
      if (depth.textContent !== label) depth.textContent = label;
    }
  }
  requestAnimationFrame((t) => frame(t, id));
}
function tickMission(dt) {
  state.time += dt;
  if (state.line === "mother" && !sound && state.time >= state.mutedMotherUntil) startReunionPause();
  if (state.step === "still") {
    const elapsed = state.time - state.stillSince;
    if (elapsed >= 2.3 && !state.knocked) {
      state.knocked = true;
      if (sound) score?.signal();
      page("", "", "礁石后响了三下。咚、咚……咚。");
      $("panel").classList.add("dialogue");
      $("panel").hidden = false;
    }
    if (elapsed >= 4.8) conversation("knock");
  }
  state.tide = Math.sin(state.time * (state.gate === 1 ? 0.94 : 0.78) + 0.53);
  state.flow = Math.abs(state.tide) * 100;
  state.push = ease(state.push, 0, dt * 2);
  if (state.step === "cross") {
    state.showFlow(state.flow, (state.flow / 68).toFixed(1) + " 节");
  }
  if(state.step==="crewOut" || state.step==="crewBack"){
    const previous=state.crewTime;
    state.crewTime+=dt;
    if(state.step==="crewOut"){
      if(previous<3.4 && state.crewTime>=3.4)$("panel").hidden=true;
      if(state.crewTime>=14.05)armControl();
    }else{
      state.armReach=Math.max(0,state.armReach-dt*1.1);
      if(previous<16.3 && state.crewTime>=16.3){
        $("panel").hidden=false;medicLine("home");
      }
      if(state.crewTime>=19.65 && !state.crewSpeaking)finish();
    }
  }
  if (state.step==="arm") {
    if(state.armHeld)state.armReach=Math.min(1,state.armReach+dt*1.15);
    if (state.armHeld && state.armReach>=1) state.lift=Math.min(1,state.lift+dt*state.vessel.lift);
    message(state.lift>0 ? Math.round(state.lift*100)+"% · "+(state.armHeld?"托举中":"止回扣已托稳") : "");
    if (state.lift>=1) {
      state.armHeld=false; state.step="escape";
      page("","通道打开了","小翼正在收翅，等它游过去。");
      $("panel").classList.add("watch-release");
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
  if(state.step==="talk" && state.line==="clear")state.armReach=Math.max(0,state.armReach-dt*.95);
  if (state.step === "escape") {
    state.escape = Math.min(1, state.escape + dt / 8.5);
    if (state.escape >= 1) {
      state.step = "lower";
      if(state.boat)medicLine("safe");
      else $("objective").textContent = "小翼游远了，笼架缓缓落下";
    }
  } else if (state.step === "lower") {
    state.lowering = Math.min(1, state.lowering + dt / 4);
    state.lift = 1 - state.lowering ** 2 * (3 - 2 * state.lowering);
    if (state.lowering >= 1) { if(state.boat)crewTransit(true); else finish(); }
  }
}
const held = new Set();
addEventListener("keydown", (e) => {
  if(e.target===$("open-chart") && e.code==="Space")return;
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
  state.armHeld=false;
  held.clear();
  releaseStick();
  ocean?.clearKeys();
});
document.addEventListener("visibilitychange", () => {
  if (document.hidden) {
    state.armHeld=false;
    quietWhale();
    score?.stop();
    held.clear();
    releaseStick();
    ocean?.clearKeys();
  }
});
function showFleet(open) {
  $("fleet-picker").hidden=!open;
  $("missions").hidden=open;
  if (open) picker.querySelector("input:checked").focus();
}
$("back-menu").onclick=()=>{showFleet(false);document.querySelector('[data-story="route"]').focus()};
$("sail").onclick=()=>start("route");
buttons.forEach(b => b.onclick=()=>b.dataset.story==="route" ? showFleet(true) : start(b.dataset.story));
refreshMenu();

if (location.hash === "#dive") start("roam");
if (location.hash === "#atlas") start("observe");

if (location.hash === "#rescue") showFleet(true);
