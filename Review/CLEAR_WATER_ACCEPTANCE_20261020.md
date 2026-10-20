# Clear-water and nylon rescue preview

2026-10-20, isolated branch `visual/clear-water-preview-20261020`, based on
`757fd3c`. Original project and port 8896 build were not overwritten.

## Scope

- Lighter cyan-blue depth gradient, warmer muted reef/sand colors, narrower
  sun-aligned shafts and less bloom/grain/chromatic aberration. Depth of field
  is disabled only in the new look to retain distant detail.
- `look=original&water=layered` retains the previous treatment in the same
  build. This compares grading, not old versus new rescue geometry.
- Fishing-line wraps and knots share one non-emissive translucent nylon
  material. Knots are round, capped, open overhand tube meshes with loose ends,
  not brown braided rope or glowing ribbons. Their filament diameter matches
  the wraps; three knots together have 6,912 triangles.
- Shell fitting bakes once, compensates renderer scale and samples real mesh
  surfaces. Three wraps remain on the rigid torso, avoiding flipper spikes.
  Temporary sampling colliders/meshes are removed; no per-frame fitting remains.
- Rescue uses one click to select the freed turtle. It keeps paddling nearby
  while the next instruction is read. Dragging begins only on the selected
  animal and after real pointer movement; knots retain input priority.
- Release restores drift once, preserving the final speed/heading. High ascent
  shows a release instruction so the turtle can continue to the surface.
- Existing split compact controls remain: movement left, ascent/descent right.

## Automated and render checks

- `MissionGameplayValidation.RunAll`: exit 0 in
  `Logs/clear-water-20261020/rope/nylon-gameplay.log`.
  Both missions × four roles, immediate abandoned-rescue replay, completed
  rescue replay, ordered knots, one-click selection, ascent hints, collision
  checks and 14 enabled moving animals passed. These fixtures manipulate game
  objects and do not replace real-input browser acceptance.
- `RescueRopeValidation`: finite unit normals, orthogonal tangents, valid indices,
  outward winding, shared translucent material, matched diameter, generous
  click colliders, 288/288 shell samples and no active temporary colliders.
  Path-length/spike and scale checks guard the observed fitting regressions.
- `ClearWaterVisualValidation.Run`: exit 0 and five actual Play Mode renders in
  `Logs/clear-water-20261020/nylon-visual.log`. Images use diagnostic camera and
  mission setup, not manual gameplay. Final PNGs are in the adjacent `visual/`.
- `UnderwaterBuild.BuildWebGL`: exit 0 in
  `Logs/clear-water-20261020/build-nylon.log`, 133,486,809 bytes.
- HTTP 200 for the page and WASM, correct gzip/content-type headers; four
  server unit tests and browser-helper syntax check passed.
- Font check: 395 UI characters; 28,601 original CJK mappings/outlines/advances
  unchanged. Animal/model/texture source bytes are unchanged.
- Original 8896 index SHA-256 remains
  `22b53a36b8b98dad2c6afa36d4803aca496e253a9aa8ccd0a049ec0022226b83`.

## Real-input browser acceptance

- Port 8897 reached the real mission menu, not just a hidden loading bar.
  Rescue was completed through normal mouse/keyboard input: mission choice,
  Whale role, approach from 9 m to 2 m, three ordered knots, one-click turtle
  selection, step back for framing, upward drag and release. No injected game
  state or teleportation was used.
- `browser-final/05-first-knot-near.png`, `06-second-knot-near.png`,
  `07-three-knots-untied.png`, `09-turtle-selected.png` and
  `11-surface-outcome.png` show each stage, including the actual completion.
- Raw recording: `browser-final/rescue-whale-full.mp4`, 418 seconds, 839 actual
  captured frames. Edited step video: `Build/WebGLClearWater/review/gameplay-steps.mp4`,
  H.264, 856×408, 102.92 seconds, 5,485,543 bytes. Only positioning/idle gaps
  were cut; active action segments retain real timestamps, without interpolation
  or playback acceleration. Source intervals in seconds:
  `[0,8], [47.9,55.5], [124.8,134.5], [171.8,181.5], [207,216],
  [247.5,257.5], [297.8,306], [328.5,336.5], [366.7,400]`.
- Normal free-roam navigation was checked at 1920×1080 in both new and original
  looks: `browser-final/12-clear-water-1080.png` and `13-original-water-1080.png`.
  Animals remained animated; these are not frozen pixel-identical comparisons.
- Two preflight/fetch connection timeouts and existing Unity compatibility
  warnings appeared during recording. Build resources loaded and the rescue
  completed; this does not claim a completely clean console.
- Final review page was checked in an isolated browser: H.264 video metadata
  reports 102.92 seconds and `readyState=4`, subtitle track `readyState=2` and
  `mode=showing`. A real click started playback (`currentTime>2`, no media
  error). One canceled media preload request was recorded during playback;
  decoding continued. All referenced review images/video/subtitles return 200.
- The thin line needs a close view to inspect. Current game text does not yet
  explicitly explain approaching before clicking or stepping back for framing;
  the step video includes both operations. This remains a guidance-polish item.

## Limits

This is a visual preview awaiting user acceptance, not a claim of photographic
realism. Lantern renders show the existing cage/core model; its full material
rework is not included. Browser recording uses software rendering and cannot
certify integrated-GPU 1080p/60 FPS. No such hardware benchmark was performed.

The external `openrouter-4o` reviewer could not start because its model metadata
was unavailable. Local static review, a second read-only agent review, Unity
tests and actual renders were used instead; external review is not marked passed.
