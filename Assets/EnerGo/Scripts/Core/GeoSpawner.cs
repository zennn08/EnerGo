using System;
using System.Collections.Generic;
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

    public static partial class GeoSpawner
    {
        public const double CellDeg = 0.0005;  // ~55 m in Indonesia
        public const long WindowSeconds = 900; // spawns change every 15 minutes

        // APPEND-ONLY: reordering or inserting changes every spawn in the world.
        private static readonly string[] Types = { ResourceIds.Sun, ResourceIds.Coal, ResourceIds.Gold, ResourceIds.Oil, ResourceIds.Uranium };
        private static readonly double[] Weights = { 35, 30, 20, 12, 3 };

        public readonly struct Spawn
        {
            public readonly string Id;
            public readonly string ResourceId;
            public readonly double Lat;
            public readonly double Lon;
            public Spawn(string id, string resourceId, double lat, double lon) { Id = id; ResourceId = resourceId; Lat = lat; Lon = lon; }
        }

        public static long CellOf(double deg) => (long)Math.Floor(deg / CellDeg);
        public static long WindowOf(long unixUtcSeconds) => (long)Math.Floor(unixUtcSeconds / (double)WindowSeconds);
        public static long SecondsToNextWindow(long unixUtcSeconds) => (WindowOf(unixUtcSeconds) + 1) * WindowSeconds - unixUtcSeconds;
        public static bool IsDaytime(int localHour) => localHour >= 6 && localHour < 18;

        // 0 (60%), 1 (30%) or 2 (10%) spawns at random spots in the cell. The draws happen in the same order
        // whatever the hour, so day/night only ever changes a spawn's type, never its position.
        public static List<Spawn> SpawnsInCell(long cellX, long cellY, long window, int localHour)
        {
            var list = new List<Spawn>(2);
            ulong state = Seed(cellX, cellY, window);
            double r = Next01(ref state);
            int count = r < 0.6 ? 0 : r < 0.9 ? 1 : 2;
            bool day = IsDaytime(localHour);
            for (int i = 0; i < count; i++)
            {
                double lat = (cellY + Next01(ref state)) * CellDeg;
                double lon = (cellX + Next01(ref state)) * CellDeg;
                string type = PickType(Next01(ref state), day);
                list.Add(new Spawn(cellX + ":" + cellY + ":" + window + ":" + i, type, lat, lon));
            }
            return list;
        }

        // Every spawn within radiusM of the player, nearest first, minus the ones this player already caught.
        public static List<Spawn> Nearby(double lat, double lon, double radiusM, long unixUtcSeconds, int localHour, ICollection<string> captured)
        {
            long window = WindowOf(unixUtcSeconds);
            double dLat = radiusM / MetersPerDegree;
            double dLon = radiusM / (MetersPerDegree * Math.Max(0.01, Math.Cos(lat * Math.PI / 180.0)));
            var result = new List<Spawn>();
            for (long y = CellOf(lat - dLat); y <= CellOf(lat + dLat); y++)
            for (long x = CellOf(lon - dLon); x <= CellOf(lon + dLon); x++)
                foreach (var s in SpawnsInCell(x, y, window, localHour))
                    if ((captured == null || !captured.Contains(s.Id)) && DistanceM(lat, lon, s.Lat, s.Lon) <= radiusM)
                        result.Add(s);
            result.Sort((a, b) => DistanceM(lat, lon, a.Lat, a.Lon).CompareTo(DistanceM(lat, lon, b.Lat, b.Lon)));
            return result;
        }

        // Captured ids only matter while their window can still be on screen (current and the one before).
        public static string[] PruneCaptured(string[] ids, long window)
        {
            var keep = new List<string>();
            if (ids != null)
                foreach (var id in ids)
                {
                    var parts = id?.Split(':');
                    if (parts != null && parts.Length == 4 && long.TryParse(parts[2], out long w) && w >= window - 1) keep.Add(id);
                }
            return keep.ToArray();
        }

        private static string PickType(double u, bool day)
        {
            double total = 0;
            for (int i = 0; i < Types.Length; i++) if (day || Types[i] != ResourceIds.Sun) total += Weights[i];
            double t = u * total;
            for (int i = 0; i < Types.Length; i++)
            {
                if (!day && Types[i] == ResourceIds.Sun) continue;
                if (t < Weights[i]) return Types[i];
                t -= Weights[i];
            }
            return Types[Types.Length - 1];
        }

        private static ulong Seed(long cellX, long cellY, long window)
        {
            unchecked
            {
                ulong s = (ulong)cellX * 0x9E3779B97F4A7C15UL ^ (ulong)cellY * 0xC2B2AE3D27D4EB4FUL ^ (ulong)window * 0x165667B19E3779F9UL;
                SplitMix(ref s);
                return s;
            }
        }

        // splitmix64: tiny, fast, identical on every platform (unlike System.Random / UnityEngine.Random).
        private static ulong SplitMix(ref ulong state)
        {
            unchecked
            {
                state += 0x9E3779B97F4A7C15UL;
                ulong z = state;
                z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
                z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
                return z ^ (z >> 31);
            }
        }

        private static double Next01(ref ulong state) => (SplitMix(ref state) >> 11) * (1.0 / 9007199254740992.0);
    }
}
