using UnityEngine;

namespace EnerGo
{
    // Shared IMGUI look ("Solar Clean", see Branding/logo/final/guide.html): palette tokens, Poppins text,
    // rounded panels and chunky game buttons with a darker lip underneath.
    public static class EnerGoUI
    {
        // ── Palette ───────────────────────────────────────────────────
        public static readonly Color Cream = Hex(0xFAF7F0);   // page background
        public static readonly Color Paper = Color.white;     // cards, panels
        public static readonly Color Ink = Hex(0x16181D);     // text, icons
        public static readonly Color Muted = Hex(0x6B6F78);   // secondary text
        public static readonly Color Line = Hex(0xE6E0D4);    // outlines, tracks
        public static readonly Color Orange = Hex(0xFF6B2C);  // primary action, "Go"
        public static readonly Color OrangeLip = Hex(0xC94A14);
        public static readonly Color Sun = Hex(0xFFC93C);     // highlight, reward
        public static readonly Color Success = Hex(0x1E9A5A);
        public static readonly Color Danger = Hex(0xE5484D);

        // In-game colour per resource (the crystal is recoloured per resource; the logo itself never changes).
        public static Color Resource(string id) =>
            id == ResourceIds.Sun ? Sun : id == ResourceIds.Coal ? Hex(0x3A3F4B) : id == ResourceIds.Gold ? Hex(0xE8B23A) : Orange;

        public static Color Alpha(Color c, float a) => new Color(c.r, c.g, c.b, a);

        private static Color Hex(int rgb) => new Color(((rgb >> 16) & 255) / 255f, ((rgb >> 8) & 255) / 255f, (rgb & 255) / 255f, 1f);

        // ── Text ──────────────────────────────────────────────────────
        private static Font regular;
        private static Font bold;

        // Poppins for all IMGUI text. Bold styles get the real Poppins-Bold file instead of Unity's synthetic bold.
        public static void ApplyFont(params GUIStyle[] styles)
        {
            if (regular == null) regular = Resources.Load<Font>("Fonts/Poppins-Regular");
            if (bold == null) bold = Resources.Load<Font>("Fonts/Poppins-Bold");

            foreach (var s in styles)
            {
                if (s == null || s.font == bold) continue; // already applied
                bool isBold = s.fontStyle == FontStyle.Bold || s.fontStyle == FontStyle.BoldAndItalic;
                s.font = isBold ? bold : regular;
                if (isBold) s.fontStyle = s.fontStyle == FontStyle.BoldAndItalic ? FontStyle.Italic : FontStyle.Normal;
            }
        }

        // One-line label style in Poppins. Call from OnGUI (needs GUI.skin).
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

        // ── Shapes ────────────────────────────────────────────────────
        public const float Radius = 16f;
        private const float Lip = 4f; // button depth; a pressed button sinks onto its lip

        // Flat rounded panel. Tinted by GUI.color.
        public static void DrawPanel(Rect rect, Color fill, Color outline, float radius = Radius, float outlineWidth = 1.5f)
        {
            if (Event.current.type != EventType.Repaint) return;
            Color tint = GUI.color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, fill * tint, 0f, radius);
            if (outline.a > 0f)
                GUI.DrawTexture(rect, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, outline * tint, outlineWidth, radius);
        }

        // White card with a soft drop shadow: the default surface on both the cream lobby and the camera feed.
        public static void DrawCard(Rect rect, float radius = Radius, Color? outline = null, float outlineWidth = 1.5f)
        {
            DrawPanel(new Rect(rect.x, rect.y + 6f, rect.width, rect.height), Alpha(Ink, 0.10f), Color.clear, radius);
            DrawPanel(rect, Paper, outline ?? Line, radius, outlineWidth);
        }

        // Returns the face rect (shifted down while pressed) so callers can place the label on it.
        public static Rect DrawPrimaryButton(Rect rect, bool pressed = false) => DrawButton(rect, Orange, OrangeLip, Color.clear, pressed);

        public static Rect DrawSecondaryButton(Rect rect, bool pressed = false) => DrawButton(rect, Paper, Line, Line, pressed);

        public static Rect DrawDarkButton(Rect rect, bool pressed = false) => DrawButton(rect, Ink, Hex(0x000000), Color.clear, pressed);

        private static Rect DrawButton(Rect rect, Color face, Color lip, Color outline, bool pressed)
        {
            var faceRect = new Rect(rect.x, rect.y + (pressed ? Lip : 0f), rect.width, rect.height - Lip);
            float r = Mathf.Min(Radius, faceRect.height * 0.5f);
            if (!pressed) DrawPanel(new Rect(rect.x, rect.y + Lip, rect.width, rect.height - Lip), lip, Color.clear, r);
            DrawPanel(faceRect, face, outline, r, 2f);
            return faceRect;
        }

        // Horizontal progress bar on a Line-coloured track.
        public static void DrawBar(Rect track, float progress, Color fill)
        {
            DrawPanel(track, Line, Color.clear, track.height * 0.5f);
            if (progress > 0.01f)
                DrawPanel(new Rect(track.x, track.y, Mathf.Max(track.height, track.width * Mathf.Clamp01(progress)), track.height),
                    fill, Color.clear, track.height * 0.5f);
        }
    }
}
