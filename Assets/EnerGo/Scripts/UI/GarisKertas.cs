using UnityEngine;
using UnityEngine.UIElements;

namespace EnerGo.UI
{
    // Notebook rule lines every 28 units (design system .eg-kartu--garis), drawn because USS has no gradients.
    [UxmlElement]
    public partial class GarisKertas : VisualElement
    {
        static readonly Color Garis = new Color32(0xd3, 0xc4, 0xa1, 0xff); // garis-kertas

        public GarisKertas()
        {
            pickingMode = PickingMode.Ignore;
            generateVisualContent += ctx =>
            {
                var r = contentRect;
                var p = ctx.painter2D;
                p.strokeColor = Garis;
                p.lineWidth = 1f;
                for (float y = 27.5f; y < r.height; y += 28f)
                {
                    p.BeginPath();
                    p.MoveTo(new Vector2(0, y));
                    p.LineTo(new Vector2(r.width, y));
                    p.Stroke();
                }
            };
        }
    }
}
