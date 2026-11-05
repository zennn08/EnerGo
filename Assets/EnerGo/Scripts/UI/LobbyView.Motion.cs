using UnityEngine;
using UnityEngine.UIElements;

namespace EnerGo.UI
{
    // Splash and transition motion, driven every frame from LobbyController's timers (same curves as the IMGUI version).
    public partial class LobbyView
    {
        private VisualElement splash, splashTanda, splashBarIsi, transisi, transisiTanda, transisiBarIsi;
        private Label splashAjakan, transisiTeks;

        partial void AnimateSplashAndTransition()
        {
            if (splash == null)
            {
                splash = root.Q("splash");
                splashTanda = root.Q("splash-tanda");
                splashBarIsi = root.Q("splash-bar").Q(className: "eg-bar__isi");
                splashAjakan = root.Q<Label>("splash-ajakan");
                transisi = root.Q("transisi");
                transisiTanda = root.Q("transisi-tanda");
                transisiTeks = root.Q<Label>("transisi-teks");
                transisiBarIsi = root.Q("transisi-bar").Q(className: "eg-bar__isi");
            }

            if (controller.State == LobbyController.ScreenState.Splash)
            {
                float t = controller.SplashTimer, d = controller.SplashSeconds;
                float alpha = 1f;
                if (t < LobbyController.SplashFadeIn) alpha = Mathf.Clamp01(t / LobbyController.SplashFadeIn);
                else if (t > d - LobbyController.SplashFadeOut) alpha = Mathf.Clamp01((d - t) / LobbyController.SplashFadeOut);
                splash.style.opacity = alpha;

                // The mark settles in from 88 % over 0.6 s; the crystal of the old splash did the same.
                float entrance = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / 0.6f));
                float k = Mathf.Lerp(0.88f, 1f, entrance);
                splashTanda.style.scale = new Scale(new Vector3(k, k, 1f));
                splashBarIsi.style.width = Length.Percent(100f * Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / (d - 0.2f))));

                bool prompt = t > 0.35f;
                splashAjakan.style.visibility = prompt ? Visibility.Visible : Visibility.Hidden;
                if (prompt) splashAjakan.style.opacity = 0.6f + 0.4f * Mathf.Sin(t * 4f);
            }
            else if (controller.State == LobbyController.ScreenState.TransitioningToAR)
            {
                float a = controller.TransitionProgress;
                transisi.style.opacity = a;
                transisiTeks.text = controller.TransitionText;
                transisiBarIsi.style.width = Length.Percent(100f * a);
                transisiTanda.style.rotate = new Rotate(-24f - 360f * a); // the compass needle spins while loading
            }
        }
    }
}
