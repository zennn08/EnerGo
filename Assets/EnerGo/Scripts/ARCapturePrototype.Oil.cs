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

            Vector3 fwd = compass.Direction(captureBearing, 0f);
            target.transform.position = arCamera.transform.position + fwd * 1.4f + Vector3.up * 0.05f;
            target.transform.rotation = Quaternion.LookRotation(fwd, Vector3.up); // beam runs left-right, seen side-on
            target.transform.SetParent(compass.North, true);

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
            if (float.IsNegativeInfinity(oilLastStrokeAt)) // first press only starts the rhythm
            {
                oilLastStrokeAt = Time.time;
                if (SFXManager.Instance != null) SFXManager.Instance.PlayPumpBeat();
                Haptics.Pulse(15);
                return;
            }
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
            var c = new Vector2(sw * 0.5f, sh * 0.7f);
            const float r0 = 44f;
            Rect Circle(float r) => new Rect(c.x - r, c.y - r, r * 2f, r * 2f);
            EnerGoUI.DrawPanel(Circle(r0), EnerGoUI.Alpha(EnerGoUI.Orange, 0.18f), EnerGoUI.Orange, r0, 3f);
            if (float.IsNegativeInfinity(oilLastStrokeAt))
                DrawHudTag(c.x, c.y + r0 + 10f, 240f, "TEKAN UNTUK MULAI MEMOMPA", sw);
            else
            {
                float t = Mathf.Clamp01((Time.time - oilLastStrokeAt) / beat);
                float r = Mathf.Lerp(r0 * 2.6f, r0, t);
                // Light ring with a dark rim: readable over both bright and dark camera feeds.
                EnerGoUI.DrawPanel(Circle(r + 1.5f), Color.clear, EnerGoUI.Alpha(EnerGoUI.Ink, 0.7f), r + 1.5f, 6f);
                EnerGoUI.DrawPanel(Circle(r), Color.clear, EnerGoUI.Paper, r, 3f);
            }
            DrawHudTag(c.x, c.y - 11f, 80f, "POMPA", sw);

            float dw = Mathf.Min(sw - 64f, 320f);
            var pr = new Rect((sw - dw) * 0.5f, sh - 104f, dw, 34f);
            bool high = oilPumpPressure > 0.6f;
            EnerGoUI.DrawCard(pr, EnerGoUI.Radius, high ? HudWarn : HudOutline, 2f);
            GUI.Label(new Rect(pr.x + 10f, pr.y + 8f, 70f, 18f), "TEKANAN", styleTapsCounter);
            EnerGoUI.DrawBar(new Rect(pr.x + 80f, pr.y + 11f, dw - 94f, 12f), oilPumpPressure, high ? EnerGoUI.Danger : EnerGoUI.Orange);
        }
    }
}
