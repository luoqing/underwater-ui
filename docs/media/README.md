# Showcase media

These are production-scene captures, not generated concept artwork.

- Scene source: `aef9646`, Unity 6000.3.26f1, original model/texture bytes and
  accepted water, lighting and post-processing settings.
- `reef-overview.png`, `blue-tang.png`, `clownfish.png`: 1280x720 native Unity
  Play Mode renders from the 2026-10-25 reef-life validation. Diagnostic cameras;
  animals and their actual animations remain active.
- `adventure-ui.png`: actual 856x408 WebGL browser screenshot after selecting
  the turtle role and entering the adventure mission in that same version.
- `underwater-showcase.mp4`: current production scene, choreographed cameras,
  four two-second shots: reef, blue tang, whale, entangled rescue turtle.
  Captured at fixed 1/12-second simulation steps, encoded as H.264/yuv420p,
  1280x720, 12 fps, eight seconds, silent. No frame interpolation or retiming.
  This is a scene showcase, not a manual gameplay completion or real-time FPS test.
- `preview.gif`: reduced-size animated preview of the first four seconds of
  the same footage. Use the MP4 for the better-quality version.

Capture from Edit Mode using `ReadmeShowcaseCapture.Run` in batch Unity without
`-quit`; the capture utility exits after writing its frames. Encode with:

```bash
ffmpeg -framerate 12 -i Logs/readme-showcase-20261025/frames-final/frame-%05d.jpg \
  -frames:v 96 -c:v libx264 -crf 21 -pix_fmt yuv420p -movflags +faststart \
  docs/media/underwater-showcase.mp4
python3 Tools/encode_readme_preview.py \
  Logs/readme-showcase-20261025/frames-final docs/media/preview.gif
```

The preview encoder requires Pillow and learns its GIF palette from the actual
frames rather than using a coarse fixed RGB palette. GIF is still a reduced
preview; the MP4 retains more colour and detail.

The raw capture frames and logs stay excluded from Git. The MP4 and screenshots
are committed with relative README links so the presentation does not depend on
the developer's filesystem or private preview server. GitHub may open the MP4
on its file page rather than showing a player directly inside README; the GIF
provides an inline preview either way. Asset notices in `docs/DEVELOPMENT.md`
also apply to these recorded assets; no separate blanket media license is added.
