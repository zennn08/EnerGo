# Radar Eksplorasi: Desain

Tanggal: 2026-10-10 · Status: disetujui per bagian di chat, menunggu review spec

## 1. Tujuan

Fitur eksplorasi ala Pokemon GO untuk EnerGo. Pemain **berjalan sungguhan** sambil berburu sumber daya alam (SDA) yang muncul acak di sekitarnya. SDA ditampilkan di **radar melingkar**, bukan di peta. Setiap SDA punya tingkat kelangkaan, dan Uranium adalah yang paling langka. Menangkap SDA tetap memakai mini-game AR dan kuis yang sudah ada.

**Keputusan pengguna**

- Lokasi berbasis GPS dan pemain harus jalan kaki.
- Tampilan berupa radar saja, tanpa peta. Lokasi SDA tetap disimpan sebagai koordinat GPS supaya peta bisa ditambahkan nanti.
- Semua pemain melihat SDA yang sama, tanpa server.
- Rate kelangkaan ditambah aturan konteks: Matahari hanya muncul di siang hari.
- Minyak Bumi dan Uranium tampil di radar tapi masih terkunci, karena mini-game-nya belum ada.
- Arsitektur pendekatan A: scene Radar baru, terpisah dari scene AR.

**Asumsi**

- Area main ada di sekitar sekolah, dengan radius radar 150 m.
- Radius tangkap 20 m.
- Pemain memakai HP Android. iOS di luar cakupan.

**Kriteria berhasil**

1. Dua HP di lokasi dan jendela waktu yang sama menampilkan SDA yang sama di posisi yang sama.
2. Dalam radius 150 m, rata-rata ada sekitar 12 SDA. Uranium muncul rata-rata sekitar 1 kali per 45 menit di sekitar pemain.
3. Pemain bisa menangkap SDA dari jarak ≤ 20 m (dengan koreksi akurasi GPS), lalu masuk mini-game, kuis, dan kembali ke Radar. SDA yang sudah ditangkap hilang dari radarnya.
4. Radar bisa dites sepenuhnya di Unity Editor tanpa GPS.

**Di luar cakupan**

- Peta sungguhan.
- Server atau sinkronisasi antar pemain.
- Mini-game Minyak Bumi dan Uranium, termasuk slot inventorinya.
- iOS.
- Anti-cheat untuk pemalsuan jam atau lokasi.

## 2. Alur layar

```
Lobby ──[MULAI BERBURU]──► Radar (ExploreScene, baru)
                             ├─ SDA ≤ 20 m + TANGKAP ──► AR capture (scene lama) ──► Kuis ──► Hasil
                             │◄──────────────── [LANJUT] ─────────────────────────────────────┘
                             └─ [‹ LOBBY] ──► Lobby
```

- **Lobby:** kartu SDA berubah dari pemilih target menjadi **koleksi** yang hanya menampilkan jumlah unit. Ada lima kartu. Matahari, Batu Bara, dan Emas menampilkan jumlah unit. Minyak Bumi dan Uranium diberi gembok dan label "Segera hadir". Target perburuan sekarang ditentukan dari radar.
- **Mode Latihan:** ada tombol "Latihan" di bottom sheet Pengaturan. Tombol ini membuka pemilih SDA yang lama (Matahari, Batu Bara, Emas, Acak), lalu langsung masuk ke mini-game tanpa GPS. Gunanya untuk demo di kelas, ruangan tanpa GPS, dan pengetesan.
- **Tombol LANJUT di kartu hasil:** kembali ke Radar kalau sesi berasal dari radar (`spawnId` terisi), dan kembali ke Lobby kalau berasal dari mode Latihan.
- **Izin lokasi:** diminta saat Radar pertama kali dibuka. Kalau izin ditolak atau GPS mati, muncul kartu penjelasan dengan tombol **Coba lagi** dan **Main mode Latihan**.

## 3. Aturan spawn

Hasil spawn bersifat deterministik dan tidak butuh server. Semua HP menghitung hasil yang sama dari input yang sama.

- **Petak:** `cellX = floor(lon / 0.0005)` dan `cellY = floor(lat / 0.0005)`. Ukurannya sekitar 55 × 55 m di Indonesia.
- **Jendela waktu:** `window = floor(unixUtcSeconds / 900)`, jadi isi petak berganti setiap 15 menit. Radar menampilkan hitungan mundur ke jendela berikutnya.
- **Seed:** dibuat dari hash `(cellX, cellY, window)` lewat **splitmix64** milik sendiri. Generator acak bawaan sengaja tidak dipakai (`System.Random` dan `UnityEngine.Random`), karena hasilnya tidak dijamin sama di semua platform dan runtime.
- **Jumlah SDA per petak:** 0 (60%), 1 (30%), atau 2 (10%). Rata-ratanya 0,5.
- **Posisi:** titik acak di dalam petak, diambil dari RNG yang sama.
- **Jenis:**

| SDA | id | Peluang | Bisa ditangkap |
|---|---|---|---|
| Matahari | `sun` | 35% | ya |
| Batu Bara | `coal` | 30% | ya |
| Emas | `gold` | 20% | ya |
| Minyak Bumi | `oil` | 12% | belum (terkunci) |
| Uranium | `uranium` | 3% | belum (terkunci) |

- **Aturan siang:** Matahari hanya muncul saat jam lokal HP berada di antara 06:00 dan 17:59. Di luar jam itu, bobot Matahari dihapus dan bobot SDA lainnya dinormalisasi ulang.
  - Catatan: jam lokal ikut menentukan hasil. Pemain di zona waktu yang sama tetap melihat hasil yang sama.
- **ID spawn:** `"{cellX}:{cellY}:{window}:{index}"`.
- **Spawn yang sudah ditangkap:** tidak ditampilkan untuk pemain yang bersangkutan. Pemain lain tetap melihat salinannya sendiri.

**Batasan yang diterima:** memajukan jam HP bisa membuat pemain melompat ke jendela waktu berikutnya. Hal ini tidak dicegah.

## 4. UI Radar (Solar Clean, IMGUI dengan `EnerGoUI`)

- **Latar:** krem. Radar berupa lingkaran putih dengan cincin bertanda 50, 100, dan 150 m, dan garis sapuan yang berputar pelan.
- **Orientasi:** radar berputar mengikuti arah hadap HP, jadi bagian atas layar selalu arah depan pemain. Penanda **U** (utara) kecil ikut berputar.
- **Posisi pemain:** di tengah radar, dengan kerucut kecil yang menunjukkan arah hadap.
- **Titik SDA:** kristal mini berwarna sesuai `crystal_<id>`.
  - SDA terkunci diberi gembok.
  - Uranium berdenyut dan bercahaya.
  - SDA yang sudah dalam jangkauan tangkap (≤ 20 m) membesar dan diberi cincin oranye.
- **Atas:** pil `‹ LOBBY` di kiri, dan pil `Spawn baru mm:ss` di kanan.
- **Pojok:** perkiraan akurasi GPS, misalnya `GPS ±8 m`. Kalau akurasinya lebih dari 30 m, muncul peringatan "Sinyal GPS lemah, coba ke tempat terbuka".
- **Kartu bawah:** menampilkan SDA yang dipilih. Defaultnya SDA terdekat, dan pemain bisa mengetuk titik lain untuk memilihnya. Isi kartu:
  - kristal, nama, jarak, dan arah dalam kata-kata (Utara, Timur Laut, dan seterusnya) beserta panahnya;
  - tombol yang berubah sesuai keadaan:
    - **TANGKAP** (oranye) kalau sudah dalam jangkauan;
    - **DEKATI DULU (82 m)** (nonaktif) kalau masih terlalu jauh;
    - **Segera hadir** (nonaktif) untuk SDA terkunci, bisa diketuk dari jarak berapa pun.
- **Keamanan:** kartu sekali tampil saat pertama kali membuka Radar, berisi "Perhatikan jalan dan sekitarmu saat berburu". Status sudah tampil disimpan di PlayerPrefs.

## 5. GPS dan kompas

- **Sumber data:** `Input.location.Start(desiredAccuracy: 5, updateDistance: 2)` untuk GPS dan `Input.compass` (`trueHeading`) untuk arah hadap. Tidak ada plugin tambahan.
- **Izin:** `FINE_LOCATION` diminta lewat `UnityEngine.Android.Permission`. Unity otomatis menambahkan izin ini ke manifest, karena project belum memakai manifest kustom.
- **Hemat baterai:** GPS dan kompas hanya aktif selama ExploreScene terbuka, dan dimatikan saat pindah scene.
- **Penghalusan posisi:** loncatan lebih dari 50 m dalam 1 detik diabaikan.
- **Penghalusan kompas:** low-pass filter pada sudut melingkar, supaya perpindahan 359° ke 1° tidak membuat radar berputar penuh.
- **Aturan tangkap:** `jarak − min(akurasi, 15) ≤ 20 m`.
- **Jarak dan arah:** pendekatan equirectangular, cukup akurat untuk jarak di bawah 1 km.
- **Simulasi di editor:**
  - Titik awal diatur lewat field inspector berisi latitude dan longitude.
  - **WASD** untuk berjalan sekitar 1,5 m per detik, dan **Q/E** untuk memutar arah hadap.
  - Akurasi simulasi tetap ±5 m.

## 6. Data dan save

- **`ResourceIds`:** ditambah `Oil = "oil"` dan `Uranium = "uranium"`. `Name()` mengenali nama "Minyak Bumi" dan "Uranium", dan `IsPlayable(id)` bernilai *true* hanya untuk `sun`, `coal`, dan `gold`.
- **Inventori tidak berubah:** tetap tiga entri dan validasi save tidak disentuh, sehingga tidak perlu migrasi.
- **`SaveGame.capturedSpawns: string[]`:** daftar ID spawn yang sudah ditangkap. Entri yang jendela waktunya lebih tua dari `window − 1` dibuang setiap kali save di-commit. Save lama otomatis dibaca sebagai array kosong.
- **`CaptureSession.spawnId: string`:** kosong untuk mode Latihan dan save lama.
  - `EnerGoProgress.BeginAnalysis(resourceId, performance, spawnId)`.
  - Saat jawaban terakhir dicommit, ID spawn ditambahkan ke `capturedSpawns` **dalam commit yang sama** dengan pemberian hadiah.
  - Kalau pemain keluar sebelum kuis selesai, spawn tetap ada di radar dan hadiah tidak tercatat dobel.
- **Perpindahan Radar → AR:** `ResourceIds.Selected` dan `ExploreSession.PendingSpawnId` (field statis) diisi, lalu scene AR dimuat (ARPrototype atau Sensor360Scene, mengikuti pengaturan Mode AR).

## 7. Struktur kode

| File | Tugas | Bergantung pada |
|---|---|---|
| `Scripts/GeoSpawner.cs` (baru) | Murni hitungan tanpa API Unity selain `Mathf`. Isinya: petak, jendela waktu, splitmix64, `SpawnsInCell`, `Nearby(lat, lon, radius, utc, localHour, captured)`, jarak dan arah, nama arah mata angin. | Tidak ada |
| `Scripts/LocationFeed.cs` (baru) | `MonoBehaviour` yang menyediakan `Lat`, `Lon`, `Accuracy`, `Heading`, dan `Status` (Starting, Running, Denied, Disabled, Failed). Di HP memakai perangkat asli, di editor memakai simulasi. | Unity Input, Android Permission |
| `Scripts/RadarController.cs` (baru) | UI radar, pemilihan SDA, dan tombol TANGKAP yang memuat scene AR. | GeoSpawner, LocationFeed, EnerGoUI, EnerGoProgress |
| `ExploreScene.unity` (baru) | Kamera dan satu GameObject berisi LocationFeed dan RadarController. Didaftarkan di Build Settings. | Tiga unit di atas |
| `LobbyController.cs` | Kartu menjadi koleksi lima SDA, `MULAI BERBURU` membuka ExploreScene, dan pemilih SDA lama dipindah ke mode Latihan. | |
| `ARCapturePrototype.cs` | Meneruskan `spawnId` ke `BeginAnalysis`, dan tombol LANJUT menuju Radar atau Lobby. | |
| `EnerGoProgress.cs` | Field dan aturan pada bagian 6. | |
| Aset | `crystal_oil.png` dan `crystal_uranium.png` dibuat dari `Branding/logo` (warna dari sistem sumber daya di panduan logo), plus ikon gembok kecil yang digambar dengan `EnerGoUI`. | |

## 8. Penanganan error

- **Izin ditolak, GPS mati, atau GPS gagal start dalam 20 detik:** muncul kartu penjelasan dengan tombol Coba lagi dan Main mode Latihan. Radar tidak menampilkan spawn.
- **Akurasi lebih dari 30 m:** muncul peringatan. Spawn tetap tampil, tapi tombol TANGKAP baru aktif setelah aturan tangkap terpenuhi.
- **Kompas tidak tersedia:** radar dikunci dengan arah utara di atas, dan muncul catatan "Kompas tidak tersedia, utara di atas".
- **Gagal menyimpan:** memakai alur `EnerGoProgress.Error` yang sudah ada, dan pesannya ditampilkan di kartu bawah.

## 9. Pengetesan

- **EditMode tests** (Unity Test Framework 1.6 sudah terpasang) di `Assets/EnerGo/Tests/Editor/GeoSpawnerTests.cs` dengan asmdef khusus tes:
  1. Input yang sama selalu menghasilkan daftar spawn yang identik, termasuk ID dan posisinya.
  2. Pada 100.000 petak, rata-rata 0,5 SDA per petak (±0,01), dan proporsi tiap jenis sesuai tabel (±1 poin persen).
  3. Pada jam lokal 20:00, tidak ada Matahari.
  4. Jarak dan arah antara dua koordinat yang sudah diketahui cocok (toleransi 0,5 m dan 1°).
  5. ID yang ada di `captured` tidak muncul di hasil `Nearby`.
  6. Pembersihan `capturedSpawns` membuang ID yang jendela waktunya lebih tua dari `window − 1`.
- **Di editor:** dengan simulasi WASD, ambil screenshot Radar, lalu jalankan alur Radar → TANGKAP → AR → kuis → LANJUT → Radar, dan pastikan spawn yang ditangkap sudah hilang.
- **Di lapangan, oleh pengguna dengan HP:** akurasi GPS, kestabilan kompas, apakah radius tangkap 20 m terasa pas, dan pemakaian baterai.
