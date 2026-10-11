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
