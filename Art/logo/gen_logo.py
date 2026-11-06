"""EnerGo logo masters. Run from Art/logo: writes the SVGs here; render them to PNG with any SVG renderer.

The mark is a radar whose sweep has caught one signal, a small lowercase e.
"""
import math

from fontTools.pens.boundsPen import BoundsPen
from fontTools.pens.svgPathPen import SVGPathPen
from fontTools.pens.transformPen import TransformPen
from fontTools.ttLib import TTFont

TINTA, KERTAS = '#1F2A36', '#FBF6EA'
KUNING, ORANYE, BATA = '#FFD35C', '#F59A3C', '#D9562B'
BIRU, BIRU_TUA = '#2F6E8F', '#0F2A3A'
J = 'stroke-linejoin="round" stroke-linecap="round"'
S = f'stroke="{TINTA}" stroke-width="4" {J}'

font = TTFont('../../Assets/Fonts/Bricolage-ExtraBold.ttf')
glyphs, cmap = font.getGlyphSet(), font.getBestCmap()


def letter(ch, height, cx, cy):
    name = cmap[ord(ch)]
    bounds = BoundsPen(glyphs)
    glyphs[name].draw(bounds)
    x0, y0, x1, y1 = bounds.bounds
    s = height / (y1 - y0)
    pen = SVGPathPen(glyphs)
    glyphs[name].draw(TransformPen(pen, (s, 0, 0, -s, cx - s * (x0 + x1) / 2, cy + s * (y0 + y1) / 2)))
    d = pen.getCommands()
    # Outline under the fill, so overlapping font contours never show inside the letter.
    return f'<path d="{d}" fill="{TINTA}" stroke="{TINTA}" stroke-width="5.5" {J}/><path d="{d}" fill="{KUNING}"/>'


def pol(r, d):
    a = math.radians(d)
    return 128 + r * math.cos(a), 128 + r * math.sin(a)


p, q = pol(110, -92), pol(110, -30)
SWEEP = (f'<linearGradient id="sapuan" gradientUnits="userSpaceOnUse" x1="128" y1="18" x2="200" y2="128">'
         f'<stop offset="0" stop-color="{ORANYE}"/><stop offset="1" stop-color="{BATA}"/></linearGradient>')
RADAR = (f'<circle cx="128" cy="128" r="110" fill="{KERTAS}"/>'
         f'<path d="M128 128 L{p[0]:.1f} {p[1]:.1f} A110 110 0 0 1 {q[0]:.1f} {q[1]:.1f} Z" fill="url(#sapuan)" {S}/>'
         f'<g fill="none" {S}><circle cx="128" cy="128" r="74"/><circle cx="128" cy="128" r="38"/><circle cx="128" cy="128" r="110"/></g>'
         f'<circle cx="128" cy="128" r="8" fill="{TINTA}"/>' + letter('e', 34, 164, 67))
BG = (f'<linearGradient id="latar" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="{BIRU}"/>'
      f'<stop offset="1" stop-color="{BIRU_TUA}"/></linearGradient>')


def write(name, size, body, defs=SWEEP, view=None):
    v = view or f'0 0 {size} {size}'
    with open(name, 'w', encoding='utf8') as f:
        f.write(f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="{v}" width="{size}" height="{size}"><defs>{defs}</defs>{body}</svg>')


def placed(size, diameter):
    """The radar centred in a `size` canvas, its outer ring `diameter` wide."""
    s = diameter / 224
    t = size / 2 - 128 * s
    return f'<g transform="translate({t:.2f} {t:.2f}) scale({s:.4f})">{RADAR}</g>'


# In-game mark: radar only, transparent, cropped to the ring.
write('energo-tanda.svg', 512, RADAR, view='14 14 228 228')
# Legacy and round launcher icon: full square, launchers apply their own mask.
write('icon_full.svg', 1024, f'<rect width="1024" height="1024" fill="url(#latar)"/>' + placed(1024, 760), SWEEP + BG)
# Adaptive icon: the launcher may crop to the middle 61%, so the radar stays inside it.
write('icon_adaptive_fg.svg', 432, placed(432, 256))
write('icon_adaptive_bg.svg', 432, '<rect width="432" height="432" fill="url(#latar)"/>', BG)
