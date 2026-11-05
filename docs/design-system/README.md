EnerGo adalah buku lapangan seorang penjelajah energi. Setiap layar terasa seperti halaman jurnal ekspedisi: kertas krem, garis tinta tebal, selotip kraft, cap karet, peta kontur, dan coretan tangan di pinggir. Gaya ini harus tetap ramah untuk pelajar SMP/SMA: besar, jelas, dan sedikit jenaka, bukan arsip kuno yang muram.

## Suara dan teks

- Tulis dalam Bahasa Indonesia baku yang santai. Sapa pemain dengan "kamu" dan "-mu" ("Koleksi energimu", "Gerakanmu terlalu kencang").
- Instruksi dan status ditulis HURUF KAPITAL dan pendek, maksimal 6 kata per baris: "TAHAN UNTUK MENGEBOR", "CAHAYA TERKUNCI · SISA 12 DTK". Pakai titik tengah ( · ) untuk menyambung dua info, bukan koma.
- Penjelasan, isi panduan, dan pernyataan kuis memakai huruf biasa dan kalimat lengkap.
- Angka bacaan alat selalu dengan satuannya dan spasi: "42 m", "±8 m", "15 dtk", "3 j". Desimal pakai koma ("0,6 g").
- Tanda seru hanya untuk kejadian: "BLOWOUT!", "SEMPURNA!", "BERBURU!". Jangan di instruksi biasa.
- Tidak ada emoji. Ikon dari set Ikon.
- Coretan tangan (`catatan`) adalah suara penjelajah: komentar kecil, huruf kecil, tanpa titik ("langka! cuma 3%", "hmm, sepi…"). Maksimal satu per layar, dan tidak pernah berisi instruksi yang wajib dibaca.

## Fondasi visual

**Kertas dan tinta.** Semua permukaan adalah kertas: `kertas-100` untuk kartu, `kertas-200` untuk halaman, `kertas-300` untuk kraft dan isian lekuk. Semua bentuk diberi garis tepi `tinta`: `garis-tipis` untuk chip, `garis-tinta` untuk kartu/tombol/pil, `garis-tebal` untuk tombol utama besar, cap, dan radar. Teks utama selalu `tinta`, teks sekunder `tinta-pudar`.

**Latar dunia.** Splash, Lobby, Radar, Kuis, dan Desa memakai peta topografi (`.eg-peta`): `kertas-200`, daratan `peta-hijau`, air `peta-air`, garis kontur `peta-hijau-tua` dengan `opasitas-kontur`, dan grid `grid-peta`. Latar ini hiasan; jangan menaruh teks langsung di atasnya tanpa kartu atau pil.

**Di atas kamera.** HUD layar tangkap adalah kartu kertas terang yang ditempel di atas kamera, bukan panel gelap transparan. Setiap teks di atas kamera harus berada di atas `kertas-100` (kotak status, pil, pita kompas, label kamera `.eg-label-kamera`). Satu-satunya warna menyala yang boleh di kamera adalah `uranium-nyala`, dan hanya untuk objek uranium.

**Bayangan dan kedalaman.** Bayangan adalah offset keras berwarna tinta, seperti cetakan: `bayang-tombol` untuk bibir tombol, `bayang-kartu` untuk kartu di atas peta/kamera, `bayang-tile` untuk tile dan pil kertas. Tidak ada blur lembut tanpa offset tinta, tidak ada gradasi selain bar dan latar dunia.

**Radius.** `radius-sm` (6) untuk chip dan pita kompas, `radius-md` (10) untuk tombol, tile, kotak status, `radius-lg` (16) untuk kartu dan sheet, `radius-pil` untuk pil, bar, dan tombol bulat.

**Jarak.** Gutter layar `space-4` kiri-kanan. Padding kartu `space-4`, ditambah `space-6` di atas kalau ada selotip. Celah antar elemen di layar `space-3`. Semua ukuran dalam unit layar dasar 480 × 800 (skala IMGUI); 64 unit ≈ 48 dp, jadi tidak ada target sentuh di bawah `sentuh-min`.

**Motif.** Tiga motif yang membawa tema, masing-masing dengan tugas jelas:
- **Selotip** (`.eg-selotip`) = judul kartu. Pengganti pita judul lama.
- **Cap karet** (`.eg-stempel`) = penilaian: BENAR, SALAH, MURNI, NORMAL, PADAM.
- **Nomor katalog** (`No.01`–`No.05` dalam `data`) = identitas spesimen SDA.
Jangan pakai motif di luar tugasnya; cap bukan dekorasi.

## Warna

- Satu aksi utama per layar memakai `oranye` dengan label `tinta`. Teks atau tanda oranye di atas kertas memakai `oranye-tua`.
- Energi dan hadiah memakai `oker`: total unit, bintang, chip "+N unit", stok pembangkit.
- Benar/aman = `hutan`, salah/bahaya/gagal = `stempel`, info/navigasi sekunder = `laut`. Ketiganya selalu ditemani kata atau ikon (✓, ✕, tag AMAN/AWAS/GAGAL), karena pemain buta warna merah-hijau tidak bisa mengandalkan warna.
- Muka kartu umpan balik: `hutan-muda` (benar), `stempel-muda` (salah).
- Tirai di belakang sheet, panduan, dan peringatan: `redup`.

**Warna SDA** adalah identitas, dipakai konsisten di tile, node radar, ikon, dan isian bar mini-game:

| SDA | Utama | Muda | Teks di atas utama |
|---|---|---|---|
| Matahari | `sda-matahari` | `sda-matahari-muda` | `tinta` |
| Batu Bara | `sda-batubara` | `sda-batubara-muda` | `kertas-100` |
| Emas | `sda-emas` | `sda-emas-muda` | `tinta` |
| Minyak Bumi | `sda-minyak` | `sda-minyak-muda` | `kertas-100` |
| Uranium | `sda-uranium` | `sda-uranium-muda` | `tinta` |
| Acak | `sda-acak` | `sda-acak-muda` | `kertas-100` |

Penampang minyak memakai `lapis-tanah`, `lapis-batuan`, `lapis-penutup`, `lapis-gas`, `lapis-minyak`, `lapis-air-asin`, selalu dengan label nama lapisan di sebelahnya.

**Kontras.** Semua pasangan teks di atas memenuhi 4,5:1. `tinta-samar` hanya di atas `kertas-100`. Cincin fokus: `laut` 3 unit dengan jarak 4 unit dari tombol.

## Tipografi

- **Bricolage Grotesque** (`judul`): judul layar, judul kartu, label tombol, selotip dan cap (pakai lebar 75% untuk selotip dan cap). Gaya: `judul-layar`, `judul-1`, `judul-kartu`, `label-tombol`, `selotip`.
- **Lexend** (`isi`): semua teks isi dan status, dirancang untuk mudah dibaca. Gaya: `isi`, `isi-tebal`, `soal`, `kecil`, `status`.
- **Space Mono** (`data`): semua bacaan alat dan angka: jarak, GPS, kompas, timer, SOAL n/3, jumlah unit, hadiah "+N". Gaya: `angka-besar`, `data`, `data-tebal`.
- **Caveat** (`tulisan`): hanya coretan `catatan`.

Judul layar ditulis dengan sorotan satu kata `oranye-tua`: "ENER**GO!**", "BENAR **ATAU** SALAH?", "DESA **TEKNIK**". Paling banyak satu kata yang disorot.

Unity IMGUI belum memakai sumbu variable font, jadi ekspor instance statis dari berkas di `fonts/`: Bricolage Grotesque ExtraBold (lebar 100 dan 75), Lexend Regular/SemiBold/Bold, Space Mono Regular/Bold, Caveat SemiBold.

## Gerak dan umpan balik

- Tombol turun 3 unit saat ditekan (0,08 detik). Sheet naik dari bawah 0,25 detik. Cap muncul dari 140% ke 100% dalam 0,12 detik.
- Selotip, cap, dan label kamera sedikit miring (−3° sampai +8°). Jangan memiringkan teks instruksi, tombol, atau angka.
- Getaran mengikuti tabel PRD: tick 8-15 ms, ketukan 20-30 ms, sukses 40-80 ms, gagal 200-300 ms. Setiap getaran gagal harus disertai tag GAGAL atau cap SALAH di layar.

## Ikonografi

Set ikon sendiri di `assets/Ikon`: garis `tinta` 2 unit di kotak 24 × 24, ujung bulat, dengan isian kertas/kraft/oker di bagian yang perlu bobot. Ikon SDA (`sda-*`) berwarna penuh dengan garis tinta dan selalu dipakai untuk mewakili SDA; jangan diganti emoji atau foto. Ikon tidak bisa diwarnai ulang lewat `<img>`, jadi di atas isian gelap (tombol ok/bahaya) pakai versi terang atau filter invert, dan selalu dengan label.

Tanda EnerGo ada di `assets/Logo`. Wordmark "EnerGo" diset dengan huruf `judul`, "Go" berwarna `oranye-tua`.

## Layar

Grup **Layar** berisi mockup lengkap di bingkai 480 × 800: Splash, Lobby, transisi, sheet Pengaturan/Latihan/Panduan, Radar, peringatan keselamatan, GPS gagal, kelima mini-game, kuis, umpan balik, hasil, dan Desa Teknik (direncanakan). Pakai komponen di grup lain untuk membangunnya; kelas `.eg-hp`, `.eg-layar`, dan `.eg-baris-hp` hanya untuk bingkai mockup.
