using UnityEngine;
using UnityEngine.UIElements;

namespace EnerGo.UI
{
    // Dots round the gold pan show the circle to tilt along: one runs ahead until the player is swirling, and the big
    // one shows where the phone is tipped right now.
    [UxmlElement]
    public partial class PanduanDulang : VisualElement
    {
        const float NowDot = 9f, LeadDot = 6f, RingDot = 2.5f;
        static readonly Color Kertas = new Color32(0xfb, 0xf6, 0xea, 0xff), Tinta = new Color32(0x1f, 0x2a, 0x36, 0xff),
            Oranye = new Color32(0xf0, 0x7f, 0x3c, 0xff), Hutan = new Color32(0x2e, 0x7a, 0x47, 0xff), Stempel = new Color32(0xb5, 0x36, 0x2b, 0xff);

        bool swirling, rough;
        float leadDeg;
        float? tiltDeg;

        public PanduanDulang() => generateVisualContent += Paint;

        public void Set(bool isSwirling, bool isRough, float lead, float? tilt)
        {
            swirling = isSwirling;
            rough = isRough;
            leadDeg = lead;
            tiltDeg = tilt;
            MarkDirtyRepaint();
        }

        void Paint(MeshGenerationContext ctx)
        {
            var p = ctx.painter2D;
            Vector2 c = contentRect.center;
            float ring = Mathf.Min(contentRect.width, contentRect.height) * 0.5f - NowDot - 2f;
            if (ring <= 0f) return;

            var faint = Kertas;
            faint.a = swirling ? 0.35f : 0.8f;
            for (int i = 0; i < 16; i++) Dot(p, c, ring, i * 22.5f, RingDot, faint, null);
            if (!swirling) Dot(p, c, ring, leadDeg, LeadDot, Kertas, Tinta);
            if (tiltDeg != null) Dot(p, c, ring, tiltDeg.Value, NowDot, rough ? Stempel : swirling ? Hutan : Oranye, Tinta);
        }

        static void Dot(Painter2D p, Vector2 c, float ring, float deg, float r, Color fill, Color? edge)
        {
            float a = deg * Mathf.Deg2Rad;
            p.BeginPath();
            p.Arc(c + new Vector2(Mathf.Cos(a), -Mathf.Sin(a)) * ring, r, 0f, 360f);
            p.fillColor = fill;
            p.Fill();
            if (edge == null) return;
            p.strokeColor = edge.Value;
            p.lineWidth = 2f;
            p.Stroke();
        }
    }
}
