using UnityEngine;

namespace EnerGo
{
    // Rules for closing a bottom sheet by swiping it down. Units are UI units, velocity is units per second downwards.
    public static class SheetSwipe
    {
        public const float StartDistance = 10f, CloseShare = 0.25f, FlickSpeed = 600f, FlickMinDistance = 16f;

        public static float Offset(float dragY) => Mathf.Max(0f, dragY);

        public static bool StartsDrag(float dragY) => dragY >= StartDistance;

        public static bool ShouldClose(float dragY, float sheetHeight, float velocity) =>
            dragY >= sheetHeight * CloseShare || (dragY >= FlickMinDistance && velocity >= FlickSpeed);
    }
}
