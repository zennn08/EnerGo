using UnityEngine;
using UnityEngine.InputSystem;
#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine.InputSystem.Android;
#endif

namespace EnerGo
{
    // Keeps a "North" transform whose +Z points to true north, by comparing the Android rotation-vector compass
    // (same sensor as the radar) with the AR camera's yaw. SDA targets parented to North stay at their real bearing.
    public class CompassAnchor : MonoBehaviour
    {
        public Camera cam;
        public Transform North { get; private set; }
        public float OffsetDeg { get; private set; }  // compass heading - camera yaw
        public bool HasCompass { get; private set; }
        public bool TargetPlaced;                     // set by the mini-game: hold the offset still while an SDA is out
        public bool ReadyToPlace => calibrated || Time.time - startedAt >= CompassMath.FirstFixTimeoutSec;
        public float HeadingDeg => cam == null ? 0f : CompassMath.CameraHeading(Yaw(cam.transform.forward), OffsetDeg);

        private AttitudeSensor rotationSensor;
        private bool calibrated;
        private float startedAt, lastReadingAt = -10f;
        private double lastSensorStamp;

        private static float Yaw(Vector3 f) => Mathf.Atan2(f.x, f.z) * Mathf.Rad2Deg;

        private void Awake()
        {
            North = new GameObject("North").transform;
            startedAt = Time.time;
        }

        private void OnEnable()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            rotationSensor = InputSystem.GetDevice<AndroidRotationVector>();
#endif
            if (rotationSensor != null && !rotationSensor.enabled) InputSystem.EnableDevice(rotationSensor);
        }

        private void OnDisable()
        {
            if (rotationSensor != null && rotationSensor.enabled) InputSystem.DisableDevice(rotationSensor);
        }

        private void Start()
        {
            if (cam != null) North.position = cam.transform.position; // rotation pivot = where the player stands
        }

        // LateUpdate: after ARCapturePrototype.Update has turned the camera this frame.
        private void LateUpdate()
        {
            if (cam == null) return;
            if (rotationSensor != null && rotationSensor.enabled && rotationSensor.lastUpdateTime > lastSensorStamp)
            {
                lastSensorStamp = rotationSensor.lastUpdateTime;
                Vector3 f = cam.transform.forward;
                float pitch = Mathf.Asin(Mathf.Clamp(f.y, -1f, 1f)) * Mathf.Rad2Deg;
                if (CompassMath.ShouldCalibrate(pitch, calibrated, TargetPlaced))
                {
                    float heading = CompassMath.BackCameraHeading(rotationSensor.attitude.ReadValue());
                    float target = CompassMath.OffsetTarget(heading, Yaw(f));
                    OffsetDeg = calibrated ? CompassMath.SmoothOffset(OffsetDeg, target, Time.deltaTime) : target; // first fix: snap
                    calibrated = true;
                }
                lastReadingAt = Time.time;
            }
            HasCompass = Time.time - lastReadingAt < 2f;
            North.rotation = Quaternion.Euler(0f, -OffsetDeg, 0f);
        }

        // World direction of a compass bearing, tilted up by pitchDeg.
        public Vector3 Direction(float bearingDeg, float pitchDeg) =>
            Quaternion.Euler(-pitchDeg, CompassMath.WorldYaw(bearingDeg, OffsetDeg), 0f) * Vector3.forward;
    }
}
