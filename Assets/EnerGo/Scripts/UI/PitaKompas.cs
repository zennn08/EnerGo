using UnityEngine;
using UnityEngine.UIElements;

namespace EnerGo.UI
{
    // The paper ruler under the capture header (design system PitaKompas): ±span around the view, ticks every 15° and 3°,
    // labels every 45°, and the SDA as a dot, an edge marker, or for uranium a rough sector.
    [UxmlElement]
    public partial class PitaKompas : VisualElement
    {
        public struct State
        {
            public bool HasCompass, ShowDot, ShowSector;
            public float Heading, Half, Dot01, Lo01, Hi01;
            public int EdgeSide;    // -1 left, 1 right, 0 none
            public int EdgeDeg;
        }

        const float Inset = 14f;
        static readonly Color Tinta = new Color32(0x1f, 0x2a, 0x36, 0xff), Skala = new Color32(0x1f, 0x2a, 0x36, 0x70), Oranye = new Color32(0xf0, 0x7f, 0x3c, 0xff);
        static readonly Color Sektor = new Color32(0x2e, 0x7a, 0x47, 0x55), Tengah = new Color32(0xb5, 0x36, 0x2b, 0xff);

        State state;
        readonly Label[] labels = new Label[8];
        readonly Label edge, off;

        public PitaKompas()
        {
            pickingMode = PickingMode.Ignore;
            generateVisualContent += Paint;
            for (int i = 0; i < 8; i++)
            {
                labels[i] = new Label(CompassMath.DirectionLabel(i * 45)) { pickingMode = PickingMode.Ignore };
                labels[i].AddToClassList("pita__label");
                if (i == 0) labels[i].AddToClassList("pita__label--utara");
                Add(labels[i]);
            }
            edge = new Label { pickingMode = PickingMode.Ignore };
            edge.AddToClassList("pita__tepi");
            Add(edge);
            off = new Label("KOMPAS TIDAK TERSEDIA") { pickingMode = PickingMode.Ignore };
            off.AddToClassList("pita__mati");
            Add(off);
        }

        float X(float x01) => contentRect.center.x + x01 * (contentRect.width * 0.5f - Inset);

        public void Show(State s)
        {
            state = s;
            MarkDirtyRepaint();
            off.style.display = s.HasCompass ? DisplayStyle.None : DisplayStyle.Flex;
            for (int i = 0; i < 8; i++)
            {
                float x01 = s.HasCompass ? CompassMath.StripX(i * 45f, s.Heading, s.Half) : 2f;
                bool on = Mathf.Abs(x01) <= 1f;
                labels[i].style.display = on ? DisplayStyle.Flex : DisplayStyle.None;
                if (on) labels[i].style.left = X(x01) - 16f;
            }
            edge.style.display = s.EdgeSide != 0 ? DisplayStyle.Flex : DisplayStyle.None;
            if (s.EdgeSide != 0)
            {
                edge.text = s.EdgeSide < 0 ? "‹ " + s.EdgeDeg + "°" : s.EdgeDeg + "° ›";
                edge.style.left = s.EdgeSide < 0 ? 4f : StyleKeyword.Auto;
                edge.style.right = s.EdgeSide > 0 ? 4f : StyleKeyword.Auto;
            }
        }

        void Paint(MeshGenerationContext ctx)
        {
            var r = contentRect;
            if (r.width <= 1f || !state.HasCompass) return;
            var p = ctx.painter2D;

            if (state.ShowSector)
            {
                float a = X(state.Lo01), b = X(state.Hi01);
                p.fillColor = Sektor;
                p.BeginPath(); p.MoveTo(new Vector2(a, 2f)); p.LineTo(new Vector2(b, 2f)); p.LineTo(new Vector2(b, r.height - 2f)); p.LineTo(new Vector2(a, r.height - 2f)); p.ClosePath(); p.Fill();
            }

            p.strokeColor = Skala;
            for (int d = 0; d < 360; d += 3)
            {
                if (d % 45 == 0) continue; // the label sits here
                float x01 = CompassMath.StripX(d, state.Heading, state.Half);
                if (Mathf.Abs(x01) > 1f) continue;
                bool major = d % 15 == 0;
                p.lineWidth = major ? 2f : 1f;
                float h = major ? 14f : 7f, x = X(x01);
                p.BeginPath(); p.MoveTo(new Vector2(x, r.height - 2f - h)); p.LineTo(new Vector2(x, r.height - 2f)); p.Stroke();
            }

            if (state.ShowDot)
            {
                var c = new Vector2(X(state.Dot01), r.height * 0.5f);
                p.fillColor = Oranye; p.BeginPath(); p.Arc(c, 8f, 0f, 360f); p.Fill();
                p.strokeColor = Tinta; p.lineWidth = 2f; p.BeginPath(); p.Arc(c, 8f, 0f, 360f); p.Stroke();
            }

            // Where the phone looks.
            p.strokeColor = Tengah; p.lineWidth = 3f;
            p.BeginPath(); p.MoveTo(new Vector2(r.center.x, 2f)); p.LineTo(new Vector2(r.center.x, r.height - 2f)); p.Stroke();
        }
    }
}
