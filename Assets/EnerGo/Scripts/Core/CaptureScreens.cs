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
    }
}
