using UnityEngine;
using UnityEngine.InputSystem;
#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine.Android;
#endif

namespace EnerGo
{
    // Player position + facing for the radar. On the phone: GPS + compass, running only while this component is
    // enabled (battery). In the editor: a simulated walk, WASD to move and Q/E to turn.
    public class LocationFeed : MonoBehaviour
    {
        public enum FeedStatus { Starting, Running, Denied, Disabled, Failed }

        [Header("Editor simulation")]
        [SerializeField] private double simStartLat = -6.200000;  // change to the school's coordinates
        [SerializeField] private double simStartLon = 106.816666;
        [SerializeField] private float simWalkSpeed = 1.5f;       // m/s
        [SerializeField] private float simTurnSpeed = 90f;        // deg/s

        private const float StartTimeoutSec = 20f;

        public FeedStatus Status { get; private set; } = FeedStatus.Starting;
        public double Lat { get; private set; }
        public double Lon { get; private set; }
        public float AccuracyM { get; private set; }
        public float HeadingDeg { get; private set; }
        public bool HasCompass { get; private set; }

        private float startedAt;
        private double lastFixTime = -1;
        private bool waitingForPermission;

        private void OnEnable() => Restart();

        private void OnDisable()
        {
#if !UNITY_EDITOR
            Input.location.Stop();
            Input.compass.enabled = false;
#endif
        }

        public void Restart()
        {
            Status = FeedStatus.Starting;
            startedAt = Time.unscaledTime;
#if UNITY_EDITOR
            SimulateAt(simStartLat, simStartLon, HeadingDeg);
#else
  #if UNITY_ANDROID
            if (!Permission.HasUserAuthorizedPermission(Permission.FineLocation))
            {
                waitingForPermission = true;
                var callbacks = new PermissionCallbacks();
                callbacks.PermissionGranted += _ => { waitingForPermission = false; StartDevice(); };
                callbacks.PermissionDenied += _ => { waitingForPermission = false; Status = FeedStatus.Denied; };
                callbacks.PermissionDeniedAndDontAskAgain += _ => { waitingForPermission = false; Status = FeedStatus.Denied; };
                Permission.RequestUserPermission(Permission.FineLocation, callbacks);
                return;
            }
  #endif
            StartDevice();
#endif
        }

        // Editor/test helper: jump to a place and facing.
        public void SimulateAt(double lat, double lon, float headingDeg)
        {
            Lat = lat; Lon = lon; HeadingDeg = headingDeg; AccuracyM = 5f; HasCompass = true; Status = FeedStatus.Running;
        }

        private void StartDevice()
        {
            if (!Input.location.isEnabledByUser) { Status = FeedStatus.Disabled; return; }
            Input.compass.enabled = true;
            Input.location.Start(5f, 2f);
            startedAt = Time.unscaledTime;
            lastFixTime = -1;
            Status = FeedStatus.Starting;
        }

        private void Update()
        {
#if UNITY_EDITOR
            UpdateSimulation(Time.unscaledDeltaTime);
#else
            UpdateDevice();
#endif
        }

        private void UpdateDevice()
        {
            if (waitingForPermission || Status == FeedStatus.Denied || Status == FeedStatus.Disabled || Status == FeedStatus.Failed) return;
            var service = Input.location.status;
            if (service == LocationServiceStatus.Failed) { Status = FeedStatus.Failed; return; }
            if (service != LocationServiceStatus.Running)
            {
                if (Status == FeedStatus.Starting && Time.unscaledTime - startedAt > StartTimeoutSec) Status = FeedStatus.Failed;
                return;
            }

            var fix = Input.location.lastData;
            if (fix.timestamp != lastFixTime)
            {
                bool first = lastFixTime < 0;
                double moved = GeoSpawner.DistanceM(Lat, Lon, fix.latitude, fix.longitude);
                if (first || GeoSpawner.PlausibleMove(moved, fix.timestamp - lastFixTime))
                {
                    Lat = fix.latitude;
                    Lon = fix.longitude;
                    AccuracyM = fix.horizontalAccuracy;
                }
                lastFixTime = fix.timestamp;
                Status = FeedStatus.Running;
            }

            HasCompass = Input.compass.enabled && Input.compass.timestamp > 0;
            if (HasCompass) HeadingDeg = GeoSpawner.SmoothHeading(HeadingDeg, Input.compass.trueHeading, Time.unscaledDeltaTime);
        }

        private void UpdateSimulation(float dt)
        {
            var kb = Keyboard.current;
            if (kb == null || Status != FeedStatus.Running) return;
            float turn = (kb.eKey.isPressed ? 1f : 0f) - (kb.qKey.isPressed ? 1f : 0f);
            HeadingDeg = Mathf.Repeat(HeadingDeg + turn * simTurnSpeed * dt, 360f);
            float fwd = (kb.wKey.isPressed ? 1f : 0f) - (kb.sKey.isPressed ? 1f : 0f);
            float side = (kb.dKey.isPressed ? 1f : 0f) - (kb.aKey.isPressed ? 1f : 0f);
            if (fwd == 0f && side == 0f) return;
            float h = HeadingDeg * Mathf.Deg2Rad;
            // forward = (north cos h, east sin h); right = (north -sin h, east cos h)
            double north = (fwd * Mathf.Cos(h) - side * Mathf.Sin(h)) * simWalkSpeed * dt;
            double east = (fwd * Mathf.Sin(h) + side * Mathf.Cos(h)) * simWalkSpeed * dt;
            Lat += north / GeoSpawner.MetersPerDegree;
            Lon += east / (GeoSpawner.MetersPerDegree * System.Math.Cos(Lat * System.Math.PI / 180.0));
        }
    }
}
