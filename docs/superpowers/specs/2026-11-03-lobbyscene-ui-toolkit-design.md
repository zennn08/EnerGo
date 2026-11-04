# LobbyScene ke UI Toolkit (Jurnal Ekspedisi): desain

Tanggal: 3 Nov 2026. Branch: `feat/ui-arcade-cerah`. Status: menunggu persetujuan.

## Maksud

Seluruh tampilan LobbyScene pindah dari IMGUI (`LobbyController.OnGUI`) ke UI Toolkit, dengan gaya design system Jurnal Ekspedisi di `docs/design-system/`. Ini Tahap 1a di `docs/tutorial.md`, diperluas sesuai keputusan pemilik: seluruh scene dipindah sekaligus, bukan Lobby saja, supaya IMGUI dan UI Toolkit tidak pernah menumpuk di satu scene (lihat risiko di `docs/peta-proyek.md` bagian 5).

Pemilik produk sudah mencoba arah lain (latar lukisan, gaya game kasual) dan memutuskan kembali ke design system yang ada.

## Tujuan

- Splash, Lobby, sheet Pengaturan, sheet Latihan, sheet Panduan, kartu panduan mode baca, dan transisi digambar UI Toolkit sesuai mockup `docs/design-system/components/LayarAwal/preview.html` dan `LayarSheet/preview.html` (ukuran dan posisi 480 × 800 diambil dari sini), plus README dan `preview.html` komponen terkait (Tombol, TombolBulat, Kartu, Pil, TileSpesimen, Kontrol, Logo, BarUkur). Preview disalin dari artifact design system pemilik (https://claude.ai/artifact/FTNaqKEEfNjKAeTJkkB5Ln) dan memakai kelas `components/bundle.css` serta token `tokens.json`.
- Perilaku game tidak berubah: urutan layar, teks, angka, waktu (splash 2,4 dtk, transisi 0,45 dtk, reset 3,5 dtk, jeda input), PlayerPrefs, dan pemanggilan SFX/haptics persis seperti sekarang.
- Fondasi yang bisa dipakai ulang oleh layar berikutnya (Radar, layar tangkap): `Theme.uss` dari token, `Components.uss`, PanelSettings, font statis.

## Bukan tujuan

- Radar (ExploreScene) dan layar tangkap (ARPrototype/Sensor360Scene) tetap IMGUI. `GuideViewer` IMGUI tetap dipakai layar tangkap.
- Tidak mengubah logika game, mekanik, teks PRD, atau data save.
- Tidak mengubah aset 3D, suara, atau ikon game di `Resources/UI/Icons` yang dipakai layar IMGUI.
- Tidak mengganti latar dengan gambar; latar peta dibuat dari kode (keputusan pemilik).

## Anti-tujuan (gagal walau berjalan)

- Satu ketukan menekan dua tombol (misalnya menutup sheet sekaligus menekan BERBURU! di bawahnya).
- Ada layar atau state dari PRD yang hilang (tile terkunci, konfirmasi reset, "Panduan akan muncul lagi", lompat ke kuis tertunda saat ada `pendingCapture`).
- Teks terpotong atau tertutup notch di HP dengan safe area.
- Nilai di layar tidak ikut berubah setelah aksi (total unit setelah RESET PROGRES, info mode kamera setelah toggle Mode AR).

## Batasan

- Unity 6000.3.24f1, Input System saja (`activeInputHandler: 1`), modul `uielements` dan `vectorgraphics` sudah ada di manifest.
- Satuan UI = unit dasar 480 × 800, sama dengan token. Nama variabel USS = nama token di `tokens.json`.
- Bahasa UI Bahasa Indonesia, teks persis dari PRD dan kode sekarang.
- Path aset dan suara lama dipertahankan (`Assets/EnerGo/Resources/...`).
- USS tidak punya `box-shadow`: bayangan offset keras dibuat dari elemen bayangan di belakang (atau border bawah yang lebih tebal untuk bibir tombol, seperti contoh di tutorial).

## Keputusan pemilik (sudah disepakati)

1. Seluruh LobbyScene pindah sekaligus; `OnGUI` LobbyController dimatikan seluruhnya.
2. Aksi tombol boleh dipindah dari dalam fungsi gambar ke metode publik `LobbyController`, dengan isi yang sama.
3. Latar peta dari kode (`.eg-peta`: kertas-200, daratan peta-hijau, air peta-air, kontur, grid tipis), bukan gambar.
4. Kartu panduan mode baca di Lobby dibuat ulang di UI Toolkit, membaca isi dari `GuideBook` yang sama; layar tangkap tetap memakai `GuideViewer` IMGUI.
5. Logo ikut design system: tanda kompas-petir `docs/design-system/assets/Logo/energo-mark.svg` dengan wordmark "EnerGo" diset dalam Bricolage Grotesque ExtraBold ("Go" `oranye-tua`). Logo kristal lama (`Resources/UI/logo_game.png`, `logo_wordmark.png`) tidak dipakai lagi di LobbyScene. Ikon aplikasi Android tidak diubah di pekerjaan ini.
6. `docs/design-system/` disamakan dengan artifact design system pemilik (README, tokens, README komponen, `preview.html` semua komponen dan layar, ikon dan logo).

## Bentuk solusi

**Pemisahan state dan tampilan.** `LobbyController` tetap pemilik state (`currentState`, timer splash/transisi, cooldown, reset, pilihan latihan, `useARCore`) dan semua efek samping (PlayerPrefs, SFX, haptics, `SceneManager.LoadScene`). Isi blok aksi yang sekarang ada di dalam `DrawLobby`/`DrawSettings`/`DrawPractice`/`DrawGuides`/`DrawSheet` dipindah ke metode publik dengan isi identik (misalnya mulai berburu, buka sheet, tutup sheet, toggle mode AR, set volume saat dilepas, toggle getaran, ketuk reset, pilih tile latihan, mulai latihan, buka panduan SDA, reset panduan). `Update` tetap menjalankan timer. `OnGUI` dihapus. Nama metode bebas, isinya tidak boleh berubah.

**Tampilan.** Satu `UIDocument` di LobbyScene dengan PanelSettings 480 × 800 (Scale With Screen Size, Match 0). Satu komponen tampilan (misalnya `LobbyView`) membangun atau mengikat elemen dari UXML, mendengarkan perubahan state, dan memanggil metode publik `LobbyController`. Tampilan tidak menyimpan state game sendiri; setiap kali state berubah atau sheet ditutup, nilai yang ditampilkan dibaca ulang dari sumbernya (`EnerGoProgress.Current`, `AudioListener.volume`, `Haptics.Enabled`, pilihan latihan, `ResourceIds.IsPlayable`).

**Input.** Hanya satu layer yang menerima ketukan pada satu waktu: elemen Lobby dinonaktifkan dari picking selama sheet, panduan, atau transisi terbuka; tirai sheet menangkap ketukan di luar kartu (menutup sheet). Jeda input yang sekarang (`inputCooldownTimer` 0,15–0,25 dtk) tetap dihormati: tombol tidak bereaksi selama cooldown. Pastikan UI Toolkit menerima sentuhan dengan Input System saja; jika perlu tambahkan EventSystem + `InputSystemUIInputModule` (cek di HP). Splash tetap bisa dilewati dengan ketuk layar.

**Gaya.** `Assets/UI/Theme.uss` berisi semua token warna, jarak, radius, garis, ukuran dari `tokens.json`. `Assets/UI/Components.uss` menerjemahkan `docs/design-system/components/bundle.css` untuk komponen yang dipakai di LobbyScene. Font: instance statis dari `docs/design-system/fonts/` dengan fonttools (perintah di tutorial Tahap 1a langkah 2), disimpan di `Assets/Fonts/` beserta Font Asset. Ikon dan logo SVG dari `docs/design-system/assets/` diimpor sebagai vector image UI Toolkit.

**Gerak.** Tombol turun 3 unit saat ditekan; sheet naik dari bawah 0,25 dtk; selotip miring −2°; transisi dengan tanda berputar; splash: tanda membesar 88% → 100% dalam 0,6 dtk, ajakan berkedip, bar terisi 2,4 dtk.

**Safe area.** Padding root mengikuti `Screen.safeArea` (contoh script di tutorial langkah 6), diuji dengan Device Simulator ber-notch.

## Isi layar (ringkas, detail di PRD 3.1–3.7 dan README komponen)

- **Splash**: tanda 120 unit + wordmark di kartu kertas, tagline "Jelajahi sumber daya, temukan energinya.", bar tipis 2,4 dtk, "SENTUH LAYAR UNTUK MULAI" berkedip setelah 0,35 dtk. SFX splash tetap di `Awake` seperti sekarang.
- **Lobby**: pil total unit (oker, petir), judul ENER**GO!**, kartu berselotip KOLEKSI ENERGIMU dengan 5 tile spesimen (3 + 2, nomor katalog, badge jumlah, state terkunci), pil mode kamera ("Mode kamera: AR (ARCore)" / "Mode kamera: Sensor 360"), tombol utama besar BERBURU!, dock 3 tombol bulat Panduan / Latihan / Pengaturan.
- **Sheet Pengaturan**: toggle Mode AR (sub-label berubah), slider Volume dengan persen, toggle Getaran, tombol info LATIHAN (TANPA GPS), tombol RESET PROGRES dengan state konfirmasi "TEKAN LAGI UNTUK MENGHAPUS" 3,5 dtk, tombol utama SELESAI.
- **Sheet Latihan**: sub "Pilih sumber daya, main tanpa GPS", grid 3 × 2 (5 SDA + Acak), tile terpilih bercincin oranye + DIPILIH, tombol MULAI LATIHAN.
- **Sheet Panduan**: sub "Ketuk untuk membaca cara mainnya", grid 3 + 2 SDA, tombol TAMPILKAN SEMUA PANDUAN LAGI, teks "Panduan akan muncul lagi" 2 dtk.
- **Kartu panduan mode baca**: tirai redup, kartu bergaris dengan selotip nama SDA, LANGKAH n/N, ilustrasi, judul, isi, titik halaman, pindah halaman (ketuk sepertiga kiri/kanan atau tombol), tombol LANJUT lalu TUTUP, tombol kecil TUTUP. Isi dari `GuideBook`. Ilustrasi (keputusan Claude): sampai 13 ilustrasi sketsa jurnal dibuat di Tahap 4, kotak ilustrasi menampilkan ikon SVG SDA (`sda-*.svg`) di bingkai kertas. Gambar bentuk IMGUI di `GuideViewer.DrawGuideArt` tidak dipindahkan.
- **Transisi**: layar tinta, tanda berputar, "Menyalakan radar..." / "Menyiapkan kamera...", 0,45 dtk.

## Verifikasi

- Tes EditMode yang ada tetap lulus (84 tes).
- Uji di editor (play mode + Device Simulator ber-notch) dan di HP: semua jalur di PRD bagian 2 (splash, lewati splash, BERBURU!, dock ke tiap sheet, tutup sheet dengan ketuk luar dan SELESAI, toggle, slider, reset dua langkah, latihan Acak, panduan baca semua halaman, kembali dari radar dengan Main Mode Latihan, kuis tertunda saat buka ulang).
- Cek tidak ada ketukan ganda di batas sheet/tombol.
- Screenshot per layar dibandingkan dengan mockup design system.

## Keputusan kecil (oleh Claude, bisa dikoreksi)

- `Assets/EnerGo/Editor/LobbySceneBuilder.cs` ikut diperbarui supaya scene yang dibangun ulang tetap punya UIDocument dan komponen tampilan.
- Fade splash (masuk 0,5 dtk, keluar 0,4 dtk) dari PRD 3.1 tetap dipakai, termasuk dalam "waktu tidak berubah".

## Diserahkan ke pembangun

Struktur file UXML (satu per layar atau satu dokumen dengan beberapa layer), nama elemen, cara animasi (transition USS atau kode), dan cara membuat bayangan offset, selama hasilnya sesuai mockup dan batasan di atas.
