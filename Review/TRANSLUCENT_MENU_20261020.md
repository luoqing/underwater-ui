# Translucent mission menu preview

Branch `visual/translucent-menu-20261020`, based on `a2a1e6d`.

Only the three mission-menu background alpha values changed at runtime:
panel 0.92 → 0.58, button 0.96 → 0.42, hover/pressed 0.98 → 0.62.
Text, layout, water grading, animals, shaders, textures and input logic are unchanged.

Build output: `Build/WebGLGlassMenu`, preview port 8898. Ports 8896/8897
and their existing build directories were not overwritten.

Verification:

- `MissionGameplayValidation.RunAll` exit 0, including the new texture-alpha
  assertions, both missions × four roles and rescue replay.
  Log: `Logs/glass-menu-20261020/gameplay.log`.
- WebGL build exit 0, 133,486,330 bytes.
  Log: `Logs/glass-menu-20261020/build.log`.
- Actual isolated browser entered the mission menu, chose Adventure, showed
  the role menu, selected Turtle and entered the mission through real clicks.
  Screenshots `01-glass-mission-menu.png`, `02-glass-role-menu.png` and
  `03-adventure-started.png` in `Logs/glass-menu-20261020/browser/`.
  All three captured `errors=[]`; menu text remains legible over the scene.
- Both the old 8897 page and new 8898 page return HTTP 200. New WASM returns
  the correct application/wasm type and gzip encoding.
- Old 8897 index/WASM SHA-256 remained unchanged:
  `46ecb5404cdaf1d4453474ab56e481ef5589f578c6017d0720298a5ad88de901`,
  `39d5e4b13229a37894545626a86f6a4810eb24ac53ae1e39768e3b523b3eb766`.
- `git diff --check` passed. The external reviewer could not start because
  `openrouter-4o` model metadata was unavailable; no external review pass claimed.

The user's JSON `{"detail":"Bad Request"}` was not reproduced by the game
page or this build. Its originating window/URL is still unconfirmed, so it
has not been classified as a fixed game bug.
