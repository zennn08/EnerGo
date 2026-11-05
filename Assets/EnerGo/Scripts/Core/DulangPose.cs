using UnityEngine;

namespace EnerGo
{
    // Pose of the 3D gold pan, kept free of scene objects so it can be tested.
    // Pan-local axes: x = screen right, z = screen up, y = toward the viewer (the pan is seen from above).
    public static class DulangPose
    {
        public const float MaxTiltDeg = 22f;
        public const float DegPerTilt = 60f; // goldTilt is the smoothed sensor tilt (about 0.1–0.4 while panning)

        // The pan's normal tips toward the low side, so that side sinks away from the viewer.
        public static Quaternion Tilt(Vector2 tilt)
        {
            var downhill = new Vector3(tilt.x, 0f, tilt.y);
            float angle = Mathf.Min(downhill.magnitude * DegPerTilt, MaxTiltDeg);
            if (angle < 0.001f) return Quaternion.identity;
            return Quaternion.AngleAxis(angle, Vector3.Cross(Vector3.up, downhill).normalized);
        }

        // World size of `radiusPx` screen pixels at `distance` metres in front of a perspective camera.
        public static float WorldRadius(float radiusPx, float distance, float fovDeg, float screenHeightPx) =>
            radiusPx * 2f * distance * Mathf.Tan(fovDeg * 0.5f * Mathf.Deg2Rad) / screenHeightPx;
    }
}
