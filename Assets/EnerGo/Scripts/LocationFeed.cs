using UnityEngine;
using UnityEngine.InputSystem;
#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine.Android;
using UnityEngine.InputSystem.Android;
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
        public string HeadingSource { get; private set; } = "-"; // shown on the radar for device diagnosis

        // This project runs the new Input System only, where the legacy Input.compass stays silent on the phone,
        // so the heading comes from the Android rotation-vector sensor (compass + gyro + accelerometer fused).
        private AttitudeSensor rotationSensor;
        private double lastCompassStamp;
        private float lastHeadingAt = -10f;

        private float startedAt;
        private double lastSeenFixTime = -1;
        private FixFilter fixes = new FixFilter();
        private bool waitingForPermission;
        private volatile bool permissionGranted; // set from the Android permission callback, acted on in Update (main thread)

        private void OnEnable() => Restart();

        private void OnDisable()
        {
#if !UNITY_EDITOR
            Input.location.Stop();
            Input.compass.enabled = false;
            if (rotationSensor != null && rotationSensor.enabled) InputSystem.DisableDevice(rotationSensor);
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
                callbacks.PermissionGranted += _ => permissionGranted = true;
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
            Lat = lat; Lon = lon; HeadingDeg = headingDeg; AccuracyM = 5f; HasCompass = true; HeadingSource = "simulasi";
            Status = FeedStatus.Running;
        }

        private void StartDevice()
        {
            if (!Input.location.isEnabledByUser) { Status = FeedStatus.Disabled; return; }
            Input.compass.enabled = true;
#if UNITY_ANDROID && !UNITY_EDITOR
            rotationSensor = InputSystem.GetDevice<AndroidRotationVector>();
#endif
            if (rotationSensor != null && !rotationSensor.enabled) InputSystem.EnableDevice(rotationSensor);
            Input.location.Start(5f, 2f);
            startedAt = Time.unscaledTime;
            lastSeenFixTime = -1;
            fixes = new FixFilter();
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
            if (permissionGranted) { permissionGranted = false; waitingForPermission = false; StartDevice(); }
            if (waitingForPermission || Status == FeedStatus.Denied || Status == FeedStatus.Disabled || Status == FeedStatus.Failed) return;
            UpdateHeading(Time.unscaledDeltaTime);
            var service = Input.location.status;
            if (service == LocationServiceStatus.Failed) { Status = FeedStatus.Failed; return; }
            if (service != LocationServiceStatus.Running)
            {
                if (Status == FeedStatus.Starting && Time.unscaledTime - startedAt > StartTimeoutSec) Status = FeedStatus.Failed;
                return;
            }

            var fix = Input.location.lastData;
            if (fix.timestamp != lastSeenFixTime)
            {
                lastSeenFixTime = fix.timestamp;
                if (fixes.Offer(fix.latitude, fix.longitude, fix.timestamp, fix.horizontalAccuracy))
                {
                    Lat = fixes.Lat;
                    Lon = fixes.Lon;
                    AccuracyM = (float)fixes.Accuracy;
                    Status = FeedStatus.Running; // only once there is a real position to show
                }
            }
        }

        // Rotation-vector sensor first; the legacy compass only if it actually delivers new readings.
        private void UpdateHeading(float dt)
        {
            float? raw = null;
            if (rotationSensor != null && rotationSensor.enabled && rotationSensor.lastUpdateTime > 0)
            {
                raw = GeoSpawner.HeadingFromRotation(rotationSensor.attitude.ReadValue());
                HeadingSource = "sensor rotasi";
            }
            else if (Input.compass.enabled && Input.compass.timestamp > lastCompassStamp)
            {
                lastCompassStamp = Input.compass.timestamp;
                raw = Input.compass.trueHeading;
                HeadingSource = "kompas";
            }
            if (raw != null)
            {
                HeadingDeg = GeoSpawner.SmoothHeading(HeadingDeg, raw.Value, dt);
                lastHeadingAt = Time.unscaledTime;
            }
            HasCompass = Time.unscaledTime - lastHeadingAt < 2f; // no fresh reading for 2 s = treat as no compass
            if (!HasCompass) HeadingSource = "tidak ada";
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
