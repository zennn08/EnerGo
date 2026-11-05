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
        private GUIStyle sTitle, sBody, sSmall, sRemind;

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
                sRemind = Text(15, true, Color.white, TextAnchor.MiddleLeft);
            }

            GUI.color = Alpha(Ink, 0.55f);
            GUI.DrawTexture(new Rect(0, 0, sw, sh), Texture2D.whiteTexture);
            GUI.color = Color.white;

            float w = Mathf.Min(sw - 40f, 400f), h = 358f, x = (sw - w) * 0.5f, y = Mathf.Max(100f, sh * 0.5f - h * 0.62f);
            var card = new Rect(x, y, w, h);
            DrawCard(card);
            DrawRibbon(card, ResourceIds.Name(id).ToUpperInvariant(), ButtonKind.Orange);
            var c = cards[page];
            y += 28f;
            GUI.Label(new Rect(x + 20f, y + 14f, w - 40f, 20f), "LANGKAH " + (page + 1) + "/" + cards.Length, sSmall);
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
                var box = DrawButton(new Rect(x + 2f, by, 30f, 30f), dontRemind ? ButtonKind.Ok : ButtonKind.Off);
                if (dontRemind) DrawIcon(new Rect(box.x + 5f, box.y + 4f, box.width - 10f, box.width - 10f), "check");
                DrawStrokedLabel(new Rect(x + 42f, by, w - 42f, 30f), "Jangan ingatkan lagi", sRemind, 1.5f);
                if (TapHit(row, hudScale)) dontRemind = !dontRemind;
                by += 40f;
            }

            var main = new Rect(x, by, w, 62f);
            string label = !withStart ? (last ? "TUTUP" : "LANJUT") : last ? "MULAI" : "LANJUT";
            ButtonLabel(DrawPrimaryButton(main, IsHeld(main, hudScale)), label, 24, ButtonKind.Orange);
            if (TapHit(main, hudScale))
            {
                if (!last) page++;
                else return Finish();
            }
            var skip = new Rect(x + w * 0.5f - 80f, main.yMax + 10f, 160f, 48f);
            ButtonLabel(DrawSecondaryButton(skip, IsHeld(skip, hudScale)), withStart ? "LEWATI" : "TUTUP", 18, ButtonKind.Paper);
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
            DrawPanel(r, Color.white, Line, 14f, 2f);
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
                case GuideBook.Art.Shake:
                    DrawPanel(Box(c.x, c.y, 34f, 60f), Ink, Color.clear, 7f);
                    DrawPanel(Box(c.x - 34f, c.y, 4f, 30f), Danger, Color.clear, 2f);
                    DrawPanel(Box(c.x + 34f, c.y, 4f, 30f), Danger, Color.clear, 2f);
                    break;
            }
        }
    }
}
