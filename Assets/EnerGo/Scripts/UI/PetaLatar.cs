using UnityEngine;
using UnityEngine.UIElements;

namespace EnerGo.UI
{
    // The design system's .eg-peta world background, drawn with Painter2D because USS has no layered
    // gradients: two land masses, a lake, contour rings around the land, and a 40-unit map grid.
    // Values mirror docs/design-system/components/bundle.css (.eg-peta).
    [UxmlElement]
    public partial class PetaLatar : VisualElement
    {
        static readonly Color Darat1 = new Color32(169, 199, 154, 140);   // rgba(169,199,154,.55)
        static readonly Color Darat2 = new Color32(169, 199, 154, 128);   // .5
        static readonly Color Air = new Color32(156, 195, 211, 179);      // rgba(156,195,211,.7)
        static readonly Color Kontur = new Color32(110, 155, 94, 71);     // .28
        static readonly Color Grid = new Color32(185, 203, 211, 89);      // .35

        public PetaLatar()
        {
            AddToClassList("eg-peta");
            pickingMode = PickingMode.Ignore;
            generateVisualContent += Draw;
        }

        void Draw(MeshGenerationContext ctx)
        {
            var r = contentRect;
            if (r.width <= 0 || r.height <= 0) return;
            var p = ctx.painter2D;

            p.strokeColor = Grid; p.lineWidth = 1f;
            for (float x = 0; x <= r.width; x += 40f) Line(p, new Vector2(x, 0), new Vector2(x, r.height));
            for (float y = 0; y <= r.height; y += 40f) Line(p, new Vector2(0, y), new Vector2(r.width, y));

            var c1 = new Vector2(r.width * 0.18f, r.height * 0.26f);
            var c2 = new Vector2(r.width * 0.88f, r.height * 0.78f);
            Ellipse(p, c1, 150f, 110f, Darat1);
            Ellipse(p, c2, 170f, 120f, Darat2);
            Ellipse(p, new Vector2(r.width * 0.72f, r.height * 0.22f), 60f, 40f, Air);

            p.strokeColor = Kontur; p.lineWidth = 1.5f;
            float aspect = r.height / Mathf.Max(1f, r.width);
            Rings(p, c1, 31.5f, aspect, r);
            Rings(p, c2, 35.5f, aspect, r);
        }

        static void Line(Painter2D p, Vector2 a, Vector2 b) { p.BeginPath(); p.MoveTo(a); p.LineTo(b); p.Stroke(); }

        static void Rings(Painter2D p, Vector2 c, float step, float aspect, Rect r)
        {
            float reach = Mathf.Max(r.width, r.height) * 1.5f;
            for (float rx = step; rx < reach; rx += step) EllipsePath(p, c, rx, rx * aspect, false);
        }

        static void Ellipse(Painter2D p, Vector2 c, float rx, float ry, Color fill)
        {
            p.fillColor = fill;
            EllipsePath(p, c, rx, ry, true);
        }

        // Ellipse as four cubic Béziers (k = 0.5523 approximates a quarter circle).
        static void EllipsePath(Painter2D p, Vector2 c, float rx, float ry, bool fill)
        {
            const float k = 0.5523f;
            float ox = rx * k, oy = ry * k;
            p.BeginPath();
            p.MoveTo(new Vector2(c.x + rx, c.y));
            p.BezierCurveTo(new Vector2(c.x + rx, c.y + oy), new Vector2(c.x + ox, c.y + ry), new Vector2(c.x, c.y + ry));
            p.BezierCurveTo(new Vector2(c.x - ox, c.y + ry), new Vector2(c.x - rx, c.y + oy), new Vector2(c.x - rx, c.y));
            p.BezierCurveTo(new Vector2(c.x - rx, c.y - oy), new Vector2(c.x - ox, c.y - ry), new Vector2(c.x, c.y - ry));
            p.BezierCurveTo(new Vector2(c.x + ox, c.y - ry), new Vector2(c.x + rx, c.y - oy), new Vector2(c.x + rx, c.y));
            p.ClosePath();
            if (fill) p.Fill(); else p.Stroke();
        }
    }
}
