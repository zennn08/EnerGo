using UnityEngine;

namespace EnerGo
{
    // Layout math for the UI, kept free of scene objects so it can be tested.
    public static class UiLayout
    {
        // Unsafe strip at the bottom of the screen (gesture bar, cutout), converted to UI Toolkit panel units.
        public static float PanelBottomInset(float safeYMin, float screenW, float panelW) =>
            screenW <= 0f ? 0f : Mathf.Max(0f, safeYMin) * panelW / screenW;

        public static float SafeBottom(float safeYMin, float scale) => Mathf.Max(20f, safeYMin / scale + 16f);

        public static int TotalUnits(int[] perResource)
        {
            if (perResource == null) return 0;
            int total = 0;
            foreach (int n in perResource) if (n > 0) total += n;
            return total;
        }

        public static string ResultTitle(int right, int count) => right >= count ? "SEMPURNA!" : right <= 0 ? "COBA LAGI" : "BAGUS!";

        public static int Stars(int right) => Mathf.Clamp(right, 0, 3);
    }
}
