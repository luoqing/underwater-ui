import lost from "./assets/voices/lost.m4a";
import rhythm from "./assets/voices/rhythm.m4a";
import trust from "./assets/voices/trust.m4a";
import mother from "./assets/voices/mother.m4a";
import cage from "./assets/voices/cage.m4a";
import space from "./assets/voices/space.m4a";
import clear from "./assets/voices/clear.m4a";
const urls = { lost, rhythm, trust, mother, cage, space, clear };
const buffers = new Map();
let active,
  revision = 0;
export function stopVoice() {
  revision++;
  if (active) {
    active.stop();
    active = null;
  }
}
export async function playVoice(context, id, onStart, onFinish) {
  stopVoice();
  if (!context || context.state !== "running") { onFinish(false); return; }
  const ticket = revision;
  try {
    if (!buffers.has(id))
      buffers.set(
        id,
        fetch(urls[id])
          .then((r) => r.arrayBuffer())
          .then((bytes) => context.decodeAudioData(bytes)),
      );
    const buffer = await buffers.get(id);
    if (ticket !== revision) return;
    const source = context.createBufferSource();
    source.buffer = buffer;
    source.connect(context.destination);
    active = source;
    source.onended = () => {
      source.disconnect();
      if (active === source) { active = null; onFinish(true); }
    };
    onStart(context.currentTime);
    source.start();
  } catch {
    buffers.delete(id);
    if (ticket === revision) onFinish(false);
  }
}
