"""Turns the generated guide images (white background JPEG) into transparent PNGs.

usage: potong.py <out_dir> [name ...]    Reads sumber/*.jpeg next to this file.
White connected to the image edge becomes transparent, so the white shirt and stripes stay.
HOLES lists gaps enclosed by the body, which the edge cannot reach, as a point within 16 px of each gap in source pixels.
"""
import glob
import os
import sys

import numpy as np
from PIL import Image
from scipy import ndimage as ni

THRESH = 232
PAD = 8
HOLES = {
    'pemandu_santai': [(910, 959), (838, 967)],
    'pemandu_memperingatkan': [(903, 702)],
    'pemandu_menunjuk': [(902, 703)],
}
# Same hat width across portraits, so swapping a face does not make the head jump in the dialogue box.
PORTRAIT_HAT_WIDTH = 720


def hat_width(alpha):
    """Widest row in the top half: the hat brim."""
    rows = alpha[: alpha.shape[0] // 2] > 128
    return int(rows.sum(axis=1).max())


def cut(path, name):
    im = np.asarray(Image.open(path).convert('RGB')).astype(np.int16)
    white = im.min(axis=2) >= THRESH
    lab, _ = ni.label(white)
    edge = set(lab[0]) | set(lab[-1]) | set(lab[:, 0]) | set(lab[:, -1])
    edge.discard(0)
    bg = np.isin(lab, list(edge))
    for x, y in HOLES.get(name, []):
        window = lab[max(y - 16, 0):y + 16, max(x - 16, 0):x + 16]
        found = np.unique(window[window > 0])
        if found.size == 0:
            raise SystemExit(f'{name}: no white near ({x},{y})')
        biggest = max(found, key=lambda i: int((lab == i).sum()))
        bg |= lab == biggest
    # JPEG specks and stray marks: keep only the largest piece.
    flab, count = ni.label(~bg)
    sizes = ni.sum(~bg, flab, range(1, count + 1))
    fg = flab == np.argmax(sizes) + 1
    alpha = ni.gaussian_filter(ni.binary_erosion(fg, iterations=1).astype(np.float32), .7)
    rgba = np.dstack([im.astype(np.uint8), (alpha * 255).astype(np.uint8)])
    ys, xs = np.where(fg)
    box = (max(xs.min() - PAD, 0), max(ys.min() - PAD, 0), min(xs.max() + PAD, im.shape[1]), min(ys.max() + PAD, im.shape[0]))
    out = Image.fromarray(rgba, 'RGBA').crop(box)
    if '_wajah_' in name:
        scale = PORTRAIT_HAT_WIDTH / hat_width(np.asarray(out)[:, :, 3])
        out = out.resize((round(out.width * scale), round(out.height * scale)), Image.LANCZOS)
    return out


def main():
    out_dir = sys.argv[1]
    only = set(sys.argv[2:])
    os.makedirs(out_dir, exist_ok=True)
    here = os.path.dirname(os.path.abspath(__file__))
    for path in sorted(glob.glob(os.path.join(here, 'sumber', '*.jpeg'))):
        name = os.path.splitext(os.path.basename(path))[0]
        if only and name not in only:
            continue
        img = cut(path, name)
        img.save(os.path.join(out_dir, name + '.png'))
        print(name, img.size)


if __name__ == '__main__':
    main()
