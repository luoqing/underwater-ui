"""Encode a small GIF from actual captured video frames, with an adaptive palette.

Requires Pillow. Does not change game assets or invent/interpolate video frames.
"""

import argparse
from pathlib import Path

from PIL import Image


def encode(frame_directory, destination):
    # 12 fps source -> 8 fps preview, exactly four seconds.
    frames = []
    palette_source = Image.new("RGB", (160 * 8, 90 * 4))
    for index in range(32):
        path = frame_directory / f"frame-{index * 12 // 8:05d}.jpg"
        with Image.open(path) as source:
            frame = source.convert("RGB").resize((640, 360), Image.Resampling.LANCZOS)
        frames.append(frame)
        palette_source.paste(frame.resize((160, 90)), (index % 8 * 160, index // 8 * 90))
    palette = palette_source.quantize(colors=256, method=Image.Quantize.MEDIANCUT)
    indexed = [frame.quantize(palette=palette, dither=Image.Dither.FLOYDSTEINBERG) for frame in frames]
    indexed[0].save(destination, save_all=True, append_images=indexed[1:],
                    duration=[120, 130] * 16, loop=0, optimize=False, disposal=2)
    with Image.open(destination) as preview:
        assert preview.size == (640, 360) and preview.n_frames == 32
        duration = 0
        for index in range(preview.n_frames):
            preview.seek(index)
            duration += preview.info["duration"]
        assert duration == 4000
    print(f"SHOWCASE_GIF_VALIDATED {destination}: 32 frames, 640x360, 4000ms, adaptive palette")


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("frames", type=Path)
    parser.add_argument("destination", type=Path)
    arguments = parser.parse_args()
    encode(arguments.frames, arguments.destination)
