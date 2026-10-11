using UnityEngine;
using static EnerGo.EnerGoUI;

namespace EnerGo
{
    // Guide cards for one SDA (IMGUI, call Draw from OnGUI under the HUD-scaled matrix). withStart = the pre-game
    // overlay (MULAI + "Jangan ingatkan lagi"); otherwise the lobby viewer (TUTUP only).
    public class GuideViewer
    {
        public enum Result { None, Start, Close }

        public static string SkipKey(string id) => "EnerGo.Guide.Skip." + id;
        public static bool ShouldShow(string id) => GuideBook.For(id).Length > 0 && PlayerPrefs.GetInt(SkipKey(id), 0) == 0;

        public static void ResetAll()
        {
            foreach (var id in GuideBook.Ids) PlayerPrefs.DeleteKey(SkipKey(id));
            PlayerPrefs.Save();
        }

        private readonly string id;
        private readonly bool withStart;
        private readonly GuideBook.Card[] cards;
        private int page;
        private bool dontRemind;
        private GUIStyle sTitle, sBody, sSmall, sButton, sButtonDark;

        public GuideViewer(string resourceId, bool withStart)
        {
            id = resourceId;
            this.withStart = withStart;
            cards = GuideBook.For(resourceId);
        }

        public Result Draw(float sw, float sh, float hudScale)
        {
            if (cards.Length == 0) return withStart ? Result.Start : Result.Close;
            if (sTitle == null)
            {
                sTitle = Text(20, true, Ink, TextAnchor.UpperLeft, true);
                sBody = Text(15, false, Muted, TextAnchor.UpperLeft, true);
                sSmall = Text(13, true, Ink, TextAnchor.MiddleLeft);
                sButton = Text(16, true, Color.white);
                sButtonDark = Text(15, true, Ink);
            }

            GUI.color = Alpha(Ink, 0.55f);
            GUI.DrawTexture(new Rect(0, 0, sw, sh), Texture2D.whiteTexture);
            GUI.color = Color.white;

            float w = Mathf.Min(sw - 40f, 400f), h = 330f, x = (sw - w) * 0.5f, y = Mathf.Max(70f, sh * 0.5f - h * 0.62f);
            var card = new Rect(x, y, w, h);
            DrawCard(card);
            var c = cards[page];
            GUI.Label(new Rect(x + 20f, y + 14f, w - 40f, 20f), ResourceIds.Name(id).ToUpperInvariant() + " · " + (page + 1) + "/" + cards.Length, sSmall);
            DrawGuideArt(new Rect(x + 20f, y + 42f, w - 40f, 130f), c.Art);
            GUI.Label(new Rect(x + 20f, y + 186f, w - 40f, 52f), c.Title, sTitle);
            GUI.Label(new Rect(x + 20f, y + 240f, w - 40f, 66f), c.Body, sBody);
            for (int i = 0; i < cards.Length; i++) // page dots
                DrawPanel(new Rect(x + w * 0.5f + (i - (cards.Length - 1) * 0.5f) * 16f - 4f, y + h - 18f, 8f, 8f), i == page ? Orange : Line, Color.clear, 4f);

            // Tapping the left/right third of the card flips pages.
            if (page > 0 && TapHit(new Rect(x, y, w / 3f, h), hudScale)) page--;
            else if (page < cards.Length - 1 && TapHit(new Rect(x + w * 2f / 3f, y, w / 3f, h), hudScale)) page++;

            float by = card.yMax + 14f;
            bool last = page == cards.Length - 1;
            if (withStart)
            {
                var row = new Rect(x, by, w, 30f);
                DrawPanel(new Rect(x + 4f, by + 4f, 22f, 22f), Paper, Ink, 5f, 2f);
                if (dontRemind) DrawPanel(new Rect(x + 9f, by + 9f, 12f, 12f), Orange, Color.clear, 3f);
                GUI.color = Color.white;
                GUI.Label(new Rect(x + 34f, by, w - 34f, 30f), "Jangan ingatkan lagi", Text(14, true, Paper, TextAnchor.MiddleLeft));
                if (TapHit(row, hudScale)) dontRemind = !dontRemind;
                by += 40f;
            }

            var main = new Rect(x, by, w, 56f);
            string label = !withStart ? (last ? "TUTUP" : "LANJUT") : last ? "MULAI" : "LANJUT";
            GUI.Label(DrawPrimaryButton(main, IsHeld(main, hudScale)), label, sButton);
            if (TapHit(main, hudScale))
            {
                if (!last) page++;
                else return Finish();
            }
            var skip = new Rect(x + w * 0.5f - 70f, main.yMax + 8f, 140f, 40f);
            GUI.Label(DrawSecondaryButton(skip, IsHeld(skip, hudScale)), withStart ? "LEWATI" : "TUTUP", sButtonDark);
            if (TapHit(skip, hudScale)) return Finish();
            return Result.None;
        }

        private Result Finish()
        {
            if (!withStart) return Result.Close;
            if (dontRemind) { PlayerPrefs.SetInt(SkipKey(id), 1); PlayerPrefs.Save(); }
            return Result.Start;
        }

        // Simple shapes in the brand palette, one per guide step (placeholder art until the Blender pass).
        public static void DrawGuideArt(Rect r, GuideBook.Art art)
        {
            DrawPanel(r, Cream, Line, 14f, 1.5f);
            Vector2 c = r.center;
            Rect Box(float cx, float cy, float w, float h) => new Rect(cx - w * 0.5f, cy - h * 0.5f, w, h);
            Rect Circle(float cx, float cy, float rad) => Box(cx, cy, rad * 2f, rad * 2f);
            switch (art)
            {
                case GuideBook.Art.Reticle:
                    DrawPanel(Circle(c.x + 18f, c.y - 8f, 16f), EnerGoUI.Sun, Color.clear, 16f);
                    DrawPanel(Circle(c.x, c.y, 34f), Color.clear, Orange, 34f, 3f);
                    break;
                case GuideBook.Art.Compass:
                    DrawPanel(Box(c.x, c.y, r.width - 60f, 34f), Paper, Line, 10f, 1.5f);
                    for (int i = -3; i <= 3; i++) DrawPanel(Box(c.x + i * 34f, c.y, 2f, 12f), Line, Color.clear, 1f);
                    DrawPanel(Box(c.x, c.y, 2f, 28f), Ink, Color.clear, 1f);
                    DrawPanel(Circle(c.x + 54f, c.y, 7f), Orange, Ink, 7f, 1.5f);
                    break;
                case GuideBook.Art.Tap:
                    DrawPanel(Circle(c.x, c.y, 30f), Ink, Color.clear, 30f);
                    DrawPanel(Circle(c.x + 26f, c.y + 22f, 14f), Color.clear, Orange, 14f, 3f);
                    DrawPanel(Circle(c.x + 26f, c.y + 22f, 6f), Orange, Color.clear, 6f);
                    break;
                case GuideBook.Art.PhoneFlat:
                    DrawPanel(Box(c.x, c.y + 10f, 120f, 26f), Ink, Color.clear, 8f);
                    DrawPanel(Circle(c.x, c.y - 28f, 10f), Color.clear, Success, 10f, 2f);
                    DrawPanel(Circle(c.x, c.y - 28f, 4f), Success, Color.clear, 4f);
                    break;
                case GuideBook.Art.PhoneSwirl:
                    DrawPanel(Circle(c.x, c.y, 46f), Color.clear, Alpha(Orange, 0.8f), 46f, 3f);
                    DrawPanel(Box(c.x, c.y, 30f, 54f), Ink, Color.clear, 6f);
                    DrawPanel(Circle(c.x + 46f, c.y, 6f), Orange, Color.clear, 6f);
                    break;
                case GuideBook.Art.Hold:
                    DrawPanel(Box(c.x, c.y - 30f, 6f, 70f), Ink, Color.clear, 3f);
                    DrawPanel(Box(c.x, c.y + 8f, 16f, 16f), Orange, Ink, 4f, 1.5f);
                    DrawPanel(Circle(c.x + 34f, c.y - 34f, 12f), Alpha(Orange, 0.4f), Orange, 12f, 2f);
                    break;
                case GuideBook.Art.Layers:
                {
                    Color[] bands = { new Color(0.48f, 0.31f, 0.18f), new Color(0.27f, 0.27f, 0.31f), new Color(0.56f, 0.48f, 0.71f), new Color(0.17f, 0.11f, 0.08f), new Color(0.18f, 0.43f, 0.58f) };
                    float bh = (r.height - 20f) / bands.Length;
                    for (int i = 0; i < bands.Length; i++) DrawPanel(new Rect(r.x + 30f, r.y + 10f + i * bh, r.width - 60f, bh), bands[i], Color.clear, 0f);
                    DrawPanel(Box(c.x + 30f, r.y + 10f + bh * 1.8f, 6f, bh * 3.6f), Paper, Ink, 2f, 1f);
                    break;
                }
                case GuideBook.Art.PumpRing:
                    DrawPanel(Circle(c.x, c.y, 26f), Alpha(Orange, 0.2f), Orange, 26f, 3f);
                    DrawPanel(Circle(c.x, c.y, 48f), Color.clear, Ink, 48f, 3f);
                    break;
                case GuideBook.Art.RadMeter:
                    GUI.Label(Box(c.x, c.y - 22f, 160f, 30f), "RAD 87", Text(24, true, Ink));
                    DrawBar(Box(c.x, c.y + 16f, r.width - 80f, 12f), 0.87f, new Color(0.22f, 1f, 0.08f));
                    break;
                case GuideBook.Art.Slider:
                    DrawPanel(Box(c.x - 30f, c.y, 22f, r.height - 30f), Paper, Line, 6f, 1.5f);
                    DrawPanel(Box(c.x - 30f, c.y, 14f, (r.height - 30f) * 0.3f), Alpha(Success, 0.5f), Color.clear, 4f);
                    DrawPanel(Box(c.x + 30f, c.y, 40f, r.height - 30f), Paper, Line, 10f, 1.5f);
                    DrawPanel(Box(c.x + 30f, c.y + 8f, 48f, 22f), Orange, Ink, 8f, 2f);
                    break;
                case GuideBook.Art.Shake:
                    DrawPanel(Box(c.x, c.y, 34f, 60f), Ink, Color.clear, 7f);
                    DrawPanel(Box(c.x - 34f, c.y, 4f, 30f), Danger, Color.clear, 2f);
                    DrawPanel(Box(c.x + 34f, c.y, 4f, 30f), Danger, Color.clear, 2f);
                    break;
            }
        }
    }
}
