# AR Kompas, Uranium v2, Panduan, dan Matahari Berwaktu: Desain

Tanggal: 2026-10-11 · Status: disetujui per bagian di chat, menunggu review spec

## 1. Tujuan

Pengguna sudah mencoba build Minyak & Uranium di HP. Ada tiga masukan:

- **Uranium terlalu susah, kurang responsif, dan membingungkan.** Masalahnya ada di kedua fase: Cari (menebak arah dari bunyi Geiger) dan Amankan (memiringkan HP untuk batang kendali).
- **Sensor 360 saja kurang terasa AR.** SDA muncul di arah acak, tidak sesuai posisinya di radar, jadi perlu modul kompas.
- **Belum ada petunjuk cara main.** Perlu layar panduan sebelum tiap mini-game, plus tombol panduan di lobby.

Pengguna juga ingin mini-game Matahari diberi batas waktu acak.

**Keputusan pengguna**

- **Kompas:** SDA ditaruh di arah mata angin nyata sesuai bearing dari radar, dan HUD menampilkan pita kompas. Kalibrasinya memakai offset kompas (cara 1), bukan rotation-vector langsung untuk kamera, dan bukan ARCore Geospatial.
- **Matahari tetap seperti sekarang:** muncul di depan pemain dan bergerak acak, tidak di posisi matahari asli. Bedanya, sekarang ada **waktu acak 15–25 detik**. Saat waktu habis, tangkapan berhasil kalau cahaya terkumpul **≥ 50%**. Persennya menentukan bonus.
- **Uranium, fase Cari:** pita kompas menunjukkan sektor kasar, ada indikator RAD besar dengan teks tren, bunyi klik teratur, dan syarat bijih muncul lebih longgar.
- **Uranium, fase Amankan:** batang kendali digerakkan dengan **slider sentuh**, tidak lagi dengan memiringkan HP. Respons reaktivitas lebih cepat dan angkanya lebih ramah.
- **Panduan:** kartu langkah bergambar sebelum mini-game, dengan centang "Jangan ingatkan lagi" per SDA. Ada tombol PANDUAN di lobby.
- **Aset 3D tidak diubah sekarang.** Nanti semua aset dibuat ulang dalam satu tema lewat Blender MCP.

**Asumsi**

- `AndroidRotationVector`, sensor yang dipakai radar dan sudah terbukti di HP pengguna, tersedia di scene AR.
- Jarak tangkap di radar ≤ 20 m, jadi bearing dari pemain ke SDA cukup akurat untuk menentukan arah.

**Kriteria berhasil**

1. **Arah nyata:** SDA yang di radar ada di timur laut muncul di kamera saat pemain menghadap timur laut, dengan selisih sekitar ±20° (batas wajar kompas HP). Objek tidak melompat saat HP diputar.
2. **Kompas hilang:** kalau kompas tidak ada, permainan tetap jalan seperti sekarang, dengan pesan "KOMPAS TIDAK TERSEDIA".
3. **Uranium:** pengguna bisa menyelesaikan Uranium tanpa bingung.
   - Fase Cari selesai rata-rata < 20 detik.
   - Gerakan slider langsung terlihat di gauge.
4. **Matahari:**
   - Matahari selesai saat waktu habis atau saat bar penuh.
   - Hasil < 50% berarti mengulang tanpa penalti.
   - Hasil ≥ 50% memberi bonus sesuai persennya.
5. **Panduan:**
   - Panduan tampil sebelum tiap mini-game, dan selama panduan terbuka mekanik game belum jalan.
   - "Jangan ingatkan lagi" tersimpan per SDA.
   - Tombol di lobby bisa menampilkan panduan lagi dan menghapus semua pilihan itu.
6. Semua logika murni yang baru dites di EditMode.

**Di luar cakupan**

- Model 3D baru.
- Posisi matahari asli.
- Jarak GPS dan efek skala di AR (opsi B yang ditolak).
- ARCore Geospatial.
- Demo animasi di panduan.

## 2. Modul AR Kompas

### Kalibrasi utara

- Kamera tetap memakai sensor yang sekarang (`AttitudeSensor` atau gyro, atau ARCore), karena gerakannya halus.
- Komponen baru **`CompassAnchor`** (MonoBehaviour di scene AR) bertugas:
  - membaca `AndroidRotationVector` dan menghitung arah kompas lewat `GeoSpawner.HeadingFromRotation`, seperti `LocationFeed` di radar
  - memegang transform **`North`**. Semua objek SDA (kecuali Matahari) dipasang di bawah `North`.
- **Perhitungan offset:**
  - Offset target = heading kompas − yaw kamera di dunia Unity (`atan2(fwd.x, fwd.z)`).
  - Offset dihaluskan dengan `GeoSpawner.SmoothHeading`, yang sudah menangani perputaran 359°→0°.
  - Offset **hanya diperbarui** kalau pitch kamera di antara −60° dan +60°, karena saat HP lurus ke atas atau bawah kompas tidak akurat.
- **Yaw `North`** = −offset (yaw dunia untuk bearing b = b − offset). Dengan begitu, arah lokal +Z milik `North` selalu menunjuk ke utara asli. Kalibrasi yang terus diperhalus otomatis menggeser semua objek dengan mulus. Kalibrasi pertama langsung dipakai tanpa penghalusan, dan spawn SDA menunggu kalibrasi pertama (maksimal 1,5 detik) supaya objek tidak melompat.
- **Kalau tidak ada data kompas selama > 2 detik** (di editor, atau sensor diam), `HasCompass` bernilai false, offset tetap di nilai terakhirnya (awalnya 0), dan pita kompas menampilkan "KOMPAS TIDAK TERSEDIA".

### Bearing SDA

- Saat pemain menekan TANGKAP di radar, `RadarController` mengisi **`ExploreSession.PendingBearingDeg`** dengan bearing dari pemain ke SDA (`GeoSpawner.BearingDeg`). Ini dilakukan bersamaan dengan pengisian `PendingSpawnId`.
- **Mode Latihan:** `PendingBearingDeg` bernilai null, dan scene AR memilih bearing acak 0–360°.
- **Arah dunia untuk bearing b** = `North.rotation * (Quaternion.Euler(0, b, 0) * Vector3.forward)`.
- **Penempatan per SDA:**
  - **Batu Bara:** busur bongkahan berpusat di bearing.
  - **Emas:** endapan di bearing.
  - **Minyak:** pompa angguk di bearing.
  - **Uranium:** bijih tersembunyi di bearing.
  - Semua objek ini menjadi child dari `North`.
- **Matahari tidak berubah:** tetap muncul di depan kamera dan bergerak acak, tidak dipasang di `North`.
- **Tombol reset dan ganti mode:** perilaku spawn ulang tetap seperti sekarang, dengan bearing yang sama.

### Pita kompas di HUD

- Strip horizontal di bawah kartu HUD atas, tingginya sekitar 34 px, menampilkan ±60° dari arah pandang. Isinya:
  - tanda **U, TL, T, TG, S, BD, B, BL** setiap 45°
  - garis kecil setiap 15°
  - garis tengah sebagai arah pandang
- **Penanda SDA:** titik oranye di posisi bearing. Kalau bearing di luar ±60°, di ujung pita yang sesuai muncul teks "‹ 75°" atau "75° ›".
- **Uranium:** bukan titik, melainkan **pita sektor** hijau transparan selebar ±45° di sekitar bearing. Kalau sektornya di luar pandang, panah teks di ujung pita mengarahkan ke tepi sektor.
- **Matahari:** hanya tanda arah, tanpa penanda.
- Untuk Batu Bara, Emas dan Minyak, baris lama "PUTAR KE KIRI/KANAN …°" diganti oleh pita ini. Teks status lain tetap ada.
- Arah pandang di pita memakai heading kamera = yaw kamera + offset, jadi pita konsisten dengan posisi objek.

### Fungsi murni (EnerGo.Core, `CompassMath`)

- `float CameraHeading(float cameraYawDeg, float offsetDeg)`: menghasilkan nilai 0–360.
- `float OffsetTarget(float compassHeadingDeg, float cameraYawDeg)`
- `bool CanCalibrate(float cameraPitchDeg)`: true kalau |pitch| ≤ 60°.
- `float StripX(float bearingDeg, float headingDeg, float halfSpanDeg)`: menghasilkan −1..1, atau di luar rentang itu kalau penanda ada di luar pita. Perhitungannya memakai selisih sudut bertanda yang menangani perputaran 359°→0°.
- `string DirectionLabel(int deg45)`: menghasilkan U/TL/T/TG/S/BD/B/BL.

## 3. Matahari Berwaktu

- **Saat Matahari muncul**, batas waktu diacak 15–25 detik dengan `SunTimeLimit(u01)`. Mekanik membidiknya tetap sama: cahaya dijaga di dalam lingkaran, bar naik saat di dalam dan turun saat di luar.
- **HUD** menampilkan sisa waktu, misalnya "SISA 12 DTK", di kartu HUD atas bersama bar persen. Di 5 detik terakhir, warnanya berubah oranye.
- **Bar 100% sebelum waktu habis:** langsung selesai dengan performa 1 (Hebat).
- **Waktu habis:**
  - Kalau persen ≥ 50%, tangkapan berhasil. Performa = `InverseLerp(0.5, 1, persen)`, lalu `BonusFor` yang sudah ada memberi hasil:
    - 50–69% = Cukup (+0)
    - 70–87% = Bagus (+1)
    - ≥ 88% = Hebat (+2)
  - Kalau persen < 50%, muncul "CAHAYA TERLALU SEDIKIT" selama sekitar 1,5 detik. Matahari lalu muncul lagi dengan waktu acak baru dan bar 0, tanpa penalti.
- Skor kecepatan Matahari yang lama (`SpeedScore` dengan `SunParSec`) diganti dengan perhitungan persen di atas.
- **Fungsi murni** (EnerGo.Core, `SunMath`):
  - `SunTimeLimit(u01)` menghasilkan 15–25.
  - `SunOutcome(progress, timeLeft)` menghasilkan None, Captured atau TooLittle.
  - `SunPerformance(progress)`
- Konstanta ditaruh di atas file: `MinSec = 15`, `MaxSec = 25`, `MinShare = 0.5`.

## 4. Uranium v2

### Fase Cari

- **Bearing dan pita:** bijih ditaruh di bearing nyata, pada jarak 1,6 m dan pitch acak −5° sampai +15°. Pita kompas menunjukkan sektor ±45°.
- **Indikator RAD besar:**
  - Posisinya di tengah bawah, di atas tombol reset: angka 0–100 dengan bar berwarna gradasi abu-abu → hijau neon (`#39FF14`).
  - Di bawahnya ada teks tren:
    - **"SINYAL MENGUAT"** kalau RAD naik ≥ 3 poin dalam 0,3 detik terakhir
    - **"SINYAL MELEMAH"** kalau turun ≥ 3 poin
    - kosong kalau stabil
- **Klik Geiger teratur:** jedanya 1/rate, dengan rate dari 2 klik per detik (sudut ≥ 150°) sampai 20 klik per detik (sudut 0°), interpolasi linear terhadap sudut. Klik acak eksponensial yang lama dihapus. Getaran ikut setiap klik, maksimal 12 per detik.
- **Reticle bidik** berupa lingkaran di tengah layar. Saat sudut ≤ 20°, reticle menyala hijau dan berdenyut.
- **Syarat muncul:** sudut ≤ **20°** selama **0,8 detik** berturut-turut.
- **Fungsi murni (UraniumMath):**
  - `GeigerInterval(angleDeg)`: jeda antar klik (detik)
  - `Trend(prevMeter, meter)`: menghasilkan −1, 0 atau +1, dengan ambang 3

### Fase Amankan

- **Slider batang kendali** ada di sisi kanan layar: trek vertikal lebar sekitar 64 px dengan kenop besar.
  - Menyentuh atau menggeser di area trek langsung menaruh batang di posisi jari.
  - Atas = batang ditarik keluar (`rod = 0`, reaktivitas naik). Bawah = batang masuk penuh (`rod = 1`, reaktivitas turun).
  - Di editor: drag mouse di trek, atau W/S.
- **Gauge reaktivitas** ada di sebelah kiri slider, dengan zona hijau dan zona merah.
- **Angka yang diubah (UraniumMath):**

  | Konstanta | Lama | Baru |
  |---|---|---|
  | `RodPull` | 0,9 | **3,0** |
  | Zona hijau | 0,40–0,60 | **0,35–0,65** |
  | `FillPerSec` | 1/8 | **1/6** |
  | `DrainPerSec` | 1/16 | **1/12** |
  | `DosePerSec` | 1/35 | **1/45** |
  | Interval lonjakan | 3–6 dtk | **4–7 dtk** |
  | `SpikeWarnSec` | 0,5 | **0,8** |
  | `SpikeSize` | 0,20 | tetap 0,20 |

  `Growth`, guncangan (`JoltGain` dan deadzone) dan `HotDoseMul` tidak berubah.
- **Kemiringan HP tidak lagi dipakai.** `TiltDeg` dan `RodFromTilt` dihapus beserta tesnya. Akselerometer hanya dipakai untuk menghitung guncangan.
- **Teks HUD menyebut aksi jari:**
  - "TERLALU PANAS · GESER BATANG KE BAWAH"
  - "REAKSI MELEMAH · GESER BATANG KE ATAS"
  - "STABIL · TAHAN HP"
  - "LONJAKAN NEUTRON!"
- **Fungsi murni:** `RodFromSlider(float pointerY01)` mengubah posisi jari di trek (0 = atas, 1 = bawah) menjadi nilai rod 0..1, dengan clamp.
- **Tidak berubah:** MELTDOWN, DOSIS BERLEBIH, retry tanpa penalti, perhitungan performa, dan kuis.

## 5. Panduan

### Data

**`GuideBook`** (EnerGo.Core) memetakan id SDA ke daftar kartu. Tiap kartu berisi `title`, `body` (1–2 kalimat) dan `art`, yang merupakan enum ilustrasi berikut:

- `Reticle`
- `Compass`
- `Tap`
- `PhoneFlat`
- `PhoneSwirl`
- `Hold`
- `Layers`
- `PumpRing`
- `RadMeter`
- `Slider`
- `Shake`

| SDA | Kartu |
|---|---|
| Matahari | 1. **Kejar cahaya**: arahkan lingkaran ke cahaya matahari (Reticle) · 2. **Kumpulkan sebelum waktu habis**: minimal 50%, makin penuh makin banyak bonus (Reticle) |
| Batu Bara | 1. **Ikuti kompas**: hadap ke titik oranye di pita kompas (Compass) · 2. **Pecahkan bongkahan**: ketuk tiap bongkahan sampai hancur (Tap) |
| Emas | 1. **Ikuti kompas** (Compass) · 2. **Letakkan HP datar** sampai hitungan mulai (PhoneFlat) · 3. **Putar pelan seperti mendulang**: terlalu kencang, emas tumpah (PhoneSwirl) |
| Minyak | 1. **Tahan untuk mengebor** (Hold) · 2. **Lepas di lapisan minyak**: pelan-pelan di kantong gas (Layers) · 3. **Pompa ikuti lingkaran**: tekan saat lingkaran pas (PumpRing) |
| Uranium | 1. **Hadap ke sektor hijau, ikuti angka RAD** (RadMeter) · 2. **Geser batang kendali**: jaga jarum di zona hijau (Slider) · 3. **Pegang HP stabil**: waspadai lonjakan, dosis terus naik (Shake) |

Ilustrasi digambar dari kode dengan `EnerGoUI.DrawPanel` dan `DrawCard`, berupa bentuk sederhana dari palet Solar Clean. Fungsi `DrawGuideArt(Rect, art)` ditaruh di `GuideViewer.cs` dan dipakai bersama oleh scene AR dan lobby.

### Overlay sebelum mini-game (scene AR)

- **Kapan tampil:** saat scene AR dibuka, kalau `pendingCapture == null` **dan** pemain belum memilih "jangan ingatkan" untuk SDA ini.
- **Selama overlay terbuka:**
  - Spawn SDA ditahan, jadi Update tidak memanggil `SpawnGold`.
  - Input mini-game diabaikan.
  - Kalibrasi kompas boleh tetap jalan di belakang, supaya utara sudah siap saat MULAI.
- **Tampilan:** kartu besar di tengah layar yang bisa digeser ke kiri/kanan (swipe, atau ketuk sisi kiri/kanan), dengan titik halaman di bawahnya. Di bawahnya ada centang **"Jangan ingatkan lagi"** dan tombol **MULAI** di kartu terakhir. Di kartu sebelumnya, tombolnya **LANJUT**, dan ada tombol kecil **LEWATI** untuk langsung mulai.
- **MULAI:**
  - Kalau dicentang, `PlayerPrefs EnerGo.Guide.Skip.<id>` diisi 1.
  - Overlay ditutup dan spawn dimulai, dengan `nextSpawnTime = Time.time`.
  - Timer Matahari baru berjalan setelah MULAI.
- **Tombol ‹ LOBBY/RADAR** tetap berfungsi selama overlay terbuka.

### Lobby

- Tombol **PANDUAN** ditaruh di sebelah tombol SETTING. Gayanya tombol sekunder dengan ukuran sama.
- Tombol ini membuka **bottom sheet "Panduan"** berisi kartu 5 SDA dalam grid 2 kolom, gayanya sama dengan koleksi.
  - Mengetuk satu SDA membuka viewer kartu yang sama dengan scene AR, dengan tombol **TUTUP** saja, tanpa centang dan tanpa MULAI.
  - Di bawah grid ada tombol **"TAMPILKAN SEMUA PANDUAN LAGI"**, yang menghapus semua kunci `EnerGo.Guide.Skip.*` dan menampilkan konfirmasi singkat "Panduan akan muncul lagi".
- Logika viewer kartu dipakai bersama lewat class kecil **`GuideViewer`**: plain C#, menyimpan halaman aktif dan status centang, lalu menggambar dengan IMGUI. Scene AR dan lobby masing-masing punya satu instance.

### Error dan edge case

- **PlayerPrefs gagal tulis:** panduan hanya akan muncul lagi, tidak ada data yang hilang.
- **Melanjutkan kuis tertunda:** overlay tidak tampil.
- **SDA tanpa entri di `GuideBook`:** overlay tidak tampil. Tes memastikan semua SDA yang playable punya entri.

## 6. Integrasi dan file

- **Baru (Core):** `CompassMath.cs`, `SunMath.cs`, `GuideBook.cs`
- **Diubah (Core):** `UraniumMath.cs` (angka baru, interval teratur, slider, tren; fungsi tilt dihapus), `ExploreSession.cs` (`PendingBearingDeg`)
- **Baru (Assembly-CSharp):** `CompassAnchor.cs`, `GuideViewer.cs` (berisi juga `DrawGuideArt`)
- **Diubah:**
  - `ARCapturePrototype.cs`: spawn dengan bearing di bawah `North`, pita kompas di `DrawRadarHUD`, gate overlay panduan, Matahari berwaktu
  - `ARCapturePrototype.Uranium.cs`: fase Cari dan slider
  - `ARCapturePrototype.Oil.cs`: penempatan di bearing
  - `RadarController.cs`: mengisi `PendingBearingDeg`
  - `LobbyController.cs`: tombol dan sheet Panduan
- **Batu Bara dan Emas:** hanya titik pusat spawn yang diganti ke bearing. Mekanik dan gaya visualnya tidak diubah.

## 7. Tes

**EditMode**
- **`CompassMath`:**
  - offset dan heading kamera menangani perputaran 359°→0°
  - `CanCalibrate` di batas ±60°
  - `StripX` tepat di tengah, di tepi ±60°, di luar pita, dan saat melewati utara (heading 350°, bearing 10° → kanan)
  - `DirectionLabel` untuk 8 arah
- **`SunMath`:**
  - `SunTimeLimit` di rentang 15–25
  - `SunOutcome`: bar penuh → Captured, waktu habis dengan ≥ 50% → Captured, waktu habis dengan < 50% → TooLittle, masih ada waktu → None
  - `SunPerformance` pada 50%, 70% dan 88% sesuai tingkat `BonusFor`
- **`UraniumMath` v2:**
  - `GeigerInterval` monoton dan berada di rentang 1/20–1/2 detik
  - `Trend` dengan ambang 3
  - `RodFromSlider` dengan clamp
  - ekuilibrium rod 0,5 berada di zona hijau yang baru
  - rod penuh mendinginkan reaktor lebih cepat daripada versi lama (respons dalam < 1 detik)
  - nilai konstanta yang baru
  - tes tilt dihapus
- **`GuideBook`:** setiap SDA yang playable punya 2–3 kartu, judul dan teks tidak kosong, dan tidak ada glyph yang tidak ada di Poppins (emoji, ☢, ←, →, ✓, ✗).

**Editor (reflection dan screenshot)**
- overlay panduan menahan spawn, dan MULAI memulai mini-game
- "Jangan ingatkan lagi" tersimpan, dan reset di lobby menghapusnya
- pita kompas tampil dengan penanda, dengan sektor untuk Uranium
- slider Uranium dan indikator RAD
- HUD Matahari menampilkan sisa waktu

**Perangkat (pengguna)**
- arah SDA cocok dengan radar dan arah mata angin
- Uranium terasa jelas dan responsif
- Matahari berwaktu terasa seru
- panduan mudah dipahami
