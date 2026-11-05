# LobbyScene ke UI Toolkit Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Seluruh tampilan LobbyScene (Splash, Lobby, sheet Pengaturan/Latihan/Panduan, kartu panduan mode baca, transisi) digambar UI Toolkit bergaya Jurnal Ekspedisi, tanpa mengubah perilaku game.

**Architecture:** `LobbyController` tetap pemilik state, timer, dan efek samping; isi blok aksi IMGUI dipindah apa adanya ke metode publik, `OnGUI` dihapus, dan controller memancarkan `StateChanged`. Satu `UIDocument` dengan `LobbyView` (Assembly-CSharp) membangun layar dari UXML/USS, membaca state controller, dan memanggil metode publiknya. Logika murni yang bisa dites (teks, paging panduan) masuk `EnerGo.Core`.

**Tech Stack:** Unity 6000.3.24f1, UI Toolkit (UXML/USS, PanelSettings), modul `vectorgraphics` (SVG sebagai VectorImage), TextCore Font Asset, fonttools 4.58 (Python), NUnit EditMode (`EnerGo.Tests.Editor`), Unity MCP `script-execute` untuk verifikasi di editor.

**Spec:** `docs/superpowers/specs/2026-11-03-lobbyscene-ui-toolkit-design.md`

## Global Constraints

- Acuan visual: `docs/design-system/components/LayarAwal/preview.html` dan `LayarSheet/preview.html` (posisi dan ukuran 480 × 800), kelas di `docs/design-system/components/bundle.css`, token di `docs/design-system/tokens.json`, aturan di `docs/design-system/README.md`.
- Satuan UI = unit dasar 480 × 800. PanelSettings: Scale With Screen Size, Reference Resolution 480 × 800, Screen Match Mode Match Width Or Height, Match 0.
- Nama variabel USS = nama token di `tokens.json` (mis. `--kertas-100`, `--space-4`, `--radius-lg`, `--garis-tinta`).
- Perilaku tidak berubah: urutan layar, teks, angka, waktu (splash 2,4 dtk, fade masuk 0,5 dtk, fade keluar 0,4 dtk, ajakan muncul setelah 0,35 dtk, transisi 0,45 dtk, reset 3,5 dtk, "Panduan akan muncul lagi" 2 dtk), cooldown input (0,15 / 0,2 / 0,25 dtk), PlayerPrefs (`EnerGo.UseARCore`, `EnerGo.SelectedResource`, `EnerGo.Volume`, `EnerGo.Vibration`, `EnerGo.Guide.Skip.<id>`), SFX dan haptics persis seperti `LobbyController` sekarang.
- Bahasa UI Bahasa Indonesia; teks persis seperti PRD dan kode sekarang.
- Logo: `docs/design-system/assets/Logo/energo-mark.svg` + wordmark "EnerGo" diset Bricolage ExtraBold, "Go" `--oranye-tua`. Logo kristal tidak dipakai di LobbyScene.
- Latar `.eg-peta` dibuat dari kode/USS (tanpa gambar latar).
- Radar dan layar tangkap tetap IMGUI; `GuideViewer` IMGUI tetap dipakai layar tangkap.
- Target sentuh minimal `--sentuh-min` (64 unit).

## Review Focus

1. Satu ketukan yang menutup sheet (di luar kartu atau SELESAI) tidak boleh ikut menekan tombol Lobby di bawahnya: root Lobby `pickingMode = Ignore` / disabled selama sheet, panduan, atau transisi terbuka, dan tombol tidak bereaksi selama cooldown. Dicek di Task 7.
2. Setelah RESET PROGRES, pil total unit dan badge tile langsung menjadi 0; setelah toggle Mode AR, teks pil mode kamera di Lobby berubah. Dicek di Task 4 dan Task 5.
3. Saat ada `pendingCapture`, scene langsung transisi ke layar tangkap dan Lobby tidak bisa diketuk di frame itu. Dicek di Task 3.
4. HP ber-notch: pil total unit dan dock tidak tertutup. Dicek di Task 4 (SafeArea + Device Simulator).
5. Slider volume: menyeret lalu melepas di luar bar tetap menyimpan nilai dan memutar klik sekali; jumlah 100+ di badge ditulis "99+". Dicek di Task 5 dan Task 4 (`LobbyText.Badge`).

---

### Task 1: Fondasi tema (token, font, ikon, PanelSettings)

**Files:**
- Create: `Assets/UI/Theme.uss`, `Assets/UI/PanelSettings.asset`, `Assets/Fonts/*.ttf` (instance statis) + Font Asset, `Assets/UI/Icons/*.svg` + `Assets/UI/Logo/energo-mark.svg`
- Create: `Assets/EnerGo/Editor/UiAssetSetup.cs` (menu pembuat Font Asset, importer SVG, PanelSettings)
- Test: `Assets/EnerGo/Tests/Editor/ThemeTokensTests.cs`

**Interfaces:**
- Produces: variabel USS `--<nama-token>` untuk semua token warna, spacing, radius, stroke (`garis-*`), opacity, ukuran; variabel font `--font-judul`, `--font-judul-sempit`, `--font-isi`, `--font-isi-tebal`, `--font-isi-bold`, `--font-data`, `--font-data-bold`, `--font-tulisan` berisi `url("project://database/Assets/Fonts/<nama> SDF.asset")`; ikon di `Assets/UI/Icons/<nama>.svg` (nama sama dengan `docs/design-system/assets/Ikon`).

- [ ] **Step 1: Write the failing test** `ThemeTokensTests.EveryTokenIsAUssVariableWithTheSameValue`: baca `docs/design-system/tokens.json` (path dari `Application.dataPath + "/../docs/design-system/tokens.json"`), ambil semua `name`/`value` di `color.tokens`, `spacing.tokens`, `radius.tokens`, `stroke.tokens`, `opacity.tokens`, `ukuran.tokens`; baca `Assets/UI/Theme.uss`; untuk tiap token `Assert.That(uss, Does.Contain($"--{name}: {value}"))` (bandingkan lowercase, nilai `rgba(...)` apa adanya). Tambahkan `ThemeTokensTests.EveryDesignIconIsCopied`: tiap `*.svg` di `docs/design-system/assets/Ikon` ada di `Assets/UI/Icons` dengan isi byte sama.

- [ ] **Step 2: Run test to verify it fails**

Run (MCP): `tests-run` dengan `{"testMode":"EditMode"}`; Expected: 2 tes baru FAIL (file Theme.uss belum ada).

- [ ] **Step 3: Buat font statis** (terminal, dari `docs/design-system/fonts/`, keluaran ke `Assets/Fonts/`): Bricolage-ExtraBold (`wght=800 opsz=24 wdth=100`), Bricolage-ExtraBold-Sempit (`wdth=75`), Lexend-Regular (400), Lexend-SemiBold (600), Lexend-Bold (700), Caveat-SemiBold (600) dengan `fonttools varLib.instancer` (perintah lengkap di `docs/tutorial.md` Tahap 1a langkah 2); salin `SpaceMono-Regular.ttf` dan `SpaceMono-Bold.ttf`.

- [ ] **Step 4: Buat `UiAssetSetup.cs`** dengan `[MenuItem("EnerGo/UI/Setup Aset UI")] public static void SetupUiAssets()`: (a) untuk tiap `Assets/Fonts/*.ttf` buat `FontAsset.CreateFontAsset(font)` (atlas dinamis) dan simpan sebagai `Assets/Fonts/<nama> SDF.asset`; (b) salin SVG dari `docs/design-system/assets/Ikon` ke `Assets/UI/Icons` dan `energo-mark.svg` ke `Assets/UI/Logo`, set importer SVG ke tipe UI Toolkit Vector Image; (c) buat `Assets/UI/PanelSettings.asset` sesuai Global Constraints. Jalankan sekali lewat menu/MCP. Jika API importer SVG berbeda di versi ini, set tipenya lewat Inspector dan catat di commit.

- [ ] **Step 5: Tulis `Assets/UI/Theme.uss`**: `:root { ... }` berisi semua token sebagai `--<nama>: <nilai>;` (stroke dan ukuran memakai px), variabel font di atas, plus `-unity-font-definition: var(--font-isi); color: var(--tinta);`.

- [ ] **Step 6: Run tests to verify they pass**

Run (MCP): `tests-run` EditMode; Expected: semua PASS (84 lama + 2 baru), console tanpa error import USS/SVG.

- [ ] **Step 7: Commit**

```bash
git add Assets/UI Assets/Fonts Assets/EnerGo/Editor/UiAssetSetup.cs* Assets/EnerGo/Tests/Editor/ThemeTokensTests.cs*
git commit -m "feat: UI Toolkit theme, static fonts and icons from the design system"
```

### Task 2: Components.uss dari bundle.css

**Files:**
- Create: `Assets/UI/Components.uss`, `Assets/UI/Screens/Galeri.uxml` (halaman uji komponen, tidak dipakai di game)

**Interfaces:**
- Consumes: variabel dari Task 1.
- Produces: kelas USS bernama sama dengan `bundle.css`: `.eg-peta`, `.eg-kartu`, `.eg-kartu--garis`, `.eg-selotip` (+ `--merah`), `.eg-btn` (+ `--utama`, `--kertas`, `--info`, `--bahaya`, `--nonaktif`, `--besar`, `--kecil`, `--penuh`), `.eg-btn__isi`, `.eg-bulat` (+ `--oker`, `--laut`), `.eg-pil` (+ `--angka`, `--laut`, `--oker`), `.eg-tile` (+ `--ringkas`, `.is-terpilih`, `.is-terkunci`), `.eg-tile__no`, `.eg-tile__nama`, `.eg-badge`, `.eg-bar` (+ `--tipis`), `.eg-bar__isi`, `.eg-baris-atur` (`.judul`, `.sub`), `.eg-toggle` (+ `.is-nyala`), `.eg-slider`, `.eg-redup`, `.eg-status__teks`, `.eg-data`, `.eg-catatan`.

- [ ] **Step 1: Terjemahkan kelas di atas** dari `bundle.css` ke USS. Aturan: `box-shadow` offset keras → elemen anak `.eg-bayang` di belakang (absolute, geser sesuai token `bayang-*`) atau `border-bottom-width` lebih tebal untuk bibir tombol (contoh `.eg-btn` di `docs/tutorial.md`); `:active` tombol geser 3 unit (`translate: 0 3px`); selotip `rotate: -2deg`; `.eg-peta` dari elemen anak (daratan `peta-hijau`, air `peta-air`, garis kontur `peta-hijau-tua` opasitas `--opasitas-kontur`, grid `grid-peta` 40 unit) karena USS tidak punya gradien bertumpuk.

- [ ] **Step 2: Buat `Galeri.uxml`** yang memuat Theme.uss + Components.uss dan satu contoh setiap kelas/state (tombol per varian normal/ditekan/nonaktif, tile normal/terpilih/terkunci/Acak, toggle nyala/mati, bar, pil, kartu + selotip).

- [ ] **Step 3: Verifikasi visual**: buka Galeri.uxml di UI Builder (atau render lewat MCP ke PNG), bandingkan dengan `preview.html` komponen Tombol, TombolBulat, Kartu, Pil, TileSpesimen, Kontrol, BarUkur. Expected: bentuk, warna, garis, bibir, dan miring cocok; console tanpa peringatan USS.

- [ ] **Step 4: Commit**

```bash
git add Assets/UI/Components.uss* Assets/UI/Screens/Galeri.uxml*
git commit -m "feat: Jurnal Ekspedisi component styles for UI Toolkit"
```

### Task 3: LobbyController jadi pemilik state tanpa OnGUI

**Files:**
- Modify: `Assets/EnerGo/Scripts/LobbyController.cs` (hapus `OnGUI`, `InitStyles`, semua `Draw*`, `DrawBrand`, `LoadTextures`, `HeldPointer`, `OutsideTap`, `ButtonHit`, field `GUIStyle`/tekstur logo/`lobbyGuide`; tambah API publik)

**Interfaces:**
- Produces (dipakai Task 4–7):
  - `public event System.Action StateChanged;` dipanggil setiap `currentState`, `useARCore`, `selectedResource`, `isConfirmingReset`, `OpenGuideId`, atau progres berubah lewat metode di bawah.
  - Properti: `ScreenState State`, `bool InputReady` (cooldown ≤ 0), `float SplashTimer`, `float TransitionProgress` (0..1), `string TransitionText` ("Menyalakan radar..." jika tujuan `ExploreScene`, selain itu "Menyiapkan kamera..."), `bool UseARCore`, `string SelectedPractice` (id SDA atau `"random"`), `bool IsConfirmingReset`, `float ResetConfirmRemaining` (detik, 3,5 → 0), `bool GuidesResetNoticeVisible` (< 2 dtk sejak reset), `string OpenGuideId` (null jika tidak ada kartu panduan terbuka).
  - Metode (isi = blok aksi lama, termasuk SFX, haptics, cooldown): `StartHunt()`, `OpenSheet(ScreenState sheet)` (Guides/Practice/Settings dari dock), `CloseSheet()` (ketuk luar dan SELESAI: `PlayButtonBack`, state Lobby, cooldown 0,2), `ToggleARMode()`, `PreviewVolume(float v01)` (hanya `AudioListener.volume`), `CommitVolume()` (`SFXManager.SetVolume` + klik), `ToggleVibration()`, `OpenPracticeFromSettings()`, `TapReset()`, `SelectPractice(string id)`, `StartPractice()`, `OpenGuide(string id)` (klik, cooldown 0,15, set `OpenGuideId`), `CloseGuide()` (cooldown 0,2, `OpenGuideId = null`), `ResetGuides()`.
  - Semua metode aksi tidak melakukan apa-apa jika `!InputReady` atau state tidak cocok, sama seperti `interactive`/`ready` lama.

- [ ] **Step 1: Pindahkan blok aksi** dari `DrawLobby`, `DrawSheet`, `DrawSettings`, `DrawPractice`, `DrawGuides`, `DrawToggleRow` ke metode di atas, kata per kata. `Awake`, `Start`, `Update`, `OnApplicationPause`, `KeepScreenAwake`, `CaptureSceneName` tidak berubah, kecuali `Update` memanggil `StateChanged` saat splash berakhir dan saat reset kedaluwarsa.

- [ ] **Step 2: Verifikasi compile dan tes** lewat MCP: `tests-run` EditMode, Expected: semua PASS, console tanpa error compile.

- [ ] **Step 3: Verifikasi perilaku di editor** lewat MCP `script-execute`: di scene kosong sementara buat GameObject + `LobbyController` via reflection set `currentState = Lobby` dan cooldown 0, panggil `OpenSheet(Settings)` → `State == Settings`; `CloseSheet()` → `Lobby`; `StartHunt()` → `TransitioningToAR` dan `TransitionText == "Menyalakan radar..."`; `SelectPractice("coal")` → `SelectedPractice == "coal"` dan PlayerPrefs tersimpan (kembalikan nilai lama setelahnya). Expected: semua cocok. Catat bahwa jalur `pendingCapture` (Review Focus 3) tidak berubah karena `Start` tidak disentuh.

- [ ] **Step 4: Commit**

```bash
git add Assets/EnerGo/Scripts/LobbyController.cs
git commit -m "refactor: LobbyController exposes its actions and state, drops IMGUI"
```

### Task 4: Layar Lobby, SafeArea, dan pemasangan di scene

**Files:**
- Create: `Assets/UI/Screens/LobbyScene.uxml` (root dengan lapisan `lapis-splash`, `lapis-lobby`, `lapis-sheet`, `lapis-panduan`, `lapis-transisi`), `Assets/UI/Screens/Lobby.uxml`
- Create: `Assets/EnerGo/Scripts/UI/LobbyView.cs`, `Assets/EnerGo/Scripts/UI/SafeArea.cs`
- Create: `Assets/EnerGo/Scripts/Core/LobbyText.cs`
- Modify: `Assets/EnerGo/LobbyScene.unity` (GameObject `UI` dengan `UIDocument` + `LobbyView` + `SafeArea`), `Assets/EnerGo/Editor/LobbySceneBuilder.cs` (`CreateLobbyScene` ikut menambah GameObject `UI` yang sama)
- Test: `Assets/EnerGo/Tests/Editor/LobbyTextTests.cs`, `Assets/EnerGo/Tests/Editor/LobbyUxmlTests.cs`

**Interfaces:**
- Consumes: API Task 3, kelas Task 2.
- Produces: `LobbyText.Badge(int n) -> string` ("99+" jika n > 99), `LobbyText.TotalUnits(int total) -> string` ("<n> unit"), `LobbyText.CameraMode(bool arcore) -> string` ("Mode kamera: AR (ARCore)" / "Mode kamera: Sensor 360"), `LobbyText.CatalogNo(string id) -> string` ("No.01".."No.05", "???" untuk random); nama elemen Lobby: `total-unit`, `judul`, `tile-sun`, `tile-coal`, `tile-gold`, `tile-oil`, `tile-uranium`, `mode-kamera`, `btn-berburu`, `btn-panduan`, `btn-latihan`, `btn-pengaturan`; `LobbyView.Refresh()` membaca ulang semua nilai.

- [ ] **Step 1: Write the failing tests** `LobbyTextTests`: `Badge(0)=="0"`, `Badge(99)=="99"`, `Badge(100)=="99+"`; `TotalUnits(63)=="63 unit"`; `CameraMode(true)=="Mode kamera: AR (ARCore)"`, `CameraMode(false)=="Mode kamera: Sensor 360"`; `CatalogNo("sun")=="No.01"`, `CatalogNo("uranium")=="No.05"`, `CatalogNo("random")=="???"`. `LobbyUxmlTests.LobbyHasEveryNamedElement`: `AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/Screens/Lobby.uxml").CloneTree()`, `Assert.IsNotNull(root.Q(name))` untuk setiap nama di Produces.

- [ ] **Step 2: Run to verify they fail** (MCP `tests-run` EditMode). Expected: FAIL, tipe/aset belum ada.

- [ ] **Step 3: Implement `LobbyText`** (statis, `EnerGo.Core`) dan **`Lobby.uxml`** mengikuti Lobby di `LayarAwal/preview.html`: baris atas pil `--angka` dengan ikon petir + `energo-mark` 44 unit di kanan; judul ENER**GO!** (`judul-layar`, "GO!" `--oranye-tua`); kartu berselotip "Koleksi energimu" berisi 5 `eg-tile--ringkas` (3 + 2, nomor katalog, badge, ikon `sda-*`, nama dari `ResourceIds.Name`; `is-terkunci` + gembok jika `!ResourceIds.IsPlayable`); pil `--laut` ikon kamera + teks mode; tombol `--utama --besar --penuh` ikon radar "Berburu!"; dock 3 `eg-bulat` (Panduan `--oker` buku, Latihan `--laut` target, Pengaturan gir). Latar `.eg-peta` di root.

- [ ] **Step 4: Implement `LobbyView`** (`[RequireComponent(typeof(UIDocument))]`, field `[SerializeField] LobbyController controller`): ikat tombol ke `StartHunt` / `OpenSheet(...)`; `Refresh()` mengisi total (`UiLayout.TotalUnits`), badge, mode kamera; dengarkan `StateChanged` → `Refresh()` + tampilkan lapisan sesuai `State`. **`SafeArea`** sesuai contoh `docs/tutorial.md` Tahap 1a langkah 6. Pasang di LobbyScene dan di `LobbySceneBuilder`.

- [ ] **Step 5: Run tests** (MCP). Expected: semua PASS.

- [ ] **Step 6: Verifikasi** di play mode + Device Simulator ber-notch: screenshot Lobby (`ScreenCapture`, lihat memori MCP) dibandingkan dengan `LayarAwal/preview.html`; ketuk BERBURU! → transisi lalu ExploreScene; dock membuka state sheet (layar sheet masih kosong sampai Task 5). Expected: tata letak cocok, tidak ada yang tertutup notch.

- [ ] **Step 7: Commit**

```bash
git add Assets/UI/Screens Assets/EnerGo/Scripts/UI Assets/EnerGo/Scripts/Core/LobbyText.cs* Assets/EnerGo/Tests/Editor/Lobby*Tests.cs* Assets/EnerGo/LobbyScene.unity Assets/EnerGo/Editor/LobbySceneBuilder.cs
git commit -m "feat: UI Toolkit lobby screen in the expedition-journal style"
```

### Task 5: Sheet Pengaturan dan Latihan

**Files:**
- Create: `Assets/UI/Screens/SheetPengaturan.uxml`, `Assets/UI/Screens/SheetLatihan.uxml`, `Assets/EnerGo/Scripts/UI/LobbyView.Sheets.cs` (partial `LobbyView`)
- Test: `Assets/EnerGo/Tests/Editor/LobbyUxmlTests.cs` (tambah)

**Interfaces:**
- Consumes: Task 3 (`ToggleARMode`, `PreviewVolume`, `CommitVolume`, `ToggleVibration`, `OpenPracticeFromSettings`, `TapReset`, `IsConfirmingReset`, `ResetConfirmRemaining`, `CloseSheet`, `SelectPractice`, `StartPractice`, `SelectedPractice`).
- Produces: elemen `tirai` (tirai `.eg-redup` bersama untuk semua sheet), `atur-ar`, `atur-ar-sub`, `atur-volume`, `atur-volume-persen`, `atur-getar`, `atur-getar-sub`, `btn-ke-latihan`, `btn-reset`, `reset-bar`, `btn-selesai`, `latihan-sun`..`latihan-uranium`, `latihan-random`, `btn-mulai-latihan`.

- [ ] **Step 1: Write the failing test** `SheetsHaveEveryNamedElement` (cara sama dengan Task 4) untuk semua nama di Produces.
- [ ] **Step 2: Run to verify it fails** (MCP). Expected: FAIL.
- [ ] **Step 3: Implement** kedua UXML mengikuti `LayarSheet/preview.html` (kartu dari bawah, `radius-lg` atas, selotip PENGATURAN / LATIHAN, sub-label sesuai state persis seperti PRD 3.3–3.4, reset: tombol kertas teks bahaya "RESET PROGRES" → tombol bahaya "TEKAN LAGI UNTUK MENGHAPUS" dengan `reset-bar` menyusut `ResetConfirmRemaining / 3.5`, tile Latihan dengan cincin `is-terpilih` + DIPILIH, tanpa badge di Acak) dan `LobbyView.Sheets.cs`: ketuk `tirai` di luar kartu → `CloseSheet()`; slider: PointerDown/Move → `PreviewVolume`, PointerUp atau PointerCaptureOut → `CommitVolume()` sekali (capture pointer supaya lepas di luar bar tetap tercatat); seluruh baris toggle bisa diketuk. Sheet naik dari bawah 0,25 dtk.
- [ ] **Step 4: Run tests** (MCP). Expected: PASS.
- [ ] **Step 5: Verifikasi** play mode: toggle Mode AR mengubah sub-label dan, setelah sheet ditutup, pil mode di Lobby; slider menyimpan % dan bunyi klik sekali saat dilepas (juga saat dilepas di luar bar); reset dua ketukan dalam 3,5 dtk mengosongkan total dan badge (backup dan pulihkan `energo-demo-v1.json*` dari `Application.persistentDataPath` sebelum/ sesudah); Latihan memilih dan menyimpan tile, Acak memilih SDA acak saat MULAI. Expected: semua sesuai PRD.
- [ ] **Step 6: Commit**

```bash
git add Assets/UI/Screens/Sheet*.uxml* Assets/EnerGo/Scripts/UI/LobbyView.Sheets.cs* Assets/EnerGo/Tests/Editor/LobbyUxmlTests.cs
git commit -m "feat: settings and practice sheets in UI Toolkit"
```

### Task 6: Sheet Panduan dan kartu panduan mode baca

**Files:**
- Create: `Assets/UI/Screens/SheetPanduan.uxml`, `Assets/UI/Screens/KartuPanduan.uxml`, `Assets/EnerGo/Scripts/UI/LobbyView.Guide.cs`, `Assets/EnerGo/Scripts/Core/GuidePager.cs`
- Test: `Assets/EnerGo/Tests/Editor/GuidePagerTests.cs`, `LobbyUxmlTests.cs` (tambah)

**Interfaces:**
- Consumes: `OpenGuide`, `CloseGuide`, `ResetGuides`, `OpenGuideId`, `GuidesResetNoticeVisible`; `GuideBook.For(id)`.
- Produces: `GuidePager(int pageCount)` dengan `int Page`, `bool IsLast`, `bool Next()` (maju; `false` jika sudah di halaman terakhir), `void Prev()` (tidak di bawah 0), `string PrimaryLabel` ("LANJUT" atau "TUTUP" di halaman terakhir), `string StepLabel` ("LANGKAH n/N"); elemen `panduan-sun`..`panduan-uranium`, `btn-reset-panduan`, `panduan-notice`, `kartu-panduan`, `panduan-selotip`, `panduan-langkah`, `panduan-ilustrasi`, `panduan-judul`, `panduan-isi`, `panduan-titik`, `btn-panduan-utama`, `btn-panduan-tutup`.

- [ ] **Step 1: Write the failing tests** `GuidePagerTests`: pager 3 halaman mulai `Page==0`, `StepLabel=="LANGKAH 1/3"`, `PrimaryLabel=="LANJUT"`; dua kali `Next()` → `IsLast`, `PrimaryLabel=="TUTUP"`, `Next()` berikutnya `false`; `Prev()` di halaman 0 tetap 0. Ditambah tes nama elemen.
- [ ] **Step 2: Run to verify they fail** (MCP). Expected: FAIL.
- [ ] **Step 3: Implement** `GuidePager` (Core), kedua UXML (sheet dengan sub "Ketuk untuk membaca cara mainnya", grid 3 + 2, tombol kertas "TAMPILKAN SEMUA PANDUAN LAGI", notice 2 dtk; kartu `eg-kartu--garis` di atas `tirai`, selotip nama SDA, ilustrasi = ikon `sda-*` dalam bingkai kertas, titik halaman, tombol kecil "TUTUP" + tombol utama LANJUT/TUTUP, ketuk sepertiga kiri/kanan kartu untuk mundur/maju) dan `LobbyView.Guide.cs`. TUTUP di halaman terakhir dan tombol kecil TUTUP memanggil `CloseGuide()`.
- [ ] **Step 4: Run tests** (MCP). Expected: PASS.
- [ ] **Step 5: Verifikasi** play mode untuk kelima SDA: semua halaman dan teks sama dengan `GuideBook`; reset panduan menampilkan notice 2 dtk dan menghapus `EnerGo.Guide.Skip.*` (cek PlayerPrefs). Expected: sesuai PRD 3.5–3.6 mode baca.
- [ ] **Step 6: Commit**

```bash
git add Assets/UI/Screens/*Panduan.uxml* Assets/EnerGo/Scripts/UI/LobbyView.Guide.cs* Assets/EnerGo/Scripts/Core/GuidePager.cs* Assets/EnerGo/Tests/Editor/GuidePagerTests.cs* Assets/EnerGo/Tests/Editor/LobbyUxmlTests.cs
git commit -m "feat: guide sheet and read-mode guide card in UI Toolkit"
```

### Task 7: Splash, transisi, lapisan input, dan uji menyeluruh

**Files:**
- Create: `Assets/UI/Screens/Splash.uxml`, `Assets/UI/Screens/Transisi.uxml`
- Modify: `Assets/EnerGo/Scripts/UI/LobbyView.cs` (Update: alpha/skala/bar splash dari `SplashTimer`, transisi dari `TransitionProgress`; picking tiap lapisan)

**Interfaces:**
- Consumes: `State`, `SplashTimer`, `TransitionProgress`, `TransitionText`, `InputReady`.

- [ ] **Step 1: Implement Splash** sesuai `LayarAwal/preview.html` (tanpa cap "EKSPEDISI · v0.1"): kartu dengan `energo-mark` 128 unit (skala 88% → 100% dalam 0,6 dtk), wordmark EnerGo 72 unit, tagline; bar `--tipis` terisi `SmoothStep(0,1,SplashTimer/2.2)`; "SENTUH LAYAR UNTUK MULAI" berkedip setelah 0,35 dtk; fade 0,5 / 0,4 dtk. Ketukan layar tetap dibaca `LobbyController.Update` (tidak berubah).
- [ ] **Step 2: Implement Transisi**: layar `--tinta`, `energo-mark` 120 unit berputar, teks `TransitionText` dalam `data`, bar tipis; opasitas naik sesuai `TransitionProgress`.
- [ ] **Step 3: Implement lapisan input**: hanya lapisan aktif yang `pickingMode = Position`; `lapis-lobby` `SetEnabled(State == Lobby && InputReady)` dan Ignore saat sheet/panduan/transisi; semua callback tombol keluar jika `!controller.InputReady`. Cek sentuhan UI Toolkit dengan Input System saja; jika tombol tidak merespons di HP, tambahkan `EventSystem` + `InputSystemUIInputModule` di GameObject `UI` (dan di `LobbySceneBuilder`).
- [ ] **Step 4: Run tests** (MCP `tests-run` EditMode). Expected: semua PASS.
- [ ] **Step 5: Uji menyeluruh** di editor dan HP (APK hanya jika pemilik meminta build): semua jalur di PRD bagian 2 (splash + lewati, BERBURU!, tiap sheet buka/tutup lewat luar dan SELESAI, toggle, slider, reset, latihan Acak, panduan semua halaman, kembali dari radar lewat MAIN MODE LATIHAN langsung ke sheet Latihan, buka ulang dengan kuis tertunda), plus Review Focus 1–5. Screenshot tiap layar dibandingkan dengan preview. Expected: tidak ada ketukan ganda, tidak ada state hilang, tidak ada teks tertutup notch.
- [ ] **Step 6: Commit**

```bash
git add Assets/UI/Screens/Splash.uxml* Assets/UI/Screens/Transisi.uxml* Assets/EnerGo/Scripts/UI/LobbyView.cs Assets/EnerGo/LobbyScene.unity Assets/EnerGo/Editor/LobbySceneBuilder.cs
git commit -m "feat: splash and transition in UI Toolkit, one input layer at a time"
```
