using UnityEngine;
using UnityEngine.InputSystem;

namespace EnerGo
{
    // Uranium "Pemburu Geiger" (v4): follow the compass sector and the RAD meter to the hidden ore, then find its invisible
    // fragments the same way, pick each up with the tongs (hold JEPIT while aiming) and close the lead box after it.
    // An open box drowns out the Geiger counter and shields nothing, so the dose keeps climbing until it is closed.
    public partial class ARCapturePrototype
    {
        public enum UraniumPhase { None, Search, Collect, Done, Failed }

        private static readonly Color UraniumGlow = new Color(0.22f, 1f, 0.08f); // #39FF14
        private const float UraniumFragmentSize = 1.8f, UraniumOreDistance = 1.6f; // sda_uranium_serpihan is 0.12 m long
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
        private bool uraniumBoxOpen;                // a fragment went in and the lid is still off
        private float uraniumBoxOpenedAt;
        private float uraniumGravity;
        private int uraniumMeter, uraniumTrendFrom, uraniumTrend;   // RAD 0..100, value at the start of the trend window, -1/0/+1
        private float uraniumTrendAt;
        private float uraniumRevealAt, uraniumPhaseAt;
        private string uraniumFailText = "";

        private void SpawnUranium()
        {
            // The target is a container at the player; the ore and later its fragments hang under it, fixed to north.
            target = new GameObject("Target Uranium");
            target.transform.position = arCamera.transform.position;
            target.transform.SetParent(compass.North, true);
            float pitch = Random.Range(UraniumMath.HideMinPitchDeg, UraniumMath.HideMaxPitchDeg);
            uraniumOre = new GameObject("Ore").transform; // hidden: only its position exists until the Geiger counter finds it
            uraniumOre.position = AtBearing(0f, pitch, UraniumOreDistance);
            uraniumOre.SetParent(target.transform, true);

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
            uraniumBoxOpen = false;
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
                    UpdateRadMeter(uraniumAngle);
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

        private void UpdateRadMeter(float signalAngle)
        {
            uraniumMeter = UraniumMath.Meter(signalAngle);
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
            StartUraniumBreak(uraniumOre.position);
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
                uraniumFragments[i] = MakeUraniumFragment(i, pos);
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
            float signal = UraniumMath.SignalAngle(uraniumAngle, uraniumBoxOpen);
            UpdateRadMeter(signal);
            TickGeiger(UraniumMath.GeigerInterval(signal));

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
                uraniumBoxOpen = true;
                uraniumBoxOpenedAt = Time.time;
                Haptics.Pulse(60);
                if (SFXManager.Instance != null) SFXManager.Instance.PlayButtonClick();
                feedback = "SERPIHAN MASUK WADAH TIMBAL";
            }

            int shielded = UraniumMath.Shielded(uraniumCollected, uraniumBoxOpen);
            uraniumDose = UraniumMath.StepDose(uraniumDose, UraniumMath.FragmentCount - shielded, dt);
            switch (UraniumMath.Check(uraniumDose, shielded))
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

        // TUTUP appears once the fragment has landed in the box.
        public bool UraniumBoxCloseable => uraniumPhase == UraniumPhase.Collect && uraniumBoxOpen && Time.time - uraniumBoxOpenedAt >= UraniumFlySec;

        public void CloseUraniumBox()
        {
            if (!UraniumBoxCloseable) return;
            uraniumBoxOpen = false;
            Haptics.Pulse(40);
            if (SFXManager.Instance != null) SFXManager.Instance.PlayButtonClick();
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

        // Left side, just above the flask (see PoseWadah).
        private static void UraniumCloseRect(float sh, out Rect btn) =>
            btn = new Rect(16f, sh - 456f, 104f, 104f);

        // Fragments are invisible until aimed close to, then glow; a grabbed one flies into the lead flask and disappears.
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
                    if (k >= 1f) { f.gameObject.SetActive(false); UraniumFragmentLanded(); continue; }
                    Vector3 box = UraniumFlaskMouth();
                    f.position = Vector3.Lerp(f.position, box, k);
                    f.localScale = Vector3.one * UraniumFragmentSize * (1f - k);
                    continue;
                }
                float pop = UraniumVisual.FragmentPop(Time.time - uraniumRevealAt);
                bool gripping = i == uraniumNearest && uraniumGrip > 0f;
                float reveal = UraniumVisual.FragmentReveal(uraniumFragAngles[i]);
                f.localScale = Vector3.one * UraniumFragmentSize * pop * reveal * (1f + 0.08f * Mathf.Sin(Time.time * (gripping ? 18f : 6f)));
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

        private HudStatus ReadUraniumHud()
        {
            string count = uraniumCollected + "/" + UraniumMath.FragmentCount;
            float got = uraniumCollected / (float)UraniumMath.FragmentCount;
            switch (uraniumPhase)
            {
                case UraniumPhase.Search:
                    return HudStatus.WithBar(uraniumLockHold > 0f ? "SINYAL KUAT! TAHAN ARAH HP" : "IKUTI SEKTOR HIJAU · CARI RAD TERTINGGI", false, uraniumMeter / 100f, uraniumMeter.ToString());
                case UraniumPhase.Done:
                    return HudStatus.WithBar("URANIUM AMAN DI WADAH TIMBAL!", false, 1f, UraniumMath.FragmentCount + "/" + UraniumMath.FragmentCount);
                case UraniumPhase.Failed:
                    return HudStatus.Failed(uraniumFailText, got, count);
                default:
                    bool lost = Time.time - uraniumGripLostAt < 1f;
                    string line = lost ? "JEPITAN LEPAS! PEGANG HP STABIL"
                        : uraniumGrip > 0f ? "MENJEPIT... TAHAN"
                        : uraniumBoxOpen ? "TUTUP WADAH! RADIASI BOCOR"
                        : uraniumAngle <= UraniumMath.GripAngleDeg ? "TAHAN TOMBOL JEPIT"
                        : "CARI SERPIHAN · IKUTI RAD";
                    return HudStatus.WithBar(line, lost || uraniumBoxOpen || uraniumDose > 0.75f, got, count);
            }
        }


    }
}
