"""Unity-side logo assets (app icons, UI symbol, crystals). Run from Branding/logo: python build_unity_assets.py"""
import subprocess
from build_final import symbol, A, P, H3, CREAM, ORANGE, SUN, INK

RENDER = "C:/Users/LENOVO/.claude/plugins/cache/logo-design-skill/logo-design/1.4.4/skills/logo-design/scripts/render_png.py"
UNITY = "../../Assets/EnerGo"

# Optical centre of the play-triangle: its mass sits on the vertical left edge, so centring the bounding box
# makes it look pushed left. Anchor a third of the way from the centroid towards the box centre instead.
CENTROID_X = P[0]
BOX_CX = A[0] + H3 / 2
OPTICAL_X = CENTROID_X + (BOX_CX - CENTROID_X) * 0.35
OPTICAL_Y = 128


def svg(name, vb, body):
    path = f"final/{name}.svg"
    open(path, "w").write(f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="{vb}">{body}</svg>')
    return path


def render(src, out, w, h=None):
    subprocess.run(["python", RENDER, src, "-o", out, "--width", str(w), "--height", str(h or w)], check=True)


def placed(scale, cols=(SUN, INK, ORANGE), gap=9):
    return f'<g transform="translate(128 128) scale({scale}) translate({-OPTICAL_X:.2f} {-OPTICAL_Y})">{symbol(cols, gap)}</g>'


def max_radius(scale):
    # furthest vertex from the icon centre, in 256-unit canvas
    import math
    return max(math.hypot((x - OPTICAL_X) * scale, (y - OPTICAL_Y) * scale) for x, y in [A, (A[0], 220), (A[0] + H3, 128)])


# Adaptive foreground: everything must stay inside the 66/108 safe circle (radius 0.611 * 128 = 78.2).
FG_SCALE = 0.68
assert max_radius(FG_SCALE) <= 78.2, max_radius(FG_SCALE)

# In-game crystal per resource: (top facet, bottom facet, left facet). Matches the guide's resource colour system.
CRYSTALS = {
    "sun": ("#FFE08A", "#FF9F1C", "#FFC93C"),
    "coal": ("#7A8090", "#16181D", "#3A3F4B"),
    "gold": ("#F7D98B", "#9A6A0C", "#E8B23A"),
    "oil": ("#9CC0CC", "#12303A", "#2F5D6B"),
    "uranium": ("#B9F0B0", "#0F5A2A", "#1E9A4A"),
    "random": (SUN, INK, ORANGE),
}

if __name__ == "__main__":
    full = svg("energo-appicon", "0 0 256 256", f'<rect width="256" height="256" fill="{CREAM}"/>' + placed(0.82, gap=11))
    dark = svg("energo-appicon-dark", "0 0 256 256", f'<rect width="256" height="256" fill="{INK}"/>' + placed(0.82, (SUN, CREAM, ORANGE), 11))
    fg = svg("unity-icon-fg", "0 0 256 256", placed(FG_SCALE, gap=11))
    bg = svg("unity-icon-bg", "0 0 256 256", f'<rect width="256" height="256" fill="{CREAM}"/>')
    render(full, f"{UNITY}/Textures/AppIcon/icon_full.png", 1024)
    render(fg, f"{UNITY}/Textures/AppIcon/icon_adaptive_fg.png", 432)
    render(bg, f"{UNITY}/Textures/AppIcon/icon_adaptive_bg.png", 432)
    render(full, "final/png/energo-appicon.png", 1024)
    render(dark, "final/png/energo-appicon-dark.png", 1024)
    print("optical x", round(OPTICAL_X, 1), "fg max radius", round(max_radius(FG_SCALE), 1))
    for key, cols in CRYSTALS.items():
        src = svg(f"crystal-{key}", f"54 36 {H3:.2f} 184", symbol(cols, gap=11))
        render(src, f"{UNITY}/Resources/UI/crystal_{key}.png", 173, 200)
