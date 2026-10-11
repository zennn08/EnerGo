# Compass AR, Uranium v2, Guides and Timed Sun Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make the AR capture feel anchored to the real world, rework uranium into something clear and responsive, add per-SDA guide cards, and give the sun mini-game a random time limit.

**Architecture:**
- **Pure rules in `EnerGo.Core`, each with EditMode tests:** `CompassMath`, `SunMath`, `GuideBook`, and the v2 numbers in `UraniumMath`.
- **`CompassAnchor`:** a new MonoBehaviour. It reads the Android rotation vector and keeps a `North` transform rotated so that its +Z points to true north. Coal, gold, oil and uranium targets are parented to `North` at the radar bearing.
- **`GuideViewer`:** a plain C# class that draws the guide cards with IMGUI. It is shared by the AR scene (before the mini-game) and the lobby (a PANDUAN sheet).

**Tech Stack:** Unity 6, C#, Input System (`AndroidRotationVector`, `Accelerometer`, `EnhancedTouch`), IMGUI via `EnerGoUI`, NUnit EditMode, Unity MCP CLI.

**Spec:** `docs/superpowers/specs/2026-10-11-kompas-uranium2-panduan-design.md`

## Global Constraints

- **UI text:** player-facing text is Indonesian. HUD lines are ALL CAPS.
- **Glyphs:** never use glyphs Poppins lacks, such as emoji, `☢`, `←`, `→`, `✓` and `✗`. Allowed: `·`, `‹`, `›`, `&`, `!`, `%`, `°`, `?`.
- **Colours:** use the `EnerGoUI` palette tokens (Ink, Orange, Paper, Line, Success, Danger, Muted, Sun). The uranium glow is `#39FF14`.
- **Tuning:** every tuning constant sits at the top of its `*Math` class.
- **Sun stays as it is today** (in front of the player, drifting), except for the new timer. Coal and gold mechanics and visuals are unchanged: only their spawn centre moves to the bearing.
- **Assets:** keep the placeholder primitives. All 3D assets will be redone later via Blender MCP, so do not polish models.
- **"Jangan ingatkan lagi":** stored in `PlayerPrefs` as `EnerGo.Guide.Skip.<id>` = 1, never in the save file.
- **Commits:** do not commit the unrelated working-tree changes (`Assets/Plugins/NuGet/*`, `Packages/*`, `ProjectSettings/QualitySettings.asset`, `ProjectSettings/ProjectSettings.asset`, `.claude/`, `.mcp.json`). Use `git add` with explicit paths only. Commit trailer: `Co-Authored-By:` naming the model that wrote the commit.

## Review Focus

1. **No compass (editor, or a phone without the sensor).**
   - Expected: nothing breaks. The strip shows "KOMPAS TIDAK TERSEDIA", spawns wait at most 1.5 s, and North stays at offset 0, so it behaves like today.
   - Check: Task 3 Step 6.
2. **Compass wrap-around near north.** A heading of 350° and a bearing of 10° must show the marker just right of centre, not on the far left. Offset smoothing across 359°→0° must not spin.
   - Tests: `CompassMathTests.StripWrapsAcrossNorth` and `SmoothOffsetTakesTheShortWay` (Task 1).
3. **Practice mode or a stale bearing.** Practice (no `PendingSpawnId`) must get a random bearing even if an old `PendingBearingDeg` is still set from an earlier radar capture.
   - Check: Task 3 (the Awake guard) and Task 3 Step 6.
4. **Sun at time-out with partial light.** At ≥ 50% exactly one quiz opens, with no repeated `CompleteCapture` every frame. Below 50% the sun respawns cleanly.
   - Tests: `SunMathTests` (Task 1). Check: Task 4 Step 4.
5. **Guide overlay vs. game input.**
   - While the guide is open, no SDA spawns and taps do not hit coal or gold.
   - A resumed pending quiz never shows the guide.
   - The uranium slider drag must not also rotate the gyro view.
   - Check: Task 6 Step 4 and Task 5 Step 4.

## How to run things (used by every task)

- **Compile:**
  1. `npx -y unity-mcp-cli run-tool assets-refresh --input "{}"`
  2. If test results look unchanged after editing `.cs` files, force recompilation with script-execute: `UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation()`. Wait, then run the tests. The `tests-run` output is the reliable compile check.
- **EditMode tests:** `npx -y unity-mcp-cli run-tool tests-run --input "{\"testMode\":\"EditMode\",\"testAssembly\":\"EnerGo.Tests.Editor\"}"`
  - Never run this in play mode.
  - If a stale `MCP_PendingTestRun` lock blocks it, first make sure the editor is not in play mode. Then, via script-execute, call `Tool_Tests.ClearActiveTestRun` by reflection (inspect its parameters first) and `UnityEditor.SessionState.EraseString("MCP_PendingTestRun")`.
  - Baseline on main: 62 tests pass.
- **Run C# in the editor:**
  1. Write `public class Script { public static object Main() { ... } }` to a scratch file.
  2. `python -c "import json,sys;print(json.dumps({'csharpCode':open(sys.argv[1],encoding='utf-8').read(),'isMethodBody':False}))" <file> | npx -y unity-mcp-cli run-tool script-execute --input-file -`
  3. In scripts, `Random` is ambiguous: write `UnityEngine.Random`.
- **Play-mode checks:**
  - The editor does not tick under MCP (`Time.time` stays at 0), so drive private members by reflection.
  - After a domain reload `ResourceIds.Selected` is reset to gold. Set the private `resourceId` field instead.
  - Back up `energo-demo-v1.json*` in `Application.persistentDataPath` before anything that writes the save, then restore it and call `EnerGoProgress.Load()`.
  - Exit play mode before running tests.
- **Screenshots:** `screenshot-game-view` does not include IMGUI.
  1. Use `ScreenCapture.CaptureScreenshot(path)`.
  2. Call `EditorApplication.QueuePlayerLoopUpdate()` and repaint the GameView until the file exists.
  3. Use the Game View size "EnerGo 1080x1920".
- **Scratch folder:** `.superpowers/sdd/2026-10-11-kompas-uranium2-panduan/scratch/` (git-ignored).

---

### Task 0: Branch

- [ ] **Step 1:** `git checkout main && git checkout -b kompas-uranium2`. Expected: the new branch, with the tooling changes still uncommitted.

---

### Task 1: Core rules — `CompassMath`, `SunMath`, `GuideBook`

**Files:**
- Create: `Assets/EnerGo/Scripts/Core/CompassMath.cs`, `Assets/EnerGo/Scripts/Core/SunMath.cs`, `Assets/EnerGo/Scripts/Core/GuideBook.cs`
- Test: `Assets/EnerGo/Tests/Editor/CompassMathTests.cs`, `SunMathTests.cs`, `GuideBookTests.cs`

**Interfaces (all `public static` in namespace `EnerGo`):**
- **`CompassMath`**
  - Constants: `StripHalfSpanDeg = 60`, `MaxCalibratePitchDeg = 60`, `OffsetSmoothing = 2`, `FirstFixTimeoutSec = 1.5`, `UraniumSectorHalfDeg = 45`.
  - `float CameraHeading(float cameraYawDeg, float offsetDeg)`
  - `float OffsetTarget(float compassHeadingDeg, float cameraYawDeg)`
  - `bool CanCalibrate(float cameraPitchDeg)`
  - `float SmoothOffset(float current, float target, float dt)`
  - `float StripX(float bearingDeg, float headingDeg, float halfSpanDeg)`
  - `float WorldYaw(float bearingDeg, float offsetDeg)`
  - `string DirectionLabel(int deg)`
- **`SunMath`**
  - Constants: `MinSec = 15`, `MaxSec = 25`, `MinShare = 0.5`, `FailShowSec = 1.5`.
  - `enum Outcome { None, Captured, TooLittle }`
  - `float SunTimeLimit(float u01)`
  - `Outcome SunOutcome(float progress, float timeLeft)`
  - `float SunPerformance(float progress)`
- **`GuideBook`**
  - `enum Art { Reticle, Compass, Tap, PhoneFlat, PhoneSwirl, Hold, Layers, PumpRing, RadMeter, Slider, Shake }`
  - `readonly struct Card { string Title; string Body; Art Art; }`
  - `string[] Ids`
  - `Card[] For(string resourceId)`, which returns an empty array for an unknown id

- [ ] **Step 1: Write the failing tests.**

`Assets/EnerGo/Tests/Editor/CompassMathTests.cs`:

```csharp
using NUnit.Framework;
using UnityEngine;

namespace EnerGo.Tests
{
    public class CompassMathTests
    {
        [Test]
        public void HeadingAndOffsetWrapAroundNorth()
        {
            Assert.AreEqual(10f, CompassMath.CameraHeading(350f, 20f), 1e-3f);
            Assert.AreEqual(20f, CompassMath.OffsetTarget(10f, 350f), 1e-3f);
            Assert.AreEqual(340f, CompassMath.OffsetTarget(330f, -10f), 1e-3f);
        }

        [Test]
        public void CalibratesOnlyWhenNotLookingStraightUpOrDown()
        {
            Assert.IsTrue(CompassMath.CanCalibrate(0f));
            Assert.IsTrue(CompassMath.CanCalibrate(60f));
            Assert.IsTrue(CompassMath.CanCalibrate(-60f));
            Assert.IsFalse(CompassMath.CanCalibrate(61f));
            Assert.IsFalse(CompassMath.CanCalibrate(-80f));
        }

        [Test]
        public void SmoothOffsetTakesTheShortWay()
        {
            float step = CompassMath.SmoothOffset(350f, 10f, 0.1f);
            float moved = Mathf.DeltaAngle(350f, step);
            Assert.Greater(moved, 0f);
            Assert.Less(moved, 20f);
            Assert.AreEqual(10f, Mathf.Repeat(CompassMath.SmoothOffset(350f, 10f, 100f), 360f), 0.01f);
        }

        [Test]
        public void StripCentreEdgesAndOutside()
        {
            Assert.AreEqual(0f, CompassMath.StripX(0f, 0f, 60f), 1e-4f);
            Assert.AreEqual(1f, CompassMath.StripX(60f, 0f, 60f), 1e-4f);
            Assert.AreEqual(-1f, CompassMath.StripX(300f, 0f, 60f), 1e-4f);
            Assert.Greater(CompassMath.StripX(90f, 0f, 60f), 1f);
        }

        [Test]
        public void StripWrapsAcrossNorth()
        {
            Assert.AreEqual(20f / 60f, CompassMath.StripX(10f, 350f, 60f), 1e-4f);  // just right of centre
            Assert.AreEqual(-20f / 60f, CompassMath.StripX(350f, 10f, 60f), 1e-4f); // just left
        }

        [Test]
        public void WorldYawIsBearingMinusOffset()
        {
            Assert.AreEqual(70f, CompassMath.WorldYaw(90f, 20f), 1e-4f);
            Assert.AreEqual(-10f, CompassMath.WorldYaw(10f, 20f), 1e-4f);
        }

        [Test]
        public void DirectionLabels()
        {
            string[] expect = { "U", "TL", "T", "TG", "S", "BD", "B", "BL", "U" };
            for (int i = 0; i < expect.Length; i++) Assert.AreEqual(expect[i], CompassMath.DirectionLabel(i * 45));
        }
    }
}
```

`Assets/EnerGo/Tests/Editor/SunMathTests.cs`:

```csharp
using NUnit.Framework;

namespace EnerGo.Tests
{
    public class SunMathTests
    {
        [Test]
        public void TimeLimitIsFifteenToTwentyFiveSeconds()
        {
            Assert.AreEqual(15f, SunMath.SunTimeLimit(0f), 1e-4f);
            Assert.AreEqual(25f, SunMath.SunTimeLimit(1f), 1e-4f);
            Assert.AreEqual(20f, SunMath.SunTimeLimit(0.5f), 1e-4f);
        }

        [Test]
        public void Outcomes()
        {
            Assert.AreEqual(SunMath.Outcome.Captured, SunMath.SunOutcome(1f, 5f));    // full bar ends early
            Assert.AreEqual(SunMath.Outcome.None, SunMath.SunOutcome(0.7f, 3f));      // still running
            Assert.AreEqual(SunMath.Outcome.Captured, SunMath.SunOutcome(0.5f, 0f));  // time up with exactly half
            Assert.AreEqual(SunMath.Outcome.TooLittle, SunMath.SunOutcome(0.49f, 0f));
            Assert.AreEqual(SunMath.Outcome.TooLittle, SunMath.SunOutcome(0.2f, -1f));
        }

        [Test]
        public void PerformanceMatchesBonusTiers()
        {
            // EnerGoProgress.BonusFor: >= 0.75 = Hebat (+2), >= 0.4 = Bagus (+1).
            Assert.AreEqual(0f, SunMath.SunPerformance(0.5f), 1e-4f);
            Assert.AreEqual(0.4f, SunMath.SunPerformance(0.7f), 1e-4f);
            Assert.AreEqual(0.75f, SunMath.SunPerformance(0.875f), 1e-4f);
            Assert.AreEqual(1f, SunMath.SunPerformance(1f), 1e-4f);
        }
    }
}
```

`Assets/EnerGo/Tests/Editor/GuideBookTests.cs`:

```csharp
using NUnit.Framework;

namespace EnerGo.Tests
{
    public class GuideBookTests
    {
        static readonly string[] BadGlyphs = { "☢", "←", "→", "✓", "✗", "⚠", "★" };

        [Test]
        public void EveryPlayableResourceHasTwoOrThreeCards()
        {
            foreach (var id in GuideBook.Ids)
            {
                Assert.IsTrue(ResourceIds.IsPlayable(id), id);
                var cards = GuideBook.For(id);
                Assert.That(cards.Length, Is.InRange(2, 3), id);
                foreach (var c in cards)
                {
                    Assert.IsFalse(string.IsNullOrWhiteSpace(c.Title), id);
                    Assert.IsFalse(string.IsNullOrWhiteSpace(c.Body), id);
                    foreach (var g in BadGlyphs) { StringAssert.DoesNotContain(g, c.Title); StringAssert.DoesNotContain(g, c.Body); }
                    foreach (char ch in c.Title + c.Body) Assert.IsFalse(char.IsSurrogate(ch), "emoji in " + id);
                }
            }
            Assert.AreEqual(5, GuideBook.Ids.Length);
        }

        [Test]
        public void UnknownIdHasNoCards() => Assert.AreEqual(0, GuideBook.For("random").Length);
    }
}
```

- [ ] **Step 2: Run the tests.** Expected: a compile error, because `CompassMath`, `SunMath` and `GuideBook` do not exist.

- [ ] **Step 3: Implement.**

`Assets/EnerGo/Scripts/Core/CompassMath.cs`:

```csharp
using UnityEngine;

namespace EnerGo
{
    // Compass-anchored AR: angles are degrees, headings/bearings clockwise from north, yaws clockwise from Unity +Z.
    public static class CompassMath
    {
        public const float StripHalfSpanDeg = 60f;      // the HUD compass strip shows ±60° around the view direction
        public const float MaxCalibratePitchDeg = 60f;  // looking further up/down makes the phone compass unreliable
        public const float OffsetSmoothing = 2f;        // per second; slow so a magnetic hiccup doesn't move objects
        public const float FirstFixTimeoutSec = 1.5f;   // spawns wait this long for the first compass reading
        public const float UraniumSectorHalfDeg = 45f;  // uranium shows only a rough sector, not its exact bearing

        private static readonly string[] Labels = { "U", "TL", "T", "TG", "S", "BD", "B", "BL" };

        public static float CameraHeading(float cameraYawDeg, float offsetDeg) => Mathf.Repeat(cameraYawDeg + offsetDeg, 360f);

        public static float OffsetTarget(float compassHeadingDeg, float cameraYawDeg) => Mathf.Repeat(compassHeadingDeg - cameraYawDeg, 360f);

        public static bool CanCalibrate(float cameraPitchDeg) => Mathf.Abs(cameraPitchDeg) <= MaxCalibratePitchDeg;

        public static float SmoothOffset(float current, float target, float dt) =>
            Mathf.Repeat(Mathf.LerpAngle(current, target, 1f - Mathf.Exp(-OffsetSmoothing * dt)), 360f);

        // -1 = left edge, 0 = centre, +1 = right edge of the strip; beyond ±1 the marker is off the strip.
        public static float StripX(float bearingDeg, float headingDeg, float halfSpanDeg) =>
            Mathf.DeltaAngle(headingDeg, bearingDeg) / halfSpanDeg;

        // Unity world yaw of a compass bearing; the North transform is rotated by -offset so its +Z is true north.
        public static float WorldYaw(float bearingDeg, float offsetDeg) => bearingDeg - offsetDeg;

        public static string DirectionLabel(int deg) => Labels[((Mathf.RoundToInt(deg / 45f) % 8) + 8) % 8];
    }
}
```

`Assets/EnerGo/Scripts/Core/SunMath.cs`:

```csharp
using UnityEngine;

namespace EnerGo
{
    // Timed sun: collect as much light as possible before a random deadline; at least half the bar to succeed.
    public static class SunMath
    {
        public const float MinSec = 15f, MaxSec = 25f;
        public const float MinShare = 0.5f;
        public const float FailShowSec = 1.5f;

        public enum Outcome { None, Captured, TooLittle }

        public static float SunTimeLimit(float u01) => Mathf.Lerp(MinSec, MaxSec, Mathf.Clamp01(u01));

        public static Outcome SunOutcome(float progress, float timeLeft) =>
            progress >= 1f ? Outcome.Captured
            : timeLeft > 0f ? Outcome.None
            : progress >= MinShare ? Outcome.Captured : Outcome.TooLittle;

        // 50 % = 0 (Cukup) .. 100 % = 1 (Hebat), fed to EnerGoProgress.BonusFor.
        public static float SunPerformance(float progress) => Mathf.InverseLerp(MinShare, 1f, progress);
    }
}
```

`Assets/EnerGo/Scripts/Core/GuideBook.cs`:

```csharp
namespace EnerGo
{
    // Step-by-step guide cards shown before each SDA mini-game and from the lobby's PANDUAN sheet.
    public static class GuideBook
    {
        public enum Art { Reticle, Compass, Tap, PhoneFlat, PhoneSwirl, Hold, Layers, PumpRing, RadMeter, Slider, Shake }

        public readonly struct Card
        {
            public readonly string Title, Body;
            public readonly Art Art;
            public Card(string title, string body, Art art) { Title = title; Body = body; Art = art; }
        }

        public static readonly string[] Ids = { ResourceIds.Sun, ResourceIds.Coal, ResourceIds.Gold, ResourceIds.Oil, ResourceIds.Uranium };

        private static readonly Card Compass = new Card("Ikuti kompas", "Hadap ke titik oranye di pita kompas sampai SDA terlihat di kamera.", Art.Compass);

        public static Card[] For(string resourceId)
        {
            switch (resourceId)
            {
                case ResourceIds.Sun: return new[]
                {
                    new Card("Kejar cahaya", "Arahkan lingkaran di tengah layar ke cahaya matahari yang bergerak.", Art.Reticle),
                    new Card("Kumpulkan sebelum waktu habis", "Minimal 50% cahaya. Makin penuh barnya, makin banyak bonus.", Art.Reticle)
                };
                case ResourceIds.Coal: return new[]
                {
                    Compass,
                    new Card("Pecahkan bongkahan", "Ketuk tiap bongkahan batu bara berkali-kali sampai hancur.", Art.Tap)
                };
                case ResourceIds.Gold: return new[]
                {
                    Compass,
                    new Card("Letakkan HP datar", "Setelah endapan diketuk, letakkan HP datar sampai hitungan mulai.", Art.PhoneFlat),
                    new Card("Putar pelan seperti mendulang", "Putar HP perlahan. Terlalu kencang, emasnya tumpah.", Art.PhoneSwirl)
                };
                case ResourceIds.Oil: return new[]
                {
                    new Card("Tahan untuk mengebor", "Tahan layar untuk mengebor ke bawah. Bor tidak langsung berhenti saat dilepas.", Art.Hold),
                    new Card("Lepas di lapisan minyak", "Pelan-pelan di kantong gas, lalu lepas tepat di lapisan minyak.", Art.Layers),
                    new Card("Pompa ikuti lingkaran", "Tekan saat lingkaran mengecil pas di target. Terlalu cepat, tekanan naik.", Art.PumpRing)
                };
                case ResourceIds.Uranium: return new[]
                {
                    new Card("Ikuti sinyal RAD", "Hadap ke sektor hijau di kompas, lalu putar pelan sampai angka RAD paling tinggi.", Art.RadMeter),
                    new Card("Geser batang kendali", "Geser batang di kanan layar untuk menjaga jarum reaktor di zona hijau.", Art.Slider),
                    new Card("Pegang HP stabil", "Guncangan menaikkan reaksi. Waspadai lonjakan neutron, dosis terus naik.", Art.Shake)
                };
                default: return new Card[0];
            }
        }
    }
}
```

- [ ] **Step 4: Run the tests.** Expected: 62 + 12 = 74 pass.

- [ ] **Step 5: Commit** the 3 classes, 3 test files and their 6 `.meta` files, with the message `feat: CompassMath, SunMath and GuideBook core rules with tests`.

---

### Task 2: `UraniumMath` v2 (regular Geiger, trend, slider, friendlier numbers)

**Files:**
- Modify: `Assets/EnerGo/Scripts/Core/UraniumMath.cs` (full replacement below)
- Modify: `Assets/EnerGo/Tests/Editor/UraniumMathTests.cs` (full replacement below)

**Interfaces:**
- Removed: `HideMinYawDeg`, `HideMaxYawDeg`, `RodRangeDeg`, `MinClicksPerSec`, `GeigerRate`, `NextClickDelay`, `TiltDeg`, `RodFromTilt`. After this task, `ARCapturePrototype.Uranium.cs` will NOT compile until Task 5. To keep every commit compiling, do Step 5 below.
- Added: `GeigerInterval(float angleDeg)`, `Trend(int prevMeter, int meter)`, `RodFromSlider(float pointerY01)`, `TrendThreshold`, `TrendWindowSec`.
- Kept: `Meter`, `SecureClickRate`, `StepReactivity`, `InGreen`, `StepProgress`, `StepDose`, `NextSpikeDelay`, `Outcome`, `Check`, `Performance`, `MaxPulsesPerSec`, `HideMinPitchDeg`/`HideMaxPitchDeg`, `RevealAngleDeg`/`RevealHoldSec`, `SpikeSize`, `SpikeWarnSec`, `NoiseGain`, `ParSec`, `FailShowSec`, `GreenMin`/`GreenMax`/`HotAbove`.

- [ ] **Step 1: Replace the test file** `Assets/EnerGo/Tests/Editor/UraniumMathTests.cs`:

```csharp
using NUnit.Framework;

namespace EnerGo.Tests
{
    public class UraniumMathTests
    {
        [Test]
        public void GeigerTicksFasterTheCloserTheAim()
        {
            Assert.AreEqual(1f / 20f, UraniumMath.GeigerInterval(0f), 1e-4f);
            Assert.AreEqual(1f / 2f, UraniumMath.GeigerInterval(150f), 1e-4f);
            Assert.AreEqual(1f / 2f, UraniumMath.GeigerInterval(180f), 1e-4f);
            float prev = 0f;
            for (float a = 0f; a <= 180f; a += 10f)
            {
                float gap = UraniumMath.GeigerInterval(a);
                Assert.GreaterOrEqual(gap, prev);
                prev = gap;
            }
        }

        [Test]
        public void MeterShowsHundredWhenAimedStraightAtTheOre()
        {
            Assert.AreEqual(100, UraniumMath.Meter(0f));
            Assert.AreEqual(50, UraniumMath.Meter(90f));
            Assert.AreEqual(0, UraniumMath.Meter(180f));
        }

        [Test]
        public void TrendNeedsThreePoints()
        {
            Assert.AreEqual(1, UraniumMath.Trend(40, 43));
            Assert.AreEqual(-1, UraniumMath.Trend(43, 40));
            Assert.AreEqual(0, UraniumMath.Trend(40, 42));
            Assert.AreEqual(0, UraniumMath.Trend(42, 40));
        }

        [Test]
        public void SliderTopPullsTheRodOutBottomPushesItIn()
        {
            Assert.AreEqual(0f, UraniumMath.RodFromSlider(0f), 1e-4f);
            Assert.AreEqual(1f, UraniumMath.RodFromSlider(1f), 1e-4f);
            Assert.AreEqual(0.25f, UraniumMath.RodFromSlider(0.25f), 1e-4f);
            Assert.AreEqual(0f, UraniumMath.RodFromSlider(-0.3f), 1e-4f);
            Assert.AreEqual(1f, UraniumMath.RodFromSlider(1.4f), 1e-4f);
        }

        [Test]
        public void HalfRodWithoutDisturbanceSettlesInTheGreenZone()
        {
            float r = 0.5f;
            for (int i = 0; i < 100; i++) r = UraniumMath.StepReactivity(r, 0.5f, 0f, 0f, 0f, 0.1f);
            Assert.IsTrue(UraniumMath.InGreen(r), "r = " + r);
        }

        [Test]
        public void FullRodCoolsWithinHalfASecond()
        {
            float r = 0.8f;
            for (int i = 0; i < 5; i++) r = UraniumMath.StepReactivity(r, 1f, 0f, 0f, 0f, 0.1f);
            Assert.Less(r, UraniumMath.GreenMin, "rod response must be visible in under a second");
        }

        [Test]
        public void PulledRodHeatsUp()
        {
            float r = 0.5f;
            for (int i = 0; i < 10; i++) r = UraniumMath.StepReactivity(r, 0f, 0f, 0f, 0f, 0.1f);
            Assert.Greater(r, UraniumMath.HotAbove);
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
        public void WiderGreenZoneAndFasterProgress()
        {
            Assert.IsTrue(UraniumMath.InGreen(0.35f));
            Assert.IsTrue(UraniumMath.InGreen(0.65f));
            Assert.IsFalse(UraniumMath.InGreen(0.34f));
            Assert.AreEqual(1f / 6f, UraniumMath.StepProgress(0f, 0.5f, 1f), 1e-4f);
            Assert.AreEqual(0.5f - 1f / 12f, UraniumMath.StepProgress(0.5f, 0.9f, 1f), 1e-4f);
        }

        [Test]
        public void DoseBudgetIsFortyFiveSecondsDoubledWhenHot()
        {
            Assert.AreEqual(1f / 45f, UraniumMath.StepDose(0f, 0.5f, 1f), 1e-4f);
            Assert.AreEqual(2f / 45f, UraniumMath.StepDose(0f, 0.9f, 1f), 1e-4f);
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
        public void SpikesComeEveryFourToSevenSecondsWithLongerWarning()
        {
            Assert.AreEqual(4f, UraniumMath.NextSpikeDelay(0f), 1e-4f);
            Assert.AreEqual(7f, UraniumMath.NextSpikeDelay(1f), 1e-4f);
            Assert.AreEqual(0.8f, UraniumMath.SpikeWarnSec, 1e-4f);
        }

        [Test]
        public void RevealIsMoreForgiving()
        {
            Assert.AreEqual(20f, UraniumMath.RevealAngleDeg, 1e-4f);
            Assert.AreEqual(0.8f, UraniumMath.RevealHoldSec, 1e-4f);
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

- [ ] **Step 2: Run the tests.** Expected: a compile error (`GeigerInterval`, `Trend` and `RodFromSlider` do not exist).

- [ ] **Step 3: Replace** `Assets/EnerGo/Scripts/Core/UraniumMath.cs`:

```csharp
using UnityEngine;

namespace EnerGo
{
    // Pure rules of the uranium "Pemburu Geiger" mini-game (v2). r = reactivity 0..1, rod = control rod 0 (out) .. 1 (fully in).
    public static class UraniumMath
    {
        // Search: the ore sits at the radar bearing, a little above or below eye level.
        public const float HideMinPitchDeg = -5f, HideMaxPitchDeg = 15f;
        public const float RevealAngleDeg = 20f, RevealHoldSec = 0.8f;
        public const float FarClicksPerSec = 2f, NearClicksPerSec = 20f; // regular ticks so the change is easy to hear
        public const float MaxPulsesPerSec = 12f;   // vibration pulses blur together faster than this
        public const int TrendThreshold = 3;        // RAD points per window that count as "menguat"/"melemah"
        public const float TrendWindowSec = 0.3f;
        // Secure: reactivity.
        public const float GreenMin = 0.35f, GreenMax = 0.65f, HotAbove = 0.80f;
        public const float Growth = 0.05f;          // per second: an unchecked chain reaction creeps up
        public const float RodPull = 3.0f;          // per second: r is pulled toward (1 - rod) — fast, so the slider feels direct
        public const float NoiseGain = 0.25f;       // per second, Perlin noise in [-1, 1] times this
        public const float JoltDeadzone = 0.08f;    // accelerometer jitter below this is not a shake
        public const float JoltGain = 2.0f;         // per second per unit of jolt above the deadzone
        public const float SpikeSize = 0.20f;       // neutron spike, added at once
        public const float SpikeMinSec = 4f, SpikeMaxSec = 7f, SpikeWarnSec = 0.8f;
        public const float FillPerSec = 1f / 6f, DrainPerSec = 1f / 12f;
        public const float DosePerSec = 1f / 45f, HotDoseMul = 2f;
        public const float ParSec = 18f;            // a good secure phase, for the speed score
        public const float FailShowSec = 1.5f;

        // Seconds between Geiger clicks: 1/20 s straight at the ore, 1/2 s at 150° or more away.
        public static float GeigerInterval(float angleDeg) =>
            1f / Mathf.Lerp(NearClicksPerSec, FarClicksPerSec, Mathf.Clamp01(angleDeg / 150f));

        // While securing, the counter follows the reactivity instead of the aim.
        public static float SecureClickRate(float r) => Mathf.Lerp(4f, NearClicksPerSec, Mathf.Clamp01(r));

        public static int Meter(float angleDeg) => Mathf.RoundToInt(100f * (1f - Mathf.Clamp(angleDeg, 0f, 180f) / 180f));

        // +1 = signal getting stronger, -1 = weaker, 0 = about the same.
        public static int Trend(int prevMeter, int meter) =>
            meter - prevMeter >= TrendThreshold ? 1 : prevMeter - meter >= TrendThreshold ? -1 : 0;

        // Finger position on the slider track, 0 = top (rod out) .. 1 = bottom (rod fully in).
        public static float RodFromSlider(float pointerY01) => Mathf.Clamp01(pointerY01);

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

- [ ] **Step 4: Keep the game compiling.** `ARCapturePrototype.Uranium.cs` still calls the removed members. Make these minimal edits; Task 5 rewrites the file anyway.
  - In `SpawnUranium`, replace `Random.Range(UraniumMath.HideMinYawDeg, UraniumMath.HideMaxYawDeg)` with `Random.Range(60f, 150f)`.
  - In `UpdateUranium`, change `TickGeiger(UraniumMath.GeigerRate(uraniumAngle));` to `TickGeiger(1f / UraniumMath.GeigerInterval(uraniumAngle));`.
  - In `TickGeiger`, change `uraniumNextClickAt = Time.time + UraniumMath.NextClickDelay(rate, Random.value);` to `uraniumNextClickAt = Time.time + 1f / Mathf.Max(rate, 0.01f);`.
  - In `ReadUraniumRod`, replace the two lines using `TiltDeg`/`RodFromTilt` with `uraniumRod = Mathf.Clamp01(uraniumRod);`, so the rod stays where it is; Task 5 adds the slider.

- [ ] **Step 5: Compile and run the tests.** Expected: 74 − 14 old uranium tests + 15 new = 75 pass, with no compile errors.

- [ ] **Step 6: Commit** `UraniumMath.cs`, `UraniumMathTests.cs` and `ARCapturePrototype.Uranium.cs`, with the message `feat: UraniumMath v2 — regular Geiger ticks, RAD trend, slider rod, friendlier numbers`.

---

### Task 3: `CompassAnchor`, radar bearing hand-off, spawns at the bearing, compass strip

**Files:**
- Create: `Assets/EnerGo/Scripts/CompassAnchor.cs`
- Modify: `Assets/EnerGo/Scripts/Core/ExploreSession.cs`
- Modify: `Assets/EnerGo/Scripts/RadarController.cs` (`Capture`, around line 270)
- Modify: `Assets/EnerGo/Scripts/ARCapturePrototype.cs` (Awake, the Update spawn condition, `SpawnCoalField`, `SpawnGold`, `DrawRadarHUD`, `OnGUI`)
- Modify: `Assets/EnerGo/Scripts/ARCapturePrototype.Oil.cs` (`SpawnOil` placement)
- Modify: `Assets/EnerGo/Scripts/ARCapturePrototype.Uranium.cs` (`SpawnUranium` placement)

**Interfaces:**
- Consumes: `CompassMath` (Task 1), and `GeoSpawner.HeadingFromRotation` and `GeoSpawner.BearingDeg` (existing).
- Produces:
  - `CompassAnchor` with:
    - `public Camera cam`
    - `Transform North`
    - `float OffsetDeg`
    - `bool HasCompass`
    - `bool ReadyToPlace`
    - `float HeadingDeg`
    - `Vector3 Direction(float bearingDeg, float pitchDeg)` (world unit vector)
  - `ExploreSession.PendingBearingDeg` (`float?`)
  - in `ARCapturePrototype`:
    - the field `compass`
    - the field `captureBearing` (float, degrees)
    - `DrawCompassStrip(float sw, float y)`
    - `bool CompassMarkerVisible`, which is false for the sun

- [ ] **Step 1: Create** `Assets/EnerGo/Scripts/CompassAnchor.cs`

```csharp
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Android;

namespace EnerGo
{
    // Keeps a "North" transform whose +Z points to true north, by comparing the Android rotation-vector compass
    // (same sensor as the radar) with the AR camera's yaw. SDA targets parented to North stay at their real bearing.
    public class CompassAnchor : MonoBehaviour
    {
        public Camera cam;
        public Transform North { get; private set; }
        public float OffsetDeg { get; private set; }  // compass heading - camera yaw
        public bool HasCompass { get; private set; }
        public bool ReadyToPlace => calibrated || Time.time - startedAt >= CompassMath.FirstFixTimeoutSec;
        public float HeadingDeg => cam == null ? 0f : CompassMath.CameraHeading(Yaw(cam.transform.forward), OffsetDeg);

        private AttitudeSensor rotationSensor;
        private bool calibrated;
        private float startedAt, lastReadingAt = -10f;

        private static float Yaw(Vector3 f) => Mathf.Atan2(f.x, f.z) * Mathf.Rad2Deg;

        private void Awake()
        {
            North = new GameObject("North").transform;
            startedAt = Time.time;
        }

        private void OnEnable()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            rotationSensor = InputSystem.GetDevice<AndroidRotationVector>();
#endif
            if (rotationSensor != null && !rotationSensor.enabled) InputSystem.EnableDevice(rotationSensor);
        }

        private void OnDisable()
        {
            if (rotationSensor != null && rotationSensor.enabled) InputSystem.DisableDevice(rotationSensor);
        }

        private void Start()
        {
            if (cam != null) North.position = cam.transform.position; // rotation pivot = where the player stands
        }

        // LateUpdate: after ARCapturePrototype.Update has turned the camera this frame.
        private void LateUpdate()
        {
            if (cam == null) return;
            if (rotationSensor != null && rotationSensor.enabled && rotationSensor.lastUpdateTime > lastSensorStamp)
            {
                lastSensorStamp = rotationSensor.lastUpdateTime;
                Vector3 f = cam.transform.forward;
                float pitch = Mathf.Asin(Mathf.Clamp(f.y, -1f, 1f)) * Mathf.Rad2Deg;
                if (CompassMath.CanCalibrate(pitch))
                {
                    float heading = GeoSpawner.HeadingFromRotation(rotationSensor.attitude.ReadValue());
                    float target = CompassMath.OffsetTarget(heading, Yaw(f));
                    OffsetDeg = calibrated ? CompassMath.SmoothOffset(OffsetDeg, target, Time.deltaTime) : target; // first fix: snap
                    calibrated = true;
                }
                lastReadingAt = Time.time;
            }
            HasCompass = Time.time - lastReadingAt < 2f;
            North.rotation = Quaternion.Euler(0f, -OffsetDeg, 0f);
        }
        private double lastSensorStamp;

        // World direction of a compass bearing, tilted up by pitchDeg.
        public Vector3 Direction(float bearingDeg, float pitchDeg) =>
            Quaternion.Euler(-pitchDeg, CompassMath.WorldYaw(bearingDeg, OffsetDeg), 0f) * Vector3.forward;
    }
}
```

Check that `AndroidRotationVector` resolves with `using UnityEngine.InputSystem.Android;`. Read `LocationFeed.cs`'s `using` lines and copy whatever it uses.

- [ ] **Step 2: Hand off the bearing.**
  1. In `ExploreSession.cs`, add next to `PendingSpawnId`:
     ```csharp
             public static float? PendingBearingDeg; // compass bearing player -> spawn when the capture started (radar only)
     ```
  2. In `RadarController.Capture(GeoSpawner.Spawn s)`, after `ExploreSession.PendingSpawnId = s.Id;`, add:
     ```csharp
                 ExploreSession.PendingBearingDeg = (float)GeoSpawner.BearingDeg(feed.Lat, feed.Lon, s.Lat, s.Lon);
     ```
     Check that the radar's location-feed field is named `feed`, as it is in `DrawSelected`.

- [ ] **Step 3: Wire the anchor into `ARCapturePrototype.cs`.**
  1. Add the fields, next to `private string resourceId;`:
     ```csharp
             private CompassAnchor compass;
             private float captureBearing; // compass bearing of this capture's SDA (radar), random in practice mode
     ```
  2. In `Awake`, after the `resourceId` lines, add:
     ```csharp
                 compass = gameObject.AddComponent<CompassAnchor>();
                 compass.cam = arCamera;
                 // A bearing only counts together with a radar spawn id; practice mode (or a stale bearing) gets a random one.
                 captureBearing = ExploreSession.PendingSpawnId != null && ExploreSession.PendingBearingDeg.HasValue
                     ? ExploreSession.PendingBearingDeg.Value : Random.Range(0f, 360f);
     ```
  3. In `Update`'s spawn condition, change `target == null && Time.time >= nextSpawnTime` to `target == null && Time.time >= nextSpawnTime && compass.ReadyToPlace`.
  4. Add a helper next to `RadarTargetPosition`:
     ```csharp
             // World point at this capture's bearing (plus a yaw offset), tilted by pitch, `dist` metres from the camera.
             private Vector3 AtBearing(float yawOffsetDeg, float pitchDeg, float dist) =>
                 arCamera.transform.position + compass.Direction(captureBearing + yawOffsetDeg, pitchDeg) * dist;
     ```

- [ ] **Step 4: Spawn at the bearing.**
  - **Coal (`SpawnCoalField`):** replace the two lines
    ```csharp
                Vector3 fwd = arCamera.transform.forward;
                float baseYaw = Mathf.Atan2(fwd.x, fwd.z) * Mathf.Rad2Deg;
    ```
    with `float baseYaw = CompassMath.WorldYaw(captureBearing, compass.OffsetDeg);`.

    Then change `float centreYaw = baseYaw + Random.Range(-35f, 35f);` to `float centreYaw = baseYaw + Random.Range(-10f, 10f);`, so the field is centred on the marker.

    After the lump loop (before `feedback = …`), add `target.transform.SetParent(compass.North, true);`.
  - **Gold (`SpawnGold`):** replace the whole `if (!isRandomAngle) { … } else { … }` placement block with:
    ```csharp
                // Gold sits at the radar bearing; after a spilled pan the next deposit hides a little to the side.
                float side = isRandomAngle ? Random.Range(25f, 50f) * (Random.value < 0.5f ? -1f : 1f) : 0f;
                target.transform.position = AtBearing(side, Random.Range(-12f, 10f), Random.Range(1.4f, 2.0f));
                target.transform.rotation = Quaternion.LookRotation(arCamera.transform.position - target.transform.position, Vector3.up);
    ```
    Directly after the `if (resourceId == ResourceIds.Sun) { … }` block, add `if (resourceId != ResourceIds.Sun) target.transform.SetParent(compass.North, true);`.

    The sun block itself is untouched: it still sets `sunBaseYaw` from the camera, and `UpdateSun` keeps positioning the sun in world space every frame.
  - **Oil (`ARCapturePrototype.Oil.cs`, `SpawnOil`):** replace the 6 lines from `Vector3 fwd = arCamera.transform.forward;` to the `target.transform.rotation = …` line with:
    ```csharp
                Vector3 fwd = compass.Direction(captureBearing, 0f);
                target.transform.position = arCamera.transform.position + fwd * 1.4f + Vector3.up * 0.05f;
                target.transform.rotation = Quaternion.LookRotation(fwd, Vector3.up); // beam runs left-right, seen side-on
                target.transform.SetParent(compass.North, true);
    ```
  - **Uranium (`ARCapturePrototype.Uranium.cs`, `SpawnUranium`):** replace the 6 lines from `Vector3 fwd = arCamera.transform.forward;` to `target.transform.position = …` with:
    ```csharp
                float pitch = Random.Range(UraniumMath.HideMinPitchDeg, UraniumMath.HideMaxPitchDeg);
                target.transform.position = AtBearing(0f, pitch, UraniumOreDistance);
                target.transform.SetParent(compass.North, true);
    ```
    Keep `target.SetActive(false);` after it.

- [ ] **Step 5: Compass strip.**
  1. In `DrawRadarHUD`, change `if (Mathf.Abs(signedAngle) > 28f)` to `if (resourceId == ResourceIds.Sun && Mathf.Abs(signedAngle) > 28f)`. The strip now does the pointing for the other SDAs.
  2. In `OnGUI`, right after `DrawRadarHUD(sw, safeTop + pillH + 10f);`, add:
     ```csharp
                 if (EnerGoProgress.Current.pendingCapture == null) DrawCompassStrip(sw, safeTop + pillH + 10f + 72f);
     ```
  3. Add these members to `ARCapturePrototype.cs`, near `DrawHudBar`:

```csharp
        private GUIStyle stripLabelStyle;

        // HUD compass: ticks every 15°, U/TL/T/... every 45°, the view direction in the centre, and the SDA marker
        // (a dot, or for uranium only a rough green sector). Off-strip markers show "‹ 75°" / "75° ›" at the edge.
        private void DrawCompassStrip(float sw, float y)
        {
            if (compass == null || arCamera == null) return;
            if (stripLabelStyle == null) stripLabelStyle = new GUIStyle(styleTapsCounter) { alignment = TextAnchor.MiddleCenter };
            var r = new Rect(16f, y, sw - 32f, 34f);
            EnerGoUI.DrawCard(r, EnerGoUI.Radius, HudOutline, 1.5f);
            float half = CompassMath.StripHalfSpanDeg, heading = compass.HeadingDeg;
            float X(float x01) => r.center.x + x01 * (r.width * 0.5f - 14f);

            if (!compass.HasCompass)
                GUI.Label(new Rect(r.x, r.y, r.width, r.height), "KOMPAS TIDAK TERSEDIA", stripLabelStyle);
            else
                for (int d = 0; d < 360; d += 15)
                {
                    float x01 = CompassMath.StripX(d, heading, half);
                    if (Mathf.Abs(x01) > 1f) continue;
                    if (d % 45 == 0) GUI.Label(new Rect(X(x01) - 16f, r.y + 2f, 32f, r.height - 4f), CompassMath.DirectionLabel(d), stripLabelStyle);
                    else EnerGoUI.DrawPanel(new Rect(X(x01) - 1f, r.y + 12f, 2f, 10f), EnerGoUI.Line, Color.clear, 1f);
                }

            if (resourceId == ResourceIds.Uranium)
            {
                float c = CompassMath.StripX(captureBearing, heading, half), w = CompassMath.UraniumSectorHalfDeg / half;
                float lo = Mathf.Max(-1f, c - w), hi = Mathf.Min(1f, c + w);
                if (hi > lo) EnerGoUI.DrawPanel(new Rect(X(lo), r.y + 4f, X(hi) - X(lo), r.height - 8f), EnerGoUI.Alpha(EnerGoUI.Success, 0.3f), Color.clear, 6f);
                else StripEdge(r, c, Mathf.Abs(Mathf.DeltaAngle(heading, captureBearing)) - CompassMath.UraniumSectorHalfDeg);
            }
            else if (resourceId != ResourceIds.Sun && target != null)
            {
                Vector3 to = RadarTargetPosition() - arCamera.transform.position;
                float bearing = CompassMath.CameraHeading(Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg, compass.OffsetDeg);
                float x01 = CompassMath.StripX(bearing, heading, half);
                if (Mathf.Abs(x01) <= 1f) EnerGoUI.DrawPanel(new Rect(X(x01) - 6f, r.center.y - 6f, 12f, 12f), EnerGoUI.Orange, EnerGoUI.Ink, 6f, 1.5f);
                else StripEdge(r, x01, Mathf.Abs(Mathf.DeltaAngle(heading, bearing)));
            }
            EnerGoUI.DrawPanel(new Rect(r.center.x - 1f, r.y + 2f, 2f, r.height - 4f), EnerGoUI.Ink, Color.clear, 1f); // view direction
        }

        private void StripEdge(Rect r, float x01, float deg)
        {
            string t = x01 < 0f ? "‹ " + Mathf.RoundToInt(deg) + "°" : Mathf.RoundToInt(deg) + "° ›";
            var cell = x01 < 0f ? new Rect(r.x + 4f, r.y, 64f, r.height) : new Rect(r.xMax - 68f, r.y, 64f, r.height);
            EnerGoUI.DrawPanel(new Rect(cell.x, r.y + 5f, cell.width, r.height - 10f), EnerGoUI.Orange, Color.clear, 8f);
            GUI.Label(cell, t, stripLabelStyle);
        }
```

  The uranium overlay gauges start at `sh * 0.32`. Make sure the strip (y ≈ safeTop + 126 to + 160) does not overlap them at sh = 800; it ends at about y = 172 < 256. If the uranium or oil HUD card is 64 tall, the strip sits 8 px below it.

- [ ] **Step 6: Compile, run the tests (75 pass), then check in the editor by reflection.**
  1. **Coal at a bearing.** Enter play mode in `ARPrototype` and set `resourceId = "coal"`. Set the private `captureBearing = 90f`. Wait for or force `compass.ReadyToPlace` (no compass in the editor: it becomes ready after 1.5 s of play time; under MCP `Time.time` is frozen, so set the private `startedAt` to −10 by reflection). Call `SpawnGold(false)`. Assert:
     - the target's parent is the `North` transform
     - the coal field's lumps lie around world yaw 90 ± 85° (offset 0 in the editor)
  2. **No compass.** Assert `compass.HasCompass == false`. Take a screenshot to confirm the strip shows "KOMPAS TIDAK TERSEDIA" and the orange edge tag. Save it as `scratch/strip_coal.png`.
  3. **Oil and uranium.** Do the same for oil (`SpawnOil` puts the pump jack at yaw 90 under `North`) and uranium (the ore is under `North` and inactive; the strip shows the green sector, or the edge tag).
  4. **Rotation.** Rotate `North` by 30° by reflection (set `OffsetDeg` through its backing field, or just rotate the transform) and assert that the child's world yaw changed by −30°.
  5. **Practice mode.** Set `ExploreSession.PendingSpawnId = null` and `PendingBearingDeg = 45`, then reload the scene. Assert that `captureBearing` is in 0..360 and was not forced to 45 (run it twice; a match both times is a failure).
  6. Exit play mode.

- [ ] **Step 7: Commit** `CompassAnchor.cs` (+ `.meta`), `ExploreSession.cs`, `RadarController.cs`, `ARCapturePrototype.cs`, `ARCapturePrototype.Oil.cs` and `ARCapturePrototype.Uranium.cs`, with the message `feat: compass-anchored AR — SDA at the radar bearing, HUD compass strip`.

---

### Task 4: Timed sun

**Files:**
- Modify: `Assets/EnerGo/Scripts/ARCapturePrototype.cs` (sun fields, the sun branch of `SpawnGold`, `UpdateSun`, the sun HUD lines in `DrawRadarHUD`)

**Interfaces:**
- Consumes: `SunMath` (Task 1).

- [ ] **Step 1: Fields.** Next to `private float sunSpawnTime;`, add:
  ```csharp
          private float sunTimeLimit = SunMath.MaxSec; // random per sun, SunMath.MinSec..MaxSec
          private bool sunDone;                        // time is up or the bar is full: stop updating, the quiz is opening
  ```
  Delete `private const float SunParSec = 10f;` and its comment, since the speed score is no longer used for the sun.

- [ ] **Step 2: Spawn.** In `SpawnGold`'s `if (resourceId == ResourceIds.Sun)` block, after `sunProgress = 0f;`, add:
  ```csharp
                  sunTimeLimit = SunMath.SunTimeLimit(Random.value);
                  sunDone = false;
  ```

- [ ] **Step 3: `UpdateSun`.**
  1. As the first line, after the `arCamera == null` check, add `if (sunDone) return;`.
  2. Replace the final block
     ```csharp
                 if (sunProgress >= 1f)
                 {
                     capturePerformance = SpeedScore(Time.time - sunSpawnTime, SunParSec);
                     if (!CompleteCapture()) sunProgress = 0.95f;
                 }
     ```
     with:
     ```csharp
                 switch (SunMath.SunOutcome(sunProgress, SunTimeLeft()))
                 {
                     case SunMath.Outcome.Captured:
                         // Full bar = Hebat; at time-out the share above 50 % decides the bonus. finishAt opens the quiz once (and retries on a save error).
                         capturePerformance = sunProgress >= 1f ? 1f : SunMath.SunPerformance(sunProgress);
                         sunDone = true;
                         finishAt = Time.time;
                         break;
                     case SunMath.Outcome.TooLittle:
                         if (SFXManager.Instance != null) { SFXManager.Instance.StopMatahari(); SFXManager.Instance.StopEkstrakMatahari(); }
                         Destroy(target);
                         target = null;
                         nextSpawnTime = Time.time + SunMath.FailShowSec; // HUD shows `feedback` while there is no target
                         feedback = "CAHAYA TERLALU SEDIKIT";
                         break;
                 }
     ```
  3. Add the helper next to `StepSunProgress`:
     ```csharp
             private float SunTimeLeft() => sunTimeLimit - (Time.time - sunSpawnTime);
     ```

- [ ] **Step 4: HUD.** In `DrawRadarHUD`'s `else if (resourceId == ResourceIds.Sun)` branch, replace the three lines with:
  ```csharp
                      int left = Mathf.Max(0, Mathf.CeilToInt(SunTimeLeft()));
                      line1 = (sunInside ? "CAHAYA TERKUNCI" : "ARAHKAN KE CAHAYA") + " · SISA " + left + " DTK";
                      bool hurry = left <= 5 || !sunInside;
                      style1 = hurry ? styleRadarAlert : styleRadarLocked;
                      accent = hurry ? HudWarn : HudOk;
  ```
  Keep `locked = true;`, which follows these lines.

- [ ] **Step 5: Compile and run the tests (75 pass). Then check in the editor by reflection, with the save backed up.**
  1. Set `resourceId = "sun"` and spawn.
  2. Set `sunSpawnTime = -100` so the time is up, set `sunProgress = 0.6f`, and call `UpdateSun()` twice. Assert:
     - `sunDone == true` and `finishAt` is set
     - `capturePerformance` ≈ 0.2
     - a second call changes nothing
  3. Call `CompleteCapture()` and assert it returns true. Restore the save.
  4. Spawn again with `sunProgress = 0.3f` and the time up, then call `UpdateSun()`. Assert `target == null` and `feedback == "CAHAYA TERLALU SEDIKIT"`.
  5. Take a screenshot of the HUD showing "SISA … DTK" (`scratch/sun_timer.png`).

- [ ] **Step 6: Commit** `ARCapturePrototype.cs` with the message `feat: timed sun — 15-25 s, at least 50 % light, bonus by share`.

---

### Task 5: Uranium v2 in the AR scene (RAD meter, trend, reticle, slider)

**Files:**
- Modify: `Assets/EnerGo/Scripts/ARCapturePrototype.Uranium.cs`
- Modify: `Assets/EnerGo/Scripts/ARCapturePrototype.cs` (`HandleInputs`: no look-drag while securing)

**Interfaces:**
- Consumes: `UraniumMath` v2 (Task 2), and `compass` and `captureBearing` (Task 3).

- [ ] **Step 1: Search phase.** In `ARCapturePrototype.Uranium.cs`:
  1. Fields:
     - Delete `uraniumTiltBase` and `uraniumGravity` (the gravity reset in `SpawnUranium` too). Re-add a single field `private float uraniumGravity;` only for jolt.
     - Add:
       ```csharp
               private int uraniumMeter, uraniumTrendFrom, uraniumTrend;   // RAD 0..100, value at the start of the trend window, -1/0/+1
               private float uraniumTrendAt;
               private GUIStyle uraniumRadStyle;
       ```
  2. In `SpawnUranium`, reset `uraniumMeter = uraniumTrendFrom = 0; uraniumTrend = 0; uraniumTrendAt = Time.time;`.
  3. In `UpdateUranium`, replace the `case UraniumPhase.Search:` body with:
     ```csharp
                     case UraniumPhase.Search:
                         uraniumAngle = Vector3.Angle(arCamera.transform.forward, target.transform.position - arCamera.transform.position);
                         uraniumMeter = UraniumMath.Meter(uraniumAngle);
                         if (Time.time - uraniumTrendAt >= UraniumMath.TrendWindowSec)
                         {
                             uraniumTrend = UraniumMath.Trend(uraniumTrendFrom, uraniumMeter);
                             uraniumTrendFrom = uraniumMeter;
                             uraniumTrendAt = Time.time;
                         }
                         TickGeiger(UraniumMath.GeigerInterval(uraniumAngle));
                         uraniumLockHold = uraniumAngle <= UraniumMath.RevealAngleDeg ? uraniumLockHold + dt : 0f;
                         if (uraniumLockHold >= UraniumMath.RevealHoldSec) BeginUraniumSecure();
                         break;
     ```
  4. Change `TickGeiger` to take the interval:
     - signature `private void TickGeiger(float interval)`
     - last line `uraniumNextClickAt = Time.time + interval;`
     - comment `// One regular Geiger click (and a vibration pulse, rate-limited) every `interval` seconds.`
  5. In `StepUraniumSecure`, call it as `TickGeiger(1f / UraniumMath.SecureClickRate(uraniumR));`.

- [ ] **Step 2: Secure phase slider.**
  1. Replace `ReadUraniumRod` with:
     ```csharp
             // Control rod from the slider on the right (finger or mouse), W/S in the editor; the accelerometer only measures shaking.
             private void ReadUraniumRod(float dt, out float jolt)
             {
                 jolt = 0f;
                 if (ReadGravity(out Vector3 a))
                 {
                     if (uraniumGravity <= 0f) uraniumGravity = a.magnitude;
                     jolt = Mathf.Abs(a.magnitude / uraniumGravity - 1f);
                 }
                 Vector2? p = PointerScreen();
                 if (p != null)
                 {
                     UraniumSliderRect(Screen.width / HudScale, Screen.height / HudScale, out Rect track);
                     Vector2 gui = new Vector2(p.Value.x, Screen.height - p.Value.y) / HudScale;
                     var grab = new Rect(track.x - 24f, track.y - 30f, track.width + 40f, track.height + 60f); // generous for thumbs
                     if (grab.Contains(gui)) uraniumRod = UraniumMath.RodFromSlider((gui.y - track.y) / track.height);
                 }
                 if (Keyboard.current == null) return;
                 float dir = (Keyboard.current.sKey.isPressed ? 1f : 0f) - (Keyboard.current.wKey.isPressed ? 1f : 0f);
                 uraniumRod = Mathf.Clamp01(uraniumRod + dir * 1.5f * dt);
             }

             private static void UraniumSliderRect(float sw, float sh, out Rect track) =>
                 track = new Rect(sw - 16f - 64f, sh * 0.32f, 64f, sh * 0.38f);
     ```
  2. In `SpawnUranium` and `BeginUraniumSecure`, remove the `uraniumTiltBase = …` lines. Keep `uraniumGravity = 0f;` in `SpawnUranium`.
  3. In `DrawUraniumHud`, change the secure-phase text lines to:
     ```csharp
                         line = UraniumSpikeSoon ? "LONJAKAN NEUTRON!"
                             : uraniumR > UraniumMath.GreenMax ? "TERLALU PANAS · GESER BATANG KE BAWAH"
                             : uraniumR < UraniumMath.GreenMin ? "REAKSI MELEMAH · GESER BATANG KE ATAS"
                             : "STABIL · TAHAN HP";
     ```
     Change the search line to `line = uraniumLockHold > 0f ? "SINYAL KUAT! TAHAN ARAH HP" : "IKUTI SEKTOR HIJAU · CARI RAD TERTINGGI";`.
  4. Update the file's top comment: `// Uranium "Pemburu Geiger" (v2): follow the compass sector and the RAD meter to the hidden ore, then keep the reactivity in the green zone with the control-rod slider before the radiation dose fills up.`

- [ ] **Step 3: Overlay.** Replace `DrawUraniumOverlay` with:

```csharp
        // Search: aiming reticle + big RAD meter with a trend line. Secure: reactor gauge, control-rod slider (right), dose bar.
        private void DrawUraniumOverlay(float sw, float sh)
        {
            if (uraniumPhase == UraniumPhase.Search)
            {
                bool near = uraniumAngle <= UraniumMath.RevealAngleDeg;
                float rr = ReticleRadius * (near ? 1f + 0.06f * Mathf.Sin(Time.time * 10f) : 1f);
                EnerGoUI.DrawPanel(new Rect(sw * 0.5f - rr, sh * 0.5f - rr, rr * 2f, rr * 2f),
                    near ? EnerGoUI.Alpha(UraniumGlow, 0.15f) : Color.clear, near ? UraniumGlow : new Color(1f, 1f, 1f, 0.9f), rr, 3f);

                if (uraniumRadStyle == null) uraniumRadStyle = new GUIStyle(styleRadarLocked) { fontSize = 26, alignment = TextAnchor.MiddleCenter };
                float mw = Mathf.Min(sw - 64f, 300f);
                var m = new Rect((sw - mw) * 0.5f, sh - 176f, mw, 64f);
                EnerGoUI.DrawCard(m, EnerGoUI.Radius, near ? HudOk : HudOutline, 2f);
                GUI.Label(new Rect(m.x, m.y + 4f, mw, 30f), "RAD " + uraniumMeter, uraniumRadStyle);
                Color heat = Color.Lerp(EnerGoUI.Muted, UraniumGlow, uraniumMeter / 100f);
                EnerGoUI.DrawBar(new Rect(m.x + 16f, m.y + 38f, mw - 32f, 10f), uraniumMeter / 100f, heat);
                string trend = uraniumTrend > 0 ? "SINYAL MENGUAT" : uraniumTrend < 0 ? "SINYAL MELEMAH" : "";
                if (trend.Length > 0) DrawHudTag(sw * 0.5f, m.yMax + 4f, 160f, trend, sw);
                return;
            }

            UraniumSliderRect(sw, sh, out Rect track);
            float gw = 30f;
            var g = new Rect(track.x - 12f - gw, track.y, gw, track.height);
            bool blink = UraniumSpikeSoon && Mathf.Repeat(Time.time * 8f, 1f) < 0.5f;
            EnerGoUI.DrawPanel(g, HudFill, blink ? HudWarn : HudOutline, 8f, 2f);
            float Y(float v) => g.yMax - 4f - v * (g.height - 8f);
            EnerGoUI.DrawPanel(new Rect(g.x + 4f, Y(UraniumMath.GreenMax), gw - 8f, Y(UraniumMath.GreenMin) - Y(UraniumMath.GreenMax)),
                EnerGoUI.Alpha(EnerGoUI.Success, 0.35f), Color.clear, 4f);
            EnerGoUI.DrawPanel(new Rect(g.x + 4f, Y(1f), gw - 8f, Y(UraniumMath.HotAbove) - Y(1f)),
                EnerGoUI.Alpha(EnerGoUI.Danger, 0.3f), Color.clear, 4f);
            float my = Y(uraniumR);
            EnerGoUI.DrawPanel(new Rect(g.x - 4f, my - 3f, gw + 8f, 6f), UraniumMath.InGreen(uraniumR) ? EnerGoUI.Success : EnerGoUI.Orange, Color.clear, 3f);
            DrawHudTag(g.center.x, g.y - 26f, 84f, "REAKTOR", sw);

            // Slider: the rod hangs from the top; dragging down pushes it in. Knob = where the finger sets it.
            EnerGoUI.DrawPanel(track, HudFill, HudOutline, 12f, 2f);
            float knobY = track.y + uraniumRod * track.height;
            EnerGoUI.DrawPanel(new Rect(track.x + 22f, track.y + 6f, 20f, Mathf.Max(0f, knobY - track.y - 6f)), EnerGoUI.Ink, Color.clear, 6f);
            EnerGoUI.DrawPanel(new Rect(track.x - 4f, knobY - 14f, track.width + 8f, 28f), EnerGoUI.Orange, EnerGoUI.Ink, 10f, 2f);
            DrawHudTag(track.center.x, track.yMax + 6f, 84f, "BATANG", sw);

            float dw = Mathf.Min(sw - 64f, 320f);
            var dose = new Rect((sw - dw) * 0.5f, sh - 104f, dw, 34f);
            bool high = uraniumDose > 0.75f;
            EnerGoUI.DrawCard(dose, EnerGoUI.Radius, high ? HudWarn : HudOutline, 2f);
            GUI.Label(new Rect(dose.x + 10f, dose.y + 8f, 60f, 18f), "DOSIS", styleTapsCounter);
            EnerGoUI.DrawBar(new Rect(dose.x + 70f, dose.y + 11f, dw - 84f, 12f), uraniumDose, high ? EnerGoUI.Danger : EnerGoUI.Orange);
        }
```

- [ ] **Step 4: No look-drag while securing.** In `ARCapturePrototype.cs` `HandleInputs`, change `&& isDraggingLook && isUsingGyro && !goldPanning)` to `&& isDraggingLook && isUsingGyro && !goldPanning && uraniumPhase != UraniumPhase.Secure)`. In the editor block, wrap the right-mouse look in `if (uraniumPhase != UraniumPhase.Secure)` as well.

- [ ] **Step 5: Compile and run the tests (75 pass). Then check in the editor by reflection.**
  1. **Search.** Spawn uranium (`captureBearing = 0`, `startedAt` forced as in Task 3). Point the camera at the ore by setting `lookYaw` and `lookPitch`, or by rotating `arCamera` directly, so the angle is about 15°. Call `UpdateUranium()`. Assert `uraniumMeter > 90`. Take a screenshot (`scratch/u2_search.png`): the RAD card, the green reticle and the compass strip with its sector.
  2. **Slider.** Call `BeginUraniumSecure()`. Set `uraniumRod = 1f` and step `StepUraniumSecure(0.1f)` five times. Assert `uraniumR < GreenMin`. Take a screenshot (`scratch/u2_secure.png`).
  3. **Track mapping.** Simulate the pointer at the track's top by calling `UraniumMath.RodFromSlider(0)` through `ReadUraniumRod` (if a fake pointer is impractical, assert `UraniumSliderRect` values and the mapping math instead).
  4. Exit play mode.

- [ ] **Step 6: Commit** `ARCapturePrototype.Uranium.cs` and `ARCapturePrototype.cs`, with the message `feat: uranium v2 — RAD meter and trend, aiming reticle, touch slider control rod`.

---

### Task 6: `GuideViewer` and the AR guide overlay

**Files:**
- Create: `Assets/EnerGo/Scripts/GuideViewer.cs`
- Modify: `Assets/EnerGo/Scripts/ARCapturePrototype.cs` (guide state, the spawn gate, input gate, `OnGUI`)

**Interfaces:**
- Consumes: `GuideBook` (Task 1).
- Produces:
  - `GuideViewer`, with:
    - static `string SkipKey(string id)`
    - static `bool ShouldShow(string id)`
    - static `void ResetAll()`
    - static `void DrawGuideArt(Rect r, GuideBook.Art art)`
    - `GuideViewer(string resourceId, bool withStart)`
    - `enum Result { None, Start, Close }`
    - `Result Draw(float sw, float sh, float hudScale)`
  - Task 7 uses it from the lobby.

- [ ] **Step 1: Create** `Assets/EnerGo/Scripts/GuideViewer.cs`

```csharp
using UnityEngine;
using static EnerGo.EnerGoUI;

namespace EnerGo
{
    // Guide cards for one SDA (IMGUI, call Draw from OnGUI under the HUD-scaled matrix). withStart = the pre-game
    // overlay (MULAI + "Jangan ingatkan lagi"); otherwise the lobby viewer (TUTUP only).
    public class GuideViewer
    {
        public enum Result { None, Start, Close }

        public static string SkipKey(string id) => "EnerGo.Guide.Skip." + id;
        public static bool ShouldShow(string id) => GuideBook.For(id).Length > 0 && PlayerPrefs.GetInt(SkipKey(id), 0) == 0;

        public static void ResetAll()
        {
            foreach (var id in GuideBook.Ids) PlayerPrefs.DeleteKey(SkipKey(id));
            PlayerPrefs.Save();
        }

        private readonly string id;
        private readonly bool withStart;
        private readonly GuideBook.Card[] cards;
        private int page;
        private bool dontRemind;
        private GUIStyle sTitle, sBody, sSmall, sButton, sButtonDark;

        public GuideViewer(string resourceId, bool withStart)
        {
            id = resourceId;
            this.withStart = withStart;
            cards = GuideBook.For(resourceId);
        }

        public Result Draw(float sw, float sh, float hudScale)
        {
            if (cards.Length == 0) return withStart ? Result.Start : Result.Close;
            if (sTitle == null)
            {
                sTitle = Text(20, true, Ink, TextAnchor.UpperLeft, true);
                sBody = Text(15, false, Muted, TextAnchor.UpperLeft, true);
                sSmall = Text(13, true, Ink, TextAnchor.MiddleLeft);
                sButton = Text(16, true, Color.white);
                sButtonDark = Text(15, true, Ink);
            }

            GUI.color = Alpha(Ink, 0.55f);
            GUI.DrawTexture(new Rect(0, 0, sw, sh), Texture2D.whiteTexture);
            GUI.color = Color.white;

            float w = Mathf.Min(sw - 40f, 400f), h = 330f, x = (sw - w) * 0.5f, y = Mathf.Max(70f, sh * 0.5f - h * 0.62f);
            var card = new Rect(x, y, w, h);
            DrawCard(card);
            var c = cards[page];
            GUI.Label(new Rect(x + 20f, y + 14f, w - 40f, 20f), ResourceIds.Name(id).ToUpperInvariant() + " · " + (page + 1) + "/" + cards.Length, sSmall);
            DrawGuideArt(new Rect(x + 20f, y + 42f, w - 40f, 130f), c.Art);
            GUI.Label(new Rect(x + 20f, y + 186f, w - 40f, 52f), c.Title, sTitle);
            GUI.Label(new Rect(x + 20f, y + 240f, w - 40f, 66f), c.Body, sBody);
            for (int i = 0; i < cards.Length; i++) // page dots
                DrawPanel(new Rect(x + w * 0.5f + (i - (cards.Length - 1) * 0.5f) * 16f - 4f, y + h - 18f, 8f, 8f), i == page ? Orange : Line, Color.clear, 4f);

            // Tapping the left/right third of the card flips pages.
            if (page > 0 && TapHit(new Rect(x, y, w / 3f, h), hudScale)) page--;
            else if (page < cards.Length - 1 && TapHit(new Rect(x + w * 2f / 3f, y, w / 3f, h), hudScale)) page++;

            float by = card.yMax + 14f;
            bool last = page == cards.Length - 1;
            if (withStart)
            {
                var row = new Rect(x, by, w, 30f);
                DrawPanel(new Rect(x + 4f, by + 4f, 22f, 22f), Paper, Ink, 5f, 2f);
                if (dontRemind) DrawPanel(new Rect(x + 9f, by + 9f, 12f, 12f), Orange, Color.clear, 3f);
                GUI.color = Color.white;
                GUI.Label(new Rect(x + 34f, by, w - 34f, 30f), "Jangan ingatkan lagi", Text(14, true, Paper, TextAnchor.MiddleLeft));
                if (TapHit(row, hudScale)) dontRemind = !dontRemind;
                by += 40f;
            }

            var main = new Rect(x, by, w, 56f);
            string label = !withStart ? (last ? "TUTUP" : "LANJUT") : last ? "MULAI" : "LANJUT";
            GUI.Label(DrawPrimaryButton(main, IsHeld(main, hudScale)), label, sButton);
            if (TapHit(main, hudScale))
            {
                if (!last) page++;
                else return Finish();
            }
            var skip = new Rect(x + w * 0.5f - 70f, main.yMax + 8f, 140f, 40f);
            GUI.Label(DrawSecondaryButton(skip, IsHeld(skip, hudScale)), withStart ? "LEWATI" : "TUTUP", sButtonDark);
            if (TapHit(skip, hudScale)) return Finish();
            return Result.None;
        }

        private Result Finish()
        {
            if (!withStart) return Result.Close;
            if (dontRemind) { PlayerPrefs.SetInt(SkipKey(id), 1); PlayerPrefs.Save(); }
            return Result.Start;
        }

        // Simple shapes in the brand palette, one per guide step (placeholder art until the Blender pass).
        public static void DrawGuideArt(Rect r, GuideBook.Art art)
        {
            DrawPanel(r, Cream, Line, 14f, 1.5f);
            Vector2 c = r.center;
            Rect Box(float cx, float cy, float w, float h) => new Rect(cx - w * 0.5f, cy - h * 0.5f, w, h);
            Rect Circle(float cx, float cy, float rad) => Box(cx, cy, rad * 2f, rad * 2f);
            switch (art)
            {
                case GuideBook.Art.Reticle:
                    DrawPanel(Circle(c.x + 18f, c.y - 8f, 16f), EnerGoUI.Sun, Color.clear, 16f);
                    DrawPanel(Circle(c.x, c.y, 34f), Color.clear, Orange, 34f, 3f);
                    break;
                case GuideBook.Art.Compass:
                    DrawPanel(Box(c.x, c.y, r.width - 60f, 34f), Paper, Line, 10f, 1.5f);
                    for (int i = -3; i <= 3; i++) DrawPanel(Box(c.x + i * 34f, c.y, 2f, 12f), Line, Color.clear, 1f);
                    DrawPanel(Box(c.x, c.y, 2f, 28f), Ink, Color.clear, 1f);
                    DrawPanel(Circle(c.x + 54f, c.y, 7f), Orange, Ink, 7f, 1.5f);
                    break;
                case GuideBook.Art.Tap:
                    DrawPanel(Circle(c.x, c.y, 30f), Ink, Color.clear, 30f);
                    DrawPanel(Circle(c.x + 26f, c.y + 22f, 14f), Color.clear, Orange, 14f, 3f);
                    DrawPanel(Circle(c.x + 26f, c.y + 22f, 6f), Orange, Color.clear, 6f);
                    break;
                case GuideBook.Art.PhoneFlat:
                    DrawPanel(Box(c.x, c.y + 10f, 120f, 26f), Ink, Color.clear, 8f);
                    DrawPanel(Circle(c.x, c.y - 28f, 10f), Color.clear, Success, 10f, 2f);
                    DrawPanel(Circle(c.x, c.y - 28f, 4f), Success, Color.clear, 4f);
                    break;
                case GuideBook.Art.PhoneSwirl:
                    DrawPanel(Circle(c.x, c.y, 46f), Color.clear, Alpha(Orange, 0.8f), 46f, 3f);
                    DrawPanel(Box(c.x, c.y, 30f, 54f), Ink, Color.clear, 6f);
                    DrawPanel(Circle(c.x + 46f, c.y, 6f), Orange, Color.clear, 6f);
                    break;
                case GuideBook.Art.Hold:
                    DrawPanel(Box(c.x, c.y - 30f, 6f, 70f), Ink, Color.clear, 3f);
                    DrawPanel(Box(c.x, c.y + 8f, 16f, 16f), Orange, Ink, 4f, 1.5f);
                    DrawPanel(Circle(c.x + 34f, c.y - 34f, 12f), Alpha(Orange, 0.4f), Orange, 12f, 2f);
                    break;
                case GuideBook.Art.Layers:
                {
                    Color[] bands = { new Color(0.48f, 0.31f, 0.18f), new Color(0.27f, 0.27f, 0.31f), new Color(0.56f, 0.48f, 0.71f), new Color(0.17f, 0.11f, 0.08f), new Color(0.18f, 0.43f, 0.58f) };
                    float bh = (r.height - 20f) / bands.Length;
                    for (int i = 0; i < bands.Length; i++) DrawPanel(new Rect(r.x + 30f, r.y + 10f + i * bh, r.width - 60f, bh), bands[i], Color.clear, 0f);
                    DrawPanel(Box(c.x + 30f, r.y + 10f + bh * 1.8f, 6f, bh * 3.6f), Paper, Ink, 2f, 1f);
                    break;
                }
                case GuideBook.Art.PumpRing:
                    DrawPanel(Circle(c.x, c.y, 26f), Alpha(Orange, 0.2f), Orange, 26f, 3f);
                    DrawPanel(Circle(c.x, c.y, 48f), Color.clear, Ink, 48f, 3f);
                    break;
                case GuideBook.Art.RadMeter:
                    GUI.Label(Box(c.x, c.y - 22f, 160f, 30f), "RAD 87", Text(24, true, Ink));
                    DrawBar(Box(c.x, c.y + 16f, r.width - 80f, 12f), 0.87f, new Color(0.22f, 1f, 0.08f));
                    break;
                case GuideBook.Art.Slider:
                    DrawPanel(Box(c.x - 30f, c.y, 22f, r.height - 30f), Paper, Line, 6f, 1.5f);
                    DrawPanel(Box(c.x - 30f, c.y, 14f, (r.height - 30f) * 0.3f), Alpha(Success, 0.5f), Color.clear, 4f);
                    DrawPanel(Box(c.x + 30f, c.y, 40f, r.height - 30f), Paper, Line, 10f, 1.5f);
                    DrawPanel(Box(c.x + 30f, c.y + 8f, 48f, 22f), Orange, Ink, 8f, 2f);
                    break;
                case GuideBook.Art.Shake:
                    DrawPanel(Box(c.x, c.y, 34f, 60f), Ink, Color.clear, 7f);
                    DrawPanel(Box(c.x - 34f, c.y, 4f, 30f), Danger, Color.clear, 2f);
                    DrawPanel(Box(c.x + 34f, c.y, 4f, 30f), Danger, Color.clear, 2f);
                    break;
            }
        }
    }
}
```

  If `EnerGoUI` has no `Cream` or `Paper` token, check its token names (`Cream`, `Paper`, `Line` and `Sun` were all present when this plan was written).

- [ ] **Step 2: Gate the AR scene.** In `ARCapturePrototype.cs`:
  1. Add a field `private GuideViewer guide; // pre-game guide overlay; null = closed`.
  2. At the end of `Awake`, add:
     ```csharp
                 if (save.pendingCapture == null && GuideViewer.ShouldShow(resourceId)) guide = new GuideViewer(resourceId, withStart: true);
     ```
  3. In `Update`'s spawn condition, add `&& guide == null`.
  4. At the top of `HandleInputs`, add `if (guide != null) return; // the overlay handles its own taps`.
  5. In `OnGUI`:
     - Wrap the calls for the sun reticle, coal pips, gold pan, uranium overlay, oil overlay, `DrawRadarHUD` and `DrawCompassStrip` in `if (guide == null) { … }`.
     - Hide the bottom reset button while the guide is open: add `guide == null &&` to its `if`.
     - Before `if (EnerGoProgress.Current.pendingCapture != null) DrawAnalysis(sw, sh);`, add:
       ```csharp
                   if (guide != null && guide.Draw(sw, sh, scale) == GuideViewer.Result.Start)
                   {
                       guide = null;
                       nextSpawnTime = Time.time; // the mini-game (and the sun timer) starts now
                   }
       ```
     - The top-left ‹ LOBBY/RADAR button keeps working, because it is drawn before the guide.

- [ ] **Step 3: Compile and run the tests (75 pass).**

- [ ] **Step 4: Check in the editor.**
  1. Delete `EnerGo.Guide.Skip.coal` and open `ARPrototype` with `resourceId = "coal"`. Assert:
     - `guide != null`
     - after ticking, or calling the spawn condition, `target == null`
     - `CheckTapOnTarget` is not reached, because `HandleInputs` returns
  2. Take a screenshot of card 1 (`scratch/guide_coal.png`).
  3. Simulate MULAI with "jangan ingatkan" ticked: set `dontRemind = true` and `page` to the last card by reflection, then call the private `Finish()`. Assert `PlayerPrefs.GetInt("EnerGo.Guide.Skip.coal") == 1`.
  4. Reload and assert `guide == null`.
  5. With a fake `pendingCapture` (save backed up), assert that no guide appears.
  6. Delete the key afterwards.

- [ ] **Step 5: Commit** `GuideViewer.cs` (+ `.meta`) and `ARCapturePrototype.cs`, with the message `feat: guide cards before each mini-game with "Jangan ingatkan lagi"`.

---

### Task 7: Lobby PANDUAN button and sheet

**Files:**
- Modify: `Assets/EnerGo/Scripts/LobbyController.cs` (`ScreenState`, `DrawLobby` next to the settings button, `OnGUI` dispatch, a new `DrawGuides`)

**Interfaces:**
- Consumes: `GuideViewer`, `GuideBook.Ids` (Task 6 / Task 1).

- [ ] **Step 1: Make the edits.**
  1. Add `Guides` to `ScreenState`, after `Practice`.
  2. Add the fields:
     ```csharp
             private GuideViewer lobbyGuide;   // card viewer opened from the PANDUAN sheet
             private float guidesResetAt = -10f;
     ```
  3. In `DrawLobby`, right after the settings button's `if (interactive && ButtonHit(setRect)) { … }` block, add:
     ```csharp
                 // Guide (left of settings): round "?" button opening the PANDUAN sheet.
                 var helpRect = new Rect(setRect.x - 58f, top, 48f, 52f);
                 GUI.Label(DrawSecondaryButton(helpRect, IsHeld(helpRect, scale)), "?", new GUIStyle(sTitle) { alignment = TextAnchor.MiddleCenter });
                 if (interactive && ButtonHit(helpRect))
                 {
                     if (SFXManager.Instance != null) SFXManager.Instance.PlayButtonClick();
                     currentState = ScreenState.Guides;
                     inputCooldownTimer = 0.2f;
                 }
     ```
  4. In `OnGUI`, next to `if (currentState == ScreenState.Practice) DrawPractice(sw, sh);`, add `if (currentState == ScreenState.Guides) DrawGuides(sw, sh);`.
  5. Add `DrawGuides`, following the structure of `DrawPractice`:
     ```csharp
             // PANDUAN: the 5 SDA as cards; tap one to read its guide. Also resets every "Jangan ingatkan lagi".
             private void DrawGuides(float sw, float sh)
             {
                 float scale = HudScale;
                 if (lobbyGuide != null)
                 {
                     if (lobbyGuide.Draw(sw, sh, scale) != GuideViewer.Result.None) { lobbyGuide = null; inputCooldownTimer = 0.2f; }
                     return;
                 }
                 GUI.color = Alpha(Ink, 0.45f);
                 GUI.DrawTexture(new Rect(0, 0, sw, sh), Texture2D.whiteTexture);
                 GUI.color = Color.white;
                 float gap = 10f, cardH = 62f;
                 float sheetH = 32f + 46f + cardH * 3f + gap * 2f + 20f + 56f + 30f + SafeBottom(scale);
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
                 GUI.Label(new Rect(x, y, w, 30f), "Panduan", new GUIStyle(sTitle) { alignment = TextAnchor.MiddleLeft });
                 y += 46f;
                 float cardW = (w - gap) * 0.5f;
                 for (int i = 0; i < GuideBook.Ids.Length; i++)
                 {
                     var rect = new Rect(x + (i % 2) * (cardW + gap), y + (i / 2) * (cardH + gap), cardW, cardH);
                     DrawCollectionCard(rect, GuideBook.Ids[i]);
                     if (ready && ButtonHit(rect))
                     {
                         if (SFXManager.Instance != null) SFXManager.Instance.PlayButtonClick();
                         lobbyGuide = new GuideViewer(GuideBook.Ids[i], withStart: false);
                         inputCooldownTimer = 0.15f;
                     }
                 }
                 y += cardH * 3f + gap * 2f + 20f;
                 var reset = new Rect(x, y, w, 56f);
                 GUI.Label(DrawSecondaryButton(reset, IsHeld(reset, scale)), "TAMPILKAN SEMUA PANDUAN LAGI", new GUIStyle(sSecondary) { fontSize = 14 });
                 if (ready && ButtonHit(reset))
                 {
                     if (SFXManager.Instance != null) SFXManager.Instance.PlayButtonClick();
                     GuideViewer.ResetAll();
                     guidesResetAt = Time.time;
                 }
                 if (Time.time - guidesResetAt < 2f)
                     GUI.Label(new Rect(x, reset.yMax + 4f, w, 22f), "Panduan akan muncul lagi", new GUIStyle(sMuted) { alignment = TextAnchor.MiddleCenter });
             }
     ```
     Check that `sSecondary`, `sMuted`, `OutsideTap`, `ButtonHit`, `DrawCollectionCard`, `SafeTop` and `SafeBottom` exist with these names; they did when this plan was written. If the "?" glyph renders as a box, use "PANDUAN" text on a wider button instead.

- [ ] **Step 2: Compile and run the tests (75 pass).**

- [ ] **Step 3: Check in the editor.**
  1. Enter play mode in `LobbyScene` and screenshot the lobby top bar with "?" next to the settings button (`scratch/lobby_help.png`).
  2. Set `currentState = Guides` by reflection and screenshot the sheet (`scratch/lobby_guides.png`).
  3. Set `lobbyGuide = new GuideViewer("uranium", false)` and screenshot it (`scratch/lobby_guide_uranium.png`).
  4. Set `EnerGo.Guide.Skip.coal = 1`, call `GuideViewer.ResetAll()`, and assert the key is gone.

- [ ] **Step 4: Commit** `LobbyController.cs` with the message `feat: lobby PANDUAN sheet with guide viewer and reset`.

---

### Task 8: End-to-end check and APK

- [ ] **Step 1: Run the full suite.** Expected: 75 pass.
- [ ] **Step 2: Run the Review Focus checks** (items 1–5 above) that earlier tasks did not already cover. Take screenshots of: the coal strip with the marker on-strip, `DrawRadarHUD` for gold in view, and the oil cross-section with the strip. Read each PNG and note layout problems (overlaps, clipped text) as concerns. Do not redesign.
- [ ] **Step 3: Build the APK.**
  1. Use script-execute with `EditorApplication.delayCall` calling `BuildPipeline.BuildPlayer`: scenes `Assets/EnerGo/LobbyScene.unity`, `Assets/EnerGo/ExploreScene.unity`, `Assets/EnerGo/ARPrototype.unity`, `Assets/EnerGo/Sensor360Scene.unity`; target Android; output `Builds/EnerGo-Kompas.apk`.
  2. Write `summary.result + " errors=" + totalErrors` to `Builds/kompas-result.txt`.
  3. Exit play mode first, and poll the result file; do not re-trigger the build.
  4. Expected: `Succeeded errors=0`.
- [ ] **Step 4: Write the device checklist into the report:**
  - **Coal, gold and oil from the radar:** each appears in the radar's direction, and the strip marker matches.
  - **Uranium:**
    - the green sector leads to the ore and RAD rises as you turn
    - the reveal is easy
    - the slider responds at once
    - spikes are survivable
  - **Sun:** the timer is shown, finishing at 50–99 % gives the right grade, and below 50 % retries.
  - **Guides:**
    - the guide shows before each game
    - "Jangan ingatkan lagi" sticks
    - the lobby "?" sheet works and its reset brings the guides back
  - **No compass:** cover the sensor, or test on a phone without one; the game still plays.
- [ ] **Step 5:** Commit any fixes made in Steps 1–3 with explicit paths. `Builds/` is not committed.
