using System;
using UnityEngine;

namespace EnerGo
{
    // World spawns for the radar plus the small geo helpers it needs. Pure functions only: everything here is
    // deterministic so every phone computes the same SDA for the same place and time without a server.
    public static partial class GeoSpawner
    {
        public const double EarthRadiusM = 6371000.0;
        public static readonly double MetersPerDegree = EarthRadiusM * Math.PI / 180.0;
        public const double CaptureRadiusM = 20.0;
        public const double MaxAccuracyBonusM = 15.0;   // a bad fix can stretch the capture radius by at most this
        public const double MaxPlausibleSpeedMps = 50.0; // faster than this is a GPS glitch, not walking
        private const float HeadingSmoothing = 6f;       // higher = snappier compass

        private static readonly string[] DirectionNames =
            { "Utara", "Timur Laut", "Timur", "Tenggara", "Selatan", "Barat Daya", "Barat", "Barat Laut" };

        // Equirectangular approximation: error well under a centimetre below 1 km.
        public static double DistanceM(double lat1, double lon1, double lat2, double lon2)
        {
            Offset(lat1, lon1, lat2, lon2, out double east, out double north);
            return Math.Sqrt(east * east + north * north);
        }

        public static double BearingDeg(double lat1, double lon1, double lat2, double lon2)
        {
            Offset(lat1, lon1, lat2, lon2, out double east, out double north);
            double deg = Math.Atan2(east, north) * 180.0 / Math.PI;
            return deg < 0 ? deg + 360.0 : deg;
        }

        private static void Offset(double lat1, double lon1, double lat2, double lon2, out double east, out double north)
        {
            double meanLat = (lat1 + lat2) * 0.5 * Math.PI / 180.0;
            east = (lon2 - lon1) * Math.Cos(meanLat) * MetersPerDegree;
            north = (lat2 - lat1) * MetersPerDegree;
        }

        public static string Direction8(double bearingDeg) =>
            DirectionNames[(int)Math.Floor(((bearingDeg % 360 + 360) % 360 + 22.5) / 45.0) % 8];

        public static bool CanCapture(double distanceM, double accuracyM) =>
            distanceM - Math.Min(Math.Max(accuracyM, 0), MaxAccuracyBonusM) <= CaptureRadiusM;

        public static bool PlausibleMove(double meters, double seconds) =>
            seconds > 0 ? meters / seconds <= MaxPlausibleSpeedMps : meters <= MaxPlausibleSpeedMps * 0.2;

        // Low-pass on a circle: 359° -> 1° moves 2°, not 358°.
        public static float SmoothHeading(float current, float target, float dt) =>
            Mathf.Repeat(Mathf.LerpAngle(current, target, 1f - Mathf.Exp(-HeadingSmoothing * dt)), 360f);
    }
}
