"""Turns the moving village pieces into transparent PNGs in Resources/Desa.

desa_kerbau has a white background, so it goes through the guide's cutout. desa_awan has white clouds on flat blue,
so the blue is keyed out instead. Full pictures need no processing and are copied to Resources as they are.
"""
import importlib.util
import os

import numpy as np
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(HERE, '..', '..', 'Assets', 'EnerGo', 'Resources', 'Desa')
KEY_SOFTNESS = 55.0  # colour distance from the blue where a pixel becomes fully opaque
BUFFALO_WIDTH = 512
CLOUD_WIDTH = 1024

spec = importlib.util.spec_from_file_location('potong', os.path.join(HERE, '..', 'pemandu', 'potong.py'))
potong = importlib.util.module_from_spec(spec)
spec.loader.exec_module(potong)


def clouds(path):
    rgb = np.asarray(Image.open(path).convert('RGB')).astype(np.float32)
    blue = np.median(rgb[:200].reshape(-1, 3), axis=0)
    alpha = np.clip(np.linalg.norm(rgb - blue, axis=2) / KEY_SOFTNESS, 0, 1)
    alpha[alpha < .12] = 0  # JPEG noise in the sky
    a = np.maximum(alpha[..., None], 1e-3)
    colour = np.clip((rgb - (1 - a) * blue) / a, 0, 255)  # remove the blue that bled into soft edges
    rgba = np.dstack([colour, alpha * 255]).astype(np.uint8)
    ys, xs = np.where(alpha > .5)
    pad = 12
    return Image.fromarray(rgba, 'RGBA').crop((xs.min() - pad, ys.min() - pad, xs.max() + pad, ys.max() + pad))


def fit_width(img, width):
    return img.resize((width, round(img.height * width / img.width)), Image.LANCZOS)


def main():
    os.makedirs(OUT, exist_ok=True)
    for name, img, width in (
        ('desa_kerbau', potong.cut(os.path.join(HERE, 'desa_kerbau.jpg'), 'desa_kerbau'), BUFFALO_WIDTH),
        ('desa_awan', clouds(os.path.join(HERE, 'desa_awan.jpg')), CLOUD_WIDTH),
    ):
        img = fit_width(img, width)
        img.save(os.path.join(OUT, name + '.png'))
        print(name, img.size)


if __name__ == '__main__':
    main()
