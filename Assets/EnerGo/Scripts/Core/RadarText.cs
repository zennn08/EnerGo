using UnityEngine;

namespace EnerGo
{
    // Words on the exploration radar, kept free of scene objects so they can be tested.
    public static class RadarText
    {
        public enum Feed { Denied, Disabled, Failed }

        public static string Countdown(long secs) => "SPAWN BARU " + (secs / 60).ToString("00") + ":" + (secs % 60).ToString("00");

        public static string GpsLine(float accuracyM, float weakM) =>
            "GPS ±" + Mathf.RoundToInt(accuracyM) + " m" + (accuracyM > weakM ? " · Sinyal lemah, coba ke tempat terbuka" : "");

        public static string CompassLine(bool hasCompass, string source, float headingDeg) => hasCompass
            ? "Kompas: " + source + " · " + Mathf.RoundToInt(headingDeg) + "°"
            : "Kompas tidak tersedia, utara di atas";

        public static string TargetLine(double distanceM, double bearingDeg) =>
            Mathf.RoundToInt((float)distanceM) + " m · " + GeoSpawner.Direction8(bearingDeg);

        public static string CaptureButton(bool locked, bool inRange, double distanceM, out bool enabled)
        {
            enabled = !locked && inRange;
            if (locked) return "SEGERA HADIR";
            return inRange ? "TANGKAP" : "DEKATI DULU (" + Mathf.RoundToInt((float)distanceM) + " m)";
        }

        public static string FeedTitle(Feed feed)
        {
            switch (feed)
            {
                case Feed.Denied: return "Izin lokasi ditolak";
                case Feed.Disabled: return "Lokasi HP mati";
                default: return "GPS tidak merespons";
            }
        }

        public static string FeedBody(Feed feed)
        {
            switch (feed)
            {
                case Feed.Denied: return "Radar butuh lokasimu untuk menemukan SDA di sekitar.";
                case Feed.Disabled: return "Nyalakan Lokasi di pengaturan HP, lalu coba lagi.";
                default: return "Coba pindah ke tempat terbuka, lalu coba lagi.";
            }
        }
    }
}
