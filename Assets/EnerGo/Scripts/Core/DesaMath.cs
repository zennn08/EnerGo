using System;
using UnityEngine;

namespace EnerGo
{
    public enum DesaStatus { Padam, Menipis, Menyala }

    // Rules of the village power plant. The stock is stored as one UTC end time, so nothing has to simulate
    // the hours the app was closed. Time is always passed in, so every rule can be tested.
    public static class DesaMath
    {
        public const long CapacitySec = 86400;
        public const long LowSec = 10800;
        public const int NameMaxLength = 16;
        public const string DefaultName = "Desa Teknik";
        public const int PracticeUnits = 1;

        public static long SecondsPerUnit(string resourceId)
        {
            switch (resourceId)
            {
                case ResourceIds.Sun: return 1200;
                case ResourceIds.Coal: return 1200;
                case ResourceIds.Gold: return 1800;
                case ResourceIds.Oil: return 2700;
                case ResourceIds.Uranium: return 10800;
                default: throw new ArgumentException("Unknown resource: " + resourceId);
            }
        }

        // Clamped, so a phone clock moved back can never show more than the capacity.
        public static long Remaining(long endsUtc, long nowUtc) => Math.Min(CapacitySec, Math.Max(0L, endsUtc - nowUtc));

        public static DesaStatus StatusOf(long remaining) =>
            remaining <= 0 ? DesaStatus.Padam : remaining < LowSec ? DesaStatus.Menipis : DesaStatus.Menyala;

        // Units that would overflow the capacity are refused, not cut, so none is wasted.
        public static int UnitsThatFit(long remaining, long secPerUnit, int owned) =>
            (int)Math.Min(owned, Math.Max(0L, CapacitySec - remaining) / secPerUnit);

        public static long EndsAfterFill(long endsUtc, long nowUtc, long secPerUnit, int units) =>
            Math.Max(nowUtc, endsUtc) + secPerUnit * units;

        public static void Spend(ref int normal, ref int pure, int units)
        {
            int fromNormal = Math.Min(normal, units);
            normal -= fromNormal;
            pure -= units - fromNormal;
        }

        public static string CleanName(string raw)
        {
            if (raw == null) return DefaultName;
            var chars = raw.ToCharArray();
            for (int i = 0; i < chars.Length; i++)
                if (char.IsControl(chars[i])) chars[i] = ' ';
            string name = new string(chars).Trim();
            if (name.Length > NameMaxLength) name = name.Substring(0, NameMaxLength).TrimEnd();
            return name.Length == 0 ? DefaultName : name;
        }

        // A radar capture pays 1 + correct answers + mini-game bonus; a practice round always pays one unit.
        public static int CaptureReward(bool fromRadar, int correct, int bonus) =>
            fromRadar ? 1 + correct + bonus : PracticeUnits;

        // Where the point (u, v) of an image lands when it is drawn "cover" inside a box, in box units.
        public static Vector2 CoverPoint(float imgW, float imgH, float boxW, float boxH, float u, float v)
        {
            float scale = Mathf.Max(boxW / imgW, boxH / imgH);
            return new Vector2((boxW - imgW * scale) / 2f + imgW * scale * u, (boxH - imgH * scale) / 2f + imgH * scale * v);
        }
    }
}
