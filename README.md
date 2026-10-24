# The Blue Between Worlds

Unity 6.3 URP underwater exploration game for WebGL, with adventure and
turtle-rescue missions. This repository maintains an isolated development
snapshot; it does not automatically synchronize another developer's project.

## Open the project

Use **Unity 6000.3.26f1** with the **WebGL Build Support** module. Open this
repository's root in Unity Hub and let Package Manager restore the versions in
`Packages/packages-lock.json`. The required models, textures, fonts and recorded
dialogue are included under `Assets/Resources`; no asset-generation account is
needed to build this snapshot.

- `Assets/Scripts`: world generation, animal movement, controls and missions.
- `Assets/Shaders`: water, light beams, caustics and fish shading.
- `Assets/Resources`: runtime assets and their available license notices.
- `Assets/Editor`: scene creation, WebGL build and validation entry points.
- `Tools`: local preview server, delivery checks and asset-maintenance tools.
- `Review`: dated acceptance notes and known validation limitations.

Open `Assets/Scenes/UnderwaterJourney.unity` and press Play, or use
**Underwater > Create Benchmark Scene** to regenerate the minimal startup scene.

## Build

Run from the repository root, setting the editor path for your installation:

```bash
UNITY_EDITOR="/path/to/Unity/Editor/Unity"
mkdir -p Logs
"$UNITY_EDITOR" -batchmode -noaudio -quit \
  -projectPath "$PWD" \
  -executeMethod UnderwaterBuild.BuildWebGL \
  -logFile "$PWD/Logs/build.log"
```

The active preview target is `Build/WebGLReefLife`; the authoritative output
path is `BuildPath` in `Assets/Editor/UnderwaterBuild.cs`. Keep earlier preview
outputs intact when comparing versions. A successful build logs
`UNDERWATER_WEBGL_READY` with the actual output directory.

Serve the resulting directory with the included server rather than opening
`index.html` directly:

```bash
python3 Tools/serve_webgl.py --directory Build/WebGLReefLife --port 8904
```

Open `http://localhost:8904/?water=layered`. The server supplies the gzip and
WebAssembly headers Unity requires. Use HTTPS when hosting publicly; browser
audio requires an initial click or key press, and persistent browser caching
may be unavailable on non-local HTTP origins.

## Play

- Hold the lower-left arrows, or use WASD / arrow keys, to move.
- Use the lower-right rise/dive arrows; E rises and Q dives.
- Drag empty water to look around. Scroll the mouse wheel to change the view.
- Use **菜单** to choose a mission and a turtle, manta ray, clownfish or whale role.
- Choose **寻找鲸鱼** for a direction/distance guide; it does not teleport you.
- **语音提示** and **背景音** are independent menu switches. Dialogue subtitles
  have their own switch. Browser audio preferences are saved locally.
- Follow the short current-step hint. In rescue, equip the net cutter and drag
  across the marked net strands before guiding the freed turtle upward.

## Validate

The complete Unity check enters Play Mode and exits the editor itself. Do not
add `-quit` to this command:

```bash
"$UNITY_EDITOR" -batchmode -noaudio \
  -projectPath "$PWD" \
  -executeMethod MissionGameplayValidation.RunAll \
  -logFile "$PWD/Logs/gameplay-validation.log"

python3 -m unittest discover -s Tools -p test_serve_webgl.py -v
node --test Tools/test_prepare_webgl_delivery.mjs
python3 Tools/build_ui_font.py --check
```

Unity rendering/build checks need a working graphics display; on a headless
Linux machine, provide a suitable virtual display. Source-level movement and
geometry checks do not replace real-browser input, rendering and audible-audio
checks. The target is integrated-graphics laptops at 1080p / 60 fps; meeting
that target still requires testing on the actual hardware. Software-rendered
browser tests are not proof of laptop frame rate.

`Build`, `Library`, `Logs`, temporary files and user settings are intentionally
excluded from Git. Local preview links and paths in dated review notes describe
the original test environment, not a public hosted deployment.

## Asset provenance and licenses

- Seafloor textures: Rob Tuytel / Poly Haven, CC0; see
  `Assets/Resources/Textures/CoastSandRocks02/LICENSE.md`.
- Fonts: Droid Sans Fallback / Liberation Sans and the renamed combined Blue
  Lantern UI font; retain the Apache-2.0 / SIL-OFL-1.1 notices under
  `Assets/Resources/Fonts`.
- Model-maintenance scripts describe a Tripo-based source workflow. The model
  files and recorded dialogue do not include individual redistribution-license
  statements in this snapshot; confirm the original asset/platform permissions
  before reusing or redistributing them outside this project.
- Images under `Review/Concepts` are generated concept references, not gameplay
  screenshots.

No blanket open-source license is assigned to the repository or its assets.
