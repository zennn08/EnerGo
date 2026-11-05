# Peta Proyek EnerGo

> **Pembaruan 5 Nov 2026:** semua layar sudah pindah ke UI Toolkit dan tidak ada lagi `OnGUI`, `EnerGoUI`, font Fredoka/Lilita, atau `Resources/UI/Icons`. Tampilan kini ada di `Assets/UI` (UXML, USS) dan `Assets/EnerGo/Scripts/UI` (`LobbyView`, `RadarView`, `CaptureView` beserta `.Quiz` dan `.Overlay`). Keadaan yang dibaca overlay ada di `ARCapturePrototype.Overlay.cs`. Nama fungsi gambar dan nomor baris IMGUI di bawah ini adalah catatan saat dokumen dibuat.

Dibuat 3 Nov 2026 dari branch `feat/ui-arcade-cerah` (commit d114c55). Dokumen ini hanya membaca kode, tidak ada kode yang diubah. Semua path relatif ke root repo, nomor baris dari kode saat ini.

Script game ada di `Assets/EnerGo/Scripts/`, resource di `Assets/EnerGo/Resources/` (bukan `Assets/Resources/`, folder itu ada tapi kosong). Folder `Assets/UI/` belum ada.

## 1. Scene dan layar

### Scene (urutan di Build Settings)

| # | Scene | Komponen utama | Isi |
|---|---|---|---|
| 0 | `Assets/EnerGo/LobbyScene.unity` | `LobbyController` | Splash, Lobby, 3 sheet, transisi, kartu panduan mode baca |
| 1 | `Assets/EnerGo/ExploreScene.unity` | `RadarController` + `LocationFeed` | Radar, state GPS, peringatan keselamatan |
| 2 | `Assets/EnerGo/ARPrototype.unity` | `ARCapturePrototype` (+ ARSession) | Layar tangkap mode ARCore |
| 3 | `Assets/EnerGo/Sensor360Scene.unity` | `ARCapturePrototype` (tanpa ARSession) | Layar tangkap mode Sensor 360 |

`Assets/Scenes/SampleScene.unity` tidak masuk build. Scene Lobby dan Explore bisa dibangun ulang dari `Assets/EnerGo/Editor/LobbySceneBuilder.cs`, scene AR dari `ARPrototypeBuilder.cs`.

Pemilihan scene tangkap: `LobbyController.CaptureSceneName()` (`LobbyController.cs:32`) membaca PlayerPrefs `EnerGo.UseARCore`: 1 = `ARPrototype`, 0 = `Sensor360Scene`.

Hanya ada **3 fungsi `OnGUI`** di seluruh proyek. Setiap layar adalah cabang di dalam salah satunya. Komponen gambar bersama (tombol, kartu, pita, tile, bar, ikon, latar dunia) ada di `EnerGoUI.cs` dan `EnerGoUI.Draw.cs` (kelas statis `EnerGoUI`).

### Layar PRD → script dan fungsi

| Layar (PRD) | Script | Fungsi yang menggambar | Catatan |
|---|---|---|---|
| Splash (3.1) | `LobbyController.cs` | `OnGUI` (166) → `DrawWorld` + `DrawSplash` (220), logo lewat `DrawBrand` (198) | Timer dan ketukan lewati ada di `Update` (122–141), bukan di OnGUI |
| Lobby (3.2) | `LobbyController.cs` | `DrawLobby` (246) | Selalu digambar, juga di bawah sheet dan transisi |
| Sheet Pengaturan (3.3) | `LobbyController.cs` | `DrawSettings` (381), bingkai `DrawSheet` (362), baris toggle `DrawToggleRow` (550) | Slider volume dibaca dari `HeldPointer` (566) |
| Sheet Latihan (3.4) | `LobbyController.cs` | `DrawPractice` (469), tile `DrawPracticeTile` (337) | |
| Sheet Panduan (3.5) | `LobbyController.cs` | `DrawGuides` (509) | |
| Kartu Panduan (3.6) | `GuideViewer.cs` | `GuideViewer.Draw` (35), ilustrasi `DrawGuideArt` (101) | Mode baca dari `DrawGuides`; mode main dari `ARCapturePrototype.OnGUI` (1663). Isi teks di `Core/GuideBook.cs` |
| Transisi (3.7) | `LobbyController.cs` | `DrawTransition` (346) | Pindah scene di `Update` (142–150) |
| Radar (3.8) | `RadarController.cs` | `OnGUI` (75), piringan + node `DrawRadar` (160), kartu SDA `DrawSelected` (214) | Pil kompas/GPS langsung di `OnGUI` (106–117) |
| Radar: state GPS | `RadarController.cs` | `DrawStatus` (268) | Mencari, izin ditolak, lokasi mati, GPS gagal |
| Peringatan keselamatan | `RadarController.cs` | `DrawSafety` (298) | |
| HUD tangkap umum (3.9) | `ARCapturePrototype.cs` | `OnGUI` (1570): header langsung di OnGUI (1589–1624), kotak status `DrawRadarHUD` (2001), pita kompas `DrawCompassStrip` (2101), tombol bawah dan efek ketukan (1638–1661) | Helper: `DrawHudBox` (2163), `DrawHudBar` (2151), `DrawHudPill` (2173), `DrawPill` (2223), `DrawBtn` (2233) |
| Matahari | `ARCapturePrototype.cs` | `DrawSunReticle` (2203) | Logika `UpdateSun` (731) |
| Batu Bara | `ARCapturePrototype.cs` | `DrawCoalPips` (2182) | Logika `SpawnCoalField` (860), `HitCoal` (938) |
| Emas | `ARCapturePrototype.cs` | `DrawGoldPan` (1223), keping `DrawGoldPiece` (1316) | Kartu "EMAS TERBUANG!" + tombol CARI ENDAPAN BARU di dalam `DrawGoldPan` (1308) |
| Minyak Bumi | `ARCapturePrototype.Oil.cs` | `DrawOilOverlay` (226) → `DrawOilSection` (235), `DrawOilPump` (260); isi kotak status `DrawOilHud` (185) | |
| Uranium | `ARCapturePrototype.Uranium.cs` | `DrawUraniumOverlay` (332); isi kotak status `DrawUraniumHud` (278), tag `DrawHudTag` (321) | |
| Kuis (3.11) | `ARCapturePrototype.cs` | `DrawAnalysis` (1750), cabang `state == "awaiting-analysis"` (sekitar 1885–2000), penanda + pil SOAL `DrawQuizTop` (1735) | Geser kartu dibaca di `Update` → `HandleCardSwipeInput` (609); tombol BENAR/SALAH `TryAnswerButton` (712) + `QuizButtonRect` (1692); jawaban `CommitAnswer` (694); timer `UpdateQuizTimer` (494) |
| Kartu umpan balik | `ARCapturePrototype.cs` | `DrawAnalysis`, cabang `state == "feedback"` (1774) | Chip dari `DrawChipRow` (1714) |
| Hasil (3.12) | `ARCapturePrototype.cs` | `DrawAnalysis`, cabang `state == "result"` (1822) | Tombol LANJUT pindah scene di 1870–1878 |
| Pesan error simpan | `ARCapturePrototype.cs` | Di dalam `DrawAnalysis`: umpan balik (1816), hasil (1882), kuis (1990), teks dari `EnerGoProgress.Error` | Gagal membuka kuis: `CompleteCapture` (810) |

Status kuis disimpan di `EnerGoProgress.Current.pendingCapture` (`EnerGoProgress.cs`), bukan di komponen. Selama `pendingCapture != null`, `OnGUI` layar tangkap menyembunyikan tombol kembali, tombol mode, dan tombol munculkan ulang.

## 2. SFX: cara dimuat dan diputar

**Kelas:** `EnerGo.SFXManager` (`Assets/EnerGo/Scripts/SFXManager.cs`), singleton `SFXManager.Instance`.

- Dibuat otomatis sebelum scene pertama dimuat (`AutoCreate`, baris 337–345, `[RuntimeInitializeOnLoadMethod(BeforeSceneLoad)]`) sebagai GameObject `[SFXManager]` dengan `DontDestroyOnLoad`. Tidak perlu ditaruh di scene.
- `Awake` membuat 5 AudioSource: `sfxSource` (one-shot), `loopSource` (dipakai bergantian oleh `sfx_aliran_air` dan `sfx_matahari`, jadi keduanya tidak bisa bunyi bersamaan), `musicSource` (volume 0,6), `ambienceSource` (sungai), `ekstrakSource` (ekstraksi matahari yang posisinya di-seek). `pumpSource` (pitch 0,6) dibuat saat pertama dipakai.
- Klip dimuat sekali di `LoadClips` (88–105) dengan `Resources.Load<AudioClip>("Audio/SFX/<nama>")`.
- Folder: `Assets/EnerGo/Resources/Audio/SFX/`, semua file `.mp3`.
- Volume utama = `AudioListener.volume`, disimpan di PlayerPrefs `EnerGo.Volume` lewat `SFXManager.SetVolume` (21).
- Pola pemanggilan di semua script: `if (SFXManager.Instance != null) SFXManager.Instance.PlayButtonClick();`

| File (tanpa ekstensi) | Fungsi pemutar | Dipanggil dari |
|---|---|---|
| `sfx_splash` | `PlaySplash` | `LobbyController.Awake` (85) |
| `music_lobby` | `PlayLobbyMusic` / `StopLobbyMusic` | `LobbyController.Start` (102), `Update` (139, 147) |
| `sfx_button_click` | `PlayButtonClick` | Lobby (semua tombol), Radar `Capture` (261), layar tangkap (1310, 1609, 1800, 1872), Uranium (193) |
| `sfx_button_back` | `PlayButtonBack` | Lobby `DrawSheet` (370), SELESAI (462); Radar ‹ LOBBY (94); tangkap ‹ RADAR/LOBBY (1595) |
| `sfx_matahari` (loop) | `PlayMatahari`, `SetMatahariMix`, `StopMatahari` | `ARCapturePrototype.UpdateSun` (760–775) |
| `sfx_ekstrak_matahari` | `SyncEkstrakMatahari`, `StopEkstrakMatahari` | `UpdateSun` (754) |
| `sfx_bara_pukul` | `PlayBaraPukul` | `HitCoal` (954) |
| `sfx_bara_hancur` | `PlayBaraHancur` | `HitCoal` (961) |
| `sfx_sungai` (loop) | `PlaySungai`, `StopSungai` | `Start` (286), `OnDestroy` (364) |
| `sfx_aliran_air` (loop) | `PlayAliranAir`, `SetAliranAirVolume`, `StopAliranAir` | `UpdateGoldPan` (1128, 1141), 1188, 1207, 1374 |
| `sfx_quiz_benar` | `PlayQuizBenar` | `CommitAnswer` (704) |
| `sfx_quiz_salah` | `PlayQuizSalah` | `CommitAnswer` (705), waktu habis (506), `FailOil` (Oil 172), `FailUranium` (Uranium 265) |
| `sfx_batu_jatuh` | `PlayPumpBeat` (pitch 0,6, placeholder detak pompa); `PlayBatuJatuh` tidak dipanggil | `OilStroke` (Oil 151, 161) |
| `sfx_dulang_emas` | `PlayDulangEmas` | Tidak dipanggil di mana pun |
| `sfx_pickaxe` | `PlayPickaxe` | Tidak dipanggil di mana pun |
| (dibuat dari kode) `geiger_click` | `PlayGeigerClick` | `TickGeiger` (Uranium 135) |

Akibat untuk ElevenLabs:

- File baru dengan nama sama di folder ini langsung terpakai tanpa ubah kode. `Resources.Load` tidak peduli ekstensi, jadi `.wav` juga boleh, asal **file `.mp3` lama dihapus** (dua file bernama sama di satu folder Resources membuat salah satunya tidak terpanggil).
- Tutorial menyarankan `Assets/Audio/`. Folder itu **tidak** dibaca kode; selama SFXManager belum diubah, simpan di `Assets/EnerGo/Resources/Audio/SFX/`.
- 8 suara baru di `prompt-aset.md` (`sfx_radar_ping`, `sfx_hitung_mundur`, `sfx_bor`, `sfx_blowout`, `sfx_jepit`, `sfx_serpihan_masuk`, `sfx_gagal`, `sfx_hasil_sempurna`) belum punya field, baris `LoadClips`, maupun fungsi `Play...`. `sfx_pompa_detak` juga baru: sekarang `PlayPumpBeat` memutar `sfx_batu_jatuh`. Semuanya butuh perubahan kecil di `SFXManager` plus satu baris pemanggil.
- `sfx_dulang_emas` sudah dimuat tetapi pemanggilnya (emas tumpah, di `StepGoldPieces` 1150) belum ada.

## 3. Model dan prefab SDA

Semua model dimuat dengan `Resources.Load<GameObject>` dari `Assets/EnerGo/Resources/`. Tidak ada prefab: yang dimuat langsung file `.fbx`. Tidak ada Addressables, AssetBundle, atau referensi Inspector.

| SDA | Path yang dipanggil kode | File sekarang | Lokasi kode |
|---|---|---|---|
| Emas | `"Gold/GoldModel"` + tekstur `Resources.Load<Texture2D>("Gold/texture")` | `Gold/GoldModel.fbx`, `Gold/texture.png` | `ARCapturePrototype.Start` (259, 275); dipakai `SpawnGold` (1369) |
| Batu bara | `"Coal/bara_" + (i + 1)` → `Coal/bara_1` … `Coal/bara_4` | `Coal/bara_1.fbx` … `bara_4.fbx` | `SpawnCoalField` (865); tahap diganti di `SetCoalStage` (920) dengan mengganti model, bukan mengaktifkan child |
| Matahari | `"Sun/sun"` | `Sun/sun.fbx` | `HasSunModel` (1449); dibuat unlit lewat `MakeUnlit` (1454) |
| Minyak (pompa angguk) | Tidak memuat file. Dibuat dari `GameObject.CreatePrimitive` (kubus, silinder) | — | `ARCapturePrototype.Oil.cs` `SpawnOil` (39), `OilPart` (73). Komentar di kode: ganti dengan `Resources/Oil/pumpjack` kalau model sudah ada. Lengan = `oilBeam` (Transform) |
| Uranium (bijih, serpihan) | Tidak memuat file. Bola primitif dengan material unlit hijau | — | `ARCapturePrototype.Uranium.cs` `MakeUraniumOre` (47), `UnlitColor` (35) |
| Dulang, keping emas, endapan | Tidak ada model 3D. Dulang dan keping digambar 2D di IMGUI | — | `DrawGoldPan` (1223), `DrawGoldPiece` (1316) |

Aset lain yang dimuat dari Resources:

- Font IMGUI: `Fonts/Fredoka-SemiBold`, `Fonts/Fredoka-Bold`, `Fonts/LilitaOne-Regular` (`EnerGoUI.cs` 63, 64, 98).
- Ikon: `UI/Icons/<nama>` (`EnerGoUI.Draw.cs` 196). Nama: arrow, bolt, book, check, clock, coal, compass, cross, dice, gear, gold, oil, star, star_empty, sun, target, tree, uranium. Ikon SDA dipanggil dengan id SDA (`sun`, `coal`, `gold`, `oil`, `uranium`), Acak dengan `dice`.
- Logo: `UI/logo_game`, `UI/logo_wordmark` (`LobbyController.cs` 108–109).

**Ketidakcocokan nama dengan `prompt-aset.md`.** Dokumen aset memberi nama baru (`sda_batubara_1..4`, `sda_matahari`, `sda_emas_endapan`, `sda_pompa`, `sda_uranium_bijih`, …) dan menyarankan `Assets/Models/` + prefab di `Assets/Resources/`. Kode sekarang memanggil nama lama di `Assets/EnerGo/Resources/`. Pilihannya: simpan prefab baru dengan nama lama (`Coal/bara_1`, `Sun/sun`, `Gold/GoldModel`), atau ubah string path di kode. Rencana "empat tahap batu bara jadi empat child di satu prefab" juga butuh perubahan `SetCoalStage`, karena kode sekarang memuat 4 aset terpisah.

## 4. Tombol di Lobby

Semua tombol Lobby ada di `LobbyController.cs`. Setiap tombol: putar SFX, lalu ubah `currentState` atau pindah scene. Ketukan dideteksi dengan `ButtonHit` (593) → `EnerGoUI.TapHit`, dan hanya aktif jika `inputCooldownTimer <= 0`.

### Lobby utama (`DrawLobby`)

| Tombol | Baris | Yang dilakukan |
|---|---|---|
| BERBURU! | 304–311 | `PlayButtonClick()`; `ExploreSession.PendingSpawnId = null`; `transitionScene = "ExploreScene"`; `currentState = TransitioningToAR`; `transitionTimer = 0`. Setelah 0,45 dtk `Update` memanggil `StopLobbyMusic()` lalu `SceneManager.LoadScene("ExploreScene")` |
| Panduan (dock) | 327–332 | `PlayButtonClick()`; `currentState = Guides`; cooldown 0,2 dtk |
| Latihan (dock) | 327–332 | `PlayButtonClick()`; `currentState = Practice`; cooldown 0,2 dtk |
| Pengaturan (dock) | 327–332 | `PlayButtonClick()`; `currentState = Settings`; cooldown 0,2 dtk |
| Tile koleksi | 290–298 | Bukan tombol (hanya tampilan) |
| Pil total, judul, info mode kamera | 254–301 | Hanya tampilan. Total = `UiLayout.TotalUnits(...)` dari `EnerGoProgress.Current.Get(id).Total` |

### Bingkai sheet (`DrawSheet`, 362)

| Aksi | Yang dilakukan |
|---|---|
| Ketuk di luar kartu | `PlayButtonBack()`; `currentState = Lobby`; cooldown 0,2 dtk. Deteksi di `OutsideTap` (575) |

### Pengaturan (`DrawSettings`)

| Tombol | Baris | Yang dilakukan |
|---|---|---|
| Toggle Mode AR | 392–398 | `useARCore = !useARCore`; `PlayerPrefs.SetInt("EnerGo.UseARCore", …)`; `PlayerPrefs.Save()`. Klik + cooldown 0,15 dtk di `DrawToggleRow` |
| Slider Volume | 402–424 | Saat ditahan: `AudioListener.volume = posisi`. Saat dilepas: `SFXManager.SetVolume(AudioListener.volume)` + `PlayButtonClick()` |
| Toggle Getaran | 427–431 | `Haptics.SetEnabled(!Haptics.Enabled)`; `Haptics.Pulse(60)` |
| LATIHAN (TANPA GPS) | 434–442 | `PlayButtonClick()`; `currentState = Practice` |
| RESET PROGRES | 446–455 | `PlayButtonClick()`; ketuk 1: `isConfirmingReset = true`, `resetConfirmTimer = 3.5f` (hitung mundur di `Update` 116–120); ketuk 2: `EnerGoProgress.Reset()` |
| SELESAI | 458–465 | `PlayButtonBack()`; `currentState = Lobby` |

### Latihan (`DrawPractice`)

| Tombol | Baris | Yang dilakukan |
|---|---|---|
| 6 tile (5 SDA + Acak) | 482–492 | `PlayButtonClick()`; `selectedResource = id`; `PlayerPrefs.SetString("EnerGo.SelectedResource", id)`; cooldown 0,15 dtk. Acak memakai id `"random"` |
| MULAI LATIHAN | 496–505 | `PlayButtonClick()`; `ResourceIds.Selected = selectedResource` (atau acak dari 5 SDA); `ExploreSession.PendingSpawnId = null`; `transitionScene = CaptureSceneName()`; `currentState = TransitioningToAR` |

### Panduan (`DrawGuides`)

| Tombol | Baris | Yang dilakukan |
|---|---|---|
| 5 tile SDA | 526–535 | `PlayButtonClick()`; `lobbyGuide = new GuideViewer(id, withStart: false)` |
| Kartu panduan terbuka | 512–516 | `lobbyGuide.Draw(...)`; jika hasil bukan `None` → `lobbyGuide = null` |
| TAMPILKAN SEMUA PANDUAN LAGI | 537–546 | `PlayButtonClick()`; `GuideViewer.ResetAll()`; teks "Panduan akan muncul lagi" 2 dtk |

### Perilaku Lobby di luar tombol

- `Awake` (74): `Application.targetFrameRate = 60`, baca PlayerPrefs, lewati splash jika `hasShownSplash`, buka sheet Latihan jika `OpenPracticeOnLoad` (diset radar).
- `Start` (91): jika ada `pendingCapture` (kuis belum selesai), langsung transisi ke scene tangkap.
- `OnApplicationPause` (72): `EnerGoProgress.Commit(...)`.
- `KeepScreenAwake` (67): layar tidak pernah meredup, untuk seluruh aplikasi.

## 5. Risiko memindah Lobby dari IMGUI ke UI Toolkit

1. **Satu OnGUI menggambar lima layar.** Splash, Lobby, tiga sheet, dan transisi ada di satu `OnGUI` dan dikendalikan satu enum `currentState`. Kalau hanya Lobby yang pindah, cabang `DrawLobby` harus dimatikan tanpa mematikan `DrawSplash`, `DrawSettings`, `DrawPractice`, `DrawGuides`, `DrawTransition`. `DrawLobby` juga dipanggil di bawah semua sheet, jadi Lobby UI Toolkit harus tetap tampil (tapi tidak bisa diketuk) saat `currentState` adalah sheet atau transisi.
2. **Latar dunia digambar IMGUI.** `DrawWorld` dipanggil di awal `OnGUI` (178) dan menutup seluruh layar. IMGUI digambar di atas panel UI Toolkit, jadi kalau `DrawWorld` tetap jalan, Lobby UI Toolkit akan tertutup. Latar harus ikut pindah ke UI Toolkit, atau `DrawWorld` hanya dipanggil di splash dan sheet.
3. **Ketukan ganda antara dua sistem UI.** Sheet IMGUI tetap digambar di atas Lobby UI Toolkit. Ketukan pada sheet bisa juga diterima tombol UI Toolkit di bawahnya (misalnya BERBURU! di bawah tombol SELESAI). Kode lama mencegah ini dengan `inputCooldownTimer` (0,15–0,25 dtk) dan `EnerGoUI.TapHit` yang memblokir ketukan 0,35 dtk lewat `tapBlockedUntil`. Tombol UI Toolkit tidak memeriksa keduanya. Paling aman: `pickingMode = Ignore` / `SetEnabled(false)` pada root Lobby selama `currentState != Lobby` dan selama cooldown.
4. **Input System saja.** `activeInputHandler: 1` (hanya Input System 1.20). Lobby tidak punya EventSystem. UI Toolkit runtime perlu bisa membaca sentuhan; ini harus dicek di HP. Kalau tombol tidak merespons, tambahkan EventSystem + `InputSystemUIInputModule`. Splash dan sheet tetap memakai `EnhancedTouch`, jadi `EnhancedTouchSupport.Enable()` di `OnEnable` jangan dihapus.
5. **Keadaan ditekan.** Tampilan tombol "turun saat ditekan" sekarang dihitung tiap frame dengan `IsHeld`. Di UI Toolkit diganti pseudo-class `:active`, perilakunya sedikit berbeda (Android juga mengirim sentuhan sebagai mouse; IMGUI sudah mengakalinya di `TapHit`).
6. **Skala dan safe area berbeda.** IMGUI memakai `HudScale = max(1, min(W/480, H/800))` sehingga layar tinggi mendapat ruang vertikal lebih (sh > 800) dan layar kecil tidak pernah diperkecil di bawah 1. Panel Settings di tutorial memakai Match 0 (ikut lebar). Hasilnya mirip di HP portrait biasa, tetapi tidak identik di layar lebih kecil dari 480 px atau tablet. Safe area sekarang dari `EnerGoUI.SafeTop/SafeBottom` (`UiLayout`). Script `SafeArea` di tutorial memakai `root.layout.width / Screen.width`, yang benar hanya jika panel mengikuti lebar.
7. **Tata letak dihitung dari bawah.** Posisi dock, BERBURU!, grid, dan judul dihitung dari bawah layar ke atas, dan judul diletakkan di tengah ruang sisa (265–275). Flexbox harus meniru urutan ini, terutama di layar pendek.
8. **Data hanya dibaca saat menggambar.** Total unit, jumlah per tile, dan teks mode kamera dibaca ulang setiap frame. Di UI Toolkit tidak ada event saat data berubah, jadi label harus diperbarui saat Lobby tampil lagi (setelah sheet Pengaturan mengubah mode AR, setelah RESET PROGRES mengosongkan koleksi). Mode AR disimpan di field privat `useARCore`, bukan properti publik.
9. **Fungsi tombol bersifat privat.** Contoh di tutorial (`LobbyController.MulaiBerburu()`, `Progres.TotalUnit`) tidak ada. Aksi BERBURU! adalah blok di dalam `DrawLobby` yang mengubah field privat (`transitionScene`, `currentState`, `transitionTimer`). Untuk memanggilnya dari `LobbyView`, blok itu perlu dipindah ke metode publik tanpa mengubah isinya. Itu satu-satunya perubahan pada `LobbyController` yang tidak bisa dihindari.
10. **Tile terkunci.** `ResourceIds.IsPlayable` sekarang true untuk kelima SDA, tetapi PRD meminta state terkunci tetap didesain. Versi UI Toolkit harus tetap membaca `IsPlayable`.
11. **Transisi kuis tertunda.** Jika `pendingCapture` ada, `Start` langsung masuk transisi ke scene tangkap. Lobby UI Toolkit tidak boleh sempat bisa diketuk di frame itu.
12. **Font dan aset.** Font IMGUI (Fredoka, Lilita One) ada di Resources. Design system memakai Bricolage, Lexend, Space Mono, Caveat yang belum dibuat sebagai Font Asset statis. Ikon dan logo baru ada di `docs/design-system/assets`, belum di proyek. Selama masa transisi, Lobby (gaya Jurnal Ekspedisi) akan berdampingan dengan sheet dan radar gaya Arcade Cerah.
13. **Paket.** `com.unity.modules.uielements` sudah ada di manifest. Folder `Assets/UI/`, PanelSettings, dan UIDocument belum ada.

## 6. MCP di sesi ini

Diperiksa dengan `claude mcp list` dan daftar alat yang tersedia di sesi ini.

| MCP | Status |
|---|---|
| Blender | **Tidak tersedia.** Tidak terdaftar di `claude mcp list` maupun di `.mcp.json`, dan tidak ada alat Blender di sesi. |
| ElevenLabs | **Tidak tersedia.** Tidak terdaftar, tidak ada alat ElevenLabs di sesi. Karena itu daftar alatnya tidak bisa diperiksa langsung. |
| Unity (`ai-game-developer`, paket `com.ivanmurzak.unity.mcp` 1.6.0) | Terdaftar di `.mcp.json` (http://localhost:22701). Alatnya tersedia sebagai skill: scene, GameObject, asset, prefab, script, screenshot, console, tests, dll. Butuh Unity Editor terbuka. |
| Lainnya yang tersambung | Claude Docs, Canva, Google Drive, Google Calendar, Gmail, Playwright. Notion dan Figma butuh login. |

**Tentang alat ElevenLabs** (dari pengetahuan umum tentang server resmi `elevenlabs-mcp`, belum diverifikasi di sesi ini karena tidak terpasang): server itu punya alat `text_to_sound_effects` (teks → file sound effect, dengan durasi 0,5–5 detik, direktori output, dan format output), selain alat suara/TTS, speech-to-text, voice clone, isolasi audio, dan putar audio. Dua hal yang perlu dicek setelah dipasang:

- Batas durasinya mungkin lebih pendek dari 30 detik di `prompt-aset.md`. Kalau masih 5 detik, `sfx_matahari` (10 dtk), `sfx_sungai` (15 dtk), dan `sfx_aliran_air` (6 dtk) harus dibuat lewat web.
- Apakah ada opsi loop dan pilihan model `eleven_text_to_sound_v3`.

Cara memasang (untuk dicoba pengguna): `claude mcp add elevenlabs -e ELEVENLABS_API_KEY=... -- uvx elevenlabs-mcp`, dan untuk Blender `claude mcp add blender -- uvx blender-mcp` (sesuai tutorial), lalu mulai sesi baru dan cek `/mcp`.

## Ringkasan

- Ada 4 scene build dan hanya 3 `OnGUI`: `LobbyController` (splash, lobby, 3 sheet, transisi), `RadarController` (radar, GPS, keselamatan), dan `ARCapturePrototype` (HUD, 5 mini-game, kuis, umpan balik, hasil, dipakai bersama oleh scene ARCore dan Sensor 360). `GuideViewer` dipakai di dua tempat.
- Semua suara lewat singleton `SFXManager`, dimuat dari `Assets/EnerGo/Resources/Audio/SFX/`. Ganti file dengan nama sama = tanpa ubah kode. 9 suara baru di prompt-aset butuh fungsi baru di `SFXManager`.
- Model SDA dimuat dari `Assets/EnerGo/Resources/` sebagai FBX tanpa prefab (`Gold/GoldModel`, `Coal/bara_1..4`, `Sun/sun`). Minyak dan uranium masih primitif buatan kode, dulang dan keping emas digambar 2D.
- Kelima tombol Lobby hanya mengubah `currentState` atau memulai transisi. Risiko utama pemindahan: latar IMGUI menutupi UI Toolkit, ketukan ganda antara sheet IMGUI dan Lobby UI Toolkit, dan logika tombol yang privat di dalam fungsi gambar.
- Blender MCP dan ElevenLabs MCP belum terpasang di sesi ini. MCP Unity terdaftar.

## Keputusan (3 Nov 2026)

1. Aset dan suara memakai path dan nama lama di `Assets/EnerGo/Resources/` (`Coal/bara_1..4`, `Sun/sun`, `Gold/GoldModel`, `Audio/SFX/<nama>`). Suara baru masuk ke folder yang sama.
2. Boleh menjadikan aksi tombol Lobby sebagai metode publik `LobbyController` (isi tidak berubah).
3. Seluruh LobbyScene dipindah sekaligus: Splash, Lobby, 3 sheet, transisi, dan kartu panduan mode baca. `OnGUI` LobbyController dimatikan seluruhnya, jadi tidak ada lagi IMGUI dan UI Toolkit di scene yang sama. Radar dan layar tangkap tetap IMGUI sampai gilirannya (beda scene, tidak saling tumpuk). `GuideViewer` IMGUI tetap dipakai layar tangkap; Lobby memakai versi UI Toolkit yang membaca `GuideBook` yang sama.
4. `PlayPumpBeat` diarahkan ke `sfx_pompa_detak` begitu file itu ada.

Update MCP: Blender terdaftar sebagai `blender` (`uvx mcp-for-blender`, bukan `blender-mcp` seperti di tutorial) dan berstatus Connected. ElevenLabs belum terdaftar sebagai MCP; suara bisa dibuat langsung lewat REST API (`POST https://api.elevenlabs.io/v1/sound-generation`, parameter `text`, `duration_seconds`, `prompt_influence`, `loop`, `model_id`) dengan kunci di env `ELEVENLABS_API_KEY`.

## Pertanyaan (sudah dijawab, lihat Keputusan)

1. **Lokasi aset dan nama file.** Tutorial dan prompt-aset menyarankan `Assets/Audio/`, `Assets/Models/`, dan `Assets/Resources/` dengan nama baru (`sda_batubara_1`, `sda_matahari`, …), tetapi kode memuat dari `Assets/EnerGo/Resources/` dengan nama lama (`Coal/bara_1`, `Sun/sun`). Mau tetap memakai path dan nama lama (tanpa ubah kode), atau mengubah path di kode mengikuti dokumen aset?
2. **Perubahan kecil di `LobbyController`.** Untuk menyambungkan tombol UI Toolkit, aksi BERBURU! dan buka sheet perlu dijadikan metode publik (isi sama). Apakah ini dihitung "mengubah logika game", atau boleh?
3. **Latar dunia.** Lobby baru memakai peta topografi (Jurnal Ekspedisi), sementara sheet, radar, dan kuis masih Arcade Cerah. Apakah tampilan campur ini dapat diterima selama Tahap 1a, atau sheet ikut dipindah bersamaan?
4. **Prompt-aset menyebut 21 suara dengan 13 nama lama**, tapi `sfx_pompa_detak` belum ada di kode (sekarang memakai `sfx_batu_jatuh`). Apakah `PlayPumpBeat` boleh diubah ke `sfx_pompa_detak` saat file itu dibuat?
