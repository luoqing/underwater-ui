import reef from "./assets/score/reef.m4a";
import escort from "./assets/score/escort.m4a";
import reunion from "./assets/score/reunion.m4a";
import cage from "./assets/score/cage.m4a";
import lift from "./assets/score/lift.m4a";
import safe from "./assets/score/safe.m4a";

const recordings = { reef, escort, reunion, cage, lift, safe };

function chapter(s) {
  if (s.leg === "song" || s.step === "still") return null;
  if (s.step === "listen" || s.step === "tune") return null;
  if (s.line === "clear" || s.step === "done") return "safe";
  if (s.step === "escape") return s.escape < .66 ? "lift" : "safe";
  if (s.step === "lift") return "lift";
  if (s.story === "manta" && (s.route || !s.roaming)) return "cage";
  if (s.leg === "mother" || s.line === "mother" || s.line === "knock") return "reunion";
  if (s.escorting || s.step === "cross" || s.step === "swim") return "escort";
  return "reef";
}

export function createScore(ctx) {
  const cache = new Map();
  let wanted, current, change = 0, level, nextCall = Infinity, signalBus;
  function get(name) {
    if (!cache.has(name)) {
      cache.set(name, fetch(recordings[name]).then(response => {
        if (!response.ok) throw Error("Score load failed");
        return response.arrayBuffer();
      }).then(bytes => ctx.decodeAudioData(bytes)).catch(() => {
        cache.delete(name);
        return null;
      }));
    }
    return cache.get(name);
  }
  function fadeOut() {
    if (!current) return;
    const { source, gain } = current;
    const now = ctx.currentTime;
    if (gain.gain.cancelAndHoldAtTime) gain.gain.cancelAndHoldAtTime(now);
    else {
      const value = gain.gain.value;
      gain.gain.cancelScheduledValues(now);
      gain.gain.setValueAtTime(value, now);
    }
    gain.gain.setTargetAtTime(.0001, now, .16);
    source.stop(now + .85);
    current = null;
  }
  function stop() {
    change++;
    wanted = undefined;
    nextCall = Infinity;
    if (signalBus) { signalBus.disconnect(); signalBus = null; }
    fadeOut();
  }
  // In the story this is a wing knocking the hollow frame, audible on phone speakers.
  function knock(at, length, strength, pan) {
    if (!signalBus) {
      signalBus = ctx.createGain();
      signalBus.connect(ctx.destination);
    }
    const side = ctx.createStereoPanner();
    side.pan.value = pan;
    side.connect(signalBus);
    [260, 617, 1028].forEach((hz, i) => {
      const tone = ctx.createOscillator(), env = ctx.createGain();
      tone.frequency.setValueAtTime(hz, at);
      tone.frequency.exponentialRampToValueAtTime(hz * .95, at + length);
      env.gain.setValueAtTime(.00001, at);
      env.gain.exponentialRampToValueAtTime(strength / (1 + i * 2.2), at + .009);
      env.gain.exponentialRampToValueAtTime(.00001, at + length);
      tone.connect(env).connect(side);
      tone.onended = () => { tone.disconnect(); env.disconnect(); if (i === 2) side.disconnect(); };
      tone.start(at); tone.stop(at + length + .01);
    });
  }
  let retryAt = 0;
  function update(s, location, speaking) {
    if (ctx.state !== "running") return;
    const name = chapter(s), now = ctx.currentTime;
    if (name !== wanted && now >= retryAt) {
      const version = ++change;
      wanted = name;
      fadeOut();
      nextCall = name === "cage" && (s.leg === "ridge" || s.leg === "cage") ? now + .25 : Infinity;
      if (name) get(name).then(buffer => {
        if (version !== change) return;
        if (!buffer) { wanted = undefined; retryAt = ctx.currentTime + 3; return; }
        const source = ctx.createBufferSource(), gain = ctx.createGain();
        source.buffer = buffer;
        source.loop = true;
        source.connect(gain).connect(ctx.destination);
        source.onended = () => { source.disconnect(); gain.disconnect(); };
        gain.gain.value = .0001;
        source.start(ctx.currentTime + (s.leg === "cage" ? 1.35 : 0));
        current = { source, gain };
        level = undefined;
      });
    }
    if ((s.leg === "ridge" || s.leg === "cage") && now >= nextCall) {
      const proximity = 1 / (1 + (location?.range ?? 12) / 14);
      const pan = location?.signalPan ?? 0;
      for (const [delay, tail] of [[0,.14],[.28,.14],[.67,.51]]) {
        knock(now + .025 + delay, tail, .08 + .15 * proximity, pan);
      }
      nextCall = now + 6.8;
    }
    if (!current) return;
    const target = speaking ? .19 : name === "cage" ? .55 : .71;
    if (target !== level) {
      current.gain.gain.setTargetAtTime(target, now, speaking ? .09 : .48);
      level = target;
    }
  }
  function signal() {
    const now = ctx.currentTime;
    for (const [delay, tail] of [[0,.14],[.28,.14],[.67,.51]])
      knock(now + .025 + delay, tail, .14, .38);
  }
  return { update, stop, signal };
}
