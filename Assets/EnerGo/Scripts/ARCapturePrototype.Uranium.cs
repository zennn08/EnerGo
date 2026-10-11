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
            DrawHudTag(g.center.x, g.yMax + 4f, 80f, "REAKTOR", sw);

            var rod = new Rect(16f, gy, gw, gh);
            EnerGoUI.DrawPanel(rod, HudFill, HudOutline, 8f, 2f);
            EnerGoUI.DrawPanel(new Rect(rod.x + 8f, rod.y + 4f, gw - 16f, (gh - 8f) * uraniumRod), EnerGoUI.Ink, Color.clear, 4f);
            DrawHudTag(rod.center.x, rod.yMax + 4f, 80f, "BATANG", sw);

            float dw = Mathf.Min(sw - 64f, 320f);
            var dose = new Rect((sw - dw) * 0.5f, sh - 104f, dw, 34f);
            bool high = uraniumDose > 0.75f;
            EnerGoUI.DrawCard(dose, EnerGoUI.Radius, high ? HudWarn : HudOutline, 2f);
            GUI.Label(new Rect(dose.x + 10f, dose.y + 8f, 60f, 18f), "DOSIS", styleTapsCounter);
            EnerGoUI.DrawBar(new Rect(dose.x + 70f, dose.y + 11f, dw - 84f, 12f), uraniumDose, high ? EnerGoUI.Danger : EnerGoUI.Orange);
        }
    }
}
