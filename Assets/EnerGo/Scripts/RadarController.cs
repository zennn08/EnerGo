using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EnerGo
{
    public struct RadarNode { public string Id, ResourceId; public double Bearing, Distance; public bool InRange, Locked; }

    // Exploration screen: SDA around the player on a heading-up 3D map (RadarWorld3D). Walk within range of one and tap TANGKAP
    // to open its capture mini-game; the spawn id travels along so the reward also removes it from the radar.
    [RequireComponent(typeof(LocationFeed))]
    public class RadarController : MonoBehaviour
    {
        private const double RangeM = 150.0;
        private const float RefreshSec = 0.5f;
        public const float WeakGpsM = 30f;
        private const string PrefSafetySeen = "EnerGo.RadarSafetySeen";

        private LocationFeed feed;
        private List<GeoSpawner.Spawn> nearby = new List<GeoSpawner.Spawn>();
        private readonly GeoSpawner.RangeAlert rangeAlert = new GeoSpawner.RangeAlert(); // one buzz when a spawn comes into range
        private float nextRefresh;
        private string selectedId;
        private bool safetySeen;

        // State for RadarView.
        public LocationFeed.FeedStatus Status => feed.Status;
        public bool SafetySeen => safetySeen;
        public string SelectedId => selectedId;
        public float Heading => feed.HasCompass ? feed.HeadingDeg : 0f;
        public bool HasCompass => feed.HasCompass;
        public string CompassSource => feed.HeadingSource;
        public float AccuracyM => feed.AccuracyM;
        public double Lat => feed.Lat;
        public double Lon => feed.Lon;
        public RadarWorld3D World { get; private set; }
        public long SecondsToNextSpawn => GeoSpawner.SecondsToNextWindow(NowUtc());

        private void Awake()
        {
            feed = GetComponent<LocationFeed>();
            safetySeen = PlayerPrefs.GetInt(PrefSafetySeen, 0) == 1;
            World = gameObject.AddComponent<RadarWorld3D>();
            World.Init(this);
        }

        private static long NowUtc() => DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        private void Update()
        {
            if (feed.Status != LocationFeed.FeedStatus.Running || Time.unscaledTime < nextRefresh) return;
            nextRefresh = Time.unscaledTime + RefreshSec;
            Refresh();
        }

        // Public so editor checks can force a refresh (the editor does not tick in play mode).
        public void Refresh()
        {
            var captured = new HashSet<string>(EnerGoProgress.Current.capturedSpawns ?? new string[0]);
            nearby = GeoSpawner.Nearby(feed.Lat, feed.Lon, RangeM, NowUtc(), DateTime.Now.Hour, captured);
            if (nearby.FindIndex(s => s.Id == selectedId) < 0) selectedId = nearby.Count > 0 ? nearby[0].Id : null;
            var inRange = new List<string>();
            foreach (var s in nearby)
                if (ResourceIds.IsPlayable(s.ResourceId) && GeoSpawner.CanCapture(GeoSpawner.DistanceM(feed.Lat, feed.Lon, s.Lat, s.Lon), feed.AccuracyM))
                    inRange.Add(s.Id);
            if (rangeAlert.Update(inRange, Time.unscaledTime)) Haptics.Pulse(60);
        }

        public void FillNodes(List<RadarNode> into)
        {
            foreach (var s in nearby) into.Add(NodeFor(s));
        }

        public bool TryGetSelected(out RadarNode node)
        {
            int i = nearby.FindIndex(s => s.Id == selectedId);
            node = i >= 0 ? NodeFor(nearby[i]) : default;
            return i >= 0;
        }

        private RadarNode NodeFor(GeoSpawner.Spawn s)
        {
            double d = GeoSpawner.DistanceM(feed.Lat, feed.Lon, s.Lat, s.Lon);
            return new RadarNode
            {
                Id = s.Id,
                ResourceId = s.ResourceId,
                Bearing = GeoSpawner.BearingDeg(feed.Lat, feed.Lon, s.Lat, s.Lon),
                Distance = d,
                InRange = GeoSpawner.CanCapture(d, feed.AccuracyM),
                Locked = !ResourceIds.IsPlayable(s.ResourceId)
            };
        }

        // Actions for RadarView. The safety curtain blocks them all until it has been accepted.
        public void Select(string id) { if (safetySeen && id != null) selectedId = id; }

        public void BackToLobby()
        {
            if (!safetySeen) return;
            if (SFXManager.Instance != null) SFXManager.Instance.PlayButtonBack();
            SceneManager.LoadScene("LobbyScene");
        }

        public void CaptureSelected()
        {
            if (!safetySeen) return;
            int i = nearby.FindIndex(s => s.Id == selectedId);
            if (i < 0) return;
            var sel = nearby[i];
            if (!ResourceIds.IsPlayable(sel.ResourceId) || !GeoSpawner.CanCapture(GeoSpawner.DistanceM(feed.Lat, feed.Lon, sel.Lat, sel.Lon), feed.AccuracyM)) return;
            if (SFXManager.Instance != null) SFXManager.Instance.PlayButtonClick();
            ResourceIds.Selected = sel.ResourceId;
            ExploreSession.PendingSpawnId = sel.Id;
            ExploreSession.PendingBearingDeg = (float)GeoSpawner.BearingDeg(feed.Lat, feed.Lon, sel.Lat, sel.Lon);
            SceneManager.LoadScene(LobbyController.CaptureSceneName());
        }

        public void Retry() { if (safetySeen) feed.Restart(); }

        public void PlayPractice()
        {
            if (!safetySeen) return;
            LobbyController.OpenPracticeOnLoad = true;
            SceneManager.LoadScene("LobbyScene");
        }

        // One-time safety reminder before the first hunt.
        public void AcceptSafety()
        {
            safetySeen = true;
            PlayerPrefs.SetInt(PrefSafetySeen, 1);
            PlayerPrefs.Save();
        }
    }
}
