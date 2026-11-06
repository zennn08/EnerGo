using UnityEngine;

namespace EnerGo
{
    // Rules and shape of gold panning, kept free of scene objects so they can be tested.
    // Tilt is in units of g (about the sine of the tilt angle); jolt is |accel|/gravity - 1.
    // Lengths are sda_dulang model units; heights are measured up from the pan's underside.
    public static class DulangMath
    {
        public const float FillPerSec = 1f / 10f;   // about 10 s of good swirling
        public const float SpillPerSec = 1f / 6f;   // too rough: the bar falls back
        public const float MinTilt = 0.08f;         // below this the pan is just being held level
        public const float MaxTilt = 0.6f;          // tipping the pan further than this spills
        public const float MinSwirl = 40f;          // deg/s of the tilt direction that counts as swirling
        public const float MaxTiltSpeed = 2.2f;     // g/s: how fast the tilt itself may move, about 420 deg/s at a 0.3 g tilt
        public const float MaxJolt = 0.5f;          // a shake or jerk stronger than this spills
        public const float RoughGraceSec = 0.25f;   // rough motion has to last this long before it counts
        public const float InnerRim = 0.235f;       // radius where the inner surface meets the lip

        // Smoothed signed turning speed (deg/s) of the tilt direction. Steady one-way circles give a large value;
        // rocking averages out. Near level the direction is meaningless, so it is not measured there.
        public static float StepSwirl(float swirl, ref float lastAngle, Vector2 tilt, float dt)
        {
            float k = 1f - Mathf.Exp(-6f * dt);
            if (tilt.magnitude < MinTilt) { lastAngle = float.NaN; return Mathf.Lerp(swirl, 0f, k * 0.35f); }
            float angle = Mathf.Atan2(tilt.y, tilt.x) * Mathf.Rad2Deg;
            if (!float.IsNaN(lastAngle)) swirl = Mathf.Lerp(swirl, Mathf.DeltaAngle(lastAngle, angle) / dt, k);
            lastAngle = angle;
            return swirl;
        }

        // Smoothed speed of the tilt vector itself: what "too fast" really is, and it stays small near level.
        public static float StepTiltSpeed(float speed, Vector2 before, Vector2 now, float dt) =>
            Mathf.Lerp(speed, (now - before).magnitude / dt, 1f - Mathf.Exp(-8f * dt));

        // One step of the panning bar. `rough` is only true once rough motion has lasted RoughGraceSec.
        public static float StepProgress(float progress, float swirl, float tiltSpeed, float tilt, float jolt, ref float roughFor, float dt, out bool rough)
        {
            bool tooMuch = tiltSpeed > MaxTiltSpeed || tilt > MaxTilt || jolt > MaxJolt;
            roughFor = tooMuch ? roughFor + dt : 0f;
            rough = roughFor >= RoughGraceSec;
            if (rough) return Mathf.Clamp01(progress - SpillPerSec * dt);
            if (!tooMuch && Mathf.Abs(swirl) >= MinSwirl && tilt >= MinTilt) return Mathf.Clamp01(progress + FillPerSec * dt);
            return progress; // held still or barely moving: nothing washes out
        }

        // Height of the pan's inner surface at a radius (the profile sda_dulang was modelled with).
        public static float InnerHeight(float r)
        {
            if (r <= 0.05f) return Mathf.Lerp(0.012f, 0.016f, r / 0.05f);
            if (r <= 0.15f) return Mathf.Lerp(0.016f, 0.045f, (r - 0.05f) / 0.10f);
            return Mathf.Lerp(0.045f, 0.078f, (r - 0.15f) / (InnerRim - 0.15f));
        }

        // Height of a gold piece's centre at a radius: it lies on the bowl, so only the sand above it hides it.
        public static float GoldHeight(float r) => InnerHeight(r) + 0.004f;

        // Radius at angle `phi` where a flat water surface (height `level` at the centre, rising by `slope` per unit) meets the wall.
        public static float WaterEdge(float phi, float level, Vector2 slope)
        {
            float rise = Mathf.Cos(phi) * slope.x + Mathf.Sin(phi) * slope.y;
            float lo = 0f, hi = InnerRim;
            if (level + rise * hi >= InnerHeight(hi)) return hi;
            for (int i = 0; i < 16; i++)
            {
                float mid = (lo + hi) * 0.5f;
                if (level + rise * mid > InnerHeight(mid)) lo = mid; else hi = mid;
            }
            return (lo + hi) * 0.5f;
        }

        public static float SandRadius(float progress) => 0.05f + 0.16f * Mathf.Sqrt(Mathf.Clamp01(1f - progress));

        // Depth of the sand mound at distance `d` from its centre: a dome that shrinks and thins as it washes out.
        public static float SandThickness(float d, float progress)
        {
            float left = Mathf.Clamp01(1f - progress);
            if (left <= 0f) return 0f;
            float k = 1f - d * d / (SandRadius(progress) * SandRadius(progress));
            return k <= 0f ? 0f : 0.024f * Mathf.Pow(left, 0.6f) * Mathf.Pow(k, 0.6f);
        }
    }
}
