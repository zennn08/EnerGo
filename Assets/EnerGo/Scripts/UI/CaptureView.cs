using UnityEngine;
using UnityEngine.UIElements;

namespace EnerGo.UI
{
    // Draws the capture screens from ARCapturePrototype's state: the HUD frame and pre-game guide here, the quiz and
    // mini-game overlays in the partial files. Input for the games themselves stays in the controller, so everything
    // that is not a button ignores the pointer.
    [RequireComponent(typeof(UIDocument))]
    public partial class CaptureView : MonoBehaviour
    {
        private const float RippleSec = 0.35f;

        [SerializeField] private ARCapturePrototype controller;

        private VisualElement root, lapisHud, lapisPanduan, kotak, barRow, barIsi, riak, ingatkan, centang, sdaIkon;
        private Label tag, teks, sub, angka, pilMode, sdaTeks;
        private Button kembali, mode, ulang;
        private PitaKompas pita;
        private GuideCard guideCard;
        private bool hasARSession, dontRemind, guideShown;
        private string shownTag, shownIcon;

        private void OnEnable()
        {
            root = GetComponent<UIDocument>().rootVisualElement;
            lapisHud = root.Q("lapis-hud");
            lapisPanduan = root.Q("lapis-panduan");
            kotak = root.Q("kotak-status");
            barRow = root.Q("status-bar").parent;
            barIsi = root.Q("status-bar").Q(className: "eg-bar__isi");
            riak = root.Q("riak");
            ingatkan = root.Q("panduan-ingatkan");
            centang = root.Q("panduan-centang");
            sdaIkon = root.Q("pil-sda-ikon");
            tag = root.Q<Label>("status-tag");
            teks = root.Q<Label>("status-teks");
            sub = root.Q<Label>("status-sub");
            angka = root.Q<Label>("status-angka");
            pilMode = root.Q<Label>("pil-mode");
            sdaTeks = root.Q<Label>("pil-sda-teks");
            kembali = root.Q<Button>("btn-kembali");
            mode = root.Q<Button>("btn-mode");
            ulang = root.Q<Button>("btn-ulang");
            pita = root.Q<PitaKompas>("pita-kompas");

            kembali.clicked += () => controller.BackFromCapture();
            mode.clicked += () => controller.ToggleTrackingMode();
            ulang.clicked += () => controller.RespawnTarget();

            guideCard = new GuideCard(root, "MULAI", FinishGuide, FinishGuide);
            root.Q<Button>("btn-panduan-tutup").text = "LEWATI";
            Show(ingatkan, true);
            ingatkan.RegisterCallback<ClickEvent>(_ =>
            {
                dontRemind = !dontRemind;
                centang.EnableInClassList("is-nyala", dontRemind);
            });

            hasARSession = controller.HasARSession;
            Show(mode, hasARSession);
            Show(pilMode.parent, !hasARSession);
            BindQuiz();
            BindOverlay();
        }

        private void FinishGuide()
        {
            guideCard.Close();
            controller.FinishGuide(dontRemind);
        }

        private void Update()
        {
            var screen = controller.CurrentScreen;
            bool frame = CaptureScreens.ShowsHudFrame(screen);
            bool guide = controller.GuideOpen;
            Show(lapisHud, frame);
            ShowGuide(guide);
            UpdateRipple();
            UpdateQuiz(screen);
            UpdateOverlay(screen, guide);
            if (!frame) return;

            kembali.text = controller.BackLabel;
            mode.text = controller.ModeLabel;
            pilMode.text = "TANPA ARCORE";
            sdaTeks.text = controller.ResourceUnitsLabel;
            if (shownIcon != controller.ResourceId)
            {
                if (shownIcon != null) sdaIkon.RemoveFromClassList("eg-ikon--" + shownIcon);
                shownIcon = controller.ResourceId;
                sdaIkon.AddToClassList("eg-ikon--" + shownIcon);
            }

            Show(kotak, !guide);
            Show(pita, !guide);
            Show(ulang, !guide);
            if (guide) return;
            ShowStatus(controller.ReadHud());
            pita.Show(controller.ReadCompassStrip());
        }

        private void ShowStatus(HudStatus s)
        {
            teks.text = s.Line;
            if (shownTag != s.Tag)
            {
                if (shownTag != null) tag.RemoveFromClassList("status__tag--" + shownTag);
                shownTag = s.Tag;
                tag.text = s.Tag;
                tag.AddToClassList("status__tag--" + s.Tag);
                kotak.EnableInClassList("status--gagal", s.Tag == HudStatus.Gagal);
            }
            Show(sub, s.Sub != null);
            if (s.Sub != null) sub.text = s.Sub;
            Show(barRow, s.HasBar);
            if (!s.HasBar) return;
            barIsi.style.width = Length.Percent(100f * Mathf.Clamp01(s.Bar));
            barIsi.EnableInClassList("eg-isi--oker", s.Warn);
            barIsi.EnableInClassList("eg-isi--hutan", !s.Warn);
            angka.text = s.Label;
        }

        private void ShowGuide(bool open)
        {
            Show(lapisPanduan, open);
            lapisPanduan.pickingMode = open ? PickingMode.Position : PickingMode.Ignore;
            if (open == guideShown) return;
            guideShown = open;
            if (open) guideCard.Open(controller.ResourceId);
        }

        // Expanding, fading ring where the finger landed.
        private void UpdateRipple()
        {
            float t = (Time.time - controller.LastHitTime) / RippleSec;
            bool on = t >= 0f && t < 1f;
            Show(riak, on);
            if (!on) return;
            Vector2 p = ToPanel(controller.LastHitScreenPos);
            float r = Mathf.Lerp(12f, 34f, t);
            riak.style.left = p.x - r;
            riak.style.top = p.y - r;
            riak.style.width = r * 2f;
            riak.style.height = r * 2f;
            riak.style.borderTopLeftRadius = r;
            riak.style.borderTopRightRadius = r;
            riak.style.borderBottomLeftRadius = r;
            riak.style.borderBottomRightRadius = r;
            riak.style.opacity = 1f - t;
        }

        // Screen pixels (y up) to panel units (y down).
        private Vector2 ToPanel(Vector2 screen)
        {
            float k = root.layout.width / Screen.width;
            return new Vector2(screen.x, Screen.height - screen.y) * k;
        }

        private static void Show(VisualElement e, bool on)
        {
            var d = on ? DisplayStyle.Flex : DisplayStyle.None;
            if (e.style.display != d) e.style.display = d;
        }

        partial void BindQuiz();
        partial void BindOverlay();
        partial void UpdateQuiz(CaptureScreen screen);
        partial void UpdateOverlay(CaptureScreen screen, bool guideOpen);
    }
}
