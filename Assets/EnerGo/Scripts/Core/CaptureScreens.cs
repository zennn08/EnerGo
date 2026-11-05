namespace EnerGo
{
    public enum CaptureScreen { Play, Quiz, Feedback, Result }

    // Which capture screen shows, and small rules of its display. Kept free of scene objects so they can be tested.
    public static class CaptureScreens
    {
        public static CaptureScreen For(bool hasPending, string state)
        {
            if (!hasPending) return CaptureScreen.Play;
            if (state == "feedback") return CaptureScreen.Feedback;
            if (state == "result") return CaptureScreen.Result;
            return CaptureScreen.Quiz;
        }

        public static bool ShowsHudFrame(CaptureScreen screen) => screen == CaptureScreen.Play;

        public const float PanelWidth = 480f;      // PanelSettings: every phone is 480 units wide
        public const float SwipeThreshold = 90f;   // panel units to commit an answer

        public static float CardRotation(float offsetUnits) => UnityEngine.Mathf.Clamp(offsetUnits * 0.08f, -18f, 18f);

        public static bool PastThreshold(float offsetUnits) => UnityEngine.Mathf.Abs(offsetUnits) >= SwipeThreshold;

        // A wrong answer holds the button 2 s so the explanation gets read; 1 = ready.
        public static float ReadHold(bool correct, float secondsSinceShown) => correct ? 1f : UnityEngine.Mathf.Clamp01(secondsSinceShown / 2f);

        // The pump's ring shrinks from 2.6 x the target to the target over one beat; press when they meet.
        public static float PumpRingScale(float secsSinceStroke, float beat) => UnityEngine.Mathf.Lerp(2.6f, 1f, secsSinceStroke / beat);

        public static float UraniumHeat(int meter) => UnityEngine.Mathf.Clamp01(meter / 100f);

        public static bool GaugeWarn(float value, float limit) => value > limit;

        // No check or cross marks in the text: no project font has them, the chip shows an icon instead.
        public static string AnswerChip(bool statementIsTrue) => statementIsTrue ? "Pernyataan ini BENAR" : "Pernyataan ini SALAH";

        public static string ResultMessage(int right, int count) => right == count ? "Sempurna! Semua jawaban tepat."
            : right == 0 ? "Belum ada yang tepat. Coba lagi di tangkapan berikutnya."
            : "Bagus! Jawab semua dengan tepat untuk hasil Murni.";
    }
}
