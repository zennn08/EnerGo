using UnityEngine;

namespace EnerGo
{
    // Short vibration pulses. Android: Vibrator.vibrate(ms) so pulses can be short and frequent;
    // otherwise Handheld.Vibrate (~0.5 s, so rate-limited). Referencing Handheld.Vibrate also makes
    // Unity add the VIBRATE permission to the Android manifest.
    public static class Haptics
    {
        private static AndroidJavaObject vibrator;
        private static bool triedVibrator;
        private static float lastFallbackAt = -10f;

        public static void Pulse(long ms)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (!triedVibrator)
            {
                triedVibrator = true;
                try
                {
                    using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                    using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                        vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
                }
                catch (System.Exception e) { Debug.LogWarning("EnerGo haptics: " + e.Message); }
            }
            if (vibrator != null)
            {
                try { vibrator.Call("vibrate", ms); return; }
                catch (System.Exception) { vibrator = null; }
            }
#endif
#if UNITY_ANDROID || UNITY_IOS
            if (Time.unscaledTime - lastFallbackAt < 0.5f) return;
            lastFallbackAt = Time.unscaledTime;
            Handheld.Vibrate();
#endif
        }
    }
}
