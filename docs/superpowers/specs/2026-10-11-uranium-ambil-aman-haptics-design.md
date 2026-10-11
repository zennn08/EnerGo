# Uranium "Ambil dengan Aman" + getaran di semua mode

Tanggal: 2026-10-11. Disetujui di chat.

## Latar

Fase kedua uranium (reaktor + slider batang kendali) terasa kurang: kamera tidak terpakai, konsepnya
reaktor PLTN (bukan menambang), dan ketegangannya cuma menatap jarum. Diganti dengan mengambil serpihan
bijih dengan aman, yang mengajarkan prinsip proteksi radiasi **Waktu, Jarak, Perisai**.
Sekalian: semua mode memakai getaran HP (mengikuti saklar Pengaturan → Getaran, `Haptics.Enabled`).

## 1. Uranium

**Fase Cari** — tidak berubah (kompas, RAD, Geiger, tahan `RevealHoldSec` dalam `RevealAngleDeg`).

**Fase Ambil** (menggantikan Secure):
- Bijih pecah jadi `FragmentCount = 3` serpihan pada bearing acak di 360°, jarak antarserpihan ≥ 80°
  (bearing serpihan pertama = bijih), pitch acak `HideMinPitchDeg..HideMaxPitchDeg`, jarak `UraniumOreDistance`.
  Serpihan selalu terlihat (bercahaya, berputar).
- RAD, panah tren, dan Geiger menunjuk serpihan terdekat (sudut terkecil) yang belum diambil.
- **Jepit:** tombol JEPIT bulat di kanan tengah (setinggi jempol; bawah tengah sudah dipakai bar dosis dan tombol reset). Selama tombol ditahan dan serpihan terdekat ada di dalam
  `GripAngleDeg` (= `RevealAngleDeg`) dan guncangan < `GripShakeMax`, `grip` naik `1/GripSec` per detik
  (`GripSec = 1`). Lepas tombol, serpihan keluar reticle, atau guncangan → `grip = 0` ("JEPITAN LEPAS").
  `grip >= 1` → serpihan masuk wadah (terbang ke bawah layar lalu hilang), hitungan `n/3` naik.
- **Dosis:** `dose += DoseBasePerSec * (FloorShare + (1 - FloorShare) * left / 3) * dt`,
  `DoseBasePerSec = 1/40`, `FloorShare = 0.25` (sisa paparan meski semua di wadah — fase berakhir saat 3/3).
- **Hasil:** 3/3 → Captured, `Performance(elapsed, dose)` seperti sekarang (`ParSec` disesuaikan ±20 s).
  `dose >= 1` → "DOSIS BERLEBIH" → `RetryUranium` (bijih baru, mulai dari Cari).
- **Dibuang:** reaktivitas, slider, gauge reaktor, lonjakan neutron, drift, meltdown, beserta konstanta & test-nya.
- **HUD:** kartu atas = teks fase + bar `n/3`; reticle tetap; RAD meter tetap; tombol JEPIT dengan cincin
  progres `grip` berupa lingkaran yang membesar; bar DOSIS tetap.
- **Panduan** (`GuideBook`): kartu 2 "Jepit serpihan" (Art.Hold): arahkan lingkaran ke serpihan, tahan JEPIT,
  penjepit menjaga jarak. Kartu 3 "Cepat & stabil" (Art.Shake): dosis terus naik, tiap serpihan di wadah timbal
  memperlambatnya, guncangan melepas jepitan. `Art.Slider` dihapus.
- **Kuis:** tambah `uranium-31` (id 08 sudah terpakai) tentang Waktu, Jarak, Perisai (sumber diverifikasi saat implementasi).

Logika murni tetap di `Core/UraniumMath.cs` (penempatan bearing, langkah grip, langkah dosis, terdekat, hasil),
diuji di `UraniumMathTests`.

## 2. Getaran

Semua lewat `Haptics.Pulse(ms)`; tik berulang dibatasi laju seperti `oilLastRumbleAt`.

| Mode | Kejadian → pulsa |
|---|---|
| Matahari | di reticle: 8 ms tiap 0,25 s · Captured: 80 · TooLittle: 300 |
| Batubara | pukulan: 20 · pecah: 50 · semua terkumpul: 120 |
| Emas | datar tercapai (→ Countdown): 40 · goyang kasar: 10 tiap 0,3 s · emas terbuang (spill): 120 · Done: 80 · Failed: 300 |
| Radar | target baru masuk jangkauan tangkap: 60 |
| Kuis | benar: 30 · salah / waktu habis: 200 |
| Uranium | Geiger (ada) · menjepit: 8 tiap 0,2 s · serpihan masuk: 60 · gagal: 300 |
| Minyak | tidak berubah |

## Di luar cakupan

Pola getar (dua ketukan dsb.), model 3D serpihan (tetap primitif, lihat rencana aset Blender).

## Pengujian

EditMode: `UraniumMathTests` diperbarui (bearing ≥ 80°, grip naik/reset, dosis turun per serpihan, terdekat,
urutan hasil). Getaran: cek manual di HP. Build APK dan tes di perangkat.
