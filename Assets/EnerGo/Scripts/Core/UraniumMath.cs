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
