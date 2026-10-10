"""One-page EnerGo logo guide -> final/guide.html (+ PNG). Run after build_final.py."""
import re
from build_final import symbol, CREAM, ORANGE, SUN, INK, A
def inner(f): s=open(f"final/{f}.svg").read(); return re.search(r'<svg[^>]*>(.*)</svg>',s,re.S).group(1)
def vb(f): return re.search(r'viewBox="([^"]+)"',open(f"final/{f}.svg").read()).group(1)
def art(f,h): return f'<svg viewBox="{vb(f)}" style="height:{h}px">{inner(f)}</svg>'
RES=[("Surya","#FFC93C","#16181D","#FF9F1C"),("Batu Bara","#7A8090","#16181D","#3A3F4B"),
     ("Emas","#F2C14E","#16181D","#B07D12"),("Minyak Bumi","#6E98A6","#16181D","#2F5D6B"),
     ("Uranium","#8BE07A","#16181D","#1E9A4A")]
res="".join(f'<div class="res"><svg viewBox="40 22 190 212" style="height:84px">{symbol((a,b,c))}</svg><b>{n}</b><code>{a} · {c}</code></div>' for n,a,b,c in RES)
pal="".join(f'<div class="sw"><i style="background:{c}"></i><b>{n}</b><code>{c}<br>RGB {int(c[1:3],16)} {int(c[3:5],16)} {int(c[5:7],16)}</code><small>{u}</small></div>'
  for n,c,u in [("Cream",CREAM,"Latar utama"),("Solar Orange",ORANGE,"Aksen, tombol utama, 'Go'"),("Sun",SUN,"Highlight, reward"),("Ink",INK,"Teks, ikon")])
html=f'''<!doctype html><html lang="id"><meta charset="utf-8"><title>EnerGo Logo Guide</title>
<style>
body{{margin:0;background:{CREAM};color:{INK};font-family:Poppins,Arial,sans-serif}}
main{{max-width:1200px;margin:0 auto;padding:56px 40px}}
h1{{font-size:44px;margin:0}} h2{{font-size:20px;margin:48px 0 16px;text-transform:uppercase;letter-spacing:.08em}}
p{{max-width:720px;line-height:1.6;color:#444}}
.row{{display:flex;gap:20px;flex-wrap:wrap;align-items:stretch}}
.card{{background:#fff;border-radius:20px;padding:36px;display:flex;align-items:center;justify-content:center;flex:1;min-width:260px}}
.dark{{background:{INK}}} .org{{background:{ORANGE}}}
.sw{{flex:1;min-width:200px}} .sw i{{display:block;height:90px;border-radius:14px;border:1px solid #0001}}
.sw b,.res b{{display:block;margin-top:10px}} code{{font-size:13px;color:#555}} small{{display:block;color:#777;margin-top:4px}}
.res{{background:#fff;border-radius:16px;padding:18px;text-align:center;flex:1;min-width:150px}}
ul{{line-height:1.9;color:#444}} .x{{color:#C0392B;font-weight:700}}
</style><main>
{art("energo-horizontal-color",70)}
<h1 style="margin-top:28px">Panduan Logo</h1>
<p>Simbolnya adalah kristal berbentuk tombol <i>play</i>: <b>game</b> dan <b>sumber daya</b> dalam satu bentuk. Tiga sisinya adalah satu kristal, bukan daftar sumber daya, jadi logo tetap berlaku berapa pun sumber daya yang ditambahkan ke game.</p>
<h2>Versi logo</h2>
<div class="row"><div class="card">{art("energo-horizontal-color",64)}</div><div class="card dark">{art("energo-horizontal-color-dark",64)}</div></div>
<div class="row" style="margin-top:20px"><div class="card">{art("energo-stacked-color",150)}</div><div class="card">{art("energo-symbol-color",130)}</div>
<div class="card">{art("energo-horizontal-black",40)}</div><div class="card org">{art("energo-horizontal-white",40)}</div></div>
<h2>Warna</h2><div class="row">{pal}</div>
<h2>Tipografi</h2><p><b style="font-size:28px">Poppins Bold</b> untuk judul &amp; tombol · <span style="font-size:20px">Poppins Regular</span> untuk teks. Wordmark sudah di-outline; jangan ketik ulang "EnerGo".</p>
<h2>Ruang kosong &amp; ukuran minimum</h2>
<ul><li>Ruang kosong di sekeliling logo minimal <b>x = tinggi huruf "E"</b> pada wordmark.</li>
<li>Simbol: minimum <b>16 px</b> — di bawah 32 px pakai <code>energo-symbol-small.svg</code> (celah lebih lebar).</li>
<li>Logo horizontal: minimum lebar <b>96 px</b> / 25 mm.</li></ul>
<h2>Sistem warna sumber daya (in-game)</h2>
<p>Bentuk kristal tetap; warnanya berganti per sumber daya — untuk ikon level, badge koleksi, notifikasi temuan. Sumber daya baru = warna baru, logo tidak berubah.</p>
<div class="row">{res}</div>
<h2>Jangan</h2>
<ul><li><span class="x">✕</span> Mengubah urutan/warna sisi kristal pada logo utama</li>
<li><span class="x">✕</span> Memutar, memiringkan, menambah bayangan, gradien, atau outline</li>
<li><span class="x">✕</span> Mengganti font wordmark atau mengubah jarak simbol–teks</li>
<li><span class="x">✕</span> Menaruh versi berwarna di atas foto/kamera ramai tanpa panel — pakai versi putih atau panel Ink</li></ul>
</main></html>'''
open("final/guide.html","w",encoding="utf-8").write(html)
