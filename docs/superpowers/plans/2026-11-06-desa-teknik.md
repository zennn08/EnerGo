# Desa Teknik, Prolog, dan Sya Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Prolog sekali di awal dengan pemandu Sya, nama desa dari pemain, dan layar Desa berisi pembangkit yang menyala dari unit SDA dengan stok waktu nyata.

**Architecture:** Aturan murni di `Core/DesaMath.cs` dan `Core/DesaText.cs`, diuji NUnit. Data di `SaveGame`, ditulis lewat `EnerGoProgress`. Dua layar baru menjadi lapisan di `LobbyScene` (`Prolog.uxml`, `Desa.uxml`), mengikuti pola state di `LobbyController` dan partial class `LobbyView`, jadi tidak ada scene baru.

**Tech Stack:** Unity 6000.3.24f1, UI Toolkit, NUnit EditMode, Python + Pillow + SciPy untuk memproses gambar.

**Spec:** `docs/superpowers/specs/2026-11-06-desa-teknik-design.md`

## Global Constraints

- Bahasa UI Indonesia, sapaan "kamu", instruksi huruf kapital dan pendek, tanpa emoji, angka alat dengan satuan ("5 j 12 m").
- Teks di layar selalu di atas kartu atau pil `kertas-100`. Satuan UI = unit dasar 480 x 800. Nama variabel USS = token design system.
- Teks tidak boleh menyebut emas sebagai bahan bakar atau sumber energi.
- Padam tidak menghukum: unit, koleksi, dan nama desa tidak berkurang.
- APK dibangun hanya bila pengguna meminta. Jangan push atau merge tanpa diminta.
- Commit hanya menyebut path yang dikerjakan. Pohon kerja punya perubahan lain yang tidak boleh ikut (`.claude/skills`, `Assets/Plugins/NuGet`, `Assets/Fonts/* SDF.asset`, `Assets/UI/PanelSettings.asset`, `Packages/*`, `ProjectSettings/ProjectSettings.asset`).
- Akhiri pesan commit dengan `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`.
- Sebelum uji Play Mode yang bisa menulis save, cadangkan `energo-demo-v1.json*` dari `Application.persistentDataPath`, lalu pulihkan.

## Review Focus

- Jam HP dimundurkan atau dimajukan: stok tidak pernah tampil di atas 24 jam dan tidak error (Task 1).
- Stok habis saat layar Desa terbuka: gambar dan cap PADAM berubah tanpa harus membuka ulang (Task 1, Task 6).
- Nama berisi tag seperti `<color=red>`, hanya spasi, atau lebih dari 16 karakter: tampil apa adanya sebagai teks (Task 1, Task 2).
- Unit tidak muat, misalnya uranium saat sisa 22 jam, atau ketukan cepat di ISI SEMUA: unit tidak hilang dan stok tidak lewat kapasitas (Task 1, Task 2).
- Pembukaan pertama dengan tangkapan tertunda di save lama: tangkapan diselesaikan dulu, prolog muncul setelah kembali ke Lobby (Task 5).

---

### Task 1: Aturan stok dan nama

**Files:**
- Create: `Assets/EnerGo/Scripts/Core/DesaMath.cs`
- Test: `Assets/EnerGo/Tests/Editor/DesaMathTests.cs`

**Interfaces:**
- Produces, semua di `public static class DesaMath` (namespace `EnerGo`):
  - `public enum DesaStatus { Padam, Menipis, Menyala }` di namespace yang sama
  - `const long CapacitySec = 86400, LowSec = 10800`; `const int NameMaxLength = 16`; `const string DefaultName = "Desa Teknik"`
  - `long SecondsPerUnit(string resourceId)`: sun 1200, coal 1200, gold 1800, oil 2700, uranium 10800, id lain `ArgumentException`
  - `long Remaining(long endsUtc, long nowUtc)`: di antara 0 dan `CapacitySec`
  - `DesaStatus StatusOf(long remaining)`
  - `int UnitsThatFit(long remaining, long secPerUnit, int owned)`
  - `long EndsAfterFill(long endsUtc, long nowUtc, long secPerUnit, int units)`
  - `void Spend(ref int normal, ref int pure, int units)`: Normal dulu
  - `string CleanName(string raw)`
  - `int CaptureReward(bool fromRadar, int correct, int bonus)`
  - `Vector2 CoverPoint(float imgW, float imgH, float boxW, float boxH, float u, float v)`: titik (u, v) di gambar yang digambar cover di dalam kotak

- [ ] **Step 1: Buat branch `feat/desa-teknik` dari branch sekarang, dan commit dokumen**

```bash
git switch -c feat/desa-teknik
git add docs/superpowers docs/prompt-karakter.md
git commit -m "docs: design and prompts for the village, prologue and guide"
```

- [ ] **Step 2: Tulis pengujian yang gagal**

```csharp
const long Now = 1000000;
[Test] public void RemainingNeverNegativeAndNeverAboveCapacity() {
    Assert.AreEqual(0, DesaMath.Remaining(Now - 5, Now));
    Assert.AreEqual(3600, DesaMath.Remaining(Now + 3600, Now));
    Assert.AreEqual(86400, DesaMath.Remaining(Now + 30 * 3600, Now)); // clock moved back
}
[Test] public void StatusBoundaries() {
    Assert.AreEqual(DesaStatus.Padam, DesaMath.StatusOf(0));
    Assert.AreEqual(DesaStatus.Menipis, DesaMath.StatusOf(10799));
    Assert.AreEqual(DesaStatus.Menyala, DesaMath.StatusOf(10800));
}
[Test] public void SecondsPerUnitMatchesTheSpec() { /* sun 1200, coal 1200, gold 1800, oil 2700, uranium 10800; "x" throws */ }
[Test] public void FillFromEmptyStartsNowAndFromStockExtends() {
    Assert.AreEqual(Now + 2400, DesaMath.EndsAfterFill(Now - 99, Now, 1200, 2));
    Assert.AreEqual(Now + 500 + 1200, DesaMath.EndsAfterFill(Now + 500, Now, 1200, 1));
}
[Test] public void UnitsThatDoNotFitAreRefused() {
    Assert.AreEqual(0, DesaMath.UnitsThatFit(22 * 3600, 10800, 5)); // uranium at 22 h
    Assert.AreEqual(1, DesaMath.UnitsThatFit(21 * 3600, 10800, 5)); // exactly reaches 24 h
    Assert.AreEqual(2, DesaMath.UnitsThatFit(0, 10800, 2));          // limited by what is owned
    Assert.AreEqual(0, DesaMath.UnitsThatFit(86400, 1200, 9));
}
[Test] public void SpendUsesNormalBeforePure() { int n = 2, p = 3; DesaMath.Spend(ref n, ref p, 3); Assert.AreEqual(0, n); Assert.AreEqual(2, p); }
[Test] public void CleanName() {
    Assert.AreEqual("Desa Teknik", DesaMath.CleanName("   "));
    Assert.AreEqual("Desa Teknik", DesaMath.CleanName(null));
    Assert.AreEqual("Sukamaju", DesaMath.CleanName("  Sukamaju "));
    Assert.AreEqual(16, DesaMath.CleanName(new string('a', 40)).Length);
    Assert.AreEqual("a b", DesaMath.CleanName("a\nb"));
}
[Test] public void PracticeAlwaysPaysOneAndRadarUsesTheFormula() {
    Assert.AreEqual(1, DesaMath.CaptureReward(false, 3, 2));
    Assert.AreEqual(6, DesaMath.CaptureReward(true, 3, 2));
    Assert.AreEqual(1, DesaMath.CaptureReward(true, 0, 0));
}
[Test] public void CoverPointCropsTheLongerSide() {
    // A 9:16 image in a taller 480 x 1067 box scales by height and crops the sides.
    var mid = DesaMath.CoverPoint(1080, 1920, 480, 1067, .5f, .5f);
    Assert.AreEqual(240f, mid.x, .5f); Assert.AreEqual(533.5f, mid.y, .5f);
    Assert.AreEqual(-60f, DesaMath.CoverPoint(1080, 1920, 480, 1067, 0f, 0f).x, 1f);
}
```

- [ ] **Step 3: Jalankan, pastikan gagal.** Lewat Unity MCP `tests-run`, EditMode, filter `EnerGo.Tests.DesaMathTests`. Expected: gagal kompilasi karena `DesaMath` belum ada.

- [ ] **Step 4: Tulis `DesaMath`.** `CleanName`: ganti karakter kontrol dengan spasi, trim, potong ke 16, kosong menjadi `DefaultName`. `EndsAfterFill`: `Math.Max(now, ends) + sec * units`. `UnitsThatFit`: `min(owned, (CapacitySec - remaining) / secPerUnit)`.

- [ ] **Step 5: Jalankan semua pengujian EditMode.** Expected: semua lulus.

- [ ] **Step 6: Commit** `Assets/EnerGo/Scripts/Core/DesaMath.cs*` dan `Assets/EnerGo/Tests/Editor/DesaMathTests.cs*` dengan `feat: village stock, name and practice reward rules`.

### Task 2: Save, aturan Latihan, dan teks

**Files:**
- Modify: `Assets/EnerGo/Scripts/EnerGoProgress.cs` (`SaveGame`, `Valid`, `Answer`, API baru)
- Modify: `Assets/EnerGo/Scripts/ARCapturePrototype.cs:1560` (`ResultChips`)
- Modify: `Assets/UI/Screens/SheetLatihan.uxml:6`
- Create: `Assets/EnerGo/Scripts/Core/DesaText.cs`
- Test: `Assets/EnerGo/Tests/Editor/DesaTextTests.cs`

**Interfaces:**
- Consumes: semua `DesaMath` dari Task 1.
- Produces:
  - `SaveGame`: `public long plantEndsUtc; public string villageName = ""; public bool prologSeen; public bool plantEverFilled;`
  - `int EnerGoProgress.FillPlant(string resourceId, int units, long nowUtc)`: jumlah unit yang benar-benar dipakai, 0 bila tidak muat atau gagal simpan. Unit, stok, dan `plantEverFilled` ditulis dalam satu `Commit`.
  - `bool EnerGoProgress.FinishProlog(string rawName)`, `bool EnerGoProgress.RenameVillage(string rawName)`
  - `DesaText`: `string[] Prolog` (5 baris, `{nama}` di baris terakhir), `string Status(DesaStatus s, bool everFilled)`, `string SourceOf(string id)`, `string FormatSisa(long sec)`, `string PoseFor(DesaStatus s, bool isDay, bool everFilled)`, `string WajahFor(DesaStatus s, bool everFilled)`, `string BackgroundFor(DesaStatus s, bool isDay)`

- [ ] **Step 1: Tulis pengujian `DesaTextTests` yang gagal**

```csharp
[Test] public void GoldIsNeverCalledAFuel() {
    StringAssert.DoesNotContain("bahan bakar", DesaText.SourceOf(ResourceIds.Gold).ToLowerInvariant());
    StringAssert.Contains("kabel", DesaText.SourceOf(ResourceIds.Gold).ToLowerInvariant());
}
[Test] public void FormatSisa() {
    Assert.AreEqual("5 j 12 m", DesaText.FormatSisa(5 * 3600 + 12 * 60));
    Assert.AreEqual("12 m", DesaText.FormatSisa(12 * 60));
    Assert.AreEqual("1 m", DesaText.FormatSisa(20));  // never reads 0 while some stock is left
    Assert.AreEqual("0 m", DesaText.FormatSisa(0));
}
[Test] public void BackgroundFollowsClockAndStock() {
    Assert.AreEqual("desa_siang", DesaText.BackgroundFor(DesaStatus.Padam, true));
    Assert.AreEqual("desa_malam_padam", DesaText.BackgroundFor(DesaStatus.Padam, false));
    Assert.AreEqual("desa_malam_terang", DesaText.BackgroundFor(DesaStatus.Menipis, false));
}
[Test] public void PoseFilesMatchTheStatus() {
    Assert.AreEqual("pemandu_menunjuk", DesaText.PoseFor(DesaStatus.Padam, true, false));   // never filled: points at the plant
    Assert.AreEqual("pemandu_khawatir", DesaText.PoseFor(DesaStatus.Padam, true, true));
    Assert.AreEqual("pemandu_menunjuk", DesaText.PoseFor(DesaStatus.Menipis, true, true));
    Assert.AreEqual("pemandu_melambai", DesaText.PoseFor(DesaStatus.Menyala, true, true));
    Assert.AreEqual("pemandu_santai", DesaText.PoseFor(DesaStatus.Menyala, false, true));
}
```

Tambahkan ke `LobbyUxmlTests`: `SheetLatihan.uxml` berisi teks `1 unit` pada label `.sheet__sub`.

- [ ] **Step 2: Jalankan, pastikan gagal.** Expected: `DesaText` belum ada.

- [ ] **Step 3: Tulis `DesaText` dengan teks persis ini**

```text
Prolog:
1 "Radarmu berbunyi di tengah malam. Ada sinyal energi di balik bukit."
2 "Kamu sampai di sebuah desa. Tidak ada satu pun lampu yang menyala."
3 "Halo, penjelajah! Aku Sya, ahli geologi di sini. Pembangkit desa kami kehabisan energi."
4 "Desa ini belum tertulis di petamu. Mau kamu beri nama apa?"
5 "Yuk, cari SDA di sekitar sini. Bawa pulang, lalu kita nyalakan {nama} bersama!"

Status:
belum pernah diisi "Kamu sudah bawa SDA? Ketuk pembangkit, yuk kita isi."
Padam   "Desa gelap lagi. Tenang, begitu ada SDA kita nyalakan lagi."
Menipis "Listriknya mulai menipis. Sempat cari SDA lagi?"
Menyala "Terima kasih, desa terang berkat kamu!"

Sumber:
coal "Bahan bakar pembangkit"; sun "Dialirkan dari panel surya"; gold "Memperbaiki kabel dan sambungan";
oil "Bahan bakar pembangkit"; uranium "Bahan bakar reaktor"
```

`WajahFor`: belum pernah diisi `pemandu_wajah_netral`, Padam `pemandu_wajah_khawatir`, Menipis `pemandu_wajah_berpikir`, Menyala `pemandu_wajah_senang`.

- [ ] **Step 4: Perluas `SaveGame` dan `Valid`.** Tambah `plantEndsUtc >= 0` dan `(villageName == null || villageName.Length <= DesaMath.NameMaxLength)`. `schemaVersion` tetap 1.

- [ ] **Step 5: Tulis API `EnerGoProgress`.** `FillPlant` memakai `Copy()`, `UnitsThatFit` dengan `Get(id).Total`, `Spend`, `EndsAfterFill`, lalu `Commit`. Hasil 0 bila unit yang muat 0, dan bila `Commit` gagal.

- [ ] **Step 6: Terapkan aturan Latihan.** Di `Answer` ganti `RewardFor(...)` dengan `DesaMath.CaptureReward(!string.IsNullOrEmpty(session.spawnId), session.correctCount, session.performanceBonus)`. `ResultChips`: tanpa `spawnId` mengembalikan `new[] { "Mode Latihan +1" }`. `SheetLatihan.uxml`: sub menjadi `"Pilih sumber daya, main tanpa GPS · 1 unit per ronde"`. `RewardFor` tetap ada dan isinya memanggil `DesaMath.CaptureReward(true, correctCount, performanceBonus)`, supaya rumusnya hanya di satu tempat.

- [ ] **Step 7: Periksa save lewat `script-execute`.** Cadangkan save, lalu jalankan: `JsonUtility.FromJson<SaveGame>` pada JSON tanpa field baru menghasilkan `prologSeen == false`, `plantEndsUtc == 0`, `villageName` kosong; `FillPlant(ResourceIds.Coal, 2, now)` dengan 3 Normal dan 1 Murni menyisakan 1 Normal, 1 Murni, dan `plantEndsUtc == now + 2400`; `FillPlant(ResourceIds.Uranium, 1, now)` saat sisa 22 jam mengembalikan 0 dan unit tetap. Pulihkan save. Pengujian `EnerGo.Tests.Editor` hanya mereferensikan `EnerGo.Core`, jadi ini tidak bisa menjadi NUnit.

- [ ] **Step 8: Jalankan semua pengujian EditMode.** Expected: semua lulus.

- [ ] **Step 9: Commit** file di atas dengan `feat: save village stock and pay one unit per practice round`.

### Task 3: Proses gambar Sya

**Files:**
- Create: `Art/pemandu/potong.py`, `Art/pemandu/sumber/*.jpeg` (18 file dari `D:\Downloads\character-energo`)
- Create: `Assets/EnerGo/Resources/Pemandu/*.png` plus `.meta`

**Interfaces:**
- Produces: `Resources.Load<Texture2D>("Pemandu/<nama>")` untuk `pemandu_menunjuk`, `pemandu_khawatir`, `pemandu_melambai`, `pemandu_santai`, `pemandu_bersorak`, `pemandu_wajah_netral`, `pemandu_wajah_khawatir`, `pemandu_wajah_berpikir`, `pemandu_wajah_senang`.

- [ ] **Step 1: Tulis `potong.py`.** Near-white (kanal terendah >= 232) yang tersambung ke tepi menjadi transparan, buang pulau selain yang terbesar, kikis alpha 1 piksel dan haluskan (gaussian 0.7), potong dengan jarak 8 piksel. Tabel `HOLES` berisi titik benih per file untuk celah tertutup, mulai dari `pemandu_santai: [(910,959),(838,967)]`, `pemandu_memperingatkan: [(903,702)]`, `pemandu_menunjuk: [(902,703)]`. Potret `wajah_*` diskalakan agar lebar topi sama.

- [ ] **Step 2: Jalankan dan cek.** Buat lembar kontak di atas latar `#c0406a`, lalu lihat gambarnya. Expected: tidak ada sisa putih di tepi, celah pinggang dan bangku tembus, bagian mata dan kemeja tetap utuh, dan semua potret berukuran kepala sama.

- [ ] **Step 3: Salin ke Resources dan atur importer** lewat `script-execute`: `alphaIsTransparency = true`, tanpa mipmap, `maxTextureSize = 1024`.

- [ ] **Step 4: Commit** `Art/pemandu` dan `Assets/EnerGo/Resources/Pemandu` dengan `feat: guide character images with transparent backgrounds`.

### Task 4: Kerangka state dan tombol Desa

**Files:**
- Modify: `Assets/EnerGo/Scripts/LobbyController.cs`
- Modify: `Assets/EnerGo/Scripts/UI/LobbyView.cs`
- Modify: `Assets/UI/Screens/LobbyScene.uxml`, `Assets/UI/Screens/Lobby.uxml`, `Assets/UI/Screens/Layar.uss`
- Create: `Assets/UI/Screens/Prolog.uxml`, `Assets/UI/Screens/Desa.uxml`
- Create: `Assets/EnerGo/Scripts/UI/LobbyView.Gambar.cs`
- Test: `Assets/EnerGo/Tests/Editor/LobbyUxmlTests.cs`

**Interfaces:**
- Consumes: `prologSeen`, `plantEndsUtc`, `plantEverFilled`, `DesaMath.Remaining/StatusOf`.
- Produces:
  - `ScreenState.Prolog`, `ScreenState.Desa`
  - `LobbyController`: `bool NeedsProlog`, `long NowUtc`, `DesaStatus PlantStatus`, `bool DesaMarked`, `void OpenDesa()`, `void CloseDesa()`, `void FinishProlog(string rawName)`
  - `LobbyView`: `static void SetGambar(VisualElement target, string resourcePath, Color fallback)`, lapisan `lapis-prolog` dan `lapis-desa`, partial `BindProlog/UpdateProlog/ShowProlog` dan `BindDesa/UpdateDesa/RefreshDesa`
  - Nama UXML sesuai pengujian di Step 1

- [ ] **Step 1: Tulis pengujian nama yang gagal**

```csharp
[Test] public void LobbyHasDesaButton() => AssertNames(Clone("Assets/UI/Screens/Lobby.uxml"), "btn-desa", "desa-tanda");
[Test] public void PrologHasEveryNamedElement() => AssertNames(Clone("Assets/UI/Screens/Prolog.uxml"),
    "prolog-gambar", "prolog-kartu", "prolog-teks", "prolog-nama", "input-nama", "btn-prolog-lanjut", "btn-prolog-lewati");
[Test] public void DesaHasEveryNamedElement() => AssertNames(Clone("Assets/UI/Screens/Desa.uxml"),
    "desa-latar", "desa-malam-terang", "desa-awan", "desa-kerbau", "desa-sya", "btn-pembangkit", "pembangkit-mati", "desa-nama", "btn-ganti-nama", "desa-sisa",
    "desa-bar", "desa-padam", "desa-menipis", "btn-isi", "btn-desa-kembali", "dialog-wajah", "dialog-teks", "lapis-isi", "isi-daftar", "btn-isi-tutup", "lapis-sorak");
[Test] public void VillageNameIsPlainText() {
    var nama = Clone("Assets/UI/Screens/Desa.uxml").Q<Label>("desa-nama");
    Assert.IsFalse(nama.enableRichText);
}
```

- [ ] **Step 2: Jalankan, pastikan gagal.** Expected: UXML belum ada.

- [ ] **Step 3: Buat UXML dan USS.** Tiga kartu mengikuti design system: `eg-kartu` untuk kartu stok dan teks prolog, `eg-stempel` untuk `PADAM`, `eg-bar` dengan isian `oker`, `eg-pil` untuk kembali. Tombol Desa di dock sebagai `eg-bulat` pertama, ikon `Icons/rumah.svg`, label `Desa`, `desa-tanda` titik `oranye` di sudut. Gambar penuh memakai `-unity-background-scale-mode: scale-and-crop`.

- [ ] **Step 4: Tulis state.** `OpenDesa`/`CloseDesa` mengikuti pola `OpenSheet`/`CloseSheet`. `DesaMarked`: punya unit tetapi belum pernah mengisi, atau status Menipis. `SetGambar` memakai `Resources.Load<Texture2D>`, dan `style.backgroundColor = fallback` saat gambar belum ada, supaya layar bisa dibangun sebelum aset jadi. `CloseDesa` memanggil `Resources.UnloadUnusedAssets()`. `ShowLayers` menyembunyikan `lapis-lobby` saat Prolog dan Desa.

- [ ] **Step 5: Jalankan pengujian, lalu ambil gambar Lobby** dengan resep `unity-mcp-screenshot-recipe`. Expected: lulus, dan dock menampung 4 tombol tanpa teks terpotong.

- [ ] **Step 6: Commit** dengan `feat: lobby layers and dock button for the village`.

### Task 5: Layar prolog

**Files:**
- Create: `Assets/EnerGo/Scripts/UI/LobbyView.Prolog.cs`
- Modify: `Assets/EnerGo/Scripts/LobbyController.cs`

**Interfaces:**
- Consumes: `DesaText.Prolog`, `EnerGoProgress.FinishProlog`, `SetGambar`, `NeedsProlog`.
- Produces: alur Splash ke Prolog ke Lobby. Gambar di `Resources/Prolog/prolog_1_perjalanan`, `prolog_2_desa_gelap`, `prolog_3_pemandu`, `prolog_4_nama`, `prolog_5_ajakan`.

- [ ] **Step 1: Tulis alur controller.** Akhir Splash: `currentState = NeedsProlog ? Prolog : Lobby`. Di `Start`, cabang `pendingCapture` tetap pertama, lalu `currentState == Lobby && NeedsProlog` masuk Prolog. Musik Lobby mulai di kedua cabang. `FinishProlog` memanggil `EnerGoProgress.FinishProlog`, dan bila gagal simpan tetap di Prolog dengan `EnerGoProgress.Error` tampil.

- [ ] **Step 2: Tulis view.** Panel 1 sampai 5, gerak pelan `style.scale` 1,00 ke 1,06 selama panel tampil, teks di kartu. LANJUT maju satu panel. LEWATI lompat ke panel 4, dan pada panel 4 tombol LEWATI disembunyikan. Panel 4 menampilkan `input-nama` (`maxLength = 16`) dan menyimpan nama saat pindah ke panel 5, supaya baris `{nama}` memakai `DesaMath.CleanName`. LANJUT di panel 5 memanggil `controller.FinishProlog`.

- [ ] **Step 3: Uji Play Mode.** Cadangkan save. Hapus field baru dari JSON save, jalankan LobbyScene, dan ambil gambar panel 1, 4, 5. Expected: panel muncul dengan warna cadangan, nama kosong menjadi "Desa Teknik" di panel 5, dan setelah selesai `prologSeen` tersimpan sehingga pembukaan kedua langsung ke Lobby. Ulangi dengan `pendingCapture` di save: game masuk ke tangkapan lebih dulu, dan prolog muncul setelah kembali ke Lobby. Pulihkan save.

- [ ] **Step 4: Commit** dengan `feat: first-launch prologue that asks for the village name`.

### Task 6: Layar Desa dan pengisian

**Files:**
- Create: `Assets/EnerGo/Scripts/UI/LobbyView.Desa.cs`
- Modify: `Assets/EnerGo/Scripts/LobbyController.cs`

**Interfaces:**
- Consumes: `EnerGoProgress.FillPlant/RenameVillage`, `DesaMath.*`, `DesaText.*`, `GeoSpawner.IsDaytime(DateTime.Now.Hour)`, `LobbyView.FillTile`.
- Produces:
  - `LobbyController.FillPlant(string id, bool all)`: mengembalikan unit terpakai, memakai `InputReady`, `Click()`, dan `BlockTaps` seperti tombol lain, lalu `Changed()`.
  - `event System.Action FirstFill` di `LobbyController`, dipanggil saat pengisian mengubah `plantEverFilled` dari false ke true.

- [ ] **Step 1: Tulis `RefreshDesa`.** Dipanggil saat state berubah dan setiap 1 detik dari `UpdateDesa`, supaya stok yang habis mengganti gambar dan cap tanpa membuka ulang. Mengisi nama, `FormatSisa`, lebar bar `remaining / CapacitySec`, `desa-padam` saat Padam, `desa-menipis` dengan kata MENIPIS saat Menipis, latar dari `BackgroundFor`, pose `desa-sya` dari `PoseFor`, dan dialog dari `Status` serta `WajahFor`.

- [ ] **Step 2: Tulis tata letak pembangkit.** Konstanta `PlantU`, `PlantV` di `LobbyView.Desa.cs` (awal 0,62 dan 0,48, disetel ulang saat gambar akhir datang). Posisi `btn-pembangkit` dan `pembangkit-mati` dihitung `DesaMath.CoverPoint` dari ukuran gambar dan `resolvedStyle`, dan dihitung ulang pada `GeometryChangedEvent`. `btn-isi` di kartu stok melakukan hal yang sama, supaya pembangkit tetap bisa diisi bila area gambar meleset.

- [ ] **Step 3: Tulis sheet pengisian.** `isi-daftar` berisi lima baris dengan ikon `eg-ikon--<id>`, nama, jumlah unit, `FormatSisa(SecondsPerUnit)`, `SourceOf`, tombol `ISI` dan `ISI SEMUA`. Baris nonaktif bila `UnitsThatFit` 0, dengan teks `Stok penuh` bila karena kapasitas, dan `Belum punya` bila unit 0. Di bawah kartu ada `FormatSisa` terbaru.

- [ ] **Step 4: Tulis gerak kecil.** `desa-awan` bergeser pelan dan berulang di langit, `desa-kerbau` naik turun 2 unit, dan saat malam menyala `desa-malam-terang` di atas `desa_malam_padam` dengan opacity berdenyut 0,92-1. Elemen tanpa gambar disembunyikan.

- [ ] **Step 5: Ganti nama.** `btn-ganti-nama` membuka kartu kecil dengan `TextField` yang sama aturannya, lalu `RenameVillage`.

- [ ] **Step 6: Uji Play Mode.** Cadangkan save, isi unit lewat `script-execute`, lalu ambil gambar: padam siang, padam malam, menipis, menyala, sheet dengan stok hampir penuh, sheet tanpa unit. Set `plantEndsUtc` 3 detik ke depan dengan layar terbuka. Expected: berubah ke PADAM sendiri. Beri nama `<color=red>x` dan pastikan tampil apa adanya. Pulihkan save.

- [ ] **Step 7: Jalankan semua pengujian.** Expected: semua lulus.

- [ ] **Step 8: Commit** dengan `feat: village screen with a plant the player fills with SDA`.

### Task 7: Adegan pengisian pertama

**Files:**
- Modify: `Assets/EnerGo/Scripts/UI/LobbyView.Desa.cs`
- Modify: `Assets/UI/Screens/Desa.uxml`

**Interfaces:**
- Consumes: `LobbyController.FirstFill`, `Haptics.Pulse`, `Resources Desa/desa_malam_padam`, `Desa/desa_malam_terang`, `Pemandu/pemandu_bersorak`.

- [ ] **Step 1: Tulis adegan.** Saat `FirstFill`, sheet tertutup dan `lapis-sorak` menutup layar, jam berapa pun: `desa_malam_padam` selama 0,8 detik, memudar ke `desa_malam_terang` selama 1,5 detik sambil `Haptics.Pulse(60)` di puncak, lalu Sya bersorak dengan kartu "{nama} menyala!" dan tombol `LANJUT`. Selesai, lapisan disembunyikan dan latar kembali mengikuti jam. `plantEverFilled` tersimpan bersama unit, jadi aplikasi yang ditutup di tengah adegan tidak mengulangnya dan unit tidak hilang.

- [ ] **Step 2: Uji Play Mode.** Cadangkan save. Pengisian pertama saat jam siang memutar adegan malam, pengisian kedua tidak. Pulihkan save.

- [ ] **Step 3: Commit** dengan `feat: first plant fill lights the village at any hour`.

### Task 8: Dokumen, lalu serah terima

**Files:**
- Modify: `docs/PRD-EnerGo.md` (2 tabel mode, 3.13, 4.3, 7), `tuning-and-balancing-ringkas.md` (D, E), `tuning-and-balancing.md`, `docs/prompt-aset.md` (desa 3D dibatalkan), `docs/tutorial.md` baris Desa Teknik

- [ ] **Step 1: Perbarui dokumen.** Emas memperbaiki kabel dan sambungan, Mode Latihan 1 unit per ronde, target balancing hanya untuk radar, level ditunda, model 3D desa dibatalkan, tampilan mengikuti jam, nama Sya.

- [ ] **Step 2: Cari sisa kata yang salah.** `Grep` `Emas.*bahan bakar` di `docs`, `tuning-*`, `Assets/EnerGo/Scripts`. Expected: hanya `gold-02` dan penjelasan bank soal yang memang menyatakan emas bukan bahan bakar.

- [ ] **Step 3: Tunjukkan naskah prolog dan dialog ke pengguna,** terapkan koreksinya.

- [ ] **Step 4: Jalankan semua pengujian EditMode.** Expected: semua lulus.

- [ ] **Step 5: Commit** dengan `docs: update the PRD and tuning notes for the village`.

### Task 9: Pasang gambar desa dan prolog (setelah pengguna mengirim gambarnya)

**Files:**
- Create: `Art/desa/*`, `Art/prolog/*`, `Assets/EnerGo/Resources/Desa/*.png`, `Assets/EnerGo/Resources/Prolog/*.png`, plus `pemandu_mengisi` di `Art/pemandu/sumber`

- [ ] **Step 1: Cek keselarasan.** Tumpuk `desa_siang`, `desa_malam_terang`, dan `desa_malam_padam` dan lihat bedanya. Kalau bergeser, turunkan kedua gambar malam dari `desa_siang`: warna gelap kebiruan untuk padam, ditambah cahaya jendela dan lampu untuk menyala.

- [ ] **Step 2: Proses dan pasang.** Latar `maxTextureSize = 2048`. `desa_kerbau` lewat `potong.py`. `desa_awan` memakai kunci warna `#5FA8E0`.

- [ ] **Step 3: Setel `PlantU`, `PlantV`** agar tepat di gedung pembangkit, dan cek dengan gambar layar pada 480 x 800 dan 480 x 1067.

- [ ] **Step 4: Commit** dengan `feat: install village and prologue art`.
