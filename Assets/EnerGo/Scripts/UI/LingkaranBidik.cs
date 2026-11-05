using UnityEngine;
using UnityEngine.UIElements;

namespace EnerGo.UI
{
    // The aiming ring (design system LingkaranBidik): an ink ring with a dashed paper ring inside, readable over any
    // camera feed. Locked turns the inner ring solid ochre; Strong tints it uranium; Fill sweeps it round with the grip.
    [UxmlElement]
    public partial class LingkaranBidik : VisualElement
    {
        const int Dashes = 28;
        static readonly Color Kertas = new Color32(0xfb, 0xf6, 0xea, 0xff), Tinta = new Color32(0x1f, 0x2a, 0x36, 0xff),
            Oker = new Color32(0xe2, 0xa7, 0x2e, 0xff), Uranium = new Color32(0x4f, 0xa8, 0x3a, 0xff);

        bool locked, strong;
        float fill;

        public LingkaranBidik() => generateVisualContent += Paint;

        public void Set(bool isLocked, bool isStrong, float gripFill)
        {
            if (isLocked == locked && isStrong == strong && Mathf.Approximately(gripFill, fill)) return;
            locked = isLocked;
            strong = isStrong;
            fill = gripFill;
            MarkDirtyRepaint();
        }

        void Paint(MeshGenerationContext ctx)
        {
            var p = ctx.painter2D;
            Vector2 c = contentRect.center;
            float r = Mathf.Min(contentRect.width, contentRect.height) * 0.5f - 2f;
            if (r <= 4f) return;

            p.lineWidth = 4f;
            if (locked)
            {
                Ring(p, c, r + 8f, Tinta, 0f, 360f);
                Ring(p, c, r + 4f, Oker, 0f, 360f);
            }
            Ring(p, c, r, Tinta, 0f, 360f);

            float inner = r - 4f;
            if (locked) Ring(p, c, inner, Oker, 0f, 360f);
            else
                for (int i = 0; i < Dashes; i++)
                {
                    float step = 360f / Dashes;
                    Ring(p, c, inner, strong ? Uranium : Kertas, i * step, i * step + step * 0.55f);
                }
            if (fill > 0f) Ring(p, c, inner, Uranium, -90f, -90f + 360f * Mathf.Clamp01(fill));
        }

        static void Ring(Painter2D p, Vector2 c, float r, Color color, float fromDeg, float toDeg)
        {
            p.strokeColor = color;
            p.BeginPath();
            p.Arc(c, r, fromDeg, toDeg);
            p.Stroke();
        }
    }
}
