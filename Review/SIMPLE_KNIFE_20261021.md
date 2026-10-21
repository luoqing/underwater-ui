# Simple knife preview · 2026-10-21

Branch: `visual/simple-knife-20261021`, based on `5e90282`.
Separate output: `Build/WebGLSimpleKnife`. Previous 8899/8900 outputs and the
original developer project are not modified.

## Reference and changes

- Built-in image generation was used first, then the real Unity geometry was
  remodeled. Reference: `Concepts/simple-knife-reference-20261021.png`;
  exact final prompt and mode: `Concepts/simple-knife-reference-20261021.md`.
  The generated image is a concept, not an in-game render or runtime texture.
- Plain charcoal rounded handle, two small rivets per side and a single satin
  silver blade, matching the simple emoji-like silhouette. Orange guard,
  safety hook, block spine and protruding grip ridges are removed.
- Blade and handle have actual thickness, closed solids and beveled edges.
  Four pins reuse one low-poly mesh. The whole tool has 520 triangles, three
  distinct meshes, two materials and no new textures or runtime colliders.
- Idle scale is 0.86, positioned farther/lower at camera-local
  `(0.29, -0.22, 0.76)` instead of `(0.27, -0.19, 0.68)`.
- The side cutting edge, not an arbitrary root offset, is aligned to the
  world-space net target. Direction changes retain exact contact. A tilted
  cutting pose lifts the handle toward the viewer above the curved shell.
- Net gestures, reach limits, mission progression, water, animals, coral and
  texture bytes are unchanged.

## Verification record

- `Logs/simple-knife-20261021/gameplay.log`: first full RunAll exit 0, both
  missions × four roles plus replay. Geometry validation confirms outward
  nondegenerate closed knife solids, 520 triangles and shared resources;
  held viewport bounds and contact invariance in four stroke directions pass.
  This first run predates the additional handle-lift pose adjustment.
- `visual.log`: initial extended capture failed after six completed renders:
  the fixture read an old, pending-destruction FishingLineRescue on replay.
  The fixture now reads the director's actual current component instead.
- `visual-2.log`: exit 0, eight production Play Mode renders. Inspected held
  knife and contact pose; the latter exposed partial handle occlusion by the
  shell. The cutting pose was subsequently tilted and contact moved to the
  curved forward edge. These are diagnostic camera/mission captures, not
  manual browser gameplay.
- Four web-server unit tests pass. `git diff --check` passes.
- External Traex review was attempted. The installed CLI rejects the skill's
  `--uncommitted -` combination; retry without a custom prompt reached the
  backend but failed because `openrouter-4o` metadata could not be resolved.
  External review has not passed; local geometry, mission and visual checks
  are the available evidence.

## Final pose checks

- `gameplay-2.log`: full RunAll exit 0 after the pose adjustment. Both missions
  × all four roles, replay and all original net-gesture checks pass again.
  Knife checks additionally require its handle to be lifted at least 7 cm
  toward the camera relative to the contact edge in four stroke directions.
- `build.log`: the first candidate built successfully (133,487,918 bytes), but
  the pose was adjusted during that build. It is not the validated final
  source snapshot and was not published for acceptance.
- `visual-3.log`: RunKnife exit 0. The final held and tilted contact renders
  were inspected; the knife handle is visible above the shell rather than
  disappearing into it in the diagnostic view. Final PNGs are in
  `Logs/simple-knife-20261021/visual/knife-held.png` and
  `knife-cut-contact.png`.

## Final build and preview

- `build-2.log`: final frozen-source candidate built successfully,
  `UNDERWATER_WEBGL_READY`, 133,488,871 bytes, followed by
  `Exiting batchmode successfully now!`. The launcher's process returned 143
  while the Unity child was still alive and making build progress. The child
  was monitored to the success marker; no exit-0 launcher claim is made.
- New separate preview: port 8901, tmux `underwater-simple-knife-8901`.
  `http://10.37.216.166:8901/?water=layered&mission=rescue&v=simple-knife-20261021`
- Browser loaded the final build, entered the real rescue and equipped the
  new tool. `browser/02-equipped.png` shows its small black-handle/silver-blade
  silhouette with the actual HUD. The initial `01-roles.png` was still Unity's
  splash: loader readiness alone is not the playable-world check.
- Actual mouse strokes cut all three net panels in order:
  `browser/03-first-cut.png`, `04-second-cut.png`, `05-net-freed.png`. The last
  image shows the net gone, the knife hidden and the real objective switched
  to bringing the turtle to the surface. No game-state injection was used.
- Then a real click, upward drag and release completed the rescue:
  `browser/06-rescue-outcome.png` shows “豆豆换好气了，救援完成！”. Captured
  browser screenshots reported no JS exception/console.error/network failure.
- Silent real-input recording: `browser/simple-knife-rescue.mp4`, 164.491 s,
  297 actual captured frames, original timing retained (including waiting),
  no acceleration or interpolation. Full ffmpeg decode passed. This software
  browser recording is not a hardware frame-rate measurement.
- Unity-generated scene IDs/settings-version noise was restored; temporary
  generated billing/test resource files were removed from source (not user
  assets). Imported animal/coral model, texture and font bytes are unchanged;
  mission progression, camera controls and animal-motion source are unchanged.

No physical integrated-GPU frame-rate result or first-load improvement is
claimed. Known HTTP-origin persistent Cache API and webcam warnings remain;
they are not addressed by this knife-only preview.
