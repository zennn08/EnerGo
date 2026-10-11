# Oil & Uranium Mini-games Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make Minyak Bumi ("Bor & Pompa") and Uranium ("Pemburu Geiger") capturable, with their own AR mini-games, quiz banks and save support.

**Architecture:**
- The pure rules live in two static classes in the `EnerGo.Core` assembly, so EditMode tests can reach them: `UraniumMath` and `OilMath`.
- The Unity side lives in two new `partial` files of the existing `ARCapturePrototype` class. These handle sensors, models, IMGUI overlays and the phase flow.
- The main file only gains a handful of one-line dispatch hooks, following its existing `if (resourceId == …)` pattern.
- The save format is migrated from 3 to 5 inventory entries before anything is unlocked.

**Tech Stack:** Unity 6 (URP), C#, new Input System (`Accelerometer`, `EnhancedTouch`), IMGUI via `EnerGoUI`, NUnit EditMode tests, Unity MCP CLI (`npx -y unity-mcp-cli`).

**Spec:** `docs/superpowers/specs/2026-10-11-minyak-uranium-design.md`

## Global Constraints

- **UI text:** player-facing text is Indonesian, all caps for HUD lines, matching the existing HUD (e.g. `"EMAS TERBUANG!"`).
- **Glyphs:** never use characters Poppins lacks, such as emoji, `☢`, `←`, `→`, `✓` and `✗`. Use the word `RAD` instead of `☢`. `·`, `‹`, `›`, `&` and `!` are fine, because the existing HUD uses them.
- **Quiz ids:** ids are stored in saves. Only append new ids. Never renumber or reuse one.
- **Colours:** use the Solar Clean palette tokens in `EnerGoUI` (`Ink`, `Orange`, `Paper`, `Line`, `Success`, `Danger`). The uranium glow is `#39FF14`, and oil is "hitam kecokelatan".
- **Failure flow:** on failure, show the message for about 1.5 s, then build a fresh deposit and restart from phase 1. There is no penalty.
- **Tuning:** every tuning constant sits at the top of `UraniumMath` / `OilMath`.
- **Scope:** do not refactor the sun, coal or gold mini-games.
- **Tooling files:** do not commit the unrelated working-tree changes: `Assets/Plugins/NuGet/*`, `Packages/manifest.json`, `Packages/packages-lock.json`, `ProjectSettings/QualitySettings.asset`, `.claude/` and `.mcp.json`. Always `git add` explicit paths.
- **Commits:** end every commit message with `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`.

## Review Focus

1. **Old save after the update.** A save written by the current APK (3 inventory entries) must load with all its counts intact. It must gain oil and uranium at 0, and it must save again without errors. Check: Task 1 verification script.
2. **Reset / mode button mid-game.** The bottom button ("RESET POSISI KE HADAPAN") and the ARCORE/SENSOR toggle destroy `target` in the middle of a mini-game. The next spawn must start cleanly from phase 1, with no stale gauges and no NullReference. Check: the spawn functions reset every phase field (Tasks 6 and 7), and Task 9 Step 2.
3. **Uranium direction must not leak.** During the Geiger search, the HUD must not show the usual "PUTAR KE KIRI/KANAN" arrow, which would give the hidden ore away. Check: the `DrawRadarHUD` early return in Task 6, and Task 9 Step 2.
4. **Practice preference with oil/uranium.** If the saved practice pick is `oil` or `uranium`, the lobby must keep it after a restart, not fall back to Emas. "Acak" can pick all 5. Check: Task 8.
5. **Overshooting the oil band.** Releasing too late slides the bit through the thin oil layer into brine ("KENA AIR!"). Drilling fast through gas causes a blowout. Both are pinned by OilMath tests in Task 4: `CoastingFromGasSpeedOvershootsTheOilBand` and `FastDrillingInGasBuildsPressure`.

## How to run things (used by every task)

**Compile**
1. `npx -y unity-mcp-cli run-tool assets-refresh --input "{}"`
2. `npx -y unity-mcp-cli run-tool console-get-logs --input "{\"logTypeFilter\":\"Error\",\"maxEntries\":20}"`
3. Expected: no `error CS` newer than the refresh. Console checks can miss errors, so the `tests-run` output is the reliable compile check.

**EditMode tests**
- Command: `npx -y unity-mcp-cli run-tool tests-run --input "{\"testMode\":\"EditMode\",\"testAssembly\":\"EnerGo.Tests.Editor\"}"`
- Never run this while the editor is in play mode. It leaves a stale `MCP_PendingTestRun` lock.
- Baseline: 34 tests pass on main.

**Run C# in the editor**
1. Save a file with `public class Script { public static object Main() { ... } }` to the scratchpad.
2. Run: `python -c "import json,sys;print(json.dumps({'csharpCode':open(sys.argv[1],encoding='utf-8').read(),'isMethodBody':False}))" <file> | npx -y unity-mcp-cli run-tool script-execute --input-file -`
3. The returned value is printed.

**Editor play mode under MCP does not tick:** `Time.time` stays at 0. Drive state by reflection rather than waiting. A human playing in the focused editor ticks normally.

**Editor controls for these mini-games** (no sensors in the editor):
- Look around: right-drag, or A/D/W/S.
- Uranium rod: W/S during the secure phase.
- Oil drill and pump: hold the left mouse button or Space.

---

### Task 0: Branch

- [ ] **Step 1: Create the branch from main**

```bash
git checkout main
git checkout -b minyak-uranium
```

Expected: `Switched to a new branch 'minyak-uranium'`. The uncommitted tooling changes ride along untouched.

---

### Task 1: Save migration (3 → 5 inventory entries)

**Files:**
- Modify: `Assets/EnerGo/Scripts/EnerGoProgress.cs`. This covers the `SaveGame` class (around lines 40–60), `TryRead` (around line 105) and `Valid` (around lines 121–130).

**Interfaces:**
- Produces:
  - `SaveGame.RequiredResources` and `SaveGame.AddedResources` (`string[]`)
  - `SaveGame.AddMissingResources()`
  - every loaded `SaveGame` has entries for all 5 ids, so `Get(ResourceIds.Oil)` / `Get(ResourceIds.Uranium)` never throw

- [ ] **Step 1: Write the verification script.** Save it as `<scratchpad>/verify_save_migration.cs`.

```csharp
using System.IO;
using UnityEngine;
using EnerGo;

public class Script
{
    public static object Main()
    {
        string p = EnerGoProgress.SavePath, b = p + ".bak";
        string keepP = File.Exists(p) ? File.ReadAllText(p) : null;
        string keepB = File.Exists(b) ? File.ReadAllText(b) : null;
        try
        {
            // A save exactly like the current APK writes: only sun, coal, gold.
            var old = new SaveGame { legacyGoldImported = true };
            old.inventory = new[]
            {
                new InventoryEntry { resourceId = ResourceIds.Sun, normalQuantity = 2 },
                new InventoryEntry { resourceId = ResourceIds.Coal },
                new InventoryEntry { resourceId = ResourceIds.Gold, normalQuantity = 7, pureQuantity = 1 }
            };
            Directory.CreateDirectory(Path.GetDirectoryName(p));
            File.WriteAllText(p, JsonUtility.ToJson(old));
            if (File.Exists(b)) File.Delete(b);

            EnerGoProgress.Load();
            var s = EnerGoProgress.Current;
            string r = $"error={EnerGoProgress.Error ?? "none"} len={s.inventory.Length} sun={s.Get(ResourceIds.Sun).Total} " +
                       $"gold={s.Get(ResourceIds.Gold).Total} oil={s.Get(ResourceIds.Oil).Total} uranium={s.Get(ResourceIds.Uranium).Total}";
            return r + " commit=" + EnerGoProgress.Commit(s);
        }
        finally
        {
            if (keepP != null) File.WriteAllText(p, keepP); else if (File.Exists(p)) File.Delete(p);
            if (keepB != null) File.WriteAllText(b, keepB); else if (File.Exists(b)) File.Delete(b);
            EnerGoProgress.Load();
        }
    }
}
```

- [ ] **Step 2: Run it to see it fail.** Use the "Run C# in the editor" recipe. Expected: an `ArgumentException: Unknown resource: oil` from `Get(ResourceIds.Oil)`.

- [ ] **Step 3: Implement the migration.** In `SaveGame`, replace the `inventory` initialiser and add the two arrays and the method:

```csharp
        public static readonly string[] RequiredResources = { ResourceIds.Sun, ResourceIds.Coal, ResourceIds.Gold };
        public static readonly string[] AddedResources = { ResourceIds.Oil, ResourceIds.Uranium }; // missing in saves from before oil/uranium

        public InventoryEntry[] inventory = {
            new InventoryEntry { resourceId = ResourceIds.Sun },
            new InventoryEntry { resourceId = ResourceIds.Coal },
            new InventoryEntry { resourceId = ResourceIds.Gold },
            new InventoryEntry { resourceId = ResourceIds.Oil },
            new InventoryEntry { resourceId = ResourceIds.Uranium }
        };
```

Add this method next to `Get`:

```csharp
        // Saves from before oil/uranium list only 3 resources: add the missing ones with 0 units.
        public void AddMissingResources()
        {
            foreach (var id in AddedResources)
                if (!inventory.Any(e => e != null && e.resourceId == id))
                    inventory = inventory.Concat(new[] { new InventoryEntry { resourceId = id } }).ToArray();
        }
```

In `TryRead`, change the two lines after the `Valid` check:

```csharp
                if (!Valid(value)) return false;
                value.AddMissingResources();
                data = value;
                return true;
```

In `Valid`, replace the first `if` and the `foreach (string id in new[] { … })` block (up to and including its closing brace) with:

```csharp
            if (value == null || value.schemaVersion != 1 || value.rulesetId != "demo-v1" || value.inventory == null || !value.legacyGoldImported) return false;
            int Entries(string id) => value.inventory.Count(e => e != null && e.resourceId == id && e.normalQuantity >= 0 && e.pureQuantity >= 0);
            int known = 0;
            foreach (string id in SaveGame.RequiredResources) { int n = Entries(id); if (n != 1) return false; known += n; }
            foreach (string id in SaveGame.AddedResources) { int n = Entries(id); if (n > 1) return false; known += n; }
            if (known != value.inventory.Length) return false; // no null, unknown or duplicate entries
```

- [ ] **Step 4: Compile, then rerun the script.** Expected: `error=none len=5 sun=2 gold=8 oil=0 uranium=0 commit=True`.

- [ ] **Step 5: Run the EditMode tests.** Expected: 34/34 pass.

- [ ] **Step 6: Commit**

```bash
git add Assets/EnerGo/Scripts/EnerGoProgress.cs
git commit -m "feat: save inventory gains oil and uranium; old 3-resource saves migrate

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 2: Quiz banks for oil and uranium

**Files:**
- Modify: `Assets/EnerGo/Scripts/QuizBank.cs`. Add source constants after the `Mercury` constant (around line 50), and add questions after `gold-35` (around line 166).

**Interfaces:**
- Produces:
  - `QuizBank.ForResource("oil")` and `QuizBank.ForResource("uranium")` each return 30 questions
  - ids `oil-01..oil-30` and `uranium-01..uranium-30`
  - `EnerGoProgress.BeginAnalysis("oil" | "uranium", …)` can now succeed

- [ ] **Step 1: Write the verification script.** Save it as `<scratchpad>/verify_quiz.cs`.

```csharp
using System.Linq;
using EnerGo;

public class Script
{
    public static object Main()
    {
        var all = QuizBank.Questions;
        int dup = all.Length - all.Select(q => q.id).Distinct().Count();
        string Line(string id) { var qs = QuizBank.ForResource(id); return $"{id}={qs.Length} false={qs.Count(q => !q.correctAnswer)}"; }
        bool emptyText = all.Any(q => string.IsNullOrEmpty(q.statement) || string.IsNullOrEmpty(q.explanation) || string.IsNullOrEmpty(q.sourceUrl));
        return $"{Line(ResourceIds.Oil)} {Line(ResourceIds.Uranium)} dup={dup} emptyText={emptyText}";
    }
}
```

- [ ] **Step 2: Run it.** Expected before the change: `oil=0 false=0 uranium=0 false=0 dup=0 emptyText=False`.

- [ ] **Step 3: Add the source constants** after the line `private const string Mercury = …;`

```csharp

        private const string OilBasics = "https://www.eia.gov/energyexplained/oil-and-petroleum-products/";
        private const string OilWhere = "https://www.eia.gov/energyexplained/oil-and-petroleum-products/where-our-oil-comes-from.php";
        private const string OilRefining = "https://www.eia.gov/energyexplained/oil-and-petroleum-products/refining-crude-oil.php";
        private const string OilUse = "https://www.eia.gov/energyexplained/oil-and-petroleum-products/use-of-oil.php";
        private const string OilEnvironment = "https://www.eia.gov/energyexplained/oil-and-petroleum-products/oil-and-the-environment.php";
        private const string IndonesiaEnergy = "https://www.eia.gov/international/analysis/country/IDN";

        private const string Nuclear = "https://www.eia.gov/energyexplained/nuclear/";
        private const string NuclearPlants = "https://www.eia.gov/energyexplained/nuclear/nuclear-power-plants.php";
        private const string NuclearFuelCycle = "https://www.eia.gov/energyexplained/nuclear/the-nuclear-fuel-cycle.php";
        private const string NuclearEnvironment = "https://www.eia.gov/energyexplained/nuclear/nuclear-power-and-the-environment.php";
        private const string WhatIsUranium = "https://world-nuclear.org/information-library/nuclear-fuel-cycle/introduction/what-is-uranium-how-does-it-work";
        private const string Reactors = "https://world-nuclear.org/information-library/nuclear-fuel-cycle/nuclear-power-reactors/nuclear-power-reactors";
        private const string RadiationLife = "https://world-nuclear.org/information-library/safety-and-security/radiation-and-health/radiation-and-life";
        private const string UraniumMining = "https://world-nuclear.org/information-library/nuclear-fuel-cycle/mining-of-uranium/world-uranium-mining-production";
        private const string NuclearIndonesia = "https://world-nuclear.org/information-library/country-profiles/countries-g-n/indonesia";
```

- [ ] **Step 4: Add the questions.** Change `gold-35`'s line to end with `,` (it is the last item and currently has no trailing comma). Then insert the following before the closing `};` of `Questions`:

```csharp

            // ── Minyak Bumi ───────────────────────────────────────────
            Q("oil-01", ResourceIds.Oil, "Minyak bumi terbentuk dari sisa organisme laut kecil seperti plankton yang tertimbun jutaan tahun.", true, "Sisa tumbuhan dan hewan laut kecil tertimbun lumpur, lalu panas dan tekanan mengubahnya menjadi minyak.", OilBasics),
            Q("oil-02", ResourceIds.Oil, "Minyak bumi termasuk sumber energi terbarukan.", false, "Minyak bumi adalah bahan bakar fosil yang butuh jutaan tahun untuk terbentuk, jadi tidak terbarukan.", OilBasics),
            Q("oil-03", ResourceIds.Oil, "Minyak mentah diolah di kilang menjadi bensin, solar, dan avtur.", true, "Kilang memisahkan dan mengolah minyak mentah menjadi berbagai bahan bakar.", OilRefining),
            Q("oil-04", ResourceIds.Oil, "Minyak bumi sering terperangkap di bawah lapisan batuan kedap yang disebut batuan penutup.", true, "Batuan penutup yang kedap menahan minyak agar tidak naik ke permukaan, sehingga minyak terkumpul di reservoir.", OilWhere),
            Q("oil-05", ResourceIds.Oil, "Di dalam reservoir, gas alam biasanya berada di atas minyak, dan air di bawahnya.", true, "Gas paling ringan sehingga naik ke atas, minyak di tengah, dan air yang paling berat di bawah.", OilWhere),
            Q("oil-06", ResourceIds.Oil, "Minyak lebih berat daripada air sehingga langsung tenggelam saat tumpah di laut.", false, "Sebagian besar minyak lebih ringan daripada air, sehingga tumpahan minyak mengapung dan menyebar di permukaan laut.", OilEnvironment),
            Q("oil-07", ResourceIds.Oil, "Pompa angguk dipakai untuk mengangkat minyak ketika tekanan alami sumur sudah melemah.", true, "Saat tekanan reservoir turun, pompa membantu mengangkat minyak dari dalam sumur ke permukaan.", OilWhere),
            Q("oil-08", ResourceIds.Oil, "Satu barel minyak setara dengan sekitar 159 liter.", true, "Satu barel berisi 42 galon AS, atau sekitar 159 liter.", OilBasics),
            Q("oil-09", ResourceIds.Oil, "Pembakaran bensin di kendaraan tidak menghasilkan karbon dioksida.", false, "Membakar bensin dan solar menghasilkan karbon dioksida, gas rumah kaca utama.", OilEnvironment),
            Q("oil-10", ResourceIds.Oil, "Sektor transportasi adalah pemakai produk minyak bumi terbesar.", true, "Sebagian besar minyak dipakai sebagai bensin, solar, dan avtur untuk kendaraan, kapal, dan pesawat.", OilUse),
            Q("oil-11", ResourceIds.Oil, "Plastik dapat dibuat dari bahan hasil olahan minyak bumi.", true, "Produk petrokimia dari minyak bumi menjadi bahan baku plastik dan banyak barang lain.", OilUse),
            Q("oil-12", ResourceIds.Oil, "Kilang memisahkan minyak mentah dengan cara membekukannya.", false, "Kilang memanaskan minyak mentah lalu memisahkannya lewat distilasi, bukan membekukannya.", OilRefining),
            Q("oil-13", ResourceIds.Oil, "Distilasi di kilang memisahkan minyak mentah berdasarkan perbedaan titik didih.", true, "Tiap bagian minyak menguap pada suhu berbeda, sehingga bisa dipisahkan di menara distilasi.", OilRefining),
            Q("oil-14", ResourceIds.Oil, "Tumpahan minyak di laut dapat membahayakan burung dan ikan.", true, "Minyak melapisi bulu burung dan meracuni makhluk laut, sehingga tumpahan sangat merusak.", OilEnvironment),
            Q("oil-15", ResourceIds.Oil, "Pengeboran minyak juga dilakukan di lepas pantai memakai anjungan.", true, "Banyak cadangan minyak berada di bawah dasar laut dan dibor dari anjungan lepas pantai.", OilWhere),
            Q("oil-16", ResourceIds.Oil, "Minyak bumi selalu ditemukan dekat permukaan tanah, tidak pernah di kedalaman ribuan meter.", false, "Banyak sumur minyak dibor hingga ribuan meter di bawah permukaan.", OilWhere),
            Q("oil-17", ResourceIds.Oil, "PLTD memakai bahan bakar solar, yang merupakan produk minyak bumi.", true, "Pembangkit Listrik Tenaga Diesel membakar solar hasil olahan minyak bumi.", OilUse),
            Q("oil-18", ResourceIds.Oil, "Riau termasuk daerah penghasil minyak bumi utama di Indonesia.", true, "Lapangan-lapangan minyak besar seperti di Blok Rokan berada di Riau.", IndonesiaEnergy),
            Q("oil-19", ResourceIds.Oil, "Indonesia saat ini mengekspor jauh lebih banyak minyak daripada yang diimpornya.", false, "Sejak awal 2000-an konsumsi Indonesia melebihi produksinya, sehingga Indonesia menjadi pengimpor bersih minyak.", IndonesiaEnergy),
            Q("oil-20", ResourceIds.Oil, "Pertamina adalah perusahaan energi milik negara Indonesia.", true, "Pertamina adalah BUMN yang mengelola minyak dan gas bumi Indonesia.", IndonesiaEnergy),
            Q("oil-21", ResourceIds.Oil, "Minyak mentah umumnya berwarna hitam atau cokelat gelap.", true, "Minyak mentah biasanya cairan kental berwarna gelap, walau warnanya bisa berbeda tiap lapangan.", OilBasics),
            Q("oil-22", ResourceIds.Oil, "Minyak bumi terbentuk hanya dalam waktu beberapa tahun.", false, "Pembentukan minyak bumi butuh jutaan tahun di bawah panas dan tekanan.", OilBasics),
            Q("oil-23", ResourceIds.Oil, "Aspal untuk jalan berasal dari bagian berat hasil pengolahan minyak bumi.", true, "Bagian paling berat dari minyak mentah di kilang dipakai sebagai aspal.", OilRefining),
            Q("oil-24", ResourceIds.Oil, "Mengebor kantong gas bertekanan tinggi tidak memiliki risiko apa pun.", false, "Gas bertekanan tinggi bisa menyembur liar (blowout) jika pengeboran tidak dikendalikan.", OilEnvironment),
            Q("oil-25", ResourceIds.Oil, "Sumur minyak modern dilengkapi alat pencegah semburan liar (blowout preventer).", true, "Blowout preventer menutup sumur saat tekanan melonjak untuk mencegah semburan liar.", OilEnvironment),
            Q("oil-26", ResourceIds.Oil, "Avtur adalah bahan bakar khusus untuk kapal laut.", false, "Avtur adalah bahan bakar pesawat jet, salah satu produk kilang minyak.", OilUse),
            Q("oil-27", ResourceIds.Oil, "Gas buang kendaraan berbahan bakar minyak dapat menyebabkan polusi udara di kota.", true, "Pembakaran bensin dan solar melepaskan polutan yang menurunkan kualitas udara.", OilEnvironment),
            Q("oil-28", ResourceIds.Oil, "Minyak yang sudah disedot dari reservoir akan terisi kembali dengan cepat.", false, "Reservoir terisi selama jutaan tahun. Minyak yang sudah diambil tidak cepat tergantikan.", OilBasics),
            Q("oil-29", ResourceIds.Oil, "Selain bahan bakar, minyak bumi juga dipakai untuk membuat pelumas.", true, "Oli dan pelumas mesin dibuat dari hasil olahan minyak bumi.", OilUse),
            Q("oil-30", ResourceIds.Oil, "Minyak bumi dan minyak goreng berasal dari sumber yang sama.", false, "Minyak goreng dibuat dari tumbuhan seperti kelapa sawit, sedangkan minyak bumi berasal dari dalam perut bumi.", OilBasics),

            // ── Uranium ───────────────────────────────────────────────
            Q("uranium-01", ResourceIds.Uranium, "Uranium adalah logam radioaktif yang ditemukan di batuan kerak Bumi.", true, "Uranium terdapat dalam batuan, tanah, bahkan air laut dalam jumlah kecil.", WhatIsUranium),
            Q("uranium-02", ResourceIds.Uranium, "Uranium termasuk sumber energi terbarukan.", false, "Uranium adalah bahan tambang yang jumlahnya terbatas, jadi tidak terbarukan.", Nuclear),
            Q("uranium-03", ResourceIds.Uranium, "PLTN memakai panas dari fisi uranium untuk membuat uap penggerak turbin.", true, "Panas dari reaksi fisi mendidihkan air menjadi uap, lalu uap memutar turbin pembangkit.", NuclearPlants),
            Q("uranium-04", ResourceIds.Uranium, "Fisi adalah proses pembelahan inti atom yang melepaskan energi.", true, "Saat inti uranium terbelah, sejumlah besar energi dilepaskan sebagai panas.", Nuclear),
            Q("uranium-05", ResourceIds.Uranium, "Matahari menghasilkan energi dengan reaksi fisi uranium.", false, "Matahari memakai fusi, yaitu penggabungan inti hidrogen, bukan fisi uranium.", Nuclear),
            Q("uranium-06", ResourceIds.Uranium, "Batang kendali di reaktor menyerap neutron untuk mengatur laju reaksi berantai.", true, "Memasukkan batang kendali lebih dalam menyerap lebih banyak neutron, sehingga reaksi melambat.", Reactors),
            Q("uranium-07", ResourceIds.Uranium, "Uranium-235 lebih mudah mengalami fisi daripada uranium-238.", true, "Uranium-235 adalah isotop yang mudah dibelah dan menjadi bahan bakar utama reaktor.", WhatIsUranium),
            Q("uranium-08", ResourceIds.Uranium, "Sebagian besar uranium alam berupa uranium-235.", false, "Uranium alam hampir seluruhnya uranium-238. Uranium-235 hanya sekitar 0,7 persen.", WhatIsUranium),
            Q("uranium-09", ResourceIds.Uranium, "Pengayaan uranium menaikkan kadar uranium-235 dalam bahan bakar.", true, "Pengayaan menaikkan kadar uranium-235 agar reaksi berantai di reaktor dapat berlangsung.", NuclearFuelCycle),
            Q("uranium-10", ResourceIds.Uranium, "Pencacah Geiger dipakai untuk mendeteksi radiasi.", true, "Pencacah Geiger berbunyi klik setiap kali menangkap partikel radiasi.", RadiationLife),
            Q("uranium-11", ResourceIds.Uranium, "Radiasi nuklir dapat dilihat dengan mata telanjang sebagai cahaya hijau.", false, "Radiasi pengion tidak terlihat oleh mata, sehingga perlu alat seperti pencacah Geiger.", RadiationLife),
            Q("uranium-12", ResourceIds.Uranium, "Timbal dan beton tebal dapat menahan radiasi.", true, "Bahan padat seperti timbal dan beton tebal dipakai sebagai pelindung radiasi.", RadiationLife),
            Q("uranium-13", ResourceIds.Uranium, "PLTN tidak mengeluarkan karbon dioksida saat membangkitkan listrik.", true, "Reaktor nuklir tidak membakar bahan bakar, sehingga tidak melepaskan CO2 saat beroperasi.", NuclearEnvironment),
            Q("uranium-14", ResourceIds.Uranium, "Bahan bakar bekas dari reaktor tetap radioaktif dan harus disimpan dengan aman.", true, "Bahan bakar bekas sangat radioaktif dan disimpan di kolam atau wadah khusus.", NuclearEnvironment),
            Q("uranium-15", ResourceIds.Uranium, "Limbah nuklir boleh dibuang ke tempat sampah biasa.", false, "Limbah radioaktif diatur ketat dan disimpan di fasilitas khusus agar tidak mencemari lingkungan.", NuclearEnvironment),
            Q("uranium-16", ResourceIds.Uranium, "Bahan bakar reaktor dibentuk menjadi pelet kecil yang disusun dalam batang bahan bakar.", true, "Pelet uranium dimasukkan ke dalam tabung logam yang disebut batang bahan bakar.", NuclearFuelCycle),
            Q("uranium-17", ResourceIds.Uranium, "Uranium menyimpan energi jauh lebih padat daripada batu bara.", true, "Sedikit uranium menghasilkan energi yang setara dengan batu bara dalam jumlah sangat besar.", WhatIsUranium),
            Q("uranium-18", ResourceIds.Uranium, "Indonesia sudah memiliki banyak PLTN komersial yang beroperasi.", false, "Indonesia belum memiliki PLTN komersial. Saat ini hanya ada reaktor riset.", NuclearIndonesia),
            Q("uranium-19", ResourceIds.Uranium, "Indonesia memiliki reaktor riset, salah satunya di Serpong.", true, "Reaktor riset di Serpong, Bandung, dan Yogyakarta dipakai untuk penelitian dan produksi radioisotop.", NuclearIndonesia),
            Q("uranium-20", ResourceIds.Uranium, "Kalimantan Barat memiliki endapan uranium yang sudah diteliti, misalnya di daerah Kalan.", true, "Eksplorasi di Kalan, Kalimantan Barat, menemukan endapan uranium.", NuclearIndonesia),
            Q("uranium-21", ResourceIds.Uranium, "Uranium yang baru ditambang bisa langsung dimasukkan ke reaktor tanpa diolah.", false, "Bijih uranium harus diolah, dikonversi, diperkaya, lalu dibentuk menjadi bahan bakar.", NuclearFuelCycle),
            Q("uranium-22", ResourceIds.Uranium, "Yellowcake adalah konsentrat uranium hasil pengolahan bijih.", true, "Bijih uranium digerus dan diolah menjadi bubuk kuning yang disebut yellowcake.", NuclearFuelCycle),
            Q("uranium-23", ResourceIds.Uranium, "Semakin lama berada dekat sumber radiasi, semakin besar dosis yang diterima tubuh.", true, "Dosis radiasi menumpuk seiring waktu, jadi waktu paparan harus dibatasi.", RadiationLife),
            Q("uranium-24", ResourceIds.Uranium, "Menjauh dari sumber radiasi mengurangi dosis yang diterima.", true, "Waktu, jarak, dan pelindung adalah tiga cara utama mengurangi dosis radiasi.", RadiationLife),
            Q("uranium-25", ResourceIds.Uranium, "Kita sama sekali tidak pernah terkena radiasi dalam kehidupan sehari-hari.", false, "Ada radiasi latar alami dari batuan, tanah, dan luar angkasa yang kita terima setiap hari.", RadiationLife),
            Q("uranium-26", ResourceIds.Uranium, "Reaksi berantai terjadi ketika neutron dari satu fisi memicu fisi inti atom lainnya.", true, "Setiap fisi melepaskan neutron baru yang dapat membelah inti uranium berikutnya.", Reactors),
            Q("uranium-27", ResourceIds.Uranium, "Meltdown berarti reaktor sedang bekerja paling efisien.", false, "Meltdown adalah kecelakaan saat inti reaktor terlalu panas hingga bahan bakarnya meleleh.", Reactors),
            Q("uranium-28", ResourceIds.Uranium, "Banyak reaktor nuklir memakai air sebagai pendingin.", true, "Air mengambil panas dari inti reaktor dan sekaligus dipakai untuk membuat uap.", Reactors),
            Q("uranium-29", ResourceIds.Uranium, "Uranium hanya ditemukan di luar angkasa, tidak di Bumi.", false, "Uranium ditambang dari batuan di Bumi, misalnya di Kazakhstan, Kanada, dan Australia.", WhatIsUranium),
            Q("uranium-30", ResourceIds.Uranium, "Kazakhstan termasuk produsen uranium terbesar di dunia.", true, "Kazakhstan menambang uranium lebih banyak daripada negara lain mana pun.", UraniumMining)
```

- [ ] **Step 5: Compile, then rerun the script.** Expected: `oil=30 false=11 uranium=30 false=10 dup=0 emptyText=False`.

- [ ] **Step 6: Run the EditMode tests.** Expected: 34/34 pass.

- [ ] **Step 7: Commit**

```bash
git add Assets/EnerGo/Scripts/QuizBank.cs
git commit -m "feat: 30 quiz questions each for oil and uranium

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 3: `UraniumMath` (Geiger, reactivity, dose)

**Files:**
- Create: `Assets/EnerGo/Scripts/Core/UraniumMath.cs`
- Test: `Assets/EnerGo/Tests/Editor/UraniumMathTests.cs`

**Interfaces:**
- Produces (namespace `EnerGo`, static class `UraniumMath`):
  - **Constants (all `public const float`):**
    - hiding the ore: `HideMinYawDeg`, `HideMaxYawDeg`, `HideMinPitchDeg`, `HideMaxPitchDeg`
    - finding it: `RevealAngleDeg`, `RevealHoldSec`, `MaxPulsesPerSec`
    - reactivity zones: `GreenMin`, `GreenMax`, `HotAbove`
    - neutron spikes: `SpikeSize`, `SpikeWarnSec`
    - noise: `NoiseGain`
    - outcome: `ParSec`, `FailShowSec`
  - `float GeigerRate(float angleDeg)`: clicks per second, from 1 at 150° or more up to 25 at 0°
  - `float SecureClickRate(float r)`
  - `float NextClickDelay(float rate, float u01)`
  - `int Meter(float angleDeg)`: 0..100
  - `float TiltDeg(Vector3 gravity)`
  - `float RodFromTilt(float tiltDeg, float baselineDeg)`
  - `float StepReactivity(float r, float rod, float jolt, float spike, float noise, float dt)`
  - `bool InGreen(float r)`
  - `float StepProgress(float progress, float r, float dt)`
  - `float StepDose(float dose, float r, float dt)`
  - `float NextSpikeDelay(float u01)`
  - `enum Outcome { None, Captured, Meltdown, Overdose }`
  - `Outcome Check(float r, float dose, float progress)`
  - `float Performance(float elapsed, float dose)`

- [ ] **Step 1: Write the failing tests** in `Assets/EnerGo/Tests/Editor/UraniumMathTests.cs`

```csharp
using NUnit.Framework;
using UnityEngine;

namespace EnerGo.Tests
{
    public class UraniumMathTests
    {
        [Test]
        public void GeigerClicksFasterTheCloserTheAim()
        {
            Assert.AreEqual(25f, UraniumMath.GeigerRate(0f), 0.01f);
            Assert.AreEqual(1f, UraniumMath.GeigerRate(150f), 0.01f);
            Assert.AreEqual(1f, UraniumMath.GeigerRate(180f), 0.01f);
            float prev = float.MaxValue;
            for (float a = 0f; a <= 180f; a += 10f)
            {
                float rate = UraniumMath.GeigerRate(a);
                Assert.LessOrEqual(rate, prev);
                prev = rate;
            }
        }

        [Test]
        public void ClickDelayIsExponentialAndNeverInfinite()
        {
            Assert.AreEqual(0.1f, UraniumMath.NextClickDelay(10f, Mathf.Exp(-1f)), 0.001f);
            Assert.IsFalse(float.IsInfinity(UraniumMath.NextClickDelay(10f, 0f)));
        }

        [Test]
        public void MeterShowsHundredWhenAimedStraightAtTheOre()
        {
            Assert.AreEqual(100, UraniumMath.Meter(0f));
            Assert.AreEqual(50, UraniumMath.Meter(90f));
            Assert.AreEqual(0, UraniumMath.Meter(180f));
        }

        [Test]
        public void TiltIsZeroUprightAndNinetyFlat()
        {
            Assert.AreEqual(0f, UraniumMath.TiltDeg(new Vector3(0f, -1f, 0f)), 0.1f);
            Assert.AreEqual(90f, UraniumMath.TiltDeg(new Vector3(0f, 0f, -1f)), 0.1f);
        }

        [Test]
        public void RodStartsHalfwayAndTiltingForwardPushesItIn()
        {
            Assert.AreEqual(0.5f, UraniumMath.RodFromTilt(20f, 20f), 1e-4f);
            Assert.Greater(UraniumMath.RodFromTilt(30f, 20f), 0.5f);
            Assert.AreEqual(1f, UraniumMath.RodFromTilt(200f, 20f), 1e-4f);
            Assert.AreEqual(0f, UraniumMath.RodFromTilt(-200f, 20f), 1e-4f);
        }

        [Test]
        public void HalfRodWithoutDisturbanceSettlesInTheGreenZone()
        {
            float r = 0.5f;
            for (int i = 0; i < 100; i++) r = UraniumMath.StepReactivity(r, 0.5f, 0f, 0f, 0f, 0.1f);
            Assert.IsTrue(UraniumMath.InGreen(r), "r = " + r);
        }

        [Test]
        public void FullRodCoolsAndPulledRodHeatsUp()
        {
            float cool = 0.8f, hot = 0.5f;
            for (int i = 0; i < 20; i++)
            {
                cool = UraniumMath.StepReactivity(cool, 1f, 0f, 0f, 0f, 0.1f);
                hot = UraniumMath.StepReactivity(hot, 0f, 0f, 0f, 0f, 0.1f);
            }
            Assert.Less(cool, 0.5f);
            Assert.Greater(hot, 0.7f);
        }

        [Test]
        public void SpikeAndShakingRaiseReactivity()
        {
            Assert.AreEqual(0.5f + UraniumMath.SpikeSize, UraniumMath.StepReactivity(0.5f, 0.5f, 0f, UraniumMath.SpikeSize, 0f, 0f), 1e-4f);
            float calm = UraniumMath.StepReactivity(0.5f, 0.5f, 0f, 0f, 0f, 0.1f);
            float shaken = UraniumMath.StepReactivity(0.5f, 0.5f, 0.6f, 0f, 0f, 0.1f);
            Assert.Greater(shaken, calm);
            Assert.AreEqual(1f, UraniumMath.StepReactivity(0.95f, 0f, 0f, 0.5f, 0f, 0f), 1e-4f);
        }

        [Test]
        public void TinySensorJitterDoesNotCountAsShaking()
        {
            Assert.AreEqual(UraniumMath.StepReactivity(0.5f, 0.5f, 0f, 0f, 0f, 0.1f),
                            UraniumMath.StepReactivity(0.5f, 0.5f, 0.05f, 0f, 0f, 0.1f), 1e-5f);
        }

        [Test]
        public void ProgressFillsOnlyInTheGreenZone()
        {
            Assert.AreEqual(1f / 8f, UraniumMath.StepProgress(0f, 0.5f, 1f), 1e-4f);
            Assert.AreEqual(0.5f - 1f / 16f, UraniumMath.StepProgress(0.5f, 0.9f, 1f), 1e-4f);
            Assert.AreEqual(0f, UraniumMath.StepProgress(0f, 0f, 1f), 1e-4f);
        }

        [Test]
        public void DoseRisesTwiceAsFastWhenHot()
        {
            float normal = UraniumMath.StepDose(0f, 0.5f, 1f);
            float hot = UraniumMath.StepDose(0f, 0.9f, 1f);
            Assert.AreEqual(1f / 35f, normal, 1e-4f);
            Assert.AreEqual(2f * normal, hot, 1e-4f);
        }

        [Test]
        public void MeltdownBeatsOverdoseBeatsCapture()
        {
            Assert.AreEqual(UraniumMath.Outcome.Meltdown, UraniumMath.Check(1f, 1f, 1f));
            Assert.AreEqual(UraniumMath.Outcome.Overdose, UraniumMath.Check(0.5f, 1f, 1f));
            Assert.AreEqual(UraniumMath.Outcome.Captured, UraniumMath.Check(0.5f, 0.4f, 1f));
            Assert.AreEqual(UraniumMath.Outcome.None, UraniumMath.Check(0.5f, 0.4f, 0.9f));
        }

        [Test]
        public void SpikesComeEveryThreeToSixSeconds()
        {
            Assert.AreEqual(3f, UraniumMath.NextSpikeDelay(0f), 1e-4f);
            Assert.AreEqual(6f, UraniumMath.NextSpikeDelay(1f), 1e-4f);
        }

        [Test]
        public void FastLowDoseCaptureScoresHigher()
        {
            float good = UraniumMath.Performance(UraniumMath.ParSec, 0.2f);
            float bad = UraniumMath.Performance(UraniumMath.ParSec * 3f, 0.9f);
            Assert.Greater(good, bad);
            Assert.That(good, Is.InRange(0f, 1f));
            Assert.That(bad, Is.InRange(0f, 1f));
        }
    }
}
```

- [ ] **Step 2: Run the tests to see them fail.** Run the EditMode tests. Expected: compile error, `The name 'UraniumMath' does not exist`.

- [ ] **Step 3: Implement** `Assets/EnerGo/Scripts/Core/UraniumMath.cs`

```csharp
using UnityEngine;

namespace EnerGo
{
    // Pure rules of the uranium "Pemburu Geiger" mini-game. r = reactivity 0..1, rod = control rod 0 (out) .. 1 (fully in).
    public static class UraniumMath
    {
        // Where the ore hides, relative to the camera's starting direction.
        public const float HideMinYawDeg = 60f, HideMaxYawDeg = 150f;
        public const float HideMinPitchDeg = -10f, HideMaxPitchDeg = 15f;
        // Search: aim within this cone for this long to reveal the ore.
        public const float RevealAngleDeg = 10f, RevealHoldSec = 1.5f;
        public const float MinClicksPerSec = 1f, MaxClicksPerSec = 25f;
        public const float MaxPulsesPerSec = 12f;   // vibration pulses blur together faster than this
        // Secure: reactivity.
        public const float GreenMin = 0.40f, GreenMax = 0.60f, HotAbove = 0.80f;
        public const float Growth = 0.05f;          // per second: an unchecked chain reaction creeps up
        public const float RodPull = 0.9f;          // per second: r is pulled toward (1 - rod)
        public const float NoiseGain = 0.25f;       // per second, Perlin noise in [-1, 1] times this
        public const float JoltDeadzone = 0.08f;    // accelerometer jitter below this is not a shake
        public const float JoltGain = 2.0f;         // per second per unit of jolt above the deadzone
        public const float SpikeSize = 0.20f;       // neutron spike, added at once
        public const float SpikeMinSec = 3f, SpikeMaxSec = 6f, SpikeWarnSec = 0.5f;
        public const float RodRangeDeg = 40f;       // tilting this far from the start moves the rod from 0 to 1
        public const float FillPerSec = 1f / 8f, DrainPerSec = 1f / 16f;
        public const float DosePerSec = 1f / 35f, HotDoseMul = 2f;
        public const float ParSec = 18f;            // a good secure phase, for the speed score
        public const float FailShowSec = 1.5f;

        public static float GeigerRate(float angleDeg)
        {
            float t = Mathf.Clamp01(angleDeg / 150f);
            return MinClicksPerSec * Mathf.Pow(MaxClicksPerSec / MinClicksPerSec, 1f - t);
        }

        // While securing, the counter follows the reactivity instead of the aim.
        public static float SecureClickRate(float r) => Mathf.Lerp(4f, MaxClicksPerSec, Mathf.Clamp01(r));

        // Radioactive decay is random: exponential gaps between clicks, mean 1 / rate.
        public static float NextClickDelay(float rate, float u01) => -Mathf.Log(Mathf.Max(u01, 1e-4f)) / Mathf.Max(rate, 0.01f);

        public static int Meter(float angleDeg) => Mathf.RoundToInt(100f * (1f - Mathf.Clamp(angleDeg, 0f, 180f) / 180f));

        // Forward tilt from the accelerometer: 0 = phone upright, 90 = lying flat screen up.
        public static float TiltDeg(Vector3 gravity) =>
            Mathf.Asin(Mathf.Clamp(-gravity.normalized.z, -1f, 1f)) * Mathf.Rad2Deg;

        public static float RodFromTilt(float tiltDeg, float baselineDeg) =>
            Mathf.Clamp01(0.5f + (tiltDeg - baselineDeg) / RodRangeDeg);

        public static float StepReactivity(float r, float rod, float jolt, float spike, float noise, float dt)
        {
            float shake = Mathf.Max(0f, jolt - JoltDeadzone) * JoltGain;
            r += (Growth + ((1f - Mathf.Clamp01(rod)) - r) * RodPull + shake + noise) * dt + spike;
            return Mathf.Clamp01(r);
        }

        public static bool InGreen(float r) => r >= GreenMin && r <= GreenMax;

        public static float StepProgress(float progress, float r, float dt) =>
            Mathf.Clamp01(progress + (InGreen(r) ? FillPerSec : -DrainPerSec) * dt);

        public static float StepDose(float dose, float r, float dt) =>
            Mathf.Clamp01(dose + DosePerSec * (r > HotAbove ? HotDoseMul : 1f) * dt);

        public static float NextSpikeDelay(float u01) => Mathf.Lerp(SpikeMinSec, SpikeMaxSec, Mathf.Clamp01(u01));

        public enum Outcome { None, Captured, Meltdown, Overdose }

        public static Outcome Check(float r, float dose, float progress) =>
            r >= 1f ? Outcome.Meltdown : dose >= 1f ? Outcome.Overdose : progress >= 1f ? Outcome.Captured : Outcome.None;

        // Half speed, half dose left.
        public static float Performance(float elapsed, float dose) =>
            Mathf.Clamp01(0.5f * Mathf.InverseLerp(ParSec * 2.5f, ParSec, elapsed) + 0.5f * (1f - Mathf.Clamp01(dose)));
    }
}
```

- [ ] **Step 4: Run the tests.** Expected: all pass (34 + 14 = 48).

- [ ] **Step 5: Commit**

```bash
git add Assets/EnerGo/Scripts/Core/UraniumMath.cs Assets/EnerGo/Scripts/Core/UraniumMath.cs.meta Assets/EnerGo/Tests/Editor/UraniumMathTests.cs Assets/EnerGo/Tests/Editor/UraniumMathTests.cs.meta
git commit -m "feat: UraniumMath rules (Geiger rate, reactivity, dose) with tests

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 4: `OilMath` (layers, drill, gas, pump rhythm)

**Files:**
- Create: `Assets/EnerGo/Scripts/Core/OilMath.cs`
- Test: `Assets/EnerGo/Tests/Editor/OilMathTests.cs`

**Interfaces:**
- Produces (namespace `EnerGo`, static class `OilMath`):
  - **Enums:**
    - `enum Layer { Soil, Rock, CapRock, Gas, Oil, Brine }`
    - `enum Stroke { Perfect, Early, Late }`
    - `enum DrillOutcome { None, FoundOil, Blowout, HitWater }`
  - **Constants (all `public const float`):**
    - drilling: `MaxSpeed`, `StopSpeed`, `SafeGasSpeed`
    - pumping: `Beat`, `PerfectWindow`
    - outcome: `ParSec`, `FailShowSec`
    - display: `DisplayDepthM`
  - `float[] Layers(int seed)`: 5 ascending bottoms for Soil..Oil. Everything below the last one is Brine.
  - `Layer LayerAt(float depth, float[] bottoms)`
  - `float SpeedMul(Layer layer)`
  - `void StepDrill(ref float depth, ref float speed, bool held, float layerMul, float dt)`
  - `float StepGasPressure(float pressure, float speed, bool inGas, float dt)`
  - `DrillOutcome CheckDrill(Layer layer, float speed, bool held, float gasPressure)`
  - `float BeatFor(float drum)`
  - `Stroke JudgeStroke(float interval, float beat)`
  - `void ApplyStroke(Stroke stroke, ref float drum, ref float pressure)`
  - `void StepPumpIdle(ref float drum, ref float pressure, float sinceStroke, float dt)`
  - `float Performance(float elapsed, int perfects, int strokes)`

- [ ] **Step 1: Write the failing tests** in `Assets/EnerGo/Tests/Editor/OilMathTests.cs`

```csharp
using NUnit.Framework;

namespace EnerGo.Tests
{
    public class OilMathTests
    {
        static readonly float[] Fixed = { 0.1f, 0.3f, 0.45f, 0.55f, 0.62f };

        [Test]
        public void LayersAreDeterministicPerSeed()
        {
            CollectionAssert.AreEqual(OilMath.Layers(42), OilMath.Layers(42));
            CollectionAssert.AreNotEqual(OilMath.Layers(1), OilMath.Layers(2));
        }

        [Test]
        public void LayersAreOrderedWithAThinOilBandAndBrineBelow()
        {
            for (int seed = 0; seed < 200; seed++)
            {
                var b = OilMath.Layers(seed);
                Assert.AreEqual(5, b.Length);
                for (int i = 1; i < b.Length; i++) Assert.Greater(b[i], b[i - 1]);
                float oil = b[4] - b[3];
                Assert.That(oil, Is.InRange(0.06f - 1e-4f, 0.09f + 1e-4f), "seed " + seed);
                Assert.Less(b[4], 0.9f, "brine must remain below, seed " + seed);
            }
        }

        [Test]
        public void LayerAtUsesHalfOpenBands()
        {
            Assert.AreEqual(OilMath.Layer.Soil, OilMath.LayerAt(0f, Fixed));
            Assert.AreEqual(OilMath.Layer.Soil, OilMath.LayerAt(0.0999f, Fixed));
            Assert.AreEqual(OilMath.Layer.Rock, OilMath.LayerAt(0.1f, Fixed));
            Assert.AreEqual(OilMath.Layer.Gas, OilMath.LayerAt(0.5f, Fixed));
            Assert.AreEqual(OilMath.Layer.Oil, OilMath.LayerAt(0.6f, Fixed));
            Assert.AreEqual(OilMath.Layer.Brine, OilMath.LayerAt(0.62f, Fixed));
            Assert.AreEqual(OilMath.Layer.Brine, OilMath.LayerAt(1f, Fixed));
        }

        [Test]
        public void HardCapRockIsSlowerAndGasIsFaster()
        {
            Assert.Less(OilMath.SpeedMul(OilMath.Layer.CapRock), OilMath.SpeedMul(OilMath.Layer.Rock));
            Assert.Greater(OilMath.SpeedMul(OilMath.Layer.Gas), OilMath.SpeedMul(OilMath.Layer.Soil));
        }

        [Test]
        public void HeldDrillAcceleratesUpToTheLayerCap()
        {
            float depth = 0f, speed = 0f;
            for (int i = 0; i < 100; i++) OilMath.StepDrill(ref depth, ref speed, true, 0.35f, 0.1f);
            Assert.AreEqual(OilMath.MaxSpeed * 0.35f, speed, 1e-4f);
            Assert.Greater(depth, 0f);
        }

        [Test]
        public void ReleasedDrillKeepsMovingForAWhile()
        {
            float depth = 0.2f, speed = OilMath.MaxSpeed;
            OilMath.StepDrill(ref depth, ref speed, false, 1f, 0.1f);
            Assert.Greater(speed, 0f);
            Assert.Greater(depth, 0.2f);
            for (int i = 0; i < 100; i++) OilMath.StepDrill(ref depth, ref speed, false, 1f, 0.1f);
            Assert.AreEqual(0f, speed, 1e-6f);
        }

        [Test]
        public void CoastingFromGasSpeedOvershootsTheOilBand()
        {
            // Releasing right at the top of the oil band at full gas speed slides past even the thickest (9 %) band.
            float depth = 0f, speed = OilMath.MaxSpeed * OilMath.SpeedMul(OilMath.Layer.Gas);
            while (speed > 0f) OilMath.StepDrill(ref depth, ref speed, false, 1f, 0.02f);
            Assert.Greater(depth, 0.09f);
        }

        [Test]
        public void FastDrillingInGasBuildsPressure()
        {
            Assert.AreEqual(0f, OilMath.StepGasPressure(0f, OilMath.SafeGasSpeed * 0.5f, true, 1f), 1e-5f);
            float p = 0f;
            for (int i = 0; i < 20; i++) p = OilMath.StepGasPressure(p, OilMath.MaxSpeed * 1.2f, true, 0.05f);
            Assert.AreEqual(1f, p, 1e-4f); // one second of full speed in gas = blowout
            Assert.Less(OilMath.StepGasPressure(0.5f, OilMath.MaxSpeed, false, 1f), 0.5f);
        }

        [Test]
        public void DrillOutcomes()
        {
            Assert.AreEqual(OilMath.DrillOutcome.Blowout, OilMath.CheckDrill(OilMath.Layer.Gas, 0.1f, true, 1f));
            Assert.AreEqual(OilMath.DrillOutcome.HitWater, OilMath.CheckDrill(OilMath.Layer.Brine, 0.1f, false, 0f));
            Assert.AreEqual(OilMath.DrillOutcome.FoundOil, OilMath.CheckDrill(OilMath.Layer.Oil, 0f, false, 0f));
            Assert.AreEqual(OilMath.DrillOutcome.None, OilMath.CheckDrill(OilMath.Layer.Oil, 0f, true, 0f));
            Assert.AreEqual(OilMath.DrillOutcome.None, OilMath.CheckDrill(OilMath.Layer.Oil, 0.1f, false, 0f));
            Assert.AreEqual(OilMath.DrillOutcome.None, OilMath.CheckDrill(OilMath.Layer.Rock, 0f, false, 0f));
        }

        [Test]
        public void StrokeTiming()
        {
            float b = OilMath.Beat;
            Assert.AreEqual(OilMath.Stroke.Perfect, OilMath.JudgeStroke(b, b));
            Assert.AreEqual(OilMath.Stroke.Perfect, OilMath.JudgeStroke(b - 0.19f, b));
            Assert.AreEqual(OilMath.Stroke.Early, OilMath.JudgeStroke(b - 0.3f, b));
            Assert.AreEqual(OilMath.Stroke.Late, OilMath.JudgeStroke(b + 0.3f, b));
            Assert.AreEqual(OilMath.Stroke.Late, OilMath.JudgeStroke(float.PositiveInfinity, b)); // first stroke only starts the rhythm
        }

        [Test]
        public void TenPerfectStrokesFillTheDrumAndFourEarlyOnesBlowIt()
        {
            float drum = 0f, pressure = 0f;
            for (int i = 0; i < 10; i++) OilMath.ApplyStroke(OilMath.Stroke.Perfect, ref drum, ref pressure);
            Assert.AreEqual(1f, drum, 1e-6f);
            Assert.AreEqual(0f, pressure, 1e-6f);
            drum = 0f;
            for (int i = 0; i < 4; i++) OilMath.ApplyStroke(OilMath.Stroke.Early, ref drum, ref pressure);
            Assert.AreEqual(1f, pressure, 1e-6f);
            Assert.AreEqual(0f, drum, 1e-6f);
            float d = 0.3f, p = 0f;
            OilMath.ApplyStroke(OilMath.Stroke.Late, ref d, ref p);
            Assert.AreEqual(0.3f, d, 1e-6f);
        }

        [Test]
        public void IdlePumpStallsAfterTwoSecondsAndPressureRelaxes()
        {
            float drum = 0.5f, pressure = 0.5f;
            OilMath.StepPumpIdle(ref drum, ref pressure, 1f, 1f);
            Assert.AreEqual(0.5f, drum, 1e-6f);
            Assert.Less(pressure, 0.5f);
            OilMath.StepPumpIdle(ref drum, ref pressure, 3f, 1f);
            Assert.Less(drum, 0.5f);
        }

        [Test]
        public void TempoPicksUpNearlyFull()
        {
            Assert.AreEqual(OilMath.Beat, OilMath.BeatFor(0.5f), 1e-6f);
            Assert.AreEqual(OilMath.Beat * 0.85f, OilMath.BeatFor(0.8f), 1e-6f);
        }

        [Test]
        public void PerformanceRewardsSpeedAndRhythm()
        {
            Assert.Greater(OilMath.Performance(OilMath.ParSec, 10, 10), OilMath.Performance(OilMath.ParSec * 3f, 3, 10));
            Assert.That(OilMath.Performance(0f, 0, 0), Is.InRange(0f, 1f));
        }
    }
}
```

- [ ] **Step 2: Run the tests to see them fail.** Expected: compile error, `The name 'OilMath' does not exist`.

- [ ] **Step 3: Implement** `Assets/EnerGo/Scripts/Core/OilMath.cs`

```csharp
using UnityEngine;

namespace EnerGo
{
    // Pure rules of the oil "Bor & Pompa" mini-game. Depth runs 0 (surface) .. 1 (bottom of the cross-section).
    public static class OilMath
    {
        public enum Layer { Soil, Rock, CapRock, Gas, Oil, Brine }
        public enum Stroke { Perfect, Early, Late }
        public enum DrillOutcome { None, FoundOil, Blowout, HitWater }

        // Drill
        public const float MaxSpeed = 0.22f;          // depth per second at x1 layer speed
        public const float Accel = 0.35f;             // speed gained per second while held
        public const float Brake = 0.30f;             // speed lost per second once released (momentum)
        public const float StopSpeed = 0.004f;        // slower than this counts as stopped
        public const float SafeGasSpeed = 0.08f;      // gas pressure only builds above this speed
        public const float GasPressureGain = 9f;      // pressure per second per unit of speed above safe
        public const float GasPressureRelief = 0.25f; // per second when slow or outside the gas
        public const float DisplayDepthM = 2000f;     // depth 1.0 shown as metres
        // Pump
        public const float Beat = 1.2f;               // seconds per stroke
        public const float FastBeatAbove = 0.7f, FastBeatMul = 0.85f;
        public const float PerfectWindow = 0.2f;
        public const float DrumPerPerfect = 0.1f;
        public const float EarlyPressure = 0.25f;
        public const float StallAfterSec = 2f, StallDrainPerSec = 1f / 20f;
        public const float PumpPressureRelief = 0.1f;
        public const float ParSec = 25f;              // drill + pump, for the speed score
        public const float FailShowSec = 1.5f;

        // Bottoms of Soil, Rock, CapRock, Gas, Oil; everything deeper is brine.
        public static float[] Layers(int seed)
        {
            var rng = new System.Random(seed);
            float R(float min, float max) => min + (float)rng.NextDouble() * (max - min);
            float soil = R(0.08f, 0.14f);
            float rock = soil + R(0.18f, 0.28f);
            float cap = rock + R(0.10f, 0.16f);
            float gas = cap + R(0.08f, 0.14f);
            float oil = gas + R(0.06f, 0.09f);
            return new[] { soil, rock, cap, gas, oil };
        }

        public static Layer LayerAt(float depth, float[] bottoms)
        {
            for (int i = 0; i < bottoms.Length; i++)
                if (depth < bottoms[i]) return (Layer)i;
            return Layer.Brine;
        }

        public static float SpeedMul(Layer layer)
        {
            switch (layer)
            {
                case Layer.Rock: return 0.7f;
                case Layer.CapRock: return 0.35f;
                case Layer.Gas: return 1.2f;
                case Layer.Oil: return 0.8f;
                default: return 1f;
            }
        }

        public static void StepDrill(ref float depth, ref float speed, bool held, float layerMul, float dt)
        {
            speed = held ? Mathf.MoveTowards(speed, MaxSpeed * layerMul, Accel * dt) : Mathf.MoveTowards(speed, 0f, Brake * dt);
            depth = Mathf.Clamp01(depth + speed * dt);
        }

        public static float StepGasPressure(float pressure, float speed, bool inGas, float dt)
        {
            float rate = inGas && speed > SafeGasSpeed ? (speed - SafeGasSpeed) * GasPressureGain : -GasPressureRelief;
            return Mathf.Clamp01(pressure + rate * dt);
        }

        public static DrillOutcome CheckDrill(Layer layer, float speed, bool held, float gasPressure)
        {
            if (gasPressure >= 1f) return DrillOutcome.Blowout;
            if (layer == Layer.Brine) return DrillOutcome.HitWater;
            if (layer == Layer.Oil && !held && speed <= StopSpeed) return DrillOutcome.FoundOil;
            return DrillOutcome.None;
        }

        public static float BeatFor(float drum) => drum >= FastBeatAbove ? Beat * FastBeatMul : Beat;

        // `interval` = time since the previous stroke (infinity for the first one).
        public static Stroke JudgeStroke(float interval, float beat) =>
            interval < beat - PerfectWindow ? Stroke.Early : interval > beat + PerfectWindow ? Stroke.Late : Stroke.Perfect;

        public static void ApplyStroke(Stroke stroke, ref float drum, ref float pressure)
        {
            if (stroke == Stroke.Perfect) drum = Mathf.Clamp01(drum + DrumPerPerfect);
            else if (stroke == Stroke.Early) pressure = Mathf.Clamp01(pressure + EarlyPressure);
        }

        public static void StepPumpIdle(ref float drum, ref float pressure, float sinceStroke, float dt)
        {
            pressure = Mathf.Clamp01(pressure - PumpPressureRelief * dt);
            if (sinceStroke > StallAfterSec) drum = Mathf.Clamp01(drum - StallDrainPerSec * dt);
        }

        // Half speed, half share of perfect strokes.
        public static float Performance(float elapsed, int perfects, int strokes) =>
            Mathf.Clamp01(0.5f * Mathf.InverseLerp(ParSec * 2.5f, ParSec, elapsed) + 0.5f * (strokes > 0 ? (float)perfects / strokes : 0f));
    }
}
```

- [ ] **Step 4: Run the tests.** Expected: all pass (48 + 14 = 62). If `TenPerfectStrokesFillTheDrum…` fails on float rounding, set `DrumPerPerfect = 0.1001f`. Do not loosen the assertion.

- [ ] **Step 5: Commit**

```bash
git add Assets/EnerGo/Scripts/Core/OilMath.cs Assets/EnerGo/Scripts/Core/OilMath.cs.meta Assets/EnerGo/Tests/Editor/OilMathTests.cs Assets/EnerGo/Tests/Editor/OilMathTests.cs.meta
git commit -m "feat: OilMath rules (layers, drill momentum, gas pressure, pump rhythm) with tests

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 5: Haptics + Geiger click + pump beat sounds

**Files:**
- Create: `Assets/EnerGo/Scripts/Haptics.cs`
- Modify: `Assets/EnerGo/Scripts/SFXManager.cs`. Add the methods after `PlayBaraHancur` (around line 127).

**Interfaces:**
- Produces:
  - `Haptics.Pulse(long ms)`
  - `SFXManager.Instance.PlayGeigerClick()`
  - `SFXManager.Instance.PlayPumpBeat()`

- [ ] **Step 1: Create** `Assets/EnerGo/Scripts/Haptics.cs`

```csharp
using UnityEngine;

namespace EnerGo
{
    // Short vibration pulses. Android: Vibrator.vibrate(ms) so pulses can be short and frequent;
    // otherwise Handheld.Vibrate (~0.5 s, so rate-limited). Referencing Handheld.Vibrate also makes
    // Unity add the VIBRATE permission to the Android manifest.
    public static class Haptics
    {
        private static AndroidJavaObject vibrator;
        private static bool triedVibrator;
        private static float lastFallbackAt = -10f;

        public static void Pulse(long ms)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (!triedVibrator)
            {
                triedVibrator = true;
                try
                {
                    using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                    using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                        vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
                }
                catch (System.Exception e) { Debug.LogWarning("EnerGo haptics: " + e.Message); }
            }
            if (vibrator != null)
            {
                try { vibrator.Call("vibrate", ms); return; }
                catch (System.Exception) { vibrator = null; }
            }
#endif
#if UNITY_ANDROID || UNITY_IOS
            if (Time.unscaledTime - lastFallbackAt < 0.5f) return;
            lastFallbackAt = Time.unscaledTime;
            Handheld.Vibrate();
#endif
        }
    }
}
```

- [ ] **Step 2: Add the sounds.** In `SFXManager.cs`, after `public void PlayBaraHancur() => …;`, add:

```csharp

        private AudioClip geigerClick;
        private AudioSource pumpSource;

        // Geiger counter tick, made in code: 5 ms of fading noise.
        public void PlayGeigerClick()
        {
            if (geigerClick == null)
            {
                const int rate = 44100;
                int n = rate / 200;
                var data = new float[n];
                var rng = new System.Random(7);
                for (int i = 0; i < n; i++) data[i] = (float)(rng.NextDouble() * 2.0 - 1.0) * (1f - i / (float)n);
                geigerClick = AudioClip.Create("geiger_click", n, 1, rate, false);
                geigerClick.SetData(data, 0);
            }
            PlayOneShot(geigerClick, 0.8f);
        }

        // Pump-jack thud: the falling-rock clip played low (placeholder until a real pump sound exists).
        public void PlayPumpBeat()
        {
            if (clipBatuJatuh == null) return;
            if (pumpSource == null) { pumpSource = gameObject.AddComponent<AudioSource>(); pumpSource.playOnAwake = false; pumpSource.pitch = 0.6f; }
            pumpSource.PlayOneShot(clipBatuJatuh, 0.7f);
        }
```

- [ ] **Step 3: Compile and run the EditMode tests.** Expected: no compile errors, 62/62 pass.

- [ ] **Step 4: Commit**

```bash
git add Assets/EnerGo/Scripts/Haptics.cs Assets/EnerGo/Scripts/Haptics.cs.meta Assets/EnerGo/Scripts/SFXManager.cs
git commit -m "feat: short haptic pulses, code-made Geiger click and pump beat sound

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 6: Uranium mini-game "Pemburu Geiger"

**Files:**
- Create: `Assets/EnerGo/Scripts/ARCapturePrototype.Uranium.cs`
- Modify: `Assets/EnerGo/Scripts/ARCapturePrototype.cs` (hooks listed in Step 2)
- Modify: `Assets/EnerGo/Scripts/Core/ResourceIds.cs:15-16`
- Modify: `Assets/EnerGo/Tests/Editor/ResourceIdsTests.cs:16-24`

**Interfaces:**
- Consumes:
  - `UraniumMath.*` (Task 3)
  - `Haptics.Pulse`, `SFXManager.PlayGeigerClick` (Task 5)
  - existing private members of `ARCapturePrototype`:
    - state: `target`, `arCamera`, `energyMaterial`, `feedback`, `finishAt`, `FinishDelay`, `capturePerformance`, `nextSpawnTime`
    - helpers: `ReadGravity(out Vector3)`, `DrawHudBar(...)`
    - styles and colours: `styleRadarAlert`, `styleRadarLocked`, `styleTapsCounter`, `HudOk`, `HudWarn`, `HudOutline`, `HudFill`
- Produces:
  - `enum UraniumPhase { None, Search, Secure, Done, Failed }` and the field `uraniumPhase`
  - `SpawnUranium()`, `UpdateUranium()`, `DrawUraniumHud(float x, float y, float w)`, `DrawUraniumOverlay(float sw, float sh)`
  - the shared helper `Material UnlitColor(Color c)`, which Task 7 also uses
  - `ResourceIds.IsPlayable("uranium") == true`

- [ ] **Step 1: Update the ResourceIds test first.** Replace `OnlySunCoalGoldArePlayable` in `ResourceIdsTests.cs` with:

```csharp
        [Test]
        public void PlayableResources()
        {
            Assert.IsTrue(ResourceIds.IsPlayable(ResourceIds.Sun));
            Assert.IsTrue(ResourceIds.IsPlayable(ResourceIds.Coal));
            Assert.IsTrue(ResourceIds.IsPlayable(ResourceIds.Gold));
            Assert.IsTrue(ResourceIds.IsPlayable(ResourceIds.Uranium));
            Assert.IsFalse(ResourceIds.IsPlayable(ResourceIds.Oil));
            Assert.IsFalse(ResourceIds.IsPlayable("random"));
        }
```

Run the tests. Expected: `PlayableResources` FAILS on the uranium assert.

- [ ] **Step 2: Add the hooks in `ARCapturePrototype.cs`.** Make these exact edits:
  1. Line 11: change `public class ARCapturePrototype : MonoBehaviour` to `public partial class ARCapturePrototype : MonoBehaviour`.
  2. In `Awake` (line 236): replace `if (resourceId != ResourceIds.Sun && resourceId != ResourceIds.Coal && resourceId != ResourceIds.Gold) resourceId = ResourceIds.Gold;` with `if (!ResourceIds.IsPlayable(resourceId)) resourceId = ResourceIds.Gold;`.
  3. In `Update`, in the `if (target != null)` block, after `if (resourceId == ResourceIds.Coal) UpdateCoal();` add:
     ```csharp
                if (resourceId == ResourceIds.Uranium && EnerGoProgress.Current.pendingCapture == null) UpdateUranium();
     ```
  4. In `CompleteCapture`, after `goldPhase = GoldPhase.None;` add `uraniumPhase = UraniumPhase.None;`.
  5. In `CheckTapOnTarget`, after the line `if (resourceId == ResourceIds.Sun) return false; …` add:
     ```csharp
            if (resourceId == ResourceIds.Uranium) return false; // found by listening, secured by tilting
     ```
  6. In `SpawnGold`, as its first line (before the coal line), add:
     ```csharp
            if (resourceId == ResourceIds.Uranium) { SpawnUranium(); return; }
     ```
  7. In `OnGUI`, after `if (goldPanning && EnerGoProgress.Current.pendingCapture == null) DrawGoldPan(sw, sh);` add:
     ```csharp
            if (uraniumPhase != UraniumPhase.None && EnerGoProgress.Current.pendingCapture == null) DrawUraniumOverlay(sw, sh);
     ```
  8. In `DrawRadarHUD`, right after `bool locked = false;`, add the line below. It keeps the usual turn-left/right arrow from giving away the hidden ore:
     ```csharp
            if (uraniumPhase != UraniumPhase.None) { DrawUraniumHud(hudX, hudY, hudW); return; }
     ```
  9. Replace `MiniGameName` with:
     ```csharp
        private static string MiniGameName(string id) =>
            id == ResourceIds.Sun ? "Ekstraksi" : id == ResourceIds.Coal ? "Tambang" : id == ResourceIds.Uranium ? "Pemburu Geiger" : "Dulang";
     ```

- [ ] **Step 3: Create** `Assets/EnerGo/Scripts/ARCapturePrototype.Uranium.cs`

```csharp
using UnityEngine;
using UnityEngine.InputSystem;

namespace EnerGo
{
    // Uranium "Pemburu Geiger": find the hidden ore by its Geiger clicks and vibration, then keep its
    // reactivity in the green zone with the control rod (forward tilt) before the radiation dose fills up.
    public partial class ARCapturePrototype
    {
        private enum UraniumPhase { None, Search, Secure, Done, Failed }

        private static readonly Color UraniumGlow = new Color(0.22f, 1f, 0.08f); // #39FF14
        private const float UraniumOreSize = 0.22f, UraniumOreDistance = 1.6f;

        private UraniumPhase uraniumPhase = UraniumPhase.None;
        private float uraniumAngle = 180f;          // camera forward vs ore, degrees
        private float uraniumLockHold;              // seconds aimed within RevealAngleDeg
        private float uraniumNextClickAt, uraniumLastPulseAt = -10f;
        private float uraniumR, uraniumRod = 0.5f, uraniumDose, uraniumProgress;
        private float uraniumNextSpikeAt, uraniumNoiseSeed;
        private float uraniumTiltBase = float.NaN, uraniumGravity;
        private float uraniumRevealAt, uraniumPhaseAt;
        private string uraniumFailText = "";

        private bool UraniumSpikeSoon => uraniumPhase == UraniumPhase.Secure && Time.time >= uraniumNextSpikeAt - UraniumMath.SpikeWarnSec;

        // Flat-coloured unlit material (these scenes have no light). Shared with the oil pump jack.
        private Material UnlitColor(Color c)
        {
            var m = energyMaterial != null ? new Material(energyMaterial)
                : new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Standard"));
            if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", null);
            if (m.HasProperty("_MainTex")) m.SetTexture("_MainTex", null);
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            if (m.HasProperty("_Color")) m.SetColor("_Color", c);
            return m;
        }

        private void SpawnUranium()
        {
            target = new GameObject("Target Uranium");
            var mat = UnlitColor(UraniumGlow);
            // ponytail: primitive ore cluster; swap for Resources/Uranium/uranium when a real model exists.
            for (int i = 0; i < 4; i++)
            {
                var lump = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Destroy(lump.GetComponent<Collider>());
                lump.transform.SetParent(target.transform, false);
                lump.transform.localPosition = i == 0 ? Vector3.zero : Random.onUnitSphere * 0.45f;
                lump.transform.localScale = Vector3.one * (i == 0 ? 1f : Random.Range(0.45f, 0.65f));
                lump.GetComponent<Renderer>().sharedMaterial = mat;
            }
            target.transform.localScale = Vector3.one * UraniumOreSize;

            Vector3 fwd = arCamera.transform.forward;
            fwd.y = 0f;
            if (fwd.sqrMagnitude < 0.01f) fwd = Vector3.forward;
            float yaw = Random.Range(UraniumMath.HideMinYawDeg, UraniumMath.HideMaxYawDeg) * (Random.value < 0.5f ? -1f : 1f);
            float pitch = Random.Range(UraniumMath.HideMinPitchDeg, UraniumMath.HideMaxPitchDeg);
            Vector3 dir = Quaternion.LookRotation(fwd.normalized) * Quaternion.Euler(-pitch, yaw, 0f) * Vector3.forward;
            target.transform.position = arCamera.transform.position + dir * UraniumOreDistance;
            target.SetActive(false); // invisible until the Geiger counter finds it

            uraniumPhase = UraniumPhase.Search;
            uraniumAngle = 180f;
            uraniumLockHold = 0f;
            uraniumNextClickAt = Time.time;
            uraniumR = 0.5f;
            uraniumRod = 0.5f;
            uraniumDose = 0f;
            uraniumProgress = 0f;
            uraniumTiltBase = float.NaN;
            uraniumGravity = 0f;
            uraniumFailText = "";
            feedback = "DENGARKAN PENCACAH GEIGER";
        }

        private void UpdateUranium()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f || arCamera == null) return;
            switch (uraniumPhase)
            {
                case UraniumPhase.Search:
                    uraniumAngle = Vector3.Angle(arCamera.transform.forward, target.transform.position - arCamera.transform.position);
                    TickGeiger(UraniumMath.GeigerRate(uraniumAngle));
                    uraniumLockHold = uraniumAngle <= UraniumMath.RevealAngleDeg ? uraniumLockHold + dt : 0f;
                    if (uraniumLockHold >= UraniumMath.RevealHoldSec) BeginUraniumSecure();
                    break;
                case UraniumPhase.Secure:
                    StepUraniumSecure(dt);
                    break;
                case UraniumPhase.Failed:
                    if (Time.time - uraniumPhaseAt >= UraniumMath.FailShowSec) RetryUranium();
                    return; // RetryUranium may have destroyed the target
            }
            if (target != null && target.activeSelf)
            {
                // Pop in on reveal, then pulse like it is glowing.
                float pop = Mathf.Clamp01((Time.time - uraniumRevealAt) / 0.3f);
                target.transform.localScale = Vector3.one * UraniumOreSize * pop * (1f + 0.08f * Mathf.Sin(Time.time * 6f));
                target.transform.Rotate(0f, 20f * dt, 0f, Space.Self);
            }
        }

        // One random Geiger click (and a vibration pulse, rate-limited) at `rate` clicks per second on average.
        private void TickGeiger(float rate)
        {
            if (Time.time < uraniumNextClickAt) return;
            if (SFXManager.Instance != null) SFXManager.Instance.PlayGeigerClick();
            if (Time.time - uraniumLastPulseAt >= 1f / UraniumMath.MaxPulsesPerSec)
            {
                Haptics.Pulse(12);
                uraniumLastPulseAt = Time.time;
            }
            uraniumNextClickAt = Time.time + UraniumMath.NextClickDelay(rate, Random.value);
        }

        private void BeginUraniumSecure()
        {
            uraniumPhase = UraniumPhase.Secure;
            uraniumPhaseAt = uraniumRevealAt = Time.time;
            target.SetActive(true);
            uraniumR = 0.5f;
            uraniumRod = 0.5f;
            uraniumTiltBase = float.NaN; // "halfway" is however the player holds the phone right now
            uraniumNextSpikeAt = Time.time + UraniumMath.NextSpikeDelay(Random.value);
            uraniumNoiseSeed = Random.Range(0f, 100f);
            Haptics.Pulse(80);
            feedback = "BIJIH DITEMUKAN! JAGA REAKTIVITAS";
        }

        private void StepUraniumSecure(float dt)
        {
            ReadUraniumRod(dt, out float jolt);
            float spike = 0f;
            if (Time.time >= uraniumNextSpikeAt)
            {
                spike = UraniumMath.SpikeSize;
                uraniumNextSpikeAt = Time.time + UraniumMath.NextSpikeDelay(Random.value);
                Haptics.Pulse(60);
            }
            float noise = (Mathf.PerlinNoise(uraniumNoiseSeed, Time.time * 0.7f) - 0.5f) * 2f * UraniumMath.NoiseGain;
            uraniumR = UraniumMath.StepReactivity(uraniumR, uraniumRod, jolt, spike, noise, dt);
            uraniumProgress = UraniumMath.StepProgress(uraniumProgress, uraniumR, dt);
            uraniumDose = UraniumMath.StepDose(uraniumDose, uraniumR, dt);
            TickGeiger(UraniumMath.SecureClickRate(uraniumR));

            switch (UraniumMath.Check(uraniumR, uraniumDose, uraniumProgress))
            {
                case UraniumMath.Outcome.Meltdown: FailUranium("MELTDOWN!"); break;
                case UraniumMath.Outcome.Overdose: FailUranium("DOSIS BERLEBIH!"); break;
                case UraniumMath.Outcome.Captured:
                    uraniumPhase = UraniumPhase.Done;
                    uraniumPhaseAt = Time.time;
                    capturePerformance = UraniumMath.Performance(Time.time - uraniumRevealAt, uraniumDose);
                    finishAt = Time.time + FinishDelay;
                    feedback = "URANIUM AMAN DI WADAH TIMBAL";
                    break;
            }
        }

        // Control rod from the forward tilt relative to how the phone was held at the reveal, plus how hard it was shaken.
        // Editor (no accelerometer): W pushes the rod in, S pulls it out.
        private void ReadUraniumRod(float dt, out float jolt)
        {
            jolt = 0f;
            if (ReadGravity(out Vector3 a))
            {
                if (uraniumGravity <= 0f) uraniumGravity = a.magnitude;
                jolt = Mathf.Abs(a.magnitude / uraniumGravity - 1f);
                float tilt = UraniumMath.TiltDeg(a);
                if (float.IsNaN(uraniumTiltBase)) uraniumTiltBase = tilt;
                uraniumRod = Mathf.Lerp(uraniumRod, UraniumMath.RodFromTilt(tilt, uraniumTiltBase), 1f - Mathf.Exp(-10f * dt));
                return;
            }
            if (Keyboard.current == null) return;
            float dir = (Keyboard.current.wKey.isPressed ? 1f : 0f) - (Keyboard.current.sKey.isPressed ? 1f : 0f);
            uraniumRod = Mathf.Clamp01(uraniumRod + dir * 0.8f * dt);
        }

        private void FailUranium(string text)
        {
            uraniumPhase = UraniumPhase.Failed;
            uraniumPhaseAt = Time.time;
            uraniumFailText = text;
            feedback = text;
            Haptics.Pulse(300);
            if (SFXManager.Instance != null) SFXManager.Instance.PlayQuizSalah();
        }

        // After a failure: hide a fresh ore somewhere else and start listening again.
        private void RetryUranium()
        {
            uraniumPhase = UraniumPhase.None;
            if (target != null) Destroy(target);
            target = null;
            nextSpawnTime = Time.time + 0.3f;
            feedback = "CARI BIJIH URANIUM BARU";
        }

        private void DrawUraniumHud(float x, float y, float w)
        {
            string line;
            bool warn;
            float bar;
            string label;
            switch (uraniumPhase)
            {
                case UraniumPhase.Search:
                    int meter = UraniumMath.Meter(uraniumAngle);
                    line = uraniumLockHold > 0f ? "SINYAL KUAT! TAHAN ARAH HP" : "SINYAL RAD · PUTAR PELAN-PELAN";
                    warn = false;
                    bar = meter / 100f;
                    label = meter.ToString();
                    break;
                case UraniumPhase.Done:
                    line = "URANIUM AMAN DI WADAH TIMBAL!";
                    warn = false;
                    bar = 1f;
                    label = "100%";
                    break;
                case UraniumPhase.Failed:
                    line = uraniumFailText;
                    warn = true;
                    bar = uraniumProgress;
                    label = Mathf.FloorToInt(uraniumProgress * 100f) + "%";
                    break;
                default:
                    line = UraniumSpikeSoon ? "LONJAKAN NEUTRON!"
                        : uraniumR > UraniumMath.GreenMax ? "TERLALU PANAS · MIRINGKAN HP KE DEPAN"
                        : uraniumR < UraniumMath.GreenMin ? "REAKSI MELEMAH · MIRINGKAN KE BELAKANG"
                        : "STABIL · TAHAN HP";
                    warn = !UraniumMath.InGreen(uraniumR) || UraniumSpikeSoon;
                    bar = uraniumProgress;
                    label = Mathf.FloorToInt(uraniumProgress * 100f) + "%";
                    break;
            }
            EnerGoUI.DrawCard(new Rect(x, y, w, 64f), EnerGoUI.Radius, warn ? HudWarn : HudOk, 2f);
            GUI.Label(new Rect(x + 10f, y + 8f, w - 20f, 20f), line, warn ? styleRadarAlert : styleRadarLocked);
            DrawHudBar(x + 20f, y + 36f, w - 40f, bar, warn ? HudWarn : HudOk, label);
        }

        // Secure phase: reactor gauge (right), control rod (left), dose bar (bottom).
        private void DrawUraniumOverlay(float sw, float sh)
        {
            if (uraniumPhase == UraniumPhase.Search) return;
            float gw = 30f, gh = sh * 0.38f, gy = sh * 0.32f;

            var g = new Rect(sw - 16f - gw, gy, gw, gh);
            bool blink = UraniumSpikeSoon && Mathf.Repeat(Time.time * 8f, 1f) < 0.5f;
            EnerGoUI.DrawPanel(g, HudFill, blink ? HudWarn : HudOutline, 8f, 2f);
            float Y(float v) => g.yMax - 4f - v * (gh - 8f);
            EnerGoUI.DrawPanel(new Rect(g.x + 4f, Y(UraniumMath.GreenMax), gw - 8f, Y(UraniumMath.GreenMin) - Y(UraniumMath.GreenMax)),
                EnerGoUI.Alpha(EnerGoUI.Success, 0.35f), Color.clear, 4f);
            EnerGoUI.DrawPanel(new Rect(g.x + 4f, Y(1f), gw - 8f, Y(UraniumMath.HotAbove) - Y(1f)),
                EnerGoUI.Alpha(EnerGoUI.Danger, 0.3f), Color.clear, 4f);
            float my = Y(uraniumR);
            EnerGoUI.DrawPanel(new Rect(g.x - 4f, my - 3f, gw + 8f, 6f), UraniumMath.InGreen(uraniumR) ? EnerGoUI.Success : EnerGoUI.Orange, Color.clear, 3f);
            GUI.Label(new Rect(g.x - 40f, g.yMax + 4f, gw + 48f, 18f), "REAKTOR", styleTapsCounter);

            var rod = new Rect(16f, gy, gw, gh);
            EnerGoUI.DrawPanel(rod, HudFill, HudOutline, 8f, 2f);
            EnerGoUI.DrawPanel(new Rect(rod.x + 8f, rod.y + 4f, gw - 16f, (gh - 8f) * uraniumRod), EnerGoUI.Ink, Color.clear, 4f);
            GUI.Label(new Rect(rod.x - 8f, rod.yMax + 4f, gw + 48f, 18f), "BATANG", styleTapsCounter);

            float dw = Mathf.Min(sw - 64f, 320f);
            var dose = new Rect((sw - dw) * 0.5f, sh - 104f, dw, 34f);
            bool high = uraniumDose > 0.75f;
            EnerGoUI.DrawCard(dose, EnerGoUI.Radius, high ? HudWarn : HudOutline, 2f);
            GUI.Label(new Rect(dose.x + 10f, dose.y + 8f, 60f, 18f), "DOSIS", styleTapsCounter);
            EnerGoUI.DrawBar(new Rect(dose.x + 70f, dose.y + 11f, dw - 84f, 12f), uraniumDose, high ? EnerGoUI.Danger : EnerGoUI.Orange);
        }
    }
}
```

- [ ] **Step 4: Unlock uranium.** In `ResourceIds.cs`, replace the `IsPlayable` line and its comment with:

```csharp
        // Has a capture mini-game + quiz bank. Oil is shown locked until its mini-game exists.
        public static bool IsPlayable(string id) => id == Sun || id == Coal || id == Gold || id == Uranium;
```

Also change the `Uranium` constant's comment to `// rarest`.

- [ ] **Step 5: Compile and run the EditMode tests.** Expected: no compile errors, 62/62 pass.

- [ ] **Step 6: Smoke-check the flow by reflection.** Play mode does not tick under MCP, so drive the methods directly.
  1. Back up the save files.
  2. Set `ResourceIds.Selected = "uranium"` and load `ARPrototype` in play mode.
  3. Get the `ARCapturePrototype` instance and call `SpawnUranium`.
  4. Assert `uraniumPhase == Search` and `target.activeSelf == false`. The "no PUTAR KE arrow" check is done visually in Task 9 Step 2.
  5. Call `BeginUraniumSecure`, set `uraniumProgress = 1f`, and call `StepUraniumSecure(0.016f)`. Assert `uraniumPhase == Done` and `finishAt > 0`.
  6. Call `CompleteCapture()`. Assert it returns `true`, and that `EnerGoProgress.Current.pendingCapture.resourceId == "uranium"` with 3 `questionIds` starting with `"uranium-"`.
  7. Exit play mode and restore the save files (see the MCP recipe in memory: back up `energo-demo-v1.json*` from `Application.persistentDataPath`).

  Expected: every assert holds. Then take a `ScreenCapture.CaptureScreenshot` of the secure overlay to eyeball the gauge layout at 1080x1920.

- [ ] **Step 7: Commit**

```bash
git add Assets/EnerGo/Scripts/ARCapturePrototype.cs Assets/EnerGo/Scripts/ARCapturePrototype.Uranium.cs Assets/EnerGo/Scripts/ARCapturePrototype.Uranium.cs.meta Assets/EnerGo/Scripts/Core/ResourceIds.cs Assets/EnerGo/Tests/Editor/ResourceIdsTests.cs
git commit -m "feat: uranium Pemburu Geiger mini-game (Geiger search, control rod, dose)

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 7: Oil mini-game "Bor & Pompa"

**Files:**
- Create: `Assets/EnerGo/Scripts/ARCapturePrototype.Oil.cs`
- Modify: `Assets/EnerGo/Scripts/ARCapturePrototype.cs` (hooks listed in Step 2)
- Modify: `Assets/EnerGo/Scripts/Core/ResourceIds.cs`
- Modify: `Assets/EnerGo/Tests/Editor/ResourceIdsTests.cs`

**Interfaces:**
- Consumes:
  - `OilMath.*` (Task 4)
  - `Haptics.Pulse`, `SFXManager.PlayPumpBeat` (Task 5)
  - `UnlitColor(Color)` (Task 6)
  - existing members: `PointerScreen()`, `HudScale`, `DrawHudBar`, `target`, `arCamera`, `feedback`, `finishAt`, `FinishDelay`, `capturePerformance`, `nextSpawnTime`, plus the styles and colours listed in Task 6
- Produces:
  - `enum OilPhase { None, Drill, Pump, Done, Failed }` and the field `oilPhase`
  - `SpawnOil()`, `UpdateOil()`, `DrawOilHud(float x, float y, float w)`, `DrawOilOverlay(float sw, float sh)`
  - `ResourceIds.IsPlayable("oil") == true`

- [ ] **Step 1: Update the ResourceIds test.** In `PlayableResources`, change the oil line to `Assert.IsTrue(ResourceIds.IsPlayable(ResourceIds.Oil));`. Run the tests. Expected: FAIL on the oil assert.

- [ ] **Step 2: Add the hooks in `ARCapturePrototype.cs`:**
  1. In `Update`, after the uranium line from Task 6, add:
     ```csharp
                if (resourceId == ResourceIds.Oil && EnerGoProgress.Current.pendingCapture == null) UpdateOil();
     ```
  2. In `CompleteCapture`, after `uraniumPhase = UraniumPhase.None;` add `oilPhase = OilPhase.None;`.
  3. In `CheckTapOnTarget`, after the uranium line add:
     ```csharp
            if (resourceId == ResourceIds.Oil) return false;     // drilled and pumped by holding, not tapping
     ```
  4. In `SpawnGold`, after the uranium line add:
     ```csharp
            if (resourceId == ResourceIds.Oil) { SpawnOil(); return; }
     ```
  5. In `OnGUI`, after the uranium overlay line add:
     ```csharp
            if (oilPhase != OilPhase.None && EnerGoProgress.Current.pendingCapture == null) DrawOilOverlay(sw, sh);
     ```
  6. In `DrawRadarHUD`, after the uranium early return add:
     ```csharp
            if (oilPhase != OilPhase.None) { DrawOilHud(hudX, hudY, hudW); return; }
     ```
  7. In `MiniGameName`, insert `id == ResourceIds.Oil ? "Bor & Pompa" :` before the final `"Dulang"`.

- [ ] **Step 3: Create** `Assets/EnerGo/Scripts/ARCapturePrototype.Oil.cs`

```csharp
using UnityEngine;
using UnityEngine.InputSystem;

namespace EnerGo
{
    // Oil "Bor & Pompa": hold to drill down through the cross-section and stop inside the thin oil band
    // (the bit has momentum; rushing through the gas pocket blows out), then press and release in rhythm
    // to pump the drum full without building up too much pressure.
    public partial class ARCapturePrototype
    {
        private enum OilPhase { None, Drill, Pump, Done, Failed }

        private static readonly Color[] OilLayerColors =
        {
            new Color(0.48f, 0.31f, 0.18f), // tanah
            new Color(0.44f, 0.44f, 0.46f), // batuan
            new Color(0.27f, 0.27f, 0.31f), // batuan penutup
            new Color(0.56f, 0.48f, 0.71f), // kantong gas
            new Color(0.17f, 0.11f, 0.08f), // minyak: hitam kecokelatan
            new Color(0.18f, 0.43f, 0.58f)  // air asin
        };
        private static readonly string[] OilLayerNames = { "Tanah", "Batuan", "Batuan Penutup", "Kantong Gas", "Minyak", "Air Asin" };

        private OilPhase oilPhase = OilPhase.None;
        private float[] oilLayers;
        private float oilDepth, oilSpeed, oilGasPressure;
        private float oilDrum, oilPumpPressure, oilLastStrokeAt = float.NegativeInfinity;
        private int oilStrokes, oilPerfects;
        private OilMath.Stroke oilLastJudge;
        private float oilLastJudgeAt = -10f, oilLastRumbleAt = -10f;
        private bool oilWasHeld, oilPumping;
        private float oilStartedAt, oilPhaseAt;
        private string oilFailText = "";
        private Transform oilBeam;
        private GUIStyle oilLayerStyle;

        private OilMath.Layer OilLayerNow => OilMath.LayerAt(oilDepth, oilLayers);

        private void SpawnOil()
        {
            // ponytail: primitive pump jack; swap for Resources/Oil/pumpjack when a real model exists.
            target = new GameObject("Target Minyak Bumi");
            var ink = UnlitColor(EnerGoUI.Ink);
            var orange = UnlitColor(EnerGoUI.Orange);
            OilPart(PrimitiveType.Cube, target.transform, new Vector3(0f, -0.45f, 0f), new Vector3(1.3f, 0.1f, 0.5f), ink);   // base
            OilPart(PrimitiveType.Cube, target.transform, new Vector3(0f, -0.1f, 0f), new Vector3(0.1f, 0.7f, 0.1f), ink);    // post
            OilPart(PrimitiveType.Cylinder, target.transform, new Vector3(0.68f, -0.3f, 0f), new Vector3(0.05f, 0.15f, 0.05f), ink); // well rod
            oilBeam = new GameObject("Beam").transform;
            oilBeam.SetParent(target.transform, false);
            oilBeam.localPosition = new Vector3(0f, 0.25f, 0f);
            OilPart(PrimitiveType.Cube, oilBeam, Vector3.zero, new Vector3(1.4f, 0.08f, 0.12f), orange);                     // walking beam
            OilPart(PrimitiveType.Cube, oilBeam, new Vector3(0.7f, -0.08f, 0f), new Vector3(0.12f, 0.3f, 0.16f), orange);     // horse head
            target.transform.localScale = Vector3.one * 0.35f;

            Vector3 fwd = arCamera.transform.forward;
            fwd.y = 0f;
            if (fwd.sqrMagnitude < 0.01f) fwd = Vector3.forward;
            fwd.Normalize();
            target.transform.position = arCamera.transform.position + fwd * 1.4f + Vector3.down * 0.25f;
            target.transform.rotation = Quaternion.LookRotation(fwd, Vector3.up); // beam runs left-right, seen side-on

            oilLayers = OilMath.Layers(Random.Range(int.MinValue, int.MaxValue));
            oilPhase = OilPhase.Drill;
            oilDepth = oilSpeed = oilGasPressure = 0f;
            oilDrum = oilPumpPressure = 0f;
            oilLastStrokeAt = float.NegativeInfinity;
            oilStrokes = oilPerfects = 0;
            oilWasHeld = true; // a finger still down from the previous screen must not count as a stroke
            oilPumping = false;
            oilStartedAt = Time.time;
            oilFailText = "";
            feedback = "TAHAN LAYAR UNTUK MENGEBOR";
        }

        private Transform OilPart(PrimitiveType type, Transform parent, Vector3 pos, Vector3 scale, Material mat)
        {
            var go = GameObject.CreatePrimitive(type);
            Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            return go.transform;
        }

        // Finger (or mouse / Space in the editor) held anywhere except the top and bottom HUD bars.
        private bool OilHeld()
        {
            if (Keyboard.current != null && Keyboard.current.spaceKey.isPressed) return true;
            Vector2? p = PointerScreen();
            if (p == null) return false;
            float guiY = (Screen.height - p.Value.y) / HudScale;
            return guiY >= 128f && guiY <= Screen.height / HudScale - 70f;
        }

        private void UpdateOil()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            bool held = OilHeld();
            switch (oilPhase)
            {
                case OilPhase.Drill:
                {
                    OilMath.StepDrill(ref oilDepth, ref oilSpeed, held, OilMath.SpeedMul(OilLayerNow), dt);
                    var layer = OilLayerNow;
                    oilGasPressure = OilMath.StepGasPressure(oilGasPressure, oilSpeed, layer == OilMath.Layer.Gas, dt);
                    if (held && Time.time - oilLastRumbleAt > 0.25f) { Haptics.Pulse(8); oilLastRumbleAt = Time.time; }
                    switch (OilMath.CheckDrill(layer, oilSpeed, held, oilGasPressure))
                    {
                        case OilMath.DrillOutcome.Blowout: FailOil("BLOWOUT!"); break;
                        case OilMath.DrillOutcome.HitWater: FailOil("KENA AIR!"); break;
                        case OilMath.DrillOutcome.FoundOil: BeginOilPump(); break;
                    }
                    break;
                }
                case OilPhase.Pump:
                    if (held && !oilWasHeld) OilStroke();
                    OilMath.StepPumpIdle(ref oilDrum, ref oilPumpPressure, Time.time - oilLastStrokeAt, dt);
                    if (oilBeam != null)
                        oilBeam.localRotation = Quaternion.Slerp(oilBeam.localRotation, Quaternion.Euler(0f, 0f, held ? -14f : 14f), 1f - Mathf.Exp(-12f * dt));
                    if (oilPumpPressure >= 1f) FailOil("OVERPRESSURE!");
                    else if (oilDrum >= 1f)
                    {
                        oilPhase = OilPhase.Done;
                        oilPhaseAt = Time.time;
                        capturePerformance = OilMath.Performance(Time.time - oilStartedAt, oilPerfects, oilStrokes);
                        finishAt = Time.time + FinishDelay;
                        feedback = "DRUM MINYAK PENUH";
                    }
                    break;
                case OilPhase.Failed:
                    if (Time.time - oilPhaseAt >= OilMath.FailShowSec) RetryOil();
                    break;
            }
            oilWasHeld = held;
        }

        private void BeginOilPump()
        {
            oilPhase = OilPhase.Pump;
            oilPhaseAt = Time.time;
            oilPumping = true;
            Haptics.Pulse(80);
            feedback = "MINYAK DITEMUKAN! POMPA SESUAI IRAMA";
        }

        private void OilStroke()
        {
            var judge = OilMath.JudgeStroke(Time.time - oilLastStrokeAt, OilMath.BeatFor(oilDrum));
            OilMath.ApplyStroke(judge, ref oilDrum, ref oilPumpPressure);
            oilStrokes++;
            if (judge == OilMath.Stroke.Perfect) oilPerfects++;
            oilLastStrokeAt = oilLastJudgeAt = Time.time;
            oilLastJudge = judge;
            if (SFXManager.Instance != null) SFXManager.Instance.PlayPumpBeat();
            Haptics.Pulse(judge == OilMath.Stroke.Perfect ? 30 : 15);
        }

        private void FailOil(string text)
        {
            oilPhase = OilPhase.Failed;
            oilPhaseAt = Time.time;
            oilFailText = text;
            feedback = text;
            Haptics.Pulse(300);
            if (SFXManager.Instance != null) SFXManager.Instance.PlayQuizSalah();
        }

        // After a failure: a new well with freshly shuffled layers.
        private void RetryOil()
        {
            oilPhase = OilPhase.None;
            if (target != null) Destroy(target);
            target = null;
            nextSpawnTime = Time.time + 0.3f;
            feedback = "CARI TITIK BOR BARU";
        }

        private void DrawOilHud(float x, float y, float w)
        {
            string line;
            bool warn;
            float bar;
            string label;
            if (oilPhase == OilPhase.Failed)
            {
                line = oilFailText; warn = true; bar = oilPumping ? oilDrum : oilDepth;
                label = oilPumping ? Mathf.FloorToInt(oilDrum * 100f) + "%" : Mathf.RoundToInt(oilDepth * OilMath.DisplayDepthM) + "m";
            }
            else if (oilPhase == OilPhase.Done)
            {
                line = "DRUM MINYAK PENUH!"; warn = false; bar = 1f; label = "100%";
            }
            else if (oilPhase == OilPhase.Pump)
            {
                bool recent = Time.time - oilLastJudgeAt < 0.6f;
                line = !recent ? "TEKAN SAAT LINGKARAN PAS"
                    : oilLastJudge == OilMath.Stroke.Perfect ? "PAS! TERUS POMPA"
                    : oilLastJudge == OilMath.Stroke.Early ? "TERLALU CEPAT! TEKANAN NAIK" : "TERLAMBAT · IKUTI IRAMA";
                warn = oilPumpPressure > 0.6f || (recent && oilLastJudge == OilMath.Stroke.Early);
                bar = oilDrum;
                label = Mathf.FloorToInt(oilDrum * 100f) + "%";
            }
            else
            {
                var layer = OilLayerNow;
                bool gas = layer == OilMath.Layer.Gas;
                line = gas ? "KANTONG GAS! PELAN-PELAN"
                    : layer == OilMath.Layer.Oil ? "LAPISAN MINYAK! LEPAS SEKARANG"
                    : "TAHAN UNTUK MENGEBOR · " + OilLayerNames[(int)layer].ToUpperInvariant();
                warn = gas && oilGasPressure > 0.4f;
                bar = oilDepth;
                label = Mathf.RoundToInt(oilDepth * OilMath.DisplayDepthM) + "m";
            }
            EnerGoUI.DrawCard(new Rect(x, y, w, 64f), EnerGoUI.Radius, warn ? HudWarn : HudOk, 2f);
            GUI.Label(new Rect(x + 10f, y + 8f, w - 20f, 20f), line, warn ? styleRadarAlert : styleRadarLocked);
            DrawHudBar(x + 20f, y + 36f, w - 40f, bar, warn ? HudWarn : HudOk, label);
        }

        private void DrawOilOverlay(float sw, float sh)
        {
            if (oilLayerStyle == null)
                oilLayerStyle = new GUIStyle(styleTapsCounter) { alignment = TextAnchor.MiddleLeft, normal = { textColor = Color.white } };
            if (oilPumping) DrawOilPump(sw, sh);
            else DrawOilSection(sw, sh);
        }

        // Cross-section: coloured layers, the drill string down to the bit, and the gas pressure gauge on the right.
        private void DrawOilSection(float sw, float sh)
        {
            float pw = Mathf.Min(sw - 72f, 300f), ph = Mathf.Min(sh * 0.48f, 420f);
            var p = new Rect((sw - pw) * 0.5f, sh * 0.3f, pw, ph);
            EnerGoUI.DrawCard(new Rect(p.x - 6f, p.y - 6f, pw + 12f, ph + 12f));
            float prev = 0f;
            for (int i = 0; i < OilLayerColors.Length; i++)
            {
                float bottom = i < oilLayers.Length ? oilLayers[i] : 1f;
                var band = new Rect(p.x, p.y + prev * ph, pw, (bottom - prev) * ph);
                EnerGoUI.DrawPanel(band, OilLayerColors[i], Color.clear, 0f);
                if (band.height >= 14f) GUI.Label(new Rect(band.x + 8f, band.y, pw * 0.5f, band.height), OilLayerNames[i], oilLayerStyle);
                prev = bottom;
            }
            float cx = p.x + pw * 0.68f, tip = p.y + oilDepth * ph;
            EnerGoUI.DrawPanel(new Rect(cx - 3f, p.y - 10f, 6f, tip - p.y + 10f), EnerGoUI.Paper, EnerGoUI.Ink, 2f, 1f);
            EnerGoUI.DrawPanel(new Rect(cx - 7f, tip - 7f, 14f, 14f), EnerGoUI.Orange, EnerGoUI.Ink, 4f, 1.5f);

            var gauge = new Rect(p.xMax + 14f, p.y, 12f, ph);
            EnerGoUI.DrawPanel(gauge, HudFill, HudOutline, 6f, 1.5f);
            float gh = (ph - 4f) * oilGasPressure;
            EnerGoUI.DrawPanel(new Rect(gauge.x + 2f, gauge.yMax - 2f - gh, 8f, gh), oilGasPressure > 0.6f ? EnerGoUI.Danger : EnerGoUI.Orange, Color.clear, 4f);
        }

        // Pump: a ring shrinks from the last stroke toward the target circle; press when they meet.
        private void DrawOilPump(float sw, float sh)
        {
            float beat = OilMath.BeatFor(oilDrum);
            var c = new Vector2(sw * 0.5f, sh * 0.6f);
            const float r0 = 44f;
            Rect Circle(float r) => new Rect(c.x - r, c.y - r, r * 2f, r * 2f);
            EnerGoUI.DrawPanel(Circle(r0), EnerGoUI.Alpha(EnerGoUI.Orange, 0.18f), EnerGoUI.Orange, r0, 3f);
            if (float.IsNegativeInfinity(oilLastStrokeAt))
                GUI.Label(new Rect(c.x - 120f, c.y + r0 + 10f, 240f, 20f), "TEKAN UNTUK MULAI MEMOMPA", styleTapsCounter);
            else
            {
                float t = Mathf.Clamp01((Time.time - oilLastStrokeAt) / beat);
                float r = Mathf.Lerp(r0 * 2.6f, r0, t);
                EnerGoUI.DrawPanel(Circle(r), Color.clear, EnerGoUI.Alpha(EnerGoUI.Ink, 0.85f), r, 3f);
            }
            GUI.Label(new Rect(c.x - 40f, c.y - 10f, 80f, 20f), "POMPA", styleTapsCounter);

            float dw = Mathf.Min(sw - 64f, 320f);
            var pr = new Rect((sw - dw) * 0.5f, sh - 104f, dw, 34f);
            bool high = oilPumpPressure > 0.6f;
            EnerGoUI.DrawCard(pr, EnerGoUI.Radius, high ? HudWarn : HudOutline, 2f);
            GUI.Label(new Rect(pr.x + 10f, pr.y + 8f, 70f, 18f), "TEKANAN", styleTapsCounter);
            EnerGoUI.DrawBar(new Rect(pr.x + 80f, pr.y + 11f, dw - 94f, 12f), oilPumpPressure, high ? EnerGoUI.Danger : EnerGoUI.Orange);
        }
    }
}
```

- [ ] **Step 4: Unlock oil.** In `ResourceIds.cs`, replace the `IsPlayable` line and its comment with:

```csharp
        // Has a capture mini-game + quiz bank.
        public static bool IsPlayable(string id) => id == Sun || id == Coal || id == Gold || id == Oil || id == Uranium;
```

Also change the `Oil` constant's comment from `// spawns on the radar, no mini-game yet` to `// "Bor & Pompa"`. Update the comment above `DrawCollectionCard` in `LobbyController.cs` (line 307) to `// Lobby collection tile: crystal, name, unit count; resources without a mini-game show a lock.`

- [ ] **Step 5: Compile and run the EditMode tests.** Expected: no compile errors, 62/62 pass.

- [ ] **Step 6: Smoke-check by reflection.** Use the same save backup and restore as Task 6 Step 6.
  1. Set `ResourceIds.Selected = "oil"` and load `ARPrototype` in play mode.
  2. Call `SpawnOil`. Assert `oilPhase == Drill`, `oilLayers.Length == 5`, and that `target` has 6 renderers.
  3. Test the oil stop. Set `oilDepth` to the middle of the oil band, `(oilLayers[3] + oilLayers[4]) / 2`, and set `oilSpeed = 0`.
     - Assert `OilMath.CheckDrill(OilMath.LayerAt(oilDepth, oilLayers), 0f, false, 0f) == FoundOil`.
     - Invoke `BeginOilPump`, then assert `oilPhase == Pump` and `oilPumping == true`.
  4. Finish the capture. `Time.deltaTime` is 0 under MCP, so `UpdateOil` would not advance. Instead, set `oilDrum = 1f` and `oilPhase = Done`, then call `CompleteCapture()` and assert it returns `true`.
  5. Assert `pendingCapture.resourceId == "oil"` with `oil-` question ids. Restore the save files.
  6. Screenshot both the cross-section and the pump ring with `ScreenCapture`.

- [ ] **Step 7: Commit**

```bash
git add Assets/EnerGo/Scripts/ARCapturePrototype.cs Assets/EnerGo/Scripts/ARCapturePrototype.Oil.cs Assets/EnerGo/Scripts/ARCapturePrototype.Oil.cs.meta Assets/EnerGo/Scripts/Core/ResourceIds.cs Assets/EnerGo/Tests/Editor/ResourceIdsTests.cs Assets/EnerGo/Scripts/LobbyController.cs
git commit -m "feat: oil Bor & Pompa mini-game (layered drilling with momentum, rhythm pump)

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 8: Lobby practice picker with all five resources

**Files:**
- Modify: `Assets/EnerGo/Scripts/LobbyController.cs`. Line 55 is `AllResources`, lines 74–76 are the pref validation, and lines 469–508 are `DrawPractice`.

**Interfaces:**
- Consumes: `ResourceIds.IsPlayable` (Tasks 6 and 7).

- [ ] **Step 1: Write the verification script.** Save it as `<scratchpad>/verify_lobby_pref.cs`. It checks that a saved `oil` pick survives the lobby start-up.

```csharp
using System.Reflection;
using UnityEngine;
using EnerGo;

public class Script
{
    public static object Main()
    {
        string old = PlayerPrefs.GetString("EnerGo.SelectedResource", "");
        try
        {
            PlayerPrefs.SetString("EnerGo.SelectedResource", ResourceIds.Oil);
            var go = new GameObject("LobbyProbe");
            go.SetActive(false);
            var lobby = go.AddComponent<LobbyController>();
            var start = typeof(LobbyController).GetMethod("Start", BindingFlags.Instance | BindingFlags.NonPublic)
                     ?? typeof(LobbyController).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic);
            start.Invoke(lobby, null);
            var sel = (string)typeof(LobbyController).GetField("selectedResource", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(lobby);
            var all = (string[])typeof(LobbyController).GetField("AllResources", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            Object.DestroyImmediate(go);
            return $"selected={sel} pool={string.Join(",", all)}";
        }
        finally { PlayerPrefs.SetString("EnerGo.SelectedResource", old); PlayerPrefs.Save(); }
    }
}
```

Before running it, check which method holds the pref-loading code around line 74 (`Start` or `Awake`), and keep the lookup order above so the script finds it. If invoking it throws on unrelated scene setup, do this instead: set the pref to `oil`, enter play mode in `LobbyScene`, and read `selectedResource` and `AllResources` by reflection. Run the script. Expected before the change: `selected=gold pool=sun,coal,gold`.

- [ ] **Step 2: Implement the lobby changes:**
  1. Line 55: `private static readonly string[] AllResources = { ResourceIds.Sun, ResourceIds.Coal, ResourceIds.Gold, ResourceIds.Oil, ResourceIds.Uranium };`
  2. Lines 75–76:
     ```csharp
            selectedResource = ResourceIds.IsPlayable(savedResource) || savedResource == RandomResource ? savedResource : ResourceIds.Gold;
     ```
  3. In `DrawPractice`:
     - Change the comment above it to `// Practice: pick any SDA (or Acak) and go straight into its mini-game, no GPS.`
     - Change `float sheetH = 32f + 46f + cardH * 2f + gap + 20f + 66f + 12f + SafeBottom(scale);` to `float sheetH = 32f + 46f + cardH * 3f + gap * 2f + 20f + 66f + 12f + SafeBottom(scale);`
     - Change `string[] ids = { ResourceIds.Sun, ResourceIds.Coal, ResourceIds.Gold, RandomResource };` to `string[] ids = { ResourceIds.Sun, ResourceIds.Coal, ResourceIds.Gold, ResourceIds.Oil, ResourceIds.Uranium, RandomResource };`
     - Change `y += cardH * 2f + gap + 20f;` to `y += cardH * 3f + gap * 2f + 20f;`

- [ ] **Step 3: Compile, then rerun the script.** Expected: `selected=oil pool=sun,coal,gold,oil,uranium`.

- [ ] **Step 4: Screenshot the practice sheet.** Use the `ScreenCapture` recipe at 1080x1920, with the lobby in play mode and the practice sheet open. Check that 6 cards sit in 3 rows, "MULAI LATIHAN" is fully visible above the safe area, and the oil/uranium cards show crystal icons and a "0 unit" count.

- [ ] **Step 5: Run the EditMode tests.** Expected: 62/62 pass.

- [ ] **Step 6: Commit**

```bash
git add Assets/EnerGo/Scripts/LobbyController.cs
git commit -m "feat: practice sheet and random pick include oil and uranium

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 9: End-to-end check and APK

**Files:** none new (fixes only if something fails).

- [ ] **Step 1: Run the full EditMode suite.** Expected: 62/62 pass.

- [ ] **Step 2: Check robustness by reflection** (Review Focus 2 and 3). Use the same save backup and restore as before. For each of uranium and oil:
  1. Spawn the mini-game and advance it to the second phase: uranium via `BeginUraniumSecure`, oil via `BeginOilPump`.
  2. Simulate the reset button: `Destroy(target); target = null;`, then call `SpawnGold(false)`.
  3. Assert:
     - the phase is back to `Search` / `Drill`
     - uranium: `uraniumProgress == 0`, `uraniumDose == 0`, `target.activeSelf == false`
     - oil: `oilPumping == false`, `oilDepth == 0`
  4. Also, during the uranium `Search` phase, render one `OnGUI` frame (screenshot) and confirm no "PUTAR KE" text appears.

- [ ] **Step 3: Build the APK** with the editor script recipe.
  1. Call `BuildPipeline.BuildPlayer` with the scenes `LobbyScene, ExploreScene, ARPrototype, Sensor360Scene`, output `Builds/EnerGo-MinyakUranium.apk`, target Android.
  2. Schedule it with `EditorApplication.delayCall`, and write `report.summary.result` to `Builds/minyak-uranium-result.txt`.

  Expected: `Succeeded` with 0 errors. Check that the built manifest contains `android.permission.VIBRATE`, for example with `aapt dump permissions` if available. Otherwise, confirm the vibration on the device.

- [ ] **Step 4: Hand over for the device test.** Tell the user what to try on the phone:
  - **Uranium:**
    - Find the ore by sound and vibration alone.
    - Tilting forward cools the reactor.
    - Spikes are survivable.
    - The dose budget feels fair.
  - **Oil:**
    - The cap rock is slow.
    - Gas punishes speed.
    - Stopping in the oil band is hard but doable.
    - The pump rhythm reads clearly.
  - **Old save:** counts are kept after installing over the previous APK.

  Tuning constants are at the top of `UraniumMath.cs` and `OilMath.cs`.

- [ ] **Step 5: Commit any fixes** from Steps 1–3 with explicit paths. If nothing changed, there is nothing to commit.
