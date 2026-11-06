using UnityEngine;
using UnityEngine.UIElements;

namespace EnerGo.UI
{
    // Sheets Pengaturan and Latihan (and the shared curtain). Panduan is in LobbyView.Guide.cs.
    public partial class LobbyView
    {
        private static readonly string[] PracticeIds =
            { ResourceIds.Sun, ResourceIds.Coal, ResourceIds.Gold, ResourceIds.Oil, ResourceIds.Uranium, LobbyController.RandomResource };

        private VisualElement sheetPengaturan, sheetLatihan, sheetPanduan, aturAr, aturGetar, volume, volumeIsi, volumeKenop, resetBar, resetBarIsi;
        private Label aturArSub, aturGetarSub, volumePersen;
        private Button btnReset;
        private bool volumeDragging;

        partial void BindSheets()
        {
            sheetPengaturan = root.Q("sheet-pengaturan");
            sheetLatihan = root.Q("sheet-latihan");
            aturAr = root.Q("atur-ar");
            aturArSub = root.Q<Label>("atur-ar-sub");
            aturGetar = root.Q("atur-getar");
            aturGetarSub = root.Q<Label>("atur-getar-sub");
            volume = root.Q("atur-volume");
            volumeIsi = volume.Q(className: "eg-bar__isi");
            volumeKenop = volume.Q(className: "eg-slider__kenop");
            volumePersen = root.Q<Label>("atur-volume-persen");
            btnReset = root.Q<Button>("btn-reset");
            resetBar = root.Q("reset-bar");
            resetBarIsi = resetBar.Q(className: "eg-bar__isi");

            sheetPanduan = root.Q("sheet-panduan");
            foreach (var s in new[] { sheetPengaturan, sheetLatihan, sheetPanduan })
                new SheetGeser(s, () => { controller.CloseSheet(); return !IsSheetState(controller.State); });

            // Tap outside the card closes the sheet, on touch-down like before.
            root.Q("tirai").RegisterCallback<PointerDownEvent>(_ => controller.CloseSheet());

            // The whole settings row is the toggle.
            aturAr.RegisterCallback<ClickEvent>(_ => controller.ToggleARMode());
            aturGetar.RegisterCallback<ClickEvent>(_ => controller.ToggleVibration());
            root.Q<Button>("btn-ke-latihan").clicked += () => controller.OpenPracticeFromSettings();
            btnReset.clicked += () => controller.TapReset();
            root.Q<Button>("btn-selesai").clicked += () => controller.CloseSheet();

            // Volume: follow the finger while held; save and play a click once on release, even off the bar.
            volume.RegisterCallback<PointerDownEvent>(e =>
            {
                if (!controller.InputReady) return;
                volumeDragging = true;
                volume.CapturePointer(e.pointerId);
                controller.PreviewVolume(VolumeAt(e.localPosition.x));
            });
            volume.RegisterCallback<PointerMoveEvent>(e => { if (volumeDragging) controller.PreviewVolume(VolumeAt(e.localPosition.x)); });
            volume.RegisterCallback<PointerUpEvent>(e => { if (volume.HasPointerCapture(e.pointerId)) volume.ReleasePointer(e.pointerId); EndVolumeDrag(); });
            volume.RegisterCallback<PointerCaptureOutEvent>(_ => EndVolumeDrag());

            foreach (var id in PracticeIds)
            {
                string pick = id;
                root.Q("latihan-" + id).RegisterCallback<ClickEvent>(_ => controller.SelectPractice(pick));
            }
            root.Q<Button>("btn-mulai-latihan").clicked += () => controller.StartPractice();
        }

        private float VolumeAt(float x) => Mathf.Clamp01(x / Mathf.Max(1f, volume.layout.width));

        private void EndVolumeDrag()
        {
            if (!volumeDragging) return;
            volumeDragging = false;
            controller.CommitVolume();
        }

        partial void RefreshSheets()
        {
            SetToggle(aturAr, controller.UseARCore);
            aturArSub.text = controller.UseARCore ? "Butuh Google Play Services for AR" : "Mati: pakai Sensor 360 (giroskop)";
            SetToggle(aturGetar, Haptics.Enabled);
            aturGetarSub.text = Haptics.Enabled ? "HP bergetar saat main" : "Mati: tanpa getaran";

            foreach (var id in PracticeIds)
            {
                bool random = id == LobbyController.RandomResource;
                var holder = root.Q("latihan-" + id);
                FillTile(holder, id, random ? -1 : EnerGoProgress.Current.Get(id).Total, false, ringkas: false);
                holder.Q(className: "eg-tile").EnableInClassList("is-terpilih", controller.SelectedPractice == id);
            }
        }

        private static void SetToggle(VisualElement row, bool on) => row.Q(className: "eg-toggle").EnableInClassList("is-nyala", on);

        // Per frame: values that move on their own (volume while dragging, the 3.5 s reset countdown).
        partial void UpdateSheets()
        {
            if (controller.State != LobbyController.ScreenState.Settings) return;
            float v = AudioListener.volume;
            volumeIsi.style.width = Length.Percent(v * 100f);
            volumeKenop.style.left = Length.Percent(v * 100f);
            volumePersen.text = Mathf.RoundToInt(v * 100f) + "%";

            bool confirming = controller.IsConfirmingReset;
            btnReset.text = confirming ? "TEKAN LAGI UNTUK MENGHAPUS" : "RESET PROGRES";
            btnReset.EnableInClassList("eg-btn--bahaya", confirming);
            btnReset.EnableInClassList("eg-btn--kertas", !confirming);
            btnReset.EnableInClassList("eg-btn--teks-bahaya", !confirming);
            resetBar.style.display = confirming ? DisplayStyle.Flex : DisplayStyle.None;
            resetBarIsi.style.width = Length.Percent(100f * controller.ResetConfirmRemaining / LobbyController.ResetConfirmSeconds);
        }

        // The open sheet rises from the bottom; the others stay hidden.
        partial void ShowSheet(LobbyController.ScreenState s)
        {
            Open(sheetPengaturan, s == LobbyController.ScreenState.Settings);
            Open(sheetLatihan, s == LobbyController.ScreenState.Practice);
            Open(sheetPanduan, s == LobbyController.ScreenState.Guides);
            bool open = IsSheetState(s);
            var tirai = root.Q("tirai");
            tirai.style.opacity = open ? 1f : 0f;
            tirai.pickingMode = open ? PickingMode.Position : PickingMode.Ignore;
            if (open) { Show(lapisSheet, true); return; }
            // Stay on screen until the sheet has slid out.
            lapisSheet.pickingMode = PickingMode.Ignore;
            int token = ++sheetCloseToken;
            lapisSheet.schedule.Execute(() => { if (token == sheetCloseToken && !IsSheetState(controller.State)) Show(lapisSheet, false); }).StartingIn(SheetSlideMs);
        }

        private const int SheetSlideMs = 270; // .eg-sheet slides in 0.25 s
        private int sheetCloseToken;

        private static bool IsSheetState(LobbyController.ScreenState s) =>
            s == LobbyController.ScreenState.Settings || s == LobbyController.ScreenState.Practice || s == LobbyController.ScreenState.Guides;

        private static void Open(VisualElement sheet, bool open)
        {
            if (sheet == null) return;
            bool wasOpen = sheet.style.display != DisplayStyle.None && sheet.ClassListContains("is-terbuka");
            if (!open)
            {
                if (!wasOpen) { sheet.style.display = DisplayStyle.None; return; }
                sheet.RemoveFromClassList("is-terbuka"); // slides out, then hides unless it was reopened meanwhile
                sheet.schedule.Execute(() => { if (!sheet.ClassListContains("is-terbuka")) sheet.style.display = DisplayStyle.None; }).StartingIn(SheetSlideMs);
                return;
            }
            sheet.style.display = DisplayStyle.Flex;
            if (!wasOpen) sheet.schedule.Execute(() => sheet.AddToClassList("is-terbuka")); // next frame, so it animates
        }
    }
}
