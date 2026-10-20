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

Actual render, build, browser and performance results must be recorded after
verification. No physical integrated-GPU 1080p/60 fps result is claimed.
