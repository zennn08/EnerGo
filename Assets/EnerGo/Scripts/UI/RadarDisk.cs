using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace EnerGo.UI
{
    // The heading-up radar dial (design system Radar): graph-paper disc, range rings, turning compass letters, sweep,
    // the player's beam, and one tappable node per SDA. Painter2D because USS cannot draw any of this.
    [UxmlElement]
    public partial class RadarDisk : VisualElement
    {
        public struct Node { public string Id, ResourceId; public double Bearing, Distance; public bool InRange, Locked; }

        const double RangeM = 150.0;
        const float RimMargin = 18f;
        static readonly Color Kertas = new Color32(0xfb, 0xf6, 0xea, 0xff), Tinta = new Color32(0x1f, 0x2a, 0x36, 0xff), Grid = new Color32(0xb9, 0xcb, 0xd3, 0xff);
        static readonly Color Oranye = new Color32(0xf0, 0x7f, 0x3c, 0xff), Sapu = new Color32(0xf0, 0x7f, 0x3c, 0x60), Sorot = new Color32(0xf0, 0x7f, 0x3c, 0x48);
        static readonly Color Cincin = new Color32(0x1f, 0x2a, 0x36, 0x40);

        public float Heading { get; set; }
        public float Sweep { get; set; }
        public string SelectedId { get; set; }
        public Action<string> Tapped;

        readonly Label[] letters = new Label[4];
        readonly Label[] ranges = new Label[2];
        readonly List<VisualElement> pool = new List<VisualElement>();

        float Radius => Mathf.Max(0f, Mathf.Min(contentRect.width, contentRect.height) * 0.5f - 2f);
        Vector2 Centre => contentRect.center;

        public RadarDisk()
        {
            generateVisualContent += Paint;
            string[] names = { "U", "T", "S", "B" };
            for (int i = 0; i < 4; i++)
            {
                letters[i] = new Label(names[i]) { pickingMode = PickingMode.Ignore };
                letters[i].AddToClassList("radar__huruf");
                if (i == 0) letters[i].AddToClassList("radar__huruf--utara");
                Add(letters[i]);
            }
            ranges[0] = new Label("50 m") { pickingMode = PickingMode.Ignore };
            ranges[1] = new Label("100 m") { pickingMode = PickingMode.Ignore };
            foreach (var r in ranges) { r.AddToClassList("radar__jarak"); Add(r); }
        }

        // Offset from the centre of a world bearing and distance on the heading-up dial.
        Vector2 OnDial(double bearing, double distance)
        {
            float a = (float)(bearing - Heading) * Mathf.Deg2Rad;
            float r = Mathf.Clamp01((float)(distance / RangeM)) * (Radius - RimMargin);
            return new Vector2(Mathf.Sin(a), -Mathf.Cos(a)) * r;
        }

        public void Show(List<Node> nodes, float time)
        {
            MarkDirtyRepaint();
            Vector2 c = Centre;
            float R = Radius;
            if (R <= 1f) return;
            for (int i = 0; i < 4; i++)
            {
                float a = (i * 90f - Heading) * Mathf.Deg2Rad;
                Vector2 p = c + new Vector2(Mathf.Sin(a), -Mathf.Cos(a)) * (R - 14f);
                letters[i].style.left = p.x - 12f;
                letters[i].style.top = p.y - 12f;
            }
            for (int i = 0; i < 2; i++)
            {
                float rr = (i + 1) * 50f / (float)RangeM * (R - RimMargin);
                ranges[i].style.left = c.x + 4f;
                ranges[i].style.top = c.y - rr - 16f;
            }
            while (pool.Count < nodes.Count) pool.Add(NewNode());
            for (int i = 0; i < pool.Count; i++)
            {
                var e = pool[i];
                bool used = i < nodes.Count;
                e.style.display = used ? DisplayStyle.Flex : DisplayStyle.None;
                if (!used) continue;
                var n = nodes[i];
                e.userData = n.Id;
                float size = n.InRange ? 58f : 44f;
                Vector2 p = c + OnDial(n.Bearing, n.Distance);
                e.style.left = p.x - size * 0.5f;
                e.style.top = p.y - size * 0.5f;
                e.style.width = size;
                e.style.height = size;
                e.EnableInClassList("is-dekat", n.InRange && !n.Locked);
                e.EnableInClassList("is-terpilih", n.Id == SelectedId);
                e.EnableInClassList("is-terkunci", n.Locked);
                var ikon = e.Q(className: "radar__node-ikon");
                if ((string)ikon.userData != n.ResourceId)
                {
                    if (ikon.userData != null) ikon.RemoveFromClassList("eg-ikon--" + ikon.userData);
                    ikon.AddToClassList("eg-ikon--" + n.ResourceId);
                    ikon.userData = n.ResourceId;
                }
                var glow = e.Q(className: "radar__node-cahaya");
                bool rare = n.ResourceId == ResourceIds.Uranium; // rare: pulse a glow so it is noticed
                glow.style.display = rare ? DisplayStyle.Flex : DisplayStyle.None;
                if (rare) { float k = 1.25f + 0.2f * Mathf.Sin(time * 4f); glow.style.scale = new Scale(new Vector3(k, k, 1f)); }
            }
        }

        VisualElement NewNode()
        {
            var e = new VisualElement();
            e.AddToClassList("radar__node");
            var glow = new VisualElement { pickingMode = PickingMode.Ignore };
            glow.AddToClassList("radar__node-cahaya");
            var ikon = new VisualElement { pickingMode = PickingMode.Ignore };
            ikon.AddToClassList("radar__node-ikon");
            e.Add(glow);
            e.Add(ikon);
            e.RegisterCallback<ClickEvent>(_ => Tapped?.Invoke(e.userData as string));
            Add(e);
            return e;
        }

        void Paint(MeshGenerationContext ctx)
        {
            float R = Radius;
            if (R <= 1f) return;
            Vector2 c = Centre;
            var p = ctx.painter2D;

            p.fillColor = Kertas;
            p.BeginPath(); p.Arc(c, R, 0f, 360f); p.Fill();

            // Millimetre grid, clipped to the disc by working out each chord.
            p.strokeColor = Grid; p.lineWidth = 1f;
            for (float d = 40f; d < R; d += 40f)
            {
                float half = Mathf.Sqrt(R * R - d * d);
                foreach (float s in new[] { -1f, 1f })
                {
                    p.BeginPath(); p.MoveTo(c + new Vector2(s * d, -half)); p.LineTo(c + new Vector2(s * d, half)); p.Stroke();
                    p.BeginPath(); p.MoveTo(c + new Vector2(-half, s * d)); p.LineTo(c + new Vector2(half, s * d)); p.Stroke();
                }
            }
            p.BeginPath(); p.MoveTo(c + Vector2.down * R); p.LineTo(c + Vector2.up * R); p.Stroke();
            p.BeginPath(); p.MoveTo(c + Vector2.left * R); p.LineTo(c + Vector2.right * R); p.Stroke();

            // Dashed range rings at 50 and 100 m.
            p.strokeColor = Cincin; p.lineWidth = 2f;
            for (int ring = 1; ring <= 2; ring++)
            {
                float rr = ring * 50f / (float)RangeM * (R - RimMargin);
                for (float a = 0f; a < 360f; a += 12f) { p.BeginPath(); p.Arc(c, rr, a, a + 7f); p.Stroke(); }
            }

            // Sweep line, slowly turning.
            float sw = Sweep * Mathf.Deg2Rad;
            p.strokeColor = Sapu; p.lineWidth = 2f;
            p.BeginPath(); p.MoveTo(c); p.LineTo(c + new Vector2(Mathf.Sin(sw), -Mathf.Cos(sw)) * (R - 4f)); p.Stroke();

            // The player: an orange dot with a soft beam up the way the phone faces.
            p.fillColor = Sorot;
            p.BeginPath(); p.MoveTo(c); p.LineTo(c + new Vector2(-44f, -66f)); p.LineTo(c + new Vector2(44f, -66f)); p.ClosePath(); p.Fill();
            p.fillColor = Kertas;
            p.BeginPath(); p.Arc(c, 12f, 0f, 360f); p.Fill();
            p.fillColor = Oranye;
            p.BeginPath(); p.Arc(c, 8.5f, 0f, 360f); p.Fill();

            p.strokeColor = Tinta; p.lineWidth = 3f;
            p.BeginPath(); p.Arc(c, R, 0f, 360f); p.Stroke();
        }
    }
}
