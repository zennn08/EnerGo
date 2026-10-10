# Radar Eksplorasi Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** A GPS exploration screen where players walk to resource spawns (SDA) shown on a heading-up radar. The spawns are deterministic and rarity-weighted. Capturing one opens the existing AR mini-game and quiz.

**Architecture:**
- **`EnerGo.Core` assembly (new, pure logic):** `ResourceIds` moves here, joined by a deterministic `GeoSpawner` (cells, 15-minute windows, splitmix64, geo math) and a tiny `ExploreSession` hand-off. It gets EditMode tests.
- **`ExploreScene` (new):** holds `LocationFeed` (device GPS and compass, or an editor WASD sim) and `RadarController` (IMGUI drawn with `EnerGoUI`). TANGKAP loads the existing capture scene.
- **Existing files:** `EnerGoProgress` records `spawnId`/`capturedSpawns` in the same commit as the reward. `LobbyController` and `ARCapturePrototype` are rewired for the new flow.

**Tech Stack:** Unity 6000.3, C#, IMGUI, legacy `Input.location`/`Input.compass`, Unity Test Framework 1.6 (NUnit), Unity MCP CLI (`npx -y unity-mcp-cli`).

**Spec:** `docs/superpowers/specs/2026-10-10-radar-eksplorasi-design.md`

## Global Constraints

- **Unity:** 6000.3.24f1. Android only. No new packages or plugins.
- **Spawn determinism:** `GeoSpawner` must NOT use `System.Random` or `UnityEngine.Random`. Use only its own splitmix64.
- **Spawn table:** order is `sun, coal, gold, oil, uranium` with weights `35, 30, 20, 12, 3`. It is append-only: never reorder, because reordering changes every spawn.
- **Grid and timing:**
  - Cell size: `0.0005°`.
  - Window: `900` s.
  - Count per cell: 0 at 60%, 1 at 30%, 2 at 10%.
  - Radar range: `150` m.
  - Capture rule: `distance − min(accuracy, 15) ≤ 20` m.
  - Implausible jump: more than `50` m/s.
- **Daytime:** local hour `06:00–17:59`. Outside that, sun's weight is removed and the rest are renormalised.
- **Spawn id format:** `"{cellX}:{cellY}:{window}:{index}"`.
- **Resources:** `oil` and `uranium` spawn but are locked (`ResourceIds.IsPlayable` returns false). Inventory stays at 3 entries.
- **UI:**
  - Indonesian copy.
  - Solar Clean tokens from `EnerGoUI`. No new colour literals in UI code except resource colours added to `EnerGoUI.Resource`.
  - Poppins lacks emoji and most arrows. Use only ASCII plus `‹ ›` and `·` in labels.
- **Commits:** commit only the files a task lists. Never `git add -A`, because the working tree has unrelated changes.

## Review Focus

1. **Cell boundaries and negative coordinates.** At a cell edge, or at Indonesia's negative latitude (−6.2), `Nearby` must still return spawns from the neighbouring cells within range. Test: Task 3, "Nearby matches brute force at negative latitude".
2. **Capture finishing after a window rollover.** The spawn id is from window W−1, but it must stay excluded while that window is still visible, and the prune must keep it. Test: Task 3, "Prune keeps current and previous window". Check: Task 4, verification script.
3. **App killed mid-quiz after a radar capture.** On relaunch the pending capture resumes, and LANJUT returns to the Radar, not the Lobby, because `spawnId` was persisted. Check: Task 9, Step 3.
4. **Night and hour boundaries.** No sun at 18:00 or later, or before 06:00. Sun is allowed at exactly 06:00. Test: Task 3, "Daytime boundaries".
5. **Bad GPS.** A jump faster than 50 m/s is ignored. Poor accuracy cannot unlock capture by more than 15 m. The compass is smoothed across the 359°→1° wrap without spinning the long way. Tests: Task 2, `CanCapture`, `PlausibleMove` and `SmoothHeading`.

## How to run things (used by every task)

- **Compile:**
  1. `npx -y unity-mcp-cli run-tool assets-refresh --input "{}"`
  2. `npx -y unity-mcp-cli run-tool console-get-logs --input "{\"logTypeFilter\":\"Error\",\"maxEntries\":20}"`
  3. Expected: no `error CS` messages newer than the refresh.
- **EditMode tests:** `npx -y unity-mcp-cli run-tool tests-run --input "{\"testMode\":\"EditMode\",\"testAssembly\":\"EnerGo.Tests.Editor\"}"`
- **Run C# in the editor** (verification scripts):
  - Write a class `public class Script { public static object Main() { ... } }` to a file.
  - Pipe `{"csharpCode": <file text>, "isMethodBody": false}` as JSON into `npx -y unity-mcp-cli run-tool script-execute --input-file -`.
  - The value it returns is printed.
- **The editor does not tick in play mode:** `Time.time` stays at 0. Drive state by calling methods, not by waiting.

---

### Task 0: Pre-flight — commit the pending Solar Clean work (needs the user's OK)

The working tree holds uncommitted, user-reviewed work: the logo, the UI overhaul, the quiz timer, the random coal layout and the bonus units. Tasks 4, 8 and 9 edit the same files. Without this commit, those tasks' commits would mix in the old work.

- [ ] **Step 1: Ask the user.** Ask: "Boleh saya commit dulu perubahan logo, UI, timer kuis dan bonus yang belum di-commit, sebagai satu commit terpisah?" Wait for a yes. If the answer is no, stop and report.
- [ ] **Step 2: Commit.**

```bash
git add Assets/EnerGo/Scripts/EnerGoUI.cs Assets/EnerGo/Scripts/LobbyController.cs Assets/EnerGo/Scripts/ARCapturePrototype.cs Assets/EnerGo/Scripts/EnerGoProgress.cs Assets/EnerGo/Resources/UI Assets/EnerGo/Textures/logo_game.png Assets/EnerGo/Textures/logo_game.png.meta Assets/EnerGo/Textures/AppIcon Assets/EnerGo/Textures/AppIcon.meta ProjectSettings/ProjectSettings.asset Branding
git commit -m "feat: Solar Clean identity, UI overhaul, quiz timer, random coal, mini-game bonus"
```

---

### Task 1: `EnerGo.Core` assembly + `ResourceIds` with oil/uranium

**Files:**
- Create: `Assets/EnerGo/Scripts/Core/EnerGo.Core.asmdef`
- Create: `Assets/EnerGo/Scripts/Core/ResourceIds.cs`
- Create: `Assets/EnerGo/Scripts/Core/ExploreSession.cs`
- Modify: `Assets/EnerGo/Scripts/EnerGoProgress.cs:7-14` (delete the `ResourceIds` class there)
- Create: `Assets/EnerGo/Tests/Editor/EnerGo.Tests.Editor.asmdef`
- Test: `Assets/EnerGo/Tests/Editor/ResourceIdsTests.cs`

**Interfaces:**
- Produces:
  - `ResourceIds.Sun/Coal/Gold/Oil/Uranium` (string constants `"sun","coal","gold","oil","uranium"`).
  - `ResourceIds.Selected` (static string).
  - `ResourceIds.Name(string id)` → `string`.
  - `ResourceIds.IsPlayable(string id)` → `bool`.
  - `ExploreSession.PendingSpawnId` (static string, `null` = practice).
  - Assembly `EnerGo.Core`, which is auto-referenced by Assembly-CSharp.

- [ ] **Step 1: Create the test assembly and a failing test**

`Assets/EnerGo/Tests/Editor/EnerGo.Tests.Editor.asmdef`:

```json
{
    "name": "EnerGo.Tests.Editor",
    "rootNamespace": "EnerGo.Tests",
    "references": ["EnerGo.Core", "UnityEngine.TestRunner", "UnityEditor.TestRunner"],
    "includePlatforms": ["Editor"],
    "overrideReferences": true,
    "precompiledReferences": ["nunit.framework.dll"],
    "autoReferenced": false,
    "defineConstraints": ["UNITY_INCLUDE_TESTS"]
}
```

`Assets/EnerGo/Tests/Editor/ResourceIdsTests.cs`:

```csharp
using NUnit.Framework;

namespace EnerGo.Tests
{
    public class ResourceIdsTests
    {
        [Test]
        public void NewResourcesHaveNames()
        {
            Assert.AreEqual("Minyak Bumi", ResourceIds.Name(ResourceIds.Oil));
            Assert.AreEqual("Uranium", ResourceIds.Name(ResourceIds.Uranium));
            Assert.AreEqual("Matahari", ResourceIds.Name(ResourceIds.Sun));
        }

        [Test]
        public void OnlySunCoalGoldArePlayable()
        {
            Assert.IsTrue(ResourceIds.IsPlayable(ResourceIds.Sun));
            Assert.IsTrue(ResourceIds.IsPlayable(ResourceIds.Coal));
            Assert.IsTrue(ResourceIds.IsPlayable(ResourceIds.Gold));
            Assert.IsFalse(ResourceIds.IsPlayable(ResourceIds.Oil));
            Assert.IsFalse(ResourceIds.IsPlayable(ResourceIds.Uranium));
            Assert.IsFalse(ResourceIds.IsPlayable("random"));
        }
    }
}
```

- [ ] **Step 2: Run the tests and confirm they fail.** Use the compile command. Expected: `error CS0246`/`CS0234`, because `EnerGo.Core` does not exist, or `ResourceIds` is not visible to the test assembly.

- [ ] **Step 3: Create the Core assembly**

`Assets/EnerGo/Scripts/Core/EnerGo.Core.asmdef`:

```json
{
    "name": "EnerGo.Core",
    "rootNamespace": "EnerGo",
    "references": [],
    "autoReferenced": true
}
```

`Assets/EnerGo/Scripts/Core/ResourceIds.cs`:

```csharp
namespace EnerGo
{
    public static class ResourceIds
    {
        public const string Sun = "sun";
        public const string Coal = "coal";
        public const string Gold = "gold";
        public const string Oil = "oil";         // spawns on the radar, no mini-game yet
        public const string Uranium = "uranium"; // rarest; no mini-game yet
        public static string Selected = Gold;

        public static string Name(string id) =>
            id == Sun ? "Matahari" : id == Coal ? "Batu Bara" : id == Oil ? "Minyak Bumi" : id == Uranium ? "Uranium" : "Emas";

        // Has a capture mini-game + quiz bank. Oil and uranium are shown locked until theirs exist.
        public static bool IsPlayable(string id) => id == Sun || id == Coal || id == Gold;
    }
}
```

`Assets/EnerGo/Scripts/Core/ExploreSession.cs`:

```csharp
namespace EnerGo
{
    // Hand-off from the radar to the capture scene: the world spawn being captured (null = practice mode).
    public static class ExploreSession
    {
        public static string PendingSpawnId;
    }
}
```

In `Assets/EnerGo/Scripts/EnerGoProgress.cs`, delete this whole block (currently lines 7–14):

```csharp
    public static class ResourceIds
    {
        public const string Sun = "sun";
        public const string Coal = "coal";
        public const string Gold = "gold";
        public static string Selected = Gold;
        public static string Name(string id) => id == Sun ? "Matahari" : id == Coal ? "Batu Bara" : "Emas";
    }
```

- [ ] **Step 4: Compile and run the tests.** Run the compile command, then the tests command. Expected: no errors, and `ResourceIdsTests` passes 2/2.

- [ ] **Step 5: Commit**

```bash
git add Assets/EnerGo/Scripts/Core Assets/EnerGo/Scripts/Core.meta Assets/EnerGo/Tests Assets/EnerGo/Tests.meta Assets/EnerGo/Scripts/EnerGoProgress.cs
git commit -m "refactor: EnerGo.Core assembly with ResourceIds (+oil, uranium) and ExploreSession"
```

---

### Task 2: `GeoSpawner` geo math (distance, bearing, directions, capture and GPS rules, compass smoothing)

**Files:**
- Create: `Assets/EnerGo/Scripts/Core/GeoSpawner.cs`
- Test: `Assets/EnerGo/Tests/Editor/GeoMathTests.cs`

**Interfaces:**
- Produces (all `public static` on `GeoSpawner`):
  - Constants: `MetersPerDegree` (double), `CaptureRadiusM = 20.0`, `MaxAccuracyBonusM = 15.0`, `MaxPlausibleSpeedMps = 50.0`.
  - `double DistanceM(double lat1, double lon1, double lat2, double lon2)`
  - `double BearingDeg(double lat1, double lon1, double lat2, double lon2)` (0 = north, clockwise, `[0,360)`)
  - `string Direction8(double bearingDeg)` → one of `Utara, Timur Laut, Timur, Tenggara, Selatan, Barat Daya, Barat, Barat Laut`
  - `bool CanCapture(double distanceM, double accuracyM)`
  - `bool PlausibleMove(double meters, double seconds)`
  - `float SmoothHeading(float current, float target, float dt)`

- [ ] **Step 1: Write the failing tests**

`Assets/EnerGo/Tests/Editor/GeoMathTests.cs`:

```csharp
using NUnit.Framework;

namespace EnerGo.Tests
{
    public class GeoMathTests
    {
        const double Lat = -6.2, Lon = 106.8;

        [Test]
        public void DistanceAndBearingEast()
        {
            // 0.001° of longitude at -6.2°: 111194.93 * 0.001 * cos(6.2°) = 110.545 m, due east
            Assert.AreEqual(110.545, GeoSpawner.DistanceM(Lat, Lon, Lat, Lon + 0.001), 0.5);
            Assert.AreEqual(90.0, GeoSpawner.BearingDeg(Lat, Lon, Lat, Lon + 0.001), 1.0);
        }

        [Test]
        public void DistanceAndBearingNorthAndSouthWest()
        {
            Assert.AreEqual(111.195, GeoSpawner.DistanceM(Lat, Lon, Lat + 0.001, Lon), 0.5);
            Assert.AreEqual(0.0, GeoSpawner.BearingDeg(Lat, Lon, Lat + 0.001, Lon), 1.0);
            Assert.AreEqual(225.0, GeoSpawner.BearingDeg(Lat, Lon, Lat - 0.001, Lon - 0.001 / System.Math.Cos(6.2 * System.Math.PI / 180)), 1.0);
        }

        [TestCase(0, "Utara")]
        [TestCase(350, "Utara")]
        [TestCase(45, "Timur Laut")]
        [TestCase(90, "Timur")]
        [TestCase(200, "Selatan")]
        [TestCase(225, "Barat Daya")]
        [TestCase(300, "Barat Laut")]
        public void Direction8Names(double bearing, string expected) => Assert.AreEqual(expected, GeoSpawner.Direction8(bearing));

        [Test]
        public void CaptureRuleCapsTheAccuracyBonus()
        {
            Assert.IsTrue(GeoSpawner.CanCapture(18, 3));   // plainly in range
            Assert.IsTrue(GeoSpawner.CanCapture(30, 12));  // 30 - 12 = 18
            Assert.IsFalse(GeoSpawner.CanCapture(36, 40)); // bonus capped at 15 -> 21
            Assert.IsFalse(GeoSpawner.CanCapture(25, 0));
        }

        [Test]
        public void ImplausibleJumpsAreRejected()
        {
            Assert.IsTrue(GeoSpawner.PlausibleMove(40, 1));
            Assert.IsFalse(GeoSpawner.PlausibleMove(60, 1));
            Assert.IsTrue(GeoSpawner.PlausibleMove(300, 10));
            Assert.IsFalse(GeoSpawner.PlausibleMove(80, 0)); // same timestamp, big jump
        }

        [Test]
        public void HeadingSmoothingTakesTheShortWayAcrossNorth()
        {
            float h = GeoSpawner.SmoothHeading(350f, 10f, 0.05f);
            // moved towards 360/0, not backwards towards 180
            Assert.IsTrue(h > 350f || h < 10f, "got " + h);
            Assert.That(h, Is.InRange(0f, 360f));
        }
    }
}
```

- [ ] **Step 2: Run the tests and confirm they fail.** Use the compile command. Expected: `error CS0103: The name 'GeoSpawner' does not exist`.

- [ ] **Step 3: Implement**

`Assets/EnerGo/Scripts/Core/GeoSpawner.cs`:

```csharp
using System;
using UnityEngine;

namespace EnerGo
{
    // World spawns for the radar plus the small geo helpers it needs. Pure functions only: everything here is
    // deterministic so every phone computes the same SDA for the same place and time without a server.
    public static partial class GeoSpawner
    {
        public const double EarthRadiusM = 6371000.0;
        public static readonly double MetersPerDegree = EarthRadiusM * Math.PI / 180.0;
        public const double CaptureRadiusM = 20.0;
        public const double MaxAccuracyBonusM = 15.0;   // a bad fix can stretch the capture radius by at most this
        public const double MaxPlausibleSpeedMps = 50.0; // faster than this is a GPS glitch, not walking
        private const float HeadingSmoothing = 6f;       // higher = snappier compass

        private static readonly string[] DirectionNames =
            { "Utara", "Timur Laut", "Timur", "Tenggara", "Selatan", "Barat Daya", "Barat", "Barat Laut" };

        // Equirectangular approximation: error well under a centimetre below 1 km.
        public static double DistanceM(double lat1, double lon1, double lat2, double lon2)
        {
            Offset(lat1, lon1, lat2, lon2, out double east, out double north);
            return Math.Sqrt(east * east + north * north);
        }

        public static double BearingDeg(double lat1, double lon1, double lat2, double lon2)
        {
            Offset(lat1, lon1, lat2, lon2, out double east, out double north);
            double deg = Math.Atan2(east, north) * 180.0 / Math.PI;
            return deg < 0 ? deg + 360.0 : deg;
        }

        private static void Offset(double lat1, double lon1, double lat2, double lon2, out double east, out double north)
        {
            double meanLat = (lat1 + lat2) * 0.5 * Math.PI / 180.0;
            east = (lon2 - lon1) * Math.Cos(meanLat) * MetersPerDegree;
            north = (lat2 - lat1) * MetersPerDegree;
        }

        public static string Direction8(double bearingDeg) =>
            DirectionNames[(int)Math.Floor(((bearingDeg % 360 + 360) % 360 + 22.5) / 45.0) % 8];

        public static bool CanCapture(double distanceM, double accuracyM) =>
            distanceM - Math.Min(Math.Max(accuracyM, 0), MaxAccuracyBonusM) <= CaptureRadiusM;

        public static bool PlausibleMove(double meters, double seconds) =>
            seconds > 0 ? meters / seconds <= MaxPlausibleSpeedMps : meters <= MaxPlausibleSpeedMps * 0.2;

        // Low-pass on a circle: 359° -> 1° moves 2°, not 358°.
        public static float SmoothHeading(float current, float target, float dt) =>
            Mathf.Repeat(Mathf.LerpAngle(current, target, 1f - Mathf.Exp(-HeadingSmoothing * dt)), 360f);
    }
}
```

(`partial` lets Task 3 add the spawn half in the same file without reshuffling this one. Both halves stay in this one file.)

- [ ] **Step 4: Compile and run the tests.** Expected: `GeoMathTests` all pass, and `ResourceIdsTests` still passes.

- [ ] **Step 5: Commit**

```bash
git add Assets/EnerGo/Scripts/Core/GeoSpawner.cs Assets/EnerGo/Scripts/Core/GeoSpawner.cs.meta Assets/EnerGo/Tests/Editor/GeoMathTests.cs Assets/EnerGo/Tests/Editor/GeoMathTests.cs.meta
git commit -m "feat: GeoSpawner geo math (distance, bearing, capture and GPS rules)"
```

---

### Task 3: `GeoSpawner` deterministic spawns, `Nearby`, and the captured-id prune

**Files:**
- Modify: `Assets/EnerGo/Scripts/Core/GeoSpawner.cs` (append the second `partial` block below the first, in the same file)
- Test: `Assets/EnerGo/Tests/Editor/GeoSpawnTests.cs`

**Interfaces:**
- Consumes: `DistanceM` and `MetersPerDegree` from Task 2.
- Produces (on `GeoSpawner`):
  - Constants: `CellDeg = 0.0005`, `WindowSeconds = 900L`.
  - `readonly struct Spawn { string Id; string ResourceId; double Lat; double Lon; }`
  - `long CellOf(double deg)`
  - `long WindowOf(long unixUtcSeconds)`
  - `long SecondsToNextWindow(long unixUtcSeconds)`
  - `bool IsDaytime(int localHour)`
  - `List<Spawn> SpawnsInCell(long cellX, long cellY, long window, int localHour)`
  - `List<Spawn> Nearby(double lat, double lon, double radiusM, long unixUtcSeconds, int localHour, ICollection<string> captured)` (sorted nearest first)
  - `string[] PruneCaptured(string[] ids, long window)`

- [ ] **Step 1: Write the failing tests**

`Assets/EnerGo/Tests/Editor/GeoSpawnTests.cs`:

```csharp
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace EnerGo.Tests
{
    public class GeoSpawnTests
    {
        const double Lat = -6.2, Lon = 106.8;
        const long Now = 1_791_600_000; // fixed instant

        [Test]
        public void SameInputSameSpawns()
        {
            var a = GeoSpawner.Nearby(Lat, Lon, 150, Now, 12, null);
            var b = GeoSpawner.Nearby(Lat, Lon, 150, Now, 12, null);
            Assert.AreEqual(a.Count, b.Count);
            for (int i = 0; i < a.Count; i++)
            {
                Assert.AreEqual(a[i].Id, b[i].Id);
                Assert.AreEqual(a[i].ResourceId, b[i].ResourceId);
                Assert.AreEqual(a[i].Lat, b[i].Lat);
                Assert.AreEqual(a[i].Lon, b[i].Lon);
            }
        }

        [Test]
        public void CountAndTypeRatesMatchTheTable()
        {
            var counts = new Dictionary<string, int>();
            int spawns = 0, cells = 0;
            for (long x = -158; x < 158; x++)
            for (long y = -158; y < 158; y++)
            {
                cells++;
                foreach (var s in GeoSpawner.SpawnsInCell(x, y, 42, 12))
                {
                    spawns++;
                    counts[s.ResourceId] = counts.TryGetValue(s.ResourceId, out int n) ? n + 1 : 1;
                }
            }
            Assert.AreEqual(0.5, spawns / (double)cells, 0.01, "mean spawns per cell");
            var expected = new Dictionary<string, double> { ["sun"] = .35, ["coal"] = .30, ["gold"] = .20, ["oil"] = .12, ["uranium"] = .03 };
            foreach (var kv in expected)
                Assert.AreEqual(kv.Value, counts[kv.Key] / (double)spawns, 0.01, kv.Key);
        }

        [Test]
        public void NoSunAtNightAndRestRenormalised()
        {
            var counts = new Dictionary<string, int>();
            int spawns = 0;
            for (long x = 0; x < 300; x++)
            for (long y = 0; y < 300; y++)
                foreach (var s in GeoSpawner.SpawnsInCell(x, y, 7, 20))
                {
                    spawns++;
                    counts[s.ResourceId] = counts.TryGetValue(s.ResourceId, out int n) ? n + 1 : 1;
                }
            Assert.IsFalse(counts.ContainsKey("sun"));
            Assert.AreEqual(0.30 / 0.65, counts["coal"] / (double)spawns, 0.015);
        }

        [Test]
        public void DaytimeBoundaries()
        {
            Assert.IsFalse(GeoSpawner.IsDaytime(5));
            Assert.IsTrue(GeoSpawner.IsDaytime(6));
            Assert.IsTrue(GeoSpawner.IsDaytime(17));
            Assert.IsFalse(GeoSpawner.IsDaytime(18));
        }

        [Test]
        public void DayAndNightShareCountsAndPositions()
        {
            // Only the type may differ between day and night, so a spawn never "jumps" at 18:00.
            var day = GeoSpawner.SpawnsInCell(12, -34, 99, 12);
            var night = GeoSpawner.SpawnsInCell(12, -34, 99, 20);
            Assert.AreEqual(day.Count, night.Count);
            for (int i = 0; i < day.Count; i++) { Assert.AreEqual(day[i].Lat, night[i].Lat); Assert.AreEqual(day[i].Lon, night[i].Lon); }
        }

        [Test]
        public void NearbyMatchesBruteForceAtNegativeLatitude()
        {
            // Points sprinkled around a cell corner: Nearby must see spawns in neighbouring cells too.
            long window = GeoSpawner.WindowOf(Now);
            for (int k = 0; k < 25; k++)
            {
                double lat = Lat + (k % 5) * 0.00011, lon = Lon + (k / 5) * 0.00013;
                var got = GeoSpawner.Nearby(lat, lon, 150, Now, 12, null).Select(s => s.Id).ToList();
                var want = new List<string>();
                long cx = GeoSpawner.CellOf(lon), cy = GeoSpawner.CellOf(lat);
                for (long x = cx - 6; x <= cx + 6; x++)
                for (long y = cy - 6; y <= cy + 6; y++)
                    foreach (var s in GeoSpawner.SpawnsInCell(x, y, window, 12))
                        if (GeoSpawner.DistanceM(lat, lon, s.Lat, s.Lon) <= 150) want.Add(s.Id);
                CollectionAssert.AreEquivalent(want, got, "point " + k);
            }
        }

        [Test]
        public void NearbyIsSortedAndSkipsCaptured()
        {
            var all = GeoSpawner.Nearby(Lat, Lon, 150, Now, 12, null);
            Assert.Greater(all.Count, 2, "fixture needs a few spawns");
            for (int i = 1; i < all.Count; i++)
                Assert.LessOrEqual(GeoSpawner.DistanceM(Lat, Lon, all[i - 1].Lat, all[i - 1].Lon),
                                   GeoSpawner.DistanceM(Lat, Lon, all[i].Lat, all[i].Lon));
            var captured = new HashSet<string> { all[0].Id };
            var rest = GeoSpawner.Nearby(Lat, Lon, 150, Now, 12, captured);
            Assert.AreEqual(all.Count - 1, rest.Count);
            Assert.IsFalse(rest.Any(s => s.Id == all[0].Id));
        }

        [Test]
        public void IdsEncodeCellWindowAndIndex()
        {
            var s = GeoSpawner.Nearby(Lat, Lon, 150, Now, 12, null)[0];
            var parts = s.Id.Split(':');
            Assert.AreEqual(4, parts.Length);
            Assert.AreEqual(GeoSpawner.WindowOf(Now).ToString(), parts[2]);
        }

        [Test]
        public void WindowMath()
        {
            Assert.AreEqual(0, GeoSpawner.WindowOf(899));
            Assert.AreEqual(1, GeoSpawner.WindowOf(900));
            Assert.AreEqual(1, GeoSpawner.SecondsToNextWindow(899));
            Assert.AreEqual(900, GeoSpawner.SecondsToNextWindow(900));
        }

        [Test]
        public void PruneKeepsCurrentAndPreviousWindow()
        {
            string[] ids = { "1:2:100:0", "1:2:99:1", "3:4:98:0", "garbage" };
            CollectionAssert.AreEquivalent(new[] { "1:2:100:0", "1:2:99:1" }, GeoSpawner.PruneCaptured(ids, 100));
            CollectionAssert.IsEmpty(GeoSpawner.PruneCaptured(null, 100));
        }
    }
}
```

- [ ] **Step 2: Run the tests and confirm they fail.** Expected: compile errors, because `SpawnsInCell`, `Nearby`, `WindowOf` and the rest are missing.

- [ ] **Step 3: Implement.** Add `using System.Collections.Generic;` at the top of `GeoSpawner.cs`, then append this block after the first `partial` class (still inside `namespace EnerGo`):

```csharp
    public static partial class GeoSpawner
    {
        public const double CellDeg = 0.0005;  // ~55 m in Indonesia
        public const long WindowSeconds = 900; // spawns change every 15 minutes

        // APPEND-ONLY: reordering or inserting changes every spawn in the world.
        private static readonly string[] Types = { ResourceIds.Sun, ResourceIds.Coal, ResourceIds.Gold, ResourceIds.Oil, ResourceIds.Uranium };
        private static readonly double[] Weights = { 35, 30, 20, 12, 3 };

        public readonly struct Spawn
        {
            public readonly string Id;
            public readonly string ResourceId;
            public readonly double Lat;
            public readonly double Lon;
            public Spawn(string id, string resourceId, double lat, double lon) { Id = id; ResourceId = resourceId; Lat = lat; Lon = lon; }
        }

        public static long CellOf(double deg) => (long)Math.Floor(deg / CellDeg);
        public static long WindowOf(long unixUtcSeconds) => (long)Math.Floor(unixUtcSeconds / (double)WindowSeconds);
        public static long SecondsToNextWindow(long unixUtcSeconds) => (WindowOf(unixUtcSeconds) + 1) * WindowSeconds - unixUtcSeconds;
        public static bool IsDaytime(int localHour) => localHour >= 6 && localHour < 18;

        // 0 (60%), 1 (30%) or 2 (10%) spawns at random spots in the cell. The draws happen in the same order
        // whatever the hour, so day/night only ever changes a spawn's type, never its position.
        public static List<Spawn> SpawnsInCell(long cellX, long cellY, long window, int localHour)
        {
            var list = new List<Spawn>(2);
            ulong state = Seed(cellX, cellY, window);
            double r = Next01(ref state);
            int count = r < 0.6 ? 0 : r < 0.9 ? 1 : 2;
            bool day = IsDaytime(localHour);
            for (int i = 0; i < count; i++)
            {
                double lat = (cellY + Next01(ref state)) * CellDeg;
                double lon = (cellX + Next01(ref state)) * CellDeg;
                string type = PickType(Next01(ref state), day);
                list.Add(new Spawn(cellX + ":" + cellY + ":" + window + ":" + i, type, lat, lon));
            }
            return list;
        }

        // Every spawn within radiusM of the player, nearest first, minus the ones this player already caught.
        public static List<Spawn> Nearby(double lat, double lon, double radiusM, long unixUtcSeconds, int localHour, ICollection<string> captured)
        {
            long window = WindowOf(unixUtcSeconds);
            double dLat = radiusM / MetersPerDegree;
            double dLon = radiusM / (MetersPerDegree * Math.Max(0.01, Math.Cos(lat * Math.PI / 180.0)));
            var result = new List<Spawn>();
            for (long y = CellOf(lat - dLat); y <= CellOf(lat + dLat); y++)
            for (long x = CellOf(lon - dLon); x <= CellOf(lon + dLon); x++)
                foreach (var s in SpawnsInCell(x, y, window, localHour))
                    if ((captured == null || !captured.Contains(s.Id)) && DistanceM(lat, lon, s.Lat, s.Lon) <= radiusM)
                        result.Add(s);
            result.Sort((a, b) => DistanceM(lat, lon, a.Lat, a.Lon).CompareTo(DistanceM(lat, lon, b.Lat, b.Lon)));
            return result;
        }

        // Captured ids only matter while their window can still be on screen (current and the one before).
        public static string[] PruneCaptured(string[] ids, long window)
        {
            var keep = new List<string>();
            if (ids != null)
                foreach (var id in ids)
                {
                    var parts = id?.Split(':');
                    if (parts != null && parts.Length == 4 && long.TryParse(parts[2], out long w) && w >= window - 1) keep.Add(id);
                }
            return keep.ToArray();
        }

        private static string PickType(double u, bool day)
        {
            double total = 0;
            for (int i = 0; i < Types.Length; i++) if (day || Types[i] != ResourceIds.Sun) total += Weights[i];
            double t = u * total;
            for (int i = 0; i < Types.Length; i++)
            {
                if (!day && Types[i] == ResourceIds.Sun) continue;
                if (t < Weights[i]) return Types[i];
                t -= Weights[i];
            }
            return Types[Types.Length - 1];
        }

        private static ulong Seed(long cellX, long cellY, long window)
        {
            unchecked
            {
                ulong s = (ulong)cellX * 0x9E3779B97F4A7C15UL ^ (ulong)cellY * 0xC2B2AE3D27D4EB4FUL ^ (ulong)window * 0x165667B19E3779F9UL;
                SplitMix(ref s);
                return s;
            }
        }

        // splitmix64: tiny, fast, identical on every platform (unlike System.Random / UnityEngine.Random).
        private static ulong SplitMix(ref ulong state)
        {
            unchecked
            {
                state += 0x9E3779B97F4A7C15UL;
                ulong z = state;
                z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
                z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
                return z ^ (z >> 31);
            }
        }

        private static double Next01(ref ulong state) => (SplitMix(ref state) >> 11) * (1.0 / 9007199254740992.0);
    }
```

- [ ] **Step 4: Compile and run the tests.** Expected: `GeoSpawnTests` all pass, and so do the earlier tests. If `NearbyIsSortedAndSkipsCaptured` fails only with "fixture needs a few spawns", change `Now` to another fixed value that gives 3 or more spawns. Do not loosen the assertion.

- [ ] **Step 5: Commit**

```bash
git add Assets/EnerGo/Scripts/Core/GeoSpawner.cs Assets/EnerGo/Tests/Editor/GeoSpawnTests.cs Assets/EnerGo/Tests/Editor/GeoSpawnTests.cs.meta
git commit -m "feat: deterministic rarity-weighted world spawns (GeoSpawner.Nearby)"
```

---

### Task 4: Save data — `spawnId` on the capture and `capturedSpawns` on the save

**Files:**
- Modify: `Assets/EnerGo/Scripts/EnerGoProgress.cs` (`CaptureSession`, `SaveGame`, `Commit`, `BeginAnalysis`, `Answer`)

**Interfaces:**
- Consumes: `GeoSpawner.PruneCaptured`, `GeoSpawner.WindowOf` (Task 3).
- Produces:
  - `CaptureSession.spawnId` (string, empty = practice or an old save).
  - `SaveGame.capturedSpawns` (`string[]`).
  - `EnerGoProgress.BeginAnalysis(string resourceId, float performance = 0f, string spawnId = "")`.

- [ ] **Step 1: Write the verification script** (Assembly-CSharp is not reachable from the test asmdef, so this check runs in the editor). Save it as `scratch/check_capture_save.cs`, outside `Assets/`:

```csharp
using System.IO; using System.Linq; using EnerGo;
public class Script { public static object Main() {
  string save = EnerGoProgress.SavePath, dir = Path.GetDirectoryName(save);
  foreach (var f in Directory.GetFiles(dir, Path.GetFileName(save) + "*")) if (!f.EndsWith(".claudebak")) File.Copy(f, f + ".claudebak", true);
  string result;
  try {
    long w = GeoSpawner.WindowOf(System.DateTimeOffset.UtcNow.ToUnixTimeSeconds());
    // a stale id from 3 windows ago must be pruned on the next commit
    var s0 = EnerGoProgress.Current; s0.capturedSpawns = new[] { "9:9:" + (w - 3) + ":0" }; EnerGoProgress.Commit(s0);
    string id = "1:2:" + w + ":0";
    if (!EnerGoProgress.BeginAnalysis(ResourceIds.Coal, 0.5f, id)) return "FAIL begin " + EnerGoProgress.Error;
    if (EnerGoProgress.Current.pendingCapture.spawnId != id) return "FAIL spawnId not stored";
    if (EnerGoProgress.Current.capturedSpawns.Length != 0) return "FAIL stale id not pruned";
    for (int q = 0; q < 3; q++) {
      if (EnerGoProgress.Current.capturedSpawns.Contains(id)) return "FAIL captured before last answer";
      EnerGoProgress.Answer(true);
      if (q < 2) EnerGoProgress.NextQuestion();
    }
    if (!EnerGoProgress.Current.capturedSpawns.Contains(id)) return "FAIL not captured with reward";
    EnerGoProgress.NextQuestion(); EnerGoProgress.ClearResult();
    if (!EnerGoProgress.Current.capturedSpawns.Contains(id)) return "FAIL lost after ClearResult";
    result = "OK";
  } finally {
    foreach (var f in Directory.GetFiles(dir, "*.claudebak")) { File.Copy(f, f.Substring(0, f.Length - 10), true); File.Delete(f); }
    EnerGoProgress.Load();
  }
  return result; } }
```

- [ ] **Step 2: Run it and confirm it fails.** Run it with the "Run C# in the editor" recipe. Expected: a compile error, because `capturedSpawns` and `spawnId` do not exist.

- [ ] **Step 3: Implement.** Make these edits in `EnerGoProgress.cs`:

  1. In `CaptureSession`, after `public int performanceBonus; ...`:

     ```csharp
             public string spawnId;       // radar spawn being captured; empty for practice mode and older saves
     ```

  2. In `SaveGame`, after `public bool legacyGoldImported;`:

     ```csharp
             public string[] capturedSpawns = new string[0]; // radar spawns this player already caught (recent windows only)
     ```

  3. In `Commit`, as the first lines of the method body, before the `recoveryRequired` check:

     ```csharp
                 next.capturedSpawns = GeoSpawner.PruneCaptured(next.capturedSpawns,
                     GeoSpawner.WindowOf(DateTimeOffset.UtcNow.ToUnixTimeSeconds()));
     ```

  4. Change the `BeginAnalysis` signature and the session initialiser:

     ```csharp
             public static bool BeginAnalysis(string resourceId, float performance = 0f, string spawnId = "")
     ```

     Inside the `new CaptureSession { ... }`, add `spawnId = spawnId ?? "",`.

  5. In `Answer`, inside `if (session.IsLastQuestion) { ... }`, right after `next.lastCommittedCaptureId = session.sessionId;`:

     ```csharp
                     // The spawn disappears from this player's radar in the same commit that pays the reward.
                     if (!string.IsNullOrEmpty(session.spawnId))
                         next.capturedSpawns = (next.capturedSpawns ?? new string[0]).Concat(new[] { session.spawnId }).ToArray();
     ```

     Add `using System.Linq;` to the top of the file if it is missing (`System` is already imported for `Guid`/`DateTimeOffset`).

- [ ] **Step 4: Compile and run the script.** Expected: the compile command shows no errors, and the script returns `"OK"`. Also run the EditMode tests, which still all pass.

- [ ] **Step 5: Commit**

```bash
git add Assets/EnerGo/Scripts/EnerGoProgress.cs
git commit -m "feat: remember captured radar spawns with the capture reward"
```

---

### Task 5: `LocationFeed` — device GPS and compass, or an editor walk simulation

**Files:**
- Create: `Assets/EnerGo/Scripts/LocationFeed.cs`

**Interfaces:**
- Consumes: `GeoSpawner.DistanceM`, `PlausibleMove`, `SmoothHeading`, `MetersPerDegree`.
- Produces: `LocationFeed : MonoBehaviour` with:
  - `enum FeedStatus { Starting, Running, Denied, Disabled, Failed }`
  - Properties: `Status`, `Lat` (double), `Lon` (double), `AccuracyM` (float), `HeadingDeg` (float, 0 = north), `HasCompass` (bool).
  - `void Restart()`
  - `void SimulateAt(double lat, double lon, float headingDeg)` (editor only)

- [ ] **Step 1: Implement** `Assets/EnerGo/Scripts/LocationFeed.cs`:

```csharp
using UnityEngine;
using UnityEngine.InputSystem;
#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine.Android;
#endif

namespace EnerGo
{
    // Player position + facing for the radar. On the phone: GPS + compass, running only while this component is
    // enabled (battery). In the editor: a simulated walk, WASD to move and Q/E to turn.
    public class LocationFeed : MonoBehaviour
    {
        public enum FeedStatus { Starting, Running, Denied, Disabled, Failed }

        [Header("Editor simulation")]
        [SerializeField] private double simStartLat = -6.200000;  // change to the school's coordinates
        [SerializeField] private double simStartLon = 106.816666;
        [SerializeField] private float simWalkSpeed = 1.5f;       // m/s
        [SerializeField] private float simTurnSpeed = 90f;        // deg/s

        private const float StartTimeoutSec = 20f;

        public FeedStatus Status { get; private set; } = FeedStatus.Starting;
        public double Lat { get; private set; }
        public double Lon { get; private set; }
        public float AccuracyM { get; private set; }
        public float HeadingDeg { get; private set; }
        public bool HasCompass { get; private set; }

        private float startedAt;
        private double lastFixTime = -1;
        private bool waitingForPermission;

        private void OnEnable() => Restart();

        private void OnDisable()
        {
#if !UNITY_EDITOR
            Input.location.Stop();
            Input.compass.enabled = false;
#endif
        }

        public void Restart()
        {
            Status = FeedStatus.Starting;
            startedAt = Time.unscaledTime;
#if UNITY_EDITOR
            SimulateAt(simStartLat, simStartLon, HeadingDeg);
#else
  #if UNITY_ANDROID
            if (!Permission.HasUserAuthorizedPermission(Permission.FineLocation))
            {
                waitingForPermission = true;
                var callbacks = new PermissionCallbacks();
                callbacks.PermissionGranted += _ => { waitingForPermission = false; StartDevice(); };
                callbacks.PermissionDenied += _ => { waitingForPermission = false; Status = FeedStatus.Denied; };
                callbacks.PermissionDeniedAndDontAskAgain += _ => { waitingForPermission = false; Status = FeedStatus.Denied; };
                Permission.RequestUserPermission(Permission.FineLocation, callbacks);
                return;
            }
  #endif
            StartDevice();
#endif
        }

        // Editor/test helper: jump to a place and facing.
        public void SimulateAt(double lat, double lon, float headingDeg)
        {
            Lat = lat; Lon = lon; HeadingDeg = headingDeg; AccuracyM = 5f; HasCompass = true; Status = FeedStatus.Running;
        }

        private void StartDevice()
        {
            if (!Input.location.isEnabledByUser) { Status = FeedStatus.Disabled; return; }
            Input.compass.enabled = true;
            Input.location.Start(5f, 2f);
            startedAt = Time.unscaledTime;
            lastFixTime = -1;
            Status = FeedStatus.Starting;
        }

        private void Update()
        {
#if UNITY_EDITOR
            UpdateSimulation(Time.unscaledDeltaTime);
#else
            UpdateDevice();
#endif
        }

        private void UpdateDevice()
        {
            if (waitingForPermission || Status == FeedStatus.Denied || Status == FeedStatus.Disabled || Status == FeedStatus.Failed) return;
            var service = Input.location.status;
            if (service == LocationServiceStatus.Failed) { Status = FeedStatus.Failed; return; }
            if (service != LocationServiceStatus.Running)
            {
                if (Status == FeedStatus.Starting && Time.unscaledTime - startedAt > StartTimeoutSec) Status = FeedStatus.Failed;
                return;
            }

            var fix = Input.location.lastData;
            if (fix.timestamp != lastFixTime)
            {
                bool first = lastFixTime < 0;
                double moved = GeoSpawner.DistanceM(Lat, Lon, fix.latitude, fix.longitude);
                if (first || GeoSpawner.PlausibleMove(moved, fix.timestamp - lastFixTime))
                {
                    Lat = fix.latitude;
                    Lon = fix.longitude;
                    AccuracyM = fix.horizontalAccuracy;
                }
                lastFixTime = fix.timestamp;
                Status = FeedStatus.Running;
            }

            HasCompass = Input.compass.enabled && Input.compass.timestamp > 0;
            if (HasCompass) HeadingDeg = GeoSpawner.SmoothHeading(HeadingDeg, Input.compass.trueHeading, Time.unscaledDeltaTime);
        }

        private void UpdateSimulation(float dt)
        {
            var kb = Keyboard.current;
            if (kb == null || Status != FeedStatus.Running) return;
            float turn = (kb.eKey.isPressed ? 1f : 0f) - (kb.qKey.isPressed ? 1f : 0f);
            HeadingDeg = Mathf.Repeat(HeadingDeg + turn * simTurnSpeed * dt, 360f);
            float fwd = (kb.wKey.isPressed ? 1f : 0f) - (kb.sKey.isPressed ? 1f : 0f);
            float side = (kb.dKey.isPressed ? 1f : 0f) - (kb.aKey.isPressed ? 1f : 0f);
            if (fwd == 0f && side == 0f) return;
            float h = HeadingDeg * Mathf.Deg2Rad;
            // forward = (north cos h, east sin h); right = (north -sin h, east cos h)
            double north = (fwd * Mathf.Cos(h) - side * Mathf.Sin(h)) * simWalkSpeed * dt;
            double east = (fwd * Mathf.Sin(h) + side * Mathf.Cos(h)) * simWalkSpeed * dt;
            Lat += north / GeoSpawner.MetersPerDegree;
            Lon += east / (GeoSpawner.MetersPerDegree * System.Math.Cos(Lat * System.Math.PI / 180.0));
        }
    }
}
```

- [ ] **Step 2: Compile.** Expected: no errors. This file uses the legacy `Input.location` and `Input.compass` APIs while `activeInputHandler` is 1 (new Input System only). On 2026-10-10 those APIs were checked in this editor and do not throw: status was Stopped and heading 0. The remaining device risk is covered in Task 10, Step 4.

- [ ] **Step 3: Verify the simulation in the editor.** Save this as `scratch/check_feed.cs`. It runs outside play mode and calls the walk maths through `SimulateAt`:

```csharp
using UnityEngine; using EnerGo;
public class Script { public static object Main() {
  var go = new GameObject("feed-check"); var f = go.AddComponent<LocationFeed>();
  f.SimulateAt(-6.2, 106.8, 90f);
  var m = typeof(LocationFeed).GetMethod("UpdateSimulation", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
  // no keys held -> no movement
  m.Invoke(f, new object[] { 1f });
  string r = f.Status + " " + f.Lat + "," + f.Lon + " heading " + f.HeadingDeg;
  Object.DestroyImmediate(go);
  return r; } }
```

Expected: `Running -6.2,106.8 heading 90`.

- [ ] **Step 4: Commit**

```bash
git add Assets/EnerGo/Scripts/LocationFeed.cs Assets/EnerGo/Scripts/LocationFeed.cs.meta
git commit -m "feat: LocationFeed (GPS + compass on device, WASD/QE walk in editor)"
```

---

### Task 6: Oil and uranium crystal art, resource colours, and a lock glyph

**Files:**
- Modify: `Branding/logo/build_unity_assets.py` (add a crystals block)
- Create: `Assets/EnerGo/Resources/UI/crystal_oil.png`, `Assets/EnerGo/Resources/UI/crystal_uranium.png` (generated)
- Modify: `Assets/EnerGo/Scripts/EnerGoUI.cs` (`Resource()` and a new `DrawLock`)

**Interfaces:**
- Produces:
  - `Resources.Load<Texture2D>("UI/crystal_" + id)` works for `sun, coal, gold, oil, uranium, random`.
  - `EnerGoUI.Resource(ResourceIds.Oil/Uranium)` returns colours.
  - `EnerGoUI.DrawLock(Vector2 centre, float size)`.

- [ ] **Step 1: Generate the crystals.** In `Branding/logo/build_unity_assets.py`, add this constant above `if __name__ == "__main__":`:

```python
# In-game crystal per resource: (top facet, bottom facet, left facet). Matches the guide's resource colour system.
CRYSTALS = {
    "sun": ("#FFE08A", "#FF9F1C", "#FFC93C"),
    "coal": ("#7A8090", "#16181D", "#3A3F4B"),
    "gold": ("#F7D98B", "#9A6A0C", "#E8B23A"),
    "oil": ("#9CC0CC", "#12303A", "#2F5D6B"),
    "uranium": ("#B9F0B0", "#0F5A2A", "#1E9A4A"),
    "random": (SUN, INK, ORANGE),
}
```

Then add this at the end of the `__main__` block:

```python
    for key, cols in CRYSTALS.items():
        src = svg(f"crystal-{key}", f"54 36 {H3:.2f} 184", symbol(cols, gap=11))
        render(src, f"{UNITY}/Resources/UI/crystal_{key}.png", 173, 200)
```

Run `cd Branding/logo && python build_unity_assets.py`. Expected: it writes `crystal_oil.png` and `crystal_uranium.png` among the others.

- [ ] **Step 2: Import settings.** Run this through the editor script recipe:

```csharp
using UnityEditor; using UnityEngine;
public class Script { public static object Main() {
  AssetDatabase.Refresh();
  foreach (var k in new[] { "oil", "uranium" }) {
    var ti = (TextureImporter)AssetImporter.GetAtPath("Assets/EnerGo/Resources/UI/crystal_" + k + ".png");
    ti.alphaIsTransparency = true; ti.mipmapEnabled = false; ti.npotScale = TextureImporterNPOTScale.None;
    ti.textureCompression = TextureImporterCompression.Uncompressed; ti.wrapMode = TextureWrapMode.Clamp; ti.SaveAndReimport();
  }
  return Resources.Load<Texture2D>("UI/crystal_uranium") != null ? "OK" : "FAIL"; } }
```

Expected: `OK`.

- [ ] **Step 3: Colours and lock glyph** in `EnerGoUI.cs`. Replace `Resource()` with:

```csharp
        public static Color Resource(string id) =>
            id == ResourceIds.Sun ? Sun : id == ResourceIds.Coal ? Hex(0x3A3F4B) : id == ResourceIds.Gold ? Hex(0xE8B23A)
            : id == ResourceIds.Oil ? Hex(0x2F5D6B) : id == ResourceIds.Uranium ? Hex(0x1E9A4A) : Orange;
```

Add after `DrawBar`:

```csharp
        // Small padlock badge (locked resources): white backing, ink shackle and body. `size` = body width.
        public static void DrawLock(Vector2 c, float size)
        {
            float bodyH = size * 0.75f, shackle = size * 0.62f;
            DrawPanel(new Rect(c.x - size * 0.5f - 3f, c.y - shackle - 2f, size + 6f, shackle + bodyH * 0.5f + 5f), Paper, Color.clear, size * 0.35f);
            DrawPanel(new Rect(c.x - size * 0.3f, c.y - shackle, size * 0.6f, shackle + 2f), Color.clear, Ink, size * 0.3f, Mathf.Max(1.5f, size * 0.14f));
            DrawPanel(new Rect(c.x - size * 0.5f, c.y - bodyH * 0.5f + 1f, size, bodyH), Ink, Color.clear, size * 0.18f);
        }
```

- [ ] **Step 4: Compile.** Expected: no errors.

- [ ] **Step 5: Commit**

```bash
git add Branding/logo/build_unity_assets.py Assets/EnerGo/Resources/UI/crystal_oil.png Assets/EnerGo/Resources/UI/crystal_oil.png.meta Assets/EnerGo/Resources/UI/crystal_uranium.png Assets/EnerGo/Resources/UI/crystal_uranium.png.meta Assets/EnerGo/Scripts/EnerGoUI.cs
git commit -m "feat: oil/uranium crystal art, resource colours and lock glyph"
```

---

### Task 7: `RadarController` and `ExploreScene`

**Files:**
- Create: `Assets/EnerGo/Scripts/RadarController.cs`
- Modify: `Assets/EnerGo/Editor/LobbySceneBuilder.cs` (Explore scene creation and build settings)
- Create (generated): `Assets/EnerGo/ExploreScene.unity`
- Modify: `Assets/EnerGo/Scripts/LobbyController.cs` (only add `public static string CaptureSceneName()` and `public static bool OpenPracticeOnLoad`, which this task needs; the rest of the lobby changes are Task 8)

**Interfaces:**
- Consumes:
  - `LocationFeed` (Task 5).
  - `GeoSpawner.Nearby/DistanceM/BearingDeg/Direction8/CanCapture/SecondsToNextWindow` (Tasks 2–3).
  - `ExploreSession.PendingSpawnId`, `ResourceIds.IsPlayable` (Task 1).
  - `EnerGoUI.DrawLock`, `crystal_*` (Task 6).
  - `EnerGoProgress.Current.capturedSpawns` (Task 4).
- Produces:
  - Scene `ExploreScene` registered in the build settings.
  - `LobbyController.CaptureSceneName()` → `"ARPrototype"` or `"Sensor360Scene"`.
  - `LobbyController.OpenPracticeOnLoad` (static bool).

- [ ] **Step 1: Lobby helpers needed by the radar.** In `LobbyController.cs`:
  1. Change `private const string PrefUseARCore` to `public const string PrefUseARCore`.
  2. Add, below `hasShownSplash`:

```csharp
        // Set by the radar's "Main mode Latihan" so the lobby opens straight onto the practice picker.
        public static bool OpenPracticeOnLoad;

        public static string CaptureSceneName() => PlayerPrefs.GetInt(PrefUseARCore, 0) == 1 ? "ARPrototype" : "Sensor360Scene";
```

- [ ] **Step 2: Write `RadarController.cs`**

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem.EnhancedTouch;
using static EnerGo.EnerGoUI;

namespace EnerGo
{
    // Exploration screen: SDA around the player on a heading-up radar. Walk within range of one and tap TANGKAP
    // to open its capture mini-game; the spawn id travels along so the reward also removes it from the radar.
    [RequireComponent(typeof(LocationFeed))]
    public class RadarController : MonoBehaviour
    {
        private const double RangeM = 150.0;
        private const float RefreshSec = 0.5f;
        private const float WeakGpsM = 30f;
        private const string PrefSafetySeen = "EnerGo.RadarSafetySeen";

        private LocationFeed feed;
        private List<GeoSpawner.Spawn> nearby = new List<GeoSpawner.Spawn>();
        private float nextRefresh;
        private string selectedId;
        private bool safetySeen;
        private readonly Dictionary<string, Texture2D> crystals = new Dictionary<string, Texture2D>();
        private GUIStyle sTitle, sBody, sMuted, sSmall, sDanger, sPrimary, sSecondary, sCardTitle, sCount, sNorth;

        private float HudScale => Mathf.Max(1f, Mathf.Min(Screen.width / 480f, Screen.height / 800f));
        private float Heading => feed.HasCompass ? feed.HeadingDeg : 0f;

        private void Awake()
        {
            feed = GetComponent<LocationFeed>();
            safetySeen = PlayerPrefs.GetInt(PrefSafetySeen, 0) == 1;
        }

        private void OnEnable() => EnhancedTouchSupport.Enable();
        private void OnDisable() => EnhancedTouchSupport.Disable();

        private static long NowUtc() => DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        private void Update()
        {
            if (feed.Status != LocationFeed.FeedStatus.Running || Time.unscaledTime < nextRefresh) return;
            nextRefresh = Time.unscaledTime + RefreshSec;
            Refresh();
        }

        // Public so editor checks can force a refresh (the editor does not tick in play mode).
        public void Refresh()
        {
            var captured = new HashSet<string>(EnerGoProgress.Current.capturedSpawns ?? new string[0]);
            nearby = GeoSpawner.Nearby(feed.Lat, feed.Lon, RangeM, NowUtc(), DateTime.Now.Hour, captured);
            if (nearby.FindIndex(s => s.Id == selectedId) < 0) selectedId = nearby.Count > 0 ? nearby[0].Id : null;
        }

        private void InitStyles()
        {
            if (sTitle != null) return;
            sTitle = Text(20, true, Ink);
            sBody = Text(14, false, Ink, TextAnchor.MiddleCenter, true);
            sMuted = Text(13, false, Muted, TextAnchor.MiddleLeft);
            sSmall = Text(11, true, Muted, TextAnchor.MiddleLeft);
            sDanger = Text(11, true, Danger, TextAnchor.MiddleLeft);
            sPrimary = Text(17, true, Paper);
            sSecondary = Text(14, true, Muted);
            sCardTitle = Text(17, true, Ink, TextAnchor.MiddleLeft);
            sCount = Text(13, true, Ink);
            sNorth = Text(13, true, Orange);
        }

        private Texture2D Crystal(string id)
        {
            if (!crystals.TryGetValue(id, out var tex) || tex == null) crystals[id] = tex = Resources.Load<Texture2D>("UI/crystal_" + id);
            return tex;
        }

        private void OnGUI()
        {
            InitStyles();
            var prevMatrix = GUI.matrix;
            float scale = HudScale;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            float sw = Screen.width / scale, sh = Screen.height / scale;
            GUI.color = Cream;
            GUI.DrawTexture(new Rect(0, 0, sw, sh), Texture2D.whiteTexture);
            GUI.color = Color.white;

            float top = Mathf.Max(16f, (Screen.height - Screen.safeArea.yMax) / scale + 12f);
            float bottom = Mathf.Max(20f, Screen.safeArea.yMin / scale + 16f);
            float w = Mathf.Min(sw - 40f, 440f), x = (sw - w) * 0.5f;
            bool interactive = safetySeen;

            // Top bar: back to lobby, countdown to the next spawn window.
            var back = new Rect(x, top, 112f, 48f);
            GUI.Label(DrawSecondaryButton(back, IsHeld(back, scale)), "‹  LOBBY", sCount);
            if (interactive && TapHit(back, scale))
            {
                if (SFXManager.Instance != null) SFXManager.Instance.PlayButtonBack();
                SceneManager.LoadScene("LobbyScene");
            }
            long secs = GeoSpawner.SecondsToNextWindow(NowUtc());
            var timer = new Rect(x + w - 168f, top + 2f, 168f, 40f);
            DrawCard(timer, 20f);
            GUI.Label(timer, "Spawn baru " + (secs / 60).ToString("00") + ":" + (secs % 60).ToString("00"), sCount);

            var card = new Rect(x, sh - bottom - 136f, w, 136f);
            if (feed.Status == LocationFeed.FeedStatus.Running)
            {
                float radarTop = top + 70f, radarBottom = card.y - 44f;
                float R = Mathf.Min(w, radarBottom - radarTop) * 0.5f;
                var c = new Vector2(sw * 0.5f, (radarTop + radarBottom) * 0.5f);
                DrawRadar(c, R, scale, interactive);

                float infoY = card.y - 34f;
                GUI.Label(new Rect(x, infoY, w, 18f), "GPS ±" + Mathf.RoundToInt(feed.AccuracyM) + " m", sSmall);
                if (feed.AccuracyM > WeakGpsM)
                    GUI.Label(new Rect(x + 80f, infoY, w - 80f, 18f), "Sinyal GPS lemah, coba ke tempat terbuka", sDanger);
                else if (!feed.HasCompass)
                    GUI.Label(new Rect(x + 80f, infoY, w - 80f, 18f), "Kompas tidak tersedia, utara di atas", sSmall);

                DrawSelected(card, scale, interactive);
            }
            else DrawStatus(card, scale, interactive);

            if (!safetySeen) DrawSafety(sw, sh, scale);
            GUI.matrix = prevMatrix;
        }

        // Screen position of a world bearing/distance on the heading-up radar.
        private Vector2 OnRadar(Vector2 c, float R, double bearing, double dist)
        {
            float a = (float)(bearing - Heading) * Mathf.Deg2Rad;
            float r = Mathf.Clamp01((float)(dist / RangeM)) * (R - 18f);
            return c + new Vector2(Mathf.Sin(a), -Mathf.Cos(a)) * r;
        }

        private static Rect Circle(Vector2 p, float r) => new Rect(p.x - r, p.y - r, r * 2f, r * 2f);

        private void DrawRadar(Vector2 c, float R, float scale, bool interactive)
        {
            DrawPanel(new Rect(c.x - R, c.y - R + 6f, R * 2f, R * 2f), Alpha(Ink, 0.08f), Color.clear, R);
            DrawPanel(Circle(c, R), Paper, Line, R, 2f);
            foreach (double ring in new[] { 50.0, 100.0 })
            {
                float rr = (float)(ring / RangeM) * (R - 18f);
                DrawPanel(Circle(c, rr), Color.clear, Line, rr, 1.5f);
                GUI.Label(new Rect(c.x + 4f, c.y - rr - 16f, 60f, 14f), ring + " m", sSmall);
            }

            // Sweep line, slowly turning.
            var m = GUI.matrix;
            GUIUtility.RotateAroundPivot(Time.unscaledTime * 90f % 360f, c);
            DrawPanel(new Rect(c.x - 1f, c.y - R + 4f, 2f, R - 4f), Alpha(Orange, 0.35f), Color.clear, 1f);
            GUI.matrix = m;

            // North marker ("U") rides the rim.
            Vector2 n = OnRadar(c, R, 0, RangeM);
            n = c + (n - c).normalized * (R - 12f);
            GUI.Label(new Rect(n.x - 10f, n.y - 10f, 20f, 20f), "U", sNorth);

            foreach (var s in nearby)
            {
                double d = GeoSpawner.DistanceM(feed.Lat, feed.Lon, s.Lat, s.Lon);
                Vector2 p = OnRadar(c, R, GeoSpawner.BearingDeg(feed.Lat, feed.Lon, s.Lat, s.Lon), d);
                bool inRange = GeoSpawner.CanCapture(d, feed.AccuracyM);
                bool locked = !ResourceIds.IsPlayable(s.ResourceId);
                float size = inRange ? 30f : 22f;
                if (s.ResourceId == ResourceIds.Uranium) // rare: pulse a glow so it is noticed
                {
                    float glow = size * (0.9f + 0.15f * Mathf.Sin(Time.unscaledTime * 4f));
                    DrawPanel(Circle(p, glow), Alpha(Resource(s.ResourceId), 0.25f), Color.clear, glow);
                }
                if (s.Id == selectedId) DrawPanel(Circle(p, size * 0.75f), Color.clear, Ink, size * 0.75f, 2f);
                else if (inRange && !locked) DrawPanel(Circle(p, size * 0.75f), Color.clear, Orange, size * 0.75f, 2.5f);
                var tex = Crystal(s.ResourceId);
                if (tex != null) GUI.DrawTexture(new Rect(p.x - size * 0.43f, p.y - size * 0.5f, size * 0.866f, size), tex, ScaleMode.ScaleToFit);
                if (locked) DrawLock(p + new Vector2(size * 0.4f, size * 0.35f), 9f);
                if (interactive && TapHit(Circle(p, 22f), scale)) selectedId = s.Id;
            }

            // The player: the brand crystal pointing up (= the way the phone faces).
            var me = Crystal("random");
            if (me != null)
            {
                var mm = GUI.matrix;
                GUIUtility.RotateAroundPivot(-90f, c);
                GUI.DrawTexture(new Rect(c.x - 13f, c.y - 15f, 26f, 30f), me, ScaleMode.ScaleToFit);
                GUI.matrix = mm;
            }
        }

        private void DrawSelected(Rect card, float scale, bool interactive)
        {
            DrawCard(card, 20f);
            int i = nearby.FindIndex(s => s.Id == selectedId);
            if (i < 0)
            {
                GUI.Label(new Rect(card.x + 20f, card.y, card.width - 40f, card.height), "Belum ada SDA di sekitar. Coba berjalan ke area lain.", sBody);
                return;
            }
            var sel = nearby[i];
            double d = GeoSpawner.DistanceM(feed.Lat, feed.Lon, sel.Lat, sel.Lon);
            double b = GeoSpawner.BearingDeg(feed.Lat, feed.Lon, sel.Lat, sel.Lon);
            bool locked = !ResourceIds.IsPlayable(sel.ResourceId);
            bool inRange = GeoSpawner.CanCapture(d, feed.AccuracyM);

            var tex = Crystal(sel.ResourceId);
            if (tex != null) GUI.DrawTexture(new Rect(card.x + 18f, card.y + 14f, 30f, 35f), tex, ScaleMode.ScaleToFit);
            GUI.Label(new Rect(card.x + 60f, card.y + 12f, card.width - 80f, 22f), ResourceIds.Name(sel.ResourceId), sCardTitle);
            GUI.Label(new Rect(card.x + 60f, card.y + 34f, card.width - 110f, 18f), Mathf.RoundToInt((float)d) + " m · " + GeoSpawner.Direction8(b), sMuted);
            // Direction hint: a small crystal pointing the way to walk (relative to where the phone faces).
            var arrow = Crystal("random");
            if (arrow != null)
            {
                var m = GUI.matrix;
                var pivot = new Vector2(card.xMax - 34f, card.y + 30f);
                GUIUtility.RotateAroundPivot((float)(b - Heading) - 90f, pivot);
                GUI.DrawTexture(new Rect(pivot.x - 9f, pivot.y - 10f, 18f, 20f), arrow, ScaleMode.ScaleToFit);
                GUI.matrix = m;
            }

            var btn = new Rect(card.x + 16f, card.yMax - 16f - 58f, card.width - 32f, 58f);
            if (locked)
            {
                GUI.Label(DrawSecondaryButton(btn), "SEGERA HADIR", sSecondary);
                TapHit(btn, scale); // swallow the tap
            }
            else if (inRange)
            {
                GUI.Label(DrawPrimaryButton(btn, IsHeld(btn, scale)), "TANGKAP", sPrimary);
                if (interactive && TapHit(btn, scale)) Capture(sel);
            }
            else
            {
                GUI.Label(DrawSecondaryButton(btn), "DEKATI DULU (" + Mathf.RoundToInt((float)d) + " m)", sSecondary);
                TapHit(btn, scale);
            }
        }

        private void Capture(GeoSpawner.Spawn s)
        {
            if (SFXManager.Instance != null) SFXManager.Instance.PlayButtonClick();
            ResourceIds.Selected = s.ResourceId;
            ExploreSession.PendingSpawnId = s.Id;
            SceneManager.LoadScene(LobbyController.CaptureSceneName());
        }

        private void DrawStatus(Rect card, float scale, bool interactive)
        {
            string title, body;
            switch (feed.Status)
            {
                case LocationFeed.FeedStatus.Denied: title = "Izin lokasi ditolak"; body = "Radar butuh lokasimu untuk menemukan SDA di sekitar."; break;
                case LocationFeed.FeedStatus.Disabled: title = "Lokasi HP mati"; body = "Nyalakan Lokasi di pengaturan HP, lalu coba lagi."; break;
                case LocationFeed.FeedStatus.Failed: title = "GPS tidak merespons"; body = "Coba pindah ke tempat terbuka, lalu coba lagi."; break;
                default:
                    DrawCard(card, 20f);
                    GUI.Label(card, "Mencari sinyal GPS...", sBody);
                    return;
            }
            var box = new Rect(card.x, card.y - 150f, card.width, card.height + 150f);
            DrawCard(box, 20f);
            GUI.Label(new Rect(box.x + 20f, box.y + 18f, box.width - 40f, 26f), title, sTitle);
            GUI.Label(new Rect(box.x + 20f, box.y + 48f, box.width - 40f, 44f), body, sBody);
            var retry = new Rect(box.x + 16f, box.yMax - 16f - 58f - 66f, box.width - 32f, 58f);
            GUI.Label(DrawPrimaryButton(retry, IsHeld(retry, scale)), "COBA LAGI", sPrimary);
            if (interactive && TapHit(retry, scale)) feed.Restart();
            var practice = new Rect(box.x + 16f, box.yMax - 16f - 58f, box.width - 32f, 58f);
            GUI.Label(DrawSecondaryButton(practice, IsHeld(practice, scale)), "MAIN MODE LATIHAN", new GUIStyle(sSecondary) { normal = { textColor = Ink } });
            if (interactive && TapHit(practice, scale))
            {
                LobbyController.OpenPracticeOnLoad = true;
                SceneManager.LoadScene("LobbyScene");
            }
        }

        // One-time safety reminder before the first hunt.
        private void DrawSafety(float sw, float sh, float scale)
        {
            GUI.color = Alpha(Ink, 0.5f);
            GUI.DrawTexture(new Rect(0, 0, sw, sh), Texture2D.whiteTexture);
            GUI.color = Color.white;
            float w = Mathf.Min(sw - 40f, 380f);
            var box = new Rect((sw - w) * 0.5f, sh * 0.5f - 130f, w, 260f);
            DrawCard(box, 20f);
            GUI.Label(new Rect(box.x + 20f, box.y + 22f, w - 40f, 28f), "Hati-hati di jalan", sTitle);
            GUI.Label(new Rect(box.x + 20f, box.y + 58f, w - 40f, 90f),
                "Perhatikan jalan dan sekitarmu saat berburu. Jangan masuk ke area berbahaya atau milik orang lain.", sBody);
            var ok = new Rect(box.x + 16f, box.yMax - 16f - 58f, w - 32f, 58f);
            GUI.Label(DrawPrimaryButton(ok, IsHeld(ok, scale)), "SAYA MENGERTI", sPrimary);
            if (TapHit(ok, scale))
            {
                safetySeen = true;
                PlayerPrefs.SetInt(PrefSafetySeen, 1);
                PlayerPrefs.Save();
            }
        }
    }
}
```

- [ ] **Step 3: Scene builder.** In `LobbySceneBuilder.cs`:

  1. Add a constant: `const string ExploreScenePath = "Assets/EnerGo/ExploreScene.unity";`
  2. In `UpdateBuildSettings`, right after the Lobby entry is added, add:

     ```csharp
                 if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ExploreScenePath) != null)
                 {
                     scenes.Add(new EditorBuildSettingsScene(ExploreScenePath, true));
                 }
     ```

  3. Add the menu method below:

     ```csharp
             // Run once from the menu (or via MCP). Not done automatically on load, because it switches the open scene.
             [MenuItem("EnerGo/Setup Explore Scene")]
             public static void CreateExploreScene()
             {
                 if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ExploreScenePath) != null) { UpdateBuildSettings(); return; }
                 if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                 var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                 var camGo = new GameObject("Main Camera");
                 var cam = camGo.AddComponent<Camera>();
                 cam.clearFlags = CameraClearFlags.SolidColor;
                 cam.backgroundColor = new Color(0.98f, 0.97f, 0.94f, 1f); // Cream; the radar draws over it anyway
                 cam.orthographic = true;
                 camGo.tag = "MainCamera";
                 var radar = new GameObject("Radar");
                 radar.AddComponent<LocationFeed>();
                 radar.AddComponent<RadarController>();
                 EditorSceneManager.SaveScene(scene, ExploreScenePath);
                 UpdateBuildSettings();
             }
     ```

  This is needed because `UpdateBuildSettings` runs on every editor load and rewrites the scene list. Without the edit, ExploreScene would be dropped from the build.

- [ ] **Step 4: Compile, create the scene, and verify the build list.** Compile, then run:

```csharp
using UnityEditor;
public class Script { public static object Main() {
  EnerGo.Editor.LobbySceneBuilder.CreateExploreScene();
  return string.Join(",", System.Array.ConvertAll(EditorBuildSettings.scenes, s => s.path)); } }
```

Expected output contains `Assets/EnerGo/ExploreScene.unity` and the three existing scenes.

- [ ] **Step 5: Screenshot in the editor.** Open `ExploreScene`, enter play mode and set the Game View to "1920x1080 Portrait" (see memory: unity-mcp-screenshot-recipe). Then run:

```csharp
using UnityEngine;
public class Script { public static object Main() {
  PlayerPrefs.SetInt("EnerGo.RadarSafetySeen", 1);
  var radar = Object.FindFirstObjectByType<EnerGo.RadarController>();
  var f = radar.GetComponent<EnerGo.LocationFeed>(); f.SimulateAt(-6.2, 106.8, 30f);
  typeof(EnerGo.RadarController).GetField("safetySeen", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(radar, true);
  radar.Refresh();
  ScreenCapture.CaptureScreenshot(System.IO.Path.GetFullPath("Temp/radar.png"));
  return "ok"; } }
```

Tick the player loop until `Temp/radar.png` exists, then view it. Check:
- The radar has rings and the U marker.
- Kristal tiles sit at different distances.
- The bottom card shows the nearest SDA with distance and direction.
- The countdown pill is visible.

Then run `f.SimulateAt` at the selected spawn's own coordinates, call `radar.Refresh()` and capture again. Expected: the button reads `TANGKAP`, in orange.

- [ ] **Step 6: Commit**

```bash
git add Assets/EnerGo/Scripts/RadarController.cs Assets/EnerGo/Scripts/RadarController.cs.meta Assets/EnerGo/Editor/LobbySceneBuilder.cs Assets/EnerGo/ExploreScene.unity Assets/EnerGo/ExploreScene.unity.meta Assets/EnerGo/Scripts/LobbyController.cs ProjectSettings/EditorBuildSettings.asset
git commit -m "feat: radar exploration screen (ExploreScene)"
```

---

### Task 8: Lobby — collection cards, MULAI BERBURU opens the radar, and Latihan mode

**Files:**
- Modify: `Assets/EnerGo/Scripts/LobbyController.cs`

**Interfaces:**
- Consumes: `ResourceIds.IsPlayable`, `ExploreSession.PendingSpawnId`, `EnerGoUI.DrawLock`, `crystal_oil/uranium`, `LobbyController.CaptureSceneName/OpenPracticeOnLoad` (Task 7).
- Produces: `ScreenState.Practice`. The lobby's main button loads `ExploreScene`.

- [ ] **Step 1: State and transition target.** Make these edits:

  1. Add `Practice` to `enum ScreenState`, after `Settings`.
  2. Add a field: `private string transitionScene;`.
  3. In `Awake`, after `if (hasShownSplash) currentState = ScreenState.Lobby; else ...`, add:

     ```csharp
                 if (OpenPracticeOnLoad) { OpenPracticeOnLoad = false; hasShownSplash = true; currentState = ScreenState.Practice; }
     ```

  4. In `Start`, inside the pending-capture branch, before `currentState = ScreenState.TransitioningToAR;`, add `transitionScene = CaptureSceneName();`.
  5. In `Update`, replace `SceneManager.LoadScene(useARCore ? "ARPrototype" : "Sensor360Scene");` with `SceneManager.LoadScene(transitionScene ?? CaptureSceneName());`.
  6. In `OnGUI`, after `if (currentState == ScreenState.Settings) DrawSettings(sw, sh);`, add `if (currentState == ScreenState.Practice) DrawPractice(sw, sh);`.
  7. In `DrawTransition`, replace the `"Menyiapkan kamera..."` label text with `transitionScene == "ExploreScene" ? "Menyalakan radar..." : "Menyiapkan kamera..."`.

- [ ] **Step 2: Collection on the lobby.** In `DrawLobby`:

  1. Replace the line `float btnH = 66f, gap = 12f, cardW = (contentW - gap) * 0.5f, cardH = 76f;` with:

     ```csharp
                 float btnH = 66f, gap = 10f, cardW = (contentW - gap) * 0.5f, cardH = 62f;
     ```

  2. Replace `float cardsTop = playRect.y - 44f - (cardH * 2f + gap);` with `float cardsTop = playRect.y - 44f - (cardH * 3f + gap * 2f);`.

  3. Replace the whole block from `// Resource picker: 2x2 cards, the inventory lives on the cards.` through the end of its `for` loop with:

     ```csharp
                 // Collection: what the player has caught. Targets now come from the radar, so these are not buttons.
                 y = cardsTop - 26f;
                 GUI.Label(new Rect(x, y, contentW, 18f), "KOLEKSIMU", sSection);
                 y += 26f;
                 string[] ids = { ResourceIds.Sun, ResourceIds.Coal, ResourceIds.Gold, ResourceIds.Oil, ResourceIds.Uranium };
                 for (int i = 0; i < ids.Length; i++)
                     DrawCollectionCard(new Rect(x + (i % 2) * (cardW + gap), y + (i / 2) * (cardH + gap), cardW, cardH), ids[i]);
     ```

  4. Replace the play-button click body (the lines between `if (interactive && ButtonHit(playRect)) {` and its closing `}`) with:

     ```csharp
                     if (SFXManager.Instance != null) SFXManager.Instance.PlayButtonClick();
                     ExploreSession.PendingSpawnId = null;
                     transitionScene = "ExploreScene";
                     currentState = ScreenState.TransitioningToAR;
                     transitionTimer = 0f;
     ```

  5. Add the collection card method next to `DrawResourceCard`:

     ```csharp
             // Lobby collection tile: crystal, name, unit count; oil/uranium show a lock until their mini-games exist.
             private void DrawCollectionCard(Rect r, string id)
             {
                 bool playable = ResourceIds.IsPlayable(id);
                 DrawCard(r);
                 var iconRect = new Rect(r.x + 12f, r.center.y - 15f, 26f, 30f);
                 var icon = CrystalIcon(id);
                 GUI.color = new Color(1f, 1f, 1f, playable ? 1f : 0.45f);
                 if (icon != null) GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit);
                 GUI.color = Color.white;
                 if (!playable) DrawLock(new Vector2(iconRect.xMax - 2f, iconRect.yMax - 4f), 9f);

                 float tx = iconRect.xMax + 10f, tw = r.xMax - 12f - tx;
                 GUI.Label(new Rect(tx, r.y + 10f, tw, 18f), ResourceIds.Name(id), new GUIStyle(sCardTitle) { fontSize = 13 });
                 if (!playable)
                 {
                     GUI.Label(new Rect(tx, r.y + 32f, tw, 18f), "Segera hadir", sCardSub);
                     return;
                 }
                 string n = EnerGoProgress.Current.Get(id).Total.ToString();
                 var count = new GUIStyle(sCount) { fontSize = 18 };
                 float nw = count.CalcSize(new GUIContent(n)).x;
                 GUI.Label(new Rect(tx, r.y + 28f, nw, 24f), n, count);
                 GUI.Label(new Rect(tx + nw + 4f, r.y + 33f, tw - nw - 4f, 16f), "unit", sCardSub);
             }
     ```

  6. In `DrawResourceCard`, the `"Kejutan tiap main"` text and the rest stay unchanged; Latihan mode reuses it.

- [ ] **Step 3: Latihan button in Settings, and the practice sheet.** In `DrawSettings`:

  1. Change `float sheetH = 330f + SafeBottom(scale);` to `float sheetH = 398f + SafeBottom(scale);`.
  2. Right before `// Reset progress: two taps within 3.5 s`, insert:

     ```csharp
                 var practiceRect = new Rect(x, y, w, 54f);
                 GUI.Label(DrawSecondaryButton(practiceRect, IsHeld(practiceRect, scale)), "LATIHAN (TANPA GPS)", sSecondary);
                 if (ready && ButtonHit(practiceRect))
                 {
                     if (SFXManager.Instance != null) SFXManager.Instance.PlayButtonClick();
                     currentState = ScreenState.Practice;
                     inputCooldownTimer = 0.2f;
                     return;
                 }
                 y += 54f + 14f;
     ```

  3. Add the practice sheet method:

     ```csharp
             // Practice: the old picker (Matahari / Batu Bara / Emas / Acak) straight into a mini-game, no GPS.
             private void DrawPractice(float sw, float sh)
             {
                 float scale = HudScale;
                 GUI.color = Alpha(Ink, 0.45f);
                 GUI.DrawTexture(new Rect(0, 0, sw, sh), Texture2D.whiteTexture);
                 GUI.color = Color.white;
                 float gap = 12f, cardH = 76f;
                 float sheetH = 32f + 46f + cardH * 2f + gap + 20f + 66f + 12f + SafeBottom(scale);
                 var sheet = new Rect(0, sh - sheetH, sw, sheetH + 30f);
                 bool ready = inputCooldownTimer <= 0f;
                 if (ready && OutsideTap(sheet, scale))
                 {
                     if (SFXManager.Instance != null) SFXManager.Instance.PlayButtonBack();
                     currentState = ScreenState.Lobby;
                     inputCooldownTimer = 0.2f;
                     return;
                 }
                 DrawPanel(sheet, Cream, Color.clear, 24f);
                 float w = Mathf.Min(sw - 40f, 440f), x = (sw - w) * 0.5f, y = sheet.y + 12f;
                 DrawPanel(new Rect((sw - 40f) * 0.5f, y, 40f, 4f), Line, Color.clear, 2f);
                 y += 20f;
                 GUI.Label(new Rect(x, y, w, 30f), "Latihan (tanpa GPS)", new GUIStyle(sTitle) { alignment = TextAnchor.MiddleLeft });
                 y += 46f;
                 float cardW = (w - gap) * 0.5f;
                 string[] ids = { ResourceIds.Sun, ResourceIds.Coal, ResourceIds.Gold, RandomResource };
                 for (int i = 0; i < ids.Length; i++)
                 {
                     var rect = new Rect(x + (i % 2) * (cardW + gap), y + (i / 2) * (cardH + gap), cardW, cardH);
                     bool random = ids[i] == RandomResource;
                     if (DrawResourceCard(rect, ids[i], random ? "Acak" : ResourceIds.Name(ids[i]),
                             random ? -1 : EnerGoProgress.Current.Get(ids[i]).Total, ready))
                     {
                         if (SFXManager.Instance != null) SFXManager.Instance.PlayButtonClick();
                         selectedResource = ids[i];
                         PlayerPrefs.SetString(PrefResource, ids[i]);
                         PlayerPrefs.Save();
                         inputCooldownTimer = 0.15f;
                     }
                 }
                 y += cardH * 2f + gap + 20f;
                 var go = new Rect(x, y, w, 66f);
                 GUI.Label(DrawPrimaryButton(go, IsHeld(go, scale)), "MULAI LATIHAN", sPrimary);
                 if (ready && ButtonHit(go))
                 {
                     if (SFXManager.Instance != null) SFXManager.Instance.PlayButtonClick();
                     ResourceIds.Selected = selectedResource == RandomResource
                         ? AllResources[Random.Range(0, AllResources.Length)] : selectedResource;
                     ExploreSession.PendingSpawnId = null;
                     transitionScene = CaptureSceneName();
                     currentState = ScreenState.TransitioningToAR;
                     transitionTimer = 0f;
                 }
             }
     ```

  4. The lobby's `DrawLobby` used `selectedResource` only for the old picker. It still does, through the practice sheet, so keep the field and prefs.

- [ ] **Step 4: Compile and take screenshots.** Use the `shot.sh`-style capture from the screenshot recipe, with the states `Lobby`, `Settings` and `Practice`. Check:
  - **Lobby:** 5 collection tiles. Oil and uranium are faded, with a lock and "Segera hadir". The main button reads `MULAI BERBURU`.
  - **Settings:** shows `LATIHAN (TANPA GPS)` above `RESET PROGRES`.
  - **Practice:** the 2×2 picker plus `MULAI LATIHAN`.

- [ ] **Step 5: Commit**

```bash
git add Assets/EnerGo/Scripts/LobbyController.cs
git commit -m "feat: lobby collection, MULAI BERBURU opens radar, practice mode in settings"
```

---

### Task 9: Capture scene — carry `spawnId`, and route back to the Radar

**Files:**
- Modify: `Assets/EnerGo/Scripts/ARCapturePrototype.cs` (`CompleteCapture`, the top-left back button, and the result card's LANJUT button)

**Interfaces:**
- Consumes: `ExploreSession.PendingSpawnId` (Task 1), `BeginAnalysis(..., spawnId)` and `CaptureSession.spawnId` (Task 4), `ExploreScene` (Task 7).

- [ ] **Step 1: Implement**

  1. In `CompleteCapture`, replace `if (EnerGoProgress.BeginAnalysis(resourceId, capturePerformance))` with:

     ```csharp
                 if (EnerGoProgress.BeginAnalysis(resourceId, capturePerformance, ExploreSession.PendingSpawnId ?? ""))
     ```

     Inside the success branch, add `ExploreSession.PendingSpawnId = null;` as the first line.

  2. At the top-left back button (search for `"‹  LOBBY"`), replace

     ```csharp
                 if (EnerGoProgress.Current.pendingCapture == null && DrawBtn(lobbyRect, "‹  LOBBY", styleBtnText))
                 {
                     if (SFXManager.Instance != null) SFXManager.Instance.PlayButtonBack();
                     SceneManager.LoadScene("LobbyScene");
                     return;
                 }
     ```

     with

     ```csharp
                 bool fromRadar = ExploreSession.PendingSpawnId != null;
                 if (EnerGoProgress.Current.pendingCapture == null && DrawBtn(lobbyRect, fromRadar ? "‹  RADAR" : "‹  LOBBY", styleBtnText))
                 {
                     if (SFXManager.Instance != null) SFXManager.Instance.PlayButtonBack();
                     ExploreSession.PendingSpawnId = null; // leaving without catching: the spawn stays on the radar
                     SceneManager.LoadScene(fromRadar ? "ExploreScene" : "LobbyScene");
                     return;
                 }
     ```

  3. In the result card, replace

     ```csharp
                 if (DrawPrimaryBtn(new Rect(x + pad, cy2, resW, 52f), "LANJUT KE LOBBY"))
     ```

     with

     ```csharp
                 bool backToRadar = !string.IsNullOrEmpty(session.spawnId);
                 if (DrawPrimaryBtn(new Rect(x + pad, cy2, resW, 52f), backToRadar ? "LANJUT BERBURU" : "LANJUT KE LOBBY"))
     ```

     Inside that block, replace `SceneManager.LoadScene("LobbyScene");` with `SceneManager.LoadScene(backToRadar ? "ExploreScene" : "LobbyScene");`.

- [ ] **Step 2: Compile.** Expected: no errors.

- [ ] **Step 3: End-to-end check in the editor (covers Review Focus #2 and #3).** Back up the save first; the pattern is in the Task 4 script. In play mode:
  1. Load `ExploreScene`. `SimulateAt` the nearest playable spawn and `Refresh()`.
  2. Invoke the private `Capture(spawn)` by reflection. Expected: the scene switches to `Sensor360Scene`, and `ExploreSession.PendingSpawnId` equals the spawn id.
  3. Call `CompleteCapture()` by reflection. Expected: `pendingCapture.spawnId` equals the id, and `ExploreSession.PendingSpawnId` is null.
  4. Answer 3 times with `Answer(true)`/`NextQuestion()` until `state == "result"`. Take a screenshot. Expected: the button reads `LANJUT BERBURU`.
  5. **Simulate an app restart mid-quiz.** Before step 4, run `SceneManager.LoadScene("LobbyScene")`. Expected: the lobby auto-transitions back to the capture scene (the existing pending-capture path). After finishing, the button still reads `LANJUT BERBURU`, because `spawnId` is in the save.
  6. Click it by calling `EnerGoProgress.ClearResult()` and loading `ExploreScene`. `Refresh()`. Expected: the captured id is not in `RadarController.nearby`.
  7. Restore the save.

- [ ] **Step 4: Commit**

```bash
git add Assets/EnerGo/Scripts/ARCapturePrototype.cs
git commit -m "feat: capture scene carries radar spawn id and returns to the radar"
```

---

### Task 10: Full test pass, APK build, and the device checklist

**Files:** none (verification only)

- [ ] **Step 1: Run all EditMode tests.** Expected: every test in `EnerGo.Tests.Editor` passes.
- [ ] **Step 2: Run the Task 4 script again.** Expected: `OK`.
- [ ] **Step 3: Build the APK** through the editor script recipe. Use `BuildPipeline.BuildPlayer` with scenes `LobbyScene, ExploreScene, ARPrototype, Sensor360Scene`, output `Builds/EnerGo-Radar.apk`, target Android. Schedule it with `EditorApplication.delayCall` and write `report.summary.result` to `Builds/radar-result.txt`, as done for `EnerGo-SolarClean.apk`. Expected: `Succeeded`, 0 errors.
- [ ] **Step 4: Hand the device checklist to the user.** Do not tick these items; the user tests outdoors:
  - The first radar open asks for the location permission. Denying it shows the "Izin lokasi ditolak" card, and MAIN MODE LATIHAN works.
  - Radar dots stay still while standing still. Walking 20 m moves them about the expected amount.
  - Turning the body rotates the radar smoothly, with no 360° spins at north. **This verifies the legacy `Input.compass` under `activeInputHandler: 1` on a real phone.**
  - TANGKAP turns orange at about 20 m from a spawn, then mini-game → quiz → LANJUT BERBURU, and the caught spawn is gone.
  - Two phones side by side show the same spawns.
  - At night there are no Matahari spawns.
  - Battery and heat after 15 minutes of hunting.
