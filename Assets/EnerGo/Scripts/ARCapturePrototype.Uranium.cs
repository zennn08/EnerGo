using UnityEngine;
using UnityEngine.InputSystem;

namespace EnerGo
{
    // Uranium "Pemburu Geiger" (v2): follow the compass sector and the RAD meter to the hidden ore, then keep the reactivity in the green zone with the control-rod slider before the radiation dose fills up.
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
        private float uraniumGravity;
        private int uraniumMeter, uraniumTrendFrom, uraniumTrend;   // RAD 0..100, value at the start of the trend window, -1/0/+1
        private float uraniumTrendAt;
        private GUIStyle uraniumRadStyle;
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

            float pitch = Random.Range(UraniumMath.HideMinPitchDeg, UraniumMath.HideMaxPitchDeg);
            target.transform.position = AtBearing(0f, pitch, UraniumOreDistance);
            target.transform.SetParent(compass.North, true);
            target.SetActive(false); // invisible until the Geiger counter finds it

            uraniumPhase = UraniumPhase.Search;
            uraniumAngle = 180f;
            uraniumLockHold = 0f;
            uraniumNextClickAt = Time.time;
            uraniumR = 0.5f;
            uraniumRod = 0.5f;
            uraniumDose = 0f;
            uraniumProgress = 0f;
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

        private void BeginUraniumSecure()
        {
            uraniumPhase = UraniumPhase.Secure;
            uraniumPhaseAt = uraniumRevealAt = Time.time;
            target.SetActive(true);
            uraniumR = 0.5f;
            uraniumRod = 0.5f;
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
            float drift = UraniumMath.Drift(Mathf.PerlinNoise(uraniumNoiseSeed + 37f, (Time.time - uraniumRevealAt) * UraniumMath.DriftSpeed));
            uraniumR = UraniumMath.StepReactivity(uraniumR, uraniumRod, jolt, spike, noise, dt, drift);
            uraniumProgress = UraniumMath.StepProgress(uraniumProgress, uraniumR, dt);
            uraniumDose = UraniumMath.StepDose(uraniumDose, uraniumR, dt);
            TickGeiger(1f / UraniumMath.SecureClickRate(uraniumR));

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
                    int meter = uraniumMeter;
                    line = uraniumLockHold > 0f ? "SINYAL KUAT! TAHAN ARAH HP" : "IKUTI SEKTOR HIJAU · CARI RAD TERTINGGI";
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
                        : uraniumR > UraniumMath.GreenMax ? "TERLALU PANAS · GESER BATANG KE BAWAH"
                        : uraniumR < UraniumMath.GreenMin ? "REAKSI MELEMAH · GESER BATANG KE ATAS"
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
    }
}
