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
