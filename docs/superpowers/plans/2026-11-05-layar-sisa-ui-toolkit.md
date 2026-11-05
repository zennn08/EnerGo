# Layar sisa ke UI Toolkit: Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Semua layar yang masih IMGUI (radar eksplorasi, HUD tangkap, kuis, overlay mini-game, panduan pra-main) pindah ke UI Toolkit dengan gaya Jurnal Ekspedisi, tanpa mengubah aturan game. Setelah selesai, `OnGUI` dan `EnerGoUI` hilang dari project.

**Architecture:** Pola yang sama dengan Lobby: pengendali (`RadarController`, `ARCapturePrototype`) mengekspos state dan aksi publik, View baru di `Assets/EnerGo/Scripts/UI/` membaca state tiap frame dan menulis ke elemen UXML. Input permainan yang memakai pointer mentah (ketuk batu bara, tahan bor, JEPIT, geser kartu) tetap dibaca pengendali; elemen UI non-tombol `pickingMode = Ignore` supaya tidak menghalanginya. Model 3D (dulang, penampang bor, wadah) tetap di tempatnya.

**Tech Stack:** Unity 6000.3.24f1, UI Toolkit (UXML/USS, Painter2D), Input System, NUnit EditMode.

**Spec:** tidak ada spec terpisah. Acuan isinya `docs/PRD-EnerGo.md` (3.x), acuan tampilannya `docs/design-system/components/` (`LayarRadar`, `Radar`, `KartuTarget`, `HudTangkap`, `PitaKompas`, `LingkaranBidik`, `LayarTangkapA`, `LayarTangkapB`, `TombolJepit`, `LayarKuis`, `KartuSoal`, `KartuUmpanBalik`, `Kartu`). Contoh polanya `docs/superpowers/specs/2026-11-03-lobbyscene-ui-toolkit-design.md` dan `Assets/EnerGo/Scripts/UI/LobbyView*.cs`.

## Global Constraints

- Jangan ubah aturan, angka, waktu, atau urutan fase game. Yang pindah hanya tampilan dan tombol.
- Bahasa UI: Bahasa Indonesia. Teks status disalin persis dari kode lama, kecuali README design system menuliskan teks lain.
- Satuan UI = unit dasar 480 x 800 (`Assets/UI/PanelSettings.asset`). Nama variabel USS = nama token di `tokens.json`.
- Teks di atas kamera AR selalu di atas kartu atau pil `kertas-100`.
- USS tidak punya box-shadow, clip-path, gradient, outline: pakai garis bawah tebal, seperti `Components.uss`. `var()` tidak boleh di style inline UXML. Pil memakai radius setengah tinggi, bukan 999.
- Tes hanya bisa mereferensikan `EnerGo.Core`, jadi logika yang diuji ditaruh di `Assets/EnerGo/Scripts/Core`.
- Jangan build APK kecuali diminta. Commit di branch `feat/ui-jurnal-layar`, push hanya kalau diminta.
- Uji play mode lewat MCP: waktu editor beku, jadi gerakkan state lewat refleksi. Cadangkan save dulu, jangan biarkan mini-game mencapai selesai, pulihkan save dan buka lagi ExploreScene sesudahnya.

## Review Focus

1. Ketukan pada tombol HUD (‹ RADAR, MUNCULKAN ULANG TARGET, mode kamera) tidak boleh ikut mengetuk batu bara atau endapan emas, dan elemen non-tombol tidak boleh memakan ketukan permainan. Dites di Task 2 (`HudTapZones`).
2. Area JEPIT yang dilihat harus sama dengan area sentuh `UraniumGripRect`, termasuk di layar lebih pendek dari 800 unit, dan menahan JEPIT tetap memblokir geser layar. Dites di Task 4.
3. Kartu kuis yang digeser di HP ber-notch: ambang 90 unit dihitung dalam unit panel, bukan piksel, dan tombol BENAR/SALAH tidak menjawab dua kali. Dites di Task 3.
4. Kembali dari tangkapan yang belum dijawab (`pendingCapture` terisi): scene dibuka langsung ke kuis, tanpa tombol kembali dan tanpa guide. Dites di Task 3 (`CaptureScreens`).
5. Radar saat GPS gagal, izin ditolak, kompas tidak ada, dan belum ada SDA di sekitar: tiap keadaan punya teks dan tombolnya sendiri, dan tirai keselamatan memblokir semua tombol radar sampai SAYA MENGERTI ditekan. Dites di Task 1.

---

### Task 1: Radar eksplorasi

**Files:**
- Create: `Assets/UI/Screens/Radar.uxml`, `Assets/EnerGo/Scripts/UI/RadarView.cs`, `Assets/EnerGo/Scripts/UI/RadarDisk.cs`, `Assets/EnerGo/Scripts/Core/RadarText.cs`
- Modify: `Assets/EnerGo/Scripts/RadarController.cs` (hapus `OnGUI` dan `Draw*`), `Assets/UI/Screens/Layar.uss`, `Assets/EnerGo/Editor/LobbySceneBuilder.cs`, `Assets/EnerGo/ExploreScene.unity`
- Test: `Assets/EnerGo/Tests/Editor/RadarTextTests.cs`, `Assets/EnerGo/Tests/Editor/LobbyUxmlTests.cs`

**Interfaces:**
- Produces: `public static GameObject LobbySceneBuilder.InstallUi(string uxmlPath, System.Type viewType, Component controller)`, dipakai Task 2. Isinya digeneralisasi dari `InstallLobbyUi`, dan `InstallLobbyUi` memanggilnya.
- Produces: `RadarText.Countdown(long secs)`, `RadarText.GpsLine(float accuracyM, float weakM)`, `RadarText.CompassLine(bool has, string source, float deg)`, `RadarText.Direction(double bearing)`, `RadarText.Distance(double m)`, `RadarText.CaptureButton(bool locked, bool inRange, double m, double reach, out bool enabled)`.

- [ ] **Step 1: Tulis tes yang gagal.** `RadarTextTests`: `Countdown(125) == "SPAWN BARU 02:05"`; `GpsLine(12, 30) == "GPS ±12 m"`; `GpsLine(45, 30) == "GPS ±45 m · Sinyal lemah, coba ke tempat terbuka"`; `CompassLine(false, "", 0) == "Kompas tidak tersedia, utara di atas"`; `Direction(45) == "Timur Laut"`; `CaptureButton` memberi `"TANGKAP"` enabled, `"DEKATI DULU (14 m)"` disabled, `"SEGERA HADIR"` disabled. `LobbyUxmlTests`: `Radar.uxml` memuat `btn-kembali`, `pil-spawn`, `radar-disk`, `info-kompas`, `info-gps`, `kartu-target`, `btn-tangkap`, `kartu-gps`, `btn-coba-lagi`, `btn-latihan-gps`, `lapis-aman`, `btn-mengerti`.
- [ ] **Step 2: Jalankan, pastikan gagal.** Run: `bash .superpowers/tmp/run-tests.sh`. Expected: COMPILE FAILED karena `RadarText` belum ada.
- [ ] **Step 3: Implement `RadarText`**, teks disalin dari `RadarController` lama dan README `KartuTarget`.
- [ ] **Step 4: Ekspos state `RadarController`**: `Status`, `Nearby`, `SelectedId`, `SafetySeen`, `Heading`, `Select(string id)`, `Capture()`, `BackToLobby()`, `AcceptSafety()`, `Retry()`, `PlayPractice()`, `event Action Changed`. Isi aksinya dipindah apa adanya dari blok `TapHit` lama. Hapus `OnGUI`, `Draw*`, gaya, dan tekstur `Beam`.
- [ ] **Step 5: Buat `RadarDisk : VisualElement`** (`[UxmlElement]`, Painter2D): piringan 400 unit, cincin jarak, kipas arah, titik SDA, seperti README `Radar`. Ketukan titik memanggil `controller.Select`. Hitung posisi dengan rumus `OnRadar` lama.
- [ ] **Step 6: Buat `Radar.uxml` dan `RadarView`**, pasang di ExploreScene lewat `InstallUi` dan `SafeArea`. Lapisan GPS gagal dan tirai keselamatan penuh dan `pickingMode = Position`.
- [ ] **Step 7: Jalankan tes.** Expected: semua lolos.
- [ ] **Step 8: Uji play mode** lewat refleksi: `Status` Running/gagal/izin ditolak, daftar kosong, dalam/luar jangkauan, tirai aktif (`panel.Pick` pada `btn-tangkap` mengenai tirai). Render ke PNG dan bandingkan dengan preview `LayarRadar`.
- [ ] **Step 9: Commit.** `feat: exploration radar in UI Toolkit`

### Task 2: Bingkai HUD tangkap dan panduan pra-main

**Files:**
- Create: `Assets/UI/Screens/Tangkap.uxml`, `Assets/EnerGo/Scripts/UI/CaptureView.cs`, `Assets/EnerGo/Scripts/UI/PitaKompas.cs`, `Assets/EnerGo/Scripts/Core/HudStatus.cs`, `Assets/EnerGo/Scripts/Core/HudTapZones.cs`
- Modify: `Assets/EnerGo/Scripts/ARCapturePrototype.cs`, `ARCapturePrototype.Oil.cs`, `ARCapturePrototype.Uranium.cs`, `GuideViewer.cs`, `Assets/UI/Screens/KartuPanduan.uxml`, `Layar.uss`, `Assets/EnerGo/ARPrototype.unity`, `Assets/EnerGo/Sensor360Scene.unity`
- Test: `Assets/EnerGo/Tests/Editor/HudStatusTests.cs`, `LobbyUxmlTests.cs`

**Interfaces:**
- Consumes: `LobbySceneBuilder.InstallUi`, `GuidePager`.
- Produces: `struct HudStatus { string Tag; string Line; float Bar; string Label; bool Warn; }`, `HudStatus.Tags(bool warn)` (AMAN/AWAS).
- Produces: `ARCapturePrototype.ReadHud() -> HudStatus` menggantikan `DrawRadarHUD`, `DrawOilHud`, `DrawUraniumHud`; teks tiap fase dipindah persis.
- Produces: aksi publik `BackFromCapture()`, `ToggleTrackingMode()`, `RespawnTarget()`, `FinishGuide(bool dontRemind)`; properti `GuideOpen`, `HasARSession`, `ModeLabel`, `ResourceId`.
- Produces: `HudTapZones.Blocks(Vector2 panelPoint, float panelW, float panelH, float safeTop) -> bool`.

- [ ] **Step 1: Tulis tes yang gagal.** `HudStatusTests`: `Tags(false) == "AMAN"`, `Tags(true) == "AWAS"`. `HudTapZones`: titik di baris header dan tombol bawah memblok, titik di tengah layar tidak. `LobbyUxmlTests`: `Tangkap.uxml` memuat `btn-kembali`, `btn-mode`, `pil-mode`, `pil-sda`, `kotak-status`, `status-tag`, `status-teks`, `status-bar`, `pita-kompas`, `btn-ulang`, `riak`.
- [ ] **Step 2: Jalankan, pastikan gagal.** Expected: COMPILE FAILED.
- [ ] **Step 3: Implement Core**, zona dari ukuran `Tangkap.uxml`. Ganti pengecualian HUD lama di `CheckTapOnTarget` (`guiY < 128` dan `guiY > tinggi - 70`) dengan `HudTapZones.Blocks`, dengan zona yang sama besar supaya perilaku ketukan tidak berubah.
- [ ] **Step 4: Pindahkan isi `OnGUI` bagian header, tombol bawah, riak ketukan, dan `DrawCompassStrip`** ke `CaptureView` dan `PitaKompas : VisualElement` (Painter2D). Isi tombol (bersihkan target, set fase, feedback) dipindah apa adanya ke aksi publik.
- [ ] **Step 5: Panduan.** `GuideViewer` jadi data + `Finish`; tambah `btn-panduan-lewati` dan `panduan-ingatkan` di `KartuPanduan.uxml`, disembunyikan di Lobby. `CaptureView` menampilkannya memakai `GuidePager` dan label MULAI/LANJUT.
- [ ] **Step 6: Pasang UI di dua scene AR** lewat `InstallUi` + `SafeArea`. `OnGUI` sementara masih menggambar overlay mini-game dan kuis; beri `Event.current.type` guard hanya kalau perlu.
- [ ] **Step 7: Jalankan tes.** Expected: lolos.
- [ ] **Step 8: Uji play mode:** tiap SDA, label kembali RADAR/LOBBY, mode ARCore/sensor, status AMAN/AWAS, panduan sampai MULAI memulai spawn. `panel.Pick` di tengah layar mengembalikan null.
- [ ] **Step 9: Commit.** `feat: capture HUD frame and pre-game guide in UI Toolkit`

### Task 3: Kuis, umpan balik, hasil

**Files:**
- Create: `Assets/UI/Screens/Kuis.uxml`, `Assets/EnerGo/Scripts/UI/CaptureView.Quiz.cs`, `Assets/EnerGo/Scripts/Core/CaptureScreens.cs`
- Modify: `ARCapturePrototype.cs` (hapus `DrawAnalysis`, `DrawQuizTop`, `DrawChip*`, gaya kartu), `Layar.uss`, `Components.uss`
- Test: `Assets/EnerGo/Tests/Editor/CaptureScreensTests.cs`, `LobbyUxmlTests.cs`

**Interfaces:**
- Consumes: `CaptureSession`, `QuizBank`, `EnerGoProgress.NextQuestion/ClearResult/GradeFor`, `UiLayout.ResultTitle/Stars`.
- Produces: `enum CaptureScreen { Play, Quiz, Feedback, Result }`; `CaptureScreens.For(bool hasPending, string state)`; `CaptureScreens.ShowsHudFrame(CaptureScreen)`; `CaptureScreens.ResultMessage(int right, int count)`; `CaptureScreens.CardRotation(float offsetUnits)`; `CaptureScreens.PastThreshold(float offsetUnits)`; `CaptureScreens.ReadHold(bool correct, float secondsSinceShown)`.
- Produces: `ARCapturePrototype.Quiz` (state: `Session`, `Question`, `Marks`, `TimeLeft`, `CardOffsetUnits`, `ReadHold`), aksi `NextQuestion()`, `FinishResult()`.

- [ ] **Step 1: Tulis tes yang gagal.** `For(true, "feedback") == Feedback`; `For(true, "result") == Result`; `For(true, "awaiting-analysis") == Quiz`; `For(false, null) == Play`; `ShowsHudFrame(Quiz)` false; `CardRotation(1000) == 18`, `CardRotation(-1000) == -18`; `PastThreshold(90)` true, `PastThreshold(89)` false; `ReadHold(true, 0) == 1`, `ReadHold(false, 1) == 0.5`; `ResultMessage(3,3) == "Sempurna! Semua jawaban tepat."`. `LobbyUxmlTests`: `Kuis.uxml` memuat `kuis-penanda`, `pil-soal`, `kartu-soal`, `soal-waktu`, `soal-detik`, `soal-teks`, `cap-jawab`, `btn-benar`, `btn-salah`, `kartu-balik`, `btn-lanjut`, `kartu-hasil`, `hasil-bintang`, `hasil-plus`, `btn-selesai-hasil`.
- [ ] **Step 2: Jalankan, pastikan gagal.** Expected: COMPILE FAILED.
- [ ] **Step 3: Implement `CaptureScreens`.** Teks disalin dari `DrawAnalysis`.
- [ ] **Step 4: Ubah `HandleCardSwipeInput`** supaya offset dalam unit panel (`piksel * panelW / Screen.width`). Logika jawab tetap satu jalur, dan penguncian `cardFlicking` tetap mencegah jawaban ganda. Tombol BENAR/SALAH memakai jalur yang sama, bukan klik UI kedua.
- [ ] **Step 5: Buat `Kuis.uxml` dan `CaptureView.Quiz`**: penanda, kartu mengikuti `style.translate`/`rotate`, cap, pendar tepi, tombol terkunci BACA DULU..., kartu hasil. Saat `CaptureScreens.ShowsHudFrame` false, bingkai HUD disembunyikan.
- [ ] **Step 6: Jalankan tes.** Expected: lolos.
- [ ] **Step 7: Uji play mode:** isi `pendingCapture` lewat `BeginAnalysis`, jawab benar dan salah lewat refleksi, kunci 2 detik, hasil Murni dan bukan, masuk scene dengan `pendingCapture` tersimpan. Pulihkan save.
- [ ] **Step 8: Commit.** `feat: quiz, feedback and result in UI Toolkit`

### Task 4: Overlay mini-game

**Files:**
- Create: `Assets/UI/Screens/Overlay.uxml`, `Assets/EnerGo/Scripts/UI/CaptureView.Overlay.cs`, `Assets/EnerGo/Scripts/UI/LingkaranBidik.cs`
- Modify: `ARCapturePrototype.cs` (hapus `DrawSunReticle`, `DrawCoalPips`, `DrawGoldPan`, `DrawGoldGuide`), `ARCapturePrototype.Oil.cs` (hapus `DrawOilOverlay/Section/Pump`), `ARCapturePrototype.Uranium.cs` (hapus `DrawUraniumOverlay`, `DrawHudTag`), `Layar.uss`
- Test: `Assets/EnerGo/Tests/Editor/LobbyUxmlTests.cs`, `Assets/EnerGo/Tests/Editor/HudStatusTests.cs`

**Interfaces:**
- Consumes: `GoldPanRect`, `OilSectionRect`, `UraniumGripRect`, `BorPose`, `PompaPose`, `ReticleRadius`; rect-rect ini tetap satu sumber posisi untuk 3D dan input.
- Produces: `CaptureView.Place(VisualElement e, Rect guiRect)` menaruh elemen tepat pada rect pengendali.
- Produces: `CaptureScreens.PumpRingScale(float secsSinceStroke, float beat)`, `UraniumHeat(int meter)`, `GaugeWarn(float value, float limit)`.

- [ ] **Step 1: Tulis tes yang gagal.** `PumpRingScale(0, 1.2f) == 2.6f`, `PumpRingScale(1.2f, 1.2f) == 1f`, tetap 1 setelahnya; `GaugeWarn(0.61f, 0.6f)` true. `LobbyUxmlTests`: `Overlay.uxml` memuat `bidik`, `bidik-persen`, `dulang-pil`, `dulang-waterpass`, `dulang-hitung`, `dulang-mulai`, `kartu-terbuang`, `btn-endapan-baru`, `bor-label`, `bor-gas`, `pompa-cincin`, `pompa-drum`, `pompa-tekanan`, `rad-angka`, `rad-tren`, `btn-jepit`, `jepit-isi`, `dosis-bar`.
- [ ] **Step 2: Jalankan, pastikan gagal.** Expected: COMPILE FAILED.
- [ ] **Step 3: Implement fungsi Core** sesuai `DrawOilPump` lama.
- [ ] **Step 4: Matahari dan batu bara:** `LingkaranBidik` (Painter2D, README `LingkaranBidik`), pip batu bara sebagai elemen yang diposisikan dari `WorldToScreenPoint`, dipool, tidak dibuat ulang tiap frame.
- [ ] **Step 5: Emas:** pil EMAS n/18, titik panduan digambar dengan Painter2D pada `GoldPanRect`, waterpass, 3-2-1, MULAI!, kartu EMAS TERBUANG! dengan aksi `RetryGoldAtNewDeposit`.
- [ ] **Step 6: Minyak dan uranium:** label lapisan di atas pil, pengukur gas, cincin pompa, bar DRUM dan TEKANAN, kotak RAD, tren, JEPIT, DOSIS. `btn-jepit` memakai `Place(UraniumGripRect)` dan `pickingMode = Ignore`, karena input tetap dibaca `ReadUraniumGrip`.
- [ ] **Step 7: Jalankan tes.** Expected: lolos.
- [ ] **Step 8: Uji play mode:** tiap fase tiap SDA, dengan `Screen` 1080x1920 dan 720x1280; bandingkan `btn-jepit.worldBound` dengan `UraniumGripRect` (selisih di bawah 1 unit). Render ke PNG.
- [ ] **Step 9: Commit.** `feat: mini-game overlays in UI Toolkit`

### Task 5: Bersihkan IMGUI dan uji menyeluruh

**Files:**
- Delete: `Assets/EnerGo/Scripts/EnerGoUI.cs`, `EnerGoUI.Draw.cs`, sisa `OnGUI` di `ARCapturePrototype.cs`; `Resources/UI/Icons` dan font Arcade Cerah kalau tidak ada referensi lagi.
- Modify: `docs/PRD-EnerGo.md` hanya untuk teks yang memang berubah, `docs/peta-proyek.md`
- Test: `Assets/EnerGo/Tests/Editor/UiLayoutTests.cs` (hapus tes fungsi yang ikut terhapus)

- [ ] **Step 1: Pastikan tidak ada lagi `OnGUI`.** Run: `grep -rn "void OnGUI\|GUI\.\|EnerGoUI" Assets/EnerGo/Scripts`. Expected: tidak ada hasil, selain warna yang sudah dipindah ke konstanta lokal.
- [ ] **Step 2: Hapus berkas lama** dan fungsi `UiLayout` yang tidak terpakai beserta tesnya. Cek font dan ikon lewat `AssetDatabase.GetDependencies` sebelum menghapus.
- [ ] **Step 3: Jalankan seluruh tes.** Expected: semua lolos dan tidak ada error kompilasi.
- [ ] **Step 4: Uji jalur penuh di play mode:** Lobby → Radar → tangkap → mini-game → kuis → hasil → Radar, untuk satu SDA dan untuk jalur Latihan. Pulihkan save, buka lagi ExploreScene.
- [ ] **Step 5: Commit.** `refactor: remove IMGUI and the Arcade Cerah helpers`
