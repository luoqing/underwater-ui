import recording from "./assets/voices/glacier-call.m4a";

let decoded, source, revision = 0;
export function whaleCalling() {
  return Boolean(source);
}
export function quietWhale() {
  revision++;
  if (source) source.stop();
  source = null;
}
export async function whaleCall(ctx, pan = 0, distance = 12) {
  quietWhale();
  if (ctx?.state !== "running") return;
  const ticket = revision;
  try {
    decoded ??= fetch(recording).then(r => {
      if (!r.ok) throw Error("Whale recording unavailable");
      return r.arrayBuffer();
    }).then(data => ctx.decodeAudioData(data));
    const buffer = await decoded;
    if (ticket !== revision || ctx.state !== "running") return;
    const clip = ctx.createBufferSource(), gain = ctx.createGain(), side = ctx.createStereoPanner();
    side.pan.value = Math.max(-.75, Math.min(.75, pan));
    gain.gain.value = .88 / (1 + distance / 40);
    clip.buffer = buffer;
    clip.connect(gain).connect(side).connect(ctx.destination);
    clip.onended = () => {
      if (source === clip) source = null;
      clip.disconnect(); gain.disconnect(); side.disconnect();
    };
    source = clip;
    clip.start();
  } catch { decoded = null; }
}
