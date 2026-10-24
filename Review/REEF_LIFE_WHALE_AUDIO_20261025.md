# Reef fish, grounding, whale navigation and audio controls

Source: isolated `underwater-ui-preview-20261020` repository, based on `253d696`.
Branch: `feat/whale-finder-audio-20261024`. Original developer project untouched.
Combined build: `Build/WebGLReefLife`; earlier builds remain separate.

## Changes

- Menu: live whale direction/distance guide, without teleporting or resetting the
  active mission. Cancelling restores the mission objective. Entering whale
  navigation stops automatic camera touring at the current position.
- Whale: gradual return steering keeps the animal in its open-water encounter
  area, rather than letting it travel indefinitely away from the reef.
- Separate voice/background switches; subtitles remain separate. Voice-off stops
  narration immediately and clears queued clips; re-enabling does not replay old
  narration. Background covers ambience, movement sound and distant whale calls.
  Existing M shortcut switches both channels together. Choices use PlayerPrefs.
- Four more blue tangs and four more clownfish: nine and eight respectively;
  existing 350 schooling fish, compact reef layout, water/lighting and rescue
  assets remain intact.
- Blue tang nose is +Z and tail -Z; imported clownfish nose is -X and tail +X.
  Previous shared -Z movement assumption made the tang swim backwards and the
  clownfish sideways. The previous blue-tang deformation also targeted its head.
- Nose-forward, nearly level turns and bounded continuous routes. Speed no longer
  collapses for small swimScale values. A four-joint rear-body/tail skin replaces
  per-frame C# vertex deformation; all 17 detailed fish share two generated skins,
  preserving original vertex counts, UVs, textures and materials.
- Ground scenery using actual low root vertices over the unchanged sampled sand,
  with shallow burial, rather than a single bounding-box minimum and five terrain
  samples. No generated sand mounds; reef X/Z centres and sizes are unchanged.

## Verification

- `Logs/whale-audio-20261024/gameplay.log`: full gameplay passed for the initial
  whale/audio candidate. `WebGLWhaleAudio` built successfully, 133,494,350 bytes.
- Whale production SwimStep simulated for 600 seconds at dt=1/30 and 0.1:
  maximum horizontal radius 39.62/39.60 m, travelled 1,673.6 m; not stationary.
- `Logs/reef-life-20261025/before.log`: expected failing reproduction; small
  tang/clownfish averaged 0.411/0.380 m/s, with incorrect clownfish head alignment.
- `after.log` and `gameplay-2.log`: both species, three swim scales, dt=1/60 and
  0.1, after 12 s warmup and 30 s measurement: mean 0.874–0.875 m/s, peak <=0.951,
  nose/travel alignment >=0.999. Reference school probe mean is 0.969 m/s.
  Pure vertical input cannot tip the fish head vertically.
- First skin integration test caught incompatible renderers on one GameObject;
  corrected by putting the swimming renderer on its own child object.
- `gameplay-2.log`: full suite passed: six-direction movement, terrain and arch,
  continuous mission routes, two missions/four roles/replay, net cut/release,
  independent audio channels and queued speech cleanup, live moving whale target,
  17 textured detailed fish/two shared skins, stable heads with moving tails,
  15 grounded scenery colonies, original 350 school fish, 22 enabled large animals.
- Native production renders: entry/mid/exit, blue tang, clownfish and coral roots
  under `Logs/reef-life-20261025/visual`. The first long capture exited early and
  hit an FMOD/TLS shutdown assertion. A separate three-shot retry in
  `visual-fish-roots.log` completed with its explicit visual-pass marker and exit 0.
  These use diagnostic cameras; they are not manual gameplay or FPS evidence.
- Font check passed: 401 UI characters; all 28,601 original mappings, outlines and
  advances retained. Server tests: 4/4; lossless delivery tests: 3/3.
- Isolated browser checked the initial whale/audio candidate at 856x408: menu,
  independent off labels, saved choices after waiting for asynchronous storage
  before reload, live whale marker and an actual visible whale. No captured JS
  exception, console.error or failed request. Saving immediately before page
  navigation can race the browser's asynchronous Unity storage flush.

## Limits and publication

- Final combined build passed with explicit ready/successful-exit markers:
  `Logs/reef-life-20261025/build.log`, 133,499,648 bytes. Preview on port 8904.
- Combined browser actual inputs: entered adventure as turtle, saw the detailed
  fish and compact coral reef, switched both audio controls off, enabled whale
  guidance during adventure, then cancelled it. Screenshots 03–06 in
  `Logs/reef-life-20261025/browser` show the whale's live distance and the unchanged
  adventure 1/3 objective restored afterwards. No captured JS exception or
  console.error; core build requests succeeded and gzip/Wasm headers were checked.
- The first browser navigation raced server startup and recorded a connection
  refusal; a subsequent navigation loaded the world. A later Fetch timeout was
  also captured. Read-only resource timings show a roughly 15.5-second Unity
  cloud telemetry configuration request, distinct from the successful local
  build requests; this is not evidence of a clean external network.
- Prior compact/simple-knife HTML hashes are unchanged. Reverted only this run's
  generated scene IDs and pipeline version metadata; removed its temporary
  BillingMode files. The native crash dump is retained under ignored Logs.
- Headless audio routing/mute checks do not constitute an audible listening test.
  Integrated-graphics 1080p/60 fps has not been measured on actual target hardware.
- Existing HTTP Unity Cache API, insecure-origin and unsupported debug/cookie
  format warnings remain; no claim that all browser warnings are resolved.
- External `traex openrouter-4o` review was attempted twice; model metadata was
  unavailable (code -32600). Local review and the actual tests above are the
  available evidence, not an external review pass.
- GitHub preparation confirmed the user's public empty repository and SSH push
  access. Assets/configuration are tracked, caches/logs excluded, no oversized
  GitHub blobs or credential-scan hits. Preserve existing font/texture licenses;
  model/dialogue redistribution permissions are not established by this snapshot.
  No blanket open-source license is assigned. Existing commit author emails and
  dated review notes' local paths/internal preview links remain in history.
