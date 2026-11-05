using UnityEngine;

namespace EnerGo
{
    // Layout of the drilling diorama (sda_bor) in model units, kept free of scene objects so it can be tested.
    // The block is 1 wide; `height` is how tall the panel makes it. y = 0 is the surface, down is negative.
    public static class BorPose
    {
        public const float CrownY = 0.22f;      // where the pipe hangs from, inside the derrick
        public const float StartAbove = 0.165f; // the bit waits this far above ground before it is lowered

        // Thickness each layer's mesh was modelled at: soil, rock, cap rock, gas, oil, brine.
        public static readonly float[] NominalThickness = { 0.11f, 0.23f, 0.13f, 0.11f, 0.075f, 0.345f };

        public struct Slab { public float TopY, ScaleY; }

        // `bottoms` are the five layer bottoms from OilMath.Layers (0..1); brine fills the rest.
        public static Slab[] Slabs(float[] bottoms, float height)
        {
            var slabs = new Slab[NominalThickness.Length];
            float prev = 0f;
            for (int i = 0; i < slabs.Length; i++)
            {
                float bottom = i < bottoms.Length ? bottoms[i] : 1f;
                slabs[i] = new Slab { TopY = -prev * height, ScaleY = (bottom - prev) * height / NominalThickness[i] };
                prev = bottom;
            }
            return slabs;
        }

        // `lowered` 0..1: how far the bit has come down out of the derrick.
        public static float BitY(float depth, float height, float lowered) =>
            StartAbove * (1f - Mathf.SmoothStep(0f, 1f, lowered)) - depth * height;

        public static float HoleLength(float bitY) => Mathf.Max(0f, -bitY);

        public static float PipeLength(float bitY) => CrownY - bitY;
    }
}
