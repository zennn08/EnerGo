using UnityEngine;
using UnityEngine.UIElements;

namespace EnerGo.UI
{
    // Sheet Panduan and the read-mode guide card over it. Content comes from GuideBook, like the IMGUI card
    // the capture screen still uses. Until the journal-sketch art exists, the illustration is the SDA icon.
    public partial class LobbyView
    {
        private VisualElement kartuPanduan, panduanIlustrasi, panduanTitik;
        private Label panduanSelotip, panduanLangkah, panduanJudul, panduanIsi, panduanNotice;
        private Button btnPanduanUtama;
        private GuidePager pager;
        private GuideBook.Card[] cards;
        private string shownGuideId;
        private float pageTapBlockedUntil;

        partial void BindGuide()
        {
            sheetPanduan = root.Q("sheet-panduan");
            panduanNotice = root.Q<Label>("panduan-notice");
            foreach (var id in GuideBook.Ids)
            {
                string pick = id;
                FillTile(root.Q("panduan-" + id), id, -1, false, ringkas: false);
                root.Q("panduan-" + id).RegisterCallback<ClickEvent>(_ => controller.OpenGuide(pick));
            }
            root.Q<Button>("btn-reset-panduan").clicked += () => controller.ResetGuides();

            kartuPanduan = root.Q("kartu-panduan");
            panduanSelotip = root.Q<Label>("panduan-selotip");
            panduanLangkah = root.Q<Label>("panduan-langkah");
            panduanIlustrasi = root.Q("panduan-ilustrasi");
            panduanJudul = root.Q<Label>("panduan-judul");
            panduanIsi = root.Q<Label>("panduan-isi");
            panduanTitik = root.Q("panduan-titik");
            btnPanduanUtama = root.Q<Button>("btn-panduan-utama");

            btnPanduanUtama.clicked += () =>
            {
                if (pager == null) return;
                if (pager.Next()) ShowPage(); else controller.CloseGuide();
            };
            root.Q<Button>("btn-panduan-tutup").clicked += () => controller.CloseGuide();

            // Tapping the left/right third of the card flips pages (buttons excluded).
            kartuPanduan.RegisterCallback<PointerDownEvent>(e =>
            {
                if (pager == null || Time.unscaledTime < pageTapBlockedUntil) return;
                if (e.target is VisualElement t && t.GetFirstOfType<Button>() != null) return;
                float w = kartuPanduan.layout.width;
                if (e.localPosition.x < w / 3f && pager.Page > 0) { pager.Prev(); ShowPage(); }
                else if (e.localPosition.x > w * 2f / 3f && !pager.IsLast) { pager.Next(); ShowPage(); }
                else return;
                pageTapBlockedUntil = Time.unscaledTime + 0.35f;
            });
        }

        // Opens or closes the card when the controller's guide changes.
        partial void UpdateGuide()
        {
            if (panduanNotice != null)
                panduanNotice.style.visibility = controller.GuidesResetNoticeVisible ? Visibility.Visible : Visibility.Hidden;
            if (controller.OpenGuideId == shownGuideId) return;
            shownGuideId = controller.OpenGuideId;
            if (shownGuideId == null) { pager = null; return; }
            cards = GuideBook.For(shownGuideId);
            pager = new GuidePager(cards.Length);
            panduanSelotip.text = ResourceIds.Name(shownGuideId).ToUpperInvariant();
            foreach (var other in GuideBook.Ids) panduanIlustrasi.RemoveFromClassList("eg-ikon--" + other);
            panduanIlustrasi.AddToClassList("eg-ikon--" + shownGuideId);
            ShowPage();
        }

        private void ShowPage()
        {
            var c = cards[pager.Page];
            panduanLangkah.text = pager.StepLabel;
            panduanJudul.text = c.Title;
            panduanIsi.text = c.Body;
            btnPanduanUtama.text = pager.PrimaryLabel;
            panduanTitik.Clear();
            for (int i = 0; i < pager.Count; i++)
            {
                var dot = new VisualElement();
                dot.AddToClassList(i == pager.Page ? "panduan__titik-kini" : "panduan__titik-lain");
                panduanTitik.Add(dot);
            }
        }
    }
}
