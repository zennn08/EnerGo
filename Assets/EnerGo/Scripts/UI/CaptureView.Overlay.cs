using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace EnerGo.UI
{
    // The mini-game overlays on top of the camera and 3D props. Everything here only shows state: input is read by the
    // controller, except the one button on the gold fail card.
    public partial class CaptureView
    {
        private const float GasLimit = 0.6f, PressureLimit = 0.6f, DoseLimit = 0.75f;

        private VisualElement lapisOverlay, pipBatu, dulang, waterpass, gelembung, hitung, terbuang, bor, borLabel, borGas, gasIsi, pompa, pompaTarget, pompaBawah, drumIsi, tekananIsi,
            btnJepit, jepitIsi, radBawah, radIsi, dosisBaris, dosisIsi;
        private LingkaranBidik bidik, pompaCincin;
        private PanduanDulang panduanDulang;
        private Label bidikPersen, dulangPil, dulangAngka, dulangMulai, pompaTag, pompaMulai, radAngka, radTren, dosisHitung;
        private readonly List<ARCapturePrototype.CoalPip> pips = new List<ARCapturePrototype.CoalPip>();
        private readonly List<VisualElement> pipRows = new List<VisualElement>();

        partial void BindOverlay()
        {
            lapisOverlay = root.Q("lapis-overlay");
            pipBatu = root.Q("pip-batu");
            dulang = root.Q("dulang");
            waterpass = root.Q("dulang-waterpass");
            gelembung = root.Q("dulang-gelembung");
            hitung = root.Q("dulang-hitung");
            terbuang = root.Q("kartu-terbuang");
            bor = root.Q("bor");
            borLabel = root.Q("bor-label");
            borGas = root.Q("bor-gas");
            gasIsi = root.Q("bor-gas-isi");
            pompa = root.Q("pompa");
            pompaTarget = root.Q("pompa-target");
            pompaBawah = root.Q("pompa-bawah");
            drumIsi = root.Q("pompa-drum").Q(className: "eg-bar__isi");
            tekananIsi = root.Q("pompa-tekanan").Q(className: "eg-bar__isi");
            btnJepit = root.Q("btn-jepit");
            jepitIsi = root.Q("jepit-isi");
            radBawah = root.Q("rad-bawah");
            radIsi = root.Q("rad-bar").Q(className: "eg-bar__isi");
            dosisBaris = root.Q("dosis-baris");
            dosisIsi = root.Q("dosis-bar").Q(className: "eg-bar__isi");
            bidik = root.Q<LingkaranBidik>("bidik");
            pompaCincin = root.Q<LingkaranBidik>("pompa-cincin");
            panduanDulang = root.Q<PanduanDulang>("dulang-panduan");
            bidikPersen = root.Q<Label>("bidik-persen");
            dulangPil = root.Q<Label>("dulang-pil");
            dulangAngka = root.Q<Label>("dulang-angka");
            dulangMulai = root.Q<Label>("dulang-mulai");
            pompaTag = root.Q<Label>("pompa-tag");
            pompaMulai = root.Q<Label>("pompa-mulai");
            radAngka = root.Q<Label>("rad-angka");
            radTren = root.Q<Label>("rad-tren");
            dosisHitung = root.Q<Label>("dosis-hitung");

            root.Q<Button>("btn-endapan-baru").clicked += () => controller.RetryGoldFromCard();

            borLabel.Clear();
            for (int i = 0; i < controller.OilLayerCount; i++)
            {
                var l = new Label(controller.OilLayerName(i)) { pickingMode = PickingMode.Ignore };
                l.AddToClassList("overlay__lapis");
                borLabel.Add(l);
            }
            pipBatu.Clear();
            pipRows.Clear();
        }

        partial void UpdateOverlay(CaptureScreen screen, bool guideOpen)
        {
            bool on = screen == CaptureScreen.Play && !guideOpen && !float.IsNaN(root.layout.width);
            Show(lapisOverlay, on);
            if (!on) return;
            UpdateReticle();
            UpdateCoalPips();
            UpdateGold();
            UpdateOil();
            UpdateUranium();
        }

        private void UpdateReticle()
        {
            bool sun = controller.SunAiming, uranium = controller.UraniumAiming;
            Show(bidik, sun || uranium);
            Show(bidikPersen, sun);
            if (!sun && !uranium) return;
            Vector2 c = controller.ReticleCentre;
            float r = controller.ReticleRadiusGui;
            PlaceCircle(bidik, c, r);
            bidik.Set(sun && controller.SunLocked, uranium && controller.UraniumNear, uranium ? controller.UraniumGrip : 0f);
            if (!sun) return;
            bidikPersen.text = Mathf.FloorToInt(Mathf.Clamp01(controller.SunProgress) * 100f) + "%";
            PlaceAt(bidikPersen, c + new Vector2(0f, r + 30f));
        }

        // One row of hit marks over each coal lump. Rows and marks are reused, not rebuilt every frame.
        private void UpdateCoalPips()
        {
            controller.ReadCoalPips(pips);
            while (pipRows.Count < pips.Count)
            {
                var row = new VisualElement { pickingMode = PickingMode.Ignore };
                row.AddToClassList("overlay__pip-baris");
                pipBatu.Add(row);
                pipRows.Add(row);
            }
            for (int i = 0; i < pipRows.Count; i++)
            {
                var row = pipRows[i];
                Show(row, i < pips.Count);
                if (i >= pips.Count) continue;
                var pip = pips[i];
                Vector2 p = ToPanel(pip.ScreenPos);
                row.style.left = p.x;
                row.style.top = p.y;
                while (row.childCount < pip.Required)
                {
                    var mark = new VisualElement { pickingMode = PickingMode.Ignore };
                    mark.AddToClassList("overlay__pip");
                    row.Add(mark);
                }
                for (int j = 0; j < row.childCount; j++)
                {
                    Show(row[j], j < pip.Required);
                    row[j].EnableInClassList("is-kena", j < pip.Hits);
                }
            }
        }

        private void UpdateGold()
        {
            var phase = controller.GoldPhaseNow;
            Show(dulang, phase != ARCapturePrototype.GoldPhase.None);
            if (phase == ARCapturePrototype.GoldPhase.None) return;
            controller.ReadGoldPan(out Vector2 c, out float R);

            bool panning = phase == ARCapturePrototype.GoldPhase.Panning;
            Show(panduanDulang, panning);
            if (panning)
            {
                PlaceCircle(panduanDulang, c, R * 1.12f + 11f);
                panduanDulang.Set(controller.GoldSwirling, controller.GoldRough, Time.time * 90f, controller.GoldTiltDeg);
            }

            Show(dulangPil, phase >= ARCapturePrototype.GoldPhase.Panning);
            dulangPil.text = controller.GoldCountLabel;
            PlaceAt(dulangPil, c + new Vector2(0f, -R - 30f));

            bool level = phase == ARCapturePrototype.GoldPhase.Leveling;
            Show(waterpass, level);
            Show(gelembung, level);
            if (level)
            {
                bool flat = controller.GoldFlat;
                PlaceCircle(waterpass, c, R * 0.2f);
                PlaceCircle(gelembung, c + controller.GoldBubbleOffset, 11f);
                waterpass.EnableInClassList("is-datar", flat);
                gelembung.EnableInClassList("is-datar", flat);
            }

            Show(hitung, phase == ARCapturePrototype.GoldPhase.Countdown);
            if (phase == ARCapturePrototype.GoldPhase.Countdown)
            {
                dulangAngka.text = controller.GoldCountdown.ToString();
                PlaceAt(hitung, c);
            }

            float age = controller.GoldPhaseAge;
            bool start = panning && age < 0.8f;
            Show(dulangMulai, start);
            if (start)
            {
                dulangMulai.style.opacity = 1f - age / 0.8f;
                PlaceAt(dulangMulai, c);
            }

            Show(terbuang, phase == ARCapturePrototype.GoldPhase.Failed);
            if (phase == ARCapturePrototype.GoldPhase.Failed) PlaceAt(terbuang, c);
        }

        private void UpdateOil()
        {
            bool active = controller.OilActive, pump = active && controller.OilShowsPump;
            Show(bor, active && !pump);
            Show(pompa, pump);
            Show(pompaBawah, pump);
            if (pump) UpdatePump();
            else if (active) UpdateDrill();
        }

        private void UpdateDrill()
        {
            float k = controller.GuiToPanel(root.layout.width);
            for (int i = 0; i < borLabel.childCount; i++)
            {
                Rect band = controller.OilBandGui(i);
                var label = borLabel[i];
                Show(label, band.height >= 14f);
                label.style.left = (band.x + 6f) * k;
                label.style.top = band.center.y * k;
            }
            Rect section = controller.OilSectionGui;
            Place(borGas, new Rect(section.xMax + 14f, section.y, 12f, section.height));
            gasIsi.style.height = Length.Percent(100f * Mathf.Clamp01(controller.OilGas));
            gasIsi.EnableInClassList("overlay__isi-panas", CaptureScreens.GaugeWarn(controller.OilGas, GasLimit));
        }

        private void UpdatePump()
        {
            Vector2 c = controller.PumpCentre;
            float r0 = ARCapturePrototype.PumpTargetRadius;
            PlaceCircle(pompaTarget, c, r0);
            PlaceAt(pompaTag, c);
            bool started = controller.PumpStarted;
            Show(pompaCincin, started);
            Show(pompaMulai, !started);
            if (started) PlaceCircle(pompaCincin, c, r0 * CaptureScreens.PumpRingScale(controller.PumpStrokeAge, controller.PumpBeat));
            else PlaceAt(pompaMulai, c + new Vector2(0f, r0 + 26f));

            drumIsi.style.width = Length.Percent(100f * Mathf.Clamp01(controller.OilDrum));
            tekananIsi.style.width = Length.Percent(100f * Mathf.Clamp01(controller.PumpPressure));
            tekananIsi.EnableInClassList("overlay__isi-panas", CaptureScreens.GaugeWarn(controller.PumpPressure, PressureLimit));
        }

        private void UpdateUranium()
        {
            bool aiming = controller.UraniumAiming, collecting = controller.UraniumCollecting;
            Show(radBawah, aiming);
            Show(btnJepit, collecting);
            if (!aiming) return;

            radAngka.text = controller.UraniumMeter.ToString();
            radIsi.style.width = Length.Percent(100f * CaptureScreens.UraniumHeat(controller.UraniumMeter));
            int trend = controller.UraniumTrend;
            radTren.style.visibility = trend != 0 ? Visibility.Visible : Visibility.Hidden;
            if (trend != 0) radTren.text = trend > 0 ? "SINYAL MENGUAT ↑" : "SINYAL MELEMAH ↓";
            dosisBaris.style.visibility = collecting ? Visibility.Visible : Visibility.Hidden;
            if (!collecting) return;

            Place(btnJepit, controller.UraniumGripGui);
            btnJepit.EnableInClassList("is-tekan", controller.UraniumGripHeld);
            float size = 100f * Mathf.Clamp01(controller.UraniumGrip);
            jepitIsi.style.width = Length.Percent(size);
            jepitIsi.style.height = Length.Percent(size);
            dosisIsi.style.width = Length.Percent(100f * Mathf.Clamp01(controller.UraniumDose));
            dosisIsi.EnableInClassList("overlay__isi-panas", CaptureScreens.GaugeWarn(controller.UraniumDose, DoseLimit));
            dosisHitung.text = controller.UraniumCount;
        }

        private void PlaceCircle(VisualElement e, Vector2 centre, float radius) =>
            Place(e, new Rect(centre.x - radius, centre.y - radius, radius * 2f, radius * 2f));

        // For elements sized by USS: a class translates them so this point is their middle.
        private void PlaceAt(VisualElement e, Vector2 gui)
        {
            float k = controller.GuiToPanel(root.layout.width);
            e.style.left = gui.x * k;
            e.style.top = gui.y * k;
        }
    }
}
