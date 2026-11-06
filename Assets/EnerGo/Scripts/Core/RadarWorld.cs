using System;
using UnityEngine;

namespace EnerGo
{
    // Rules of the 3D radar map. There is no map service: the land grows from its coordinates, so the same place
    // always looks the same without being the real streets.
    public static class RadarWorld
    {
        public enum PropKind { Pohon, Palem, Semak, Batu, Rumah }

        public struct Prop
        {
            public PropKind Kind;
            public float X, Z, Scale, Yaw;
        }

        public const float CellM = 120f;
        private const int MaxPropsPerCell = 7;

        // Metres east and north. The east scale uses the whole-degree latitude, so a place keeps its cell between sessions.
        public static void ToMetres(double lat, double lon, out double east, out double north)
        {
            north = lat * GeoSpawner.MetersPerDegree;
            east = lon * GeoSpawner.MetersPerDegree * Math.Cos(Math.Round(lat) * Math.PI / 180.0);
        }

        public static long CellOf(double metres) => (long)Math.Floor(metres / CellM);

        // A spawn's place around the player: x east, y north.
        public static Vector2 Around(double bearingDeg, double distanceM)
        {
            double a = bearingDeg * Math.PI / 180.0;
            return new Vector2((float)(Math.Sin(a) * distanceM), (float)(Math.Cos(a) * distanceM));
        }

        public static Prop[] PropsInCell(long cellX, long cellZ)
        {
            ulong state = (ulong)cellX * 0x9E3779B97F4A7C15UL ^ (ulong)cellZ * 0xC2B2AE3D27D4EB4FUL ^ 0x165667B19E3779F9UL;
            var props = new Prop[(int)(Next(ref state) * (MaxPropsPerCell + 1))];
            for (int i = 0; i < props.Length; i++)
            {
                float roll = Next(ref state);
                props[i] = new Prop
                {
                    Kind = roll < 0.45f ? PropKind.Pohon : roll < 0.65f ? PropKind.Palem : roll < 0.85f ? PropKind.Semak : roll < 0.96f ? PropKind.Batu : PropKind.Rumah,
                    X = (Next(ref state) - 0.5f) * (CellM - 12f),
                    Z = (Next(ref state) - 0.5f) * (CellM - 12f),
                    Scale = 0.85f + Next(ref state) * 0.35f,
                    Yaw = Next(ref state) * 360f
                };
            }
            return props;
        }

        // SplitMix64: the same numbers on every phone, unlike a culture or platform dependent generator.
        private static float Next(ref ulong state)
        {
            state += 0x9E3779B97F4A7C15UL;
            ulong z = state;
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            z ^= z >> 31;
            return (float)((z >> 40) / (double)(1UL << 24));
        }

        // panah.svg points up, so the turn is simply the bearing as seen from where the player faces.
        public static float ArrowRotation(double bearingDeg, float headingDeg) => (float)(bearingDeg - headingDeg);

        // Index of the live point nearest the tap, or -1 when none is within reach.
        public static int NearestWithin(Vector2 tap, Vector2[] points, int count, float reach)
        {
            int best = -1;
            float bestSq = reach * reach;
            for (int i = 0; i < count; i++)
            {
                float d = (points[i] - tap).sqrMagnitude;
                if (d <= bestSq) { bestSq = d; best = i; }
            }
            return best;
        }
    }
}
