# Net rescue and reef distribution preview

Branch: `feat/net-cut-rescue-20261020`, based on `9a05135`.
First candidate: `Build/WebGLNetRescue`; corrected candidate:
`Build/WebGLNetRescueR2`. Preview port 8899 switches only after R2 completes.
Original developer project and earlier builds/ports 8896–8898 remain separate.

## Changes

- Reference-first nylon fishing net: selected concept and exact built-in image
  prompt are saved under `Review/Concepts/`. Concept art is not game evidence.
- Three real diamond tube meshes, compact junctions, irregular broken edges,
  fitted once against the animated turtle mesh. Shared muted gray-green
  translucent, non-emissive filament material. No Cloth or per-frame mesh fit.
- Guarded rescue cutter; selecting/equipping does not cut. Hold the current
  marker and move at least 22 pixels. One press consumes at most one cut;
  range, mission stage, menu and ordered-target gates remain active.
- Cut ends separate, released pieces lift away, drift and fade. Reset also
  removes pieces already detached from the animal. Existing ascent follows.
- Reef patches move toward the banks and are staggered longitudinally, leaving
  the central swimming lane open. Twelve reef/coral patches become fourteen;
  only two small existing-asset accent colonies are added.
- Natural brown/green reef base with red/orange, purple/blue and gold accents.
  Reference: [Reef Authority coral overview](https://www.gbrmpa.gov.au/learn/coral).
- Schools: 188 fish in four groups become 250 in seven groups (+32.98%),
  distributed by depth and side bay. Four shared meshes and the existing
  material palette are reused; no per-fish imported assets or texture changes.
- Nine blue tang/clownfish spawn locations are spread out. Their movement code,
  large animals, mission anchors, water grading and texture bytes are unchanged.

## Validation record

- Server tests: 4/4 passed; browser helper syntax passed.
- UI font check: 393 required characters; original CJK mappings unchanged.
- Synthetic net: 40,830 triangles, valid indices/normals/tangents and outward
  winding. The test now explicitly normalizes tiny triangle cross products;
  Unity's general-purpose `.normalized` otherwise zeros valid small faces.
- First production regression found a real locally folded tube in shell patch
  3: rays cast from the whole-animal bounding box hit raised flippers instead
  of the shell. Measure the rear/central shell width first, then restrict the
  casting radius to the shell neighbourhood. No triangle-flipping, smoothing
  workaround or weaker assertion is used. Final production sample hits are
  1539/1539 and all 40,830 triangles pass in all four roles and rescue replay.
- Revised reef layout: all 12 role/beacon routes passed with real collision
  solver steps of 0.25 m. This is automated traversal, not manual gameplay.
- External reviewer could not start: `openrouter-4o` metadata unavailable.
- `MissionGameplayValidation.RunAll` exit 0 in
  `Logs/net-rescue-20261020/gameplay-3.log`: both missions × four roles and
  rescue replay, equipped/near/ordered gates, production 22-pixel gesture,
  short/released/cancelled/UI presses, final-cut hold consumption, actual
  Update-driven drift/fade and cleanup, and ongoing animal movement passed.
- Live school checks: 250 fish, seven distinct school centres, exactly four
  shared meshes and 25 instancing-enabled shared materials. This checks asset
  reuse, not actual GPU draw calls or frame rate.
- `ClearWaterVisualValidation.Run` exit 0 in
  `Logs/net-rescue-20261020/visual-final.log`: six production Play Mode renders,
  including a still-active released net. Main-agent image inspection verified
  that the flipper-induced spikes are gone and the revised reef layout keeps
  foreground structure. These are diagnostic camera captures, not manual play.

## Additional real-input findings and corrections

- R1 WebGL build succeeded, 133,484,343 bytes; real browser reached the mission
  menu with loader at 100%, no JS exception/network failure. Initial build
  attempts were interrupted or waited during editor startup; final successful
  evidence is `Logs/net-rescue-20261020/build-3.log`, not an inferred repair to
  Unity startup, licensing, or the user's earlier Bad Request message.
- R1 adventure was completed through normal keyboard/mouse input, all three
  objectives updated. Raw recording is `browser-adventure/adventure-turtle-full.mp4`
  (1028.178 s, 791 captured frames). `adventure-steps.mp4` is 84.04 s with source
  intervals `[0,7], [79,89], [149,158], [325,343], [546,555], [721,733],
  [875,886], [979,987]`. Hard cuts only, no acceleration or interpolated frames.
  This is R1 footage; reef layout and adventure route remain the same in R2.
- The third lantern was hard to distinguish from the frontal view before
  completion. Sustained forward input continued after completion into a dark
  close view, and backing away restored the scene. This does not by itself
  prove collision penetration. Do not claim perfect lantern sightlines.
- R1 rescue reached 3/3 actual cut strokes and selected the freed turtle, but
  a real 3 m upward drag/release made it swim down and out of view. Failure
  evidence is retained in `browser/rescue-net-full.mp4` and
  `17-close-drag-regression.png`; that recording is not a passed rescue run.
- R2 preserves the actual close camera-ray depth (rather than forcing 6 m),
  bounds pointer integration to avoid endpoint overshoot, and preserves the
  last valid guidance velocity at a stationary endpoint. No rescue-specific
  forced upward velocity: upward and downward intent are both tested.
- R2 adds a small precise cut-point ring. Both gesture begin and director use
  the same 4.5 m animal-reach gate; rays may extend across the far side of the
  shell, so a reachable animal is not rejected just because its far strand is
  slightly beyond 4.5 m. Far animals still cannot start or complete a cut.
- A later animation phase exposed an inner fin-root sample even after radius
  limiting. R2 therefore strips triangles influenced by the imported turtle's
  FrontFlipper bones from the temporary baked sampling mesh, compacts its
  vertices and recomputes bounds. The rendered animal mesh/skin is unchanged.
- `gameplay-r2-body.log` passed both missions × four roles and replay, 12/12
  collision routes, all 1539/1539 shell samples and 40,830 net triangles per
  case, far-animal/far-side-strand reach checks, and real camera-pointer
  up/down/endpoint/release tests at 0.2/0.33 s timesteps. Temporary geometry
  diagnostic logging was removed after the sampler regression passed.

The final build/browser records follow. No physical integrated-GPU 1080p/60 fps
result is claimed.

## Final R2 delivery

- Source checkpoint: `786deb2` on `feat/net-cut-rescue-20261020`. No merge or
  push to the original developer project.
- `build-r2.log`: `UNDERWATER_WEBGL_READY`, 133,492,973 bytes, then
  `Exiting batchmode successfully now!`.
- `visual-r2.log` emitted the six-render pass marker. The launcher returned
  143 after captures; do not describe it as an exit-0 render run. Actual final
  PNGs were inspected, including fitted net close-up and active release frame.
  Known editor Search index exception remains separate from browser runtime.
- R2 real-input rescue via Whale role passed: `browser-r2/05-first-cut.png`,
  `06-second-cut.png`, `07-net-freed.png`, `08-selected.png`,
  `09-ascent-outcome.png`. The last image shows the actual rescue-complete
  objective after the near drag/release that failed in R1. No game-state
  injection, object teleport or simulation speed changes were used.
- Raw R2 capture: `browser-r2/rescue-r2-full.mp4`, recording duration 399.413 s,
  753 actual frames. Steps version: 94.04 s, 6,130,224 bytes, H.264 856×408;
  source intervals `[0,6], [63,72], [115,130], [175,190], [236,257], [299,327]`.
  Original frame PTS/durations retained, hard cuts only. Full decode passed.
- Main preview port 8899 now serves R2. Original R1 Build assets are retained
  alongside the new manifest for clients that started loading R1 before the
  switch; the earlier R1 output directory itself is unchanged. Port 8900 also
  serves this corrected candidate used for its real rescue recording.
- Final 8899 entry was loaded again at 1920×1080 and reached the live world:
  `browser-r2/10-final-reef-1080.png`, no JS exception/console.error/network
  failure in the captured run. The real-input captures also retain Unity log
  messages that its persistent Cache API is unavailable on this HTTP origin,
  plus the known insecure-origin webcam warning. Do not call persistent cache
  functional or claim repeated/first-load performance solved by this change.
- Original 8896/8897/8898 index hashes remain unchanged:
  `22b53a36b8b98dad2c6afa36d4803aca496e253a9aa8ccd0a049ec0022226b83`,
  `46ecb5404cdaf1d4453474ab56e481ef5589f578c6017d0720298a5ad88de901`,
  `443f329f1d1b2e3df8ee8aa23284f13a1db6dad3182cdf539b1a6d9df108e90c`.
- Final gallery browser check: both step videos reached readyState 4 with no
  media error. A real click on the rescue video's native play control advanced
  playback to 2.96 s. Two canceled media preload requests were recorded as
  ERR_ABORTED before playback; neither video reported a decoding error.

This is a palette/distribution and rescue-interaction preview, not a declaration
that photorealism, seafloor repetition, third-lantern visibility, loading speed
or the physical integrated-GPU 60-fps target has all been completed.
