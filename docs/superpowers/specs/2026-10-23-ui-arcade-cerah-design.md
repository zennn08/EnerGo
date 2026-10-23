# UI "Arcade Cerah": Desain

Tanggal: 2026-10-23 · Status: arah visual disetujui di chat (mockup v5 / Opsi A), sudah dicek builder, menunggu review pengguna

Mockup acuan: [`assets/2026-10-23-ui-arcade-cerah-mockup.html`](assets/2026-10-23-ui-arcade-cerah-mockup.html). Mockup ini berisi lobby, berburu AR, alur kuis lengkap, dan panel sistem visual. Buka di browser.

## 1. Tujuan

Pengguna merasa tampilan EnerGo sekarang "mirip web, bukan game": latar cream, kartu putih datar, dominan teks. Tujuannya membuat semua layar terasa seperti game kasual mobile. Warnanya jenuh, judulnya tebal bergaris tepi, tombolnya empuk dengan bayangan, dan latarnya dunia kartun. Data, aturan main, dan alur yang sudah ada tetap dipakai.

**Keputusan pengguna**

- **Arah visual: Opsi A "Arcade Cerah"** (mockup v5). Langit toska, bukit rumput isometrik, ubin sumber daya warna-warni, tombol oranye BERBURU!.
- **Semua judul tegak.** Tidak ada judul atau header yang dimiringkan.
- **Referensi hanya untuk gaya.** Gambar referensi pengguna diambil rasa dan cara gambarnya saja, bukan isi, tata letak, atau fiturnya. Arah "pulau isometrik dengan dock navy" (v3/v4) ditolak.
- **Bagian atas lobby:** "⚡ 1.240" dan "Lv 7" di mockup tidak dipakai karena tidak ada di game. Gantinya cukup satu kapsul berisi **total unit koleksi** (jumlah semua sumber daya). Tidak ada roda gigi di atas.
- **Dock bawah lobby berisi 3 tombol:** **Panduan, Latihan, Pengaturan**. Tidak ada tombol Radar, karena BERBURU! sudah membuka radar. Tiap fitur hanya punya satu pintu masuk.
- **Ikon kristal brand diganti** dengan keluarga ikon baru dari mockup: matahari, batu bara, emas, minyak, uranium, dan acak. Logo brand (simbol kristal + wordmark) tetap dipakai di splash dan app icon.

**Kriteria berhasil**

1. Pengguna melihat build di A16 dan menilai semua layar "terasa game", tidak ada yang tersisa bergaya cream-card lama.
2. Semua teks di layar AR terbaca di atas kamera, baik saat terang (langit) maupun gelap (dalam ruangan).
3. Tidak ada perubahan perilaku game. Alur, angka hadiah, timer, dan aturan kuis sama persis. Semua test EditMode yang ada tetap lulus.
4. Performa di A16 tidak turun terasa, termasuk saat mini-game berjalan.
5. Semua tombol penting berada di dalam safe area dan berukuran ≥ 44 pt (≈ 9 mm).

**Bukan tujuan**

- Mengganti sistem UI. IMGUI tetap dipakai, tanpa migrasi ke UGUI atau UI Toolkit.
- Mengubah aset 3D di scene AR. Itu rencana terpisah lewat Blender MCP.
- Animasi dan efek besar (game feel). Ini masuk tahap polesan terpisah setelah semua layar selesai. Pengecualiannya adalah efek tekan tombol yang sudah ada (tombol turun ke bibirnya).
- Fitur baru: level, XP, mata uang, toko.

**Anti-tujuan** (gagal walaupun "jalan"):

- Ada judul yang miring.
- Tampilan berubah jadi isi layar referensi: pulau sebagai lobby, dock navy builder.
- Teks di layar AR tidak terbaca.
- Gaya yang bercampur antara layar baru dan layar lama yang belum diganti, dalam build yang diserahkan sebagai "selesai".

## 2. Sistem visual

Semua angka di bagian ini adalah **px mockup v5**. Lebar HP di mockup 300 px, sedangkan game menata layar dengan lebar 480 unit GUI (sebelum `HudScale`). Jadi **semua ukuran dikali 1,6**, termasuk tebal garis tepi, stroke teks, dan radius, supaya proporsinya sama dengan mockup yang disetujui.

### Warna (token di `EnerGoUI`)

| Token | Nilai | Peran |
|---|---|---|
| Ink | `#14213A` | Garis tepi, stroke teks, bayangan bibir |
| SkyTop / Sky / SkyLow | `#155D78` / `#1E8FA6` / `#36B8A8` | Gradien langit |
| Grass / GrassLow | `#7BD36A` / `#4FB35A` | Bukit dan tanah |
| Sun | `#FFC93C` | Hadiah, energi, aksen label |
| Orange (tombol utama) | gradien `#FFC04D` → `#FF6B2C` | Aksi utama |
| Ok (benar) | gradien `#C6F58A` → `#45B83A` | Benar, sukses |
| Bad (salah) | gradien `#FF9AA8` → `#E8304E` | Salah, bahaya |
| Info | gradien `#8FF3FF` → `#1AA4FF` | Aksi sekunder biru |
| Berry | `#FF3B5C` | Badge jumlah dan notifikasi |
| Paper | gradien `#FFFAF0` → `#FFE9C2` | Kartu dan popup (kuis, pengaturan, panduan) |
| PillBg | Ink 75% | Label kapsul gelap di atas latar apa pun |
| Muted | `#3A4256` | Teks sekunder di atas Paper |

Ubin per sumber daya memakai gradien terang ke gelap:

| Sumber daya | Gradien |
|---|---|
| Matahari | `#FFF0A0` → `#FFC93C` |
| Batu Bara | `#B9BFCC` → `#6B7180` |
| Emas | `#FFE9A0` → `#F0B52A` |
| Minyak | `#A99BFF` → `#5A49D6` |
| Uranium | `#D6FF9C` → `#58C23A` |
| Acak | `#FFB3E0` → `#D1409F` |

`EnerGoUI.Resource(id)` dipertahankan untuk kode lain yang memakainya. Nilainya mengikuti warna tengah gradien di atas.

### Huruf

- **Judul, tombol, angka besar:** *Lilita One*, tegak, warna putih atau Sun/oranye.
  - Stroke Ink 4–6 px dan bayangan turun 3–5 px.
- **Isi dan keterangan:** *Fredoka* SemiBold atau Bold.
- Keduanya berlisensi OFL. Lisensinya disimpan di samping file font di `Resources/Fonts/`.
- Poppins tidak dipakai lagi di UI.

### Komponen (di `EnerGoUI`)

- **Tombol empuk.** Isinya:
  - gradien vertikal,
  - garis tepi Ink 4–5 px dan sudut 20 (tombol bulat untuk TANGKAP),
  - bibir Ink 6–7 px di bawah,
  - pita gelap 8 px di dasar dalam dan kilau putih 6 px di atas dalam.

  Saat ditekan, tombol turun ke bibirnya, sama seperti perilaku sekarang. Labelnya teks Lilita bergaris tepi.
- **Ubin.** Sama seperti tombol, tapi sudut 18, bibir 6 px, dan badge jumlah Berry di pojok kanan atas.
- **Kartu kertas.** Isi Paper, garis tepi Ink 5 px, sudut 24–28, dan bibir Ink 8 px. Teksnya Ink.
- **Pita judul popup.** Tombol empuk kecil tanpa aksi yang menempel di tepi atas kartu, setengah keluar dari kartu (BENAR!, SALAH, KUIS, HASIL ANALISIS).
- **Kapsul.** Isi PillBg, garis tepi Ink 3 px, dan sudut penuh. Isinya teks putih atau aksen, opsional dengan badge ikon bulat di kiri.
- **Bar.** Lintasan Ink, isi gradien, dan garis tepi Ink 4 px.
- **Teks bergaris tepi.** Helper `DrawStrokedLabel(rect, text, style, strokePx)`. Teks digambar dulu 8 kali dengan warna Ink dan offset melingkar sebesar `strokePx`, lalu sekali lagi di posisi asli dengan warnanya sendiri. Bayangan turun adalah satu gambar Ink tambahan di posisi +y.

### Ikon

Ada satu keluarga ikon: garis Ink 3 px, isi rata, dan satu kilau. Isinya:

- **Sumber daya:** matahari, batu bara, emas, minyak, uranium, acak.
- **Dock:** buku (Panduan), target (Latihan), roda gigi (Pengaturan).
- **Lainnya:** kompas (dipakai di HUD AR).
- **Kecil:** bintang, centang, silang, jam.

Sumbernya adalah SVG di mockup, diekspor ke PNG 256 px dengan alpha ke `Resources/UI/Icons/`. Teks tidak pernah ditanam di gambar.

### Latar

- **Lobby, latihan, panduan, pengaturan, kuis:** latar dunia, yaitu langit toska gradien, garis kisi isometrik tipis putih 12%, bukit rumput, dan beberapa gerombol pohon. Kuis memakai latar yang sama, digelapkan sekitar 45%.
- **Layar AR:** tetap memakai kamera. Tidak ada latar penuh, hanya kapsul, bar, dan tombol.
- **Radar:** latar dunia yang sama. Piringan radar menjadi kartu bundar Paper bergaris tepi Ink.

## 3. Per layar

Semua layar memakai komponen di atas. Isi, teks, dan alurnya mengikuti yang ada sekarang, kecuali disebutkan lain.

- **Splash:** latar dunia, logo brand, dan bar muat bergaya komponen bar. "SENTUH LAYAR UNTUK MULAI" ditulis dengan teks bergaris tepi.
- **Lobby** (lihat mockup):
  - Kapsul atas berisi ikon petir dan total unit. Tidak ada tombol lain di atas.
  - Judul ENERGO! dengan Lilita bergaris tepi, "GO!" berwarna oranye.
  - Kapsul "KOLEKSI ENERGIMU".
  - Grid ubin 3×2: lima sumber daya plus satu ubin "Panduan cepat" atau ruang kosong (lihat §6). Ubin lobby tidak bisa diketuk, sama seperti sekarang.
  - Tombol BERBURU! ke ExploreScene. Baris "Mode kamera: …" tetap ada sebagai teks kecil di atasnya.
  - Dock 3 tombol ikon: Panduan, Latihan, Pengaturan.
  - Tombol "?" dan tombol pengaturan lama di kanan atas dihapus karena digantikan dock.
- **Pengaturan, Latihan, Panduan:** lembar bawah (bottom sheet) diganti kartu kertas dengan pita judul, di atas latar dunia yang digelapkan.
  - Baris toggle (ARCore, Getaran) dan slider volume memakai kapsul dan bar baru.
  - Latihan memakai grid ubin 3×2 yang bisa diketuk, termasuk Acak.
- **Panduan mini-game (`GuideViewer`):** kartu langkah menjadi kartu kertas. Tombol lanjut memakai tombol empuk, dan centang "Jangan ingatkan lagi" tetap ada.
- **Radar:**
  - Tombol "‹ LOBBY" dan timer spawn jadi kapsul.
  - Piringan radar jadi kartu bundar.
  - Kartu SDA terpilih jadi kartu kertas dengan tombol TANGKAP oranye, atau tombol nonaktif abu-abu "DEKATI DULU (x m)".
  - Peringatan keselamatan jadi popup kartu kertas.
- **HUD AR dan mini-game:**
  - Kapsul atas berisi target dan timer.
  - Kapsul status ("DALAM JANGKAUAN!", feedback).
  - Bar energi, tombol TANGKAP bulat kuning, tombol tutup, dan kompas.
  - Pita kompas, pip batu bara, dan HUD emas/minyak/uranium memakai kapsul dan bar baru.
  - Semua teks di layar ini wajib berada di kapsul gelap atau bergaris tepi.
- **Kuis** (lihat mockup, layar 1–4):
  - Titik progres 3 soal, dan kapsul "SOAL n/3".
  - Judul "BENAR ATAU SALAH?" tegak.
  - Kartu kertas bertumpuk dengan bar waktu 15 detik, ikon sumber daya, dan pernyataan.
  - Saat digeser, kartu miring dengan cap BENAR/SALAH, dan sisi layar ikut berwarna.
  - **Tombol ✕ / ✓ ditambahkan di bawah kartu** sebagai cara menjawab selain menggeser (lihat §6). Logika jawaban tetap sama.
  - Kartu umpan balik memakai pita hijau BENAR! atau merah SALAH / WAKTU HABIS, chip "Pernyataan ini …", penjelasan, dan chip "+1 unit" kalau benar.
  - Kalau jawaban salah, tombol lanjut abu-abu dan terisi selama 2 detik. Aturan 2 detik ini sudah ada, hanya tampilannya yang baru.
- **Hasil:**
  - Bintang = jumlah benar, dari 3.
  - "n/3 BENAR", hadiah besar (ikon + "+N"), dan chip kualitas Murni/Normal.
  - Chip rincian: Dasar +1, Kuis +n, Mini-game Grade +b.
  - Judul layar: "SEMPURNA!" untuk 3/3, "BAGUS!" untuk 1–2, dan "COBA LAGI" untuk 0.
  - Pesan dan tombol LANJUT BERBURU / LANJUT KE LOBBY tetap sama.

## 4. Teknik

- **Tetap IMGUI.** Perubahan gaya dipusatkan di `EnerGoUI.cs`. Layar-layar diubah untuk memanggil helper baru dan mengatur ulang tata letak bila perlu.
- **Gradien tanpa PNG.** `EnerGoUI` membuat tekstur gradien vertikal kecil (misalnya 1×64) per pasangan warna dan menyimpannya di cache. Tekstur itu digambar lewat `GUI.DrawTexture(..., borderWidth, borderRadius)` seperti `DrawPanel` sekarang. Tombol dan kartu cukup dirakit dari beberapa rounded rect tanpa aset 9-slice. Kalau hasilnya di HP terlihat kurang rapi, baru beralih ke 9-slice PNG.
- **Teks bergaris tepi:** 9–10 kali draw per label. Pakai hanya untuk judul, tombol, angka besar, dan label di atas kamera, bukan teks paragraf. `GUIStyle` di-cache, tidak dibuat ulang tiap frame.
- **Skala dan safe area:**
  - `HudScale` (acuan 480×800) tetap dipakai.
  - `SafeTop` dan `SafeBottom` dipindah ke `EnerGoUI` supaya Lobby, Radar, dan AR memakai rumus yang sama.
  - Elemen penting dijaga di dalam safe area. Latar boleh sampai ke tepi layar.
- **Warna bukan satu-satunya penanda.** Benar/salah selalu disertai teks atau ikon ✓/✕. Item yang terkunci memakai ikon gembok.
- **Tidak ada perubahan logika.** Perubahan hanya di kode menggambar dan tata letak. Hit-test tombol (`TapHit`, `IsHeld`) tetap memakai rect yang sama dengan yang digambar.

## 5. Tahapan

Tiap tahap di-build dan dites di A16 sebelum lanjut.

1. **Fondasi.**
   - Token, font, gradien, tombol, ubin, kartu, kapsul, bar, teks bergaris tepi, dan ikon PNG.
   - Semua pemanggil `DrawPrimaryButton`, `DrawCard`, dan sejenisnya otomatis ikut bergaya baru.
2. **Lobby:** splash, lobby, pengaturan, latihan, dan panduan.
3. **Kuis dan hasil.**
4. **HUD AR dan 5 mini-game**, termasuk `GuideViewer`.
5. **Radar.**

Setelah tahap 5, bersihkan aset lama yang tidak dipakai lagi: `bg_lobby`, `btn_play`, `btn_secondary`, `card_panel`, `pill_badge`, dan `crystal_*` kalau memang sudah tidak direferensikan. Poppins juga dihapus kalau sudah tidak dipakai.

## 6. Keputusan saya (bisa dikoreksi)

- **Tombol ✕ / ✓ di kuis** ditambahkan sebagai cara menjawab selain menggeser. Pemain yang tidak tahu kartunya bisa digeser tetap bisa menjawab, dan ini lebih aksesibel. Pengguna belum menjawab soal ini secara eksplisit.
- **Slot ubin ke-6 di lobby** dibiarkan kosong, karena "Acak" hanya ada di Latihan. Grid lobby jadi 3+2 dengan baris kedua di tengah.
- **Judul hasil kuis** ("SEMPURNA!", "BAGUS!", "COBA LAGI") adalah teks baru. Pesan di bawahnya tetap dari kode sekarang.
- **Total unit di kapsul atas** dihitung dari jumlah unit kelima sumber daya.
- **Gradien dan bentuk dibuat secara prosedural**, tanpa 9-slice PNG, kecuali hasilnya di HP kurang rapi.
- **Skala mockup ke game ×1,6 untuk semua ukuran**, termasuk garis tepi. Alasannya agar tampilan sama dengan mockup. Kalau di HP garis tepinya terasa terlalu tebal, tebal garis tepi saja yang diturunkan.

## 7. Diserahkan ke pembuat

- Ukuran persis per elemen di tiap layar, selama mengikuti komponen dan proporsi mockup.
- Cara membagi `EnerGoUI` kalau filenya terlalu besar, misalnya dipecah jadi partial class.
- Detail ilustrasi latar dunia (pohon dan kisi). Boleh disederhanakan selama terasa seperti mockup.
