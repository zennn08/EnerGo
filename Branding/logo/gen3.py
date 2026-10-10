import sys
col=len(sys.argv)>1; sfx="-color" if col else ""
def f(p): return f"{p[0]:.1f} {p[1]:.1f}"
def poly(pts): return "M"+" L".join(f(p) for p in pts)+"Z"
def svg(name,body): open(name,"w").write(f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 256 256">{body}</svg>')
def facets(A,B,C,P,cols,round_=0,gap=9):
    out=""
    for (p,q),c in zip([(A,C),(C,B),(B,A)],cols):
        st=f' stroke="{c}" stroke-width="{round_}" stroke-linejoin="round"' if round_ else ""
        out+=f'<path d="{poly([p,q,P])}" fill="{c}"{st}/>'
    for v in (A,B,C): out+=f'<path d="M{f(P)} L{f(v)}" stroke="#fff" stroke-width="{gap}" stroke-linecap="round"/>'
    return out
def spark(x,y,r): return f"M{x} {y-r} Q{x} {y} {x+r} {y} Q{x} {y} {x} {y+r} Q{x} {y} {x-r} {y} Q{x} {y} {x} {y-r}Z"
K="#111"
# G: Soft Crystal - rounded, friendlier
c=("#FFD45C","#F29100","#FFB81C") if col else (K,K,K)
svg(f"g-soft{sfx}.svg",facets((70,48),(70,208),(208,128),(116,128),c,round_=28,gap=10))
# H: Resource Trio - each facet = one resource (sun, coal, gold)
c=("#FFC83D","#2B3142","#D99A0B") if col else (K,K,K)
svg(f"h-trio{sfx}.svg",facets((58,34),(58,222),(222,128),(112,128),c))
# I: Spark Crystal - sharp crystal + sparkle (energy found)
c=("#FFD45C","#F29100","#FFB81C") if col else (K,K,K)
svg(f"i-spark{sfx}.svg",facets((40,52),(40,224),(188,138),(88,138),c)+f'<path d="{spark(204,58,30)}" fill="{"#1B2340" if col else K}"/>')
