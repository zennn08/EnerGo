using UnityEngine;

namespace EnerGo
{
    // Compass-anchored AR: angles are degrees, headings/bearings clockwise from north, yaws clockwise from Unity +Z.
    public static class CompassMath
    {
        public const float StripHalfSpanDeg = 60f;      // the HUD compass strip shows ±60° around the view direction
        public const float MaxCalibratePitchDeg = 60f;  // looking further up/down makes the phone compass unreliable
        public const float OffsetSmoothing = 2f;        // per second; slow so a magnetic hiccup doesn't move objects
        public const float FirstFixTimeoutSec = 1.5f;   // spawns wait this long for the first compass reading
        public const float UraniumSectorHalfDeg = 45f;  // uranium shows only a rough sector, not its exact bearing

        private static readonly string[] Labels = { "U", "TL", "T", "TG", "S", "BD", "B", "BL" };

        public static float CameraHeading(float cameraYawDeg, float offsetDeg) => Mathf.Repeat(cameraYawDeg + offsetDeg, 360f);

        public static float OffsetTarget(float compassHeadingDeg, float cameraYawDeg) => Mathf.Repeat(compassHeadingDeg - cameraYawDeg, 360f);

        public static bool CanCalibrate(float cameraPitchDeg) => Mathf.Abs(cameraPitchDeg) <= MaxCalibratePitchDeg;

        public static float SmoothOffset(float current, float target, float dt) =>
            Mathf.Repeat(Mathf.LerpAngle(current, target, 1f - Mathf.Exp(-OffsetSmoothing * dt)), 360f);

        // -1 = left edge, 0 = centre, +1 = right edge of the strip; beyond ±1 the marker is off the strip.
        public static float StripX(float bearingDeg, float headingDeg, float halfSpanDeg) =>
            Mathf.DeltaAngle(headingDeg, bearingDeg) / halfSpanDeg;

        // Unity world yaw of a compass bearing; the North transform is rotated by -offset so its +Z is true north.
        public static float WorldYaw(float bearingDeg, float offsetDeg) => bearingDeg - offsetDeg;

        public static string DirectionLabel(int deg) => Labels[((Mathf.RoundToInt(deg / 45f) % 8) + 8) % 8];
    }
}
