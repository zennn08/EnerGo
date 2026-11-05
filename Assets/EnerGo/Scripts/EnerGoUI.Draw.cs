using System.Collections.Generic;
using UnityEngine;

namespace EnerGo
{
    public enum ButtonKind { Orange, Ok, Bad, Info, Sun, Paper, Off, Dark }

    // Arcade Cerah shapes: puffy gradient buttons and tiles with an Ink outline and lip, paper cards, pills, bars,
    // icons and the cartoon world backdrop. All sizes are mockup px x UiLayout.Mock (see M()).
    public static partial class EnerGoUI
    {
        public const float Radius = 16f;

        // Flat rounded panel. Tinted by GUI.color.
        public static void DrawPanel(Rect rect, Color fill, Color outline, float radius = Radius, float outlineWidth = 1.5f)
        {
            if (Event.current.type != EventType.Repaint) return;
            Color tint = GUI.color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, fill * tint, 0f, radius);
            if (outline.a > 0f)
                GUI.DrawTexture(rect, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, outline * tint, outlineWidth, radius);
        }

        // ── Gradients ─────────────────────────────────────────────────
        private static readonly Dictionary<(Color, Color), Texture2D> gradients = new Dictionary<(Color, Color), Texture2D>();

        // Cached 1x64 vertical gradient, `top` at the top of whatever rect it is drawn into.
        public static Texture2D Gradient(Color top, Color bottom)
        {
            if (gradients.TryGetValue((top, bottom), out var tex) && tex != null) return tex;
            tex = new Texture2D(1, 64, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, hideFlags = HideFlags.DontSave };
            for (int y = 0; y < 64; y++) tex.SetPixel(0, y, Color.Lerp(bottom, top, y / 63f)); // texture row 63 is drawn at the top
            tex.Apply();
            gradients[(top, bottom)] = tex;
            return tex;
        }

        private static void DrawGradient(Rect rect, Color top, Color bottom, float radius)
        {
            if (Event.current.type != EventType.Repaint) return;
            GUI.DrawTexture(rect, Gradient(top, bottom), ScaleMode.StretchToFill, true, 0f, GUI.color, 0f, radius);
        }

        private static void KindColors(ButtonKind kind, out Color top, out Color bottom)
        {
            switch (kind)
            {
                case ButtonKind.Ok: top = OkTop; bottom = OkBot; break;
                case ButtonKind.Bad: top = BadTop; bottom = BadBot; break;
                case ButtonKind.Info: top = InfoTop; bottom = InfoBot; break;
                case ButtonKind.Sun: top = SunTop; bottom = SunBot; break;
                case ButtonKind.Paper: top = PaperTop; bottom = PaperBot; break;
                case ButtonKind.Off: top = OffTop; bottom = OffBot; break;
                case ButtonKind.Dark: top = DarkTop; bottom = DarkBot; break;
                default: top = OrangeTop; bottom = OrangeBot; break;
            }
        }

        // ── Puffy shapes ──────────────────────────────────────────────
        // Gradient face, Ink outline, Ink lip below, dark band at the inner bottom, white shine at the inner top.
        // Outline and lip shrink on small rects so a 40-unit button does not turn into all border.
        private static Rect DrawPuffy(Rect rect, Color top, Color bottom, float radius, float lip, bool pressed)
        {
            float outline = Mathf.Min(M(5f), rect.height * 0.075f);
            lip = Mathf.Min(lip, rect.height * 0.1f);
            var face = new Rect(rect.x, rect.y + (pressed ? lip : 0f), rect.width, rect.height - lip);
            float r = Mathf.Min(radius, face.height * 0.5f, face.width * 0.5f);
            if (Event.current.type != EventType.Repaint) return face;
            if (!pressed) DrawPanel(new Rect(rect.x, rect.y + lip, rect.width, rect.height - lip), Ink, Color.clear, r);
            DrawGradient(face, top, bottom, r);
            float band = Mathf.Min(M(8f), face.height * 0.16f), shine = Mathf.Min(M(6f), face.height * 0.12f);
            var inner = new Rect(face.x + outline, face.y + outline, face.width - outline * 2f, face.height - outline * 2f);
            float ir = Mathf.Max(0f, r - outline);
            DrawPanel(new Rect(inner.x, inner.yMax - band, inner.width, band), new Color(0f, 0f, 0f, 0.18f), Color.clear, Mathf.Min(ir, band * 0.5f));
            DrawPanel(new Rect(inner.x + ir * 0.4f, inner.y + 1f, inner.width - ir * 0.8f, shine), new Color(1f, 1f, 1f, 0.45f), Color.clear, shine * 0.5f);
            DrawPanel(face, Color.clear, Ink, r, outline);
            return face;
        }

        // Returns the face rect (shifted down while pressed) so callers can place the label on it.
        public static Rect DrawButton(Rect rect, ButtonKind kind, bool pressed = false)
        {
            KindColors(kind, out var top, out var bottom);
            return DrawPuffy(rect, top, bottom, M(20f), M(7f), pressed);
        }

        public static Rect DrawPrimaryButton(Rect rect, bool pressed = false) => DrawButton(rect, ButtonKind.Orange, pressed);

        public static Rect DrawSecondaryButton(Rect rect, bool pressed = false) => DrawButton(rect, ButtonKind.Paper, pressed);

        public static Rect DrawDarkButton(Rect rect, bool pressed = false) => DrawButton(rect, ButtonKind.Dark, pressed);

        // Stroked Lilita label for a button face; Paper and Off buttons get plain Ink text.
        public static void ButtonLabel(Rect face, string text, int size, ButtonKind kind)
        {
            bool light = kind == ButtonKind.Paper || kind == ButtonKind.Off;
            var style = Title(size, light ? Ink : Color.white);
            int fit = FitSize(style, text, face.width - M(12f), Mathf.Max(10, size / 2));
            if (fit != size) style = Title(fit, light ? Ink : Color.white);
            var r = new Rect(face.x, face.y - M(1f), face.width, face.height - M(2f));
            if (light) GUI.Label(r, text, style);
            else DrawStrokedLabel(r, text, style, M(2f), M(1.5f));
        }

        // Resource tile: gradient, icon, stroked name, and a Berry count badge (count < 0 = no badge).
        public static void DrawTile(Rect r, string resourceId, int count, bool pressed = false)
        {
            string label = IsResource(resourceId) ? ResourceIds.Name(resourceId) : "Acak";
            var face = DrawPuffy(r, ResourceTop(resourceId), ResourceBot(resourceId), M(18f), M(6f), pressed);
            var icon = Icon(ResourceIcon(resourceId));
            float s = Mathf.Min(face.width * 0.55f, face.height * 0.55f);
            if (icon != null && Event.current.type == EventType.Repaint)
                GUI.DrawTexture(new Rect(face.center.x - s * 0.5f, face.y + face.height * 0.1f, s, s), icon, ScaleMode.ScaleToFit);
            var nameStyle = Title(Mathf.RoundToInt(M(11f)), Color.white);
            int fit = FitSize(nameStyle, label, face.width - M(8f), Mathf.RoundToInt(M(8f)));
            if (fit != nameStyle.fontSize) nameStyle = Title(fit, Color.white);
            DrawStrokedLabel(new Rect(face.x, face.yMax - face.height * 0.34f, face.width, face.height * 0.26f), label, nameStyle, M(1.5f));
            if (count >= 0) DrawCountBadge(new Vector2(face.xMax - M(2f), face.y + M(2f)), count.ToString());
        }

        private static bool IsResource(string id) =>
            id == ResourceIds.Sun || id == ResourceIds.Coal || id == ResourceIds.Gold || id == ResourceIds.Oil || id == ResourceIds.Uranium;

        // Berry pill centred on `topRight`, like the mockup's red counters.
        public static void DrawCountBadge(Vector2 topRight, string text)
        {
            var style = Title(Mathf.RoundToInt(M(10f)), Color.white);
            float w = Mathf.Max(M(22f), style.CalcSize(new GUIContent(text)).x + M(12f)), h = M(20f);
            var r = new Rect(topRight.x - w * 0.6f, topRight.y - h * 0.55f, w, h);
            DrawPanel(r, Berry, Color.clear, h * 0.5f);
            DrawPanel(r, Color.clear, Ink, h * 0.5f, M(3f));
            GUI.Label(r, text, style);
        }

        // Paper card: Paper gradient, Ink outline and an Ink lip. `outline` tints the border (e.g. green on a right answer).
        public static void DrawCard(Rect rect, float radius = -1f, Color? outline = null, float outlineWidth = -1f)
        {
            if (Event.current.type != EventType.Repaint) return;
            float r = Mathf.Min(radius < 0f ? M(24f) : radius, rect.height * 0.5f, rect.width * 0.5f);
            float w = outlineWidth < 0f ? M(5f) : Mathf.Max(outlineWidth, M(3f));
            DrawPanel(new Rect(rect.x, rect.y + M(8f), rect.width, rect.height), Ink, Color.clear, r);
            DrawGradient(rect, PaperTop, PaperBot, r);
            DrawPanel(rect, Color.clear, outline ?? Ink, r, w);
        }

        // Title ribbon: a small button-shaped label centred on the card's top edge, half outside it.
        public static void DrawRibbon(Rect cardRect, string text, ButtonKind kind)
        {
            var style = Title(Mathf.RoundToInt(M(22f)), Color.white);
            float w = Mathf.Min(cardRect.width - M(24f), style.CalcSize(new GUIContent(text)).x + M(48f)), h = M(46f);
            var face = DrawButton(new Rect(cardRect.center.x - w * 0.5f, cardRect.y - h * 0.55f, w, h), kind);
            ButtonLabel(face, text, style.fontSize, kind);
        }

        // Dark pill for text over any backdrop. Returns the inner rect for the caller's content.
        public static Rect DrawPill(Rect r)
        {
            float rad = r.height * 0.5f, w = Mathf.Min(M(3f), r.height * 0.1f);
            DrawPanel(r, PillBg, Color.clear, rad);
            DrawPanel(r, Color.clear, Ink, rad, w);
            return new Rect(r.x + rad * 0.6f, r.y + w, r.width - rad * 1.2f, r.height - w * 2f);
        }

        // Ink track with an Ink outline and a rounded fill.
        public static void DrawBar(Rect track, float progress, Color fill) => DrawBar(track, progress, Color.Lerp(fill, Color.white, 0.35f), fill);

        public static void DrawBar(Rect track, float progress, Color top, Color bottom)
        {
            float rad = track.height * 0.5f, w = Mathf.Min(M(4f), track.height * 0.25f);
            DrawPanel(track, Ink, Color.clear, rad);
            float p = Mathf.Clamp01(progress);
            if (p > 0.01f)
            {
                var inner = new Rect(track.x + w, track.y + w, track.width - w * 2f, track.height - w * 2f);
                var fill = new Rect(inner.x, inner.y, Mathf.Max(inner.height, inner.width * p), inner.height);
                DrawGradient(fill, top, bottom, inner.height * 0.5f);
            }
            DrawPanel(track, Color.clear, Ink, rad, w);
        }

        // Small padlock badge (locked resources): white backing, ink shackle and body. `size` = body width.
        public static void DrawLock(Vector2 c, float size)
        {
            float bodyH = size * 0.75f, shackle = size * 0.62f;
            DrawPanel(new Rect(c.x - size * 0.5f - 3f, c.y - shackle - 2f, size + 6f, shackle + bodyH * 0.5f + 5f), Color.white, Color.clear, size * 0.35f);
            DrawPanel(new Rect(c.x - size * 0.3f, c.y - shackle, size * 0.6f, shackle + 2f), Color.clear, Ink, size * 0.3f, Mathf.Max(1.5f, size * 0.14f));
            DrawPanel(new Rect(c.x - size * 0.5f, c.y - bodyH * 0.5f + 1f, size, bodyH), Ink, Color.clear, size * 0.18f);
        }

        // ── Icons ─────────────────────────────────────────────────────
        private static readonly Dictionary<string, Texture2D> icons = new Dictionary<string, Texture2D>();

        // Resources/UI/Icons/<name> (see Branding/icons). Null if missing.
        public static Texture2D Icon(string name)
        {
            if (!icons.TryGetValue(name, out var tex) || tex == null) icons[name] = tex = Resources.Load<Texture2D>("UI/Icons/" + name);
            return tex;
        }

        // Resource ids are their icon names; anything else (random practice) is the dice.
        public static string ResourceIcon(string id) => IsResource(id) ? id : "dice";

        public static void DrawIcon(Rect r, string name)
        {
            var tex = Icon(name);
            if (tex != null && Event.current.type == EventType.Repaint) GUI.DrawTexture(r, tex, ScaleMode.ScaleToFit);
        }

        // ── World backdrop ────────────────────────────────────────────
        private static Texture2D isoGrid;

        // Teal sky, a grass hill, a faint isometric grid and a few trees; `dim` lays Ink over it (popups, quiz).
        public static void DrawWorld(float sw, float sh, float dim = 0f)
        {
            if (Event.current.type != EventType.Repaint) return;
            GUI.DrawTexture(new Rect(0, 0, sw, sh), Gradient(SkyTop, SkyLow), ScaleMode.StretchToFill);
            float hillTop = sh * 0.40f;
            DrawGradient(new Rect(-sw * 0.35f, hillTop, sw * 1.7f, sh * 1.2f), Grass, GrassLow, sw * 0.8f);

            if (isoGrid == null) isoGrid = MakeIsoGrid();
            float tw = M(40f), th = M(23f);
            GUI.DrawTextureWithTexCoords(new Rect(0, 0, sw, sh), isoGrid, new Rect(0, 0, sw / tw, sh / th));

            float t = sw * 0.13f;
            Vector2[] trees = { new Vector2(0.03f, 0.375f), new Vector2(0.11f, 0.39f), new Vector2(0.80f, 0.38f), new Vector2(0.89f, 0.395f), new Vector2(0.05f, 0.70f) };
            foreach (var p in trees) DrawIcon(new Rect(sw * p.x, sh * p.y - t, t, t * 1.1f), "tree");

            if (dim > 0f) DrawPanel(new Rect(0, 0, sw, sh), Alpha(Ink, dim), Color.clear, 0f);
        }

        // One diamond cell of the isometric grid, white at 12 %, repeated across the screen.
        private static Texture2D MakeIsoGrid()
        {
            const int w = 64, h = 36;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear, hideFlags = HideFlags.DontSave };
            var clear = new Color(1f, 1f, 1f, 0f);
            var line = new Color(1f, 1f, 1f, 0.12f);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    // distance to the two diagonals of the cell, measured along y
                    float d1 = Mathf.Abs(y - x * (h / (float)w)), d2 = Mathf.Abs(y - (h - x * (h / (float)w)));
                    tex.SetPixel(x, y, Mathf.Min(d1, d2) < 0.9f ? line : clear);
                }
            tex.Apply();
            return tex;
        }
    }
}
