using UnityEngine;

namespace EnerGo
{
    // Shared IMGUI look: Poppins text and rounded-rectangle buttons.
    public static class EnerGoUI
    {
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

        // Corner radius in GUI units; the radius stays the same whatever the button's size.
        private const float Radius = 12f;
        private static Texture2D primaryFill;
        private static Texture2D secondaryFill;

        // Colours sampled from btn_play.png / btn_secondary.png so the buttons keep their look.
        public static void DrawPrimaryButton(Rect rect)
        {
            if (primaryFill == null) primaryFill = Gradient(new Color32(76, 238, 177, 255), new Color32(5, 157, 106, 255));
            DrawRounded(rect, primaryFill, new Color32(120, 255, 205, 255));
        }

        public static void DrawSecondaryButton(Rect rect)
        {
            if (secondaryFill == null) secondaryFill = Gradient(new Color32(17, 35, 49, 235), new Color32(9, 22, 31, 243));
            DrawRounded(rect, secondaryFill, new Color32(58, 110, 132, 255));
        }

        // Flat rounded panel (HUD boxes, quiz card). Tinted by GUI.color like the buttons.
        public static void DrawPanel(Rect rect, Color fill, Color outline, float radius = Radius, float outlineWidth = 1.5f)
        {
            if (Event.current.type != EventType.Repaint) return;
            Color tint = GUI.color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, fill * tint, 0f, radius);
            GUI.DrawTexture(rect, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, outline * tint, outlineWidth, radius);
        }

        private static void DrawRounded(Rect rect, Texture2D fill, Color outline)
        {
            if (Event.current.type != EventType.Repaint) return;
            Color tint = GUI.color; // callers darken GUI.color while the button is pressed
            GUI.DrawTexture(rect, fill, ScaleMode.StretchToFill, true, 0f, tint, 0f, Radius);
            GUI.DrawTexture(rect, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, outline * tint, 1.5f, Radius);
        }

        private static Texture2D Gradient(Color top, Color bottom)
        {
            const int h = 32;
            var tex = new Texture2D(1, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.DontSave };
            for (int y = 0; y < h; y++) tex.SetPixel(0, y, Color.Lerp(bottom, top, y / (h - 1f))); // y=0 is the bottom row
            tex.Apply();
            return tex;
        }
    }
}
