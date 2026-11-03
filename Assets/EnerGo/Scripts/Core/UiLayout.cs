using UnityEngine;

namespace EnerGo
{
    // Layout math for the Arcade Cerah UI, kept free of GUI calls so it can be tested.
    public static class UiLayout
    {
        // Mockup v5 is drawn on a 300 px phone; the game lays out 480 GUI units across.
        public const float Mock = 1.6f;

        public static float SafeTop(float screenH, float safeYMax, float scale) => Mathf.Max(16f, (screenH - safeYMax) / scale + 12f);

        // Unsafe strip at the bottom of the screen (gesture bar, cutout), converted to UI Toolkit panel units.
        public static float PanelBottomInset(float safeYMin, float screenW, float panelW) =>
            screenW <= 0f ? 0f : Mathf.Max(0f, safeYMin) * panelW / screenW;

        public static float SafeBottom(float safeYMin, float scale) => Mathf.Max(20f, safeYMin / scale + 16f);

        // Cells fill row by row; a short last row is centred under the full ones.
        public static Rect[] Grid(Rect area, int count, int cols, float gap, float cellH)
        {
            var cells = new Rect[Mathf.Max(0, count)];
            float cellW = (area.width - gap * (cols - 1)) / cols;
            for (int i = 0; i < cells.Length; i++)
            {
                int row = i / cols, col = i % cols;
                int inRow = Mathf.Min(cols, cells.Length - row * cols);
                float rowW = cellW * inRow + gap * (inRow - 1);
                float x0 = area.x + (area.width - rowW) * 0.5f;
                cells[i] = new Rect(x0 + col * (cellW + gap), area.y + row * (cellH + gap), cellW, cellH);
            }
            return cells;
        }

        public static int TotalUnits(int[] perResource)
        {
            if (perResource == null) return 0;
            int total = 0;
            foreach (int n in perResource) if (n > 0) total += n;
            return total;
        }

        // Largest size <= `size` whose width (measured at `size`) fits, never below `minSize`.
        public static int FitFontSize(float textWidthAtSize, int size, float maxWidth, int minSize)
        {
            if (textWidthAtSize <= maxWidth || textWidthAtSize <= 0f) return size;
            return Mathf.Clamp(Mathf.FloorToInt(size * maxWidth / textWidthAtSize), minSize, size);
        }

        public static string ResultTitle(int right, int count) => right >= count ? "SEMPURNA!" : right <= 0 ? "COBA LAGI" : "BAGUS!";

        public static int Stars(int right) => Mathf.Clamp(right, 0, 3);
    }
}
