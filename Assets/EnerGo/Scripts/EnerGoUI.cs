using System.Collections.Generic;
using UnityEngine;

namespace EnerGo
{
    // Shared IMGUI look ("Arcade Cerah", docs/superpowers/specs/2026-10-23-ui-arcade-cerah-design.md): palette tokens,
    // Lilita One titles, Fredoka text and touch input. Shapes, icons and the world backdrop live in EnerGoUI.Draw.cs.
    public static partial class EnerGoUI
    {
        // ── Palette ───────────────────────────────────────────────────
        public static readonly Color Cream = Hex(0xFAF7F0);   // legacy page background
        public static readonly Color Paper = Hex(0xFFFAF0);   // card face
        public static readonly Color Ink = Hex(0x14213A);     // outlines, text stroke, lips
        public static readonly Color Muted = Hex(0x3A4256);   // secondary text on paper
        public static readonly Color Line = Hex(0xE6DCC0);    // thin rules on paper
        public static readonly Color Orange = Hex(0xFF6B2C);  // primary action
        public static readonly Color OrangeLip = Hex(0xC94A14);
        public static readonly Color Sun = Hex(0xFFC93C);     // reward, energy, label accent
        public static readonly Color Success = Hex(0x45B83A);
        public static readonly Color Danger = Hex(0xE8304E);
        public static readonly Color Berry = Hex(0xFF3B5C);   // count badges
        public static readonly Color TitleOrange = Hex(0xFF9A1F);
        public static readonly Color PillBg = new Color(10 / 255f, 22 / 255f, 40 / 255f, 0.75f);

        public static readonly Color SkyTop = Hex(0x155D78), Sky = Hex(0x1E8FA6), SkyLow = Hex(0x36B8A8);
        public static readonly Color Grass = Hex(0x7BD36A), GrassLow = Hex(0x4FB35A);
        public static readonly Color OrangeTop = Hex(0xFFC04D), OrangeBot = Hex(0xFF6B2C);
        public static readonly Color OkTop = Hex(0xC6F58A), OkBot = Hex(0x45B83A);
        public static readonly Color BadTop = Hex(0xFF9AA8), BadBot = Hex(0xE8304E);
        public static readonly Color InfoTop = Hex(0x8FF3FF), InfoBot = Hex(0x1AA4FF);
        public static readonly Color SunTop = Hex(0xFFF07A), SunBot = Hex(0xFFBE1A);
        public static readonly Color PaperTop = Hex(0xFFFAF0), PaperBot = Hex(0xFFE9C2);
        public static readonly Color OffTop = Hex(0xC9CCD6), OffBot = Hex(0x8D93A3);
        public static readonly Color DarkTop = Hex(0x34466B), DarkBot = Hex(0x22304D);

        // Tile gradient per resource; anything else (the "Acak" tile) is pink.
        public static Color ResourceTop(string id) =>
            id == ResourceIds.Sun ? Hex(0xFFF0A0) : id == ResourceIds.Coal ? Hex(0xB9BFCC) : id == ResourceIds.Gold ? Hex(0xFFE9A0)
            : id == ResourceIds.Oil ? Hex(0xA99BFF) : id == ResourceIds.Uranium ? Hex(0xD6FF9C) : Hex(0xFFB3E0);

        public static Color ResourceBot(string id) =>
            id == ResourceIds.Sun ? Hex(0xFFC93C) : id == ResourceIds.Coal ? Hex(0x6B7180) : id == ResourceIds.Gold ? Hex(0xF0B52A)
            : id == ResourceIds.Oil ? Hex(0x5A49D6) : id == ResourceIds.Uranium ? Hex(0x58C23A) : Hex(0xD1409F);

        // In-game colour per resource: the middle of its tile gradient.
        public static Color Resource(string id) => Color.Lerp(ResourceTop(id), ResourceBot(id), 0.5f);

        public static Color Alpha(Color c, float a) => new Color(c.r, c.g, c.b, a);

        private static Color Hex(int rgb) => new Color(((rgb >> 16) & 255) / 255f, ((rgb >> 8) & 255) / 255f, (rgb & 255) / 255f, 1f);

        // Mockup px -> GUI units.
        public static float M(float mockPx) => mockPx * UiLayout.Mock;

        // ── Text ──────────────────────────────────────────────────────
        private static Font regular;
        private static Font bold;
        private static Font title;

        // Fredoka for all IMGUI body text: SemiBold for regular styles, the real Bold file for bold ones.
        public static void ApplyFont(params GUIStyle[] styles)
        {
            if (regular == null) regular = Resources.Load<Font>("Fonts/Fredoka-SemiBold");
            if (bold == null) bold = Resources.Load<Font>("Fonts/Fredoka-Bold");

            foreach (var s in styles)
            {
                if (s == null || s.font == bold || s.font == regular || (title != null && s.font == title)) continue; // already applied
                bool isBold = s.fontStyle == FontStyle.Bold || s.fontStyle == FontStyle.BoldAndItalic;
                s.font = isBold ? bold : regular;
                if (isBold) s.fontStyle = s.fontStyle == FontStyle.BoldAndItalic ? FontStyle.Italic : FontStyle.Normal;
            }
        }

        // One-line label style in Fredoka. Call from OnGUI (needs GUI.skin).
        public static GUIStyle Text(int size, bool isBold, Color color, TextAnchor align = TextAnchor.MiddleCenter, bool wrap = false)
        {
            var s = new GUIStyle(GUI.skin.label)
            {
                fontSize = size,
                fontStyle = isBold ? FontStyle.Bold : FontStyle.Normal,
                alignment = align,
                wordWrap = wrap,
                richText = true,
                padding = new RectOffset(0, 0, 0, 0),
                normal = { textColor = color }
            };
            ApplyFont(s);
            return s;
        }

        private static readonly Dictionary<(int, Color, TextAnchor), GUIStyle> titleStyles = new Dictionary<(int, Color, TextAnchor), GUIStyle>();

        // Lilita One, for titles, button labels and big numbers. Cached: safe to call every frame.
        public static GUIStyle Title(int size, Color color, TextAnchor align = TextAnchor.MiddleCenter)
        {
            if (titleStyles.TryGetValue((size, color, align), out var s)) return s;
            if (title == null) title = Resources.Load<Font>("Fonts/LilitaOne-Regular");
            s = new GUIStyle(GUI.skin.label)
            {
                font = title, fontSize = size, alignment = align, wordWrap = false, clipping = TextClipping.Overflow,
                padding = new RectOffset(0, 0, 0, 0), normal = { textColor = color }
            };
            titleStyles[(size, color, align)] = s;
            return s;
        }

        // Biggest font size (<= the style's) at which `text` fits `maxWidth`.
        public static int FitSize(GUIStyle style, string text, float maxWidth, int minSize) =>
            UiLayout.FitFontSize(style.CalcSize(new GUIContent(text)).x, style.fontSize, maxWidth, minSize);

        private static GUIStyle strokeStyle;

        // Text with an Ink outline: 8 Ink copies on a circle of `strokePx`, an optional Ink drop shadow, then the text.
        // ponytail: 9-10 draws per label; keep it to titles, buttons, numbers and camera labels, not paragraphs.
        public static void DrawStrokedLabel(Rect r, string text, GUIStyle style, float strokePx, float dropPx = 0f)
        {
            if (Event.current.type != EventType.Repaint) return;
            if (strokeStyle == null) strokeStyle = new GUIStyle(style);
            strokeStyle.font = style.font; strokeStyle.fontSize = style.fontSize; strokeStyle.fontStyle = style.fontStyle;
            strokeStyle.alignment = style.alignment; strokeStyle.wordWrap = style.wordWrap; strokeStyle.clipping = style.clipping;
            strokeStyle.padding = style.padding; strokeStyle.richText = false;
            strokeStyle.normal.textColor = Alpha(Ink, style.normal.textColor.a);
            if (dropPx > 0f) GUI.Label(new Rect(r.x, r.y + dropPx, r.width, r.height), text, strokeStyle);
            for (int i = 0; i < 8; i++)
            {
                float a = i * Mathf.PI * 0.25f;
                GUI.Label(new Rect(r.x + Mathf.Cos(a) * strokePx, r.y + Mathf.Sin(a) * strokePx, r.width, r.height), text, strokeStyle);
            }
            GUI.Label(r, text, style);
        }

        // Safe-area margins in GUI units (see UiLayout).
        public static float SafeTop(float scale) => UiLayout.SafeTop(Screen.height, Screen.safeArea.yMax, scale);
        public static float SafeBottom(float scale) => UiLayout.SafeBottom(Screen.safeArea.yMin, scale);

        // ── Input ─────────────────────────────────────────────────────
        private static float tapBlockedUntil;

        // One physical tap = one button press. Touches are read on touch-down from EnhancedTouch; IMGUI's own
        // button only counts real mouse clicks, because Android also replays every touch as a mouse event.
        // Counting both made a single tap press two buttons in a row (e.g. "LIHAT HASIL" and then
        // "LANJUT KE LOBBY", which sits in the same spot on the next card). `hudScale` maps screen pixels
        // to the scaled GUI space the rect is in.
        public static bool TapHit(Rect rect, float hudScale)
        {
            var pointer = Event.current.pointerType;
            bool clicked = GUI.Button(rect, GUIContent.none, GUIStyle.none); // always called: keeps IMGUI control ids stable
            if (Time.unscaledTime < tapBlockedUntil) return false;
            bool hit = clicked && pointer != PointerType.Touch;
            if (!hit && Event.current.type == EventType.Repaint)
            {
                foreach (var touch in UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches)
                {
                    if (touch.phase != UnityEngine.InputSystem.TouchPhase.Began) continue;
                    var p = new Vector2(touch.screenPosition.x / hudScale, (Screen.height - touch.screenPosition.y) / hudScale);
                    if (rect.Contains(p)) { hit = true; break; }
                }
            }
            if (hit) tapBlockedUntil = Time.unscaledTime + 0.35f; // also swallows the same tap's mouse replay
            return hit;
        }

        // True while a mouse button or a finger is held down inside `rect` (for the pressed look).
        public static bool IsHeld(Rect rect, float hudScale)
        {
            var mouse = UnityEngine.InputSystem.Mouse.current;
            if (mouse != null && mouse.leftButton.isPressed && rect.Contains(Event.current.mousePosition)) return true;
            foreach (var touch in UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches)
            {
                var p = new Vector2(touch.screenPosition.x / hudScale, (Screen.height - touch.screenPosition.y) / hudScale);
                if (rect.Contains(p)) return true;
            }
            return false;
        }
    }
}
