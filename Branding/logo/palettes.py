def f(p): return f"{p[0]:.1f} {p[1]:.1f}"
def poly(pts): return "M"+" L".join(f(p) for p in pts)+"Z"
A,B,C,P=(64,36),(64,220),(224,128),(116,128)
def crystal(cols,g):
    o="".join(f'<path d="{poly([p,q,P])}" fill="{c}"/>' for (p,q),c in zip([(A,C),(C,B),(B,A)],cols))
    return o+"".join(f'<path d="M{f(P)} L{f(v)}" stroke="{g}" stroke-width="9" stroke-linecap="square"/>' for v in (A,B,C))
PAL=[("1 · Electric Night","Gelap, neon, terasa 'game' &amp; AR malam",
      "#15123D","#FFE14D","#7B5CFF","#FFFFFF",("#FFE14D","#7B5CFF","#FFB627")),
     ("2 · Solar Clean","Terang, bersih, edukatif &amp; ramah sekolah",
      "#FAF7F0","#FF6B2C","#FFC93C","#16181D",("#FFC93C","#16181D","#FF6B2C")),
     ("3 · Eco Lab","Gelap kehijauan, sains &amp; energi terbarukan",
      "#0C1F1A","#C6FF3D","#19C6A0","#EFFFF6",("#C6FF3D","#19C6A0","#7EE081"))]
W=1500; body=f'<rect width="{W}" height="760" fill="#EDEDE8"/>'
for i,(name,note,bg,a1,a2,ink,cols) in enumerate(PAL):
    x=40+i*480
    body+=f'<rect x="{x}" y="40" width="450" height="560" rx="36" fill="{bg}" stroke="#d5d5d0"/>'
    body+=f'<g transform="translate({x+125} 80) scale(.78)">{crystal(cols,bg)}</g>'
    body+=f'<text x="{x+225}" y="370" text-anchor="middle" font-family="Poppins, Arial" font-weight="800" font-size="64" fill="{ink}">Ener<tspan fill="{a1}">Go</tspan></text>'
    # mini UI: button + card
    body+=f'<rect x="{x+60}" y="410" width="330" height="64" rx="32" fill="{a1}"/><text x="{x+225}" y="452" text-anchor="middle" font-family="Poppins, Arial" font-weight="700" font-size="26" fill="{bg}">MULAI BERBURU</text>'
    body+=f'<rect x="{x+60}" y="494" width="330" height="70" rx="18" fill="{a2}" opacity=".18"/><circle cx="{x+100}" cy="529" r="16" fill="{a2}"/><text x="{x+130}" y="538" font-family="Poppins, Arial" font-weight="600" font-size="22" fill="{ink}">Uranium ditemukan!</text>'
    for j,c in enumerate([bg,a1,a2,ink]):
        body+=f'<rect x="{x+j*112}" y="620" width="100" height="44" rx="10" fill="{c}" stroke="#ccc"/><text x="{x+j*112+50}" y="686" text-anchor="middle" font-family="Consolas, monospace" font-size="16" fill="#333">{c}</text>'
    body+=f'<text x="{x}" y="726" font-family="Poppins, Arial" font-weight="700" font-size="24" fill="#111">{name}</text><text x="{x}" y="752" font-family="Poppins, Arial" font-size="18" fill="#555">{note}</text>'
open("palettes.svg","w").write(f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 {W} 770">{body}</svg>')
