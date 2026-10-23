"""Rasterise every icon SVG in this folder to a 256x256 RGBA PNG for Unity.

Usage: python Branding/icons/export_icons.py
Needs: pip install resvg-py pillow
"""
import io
import pathlib

import resvg_py
from PIL import Image

HERE = pathlib.Path(__file__).resolve().parent
OUT = HERE.parents[1] / "Assets" / "EnerGo" / "Resources" / "UI" / "Icons"
SIZE = 256


def export(svg: pathlib.Path) -> pathlib.Path:
    png = bytes(resvg_py.svg_to_bytes(svg_string=svg.read_text(encoding="utf-8"), width=SIZE, height=SIZE))
    img = Image.open(io.BytesIO(png)).convert("RGBA")
    # Every icon must be transparent around the edge and drawn somewhere inside.
    assert img.size == (SIZE, SIZE), (svg.name, img.size)
    assert img.getpixel((0, 0))[3] == 0, svg.name
    assert img.getchannel("A").getextrema()[1] > 0, svg.name
    dst = OUT / (svg.stem + ".png")
    img.save(dst)
    return dst


if __name__ == "__main__":
    OUT.mkdir(parents=True, exist_ok=True)
    for svg in sorted(HERE.glob("*.svg")):
        print(export(svg).relative_to(HERE.parents[1]))
