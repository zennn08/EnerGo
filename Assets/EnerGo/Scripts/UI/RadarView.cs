using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace EnerGo.UI
{
    // Draws the exploration radar from RadarController's state. No rules live here.
    [RequireComponent(typeof(UIDocument))]
    public class RadarView : MonoBehaviour
    {
        [SerializeField] private RadarController controller;

        private RadarDisk disk;
        private VisualElement info, kartuTarget, kartuTunggu, lapisGps, lapisAman, targetIkon, targetPanah, isiTarget;
        private Label spawn, kompas, gps, nama, jarak, kosong, gpsJudul, gpsIsi;
        private Button tangkap;
        private readonly List<RadarDisk.Node> nodes = new List<RadarDisk.Node>();
        private string shownIcon;
        private long shownSecs = -1;

        private void OnEnable()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            disk = root.Q<RadarDisk>("radar-disk");
            info = root.Q("radar-info");
            kartuTarget = root.Q("kartu-target");
            kartuTunggu = root.Q("kartu-tunggu");
            lapisGps = root.Q("lapis-gps");
            lapisAman = root.Q("lapis-aman");
            targetIkon = root.Q("target-ikon");
            targetPanah = root.Q("target-panah");
            isiTarget = targetIkon.parent;
            spawn = root.Q<Label>("pil-spawn");
            kompas = root.Q<Label>("info-kompas");
            gps = root.Q<Label>("info-gps");
            nama = root.Q<Label>("target-nama");
            jarak = root.Q<Label>("target-jarak");
            kosong = root.Q<Label>("target-kosong");
            gpsJudul = root.Q<Label>("gps-judul");
            gpsIsi = root.Q<Label>("gps-isi");
            tangkap = root.Q<Button>("btn-tangkap");

            root.Q<Button>("btn-kembali").clicked += () => controller.BackToLobby();
            tangkap.clicked += () => controller.CaptureSelected();
            root.Q<Button>("btn-coba-lagi").clicked += () => controller.Retry();
            root.Q<Button>("btn-latihan-gps").clicked += () => controller.PlayPractice();
            root.Q<Button>("btn-mengerti").clicked += () => controller.AcceptSafety();
            disk.Tapped = id => controller.Select(id);
        }

        private void Update()
        {
            var status = controller.Status;
            bool running = status == LocationFeed.FeedStatus.Running;
            bool failed = status == LocationFeed.FeedStatus.Denied || status == LocationFeed.FeedStatus.Disabled || status == LocationFeed.FeedStatus.Failed;

            long secs = controller.SecondsToNextSpawn;
            if (secs != shownSecs) { shownSecs = secs; spawn.text = RadarText.Countdown(secs); }

            Show(info, running);
            Show(kartuTarget, running);
            Show(kartuTunggu, !running && !failed);
            Show(lapisGps, failed && controller.SafetySeen);
            Show(lapisAman, !controller.SafetySeen);
            disk.EnableInClassList("radar__pudar", failed);

            if (failed)
            {
                var feed = status == LocationFeed.FeedStatus.Denied ? RadarText.Feed.Denied
                    : status == LocationFeed.FeedStatus.Disabled ? RadarText.Feed.Disabled : RadarText.Feed.Failed;
                gpsJudul.text = RadarText.FeedTitle(feed).ToUpperInvariant();
                gpsIsi.text = RadarText.FeedBody(feed);
            }

            nodes.Clear();
            if (running)
            {
                kompas.text = RadarText.CompassLine(controller.HasCompass, controller.CompassSource, controller.Heading);
                kompas.EnableInClassList("radar__info--awas", !controller.HasCompass);
                gps.text = RadarText.GpsLine(controller.AccuracyM, RadarController.WeakGpsM);
                gps.EnableInClassList("radar__info--awas", controller.AccuracyM > RadarController.WeakGpsM);
                controller.FillNodes(nodes);
                ShowTarget();
            }
            disk.Heading = controller.Heading;
            disk.Sweep = Time.unscaledTime * 90f % 360f;
            disk.SelectedId = controller.SelectedId;
            disk.Show(nodes, Time.unscaledTime);
        }

        private void ShowTarget()
        {
            bool has = controller.TryGetSelected(out var sel);
            Show(isiTarget, has);
            Show(tangkap, has);
            Show(kosong, !has);
            if (!has) return;
            if (shownIcon != sel.ResourceId)
            {
                if (shownIcon != null) targetIkon.RemoveFromClassList("eg-ikon--" + shownIcon);
                targetIkon.AddToClassList("eg-ikon--" + sel.ResourceId);
                shownIcon = sel.ResourceId;
            }
            nama.text = ResourceIds.Name(sel.ResourceId);
            jarak.text = RadarText.TargetLine(sel.Distance, sel.Bearing);
            tangkap.text = RadarText.CaptureButton(sel.Locked, sel.InRange, sel.Distance, out bool on);
            tangkap.SetEnabled(on);
            tangkap.EnableInClassList("eg-btn--utama", on);
            tangkap.EnableInClassList("eg-btn--nonaktif", !on);
            targetPanah.EnableInClassList("is-terkunci", sel.Locked);
            targetPanah.style.rotate = new Rotate(sel.Locked ? 0f : (float)(sel.Bearing - controller.Heading) - 90f);
            targetIkon.style.opacity = sel.Locked ? 0.55f : 1f;
        }

        private static void Show(VisualElement e, bool on)
        {
            e.style.display = on ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
