# The Blue Between Worlds

Unity 6.3 URP benchmark for a cinematic underwater web journey.

The first 48-second loop travels through a shallow coral reef, kelp and fish schools, open water with a whale, a bioluminescent trench, submerged ruins, and a final ascent toward the surface.

## Build

```bash
env TMPDIR=/tmp TEMP=/tmp TMP=/tmp XDG_RUNTIME_DIR=/tmp \
  ~/.local/bin/unity-editor \
  -batchmode -nographics -quit \
  -projectPath "$PWD" \
  -executeMethod UnderwaterBuild.BuildWebGL \
  -logFile /tmp/underwater-build.log
```

The WebGL output is written to `Build/WebGL`.

