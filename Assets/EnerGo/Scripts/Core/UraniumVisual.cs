using UnityEngine;

namespace EnerGo
{
    // Timing and motion of the uranium models, display only: the Geiger search, grip and dose rules are in UraniumMath.
    // Kept free of scene objects so it can be tested.
    public static class UraniumVisual
    {
        public const float BreakSec = 0.55f;     // the found ore shakes this long, then breaks
        public const float ChunkLifeSec = 0.9f;
        public const float LidDropSec = 0.35f;
        public const float LidRestY = 0.275f;    // the flask rim, sda_uranium_wadah model units
        const float PopSec = 0.3f, ChunkGravity = 1.4f, LidDropHeight = 0.45f;

        // Seconds since the ore was found -> fragment size 0..1.
        public static float FragmentPop(float t) => t < BreakSec ? 0f : Mathf.SmoothStep(0f, 1f, (t - BreakSec) / PopSec);

        public static float OreShake(float t)
        {
            float k = Mathf.Clamp01(t / BreakSec);
            return 0.006f + 0.016f * k * k;
        }

        public static Vector3 ChunkOffset(Vector3 velocity, float t) => velocity * t + Vector3.down * (0.5f * ChunkGravity * t * t);

        public static float ChunkScale(float t) => 1f - Mathf.SmoothStep(0f, 1f, (t - ChunkLifeSec * 0.5f) / (ChunkLifeSec * 0.5f));

        public static float LidY(float t)
        {
            float u = Mathf.Clamp01(t / LidDropSec);
            float bounce = t > LidDropSec ? 0.015f * Mathf.Exp(-(t - LidDropSec) * 12f) * Mathf.Abs(Mathf.Sin((t - LidDropSec) * 18f)) : 0f;
            return LidRestY + LidDropHeight * (1f - u * u) + bounce;
        }

        // Seconds since a fragment landed -> how much the flask squashes and rebounds.
        public static float FlaskBump(float t) => t < 0f ? 0f : Mathf.Exp(-t * 6f) * Mathf.Sin(t * 27f);
    }
}
