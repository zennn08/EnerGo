using UnityEngine;
using UnityEngine.InputSystem;

namespace EnerGo
{
    // Oil "Bor & Pompa": hold to drill down through the cross-section and stop inside the thin oil band
    // (the bit has momentum; rushing through the gas pocket blows out), then press and release in rhythm
    // to pump the drum full without building up too much pressure.
    public partial class ARCapturePrototype
    {
        public enum OilPhase { None, Drill, Pump, Done, Failed }
        private const float OilModelScale = 0.2f;  // sda_pompa is about 3.5 m wide; at 1.4 m this fills the view like the old rig
        private const float OilCrankRate = 9f;     // how fast the crank catches up with the finger

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
        private Transform oilBeam, oilCrank, oilPitman, oilRod, oilCable;
        private float oilCrankAngle, oilCrankTarget;

        private OilMath.Layer OilLayerNow => OilMath.LayerAt(oilDepth, oilLayers);

        private void SpawnOil()
        {
            var model = Resources.Load<GameObject>("Oil/sda_pompa");
            if (model == null)
            {
                nextSpawnTime = float.PositiveInfinity;
                feedback = "Aset model pompa belum tersedia di Resources.";
                Debug.LogError("EnerGo: missing Resources/Oil/sda_pompa.");
                return;
            }
            target = new GameObject("Target Minyak Bumi");
            var visual = Instantiate(model, target.transform);
            oilBeam = visual.transform.Find("sda_pompa_lengan");
            oilCrank = visual.transform.Find("sda_pompa_engkol");
            oilPitman = visual.transform.Find("sda_pompa_pitman");
            oilRod = visual.transform.Find("sda_pompa_tali");
            oilCable = visual.transform.Find("sda_pompa_kabel");
            oilCrankAngle = oilCrankTarget = Mathf.PI * 1.5f; // rests with the head up
            PoseOilPump();
            // Target sits at the model's centre, so the radar and aiming point at the middle of the rig.
            if (TryGetBounds(visual, out var bounds)) visual.transform.position -= bounds.center;
            target.transform.localScale = Vector3.one * OilModelScale;

            Vector3 fwd = compass.Direction(captureBearing, 0f);
            target.transform.position = arCamera.transform.position + fwd * 1.4f + Vector3.up * 0.05f;
            target.transform.rotation = Quaternion.LookRotation(fwd, Vector3.up); // beam runs left-right, seen side-on
            target.transform.SetParent(compass.North, true);
            EnsureModelLight();
            oilLightRotation = Quaternion.LookRotation(fwd, Vector3.up) * Quaternion.Euler(35f, -30f, 0f); // lit from the viewer's side
            modelLight.transform.rotation = oilLightRotation;

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

        // Puts the moving parts where the crank angle says; the mechanism itself is in PompaPose.
        private void PoseOilPump()
        {
            var pose = PompaPose.At(oilCrankAngle);
            oilBeam.localRotation = Quaternion.Euler(0f, 0f, pose.HeadDownDeg);
            oilCrank.localRotation = Quaternion.Euler(0f, 0f, pose.CrankDeg);
            oilPitman.localPosition = new Vector3(pose.Pin.x, pose.Pin.y, 0f);
            oilPitman.localRotation = Quaternion.Euler(0f, 0f, pose.PitmanDeg);
            var rod = oilRod.localPosition; rod.y = pose.CarrierY; oilRod.localPosition = rod;
            oilCable.localScale = new Vector3(1f, pose.CableLength, 1f);
        }

        // Finger (or mouse / Space in the editor) held anywhere except the top and bottom HUD bars.
        private bool OilHeld()
        {
            if (Keyboard.current != null && Keyboard.current.spaceKey.isPressed) return true;
            Vector2? p = PointerScreen();
            if (p == null) return false;
            return !OverHudBand(p.Value);
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
                    // The rig follows the finger: each press or release turns the crank half a turn, always forward.
                    if (held != oilWasHeld) oilCrankTarget = PompaPose.NextCrankTarget(oilCrankTarget, held);
                    oilCrankAngle = Mathf.Lerp(oilCrankAngle, oilCrankTarget, 1f - Mathf.Exp(-OilCrankRate * dt));
                    if (target != null) PoseOilPump();
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

        private HudStatus ReadOilHud()
        {
            if (oilPhase == OilPhase.Failed)
                return HudStatus.Failed(oilFailText, oilPumping ? oilDrum : oilDepth, oilPumping ? Mathf.FloorToInt(oilDrum * 100f) + "%" : Mathf.RoundToInt(oilDepth * OilMath.DisplayDepthM) + "m");
            if (oilPhase == OilPhase.Done)
                return HudStatus.WithBar("DRUM MINYAK PENUH!", false, 1f, "100%");
            if (oilPhase == OilPhase.Pump)
            {
                bool recent = Time.time - oilLastJudgeAt < 0.6f;
                string line = !recent ? "TEKAN SAAT LINGKARAN PAS"
                    : oilLastJudge == OilMath.Stroke.Perfect ? "PAS! TERUS POMPA"
                    : oilLastJudge == OilMath.Stroke.Early ? "TERLALU CEPAT! TEKANAN NAIK" : "TERLAMBAT · IKUTI IRAMA";
                return HudStatus.WithBar(line, oilPumpPressure > 0.6f || (recent && oilLastJudge == OilMath.Stroke.Early), oilDrum, Mathf.FloorToInt(oilDrum * 100f) + "%");
            }
            var layer = OilLayerNow;
            bool gas = layer == OilMath.Layer.Gas;
            string drill = gas ? "KANTONG GAS! PELAN-PELAN"
                : layer == OilMath.Layer.Oil ? "LAPISAN MINYAK! LEPAS SEKARANG"
                : "TAHAN UNTUK MENGEBOR · " + OilLayerNames[(int)layer].ToUpperInvariant();
            return HudStatus.WithBar(drill, gas && oilGasPressure > 0.4f, oilDepth, Mathf.RoundToInt(oilDepth * OilMath.DisplayDepthM) + "m");
        }

        private static void OilSectionRect(float sw, float sh, out Rect p)
        {
            float pw = Mathf.Min(sw - 72f, 300f), ph = Mathf.Min(sh * 0.48f, 420f);
            p = new Rect((sw - pw) * 0.5f, sh * 0.3f, pw, ph);
        }

    }
}
