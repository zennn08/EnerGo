def f(p): return f"{p[0]:.1f} {p[1]:.1f}"
def poly(pts): return "M"+" L".join(f(p) for p in pts)+"Z"
A,B,C,P=(64,36),(64,220),(224,128),(116,128)
def crystal(cols,gapcol="#fff",gap=9):
    out="".join(f'<path d="{poly([p,q,P])}" fill="{c}"/>' for (p,q),c in zip([(A,C),(C,B),(B,A)],cols))
    out+="".join(f'<path d="M{f(P)} L{f(v)}" stroke="{gapcol}" stroke-width="{gap}" stroke-linecap="square"/>' for v in (A,B,C))
    return out
def svg(name,body,vb="0 0 256 256"): open(name,"w").write(f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="{vb}">{body}</svg>')
NAVY,AMB,AMB2,MINT="#0F2438","#FFC738","#FFAD1F","#2EF2B8"
V={"j-bold":(AMB,NAVY,AMB2),"k-mint":(AMB,NAVY,MINT),"l-dark":(AMB,MINT,AMB2)}
svg("j-bold.svg",crystal(("#111",)*3))
for k,c in V.items():
    dark=k=="l-dark"; bg=f'<rect width="256" height="256" rx="56" fill="{NAVY}"/>' if dark else ""
    g=NAVY if dark else "#fff"
    svg(f"{k}-color.svg",bg+crystal(c,g))
    tc="#fff" if dark else NAVY
    lbg=f'<rect width="800" height="256" rx="40" fill="{NAVY}"/>' if dark else ""
    svg(f"{k}-lockup.svg",lbg+f'<g transform="translate(20 20) scale(.85)">{crystal(c,g)}</g><text x="270" y="170" font-family="Poppins, Arial, sans-serif" font-weight="800" font-size="120" fill="{tc}">Ener<tspan fill="{AMB}">Go</tspan></text>',"0 0 800 256")
# resource system sheet: same crystal, recolored per resource
R=[("Surya","#FFD23F","#FFAD1F","#FFE58A"),("Batu Bara","#4A5262","#22283A","#6B7486"),
   ("Emas","#F2C14E","#B8860B","#FFE08A"),("Minyak Bumi","#2C4A5A","#0B1A22","#4F7487"),
   ("Uranium","#7CFF6B","#1F8A3B","#C2FFB8")]
body=f'<rect width="1400" height="420" fill="#F6F6F3"/><text x="40" y="60" font-family="Poppins, Arial" font-weight="700" font-size="30" fill="{NAVY}">Sistem warna sumber daya (in-game) — bentuk tetap, warna berganti</text>'
for i,(n,a,b,c) in enumerate([("Brand",AMB,NAVY,AMB2)]+R):
    x=40+i*225
    body+=f'<rect x="{x}" y="100" width="200" height="200" rx="40" fill="{NAVY if i else "#fff"}" stroke="#ddd"/>'
    gc=NAVY if i else "#fff"
    body+=f'<g transform="translate({x+20} 120) scale(.625)">{crystal((a,b,c) if i else (a,b,c),gc)}</g>'
    body+=f'<text x="{x+100}" y="345" text-anchor="middle" font-family="Poppins, Arial" font-weight="600" font-size="24" fill="{NAVY}">{n}</text>'
svg("system.svg",body,"0 0 1400 420")
