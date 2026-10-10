"""EnerGo final logo kit (Solar Clean). Run: python build_final.py"""
from fontTools.ttLib import TTFont
from fontTools.pens.svgPathPen import SVGPathPen
from fontTools.pens.boundsPen import BoundsPen
from fontTools.pens.transformPen import TransformPen

CREAM, ORANGE, SUN, INK = "#FAF7F0", "#FF6B2C", "#FFC93C", "#16181D"
# equilateral play-triangle, left edge vertical; cuts meet at the centroid -> all edges at 0/60/120 deg
H3 = 184 * 3 ** .5 / 2
A, B, C, P = (54, 36), (54, 220), (54 + H3, 128), (54 + H3 / 3, 128)

def sub(a, b): return (a[0]-b[0], a[1]-b[1])
def isect(p1, d1, p2, d2):
    den = d1[0]*d2[1]-d1[1]*d2[0]
    t = ((p2[0]-p1[0])*d2[1]-(p2[1]-p1[1])*d2[0])/den
    return (p1[0]+t*d1[0], p1[1]+t*d1[1])
def shift(p, d, toward, g):
    # move line (p,d) by g toward point `toward`
    n = (-d[1], d[0]); L = (n[0]**2+n[1]**2)**.5; n = (n[0]/L, n[1]/L)
    if (toward[0]-p[0])*n[0]+(toward[1]-p[1])*n[1] < 0: n = (-n[0], -n[1])
    return (p[0]+n[0]*g, p[1]+n[1]*g)
def facet(p, q, gap):
    # triangle p,q,P with the two cuts (P-p, P-q) pushed inward by gap/2 -> real gaps, no strokes
    cen = ((p[0]+q[0]+P[0])/3, (p[1]+q[1]+P[1])/3)
    dp, dq, de = sub(p, P), sub(q, P), sub(q, p)
    lp, lq = shift(P, dp, cen, gap/2), shift(P, dq, cen, gap/2)
    pts = [isect(lp, dp, p, de), isect(lq, dq, p, de), isect(lp, dp, lq, dq)]
    return "M" + " L".join(f"{x:.2f} {y:.2f}" for x, y in pts) + "Z"

def symbol(cols, gap=9):
    # cols: top (A-C), bottom (C-B), left (B-A)
    return "".join(f'<path d="{facet(p, q, gap)}" fill="{c}"/>'
                   for (p, q), c in zip([(A, C), (C, B), (B, A)], cols))

font = TTFont("../../Assets/EnerGo/Resources/Fonts/Poppins-Bold.ttf")
gs, cmap = font.getGlyphSet(), font.getBestCmap()
def word(text, tracking=-10):
    """Outlined glyph paths in font units, baseline y=0 (y down). Returns (list of (path, chars), width)."""
    x, out = 0, []
    for ch in text:
        g = cmap[ord(ch)]
        pen = SVGPathPen(gs)
        gs[g].draw(TransformPen(pen, (1, 0, 0, -1, x, 0)))
        out.append((pen.getCommands(), ch)); x += gs[g].width + tracking
    return out, x - tracking
bp = BoundsPen(gs); gs[cmap[ord("E")]].draw(bp); CAP = bp.bounds[3]  # cap height

def wordmark(ink, accent, x, base, size):
    s = size/CAP; paths, w = word("EnerGo")
    g = "".join(f'<path d="{d}" fill="{accent if i >= 4 else ink}"/>' for i, (d, _) in enumerate(paths))
    return f'<g transform="translate({x:.1f} {base:.1f}) scale({s:.4f})">{g}</g>', w*s

def svg(name, w, h, body, title="EnerGo logo"):
    open(f"final/{name}.svg", "w").write(
        f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 {w:.0f} {h:.0f}"><title>{title}</title>{body}</svg>')

THEMES = {  # symbol colours, text ink, accent, background (None = transparent)
    "color":      ((SUN, INK, ORANGE), INK, ORANGE, None),
    "color-dark": ((SUN, CREAM, ORANGE), CREAM, ORANGE, INK),
    "black":      ((INK,)*3, INK, INK, None),
    "white":      (("#FFFFFF",)*3, "#FFFFFF", "#FFFFFF", None),
}
for t, (cols, ink, acc, bg) in THEMES.items():
    # symbol
    svg(f"energo-symbol-{t}", 256, 256, (f'<rect width="256" height="256" fill="{bg}"/>' if bg else "") + symbol(cols))
    # lockups use the symbol's tight box (x 54..54+H3, y 36..220), translated to the origin
    sym = f'<g transform="translate({-A[0]} {-A[1]})">{symbol(cols)}</g>'
    SH, SW = 184, H3
    bg = None  # lockups ship transparent; '-color-dark' = artwork for dark backgrounds
    # horizontal: cap height = 0.55 x symbol height, gap = 0.45 x cap, baseline centred on symbol
    cap = SH * .55; gap = cap * .45
    wm, w = wordmark(ink, acc, SW + gap, SH/2 + cap/2, cap)
    W = SW + gap + w
    svg(f"energo-horizontal-{t}", W, SH, (f'<rect width="{W:.0f}" height="{SH}" fill="{bg}"/>' if bg else "") + sym + wm)
    # stacked: wordmark width = 1.6 x symbol width, gap = 0.6 x cap
    _, w0 = wordmark(ink, acc, 0, 0, 100); cap = 100 * SW * 1.6 / w0
    wm, w = wordmark(ink, acc, 0, SH + cap * .6 + cap, cap)
    H = SH + cap * 1.6 + 4
    svg(f"energo-stacked-{t}", w, H, (f'<rect width="{w:.0f}" height="{H:.0f}" fill="{bg}"/>' if bg else "")
        + f'<g transform="translate({(w-SW)/2:.1f} 0)">{sym}</g>' + wm)
    # wordmark only
    cap = 100; wm, w = wordmark(ink, acc, 0, cap, cap)
    svg(f"energo-wordmark-{t}", w, cap + 2, (f'<rect width="{w:.0f}" height="{cap+2}" fill="{bg}"/>' if bg else "") + wm)

# small-size cut: wider gaps so facets stay apart at 16-32 px
svg("energo-symbol-small", 256, 256, symbol((SUN, INK, ORANGE), gap=18))
# app icon: cream full-bleed tile, symbol ~62 %, optically nudged (play shapes look left-heavy)
svg("energo-appicon", 1024, 1024, f'<rect width="1024" height="1024" fill="{CREAM}"/>'
    f'<g transform="translate(512 512) scale(3.3) translate(-134 -128)">{symbol((SUN, INK, ORANGE), gap=11)}</g>')
svg("energo-appicon-dark", 1024, 1024, f'<rect width="1024" height="1024" fill="{INK}"/>'
    f'<g transform="translate(512 512) scale(3.3) translate(-134 -128)">{symbol((SUN, CREAM, ORANGE), gap=11)}</g>')
print("ok")
