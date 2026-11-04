# EnerGo

Game edukasi AR berbasis GPS untuk pelajar SMP/SMA, Unity 6000.3.24f1, URP, Android portrait.

## Acuan
- Isi game (layar, mekanik, teks, angka): docs/PRD-EnerGo.md.
- Tampilan: docs/design-system/ (gaya Jurnal Ekspedisi). Nama variabel USS = nama token di tokens.json.
- Aset 3D dan audio: docs/prompt-aset.md (material, nama file, durasi suara).
- Urutan kerja: docs/tutorial.md.

## Aturan
- UI baru memakai UI Toolkit (Assets/UI). Jangan ubah logika game saat memindahkan layar dari IMGUI.
- Satuan UI = unit dasar 480 x 800, sama dengan token.
- Teks di atas kamera AR selalu di atas kartu atau pil kertas-100.
- Nama file aset dan suara persis seperti di docs/prompt-aset.md, karena dipanggil kode.
- File .blend disimpan di Art/, bukan di Assets/.
- Sebelum perintah Blender: simpan file .blend. Satu objek per perintah.
- Bahasa UI: Bahasa Indonesia.
