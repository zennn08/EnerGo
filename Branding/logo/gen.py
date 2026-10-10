import math
C=128
def pt(r,a):
    a=math.radians(a); return (C+r*math.cos(a), C+r*math.sin(a))
def f(p): return f"{p[0]:.2f} {p[1]:.2f}"
def band(R,w,a1,a2):
    # annulus segment from a1 to a2 going counterclockwise (decreasing angle) if a2<a1
    r=R-w; large=1 if abs(a2-a1)>180 else 0; sweep=1 if a2>a1 else 0
    o1,o2,i2,i1=pt(R,a1),pt(R,a2),pt(r,a2),pt(r,a1)
    cap=w/2
    return (f"M{f(o1)} A{R} {R} 0 {large} {sweep} {f(o2)} "
            f"A{cap} {cap} 0 0 {sweep} {f(i2)} "
            f"A{r} {r} 0 {large} {1-sweep} {f(i1)} A{cap} {cap} 0 0 {sweep} {f(o1)}Z")
def leaf(p,q,bulge):
    # vesica between p and q; bulge = arc radius
    return f"M{f(p)} A{bulge} {bulge} 0 0 1 {f(q)} A{bulge} {bulge} 0 0 1 {f(p)}Z"
def svg(name,paths,fill="#111"):
    body="".join(f'<path d="{d}" fill="{c or fill}" fill-rule="evenodd"/>' for d,c in paths)
    open(name,"w").write(f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 256 256">{body}</svg>')
import sys
col = len(sys.argv)>1
CY,BL,GR=("#5BD3F0","#2F6FB0","#4DB04A") if col else (None,None,None)
sfx="-color" if col else ""
# A: Orbit Leaf - two arcs (refined swirl) + leaf closing the C
svg(f"a-orbit{sfx}.svg",[
    (band(112,26,-25,-170),CY),          # top arc
    (band(112,26,-188,-268),BL),         # left/bottom arc
    (leaf(pt(99,100),(232,118),118),GR), # leaf rising into the opening
])
# B: Power Leaf - power ring with a leaf as the stem
svg(f"b-power{sfx}.svg",[
    (band(104,26,-58,-482+360-360+0) if False else band(104,26,-60,240),BL),
    (leaf((128,22),(128,146),84),GR),
])
# C: Leaf-e - lowercase e whose crossbar is a leaf
svg(f"c-leafe{sfx}.svg",[
    (band(108,28,25,-360+0) if False else band(108,28,-2,-322),BL),
    (leaf((42,128),(222,128),190),GR),
])
