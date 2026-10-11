# Mini-game Minyak Bumi & Uranium: Desain

Tanggal: 2026-10-11 · Status: disetujui per bagian di chat, menunggu review spec

## 1. Tujuan

Minyak Bumi dan Uranium sudah muncul di radar dan lobby, tetapi masih terkunci karena belum punya mini-game tangkap. Spec ini menambahkan satu mini-game untuk masing-masing. Keduanya harus lebih seru dan lebih menantang daripada versi GDD 3.2 ("tahan HP diam" dan "tekan-lepas ikut irama"), tetapi tetap mengajarkan sesuatu yang benar tentang SDA tersebut.

**Keputusan pengguna**

- Uranium memakai **"Pemburu Geiger"** dalam dua fase. Fase pertama mencari bijih yang tidak terlihat lewat bunyi Geiger dan getaran. Fase kedua mengamankan bijih dengan batang kendali, ditambah lonjakan neutron acak dan meter dosis.
- Minyak Bumi memakai **"Bor & Pompa"**. Pemain mengebor menembus lapisan bumi dan berhenti tepat di lapisan minyak, lalu memompa mengikuti irama.
- Kalau gagal, deposit baru dibuat dan pemain mengulang dari fase pertama **tanpa penalti**. Pola ini sama dengan Emas.
- Arsitektur memakai pendekatan 1: file `partial` per mini-game. Logika murninya ditaruh di `EnerGo.Core` supaya bisa dites.
- Keduanya dikerjakan sekaligus dalam satu spec.

**Asumsi**

- Model 3D dan SFX khusus belum ada, jadi dipakai placeholder primitif dan bunyi yang dibuat lewat kode. Model asli bisa ditaruh di `Resources/Uranium` dan `Resources/Oil`.
- Soal kuis ditulis sekitar 30 per SDA, dengan gaya dan sumber yang sama seperti soal yang sudah ada.
- HP target adalah Android, yaitu HP pengguna. Di HP itu gyro dan rotation-vector sudah terbukti jalan.

**Kriteria berhasil**

1. Minyak dan Uranium bisa ditangkap dari radar maupun mode Latihan. Alurnya: mini-game, lalu kuis 3 soal, lalu reward masuk inventori, lalu kembali ke radar atau lobby.
2. Uranium bisa ditemukan hanya dengan mendengar atau merasakan getaran, tanpa melihat bijihnya. Indikator ☢ di HUD tersedia sebagai bantuan.
3. Setiap mode gagal (meltdown, dosis berlebih, blowout, kena air, overpressure) memberi pesan jelas, lalu deposit baru dibuat.
4. Save lama (berisi 3 SDA) tetap terbaca tanpa kehilangan progres.
5. Semua logika inti dites di EditMode, dan kedua mini-game bisa dimainkan penuh di Unity Editor tanpa sensor.

**Di luar cakupan**

- Model 3D dan SFX final.
- Tingkat Bintang (★) dan modul Alat Penangkap.
- Mekanik PLTN atau kota.
- Refactor mini-game Matahari, Batu Bara dan Emas.

## 2. Uranium: "Pemburu Geiger"

### Fase 1: Cari

- Saat scene AR dibuka, bijih ditaruh di arah tersembunyi:
  - yaw acak 60–150° ke kiri atau kanan dari arah awal kamera
  - pitch acak −10° sampai +15°
  - bijih belum dirender
- Kedekatan dihitung dari sudut `θ` antara arah depan kamera dan arah bijih.
  - `GeigerRate(θ)` memberi jumlah klik per detik. Nilainya sekitar 1/s saat `θ ≥ 150°`, dan naik eksponensial sampai sekitar 25/s saat `θ ≈ 0`.
  - Tiap klik diberi jitter acak supaya terdengar seperti peluruhan radioaktif, bukan metronom.
- Umpan balik ke pemain:
  - **Bunyi klik:** dibuat dari noise pendek (sekitar 4 ms) lewat `AudioClip.Create`, lalu dimainkan dengan PlayOneShot.
  - **Getaran:** pulsa pendek per klik, maksimal sekitar 12 pulsa per detik supaya tidak menyatu.
  - **Indikator HUD:** "☢ 0–100", nilainya `100 · (1 − θ/180)`.
- Kalau `θ ≤ 10°` bertahan selama 1,5 detik berturut-turut, bijih **muncul** dengan pendar hijau neon dan skala pop. Setelah itu otomatis masuk ke fase 2. Kalau keluar dari 10° sebelum 1,5 detik, timer direset.

### Fase 2: Amankan

Bijih masuk ke wadah timbal. HUD menampilkan tiga hal:
- gauge **reaktivitas** vertikal (0–1) dengan zona hijau 0,40–0,60
- bar **dosis**
- bar **progres**

Reaktivitas `r` dihitung per frame dengan `StepReactivity(r, rod, jolt, spike, dt)`:

- **Pertumbuhan alami:** `+Growth · dt`. Tanpa batang kendali, reaksi berantai cenderung naik.
- **Batang kendali:** `rod` bernilai 0–1. Nilainya diambil dari kemiringan HP ke depan, yaitu komponen gravitasi pada sumbu layar, dan sudah diberi smoothing. Batang menarik `r` ke arah `1 − rod` dengan laju `RodPull`.
- **Noise:** noise Perlin kecil.
- **Lonjakan neutron:** setiap 3–6 detik secara acak `r` naik +0,20. Sekitar 0,5 detik sebelumnya ada kedipan peringatan oranye di gauge.
- **Guncangan:** `jolt` diambil dari akselerometer (cara bacanya sama dengan Emas, `ReadGoldTilt`), dan menambah `r` sebesar `jolt · JoltGain`.
- Hasilnya di-clamp ke 0–1.

Progres dan dosis:
- **Progres:** `StepUraniumProgress` naik dengan laju `1/8` per detik selama `r` di zona hijau, dan turun dengan laju `1/16` per detik di luar zona.
- **Dosis:** `StepDose` naik dengan laju `1/35` per detik, dan 2× lebih cepat saat `r > 0,80`.
- Kalau `r = 0`, reaksi "padam". Progres tidak naik, tetapi ini tidak dihitung gagal.

Kontrol di editor: W/S, atau drag vertikal dengan mouse, untuk menggeser `rod`.

### Hasil

- **Berhasil:** progres mencapai 1. Lanjut ke `CompleteCapture()`, lalu kuis. Performa dihitung dengan `SpeedScore(elapsed, UraniumPar)` yang digabung dengan sisa dosis.
- **Gagal:** `r ≥ 1` memberi pesan **"MELTDOWN!"**, dan dosis ≥ 1 memberi pesan **"DOSIS BERLEBIH!"**. Pesan tampil sekitar 1,5 detik, lalu `RetryUranium()` dijalankan: arah tersembunyi baru dipilih dan pemain mulai lagi dari fase 1.

## 3. Minyak Bumi: "Bor & Pompa"

### Fase 1: Bor

Panel **penampang bumi** ditampilkan sebagai overlay 2D, dengan gaya kartu seperti panci Emas. Lapisan dari atas ke bawah:

| Lapisan | Kecepatan bor | Catatan |
|---|---|---|
| Tanah | ×1,0 | |
| Batuan | ×0,7 | |
| Batuan penutup | ×0,35 | keras dan lambat |
| Kantong gas | ×1,2 | ada jarum tekanan |
| **Minyak** | ×0,8 | target, pita tipis |
| Air asin | ×1,0 | kalau sampai di sini, gagal |

- `OilMath.Layers(seed)` mengacak ketebalan tiap lapisan secara deterministik dari seed. Ketebalan minyak sekitar 6–9% dari total kedalaman.
- `StepDrill(depth, speed, held, layerMul, dt)` mengatur gerak bor:
  - Selama layar ditahan, kecepatan naik pelan (`Accel`) sampai `MaxSpeed · layerMul`.
  - Saat dilepas, bor melambat dengan `Brake`. Momentum ini membuat bor bisa kebablasan.
- `StepGasPressure` hanya berlaku di kantong gas:
  - Tekanan naik sebanding dengan kecepatan bor di atas `SafeGasSpeed`.
  - Tekanan turun pelan kalau bor lambat.
  - Kalau tekanan ≥ 1, terjadi **"BLOWOUT!"** dan tangkapan gagal.
- Dua kondisi lain:
  - Kalau bor berhenti (`speed ≈ 0`) di dalam lapisan minyak, minyak ditemukan dan pemain masuk ke fase 2.
  - Kalau `depth` masuk lapisan air asin, muncul **"KENA AIR!"** dan tangkapan gagal.
- Berhenti di lapisan lain tidak apa-apa. Pemain bisa menahan layar lagi untuk lanjut.
- Di editor: tahan tombol mouse atau Spasi.

### Fase 2: Pompa

- **Pompa angguk** placeholder muncul di posisi target AR. Bentuknya 3–4 primitif: alas, tiang, balok angguk, dan kepala kuda. Baloknya mengangguk mengikuti input pemain.
- Irama dasarnya `Beat = 1,2` detik. Ketukan "dug" dimainkan dengan `clipBatuJatuh` pitch 0,6 sebagai placeholder. Cincin penanda mengecil menuju setiap ketukan.
- Pemain **tekan** untuk langkah turun dan **lepas** untuk langkah naik. Satu langkah dihitung saat layar ditekan.
- `JudgeStroke(t, lastBeatTime, beat)` mengembalikan `Perfect` kalau |selisih| ≤ 0,2 detik, `Early` kalau langkahnya terlalu cepat, dan `Late` kalau terlambat.
  - **Perfect:** drum bertambah +0,1.
  - **Early:** tekanan +0,25.
  - **Late:** langkah tidak dihitung.
- Kalau lebih dari 2 detik tanpa langkah, aliran berhenti dan drum turun dengan laju `1/20` per detik.
- `StepOilPressure` membuat tekanan turun pelan secara alami. Kalau tekanan ≥ 1, terjadi **"OVERPRESSURE!"** dan tangkapan gagal.
- Saat drum di atas 70%, `beat` dikali 0,85 supaya ada klimaks.

### Hasil

- **Berhasil:** drum mencapai 1. Lanjut ke `CompleteCapture()`, lalu kuis. Performa dihitung dari `SpeedScore` yang digabung dengan rasio langkah Perfect.
- **Gagal:** pesan tampil sekitar 1,5 detik, lalu `RetryOil()` dijalankan: seed baru dibuat (lapisan diacak ulang) dan pemain mulai lagi dari fase Bor.

## 4. Integrasi

### File baru

- `Assets/EnerGo/Scripts/Core/UraniumMath.cs` berisi `GeigerRate`, `StepReactivity`, `StepUraniumProgress` dan `StepDose`, beserta konstanta tuning.
- `Assets/EnerGo/Scripts/Core/OilMath.cs` berisi `Layers(seed)`, `LayerAt(depth, layers)`, `StepDrill`, `StepGasPressure`, `JudgeStroke` dan `StepOilPressure`, beserta konstanta tuning.
- `Assets/EnerGo/Scripts/ARCapturePrototype.Uranium.cs` berisi sisi Unity untuk uranium: arah tersembunyi, penjadwalan klik dan getaran, model, gauge HUD, dan alur fase serta retry.
- `Assets/EnerGo/Scripts/ARCapturePrototype.Oil.cs` berisi sisi Unity untuk minyak: panel penampang, pompa angguk, cincin irama, dan alur fase serta retry.
- `Assets/EnerGo/Tests/Editor/UraniumMathTests.cs`, `OilMathTests.cs` dan `QuizBankTests.cs`. Kalau `QuizBank` tidak bisa dijangkau dari assembly tes, `QuizBankTests` diganti cek editor sekali jalan.

### Perubahan file yang ada

- **`ARCapturePrototype.cs`**
  - Class dijadikan `partial`.
  - Guard di `Awake` diganti menjadi `if (!ResourceIds.IsPlayable(resourceId)) resourceId = ResourceIds.Gold;`.
  - Ditambah sekitar 6 titik dispatch dengan pola `if (resourceId == …)` yang sudah ada: spawn, Update, input tap/hold, OnGUI overlay, nilai par, dan retry.
  - `MiniGameName` ditambah "Pemburu Geiger" dan "Bor & Pompa".
- **`ResourceIds.IsPlayable`** sekarang memasukkan `Oil` dan `Uranium`. `ResourceIdsTests` diperbarui.
- **`EnerGoProgress`**
  - Inventori default menjadi 5 entri.
  - `Valid` menerima inventori 3 atau 5 entri: Sun, Coal dan Gold wajib ada, sedangkan Oil dan Uranium opsional dan masing-masing maksimal satu.
  - Setelah save terbaca, entri yang hilang ditambahkan dengan jumlah 0 (migrasi). Ini supaya save lama tidak ter-reset.
- **`LobbyController`** menghapus gembok untuk minyak dan uranium. Ini otomatis terjadi lewat `IsPlayable`, tinggal dicek jumlah koleksinya tampil.
- **`RadarController`** otomatis ikut lewat `IsPlayable`. Tidak ada perubahan logika.
- **`QuizBank`** ditambah sekitar 30 soal minyak dan sekitar 30 soal uranium, sekitar sepertiganya bernilai salah.
  - Sumber: EIA (oil, nuclear), World Nuclear Association, dan ESDM/BATAN-BRIN untuk konteks Indonesia.
  - ID baru hanya ditambahkan di akhir, tidak ada renumber.
- **`SFXManager`**
  - Klik Geiger dibuat lewat kode: `PlayGeigerClick()`.
  - Ditambah `PlayPumpBeat()` yang memakai `clipBatuJatuh` dengan pitch rendah.
- **Getaran:** helper kecil `Haptics.Pulse(ms)`.
  - Di Android memakai `Vibrator.vibrate` lewat `AndroidJavaObject`, ditaruh dalam try/catch.
  - Fallbacknya `Handheld.Vibrate`, maksimal sekali per 0,5 detik.
  - Di editor tidak melakukan apa-apa.

### Placeholder visual

- **Bijih uranium:** icosphere atau primitif dengan material unlit hijau neon `#39FF14`. Pendarnya berdenyut lewat warna dan skala.
- **Pompa angguk:** primitif berwarna Ink dan Orange dari palet Solar Clean.
- **Panel penampang dan gauge:** menggambar dengan `EnerGoUI` (`DrawCard`, `DrawBar`) sesuai palet.

### Error dan edge case

- **Sensor tidak tersedia:** dipakai fallback keyboard dan mouse, dan jolt dianggap 0.
- **App di-pause di tengah mini-game:** timer memakai `Time.time`, dan dosis tidak naik selama pause karena `Update` tidak jalan.
- **Save dengan `pendingCapture` oil/uranium:** valid setelah `IsPlayable` diperbarui.
- **Vibrator gagal atau tidak ada izin:** exception ditangkap dan getaran tidak dipakai. `VIBRATE` adalah izin normal dan cukup ditambahkan di manifest.

## 5. Tes

**EditMode**
- **`UraniumMath`:**
  - `GeigerRate` monoton turun terhadap `θ`
  - batang kendali penuh menurunkan `r`
  - lonjakan dan guncangan menaikkan `r`
  - progres naik hanya di zona hijau
  - dosis 2× lebih cepat saat `r` di atas 0,80
- **`OilMath`:**
  - `Layers(seed)` deterministik dan urutannya benar
  - `LayerAt` mengembalikan lapisan yang tepat di batas
  - bor punya momentum (tidak langsung berhenti)
  - tekanan gas naik hanya saat bor cepat
  - `JudgeStroke` mengembalikan Perfect, Early dan Late dengan benar
- **`EnerGoProgress`:** save 3 entri dimigrasi menjadi 5 entri tanpa mengubah jumlah yang sudah ada. Kalau tidak bisa dites dari assembly tes, dicek manual di editor.
- **`ResourceIds`:** oil dan uranium sekarang playable.
- **`QuizBank`:** oil dan uranium masing-masing punya minimal 3 soal, dan tidak ada ID duplikat.

**Manual dan perangkat**
- Kedua mini-game dimainkan penuh di editor, termasuk tiap mode gagal.
- Build APK, lalu pengguna mencoba di HP untuk tuning konstanta. Konstanta ada di atas `UraniumMath` dan `OilMath`.
