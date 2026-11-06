"""SDA icon masters, one SVG per energy source. Run from Art/ikon, then render each to a 256 px PNG in Assets/UI/Icons."""
import math

T, K = '#1F2A36', '#FBF6EA'
J = 'stroke-linejoin="round" stroke-linecap="round"'
LINE = 5
S = f'stroke="{T}" stroke-width="{LINE}" {J}'
THIN = f'stroke="{T}" stroke-width="3" {J}'


def lg(i, stops, x1=128, y1=16, x2=128, y2=240):
    s = ''.join(f'<stop offset="{k / (len(stops) - 1):.3f}" stop-color="{c}"/>' for k, c in enumerate(stops))
    return f'<linearGradient id="{i}" gradientUnits="userSpaceOnUse" x1="{x1}" y1="{y1}" x2="{x2}" y2="{y2}">{s}</linearGradient>'


def pol(r, d, cx=128, cy=128):
    a = math.radians(d)
    return cx + r * math.cos(a), cy + r * math.sin(a)


def P(pts):
    return 'M' + ' L'.join(f'{x:.1f} {y:.1f}' for x, y in pts) + ' Z'


def spark(x, y, r, fill=K):
    pts = [pol(r if i % 2 == 0 else r * .3, i * 45 - 90, x, y) for i in range(8)]
    return f'<path d="{P(pts)}" fill="{fill}" stroke="{T}" stroke-width="2.5" {J}/>'


D = {}

# Matahari: a warm disc with long and short rays
g = lg('sun', ['#FFE27A', '#F2B33D', '#EE8A2E'], 128, 60, 128, 196) + lg('ray', ['#FFD35C', '#F59A3C'], 128, 6, 128, 250)
rays = ''
for i in range(12):
    d = i * 30 - 90
    r2 = 118 if i % 2 == 0 else 98
    w = 9 if i % 2 == 0 else 8
    rays += f'<path d="{P([pol(68, d - w), pol(r2, d), pol(68, d + w)])}" fill="url(#ray)" {S}/>'
D['matahari'] = (g, rays + f'<circle cx="128" cy="128" r="62" fill="url(#sun)" {S}/><path d="M94 112 A40 40 0 0 1 124 90" fill="none" stroke="{K}" stroke-width="7" stroke-linecap="round"/>')

# Batu Bara: a faceted black lump, each facet its own shade
g = lg('b1', ['#6B7180', '#3A3E47'], 60, 40, 120, 150) + lg('b2', ['#4B4F58', '#26292F'], 150, 40, 210, 170) + lg('b3', ['#3A3E47', '#191B20'], 90, 130, 170, 230)
top, left, right, low = (136, 34), (34, 126), (222, 112), (168, 216)
c, bl = (122, 118), (66, 206)
D['batubara'] = (g, f'<path d="{P([left, (84, 56), top, c])}" fill="url(#b1)" {S}/><path d="{P([top, (196, 62), right, c])}" fill="url(#b2)" {S}/>'
                    f'<path d="{P([left, c, bl])}" fill="url(#b1)" {S}/><path d="{P([c, right, low, bl])}" fill="url(#b3)" {S}/>'
                 + spark(170, 86, 17))

# Emas: three stacked ingots with a glint
g = lg('e1', ['#FFE27A', '#E2A72E'], 128, 136, 128, 216) + lg('e2', ['#F2C14E', '#C99A2E'], 128, 136, 128, 216) + lg('e3', ['#FFE27A', '#D9A02E'], 128, 56, 128, 136)


def ingot(x, y, fill):
    return f'<path d="{P([(x - 76, y + 44), (x - 52, y - 40), (x + 52, y - 40), (x + 76, y + 44)])}" fill="{fill}" {S}/><path d="{P([(x - 52, y - 40), (x + 52, y - 40), (x + 42, y - 24), (x - 42, y - 24)])}" fill="{K}" fill-opacity=".55"/>'


D['emas'] = (g, ingot(74, 178, 'url(#e2)') + ingot(182, 178, 'url(#e1)') + ingot(128, 92, 'url(#e3)') + spark(196, 56, 19))

# Minyak Bumi: a heavy violet drop with a wave of oil inside
g = lg('m', ['#6B5A4A', '#3A2F27', '#1C1714'], 128, 24, 128, 234) + lg('m2', ['#2B231E', '#120E0C'], 128, 150, 128, 234)
drop = 'M128 18 C146 62 204 104 204 158 A76 76 0 0 1 52 158 C52 104 110 62 128 18 Z'
D['minyak'] = (g + f'<clipPath id="mc"><path d="{drop}"/></clipPath>',
               f'<path d="{drop}" fill="url(#m)"/><g clip-path="url(#mc)"><path d="M40 168 C76 146 104 186 136 168 S190 150 220 166 V240 H40 Z" fill="url(#m2)" {THIN}/></g><path d="{drop}" fill="none" {S}/>'
               f'<path d="M84 150 C86 124 100 104 112 90" fill="none" stroke="{K}" stroke-width="8" stroke-linecap="round"/>')


# Uranium: a crystal cluster, one tall and two short
def crystal(cx, top, base, w, a, b, c):
    return (f'<path d="{P([(cx, top), (cx - w, top + w * .9), (cx - w, base - w * .4), (cx, base)])}" fill="{a}" {S}/>'
            f'<path d="{P([(cx, top), (cx + w, top + w * .9), (cx + w, base - w * .4), (cx, base)])}" fill="{b}" {S}/>'
            f'<path d="{P([(cx, top), (cx - w * .55, top + w * .85), (cx, top + w * 1.25), (cx + w * .55, top + w * .85)])}" fill="{c}" {THIN}/>')


g = lg('u1', ['#8FE36A', '#4FA83A'], 128, 16, 128, 240) + lg('u2', ['#4FA83A', '#2E7A37'], 128, 16, 128, 240)
D['uranium'] = (g, crystal(66, 96, 232, 32, 'url(#u1)', 'url(#u2)', '#C8F5A8') + crystal(192, 112, 232, 30, 'url(#u1)', 'url(#u2)', '#C8F5A8')
                + crystal(128, 20, 240, 44, 'url(#u1)', 'url(#u2)', '#DCFBC8'))

# Acak: a tumbling die in pink tones
g = lg('a1', ['#FFB3E0', '#E86DBB'], 128, 20, 128, 120) + lg('a2', ['#E86DBB', '#D1409F'], 60, 110, 60, 240) + lg('a3', ['#D1409F', '#A52C7E'], 190, 110, 190, 240)
t, l, r, m, ll, lr, bb = (128, 24), (30, 80), (226, 80), (128, 136), (40, 188), (216, 188), (128, 240)
dots = ''.join(f'<ellipse cx="{x}" cy="{y}" rx="17" ry="10" fill="{K}" {THIN}/>' for x, y in [(128, 80)])
for x, y in [(66, 132), (98, 196)]:
    dots += f'<ellipse cx="{x}" cy="{y}" rx="9.5" ry="12" fill="{K}" {THIN}/>'
for x, y in [(158, 196), (178, 164), (198, 132)]:
    dots += f'<ellipse cx="{x}" cy="{y}" rx="9.5" ry="12" fill="{K}" {THIN}/>'
D['acak'] = (g, f'<path d="{P([t, r, m, l])}" fill="url(#a1)" {S}/><path d="{P([l, m, bb, ll])}" fill="url(#a2)" {S}/><path d="{P([m, r, lr, bb])}" fill="url(#a3)" {S}/>{dots}')

for k, (defs, body) in D.items():
    with open(f'sda-{k}.svg', 'w', encoding='utf8') as f:
        f.write(f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 256 256" width="256" height="256"><defs>{defs}</defs>{body}</svg>')
