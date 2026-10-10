import math, sys
col=len(sys.argv)>1; sfx="-color" if col else ""
INK,AMB,AMB2,AMB3=("#1B2340","#FFB81C","#FFD45C","#F29100") if col else ("#111",)*4
def f(x,y): return f"{x:.2f} {y:.2f}"
def poly(pts): return "M"+" L".join(f(*p) for p in pts)+"Z"
def svg(name,body): open(name,"w").write(f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 256 256">{body}</svg>')
# D: Energy Pin - map pin, bolt in negative space
cx,cy,R,tip=128,100,82,240
d=tip-cy; a=math.acos(R/d)
t1=(cx+R*math.sin(a), cy+R*math.cos(a)); t2=(cx-R*math.sin(a), cy+R*math.cos(a))
pin=f"M{f(*t2)} A{R} {R} 0 1 1 {f(*t1)} L{f(cx,tip)}Z"
bolt=poly([(140,44),(96,110),(124,110),(112,160),(162,90),(134,90),(150,44)])
svg(f"d-pin{sfx}.svg",f'<path d="{pin} {bolt}" fill="{INK}" fill-rule="evenodd"/>' + (f'<path d="{bolt}" fill="{AMB}"/>' if col else ""))
# E: Crystal Play - faceted play button = mined crystal + Go
A,B,Cc,P=(58,34),(58,222),(222,128),(112,128)
facets=[(A,Cc,AMB2),(Cc,B,AMB3),(B,A,AMB)]
body="".join(f'<path d="{poly([p,q,P])}" fill="{c}" stroke="#fff" stroke-width="9" stroke-linejoin="round"/>' for p,q,c in facets)
svg(f"e-crystal{sfx}.svg",body)
# F: Bolt-G - G whose crossbar is a lightning bolt
def pt(r,ang): a=math.radians(ang); return (128+r*math.cos(a),128+r*math.sin(a))
Ro,Ri=106,72
o1,o2,i2,i1=pt(Ro,-40),pt(Ro,0),pt(Ri,0),pt(Ri,-40)
ring=f"M{f(*o1)} A{Ro} {Ro} 0 1 0 {f(*o2)} L{f(*i2)} A{Ri} {Ri} 0 1 1 {f(*i1)}Z"
gbolt=poly([(128,108),(214,108),(194,128),(230,128),(162,184),(178,148),(128,148)])
svg(f"f-boltg{sfx}.svg",f'<path d="{ring}" fill="{INK}"/><path d="{gbolt}" fill="{AMB if col else INK}"/>')
