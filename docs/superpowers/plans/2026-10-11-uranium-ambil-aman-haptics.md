# Uranium "Ambil dengan Aman" + Haptics Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the uranium reactor/slider phase with picking up 3 ore fragments with tongs (Waktu, Jarak, Perisai), and add phone vibration to every mini-game.

**Architecture:** Pure rules stay in `Core/UraniumMath.cs` (EnerGo.Core assembly, EditMode-tested). The scene glue lives in the `ARCapturePrototype` partial class (`ARCapturePrototype.Uranium.cs` for uranium, `ARCapturePrototype.cs` for sun/coal/gold/quiz) and `RadarController.cs`. All vibration goes through the existing `Haptics.Pulse(ms)`, which already honours the Pengaturan → Getaran switch (`Haptics.Enabled`).

**Tech Stack:** Unity 6 (URP), C#, IMGUI HUD, new Input System (EnhancedTouch), NUnit EditMode tests.

**Spec:** `docs/superpowers/specs/2026-10-11-uranium-ambil-aman-haptics-design.md`

## Global Constraints

- Every vibration goes through `Haptics.Pulse(ms)`; never call `Handheld.Vibrate` directly.
- Repeating ticks are rate-limited with a `last…At` timestamp (pattern: `oilLastRumbleAt` in `ARCapturePrototype.Oil.cs`).
- Player-facing text is Bahasa Indonesia, HUD lines in UPPERCASE.
- Uranium must run on the Samsung A16 without ARCore (Sensor 360 / gyro only).
- `QuizBank` ids are stored in saves: append `uranium-08`, never renumber.
- Code comments match the surrounding style (short, English, `// ponytail:` for deliberate shortcuts).

## Review Focus

1. Holding JEPIT with a finger must not also drag the gyro look (touch on the button would otherwise rotate the view) → Task 2 guards drag-look with `uraniumGripHeld`.
2. Grabbing the last fragment and the dose hitting 100 % on the same frame → capture wins (`Check` order), pinned in Task 1 test `CaptureBeatsOverdoseOnTheSameFrame`.
3. Fragment placement when the random draws keep colliding → falls back to an even spread, pinned in Task 1 test `FragmentsFallBackToAnEvenSpread`.
4. Retry after an overdose must clear fragments and grip state (no stale `uraniumFragments` from the last ore) → Task 2 `SpawnUranium` resets every field; manual check in Task 6.
5. Turning Getaran off must silence every new pulse → all pulses go through `Haptics.Pulse`; manual check in Task 6.

---

### Task 0: Commit the pending settings work

The working tree holds the uncommitted Volume/Getaran settings and the uranium v2 tuning from earlier today. Commit them first so this plan starts from a clean base. Leave the unrelated `Assets/Plugins/NuGet`, `Packages/`, `ProjectSettings/` changes alone.

- [ ] **Step 1: Commit**

```bash
cd /d/code/unity/EnerGo
git add Assets/EnerGo/Scripts/Haptics.cs Assets/EnerGo/Scripts/SFXManager.cs Assets/EnerGo/Scripts/LobbyController.cs \
        Assets/EnerGo/Scripts/Core/UraniumMath.cs Assets/EnerGo/Scripts/ARCapturePrototype.Uranium.cs \
        Assets/EnerGo/Scripts/Core/GuideBook.cs Assets/EnerGo/Tests/Editor/UraniumMathTests.cs
git commit -m "feat: volume and vibration settings; harder uranium reactor tuning

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 1: UraniumMath v3 rules

**Files:**
- Modify (full rewrite): `Assets/EnerGo/Scripts/Core/UraniumMath.cs`
- Test (full rewrite): `Assets/EnerGo/Tests/Editor/UraniumMathTests.cs`

**Interfaces:**
- Produces (used by Task 2): `UraniumMath.FragmentCount`, `GripAngleDeg`, `FailShowSec`, `RevealAngleDeg`, `RevealHoldSec`, `HideMinPitchDeg`, `HideMaxPitchDeg`, `TrendWindowSec`, `MaxPulsesPerSec`;
  `float GeigerInterval(float angleDeg)`, `int Meter(float angleDeg)`, `int Trend(int prev, int now)`,
  `float[] FragmentYaws(System.Func<float> rand01)`, `int Nearest(float[] anglesDeg, bool[] taken)`,
  `float StepGrip(float grip, bool held, float angleDeg, float jolt, float dt)`,
  `float StepDose(float dose, int left, float dt)`, `enum Outcome { None, Captured, Overdose }`,
  `Outcome Check(float dose, int collected)`, `float Performance(float elapsed, float dose)`.
- Removed (Task 2 must not use): `RodFromSlider`, `StepReactivity`, `Drift`, `InGreen`, `StepProgress`, `NextSpikeDelay`, `SecureClickRate`, `GreenMin/GreenMax/HotAbove`, spike/rod/noise/jolt constants.

- [ ] **Step 1: Write the failing tests** — replace the whole of `UraniumMathTests.cs`:

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
        public void RevealIsForgiving()
        {
            Assert.AreEqual(20f, UraniumMath.RevealAngleDeg, 1e-4f);
            Assert.AreEqual(0.8f, UraniumMath.RevealHoldSec, 1e-4f);
        }

        [Test]
        public void FirstFragmentStaysWhereTheOreWasAndTheRestAreSpreadOut()
        {
            var rng = new System.Random(1);
            for (int run = 0; run < 200; run++)
            {
                float[] yaws = UraniumMath.FragmentYaws(() => (float)rng.NextDouble());
                Assert.AreEqual(UraniumMath.FragmentCount, yaws.Length);
                Assert.AreEqual(0f, yaws[0], 1e-4f);
                for (int i = 0; i < yaws.Length; i++)
                    for (int j = i + 1; j < yaws.Length; j++)
                        Assert.GreaterOrEqual(UnityEngine.Mathf.Abs(UnityEngine.Mathf.DeltaAngle(yaws[i], yaws[j])), UraniumMath.FragmentMinSepDeg - 1e-3f);
            }
        }

        [Test]
        public void FragmentsFallBackToAnEvenSpread()
        {
            float[] yaws = UraniumMath.FragmentYaws(() => 0.5f); // every draw lands on the ore's own bearing
            Assert.AreEqual(0f, yaws[0], 1e-4f);
            Assert.AreEqual(120f, yaws[1], 1e-4f);
            Assert.AreEqual(240f, yaws[2], 1e-4f);
        }

        [Test]
        public void NearestSkipsTakenFragments()
        {
            float[] angles = { 10f, 90f, 40f };
            Assert.AreEqual(0, UraniumMath.Nearest(angles, new[] { false, false, false }));
            Assert.AreEqual(2, UraniumMath.Nearest(angles, new[] { true, false, false }));
            Assert.AreEqual(-1, UraniumMath.Nearest(angles, new[] { true, true, true }));
        }

        [Test]
        public void GripFillsInOneSecondWhileHeldAimedAndSteady()
        {
            float g = 0f;
            for (int i = 0; i < 10; i++) g = UraniumMath.StepGrip(g, true, 5f, 0f, 0.1f);
            Assert.AreEqual(1f, g, 1e-4f);
            Assert.AreEqual(1f, UraniumMath.StepGrip(1f, true, 5f, 0f, 0.1f), 1e-4f);
        }

        [Test]
        public void GripLetsGoOnReleaseMissOrShake()
        {
            Assert.AreEqual(0f, UraniumMath.StepGrip(0.7f, false, 5f, 0f, 0.1f), 1e-4f);
            Assert.AreEqual(0f, UraniumMath.StepGrip(0.7f, true, UraniumMath.GripAngleDeg + 1f, 0f, 0.1f), 1e-4f);
            Assert.AreEqual(0f, UraniumMath.StepGrip(0.7f, true, 5f, UraniumMath.GripShakeMax, 0.1f), 1e-4f);
            Assert.Greater(UraniumMath.StepGrip(0.7f, true, 5f, UraniumMath.GripShakeMax - 0.05f, 0.1f), 0.7f);
        }

        [Test]
        public void DoseFillsInFortySecondsWithEverythingOut()
        {
            Assert.AreEqual(1f / 40f, UraniumMath.StepDose(0f, 3, 1f), 1e-5f);
            float d = 0f;
            for (int i = 0; i < 400; i++) d = UraniumMath.StepDose(d, 3, 0.1f);
            Assert.AreEqual(1f, d, 1e-4f);
        }

        [Test]
        public void EachShieldedFragmentSlowsTheDoseButNeverStopsIt()
        {
            float d3 = UraniumMath.StepDose(0f, 3, 1f), d2 = UraniumMath.StepDose(0f, 2, 1f);
            float d1 = UraniumMath.StepDose(0f, 1, 1f), d0 = UraniumMath.StepDose(0f, 0, 1f);
            Assert.Greater(d3, d2);
            Assert.Greater(d2, d1);
            Assert.Greater(d1, d0);
            Assert.AreEqual(0.25f / 40f, d0, 1e-5f);
        }

        [Test]
        public void CaptureBeatsOverdoseOnTheSameFrame()
        {
            Assert.AreEqual(UraniumMath.Outcome.Captured, UraniumMath.Check(1f, 3));
            Assert.AreEqual(UraniumMath.Outcome.Overdose, UraniumMath.Check(1f, 2));
            Assert.AreEqual(UraniumMath.Outcome.None, UraniumMath.Check(0.9f, 2));
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

- [ ] **Step 2: Run tests to verify they fail**

Run (Unity editor open, MCP plugin connected):
`cd /d/code/unity/EnerGo && npx -y unity-mcp-cli run-tool assets-refresh --input '{}'` then
`npx -y unity-mcp-cli run-tool console-get-logs --input '{"logTypeFilter":"Error"}'`
Expected: compile errors such as `'UraniumMath' does not contain a definition for 'FragmentYaws'`.

- [ ] **Step 3: Write the implementation** — replace the whole of `UraniumMath.cs`:

```csharp
using UnityEngine;

namespace EnerGo
{
    // Pure rules of the uranium "Pemburu Geiger" mini-game (v3, Ambil dengan Aman): find the ore with the Geiger counter,
    // then pick its fragments up with the tongs before the dose fills. Teaches Waktu, Jarak, Perisai.
    public static class UraniumMath
    {
        // Search: the ore sits at the radar bearing, a little above or below eye level.
        public const float HideMinPitchDeg = -5f, HideMaxPitchDeg = 15f;
        public const float RevealAngleDeg = 20f, RevealHoldSec = 0.8f;
        public const float FarClicksPerSec = 2f, NearClicksPerSec = 20f; // regular ticks so the change is easy to hear
        public const float MaxPulsesPerSec = 12f;   // vibration pulses blur together faster than this
        public const int TrendThreshold = 3;        // RAD points per window that count as "menguat"/"melemah"
        public const float TrendWindowSec = 0.3f;
        // Collect: fragments around the player, picked up with the tongs.
        public const int FragmentCount = 3;
        public const float FragmentMinSepDeg = 80f;
        public const float GripAngleDeg = RevealAngleDeg; // the fragment has to sit inside the reticle
        public const float GripSec = 1f;
        public const float GripShakeMax = 0.35f;    // accelerometer jolt (|a| / g - 1) that knocks a fragment out of the tongs
        public const float DoseBasePerSec = 1f / 40f; // every fragment still out
        public const float DoseFloorShare = 0.25f;  // what is left of the dose rate with everything in the lead box
        public const float ParSec = 20f;            // a good collect phase, for the speed score
        public const float FailShowSec = 1.5f;

        // Seconds between Geiger clicks: 1/20 s straight at the ore, 1/2 s at 150° or more away.
        public static float GeigerInterval(float angleDeg) =>
            1f / Mathf.Lerp(NearClicksPerSec, FarClicksPerSec, Mathf.Clamp01(angleDeg / 150f));

        public static int Meter(float angleDeg) => Mathf.RoundToInt(100f * (1f - Mathf.Clamp(angleDeg, 0f, 180f) / 180f));

        // +1 = signal getting stronger, -1 = weaker, 0 = about the same.
        public static int Trend(int prevMeter, int meter) =>
            meter - prevMeter >= TrendThreshold ? 1 : prevMeter - meter >= TrendThreshold ? -1 : 0;

        // Yaw offsets (degrees from the ore's bearing): the first fragment stays where the ore was, the rest land
        // anywhere around the player at least FragmentMinSepDeg from each other. rand01 returns [0, 1).
        public static float[] FragmentYaws(System.Func<float> rand01)
        {
            var yaws = new float[FragmentCount];
            for (int i = 1; i < FragmentCount; i++)
            {
                float y = 0f;
                bool ok = false;
                for (int tries = 0; tries < 50 && !ok; tries++)
                {
                    y = rand01() * 360f - 180f;
                    ok = true;
                    for (int j = 0; j < i; j++)
                        if (Mathf.Abs(Mathf.DeltaAngle(y, yaws[j])) < FragmentMinSepDeg) { ok = false; break; }
                }
                yaws[i] = ok ? y : i * 360f / FragmentCount; // ponytail: even spread if the dice keep colliding
            }
            return yaws;
        }

        // Index of the untaken fragment with the smallest angle to the aim, or -1 when all are taken.
        public static int Nearest(float[] anglesDeg, bool[] taken)
        {
            int best = -1;
            for (int i = 0; i < anglesDeg.Length; i++)
                if (!taken[i] && (best < 0 || anglesDeg[i] < anglesDeg[best])) best = i;
            return best;
        }

        // Tongs: fill while JEPIT is held, the fragment is in the reticle and the phone is steady; anything else lets go.
        public static float StepGrip(float grip, bool held, float angleDeg, float jolt, float dt) =>
            held && angleDeg <= GripAngleDeg && jolt < GripShakeMax ? Mathf.Min(1f, grip + dt / GripSec) : 0f;

        // Every fragment in the lead box (`left` = still out) slows the dose; it never stops while you are near the ore.
        public static float StepDose(float dose, int left, float dt)
        {
            float share = DoseFloorShare + (1f - DoseFloorShare) * Mathf.Clamp(left, 0, FragmentCount) / FragmentCount;
            return Mathf.Clamp01(dose + DoseBasePerSec * share * dt);
        }

        public enum Outcome { None, Captured, Overdose }

        // Grabbing the last fragment on the frame the dose fills still counts.
        public static Outcome Check(float dose, int collected) =>
            collected >= FragmentCount ? Outcome.Captured : dose >= 1f ? Outcome.Overdose : Outcome.None;

        // Half speed, half dose left.
        public static float Performance(float elapsed, float dose) =>
            Mathf.Clamp01(0.5f * Mathf.InverseLerp(ParSec * 2.5f, ParSec, elapsed) + 0.5f * (1f - Mathf.Clamp01(dose)));
    }
}
```

Note: `ARCapturePrototype.Uranium.cs` will not compile after this step (it still uses the reactor API). That is fixed in Task 2; do Steps 4–5 after Task 2 Step 1 if the editor refuses to run tests with compile errors.

- [ ] **Step 4: Run tests to verify they pass** (after Task 2 Step 1 compiles)

`npx -y unity-mcp-cli run-tool tests-run --input '{"testMode":"EditMode"}'`
Expected: `"Status": "Passed"`, `FailedTests: 0`.

- [ ] **Step 5: Commit** — together with Task 2 (the assembly only compiles with both).

---

### Task 2: Uranium Collect phase (scene glue)

**Files:**
- Modify (full rewrite): `Assets/EnerGo/Scripts/ARCapturePrototype.Uranium.cs`
- Modify: `Assets/EnerGo/Scripts/ARCapturePrototype.cs` — lines ~579 and ~601 (drag-look guard), ~800 (comment), ~2023-2029 (compass sector)

**Interfaces:**
- Consumes: Task 1 `UraniumMath` API; existing `AtBearing`, `compass.North`, `arCamera`, `PointerScreen()`, `ReadGravity(out Vector3)`, `HudScale`, `ReticleRadius`, `capturePerformance`, `finishAt`, `FinishDelay`, `nextSpawnTime`, `feedback`, `target`, `energyMaterial`, `DrawHudBar`, styles `styleRadarAlert/styleRadarLocked/styleTapsCounter`, colours `HudOk/HudWarn/HudFill/HudOutline`.
- Produces: `UraniumPhase { None, Search, Collect, Done, Failed }`, field `bool uraniumGripHeld` (read by `HandleInputs`).

- [ ] **Step 1: Replace the whole of `ARCapturePrototype.Uranium.cs`**

```csharp
using UnityEngine;
using UnityEngine.InputSystem;

namespace EnerGo
{
    // Uranium "Pemburu Geiger" (v3, Ambil dengan Aman): follow the compass sector and the RAD meter to the hidden ore,
    // then pick its fragments up with the tongs (hold JEPIT while aiming) before the radiation dose fills up.
    public partial class ARCapturePrototype
    {
        private enum UraniumPhase { None, Search, Collect, Done, Failed }

        private static readonly Color UraniumGlow = new Color(0.22f, 1f, 0.08f); // #39FF14
        private const float UraniumOreSize = 0.22f, UraniumFragmentSize = 0.14f, UraniumOreDistance = 1.6f;
        private const float UraniumFlySec = 0.35f;  // a grabbed fragment drops into the lead box

        private UraniumPhase uraniumPhase = UraniumPhase.None;
        private Transform uraniumOre;               // hidden until the Geiger counter finds it
        private Transform[] uraniumFragments;
        private bool[] uraniumTaken;
        private float[] uraniumTakenAt, uraniumFragAngles;
        private int uraniumNearest = -1, uraniumCollected;
        private float uraniumAngle = 180f;          // camera forward vs the ore (Search) or the nearest fragment (Collect), degrees
        private float uraniumLockHold;              // seconds aimed within RevealAngleDeg
        private float uraniumNextClickAt, uraniumLastPulseAt = -10f, uraniumLastGripTickAt = -10f;
        private float uraniumGrip, uraniumDose, uraniumGripLostAt = -10f;
        private bool uraniumGripHeld;               // finger on JEPIT (also blocks drag-look)
        private float uraniumGravity;
        private int uraniumMeter, uraniumTrendFrom, uraniumTrend;   // RAD 0..100, value at the start of the trend window, -1/0/+1
        private float uraniumTrendAt;
        private GUIStyle uraniumRadStyle;
        private float uraniumRevealAt, uraniumPhaseAt;
        private string uraniumFailText = "";

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

        // ponytail: primitive ore cluster; swap for Resources/Uranium/uranium when a real model exists.
        private Transform MakeUraniumOre(string name, Material mat, float size, Vector3 worldPos)
        {
            var ore = new GameObject(name).transform;
            for (int i = 0; i < 4; i++)
            {
                var lump = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Destroy(lump.GetComponent<Collider>());
                lump.transform.SetParent(ore, false);
                lump.transform.localPosition = i == 0 ? Vector3.zero : Random.onUnitSphere * 0.45f;
                lump.transform.localScale = Vector3.one * (i == 0 ? 1f : Random.Range(0.45f, 0.65f));
                lump.GetComponent<Renderer>().sharedMaterial = mat;
            }
            ore.position = worldPos;
            ore.localScale = Vector3.one * size;
            ore.SetParent(target.transform, true);
            return ore;
        }

        private void SpawnUranium()
        {
            // The target is a container at the player; the ore and later its fragments hang under it, fixed to north.
            target = new GameObject("Target Uranium");
            target.transform.position = arCamera.transform.position;
            target.transform.SetParent(compass.North, true);
            float pitch = Random.Range(UraniumMath.HideMinPitchDeg, UraniumMath.HideMaxPitchDeg);
            uraniumOre = MakeUraniumOre("Ore", UnlitColor(UraniumGlow), UraniumOreSize, AtBearing(0f, pitch, UraniumOreDistance));
            uraniumOre.gameObject.SetActive(false);

            uraniumPhase = UraniumPhase.Search;
            uraniumFragments = null;
            uraniumTaken = null;
            uraniumTakenAt = uraniumFragAngles = null;
            uraniumNearest = -1;
            uraniumCollected = 0;
            uraniumAngle = 180f;
            uraniumLockHold = 0f;
            uraniumNextClickAt = Time.time;
            uraniumGrip = 0f;
            uraniumGripHeld = false;
            uraniumGripLostAt = -10f;
            uraniumDose = 0f;
            uraniumMeter = uraniumTrendFrom = 0;
            uraniumTrend = 0;
            uraniumTrendAt = Time.time;
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
                    uraniumAngle = AngleTo(uraniumOre.position);
                    UpdateRadMeter();
                    TickGeiger(UraniumMath.GeigerInterval(uraniumAngle));
                    uraniumLockHold = uraniumAngle <= UraniumMath.RevealAngleDeg ? uraniumLockHold + dt : 0f;
                    if (uraniumLockHold >= UraniumMath.RevealHoldSec) BeginUraniumCollect();
                    break;
                case UraniumPhase.Collect:
                    StepUraniumCollect(dt);
                    break;
                case UraniumPhase.Failed:
                    if (Time.time - uraniumPhaseAt >= UraniumMath.FailShowSec) RetryUranium();
                    return; // RetryUranium may have destroyed the target
            }
            AnimateUraniumFragments(dt);
        }

        private float AngleTo(Vector3 worldPos) =>
            Vector3.Angle(arCamera.transform.forward, worldPos - arCamera.transform.position);

        private void UpdateRadMeter()
        {
            uraniumMeter = UraniumMath.Meter(uraniumAngle);
            if (Time.time - uraniumTrendAt < UraniumMath.TrendWindowSec) return;
            uraniumTrend = UraniumMath.Trend(uraniumTrendFrom, uraniumMeter);
            uraniumTrendFrom = uraniumMeter;
            uraniumTrendAt = Time.time;
        }

        // One regular Geiger click (and a vibration pulse, rate-limited) every `interval` seconds.
        private void TickGeiger(float interval)
        {
            if (Time.time < uraniumNextClickAt) return;
            if (SFXManager.Instance != null) SFXManager.Instance.PlayGeigerClick();
            if (Time.time - uraniumLastPulseAt >= 1f / UraniumMath.MaxPulsesPerSec)
            {
                Haptics.Pulse(12);
                uraniumLastPulseAt = Time.time;
            }
            uraniumNextClickAt = Time.time + interval;
        }

        // The ore breaks into fragments: the first where the ore was, the rest spread around the player.
        private void BeginUraniumCollect()
        {
            uraniumPhase = UraniumPhase.Collect;
            uraniumPhaseAt = uraniumRevealAt = Time.time;
            var mat = UnlitColor(UraniumGlow);
            float[] yaws = UraniumMath.FragmentYaws(() => Random.value);
            int n = UraniumMath.FragmentCount;
            uraniumFragments = new Transform[n];
            uraniumTaken = new bool[n];
            uraniumTakenAt = new float[n];
            uraniumFragAngles = new float[n];
            for (int i = 0; i < n; i++)
            {
                Vector3 pos = i == 0 ? uraniumOre.position
                    : AtBearing(yaws[i], Random.Range(UraniumMath.HideMinPitchDeg, UraniumMath.HideMaxPitchDeg), UraniumOreDistance);
                uraniumFragments[i] = MakeUraniumOre("Fragment " + i, mat, UraniumFragmentSize, pos);
            }
            Destroy(uraniumOre.gameObject);
            uraniumOre = null;
            uraniumCollected = 0;
            uraniumGrip = 0f;
            uraniumDose = 0f;
            uraniumGravity = 0f;
            Haptics.Pulse(80);
            feedback = "BIJIH PECAH! AMBIL SERPIHANNYA";
        }

        private void StepUraniumCollect(float dt)
        {
            for (int i = 0; i < uraniumFragments.Length; i++)
                uraniumFragAngles[i] = uraniumTaken[i] ? 180f : AngleTo(uraniumFragments[i].position);
            uraniumNearest = UraniumMath.Nearest(uraniumFragAngles, uraniumTaken);
            uraniumAngle = uraniumNearest >= 0 ? uraniumFragAngles[uraniumNearest] : 180f;
            UpdateRadMeter();
            TickGeiger(UraniumMath.GeigerInterval(uraniumAngle));

            uraniumGripHeld = ReadUraniumGrip(out float jolt);
            float before = uraniumGrip;
            uraniumGrip = UraniumMath.StepGrip(uraniumGrip, uraniumGripHeld, uraniumAngle, jolt, dt);
            if (before > 0.15f && uraniumGrip == 0f) { uraniumGripLostAt = Time.time; Haptics.Pulse(40); }
            if (uraniumGrip > 0f && Time.time - uraniumLastGripTickAt >= 0.2f) { Haptics.Pulse(8); uraniumLastGripTickAt = Time.time; }
            if (uraniumGrip >= 1f && uraniumNearest >= 0)
            {
                uraniumTaken[uraniumNearest] = true;
                uraniumTakenAt[uraniumNearest] = Time.time;
                uraniumCollected++;
                uraniumGrip = 0f;
                Haptics.Pulse(60);
                if (SFXManager.Instance != null) SFXManager.Instance.PlayButtonClick();
                feedback = "SERPIHAN MASUK WADAH TIMBAL";
            }

            uraniumDose = UraniumMath.StepDose(uraniumDose, UraniumMath.FragmentCount - uraniumCollected, dt);
            switch (UraniumMath.Check(uraniumDose, uraniumCollected))
            {
                case UraniumMath.Outcome.Overdose: FailUranium("DOSIS BERLEBIH!"); break;
                case UraniumMath.Outcome.Captured:
                    uraniumPhase = UraniumPhase.Done;
                    uraniumPhaseAt = Time.time;
                    uraniumGripHeld = false;
                    capturePerformance = UraniumMath.Performance(Time.time - uraniumRevealAt, uraniumDose);
                    finishAt = Time.time + FinishDelay;
                    feedback = "URANIUM AMAN DI WADAH TIMBAL";
                    break;
            }
        }

        // JEPIT is held by a finger (or the mouse / Space in the editor); the accelerometer measures shaking.
        private bool ReadUraniumGrip(out float jolt)
        {
            jolt = 0f;
            if (ReadGravity(out Vector3 a))
            {
                if (uraniumGravity <= 0f) uraniumGravity = a.magnitude;
                jolt = Mathf.Abs(a.magnitude / uraniumGravity - 1f);
            }
            if (Keyboard.current != null && Keyboard.current.spaceKey.isPressed) return true;
            Vector2? p = PointerScreen();
            if (p == null) return false;
            UraniumGripRect(Screen.width / HudScale, Screen.height / HudScale, out Rect btn);
            Vector2 gui = new Vector2(p.Value.x, Screen.height - p.Value.y) / HudScale;
            return new Rect(btn.x - 16f, btn.y - 16f, btn.width + 32f, btn.height + 32f).Contains(gui); // generous for thumbs
        }

        // Right side, thumb height: aim with the phone, hold with the thumb.
        private static void UraniumGripRect(float sw, float sh, out Rect btn) =>
            btn = new Rect(sw - 16f - 104f, sh * 0.52f, 104f, 104f);

        // Fragments pop in and glow; a grabbed one drops toward the bottom of the view and disappears.
        private void AnimateUraniumFragments(float dt)
        {
            if (uraniumFragments == null) return;
            for (int i = 0; i < uraniumFragments.Length; i++)
            {
                var f = uraniumFragments[i];
                if (f == null || !f.gameObject.activeSelf) continue;
                if (uraniumTaken[i])
                {
                    float k = (Time.time - uraniumTakenAt[i]) / UraniumFlySec;
                    if (k >= 1f) { f.gameObject.SetActive(false); continue; }
                    Vector3 box = arCamera.transform.position + arCamera.transform.forward * 0.5f - arCamera.transform.up * 0.4f;
                    f.position = Vector3.Lerp(f.position, box, k);
                    f.localScale = Vector3.one * UraniumFragmentSize * (1f - k);
                    continue;
                }
                float pop = Mathf.Clamp01((Time.time - uraniumRevealAt) / 0.3f);
                bool gripping = i == uraniumNearest && uraniumGrip > 0f;
                f.localScale = Vector3.one * UraniumFragmentSize * pop * (1f + 0.08f * Mathf.Sin(Time.time * (gripping ? 18f : 6f)));
                f.Rotate(0f, 20f * dt, 0f, Space.Self);
            }
        }

        private void FailUranium(string text)
        {
            uraniumPhase = UraniumPhase.Failed;
            uraniumPhaseAt = Time.time;
            uraniumGripHeld = false;
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
                    line = uraniumLockHold > 0f ? "SINYAL KUAT! TAHAN ARAH HP" : "IKUTI SEKTOR HIJAU · CARI RAD TERTINGGI";
                    warn = false;
                    bar = uraniumMeter / 100f;
                    label = uraniumMeter.ToString();
                    break;
                case UraniumPhase.Done:
                    line = "URANIUM AMAN DI WADAH TIMBAL!";
                    warn = false;
                    bar = 1f;
                    label = UraniumMath.FragmentCount + "/" + UraniumMath.FragmentCount;
                    break;
                case UraniumPhase.Failed:
                    line = uraniumFailText;
                    warn = true;
                    bar = uraniumCollected / (float)UraniumMath.FragmentCount;
                    label = uraniumCollected + "/" + UraniumMath.FragmentCount;
                    break;
                default:
                    bool lost = Time.time - uraniumGripLostAt < 1f;
                    line = lost ? "JEPITAN LEPAS! PEGANG HP STABIL"
                        : uraniumGrip > 0f ? "MENJEPIT... TAHAN"
                        : uraniumAngle <= UraniumMath.GripAngleDeg ? "TAHAN TOMBOL JEPIT"
                        : "CARI SERPIHAN · IKUTI RAD";
                    warn = lost || uraniumDose > 0.75f;
                    bar = uraniumCollected / (float)UraniumMath.FragmentCount;
                    label = uraniumCollected + "/" + UraniumMath.FragmentCount;
                    break;
            }
            EnerGoUI.DrawCard(new Rect(x, y, w, 64f), EnerGoUI.Radius, warn ? HudWarn : HudOk, 2f);
            GUI.Label(new Rect(x + 10f, y + 8f, w - 20f, 20f), line, warn ? styleRadarAlert : styleRadarLocked);
            DrawHudBar(x + 20f, y + 36f, w - 40f, bar, warn ? HudWarn : HudOk, label);
        }

        // Small light pill with centred Ink text, kept 8 px inside the screen edges; readable over the camera feed.
        private void DrawHudTag(float cx, float y, float w, string text, float sw)
        {
            if (hudTagStyle == null)
                hudTagStyle = new GUIStyle(styleTapsCounter) { alignment = TextAnchor.MiddleCenter };
            var r = new Rect(Mathf.Clamp(cx - w * 0.5f, 8f, sw - 8f - w), y, w, 22f);
            EnerGoUI.DrawPanel(r, HudFill, HudOutline, 11f, 1.5f);
            GUI.Label(r, text, hudTagStyle);
        }
        private GUIStyle hudTagStyle;

        // Search and Collect: aiming reticle + big RAD meter with a trend line. Collect adds the JEPIT button and the dose bar.
        private void DrawUraniumOverlay(float sw, float sh)
        {
            if (uraniumPhase != UraniumPhase.Search && uraniumPhase != UraniumPhase.Collect) return;
            float lockAngle = uraniumPhase == UraniumPhase.Search ? UraniumMath.RevealAngleDeg : UraniumMath.GripAngleDeg;
            bool near = uraniumAngle <= lockAngle;
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
            if (uraniumPhase == UraniumPhase.Search) return;

            // JEPIT: round button; the inner disc grows with the grip.
            UraniumGripRect(sw, sh, out Rect btn);
            EnerGoUI.DrawPanel(btn, uraniumGripHeld ? EnerGoUI.Orange : HudFill, EnerGoUI.Ink, btn.width * 0.5f, 2.5f);
            if (uraniumGrip > 0f)
            {
                float gr = btn.width * 0.5f * uraniumGrip;
                EnerGoUI.DrawPanel(new Rect(btn.center.x - gr, btn.center.y - gr, gr * 2f, gr * 2f), EnerGoUI.Alpha(UraniumGlow, 0.8f), Color.clear, gr);
            }
            if (hudTagStyle == null) hudTagStyle = new GUIStyle(styleTapsCounter) { alignment = TextAnchor.MiddleCenter };
            GUI.Label(btn, "JEPIT", hudTagStyle);

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

- [ ] **Step 2: Patch `ARCapturePrototype.cs`**

Drag-look guard, both places (`~579` touch, `~601` editor right mouse): replace `uraniumPhase != UraniumPhase.Secure` with `!uraniumGripHeld`.

```csharp
                else if (touch.phase == UnityEngine.InputSystem.TouchPhase.Moved && isDraggingLook && isUsingGyro && !goldPanning && !uraniumGripHeld)
```
```csharp
                if (Mouse.current.rightButton.isPressed && isUsingGyro && !uraniumGripHeld)
```

`CheckTapOnTarget` (`~800`) comment:
```csharp
            if (resourceId == ResourceIds.Uranium) return false; // found by listening, picked up with JEPIT
```

Compass strip (`~2023`): the green sector only during Search (fragments are guided by RAD), and never fall through to the generic marker for uranium:
```csharp
            if (resourceId == ResourceIds.Uranium)
            {
                if (uraniumPhase == UraniumPhase.Search)
                {
                    float c = CompassMath.StripX(captureBearing, heading, half), w = CompassMath.UraniumSectorHalfDeg / half;
                    float lo = Mathf.Max(-1f, c - w), hi = Mathf.Min(1f, c + w);
                    if (hi > lo) EnerGoUI.DrawPanel(new Rect(X(lo), r.y + 4f, X(hi) - X(lo), r.height - 8f), EnerGoUI.Alpha(EnerGoUI.Success, 0.3f), Color.clear, 6f);
                    else StripEdge(r, c, Mathf.Abs(Mathf.DeltaAngle(heading, captureBearing)) - CompassMath.UraniumSectorHalfDeg);
                }
            }
```

Also grep the whole scripts folder for leftover `UraniumPhase.Secure` and fix any hit:
`grep -rn "UraniumPhase.Secure\|uraniumRod\|uraniumR\b\|UraniumSpikeSoon" Assets/EnerGo/Scripts` → expected: no output.

- [ ] **Step 3: Compile and run tests**

`npx -y unity-mcp-cli run-tool assets-refresh --input '{}'`, then `console-get-logs` with `{"logTypeFilter":"Error"}` → no new compile errors.
`npx -y unity-mcp-cli run-tool tests-run --input '{"testMode":"EditMode"}'` → `Passed`, 0 failed.

- [ ] **Step 4: Editor smoke check** (play mode does not tick on its own — see memory note "Unity MCP screenshot recipe"): open `Sensor360Scene` in play mode with resource uranium, call `SpawnUranium()` then `BeginUraniumCollect()` via reflection in `script-execute`, take a `ScreenCapture.CaptureScreenshot` and confirm: 3 glowing fragments exist under `Target Uranium` (`target.transform.childCount == 3`), JEPIT button right-middle, dose bar bottom, RAD meter above it, no overlap with the bottom "MUNCULKAN ULANG TARGET" button.

- [ ] **Step 5: Commit**

```bash
git add Assets/EnerGo/Scripts/Core/UraniumMath.cs Assets/EnerGo/Tests/Editor/UraniumMathTests.cs \
        Assets/EnerGo/Scripts/ARCapturePrototype.Uranium.cs Assets/EnerGo/Scripts/ARCapturePrototype.cs
git commit -m "feat: uranium v3 — pick up 3 fragments with the tongs before the dose fills (Waktu, Jarak, Perisai)

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 3: Uranium guide cards and quiz question

**Files:**
- Modify: `Assets/EnerGo/Scripts/Core/GuideBook.cs:6` (enum) and `:45-50` (uranium cards)
- Modify: `Assets/EnerGo/Scripts/GuideViewer.cs:155-160` (remove `Art.Slider` case)
- Modify: `Assets/EnerGo/Scripts/QuizBank.cs` (constant near line 63, question after `uranium-07` at line 224)

- [ ] **Step 1: GuideBook** — enum without `Slider`:

```csharp
        public enum Art { Reticle, Compass, Tap, PhoneFlat, PhoneSwirl, Hold, Layers, PumpRing, RadMeter, Shake }
```

Uranium cards:

```csharp
                case ResourceIds.Uranium: return new[]
                {
                    new Card("Ikuti sinyal RAD", "Hadap ke sektor hijau di kompas, lalu putar pelan sampai angka RAD paling tinggi.", Art.RadMeter),
                    new Card("Jepit serpihan", "Bijih pecah jadi 3 serpihan di sekelilingmu. Arahkan lingkaran ke serpihan, lalu tahan JEPIT. Penjepit menjaga jarak tanganmu dari radiasi.", Art.Hold),
                    new Card("Cepat & stabil", "Dosis terus naik, tiap serpihan di wadah timbal memperlambatnya. HP terguncang, jepitan lepas.", Art.Shake)
                };
```

- [ ] **Step 2: GuideViewer** — delete the whole `case GuideBook.Art.Slider:` block (5 drawing lines + `break;`).

- [ ] **Step 3: QuizBank** — add after the `RadiationLife` constant:

```csharp
        private const string Alara = "https://www.cdc.gov/radiation-health/safety/alara.html";
```

and after the `uranium-07` line:

```csharp
            Q("uranium-08", ResourceIds.Uranium, "Tiga cara melindungi diri dari radiasi adalah membatasi waktu, menjaga jarak, dan memakai perisai.", true, "Prinsip ALARA: sesingkat mungkin di dekat sumber, sejauh mungkin darinya, dan pasang penghalang seperti timbal.", Alara),
```

(Source verified 2026-10-11: the CDC page explains ALARA with time, distance and shielding.)

- [ ] **Step 4: Compile and run tests** — same commands as Task 2 Step 3. Expected: no errors, all EditMode tests pass, `grep -rn "Art.Slider" Assets/EnerGo` → no output.

- [ ] **Step 5: Commit**

```bash
git add Assets/EnerGo/Scripts/Core/GuideBook.cs Assets/EnerGo/Scripts/GuideViewer.cs Assets/EnerGo/Scripts/QuizBank.cs
git commit -m "feat: uranium guide cards for the tongs, ALARA quiz question

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 4: Vibration in Matahari, Batubara, Emas and Kuis

**Files:**
- Modify: `Assets/EnerGo/Scripts/ARCapturePrototype.cs` — `UpdateSun` (~717-770), `HitCoal` (~920-955), `UpdateGoldPan` (~1099), `StepGoldPieces` (~1154), `CheckGoldOutcome` (~1160-1180), quiz answer (~697-706), `UpdateQuizTimer` (~506-512)

**Interfaces:** Consumes `Haptics.Pulse(long ms)`. Adds fields `sunLastTickAt`, `goldLastRumbleAt` (private floats, declare next to the other sun/gold fields).

- [ ] **Step 1: Fields** — next to the sun fields (search `private float sunProgress`) and gold fields (search `private bool goldRough`):

```csharp
        private float sunLastTickAt = -10f;   // soft tick while the light is in the reticle
```
```csharp
        private float goldLastRumbleAt = -10f; // warning rumble while panning too roughly
```

- [ ] **Step 2: Matahari** — in `UpdateSun`, right after `sunProgress = StepSunProgress(...)`:

```csharp
            if (sunInside && Time.time - sunLastTickAt >= 0.25f) { Haptics.Pulse(8); sunLastTickAt = Time.time; }
```
In the `Captured` case, before `sunDone = true;`: `Haptics.Pulse(80);`
In the `TooLittle` case, before `Destroy(target);`: `Haptics.Pulse(300);`

- [ ] **Step 3: Batubara** — in `HitCoal`: after `PlayBaraPukul()` line add `Haptics.Pulse(20);`; after `PlayBaraHancur()` line add `Haptics.Pulse(50);`; inside `if (CoalBroken() == coalTotal)` before `feedback = "SEMUA BATU BARA TERKUMPUL";` add `Haptics.Pulse(120);`.

- [ ] **Step 4: Emas**
  - Leveling → Countdown (`~1099`):
    ```csharp
                    if (goldFlatHold >= GoldFlatHoldSec) { goldPhase = GoldPhase.Countdown; goldPhaseAt = Time.time; Haptics.Pulse(40); }
    ```
  - Panning, after `StepGoldPieces(...)`:
    ```csharp
                    if (goldRough && Time.time - goldLastRumbleAt >= 0.3f) { Haptics.Pulse(10); goldLastRumbleAt = Time.time; }
    ```
  - `StepGoldPieces`, after `goldLost[best] = true;`: `Haptics.Pulse(120);`
  - `CheckGoldOutcome`: in the failed branch before `feedback = "EMAS TERBUANG!";` add `Haptics.Pulse(300);`; in the done branch before `feedback = "EMAS TERPISAH DARI PASIR";` add `Haptics.Pulse(80);`

- [ ] **Step 5: Kuis** — in the swipe answer block:

```csharp
                        if (session.correct)
                        {
                            SFXManager.Instance.PlayQuizBenar();
                            Haptics.Pulse(30);
                        }
                        else
                        {
                            SFXManager.Instance.PlayQuizSalah();
                            Haptics.Pulse(200);
                        }
```
Note the surrounding `if (SFXManager.Instance != null && session != null)` would also skip vibration when SFXManager is missing; restructure so haptics only depend on `session != null`:
```csharp
                    var session = EnerGoProgress.Current.pendingCapture;
                    if (session != null)
                    {
                        Haptics.Pulse(session.correct ? 30 : 200);
                        if (SFXManager.Instance != null)
                        {
                            if (session.correct) SFXManager.Instance.PlayQuizBenar();
                            else SFXManager.Instance.PlayQuizSalah();
                        }
                    }
```
(Use this second form.) In `UpdateQuizTimer`, next to `PlayQuizSalah()`: `Haptics.Pulse(200);`

- [ ] **Step 6: Compile and run tests** — same commands as Task 2 Step 3. Expected: no errors, all pass. `grep -c "Haptics.Pulse" Assets/EnerGo/Scripts/ARCapturePrototype.cs` → 13.

- [ ] **Step 7: Commit**

```bash
git add Assets/EnerGo/Scripts/ARCapturePrototype.cs
git commit -m "feat: vibration in sun, coal, gold and the quiz

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 5: Vibration when a radar target comes into range

**Files:**
- Modify: `Assets/EnerGo/Scripts/RadarController.cs` — fields (top of class), `Refresh()` (~50-56)

- [ ] **Step 1: Field** next to `nearby`:

```csharp
        private readonly HashSet<string> inRangeIds = new HashSet<string>(); // to buzz once when a spawn comes into range
```

- [ ] **Step 2: `Refresh()`** — append after the `selectedId` line:

```csharp
            bool entered = false;
            var now = new HashSet<string>();
            foreach (var s in nearby)
            {
                if (!ResourceIds.IsPlayable(s.ResourceId)) continue;
                if (!GeoSpawner.CanCapture(GeoSpawner.DistanceM(feed.Lat, feed.Lon, s.Lat, s.Lon), feed.AccuracyM)) continue;
                now.Add(s.Id);
                if (!inRangeIds.Contains(s.Id)) entered = true;
            }
            inRangeIds.Clear();
            inRangeIds.UnionWith(now);
            if (entered) Haptics.Pulse(60);
```

- [ ] **Step 3: Compile and run tests** — same commands. Expected: no errors, all pass.

- [ ] **Step 4: Commit**

```bash
git add Assets/EnerGo/Scripts/RadarController.cs
git commit -m "feat: vibrate when a radar spawn comes into capture range

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 6: Build APK and device checklist

- [ ] **Step 1: Build** (tool calls time out at 60 s, so queue the build and poll a result file):

```bash
cd /d/code/unity/EnerGo
cat > "$TMP/b.json" <<'EOF'
{"isMethodBody":true,"csharpCode":"System.IO.File.Delete(\"Builds/uranium3-result.txt\"); UnityEditor.EditorApplication.delayCall += () => { var scenes = System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Select(System.Linq.Enumerable.Where(UnityEditor.EditorBuildSettings.scenes, s => s.enabled), s => s.path)); var r = UnityEditor.BuildPipeline.BuildPlayer(new UnityEditor.BuildPlayerOptions { scenes = scenes, locationPathName = \"Builds/EnerGo-Uranium3.apk\", target = UnityEditor.BuildTarget.Android, options = UnityEditor.BuildOptions.None }); System.IO.File.WriteAllText(\"Builds/uranium3-result.txt\", r.summary.result + \" errors=\" + r.summary.totalErrors); };"}
EOF
npx -y unity-mcp-cli run-tool script-execute --input-file - < "$TMP/b.json"
# then poll (background): until [ -f Builds/uranium3-result.txt ]; do sleep 10; done; cat Builds/uranium3-result.txt
```
Expected: `Succeeded errors=0`, `Builds/EnerGo-Uranium3.apk` exists.

- [ ] **Step 2: Device checklist (user, Samsung A16)**
  - Uranium: find the ore → 3 fragments appear around you; Geiger/RAD lead to the nearest; hold JEPIT 1 s while aimed → fragment drops away, `1/3`; shaking or releasing → "JEPITAN LEPAS!"; holding JEPIT does not rotate the view; let the dose fill → "DOSIS BERLEBIH!" → new ore, no leftover fragments; 3/3 → quiz.
  - Vibration: sun tick while locked; coal hits/breaks; gold level/rough/spill/done; quiz right (short) vs wrong (long); radar buzz on entering range.
  - Pengaturan → Getaran off → none of the above vibrates.

- [ ] **Step 3: Update memory** — mark `settings-uranium3-progress.md` with the new commits and APK name.
