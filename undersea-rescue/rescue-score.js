// The falling four-note phrase returns in different registers across the voyage.
const cues = {
  reef:    { beat: 0.95, bass: [50, 46, 53, 48], notes: [69, null, 67, 65, null, 62, null, null] },
  escort:  { beat: 0.49, bass: [50, 48, 46, 45], notes: [62, 69, null, 67, 65, null, 62, 57] },
  reunion: { beat: 0.82, bass: [53, 50, 46, 48], notes: [69, 72, null, 70, 69, 65, null, 67] },
  cage:    { beat: 0.68, bass: [38, 41, 43, 45], notes: [57, null, 55, null, 53, 52, null, null] },
  lift:    { beat: 0.46, bass: [38, 43, 45, 48], notes: [57, null, 62, 64, 65, 64, 67, null] },
  safe:    { beat: 0.91, bass: [53, 46, 48, 53], notes: [72, null, 70, 69, null, 65, null, null] },
};
function sceneFor(s) {
  if (s.step === "listen" || s.step === "tune") return null;
  if (s.line === "clear" || s.step === "escape" || s.step === "done") return "safe";
  if (s.leg === "mother" || s.line === "mother") return "reunion";
  if (s.step === "lift") return "lift";
  if (s.story === "manta" && (s.route || !s.roaming)) return "cage";
  if (s.escorting || s.step === "cross" || s.step === "swim") return "escort";
  return "reef";
}
export function createScore(ctx) {
  let current, bus, step = 0, next = 0, level;
  const timbre = ctx.createPeriodicWave(new Float32Array(6), new Float32Array([0, 1, .27, .12, .038, .013]));
  function stop() {
    if (bus) {
      const old = bus;
      old.gain.cancelScheduledValues(ctx.currentTime);
      old.gain.setTargetAtTime(0, ctx.currentTime, .08);
      setTimeout(() => old.disconnect(), 550);
    }
    bus = null;
    current = null;
  }
  function note(midi, at, duration, volume, attack) {
    const osc = ctx.createOscillator(), envelope = ctx.createGain();
    osc.setPeriodicWave(timbre);
    osc.frequency.value = 440 * 2 ** ((midi - 69) / 12);
    envelope.gain.setValueAtTime(.0001, at);
    envelope.gain.exponentialRampToValueAtTime(volume, at + attack);
    envelope.gain.exponentialRampToValueAtTime(.0001, at + duration);
    osc.connect(envelope).connect(bus);
    osc.onended = () => { osc.disconnect(); envelope.disconnect(); };
    osc.start(at); osc.stop(at + duration + .02);
  }
  function update(s) {
    if (ctx.state !== "running") return;
    const cue = sceneFor(s), now = ctx.currentTime;
    if (cue !== current) {
      stop();
      if (!cue) return;
      current = cue;
      bus = ctx.createGain(); bus.gain.value = .001; bus.connect(ctx.destination);
      step = 0; next = now + .04; level = null;
    }
    if (!bus) return;
    const target = s.step === "talk" ? .21 : .85;
    if (target !== level) { bus.gain.setTargetAtTime(target, now, .14); level = target; }
    const { beat, bass, notes } = cues[cue];
    next = Math.max(now + .01, next);
    while (next < now + .17) {
      const n = step % 8, root = bass[Math.floor(step / 8) % bass.length];
      if (n === 0 || n === 4) {
        note(root + (n === 4 ? 7 : 0), next, beat * 4.4, .105, beat * .5);
      }
      if (notes[n] !== null) {
        note(notes[n], next, beat * 2.05, .076, .035);
        note(notes[n] + 12, next + .14, beat * 1.35, .014, .065);
      }
      next += beat; step++;
    }
  }
  return { update, stop };
}
