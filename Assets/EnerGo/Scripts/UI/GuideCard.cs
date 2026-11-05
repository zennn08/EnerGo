using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace EnerGo.UI
{
    // The read-mode guide card (KartuPanduan.uxml): one SDA's steps, flipped with the main button or by tapping the
    // card's left or right third. Used by the lobby's guide sheet and the pre-game guide. Until the journal-sketch art
    // exists, the illustration is the SDA icon.
    public class GuideCard
    {
        private readonly VisualElement kartu, ilustrasi, titik;
        private readonly Label selotip, langkah, judul, isi;
        private readonly Button utama;
        private readonly string finalLabel;
        private GuidePager pager;
        private GuideBook.Card[] cards;
        private float pageTapBlockedUntil;

        public GuideCard(VisualElement root, string finalLabel, Action finished, Action skipped)
        {
            this.finalLabel = finalLabel;
            kartu = root.Q("kartu-panduan");
            selotip = root.Q<Label>("panduan-selotip");
            langkah = root.Q<Label>("panduan-langkah");
            ilustrasi = root.Q("panduan-ilustrasi");
            judul = root.Q<Label>("panduan-judul");
            isi = root.Q<Label>("panduan-isi");
            titik = root.Q("panduan-titik");
            utama = root.Q<Button>("btn-panduan-utama");

            utama.clicked += () =>
            {
                if (pager == null) return;
                if (pager.Next()) ShowPage(); else finished();
            };
            root.Q<Button>("btn-panduan-tutup").clicked += () => skipped();

            // Tapping the left/right third of the card flips pages (buttons excluded).
            kartu.RegisterCallback<PointerDownEvent>(e =>
            {
                if (pager == null || Time.unscaledTime < pageTapBlockedUntil) return;
                if (e.target is VisualElement t && t.GetFirstOfType<Button>() != null) return;
                float w = kartu.layout.width;
                if (e.localPosition.x < w / 3f && pager.Page > 0) { pager.Prev(); ShowPage(); }
                else if (e.localPosition.x > w * 2f / 3f && !pager.IsLast) { pager.Next(); ShowPage(); }
                else return;
                pageTapBlockedUntil = Time.unscaledTime + 0.35f;
            });
        }

        public void Open(string id)
        {
            cards = GuideBook.For(id);
            pager = new GuidePager(cards.Length);
            selotip.text = ResourceIds.Name(id).ToUpperInvariant();
            foreach (var other in GuideBook.Ids) ilustrasi.RemoveFromClassList("eg-ikon--" + other);
            ilustrasi.AddToClassList("eg-ikon--" + id);
            ShowPage();
        }

        public void Close() => pager = null;

        private void ShowPage()
        {
            var c = cards[pager.Page];
            langkah.text = pager.StepLabel;
            judul.text = c.Title;
            isi.text = c.Body;
            utama.text = pager.IsLast ? finalLabel : pager.PrimaryLabel;
            titik.Clear();
            for (int i = 0; i < pager.Count; i++)
            {
                var dot = new VisualElement();
                dot.AddToClassList(i == pager.Page ? "panduan__titik-kini" : "panduan__titik-lain");
                titik.Add(dot);
            }
        }
    }
}
