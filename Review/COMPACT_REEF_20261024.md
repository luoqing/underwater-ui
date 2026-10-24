# Compact lively reef preview · 2026-10-24

Branch: `visual/compact-lively-reef-20261024`, based on `b8ce497`.
New output: `Build/WebGLCompactReef`. Previous previews and original developer
project are preserved. This is a separate candidate, not an approved merge.

## User-selected reference

The user superseded the earlier spread-out direction with the compact layout
at `http://10.37.216.166:8894/?water=layered&v=bugfix-20261020-r5`.

- Restore the original 12 shallow reef/colony positions, sizes and tints from
  `42c3a64` (r5 repair source), kelp placement/size and nine close-pass fish
  placements. Retain two later blue/orange mid-route coral accents.
- Preserve current clear-water grading, simple knife, real nylon net,
  six-direction controls, collision/open-arch repairs, fonts, compact HUD,
  progressive guidance and repaired creature motion. No imported model,
  texture or font files are altered.
- School fish: 250 → 350, seven schools, with 230 fish centered before z=60 m.
  Reuse the same four meshes and 25 instancing-enabled materials. No additional
  fish assets, textures or per-fish material copies.

## Fish movement cause and regression

Previously, nominal forward velocity opposed formation correction toward a
slowly moving, small-radius route. Increasing the nominal `speed` alone does
not fix the resulting hovering. The wider/faster but bounded school route now
supplies its actual derivative as forward velocity. Steering, collision,
individual speed variation, banking and tail animation are retained. This does
not accelerate game time or the turtle/clownfish/whale/manta scripts.

`FishSchoolMotionValidation` drives the production `SwimStep`, not a duplicated
simulation, in clear water after 12 s warmup for 18 s measured travel:

| Case | Mean speed | Peak speed | Maximum distance from center |
| --- | --- | --- | --- |
| Before, dt=1/60 | 0.381 m/s | 0.576 m/s | 4.287 m |
| After, dt=1/60 | 0.969 m/s | 1.323 m/s | 5.195 m |
| After, dt=1/30 | 0.969 m/s | 1.323 m/s | 5.195 m |
| After, dt=1/10 | 0.969 m/s | 1.323 m/s | 5.194 m |

`fish-before-2.log` fails the minimum sustained-motion assertion as expected;
`fish-after.log` exits 0. These are deterministic simulation measurements, not
live GPU frame-rate results or a claim that all 350 fish always swim at 0.969 m/s.

## Gameplay and render checks

- First full `gameplay.log` passed both missions/all roles and 12/12 traversable
  routes, then failed a newly added layout assertion. The assertion compared
  imported model pivots to intended visual centers, ignoring the normal
  grounding/recentering transform. The check now measures actual aggregate
  renderer bounds, including the r5 27 m and 30 m reef sizes.
- `gameplay-2.log`: full RunAll exit 0, both missions × four roles plus rescue
  replay, 12/12 continuous collision routes, actual imported arch openings,
  three net-cut gestures and release, knife geometry/contact, near turtle
  guidance, 350 fish resource budget and compact r5 reef bounds.
- `visual.log`: RunReef exit 0; three actual production Play Mode renders at
  entry, middle and exit. Inspected `visual/reef-entry.png`, `reef-mid.png`,
  `reef-exit.png`. Diagnostic camera setup, not manual browser traversal.
- External review attempted with Traex/openrouter-4o; backend could not resolve
  the model metadata. No external-review pass is claimed.

## Environment and reference recovery

- System root filesystem was full; project disk remained writable. Ego bridge
  reported ENOSPC even with a project temporary directory. Used the existing
  isolated local Chromium/CDP helper instead; no personal browser attached.
- A first local Chromium attempt failed because the project TMPDIR made the
  Unix socket path too long. Short private directory:
  `/data00/tmp/reef-1024-x9SGL0`.
- Archived only three verified-closed scratch browser profiles created by this
  task in earlier turns: `/tmp/underwater-video-qa-SkbcI1`, `14Faar`, `YXKtGO`,
  moved to corresponding `archived-*` directories below that private directory.
  No files were discarded; source/assets and previous builds were not deleted.
- First Unity attempt could not open a new X display and licensing mutex while
  root disk was full. Retried after scratch relocation using the existing Xvfb
  display :97 and project-disk temp storage; successful tests followed.
- Port 8894 initially had no listener. Recovered its preserved WebGLBugfix
  output from the performance workspace, served read-only with the existing
  WebGL server as `underwater-r5-reference-8894`; no archive files changed.
  Initial connection-refused screenshots are retained; the restored r5 loaded
  and was actually entered via normal role/menu clicks, with no captured JS
  exception/resource failure. Reference captures are under `before/`.
- Reference video `before/reference-r5-fish.mp4` has only four actual frames in
  15.699 s during concurrent work. It is not used as a smooth-motion or hardware
  performance comparison.

## Final WebGL candidate

- `build.log`: exit 0, `UNDERWATER_WEBGL_READY`, 133,489,743 bytes, followed by
  `Exiting batchmode successfully now!`. Only 872 bytes above the previous
  simple-knife build report (133,488,871 bytes); no new imported models,
  textures or fonts were added. This separate URL still needs its own download.
- Separate server: `underwater-compact-reef-8902`, port 8902. Entry:
  `http://10.37.216.166:8902/?water=layered&mission=adventure&v=compact-r5-20261024`.
  HTML and gzip WASM HEAD requests return 200 with appropriate content headers.
- Final build actually loaded in isolated Chromium and showed its role picker;
  `before/06-new-roles.png` retains a zero-error capture. Current HUD/transparent
  menu, not r5's oversized overlay, remains in the candidate.
- Real Turtle-role entry and forward input were checked:
  `before/07-new-entry.png` and `08-new-forward.png` show the restored compact
  geometry, current clean HUD and first-lantern distance decreasing 30 → 27 m.
  Both captures reported zero JS exception/console.error/network failure.
  This is not a claim of a full manual adventure completion on this build.
- `before/compact-reef-fish.mp4` contains only three actual frames in 15.283 s
  on this software-rendering browser, so it is not published as motion/FPS
  evidence. The previous r5 capture was similarly sparse (four frames).
  Sustained fish movement is verified by the production-step regression;
  smoothness and integrated-GPU performance still need physical-device checks.
- Known persistent Cache API/insecure-origin webcam Unity log warnings on the
  HTTP origin remain outside this layout/movement change; no clean-warning
  console or working persistent-browser-cache claim is made.
- Prior index hashes remain unchanged: simple knife
  `ebe7cce1edb342e2a2e0d26b4d172273822aa35b216338188fc97f987832dd4c`,
  net R2 `6440011a47371c9834c178b5542e2d915cfe56ff4c1f618efeb4f1ed70627a5f`;
  8896/8897/8898 match their previously recorded hashes as well.
- Unity-generated scene IDs, settings-version noise and billing/test resources
  were restored/removed from this task's source changes after the last run.

No physical integrated-GPU 1080p/60 fps result or loading-speed improvement is
claimed. Resource reuse/instancing flags are verified, not GPU draw-call counts.
