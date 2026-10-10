# quick lockups: symbol + wordmark (Poppins-like, exploration only -> <text>)
import sys
for k in ["a-orbit","b-power","c-leafe"]:
    sym=open(f"{k}-color.svg").read().split('256">',1)[1].rsplit("</svg>",1)[0]
    open(f"{k}-lockup.svg","w").write(f'''<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 760 256"><g>{sym}</g><text x="290" y="168" font-family="Poppins, Arial, sans-serif" font-weight="700" font-size="118" fill="#1E3550">Ener<tspan fill="#4DB04A">Go</tspan></text></svg>''')
