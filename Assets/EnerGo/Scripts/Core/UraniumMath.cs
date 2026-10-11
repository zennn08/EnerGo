using UnityEngine;

namespace EnerGo
{
    // Pure rules of the uranium "Pemburu Geiger" mini-game. r = reactivity 0..1, rod = control rod 0 (out) .. 1 (fully in).
    public static class UraniumMath
    {
        // Where the ore hides, relative to the camera's starting direction.
        public const float HideMinYawDeg = 60f, HideMaxYawDeg = 150f;
        public const float HideMinPitchDeg = -10f, HideMaxPitchDeg = 15f;
        // Search: aim within this cone for this long to reveal the ore.
        public const float RevealAngleDeg = 10f, RevealHoldSec = 1.5f;
        public const float MinClicksPerSec = 1f, MaxClicksPerSec = 25f;
        public const float MaxPulsesPerSec = 12f;   // vibration pulses blur together faster than this
        // Secure: reactivity.
        public const float GreenMin = 0.40f, GreenMax = 0.60f, HotAbove = 0.80f;
        public const float Growth = 0.05f;          // per second: an unchecked chain reaction creeps up
        public const float RodPull = 0.9f;          // per second: r is pulled toward (1 - rod)
        public const float NoiseGain = 0.25f;       // per second, Perlin noise in [-1, 1] times this
        public const float JoltDeadzone = 0.08f;    // accelerometer jitter below this is not a shake
        public const float JoltGain = 2.0f;         // per second per unit of jolt above the deadzone
        public const float SpikeSize = 0.20f;       // neutron spike, added at once
        public const float SpikeMinSec = 3f, SpikeMaxSec = 6f, SpikeWarnSec = 0.5f;
        public const float RodRangeDeg = 40f;       // tilting this far from the start moves the rod from 0 to 1
        public const float FillPerSec = 1f / 8f, DrainPerSec = 1f / 16f;
        public const float DosePerSec = 1f / 35f, HotDoseMul = 2f;
        public const float ParSec = 18f;            // a good secure phase, for the speed score
        public const float FailShowSec = 1.5f;

        public static float GeigerRate(float angleDeg)
        {
            float t = Mathf.Clamp01(angleDeg / 150f);
            return MinClicksPerSec * Mathf.Pow(MaxClicksPerSec / MinClicksPerSec, 1f - t);
        }

        // While securing, the counter follows the reactivity instead of the aim.
        public static float SecureClickRate(float r) => Mathf.Lerp(4f, MaxClicksPerSec, Mathf.Clamp01(r));

        // Radioactive decay is random: exponential gaps between clicks, mean 1 / rate.
        public static float NextClickDelay(float rate, float u01) => -Mathf.Log(Mathf.Max(u01, 1e-4f)) / Mathf.Max(rate, 0.01f);

        public static int Meter(float angleDeg) => Mathf.RoundToInt(100f * (1f - Mathf.Clamp(angleDeg, 0f, 180f) / 180f));

        // Forward tilt from the accelerometer: 0 = phone upright, 90 = lying flat screen up.
        public static float TiltDeg(Vector3 gravity) =>
            Mathf.Asin(Mathf.Clamp(-gravity.normalized.z, -1f, 1f)) * Mathf.Rad2Deg;

        public static float RodFromTilt(float tiltDeg, float baselineDeg) =>
            Mathf.Clamp01(0.5f + (tiltDeg - baselineDeg) / RodRangeDeg);

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
