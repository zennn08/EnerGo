using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace EnerGo.UI
{
    // Village screen: the picture follows the phone clock and the stock, and the fill sheet spends SDA on the plant.
    public partial class LobbyView
    {
        // Where things stand in the village pictures (0..1). All three pictures line up exactly.
        private const float PlantU = 0.72f, PlantV = 0.575f;
        private const float PictureW = 1536f, PictureH = 2752f;
        private const float BuffaloU = 0.36f, BuffaloV = 0.55f;
        private static readonly Color BuffaloNightTint = new Color(0.45f, 0.5f, 0.7f);
        private const float RefreshSeconds = 1f;

        private static readonly Color FallbackDay = new Color(0.61f, 0.76f, 0.83f);
        private static readonly Color FallbackNightOff = new Color(0.07f, 0.10f, 0.15f);
        private static readonly Color FallbackNightOn = new Color(0.20f, 0.25f, 0.38f);

        private sealed class IsiRow { public string id; public Label nama, sub; public Button isi, semua; }

        private readonly List<IsiRow> isiRows = new List<IsiRow>();
        private VisualElement desaLatar, desaTerang, desaAwan, desaKerbau, desaSya, dialogWajah, desaBarIsi, lapisIsi, sheetIsi, lapisNama, btnPembangkit;
        private Label pembangkitMati, desaNama, desaSisa, desaPadam, desaMenipis, dialogTeks, isiSisa;
        private TextField inputNamaDesa;
        private float nextDesaRefresh;
        private bool desaShown, desaIsNightOn;
        private static System.Func<int> localHour = () => System.DateTime.Now.Hour; // swapped only to check day and night looks in the editor
        private const float SorakDarkSeconds = 0.8f, SorakFadeSeconds = 1.5f;
        private VisualElement lapisSorak, sorakGelap, sorakTerang, sorakSya, sorakKartu;
        private float sorakSince;
        private bool sorakActive, sorakDone;

        partial void BindDesa()
        {
            desaLatar = root.Q("desa-latar");
            desaTerang = root.Q("desa-malam-terang");
            desaAwan = root.Q("desa-awan");
            desaKerbau = root.Q("desa-kerbau");
            desaSya = root.Q("desa-sya");
            dialogWajah = root.Q("dialog-wajah");
            desaBarIsi = root.Q("desa-bar").Q(className: "eg-bar__isi");
            lapisIsi = root.Q("lapis-isi");
            sheetIsi = root.Q("sheet-isi");
            lapisNama = root.Q("lapis-nama");
            btnPembangkit = root.Q("btn-pembangkit");
            pembangkitMati = root.Q<Label>("pembangkit-mati");
            desaNama = root.Q<Label>("desa-nama");
            desaSisa = root.Q<Label>("desa-sisa");
            desaPadam = root.Q<Label>("desa-padam");
            desaMenipis = root.Q<Label>("desa-menipis");
            dialogTeks = root.Q<Label>("dialog-teks");
            isiSisa = root.Q<Label>("isi-sisa");
            inputNamaDesa = root.Q<TextField>("input-nama-desa");

            root.Q<Button>("btn-isi").clicked += OpenIsi;
            root.Q<Button>("btn-pembangkit").clicked += OpenIsi;
            root.Q<Button>("btn-isi-tutup").clicked += CloseIsi;
            root.Q("desa-tirai").RegisterCallback<PointerDownEvent>(_ => CloseIsi());
            root.Q<Button>("btn-ganti-nama").clicked += OpenNama;
            root.Q<Button>("btn-nama-batal").clicked += () => CloseNama(false);
            root.Q<Button>("btn-nama-simpan").clicked += () => CloseNama(true);
            desaLatar.RegisterCallback<GeometryChangedEvent>(_ => LayoutDesa());

            var daftar = root.Q("isi-daftar");
            foreach (var id in LobbyController.AllResources) daftar.Add(BuildIsiRow(id));

            Show(lapisIsi, false);
            Show(lapisNama, false);
            lapisSorak = root.Q("lapis-sorak");
            sorakGelap = root.Q("sorak-gelap");
            sorakTerang = root.Q("sorak-terang");
            sorakSya = root.Q("sorak-sya");
            sorakKartu = root.Q("sorak-kartu");
            root.Q<Button>("btn-sorak-lanjut").clicked += EndSorak;
            controller.FirstFill += StartSorak;
            Show(lapisSorak, false);
        }

        private VisualElement BuildIsiRow(string id)
        {
            var row = new IsiRow { id = id, nama = new Label(), sub = new Label(), isi = new Button { text = "ISI" }, semua = new Button { text = "SEMUA" } };
            isiRows.Add(row);
            var el = new VisualElement();
            el.AddToClassList("isi__baris");
            var ikon = new VisualElement();
            ikon.AddToClassList("isi__ikon");
            ikon.AddToClassList("eg-ikon--" + id);
            el.Add(ikon);
            var teks = new VisualElement();
            teks.AddToClassList("isi__teks");
            row.nama.AddToClassList("isi__nama");
            row.sub.AddToClassList("isi__sub");
            teks.Add(row.nama);
            teks.Add(row.sub);
            el.Add(teks);
            foreach (var b in new[] { row.isi, row.semua })
            {
                b.AddToClassList("eg-btn");
                b.AddToClassList("isi__tombol");
                el.Add(b);
            }
            row.isi.AddToClassList("eg-btn--oker");
            row.isi.clicked += () => Fill(id, false);
            row.semua.clicked += () => Fill(id, true);
            return el;
        }

        private void Fill(string id, bool all)
        {
            if (controller.FillPlant(id, all) == 0 && !string.IsNullOrEmpty(EnerGoProgress.Error)) isiSisa.text = EnerGoProgress.Error;
        }

        private void OpenIsi()
        {
            if (!controller.InputReady) return;
            controller.Tap();
            Show(lapisIsi, true);
            sheetIsi.schedule.Execute(() => sheetIsi.AddToClassList("is-terbuka"));
            RefreshDesa();
        }

        private void CloseIsi()
        {
            if (!controller.InputReady || lapisIsi.resolvedStyle.display == DisplayStyle.None) return;
            controller.Tap();
            sheetIsi.RemoveFromClassList("is-terbuka");
            Show(lapisIsi, false);
        }

        private void OpenNama()
        {
            if (!controller.InputReady) return;
            controller.Tap();
            inputNamaDesa.value = EnerGoProgress.Current.villageName;
            Show(lapisNama, true);
        }

        private void CloseNama(bool save)
        {
            if (!controller.InputReady) return;
            controller.Tap();
            if (save && !EnerGoProgress.RenameVillage(inputNamaDesa.value)) return;
            Show(lapisNama, false);
            RefreshDesa();
        }

        partial void RefreshDesa()
        {
            if (!controller.HomeVisible)
            {
                if (desaShown) ClearDesaPictures();
                return;
            }
            desaShown = true;
            var save = EnerGoProgress.Current;
            long remaining = controller.PlantRemaining;
            var status = DesaMath.StatusOf(remaining);
            bool isDay = GeoSpawner.IsDaytime(localHour());
            bool padam = status == DesaStatus.Padam;

            desaNama.text = DesaMath.CleanName(save.villageName);
            desaSisa.text = DesaText.FormatSisa(remaining);
            desaBarIsi.style.width = Length.Percent(100f * remaining / DesaMath.CapacitySec);
            desaPadam.style.display = padam ? DisplayStyle.Flex : DisplayStyle.None;
            desaMenipis.style.display = status == DesaStatus.Menipis ? DisplayStyle.Flex : DisplayStyle.None;
            pembangkitMati.style.display = padam ? DisplayStyle.Flex : DisplayStyle.None;

            string picture = DesaText.BackgroundFor(status, isDay);
            desaIsNightOn = picture == "desa_malam_terang";
            // Night: the lit picture lies over the dark one, so its lights can breathe.
            SetGambar(desaLatar, "Desa/" + (isDay ? picture : "desa_malam_padam"), isDay ? FallbackDay : FallbackNightOff);
            desaTerang.style.display = desaIsNightOn ? DisplayStyle.Flex : DisplayStyle.None;
            if (desaIsNightOn) SetGambar(desaTerang, "Desa/desa_malam_terang", FallbackNightOn);
            SetOptionalGambar(desaAwan, isDay ? "Desa/desa_awan" : null);
            SetOptionalGambar(desaKerbau, "Desa/desa_kerbau");
            desaKerbau.style.unityBackgroundImageTintColor = isDay ? Color.white : BuffaloNightTint;

            SetGambar(desaSya, "Pemandu/" + DesaText.PoseFor(status, isDay, save.plantEverFilled), Color.clear);
            SetGambar(dialogWajah, "Pemandu/" + DesaText.WajahFor(status, save.plantEverFilled), Color.clear);
            bool hasUnits = false;
            foreach (var id in LobbyController.AllResources) hasUnits |= save.Get(id).Total > 0;
            dialogTeks.text = DesaText.Status(status, save.plantEverFilled, hasUnits);

            isiSisa.text = "SISA " + DesaText.FormatSisa(remaining) + " · MAKS " + DesaText.FormatSisa(DesaMath.CapacitySec);
            foreach (var row in isiRows)
            {
                int owned = save.Get(row.id).Total;
                long perUnit = DesaMath.SecondsPerUnit(row.id);
                int fit = DesaMath.UnitsThatFit(remaining, perUnit, owned);
                row.nama.text = ResourceIds.Name(row.id) + " · " + owned + " unit";
                row.sub.text = owned == 0 ? "Belum punya" : fit == 0 ? "Stok penuh" : DesaText.FormatSisa(perUnit) + " per unit · " + DesaText.SourceOf(row.id);
                row.isi.SetEnabled(fit > 0);
                row.semua.SetEnabled(fit > 0);
            }
            LayoutDesa();
        }

        // The plant's tap area and sign follow the picture, which is cropped differently on each phone shape.
        private void LayoutDesa()
        {
            var box = desaLatar.contentRect;
            if (box.width <= 0f || float.IsNaN(box.width)) return;
            var plant = DesaMath.CoverPoint(PictureW, PictureH, box.width, box.height, PlantU, PlantV);
            btnPembangkit.style.left = plant.x - 65f;
            btnPembangkit.style.top = plant.y - 65f;
            pembangkitMati.style.left = plant.x - 28f;
            pembangkitMati.style.top = plant.y - 74f;
            var buffalo = DesaMath.CoverPoint(PictureW, PictureH, box.width, box.height, BuffaloU, BuffaloV);
            desaKerbau.style.left = buffalo.x;
            desaKerbau.style.top = buffalo.y;
        }

        partial void UpdateDesa()
        {
            if (!controller.HomeVisible) return;
            float t = Time.unscaledTime;
            if (t >= nextDesaRefresh)
            {
                nextDesaRefresh = t + RefreshSeconds;
                RefreshDesa(); // the stock may run out, or night may fall, while this screen is open
            }
            desaAwan.style.translate = new Translate(Mathf.Sin(t * 0.05f) * 30f, 0f); // small, so the clouds never cross the palm in front
            desaKerbau.style.translate = new Translate(0f, -Mathf.Abs(Mathf.Sin(t * 1.2f)) * 2f);
            if (desaIsNightOn) desaTerang.style.opacity = 0.96f + 0.04f * Mathf.Sin(t * 1.5f);
            UpdateSorak(t);
        }

        // First fill: at any hour the village goes dark, then lights up, then Sya cheers. plantEverFilled was saved
        // together with the units, so closing the app mid-scene neither repeats it nor loses anything.
        private void StartSorak()
        {
            sheetIsi.RemoveFromClassList("is-terbuka");
            Show(lapisIsi, false);
            Show(lapisSorak, true);
            SetGambar(sorakGelap, "Desa/desa_malam_padam", FallbackNightOff);
            SetGambar(sorakTerang, "Desa/desa_malam_terang", FallbackNightOn);
            SetGambar(sorakSya, "Pemandu/pemandu_bersorak", Color.clear);
            sorakTerang.style.opacity = 0f;
            sorakSya.style.display = DisplayStyle.None;
            sorakKartu.style.display = DisplayStyle.None;
            root.Q<Label>("sorak-teks").text = DesaMath.CleanName(EnerGoProgress.Current.villageName) + " menyala!";
            sorakSince = Time.unscaledTime;
            sorakDone = false;
            sorakActive = true;
        }

        private void UpdateSorak(float now)
        {
            if (!sorakActive || sorakDone) return;
            float t = now - sorakSince;
            sorakTerang.style.opacity = Mathf.Clamp01((t - SorakDarkSeconds) / SorakFadeSeconds);
            if (t < SorakDarkSeconds + SorakFadeSeconds) return;
            sorakDone = true;
            Haptics.Pulse(60);
            sorakSya.style.display = DisplayStyle.Flex;
            sorakKartu.style.display = DisplayStyle.Flex;
        }

        private void EndSorak()
        {
            if (!controller.InputReady) return;
            controller.Tap();
            sorakActive = false;
            Show(lapisSorak, false);
            foreach (var e in new[] { sorakGelap, sorakTerang, sorakSya }) ClearGambar(e);
            RefreshDesa();
        }

        private void ClearDesaPictures()
        {
            desaShown = false;
            foreach (var e in new[] { desaLatar, desaTerang, desaAwan, desaKerbau, desaSya, dialogWajah }) ClearGambar(e);
        }
    }
}
