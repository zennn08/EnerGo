using UnityEngine;
using UnityEngine.UIElements;

namespace EnerGo.UI
{
    // First-launch prologue: five pictures with the story on a paper card. Skipping still asks for the village name.
    public partial class LobbyView
    {
        private const float PrologZoom = 0.06f;
        private const float PrologZoomSeconds = 8f;
        private static readonly Color PrologFallback = new Color(0.12f, 0.16f, 0.21f);

        private VisualElement prologGambar, prologNama;
        private Label prologTeks, prologError;
        private TextField inputNama;
        private Button btnPrologLewati;
        private int prologPanel;
        private float prologPanelSince;
        private bool prologActive;
        private string prologRawName = "";

        partial void BindProlog()
        {
            prologGambar = root.Q("prolog-gambar");
            prologNama = root.Q("prolog-nama");
            prologTeks = root.Q<Label>("prolog-teks");
            prologError = root.Q<Label>("prolog-error");
            inputNama = root.Q<TextField>("input-nama");
            btnPrologLewati = root.Q<Button>("btn-prolog-lewati");
            root.Q<Button>("btn-prolog-lanjut").clicked += PrologNext;
            btnPrologLewati.clicked += PrologSkip;
        }

        partial void ShowProlog(LobbyController.ScreenState state)
        {
            bool on = state == LobbyController.ScreenState.Prolog;
            if (on && !prologActive) ShowPanel(0);
            prologActive = on;
        }

        private void ShowPanel(int index)
        {
            prologPanel = index;
            prologPanelSince = Time.unscaledTime;
            prologTeks.text = DesaText.Prolog[index].Replace("{nama}", DesaMath.CleanName(prologRawName));
            SetGambar(prologGambar, "Prolog/" + DesaText.PrologImages[index], PrologFallback);
            bool askName = index == DesaText.PrologNamePanel;
            prologNama.style.display = askName ? DisplayStyle.Flex : DisplayStyle.None;
            btnPrologLewati.style.display = index < DesaText.PrologNamePanel ? DisplayStyle.Flex : DisplayStyle.None;
            prologError.style.display = DisplayStyle.None;
        }

        private void PrologNext()
        {
            if (!controller.InputReady) return;
            if (prologPanel == DesaText.PrologNamePanel) prologRawName = inputNama.value;
            if (prologPanel >= DesaText.Prolog.Length - 1)
            {
                controller.FinishProlog(prologRawName);
                if (controller.State == LobbyController.ScreenState.Prolog)
                {
                    prologError.text = EnerGoProgress.Error;
                    prologError.style.display = DisplayStyle.Flex;
                }
                return;
            }
            controller.PrologTap();
            ShowPanel(prologPanel + 1);
        }

        private void PrologSkip()
        {
            if (!controller.InputReady) return;
            controller.PrologTap();
            ShowPanel(DesaText.PrologNamePanel);
        }

        partial void UpdateProlog()
        {
            if (!prologActive) return;
            float t = Mathf.Clamp01((Time.unscaledTime - prologPanelSince) / PrologZoomSeconds);
            prologGambar.style.scale = new Scale(Vector2.one * (1f + PrologZoom * t));
        }
    }
}
