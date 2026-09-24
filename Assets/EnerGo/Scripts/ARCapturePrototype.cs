using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.XR.ARFoundation;
using EnhancedTouch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace EnerGo
{
    public class ARCapturePrototype : MonoBehaviour
    {
        public enum TrackingMode
        {
            Auto,
            ARCore,
            Gyro360
        }

        [Header("Configuration")]
        public Camera arCamera;
        public Material energyMaterial;
        public TrackingMode currentMode = TrackingMode.Auto;

        // Runtime state
        private bool isUsingGyro = false;
        private AttitudeSensor attitudeSensor;
        private UnityEngine.InputSystem.Gyroscope angularGyro;
        private float modeInitTimer = 0f;
        private const float ArCheckTimeout = 2.0f;

        // Real camera feed behind the gyro scene (Sensor 360 has no ARCore camera background).
        private WebCamTexture cameraFeed;
        private Transform feedQuad;
        private Material feedMaterial;
        private bool feedPermissionAsked;

        // Target & Spawning
        private GameObject target;
        private GameObject goldModel;
        private GameObject sunModel;   // Resources/Sun/sun; falls back to a yellow sphere while it has no mesh
        private Material goldMaterial;
        private string resourceId;
        private float resultShownAt;
        private int touches = 0;
        private float nextSpawnTime = 0f;

        // Sun "Lensa" mini-game: keep the drifting light inside the fixed centre reticle until the bar fills.
        // Tuning knobs, roughly GDD ★1 (slow, wide window). Retune after trying on a phone.
        private const float SunFillPerSec = 1f / 8f;    // ~8 s when held inside the whole time
        private const float SunDrainPerSec = 1f / 16f;  // escaping drains at half the fill speed
        private const float SunDriftYawDeg = 38f;       // max wander left/right of the spawn direction
        private const float SunDriftPitchDeg = 14f;     // max wander up/down around SunBasePitchDeg
        private const float SunBasePitchDeg = 12f;      // sits a little above the horizon
        private const float SunDriftSpeed = 0.22f;      // noise speed: higher = faster, jumpier light
        private const float SunDistance = 1.6f;
        private const float ReticleRadius = 56f;        // GUI units, same scale as the HUD
        private float sunProgress;
        private bool sunInside;
        private float sunBaseYaw;
        private float sunSeed;
        private float sunSpawnTime;

        // Coal "Pemicu" mini-game: 3-5 lumps in an arc in front; each needs 2-5 hits (bigger = more)
        // and steps through the four break-stage models (Resources/Coal/bara_1..4) before shattering.
        private const int CoalMinLumps = 3, CoalMaxLumps = 5;
        private const int CoalMinHits = 2, CoalMaxHits = 5;
        private const float CoalArcDeg = 60f;           // lumps spread across ±60° of the spawn direction
        private const float FinishDelay = 0.6f;         // let the last break / gold reveal play before the quiz opens
        private static readonly float[] CoalStageFit = { 1f, 0.95f, 0.9f, 0.8f }; // stage models are flatter piles
        private class CoalLump
        {
            public Transform root;
            public GameObject visual;
            public int hits, required, stage = -1;
            public float size, shakeUntil;
        }
        private readonly List<CoalLump> coalLumps = new List<CoalLump>();
        private GameObject[] coalStageModels;
        private Light coalLight;
        private int coalTotal;
        private float finishAt = -1f; // shared by coal and gold: when to open the quiz

        // Gold "Stabilizer" mini-game (mendulang): tap the deposit to scoop sand, then swirl the phone's tilt
        // in slow circles (accelerometer) so the sand washes over the rim and the gold flakes stay behind.
        // Tuning knobs; tilt is in units of g (≈ sin of the tilt angle), jolt is |accel|/gravity - 1.
        private const float GoldFillPerSec = 1f / 10f;   // ~10 s of good swirling
        private const float GoldSpillPerSec = 1f / 6f;   // too rough: gold spills back out
        private const float GoldMinTilt = 0.08f;         // below this the pan is just being held level
        private const float GoldMaxTilt = 0.6f;          // tipping the pan further than this spills
        private const float GoldMinSwirl = 40f;          // deg/s of the tilt direction that counts as swirling
        private const float GoldMaxSwirl = 420f;         // faster than this is sloshing, not panning
        private const float GoldMaxJolt = 0.5f;          // a shake/jerk stronger than this spills
        private enum GoldPhase { None, Leveling, Countdown, Panning, Done, Failed }
        private const float GoldFlatDeg = 12f;           // "datar": screen facing up within this many degrees
        private const float GoldFlatHoldSec = 1f;        // hold it level this long before the countdown
        private const float GoldCountdownSec = 3f;       // 3-2-1, then "MULAI!"
        private const int GoldPieces = 18;               // gold flakes and nuggets in the pan
        private const float GoldFailBelow = 0.4f;        // fewer than 40% of the gold left = thrown away, fail
        private const float GoldSpillEverySec = 0.35f;   // while too rough, one piece flies out this often
        private const int GoldSandGrains = 40;
        private const float GoldSloshGain = 2.4f;        // how far sand/water visibly slide per unit of tilt
        private GoldPhase goldPhase;
        private bool goldPanning => goldPhase != GoldPhase.None; // the pan is on screen
        private float goldPhaseAt;
        private float goldFlatHold;
        private float goldSpillTimer;
        private bool goldRespawnFar;   // after a failure the next deposit spawns somewhere else around the player
        private float goldProgress;
        private bool goldRough;
        private float goldSwirl;       // smoothed signed deg/s of the tilt direction
        private float goldTiltMag;
        private float goldLastAngle = float.NaN;
        private Vector2 goldTilt;      // smoothed, drives the sloshing grains
        private Vector2 goldBubble;    // spirit-level bubble offset (screen space, units of g) while levelling
        private Vector3 goldBaseline;  // gravity direction the player holds as "level"
        private float goldGravity;     // accelerometer magnitude at rest (units differ per platform)
        private Vector2[] goldSand;    // sand grain offsets inside the pan, unit circle
        // Gold pieces, in pan units (1 = inner rim), screen-oriented (y down).
        private Vector2[] goldPos, goldVel, goldHome, goldLostDir;
        private float[] goldSize, goldLostAt; // goldLostAt: when the piece flew out (for the fly-out animation)
        private bool[] goldLost;

        // Smoothed signed angular speed (deg/s) of the tilt direction. Steady one-way circles give a
        // large value; rocking back and forth flips the sign every swing and averages out near zero.
        public static float StepSwirl(float swirl, ref float lastAngle, Vector2 tilt, float dt)
        {
            float k = 1f - Mathf.Exp(-6f * dt);
            if (tilt.magnitude < GoldMinTilt * 0.5f) { lastAngle = float.NaN; return Mathf.Lerp(swirl, 0f, k); }
            float angle = Mathf.Atan2(tilt.y, tilt.x) * Mathf.Rad2Deg;
            if (!float.IsNaN(lastAngle)) swirl = Mathf.Lerp(swirl, Mathf.DeltaAngle(lastAngle, angle) / dt, k);
            lastAngle = angle;
            return swirl;
        }

        // One step of the panning bar. Returns the new progress; `rough` says the motion spilled gold.
        public static float StepGoldProgress(float progress, float swirlDegPerSec, float tiltMag, float jolt, float dt, out bool rough)
        {
            float speed = Mathf.Abs(swirlDegPerSec);
            rough = speed > GoldMaxSwirl || tiltMag > GoldMaxTilt || jolt > GoldMaxJolt;
            if (rough) return Mathf.Clamp01(progress - GoldSpillPerSec * dt);
            if (speed >= GoldMinSwirl && tiltMag >= GoldMinTilt) return Mathf.Clamp01(progress + GoldFillPerSec * dt);
            return progress; // held still or barely moving: nothing washes out
        }

        // Break-stage model to show after `hits` of `required` hits: 0 = intact lump, then spread over
        // medium (1), granules (2) and powder (3). At `required` hits the lump is gone.
        public static int CoalStage(int hits, int required) =>
            hits <= 0 ? 0 : 1 + (hits - 1) * 3 / Mathf.Max(1, required - 1);

        // Gyro & Manual Pan (for Editor / Non-Gyro Drag)
        private float lookYaw = 0f;
        private float lookPitch = 0f;
        private Vector2 previousTouchPos;
        private bool isDraggingLook = false;

        // ─── Swipe Card State ───────────────────────────────────────
        private Vector2  cardOffset      = Vector2.zero;   // current drag displacement (scaled)
        private Vector2  cardVelocity    = Vector2.zero;   // momentum for flick
        private bool     isDraggingCard  = false;          // true while finger is on card
        private Vector2  cardDragStart   = Vector2.zero;   // screen-space drag start
        private bool     cardFlicking    = false;          // animating off-screen
        private bool     cardReturning   = false;          // snapping back to center
        private const float SwipeThreshold   = 90f;        // pixels (scaled) to commit
        private const float FlickSpeed       = 2800f;      // pixels/sec exit speed
        private const float ReturnSmoothing  = 12f;        // lerp speed for snap-back
        private GUIStyle styleSwipeBenar;
        private GUIStyle styleSwipeSalah;
        private GUIStyle styleCardStatement;
        private GUIStyle styleCardHint;
        private bool cardStylesInit = false;

        // Visual Feedback
        private string feedback = "Memulai inisialisasi sensor spasial...";
        private float lastHitTime = -10f;
        private Vector2 lastHitScreenPos;

        // UI Textures
        private Texture2D texWhite;
        private Texture2D texPill;

        // Styles
        private GUIStyle styleHeaderTag;
        private GUIStyle styleHeaderVal;
        private GUIStyle styleGoldVal;
        private GUIStyle styleRadarAlert;
        private GUIStyle styleRadarLocked;
        private GUIStyle styleTapsCounter;
        private GUIStyle styleBtnText;
        private GUIStyle styleBtnWarn;
        private GUIStyle styleStatusSub;
        private bool stylesInitialized = false;

        private float HudScale => Mathf.Max(1f, Mathf.Min(Screen.width / 480f, Screen.height / 800f));

        private void OnEnable()
        {
            EnhancedTouchSupport.Enable();
            EnableMotionSensors();
        }

        private void OnDisable()
        {
            EnhancedTouchSupport.Disable();
        }

        private void EnableMotionSensors()
        {
            attitudeSensor = AttitudeSensor.current;
            angularGyro = UnityEngine.InputSystem.Gyroscope.current;
            if (attitudeSensor != null && !attitudeSensor.enabled) InputSystem.EnableDevice(attitudeSensor);
            if (angularGyro != null && !angularGyro.enabled) InputSystem.EnableDevice(angularGyro);
            if (Accelerometer.current != null && !Accelerometer.current.enabled) InputSystem.EnableDevice(Accelerometer.current); // gold panning
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused && EnerGoProgress.Current != null) EnerGoProgress.Commit(EnerGoProgress.Current);
            if (cameraFeed != null) { if (paused) cameraFeed.Pause(); else cameraFeed.Play(); }
        }

        private void Awake()
        {
            Application.targetFrameRate = 60;
            var save = EnerGoProgress.Current;
            resourceId = save.pendingCapture != null ? save.pendingCapture.resourceId : ResourceIds.Selected;
            if (resourceId != ResourceIds.Sun && resourceId != ResourceIds.Coal && resourceId != ResourceIds.Gold) resourceId = ResourceIds.Gold;
            if (save.pendingCapture != null && save.pendingCapture.state != "awaiting-analysis") resultShownAt = Time.time;

            LoadTextures();
            CreateSolidTextures();

            // Set camera background fallback color
            if (arCamera != null)
            {
                arCamera.backgroundColor = new Color(0.04f, 0.08f, 0.12f, 1f);
            }
        }

        private void Start()
        {
            goldModel = Resources.Load<GameObject>("Gold/GoldModel");
            if (goldModel == null && resourceId == ResourceIds.Gold)
            {
                feedback = "Aset model emas belum tersedia di Resources.";
                Debug.LogError("EnerGo: missing Resources/Gold/GoldModel.");
                return;
            }

            var shader = energyMaterial != null ? energyMaterial.shader :
                (Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Texture") ?? Shader.Find("Standard"));

            if (energyMaterial != null)
                goldMaterial = new Material(energyMaterial) { name = "Gold runtime material" };
            else if (shader != null)
                goldMaterial = new Material(shader) { name = "Gold runtime material" };

            var texture = Resources.Load<Texture2D>("Gold/texture");
            if (texture != null && goldMaterial != null)
            {
                texture.filterMode = FilterMode.Point;
                goldMaterial.SetTexture("_BaseMap", texture);
                if (goldMaterial.HasProperty("_MainTex")) goldMaterial.SetTexture("_MainTex", texture);
                goldMaterial.SetColor("_BaseColor", Color.white);
                if (goldMaterial.HasProperty("_Color")) goldMaterial.SetColor("_Color", Color.white);
            }

            // Gold is panned at a river: quiet ambience for the whole gold capture scene.
            if (resourceId == ResourceIds.Gold && SFXManager.Instance != null) SFXManager.Instance.PlaySungai();

            // Initial detection
            DecideInitialMode();
        }

        private void DecideInitialMode()
        {
#if UNITY_EDITOR
            // In Unity Editor, default directly to Gyro360 / Manual Pan
            ApplyTrackingModeState(true);
            feedback = "MODE DRAG 360 AKTIF (EDITOR)";
#else
            if (currentMode == TrackingMode.Gyro360)
            {
                ApplyTrackingModeState(true);
                feedback = "MODE SENSOR 360 AKTIF";
            }
            else if (currentMode == TrackingMode.ARCore)
            {
                ApplyTrackingModeState(false);
                feedback = "MEMERIKSA SENSOR ARCORE...";
            }
            else
            {
                ApplyTrackingModeState(false);
            }
#endif
        }

        private void ApplyTrackingModeState(bool usingGyro)
        {
            isUsingGyro = usingGyro;
            if (!usingGyro) StopCameraFeed(); // ARCore needs exclusive access to the camera.
            var session = FindFirstObjectByType<ARSession>();
            if (session != null) session.enabled = !usingGyro;
            if (arCamera != null)
            {
                var tpd = arCamera.GetComponent<UnityEngine.InputSystem.XR.TrackedPoseDriver>();
                if (tpd != null) tpd.enabled = !usingGyro;

                var bg = arCamera.GetComponent<UnityEngine.XR.ARFoundation.ARCameraBackground>();
                if (bg != null) bg.enabled = !usingGyro;
                var cameraManager = arCamera.GetComponent<ARCameraManager>();
                if (cameraManager != null) cameraManager.enabled = !usingGyro;

                if (usingGyro)
                {
                    arCamera.clearFlags = CameraClearFlags.SolidColor;
                    arCamera.backgroundColor = new Color(0.04f, 0.08f, 0.12f, 1f);
                }
                else
                {
                    arCamera.clearFlags = CameraClearFlags.Depth;
                }
            }
        }

        private void LoadTextures()
        {
            texPill = Resources.Load<Texture2D>("UI/pill_badge");
        }

        private void CreateSolidTextures()
        {
            if (texWhite == null)
            {
                texWhite = new Texture2D(2, 2);
                texWhite.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white });
                texWhite.Apply();
            }
        }

        private void OnDestroy()
        {
            if (target != null) Destroy(target);
            if (goldMaterial != null) Destroy(goldMaterial);
            if (texWhite != null) Destroy(texWhite);
            if (SFXManager.Instance != null) // manager outlives the scene
            {
                SFXManager.Instance.StopMatahari();
                SFXManager.Instance.StopAliranAir();
                SFXManager.Instance.StopSungai();
            }
            if (coalLight != null) Destroy(coalLight.gameObject);
            StopCameraFeed();
        }

        private void UpdateCameraFeed()
        {
            if (cameraFeed == null && !StartCameraFeed()) return;
            if (cameraFeed.width < 32) return; // Android reports 16x16 until the first frame arrives.

            // Fit the rotated camera image to cover the whole view, far behind every spawned target.
            float angle = cameraFeed.videoRotationAngle;
            bool sideways = Mathf.Abs(angle % 180f) > 45f;
            float texAspect = sideways ? (float)cameraFeed.height / cameraFeed.width : (float)cameraFeed.width / cameraFeed.height;
            float dist = arCamera.farClipPlane * 0.9f;
            float viewH = 2f * dist * Mathf.Tan(arCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
            float h = Mathf.Max(viewH, viewH * arCamera.aspect / texAspect);
            float w = h * texAspect;
            feedQuad.localPosition = new Vector3(0f, 0f, dist);
            feedQuad.localRotation = Quaternion.Euler(0f, 0f, -angle);
            feedQuad.localScale = sideways ? new Vector3(h, w, 1f) : new Vector3(w, h, 1f);
            // Flip via UVs, not negative scale, so the quad is not back-face culled.
            bool flip = cameraFeed.videoVerticallyMirrored;
            feedMaterial.SetTextureScale("_BaseMap", new Vector2(1f, flip ? -1f : 1f));
            feedMaterial.SetTextureOffset("_BaseMap", new Vector2(0f, flip ? 1f : 0f));
            feedQuad.gameObject.SetActive(true);
        }

        private bool StartCameraFeed()
        {
            if (arCamera == null || energyMaterial == null) return false;
#if UNITY_ANDROID && !UNITY_EDITOR
            if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Camera))
            {
                // Ask once; if the player declines, the 360 scene keeps its solid background.
                if (!feedPermissionAsked) UnityEngine.Android.Permission.RequestUserPermission(UnityEngine.Android.Permission.Camera);
                feedPermissionAsked = true;
                return false;
            }
#endif
            string device = null;
            foreach (var d in WebCamTexture.devices)
                if (!d.isFrontFacing) { device = d.name; break; }
            if (device == null) return false;

            cameraFeed = new WebCamTexture(device, 1280, 720, 30);
            cameraFeed.Play();
            // energyMaterial is URP/Unlit and already referenced by the scene, so the shader ships in the build.
            feedMaterial = new Material(energyMaterial) { name = "Camera feed" };
            feedMaterial.SetTexture("_BaseMap", cameraFeed);
            feedMaterial.SetColor("_BaseColor", Color.white);
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "Camera feed";
            Destroy(quad.GetComponent<Collider>());
            quad.GetComponent<Renderer>().sharedMaterial = feedMaterial;
            quad.SetActive(false);
            feedQuad = quad.transform;
            feedQuad.SetParent(arCamera.transform, false);
            return true;
        }

        private void StopCameraFeed()
        {
            if (cameraFeed != null) { cameraFeed.Stop(); Destroy(cameraFeed); cameraFeed = null; }
            if (feedQuad != null) { Destroy(feedQuad.gameObject); feedQuad = null; }
            if (feedMaterial != null) { Destroy(feedMaterial); feedMaterial = null; }
        }

        private void Update()
        {
            if (isUsingGyro) UpdateCameraFeed();

            // 1. Auto-Fallback Evaluation
            if (currentMode == TrackingMode.Auto && !isUsingGyro)
            {
                modeInitTimer += Time.deltaTime;
                bool isArReady = ARSession.state == ARSessionState.SessionTracking;
                bool isArUnsupported = ARSession.state == ARSessionState.Unsupported || ARSession.state == ARSessionState.NeedsInstall;

                if (isArUnsupported || (modeInitTimer >= ArCheckTimeout && !isArReady))
                {
                    ApplyTrackingModeState(true);
                    feedback = "SENSOR 360 AKTIF (ARCORE TIDAK TERSEDIA)";
                }
            }

            // 2. Camera Orientation handling in Gyro Mode
            if (isUsingGyro && arCamera != null)
            {
                UpdateGyroCamera();
            }

            // 3. Target Spawning
            bool canSpawn = (isUsingGyro || ARSession.state == ARSessionState.SessionTracking);
            if (EnerGoProgress.Current.pendingCapture == null && arCamera != null && canSpawn && target == null && Time.time >= nextSpawnTime)
            {
                // Place the first target in view so sensor-mode users see an immediate response.
                // A failed gold pan hides the next deposit somewhere around the player instead of in front.
                SpawnGold(isRandomAngle: goldRespawnFar);
            }

            // 4. Idle Target Animation
            if (target != null)
            {
                // The coal field is a container at the origin; spinning it would orbit every lump.
                // The flat sun model is billboarded in UpdateSun instead: spinning it on Y would hide its rays edge-on.
                if (resourceId == ResourceIds.Gold) target.transform.Rotate(0f, 28f * Time.deltaTime, 0f, Space.Self);
                if (resourceId == ResourceIds.Sun && EnerGoProgress.Current.pendingCapture == null) UpdateSun();
                if (resourceId == ResourceIds.Coal) UpdateCoal();
                if (goldPanning && EnerGoProgress.Current.pendingCapture == null) UpdateGoldPan();
            }

            if (finishAt > 0f && Time.time >= finishAt)
            {
                finishAt = -1f;
                if (!CompleteCapture()) finishAt = Time.time + 1f; // retry; the error is shown in the HUD
            }

            // 5. Input Handling (Touch & Mouse)
            HandleInputs();
        }

        private void UpdateGyroCamera()
        {
            // Hardware Gyroscope (on mobile devices)
            if (attitudeSensor == null && angularGyro == null) EnableMotionSensors();
            if (attitudeSensor != null && attitudeSensor.enabled)
            {
                Quaternion attitude = attitudeSensor.attitude.ReadValue();
                Quaternion convertedAttitude = new Quaternion(attitude.x, attitude.y, -attitude.z, -attitude.w);
                Quaternion portraitCorrection = Quaternion.Euler(90f, 0f, 0f);
                Quaternion gyroRot = portraitCorrection * convertedAttitude;

                // Combine gyro with manual pan offset
                Quaternion manualRot = Quaternion.Euler(lookPitch, lookYaw, 0f);
                arCamera.transform.localRotation = Quaternion.Slerp(arCamera.transform.localRotation, manualRot * gyroRot, 0.2f);
            }
            else
            {
                if (angularGyro != null && angularGyro.enabled)
                {
                    Vector3 velocity = angularGyro.angularVelocity.ReadValue() * Mathf.Rad2Deg * Time.deltaTime;
                    lookYaw += velocity.y;
                    lookPitch = Mathf.Clamp(lookPitch - velocity.x, -60f, 60f);
                }
                // Keyboard / Arrow keys for easy testing
#if UNITY_EDITOR
                if (Keyboard.current != null)
                {
                    float speed = 65f * Time.deltaTime;
                    if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) lookYaw -= speed;
                    if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) lookYaw += speed;
                    if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) lookPitch -= speed;
                    if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) lookPitch += speed;
                    lookPitch = Mathf.Clamp(lookPitch, -60f, 60f);
                }
#endif
                arCamera.transform.localRotation = Quaternion.Euler(lookPitch, lookYaw, 0f);
            }
        }

        private void HandleInputs()
        {
            // ── When quiz card is showing, route input to card swipe ──
            if (EnerGoProgress.Current.pendingCapture != null
                && EnerGoProgress.Current.pendingCapture.state == "awaiting-analysis")
            {
                HandleCardSwipeInput();
                return;
            }

            if (EnerGoProgress.Current.pendingCapture != null) return;

            // Touch inputs (AR / Gyro look + tap-to-collect)
            foreach (var touch in EnhancedTouch.activeTouches)
            {
                if (touch.phase == UnityEngine.InputSystem.TouchPhase.Began)
                {
                    previousTouchPos = touch.screenPosition;
                    bool hitTarget = CheckTapOnTarget(touch.screenPosition);
                    isDraggingLook = !hitTarget;
                }
                else if (touch.phase == UnityEngine.InputSystem.TouchPhase.Moved && isDraggingLook && isUsingGyro && !goldPanning)
                {
                    Vector2 delta = touch.screenPosition - previousTouchPos;
                    previousTouchPos = touch.screenPosition;
                    lookYaw += delta.x * 0.12f;
                    lookPitch -= delta.y * 0.12f;
                    lookPitch = Mathf.Clamp(lookPitch, -60f, 60f);
                }
                else if (touch.phase == UnityEngine.InputSystem.TouchPhase.Ended)
                {
                    isDraggingLook = false;
                }
            }

#if UNITY_EDITOR
            if (Mouse.current != null)
            {
                if (Mouse.current.leftButton.wasPressedThisFrame)
                {
                    Vector2 mPos = Mouse.current.position.ReadValue();
                    CheckTapOnTarget(mPos);
                }
                if (Mouse.current.rightButton.isPressed && isUsingGyro)
                {
                    Vector2 delta = Mouse.current.delta.ReadValue();
                    lookYaw += delta.x * 0.18f;
                    lookPitch -= delta.y * 0.18f;
                    lookPitch = Mathf.Clamp(lookPitch, -60f, 60f);
                }
            }
#endif
        }

        // ─── Card Swipe Input (called instead of HandleInputs during quiz) ────
        private void HandleCardSwipeInput()
        {
            if (cardFlicking) return; // ignore input while card is animating off

            float scale = HudScale;

            // ── TOUCH ──
            foreach (var touch in EnhancedTouch.activeTouches)
            {
                if (touch.phase == UnityEngine.InputSystem.TouchPhase.Began && !isDraggingCard)
                {
                    isDraggingCard  = true;
                    cardReturning   = false;
                    cardDragStart   = touch.screenPosition;
                    cardVelocity    = Vector2.zero;
                }
                else if (touch.phase == UnityEngine.InputSystem.TouchPhase.Moved && isDraggingCard)
                {
                    Vector2 raw = touch.screenPosition - cardDragStart;
                    cardOffset   = new Vector2(raw.x / scale, -raw.y / scale);
                    cardVelocity = new Vector2(
                        (touch.screenPosition.x - previousTouchPos.x) / scale / Time.deltaTime,
                        -(touch.screenPosition.y - previousTouchPos.y) / scale / Time.deltaTime);
                    previousTouchPos = touch.screenPosition;
                }
                else if (touch.phase == UnityEngine.InputSystem.TouchPhase.Ended && isDraggingCard)
                {
                    isDraggingCard = false;
                    CommitOrReturnCard();
                }
            }

#if UNITY_EDITOR
            if (Mouse.current != null)
            {
                if (Mouse.current.leftButton.wasPressedThisFrame && !isDraggingCard)
                {
                    isDraggingCard  = true;
                    cardReturning   = false;
                    cardDragStart   = Mouse.current.position.ReadValue();
                    cardVelocity    = Vector2.zero;
                    previousTouchPos = cardDragStart;
                }
                else if (Mouse.current.leftButton.isPressed && isDraggingCard)
                {
                    Vector2 mPos = Mouse.current.position.ReadValue();
                    Vector2 raw  = mPos - cardDragStart;
                    cardOffset   = new Vector2(raw.x / scale, -raw.y / scale);
                    cardVelocity = new Vector2(
                        Mouse.current.delta.ReadValue().x / scale / Time.deltaTime,
                        -Mouse.current.delta.ReadValue().y / scale / Time.deltaTime);
                    previousTouchPos = mPos;
                }
                else if (Mouse.current.leftButton.wasReleasedThisFrame && isDraggingCard)
                {
                    isDraggingCard = false;
                    CommitOrReturnCard();
                }
            }
#endif

            // ── Animate flick / return ──
            if (cardFlicking)
            {
                float exitDir = cardOffset.x >= 0f ? 1f : -1f;
                cardOffset += new Vector2(exitDir * FlickSpeed * Time.deltaTime,
                                          cardVelocity.y * Time.deltaTime * 0.4f);
            }
            else if (cardReturning)
            {
                cardOffset = Vector2.Lerp(cardOffset, Vector2.zero, ReturnSmoothing * Time.deltaTime);
                if (cardOffset.magnitude < 1f) { cardOffset = Vector2.zero; cardReturning = false; }
            }
        }

        private void CommitOrReturnCard()
        {
            if (Mathf.Abs(cardOffset.x) >= SwipeThreshold)
            {
                // Commit answer: slide kanan = true (BENAR), swipe kiri = false (SALAH)
                bool answer = cardOffset.x > 0f;
                cardFlicking = true;
                // Slight delay via coroutine is not available in OnGUI; we'll
                // commit immediately and let the flick animation play for one frame.
                if (EnerGoProgress.Answer(answer))
                {
                    var session = EnerGoProgress.Current.pendingCapture;
                    if (SFXManager.Instance != null && session != null)
                    {
                        if (session.correct)
                            SFXManager.Instance.PlayQuizBenar();
                        else
                            SFXManager.Instance.PlayQuizSalah();
                    }
                    resultShownAt = Time.time;
                }
            }
            else
            {
                cardReturning = true;
            }
        }

        // Drift the sun around its spawn direction and fill/drain the bar depending on the reticle.
        private void UpdateSun()
        {
            if (arCamera == null) return;
            float t = (Time.time - sunSpawnTime) * SunDriftSpeed;
            float ramp = Mathf.Clamp01((Time.time - sunSpawnTime) / 1.5f); // start centred, then begin to wander
            float yaw = Noise(sunSeed, t) * SunDriftYawDeg * ramp;
            float pitch = SunBasePitchDeg + Noise(sunSeed + 31.7f, t) * SunDriftPitchDeg * ramp;
            // Follows the camera's position (not rotation) so the light feels far away, like the real sky.
            Vector3 dir = Quaternion.Euler(-pitch, sunBaseYaw + yaw, 0f) * Vector3.forward;
            target.transform.position = arCamera.transform.position + dir * SunDistance;
            // Always face the player and turn slowly in place like a wheel, so the rays stay visible.
            target.transform.rotation = Quaternion.LookRotation(dir, arCamera.transform.up) * Quaternion.Euler(0f, 0f, (Time.time - sunSpawnTime) * 20f);

            Vector3 sp = arCamera.WorldToScreenPoint(target.transform.position);
            Vector2 centre = new Vector2(Screen.width, Screen.height) * 0.5f;
            sunInside = sp.z > arCamera.nearClipPlane && Vector2.Distance(sp, centre) <= ReticleRadius * HudScale;
            sunProgress = StepSunProgress(sunProgress, sunInside, Time.deltaTime);

            // Whoosh loop: loud while the light is getting away, soft once it is locked,
            // panned toward the sun so it also hints where to turn.
            if (SFXManager.Instance != null)
            {
                SFXManager.Instance.PlayMatahari();
                float pan = Mathf.Clamp(arCamera.transform.InverseTransformDirection(dir).x * 1.5f, -1f, 1f);
                SFXManager.Instance.SetMatahariMix(sunInside ? 0.3f : 0.9f, pan);
            }

            if (sunProgress >= 1f && !CompleteCapture()) sunProgress = 0.95f;
        }

        public static float StepSunProgress(float progress, bool inside, float dt) =>
            Mathf.Clamp01(progress + (inside ? SunFillPerSec : -SunDrainPerSec) * dt);

        // Perlin noise mapped to roughly [-1, 1].
        private static float Noise(float seed, float t) => Mathf.Clamp((Mathf.PerlinNoise(seed, t) - 0.5f) * 2.6f, -1f, 1f);

        // Shared finish for every mini-game: open the quiz, or report why it could not.
        private bool CompleteCapture()
        {
            if (EnerGoProgress.BeginAnalysis(resourceId))
            {
                if (SFXManager.Instance != null) { SFXManager.Instance.StopMatahari(); SFXManager.Instance.StopAliranAir(); }
                goldPhase = GoldPhase.None;
                Destroy(target);
                target = null;
                feedback = "ANALISIS SIAP";
                return true;
            }
            feedback = EnerGoProgress.Error ?? "Gagal membuka analisis. Coba lagi.";
            return false;
        }

        private bool CheckTapOnTarget(Vector2 screenPosition)
        {
            if (EnerGoProgress.Current.pendingCapture != null) return false;
            if (resourceId == ResourceIds.Sun) return false; // sun is caught by aiming, so touches only drag the view
            if (goldPanning) return false;                    // the pan is controlled by tilting, not tapping
            touches++;

            // Ignore taps on UI top or bottom regions
            float scale = HudScale;
            float guiY = (Screen.height - screenPosition.y) / scale;
            if (guiY < 128f || guiY > (Screen.height / scale - 70f))
            {
                return false;
            }

            if (target == null || arCamera == null)
            {
                feedback = "MENUNGGU TARGET TERDETEKSI...";
                return false;
            }

            if (resourceId == ResourceIds.Coal) return HitCoal(screenPosition);

            if (!TryGetBounds(target, out var bounds)) return false;

            Vector3 center = arCamera.WorldToScreenPoint(bounds.center);
            if (center.z <= arCamera.nearClipPlane || !arCamera.pixelRect.Contains((Vector2)center))
            {
                feedback = "TARGET DI LUAR JANGKAUAN // IKUTI ARAH RADAR";
                return false;
            }

            // Only gold reaches here (sun aims, coal has its own lumps): one tap scoops the deposit into the pan.
            if (ScreenHit(target, screenPosition, out _))
            {
                lastHitTime = Time.time;
                lastHitScreenPos = screenPosition;
                StartGoldPan();
                return true;
            }

            return false;
        }

        private void SpawnCoalField()
        {
            if (coalStageModels == null)
            {
                coalStageModels = new GameObject[4];
                for (int i = 0; i < 4; i++) coalStageModels[i] = Resources.Load<GameObject>("Coal/bara_" + (i + 1));
            }
            if (System.Array.IndexOf(coalStageModels, null) >= 0)
            {
                nextSpawnTime = float.PositiveInfinity;
                feedback = "Aset model batu bara belum tersedia di Resources.";
                Debug.LogError("EnerGo: missing Resources/Coal/bara_1..4.");
                return;
            }

            // The coal materials are lit and these scenes have no light, so they would render near-black.
            if (coalLight == null)
            {
                coalLight = new GameObject("Coal light").AddComponent<Light>();
                coalLight.type = LightType.Directional;
                coalLight.intensity = 1.2f;
                coalLight.color = new Color(1f, 0.96f, 0.9f);
                coalLight.transform.rotation = Quaternion.Euler(45f, -30f, 0f);
            }

            target = new GameObject("Coal field");
            coalLumps.Clear();
            finishAt = -1f;
            Vector3 fwd = arCamera.transform.forward;
            float baseYaw = Mathf.Atan2(fwd.x, fwd.z) * Mathf.Rad2Deg;
            coalTotal = Random.Range(CoalMinLumps, CoalMaxLumps + 1);
            for (int i = 0; i < coalTotal; i++)
            {
                // Evenly across the arc with a little jitter, slightly below eye level.
                float yaw = baseYaw + Mathf.Lerp(-CoalArcDeg, CoalArcDeg, (i + 0.5f) / coalTotal) + Random.Range(-6f, 6f);
                float pitch = Random.Range(8f, 20f);
                float dist = Random.Range(1.3f, 1.7f);
                int required = Random.Range(CoalMinHits, CoalMaxHits + 1);

                var root = new GameObject("Coal lump").transform;
                root.SetParent(target.transform, false);
                root.position = arCamera.transform.position + Quaternion.Euler(pitch, yaw, 0f) * Vector3.forward * dist;
                root.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                var lump = new CoalLump { root = root, required = required, size = 0.16f + 0.05f * (required - CoalMinHits) };
                coalLumps.Add(lump);
                SetCoalStage(lump, 0);
            }
            feedback = "PECAHKAN SEMUA BATU BARA";
        }

        // Swap the lump's model for the given break stage, fitted to the lump's size and centred on it.
        private void SetCoalStage(CoalLump lump, int stage)
        {
            if (lump.stage == stage) return;
            if (lump.visual != null) Destroy(lump.visual);
            var v = Instantiate(coalStageModels[stage], lump.root);
            v.transform.localPosition = Vector3.zero;
            v.transform.localRotation = Quaternion.identity;
            if (TryGetBounds(v, out var b))
            {
                float longest = Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z));
                if (longest > 0.00001f) v.transform.localScale *= lump.size * CoalStageFit[stage] / longest;
                TryGetBounds(v, out b);
                v.transform.position += lump.root.position - b.center;
            }
            lump.visual = v;
            lump.stage = stage;
        }

        private bool HitCoal(Vector2 screenPosition)
        {
            CoalLump best = null;
            float bestDist = float.PositiveInfinity;
            foreach (var lump in coalLumps)
            {
                if (lump.root == null || lump.hits >= lump.required) continue;
                if (ScreenHit(lump.visual, screenPosition, out float d) && d < bestDist) { best = lump; bestDist = d; }
            }
            if (best == null) return false;

            best.hits++;
            lastHitTime = Time.time;
            lastHitScreenPos = screenPosition;
            if (best.hits < best.required)
            {
                if (SFXManager.Instance != null) SFXManager.Instance.PlayBaraPukul();
                SetCoalStage(best, CoalStage(best.hits, best.required));
                best.shakeUntil = Time.time + 0.18f;
                return true;
            }

            if (SFXManager.Instance != null) SFXManager.Instance.PlayBaraHancur();
            SpawnCoalDebris(best);
            Destroy(best.root.gameObject);
            if (CoalBroken() == coalTotal)
            {
                finishAt = Time.time + FinishDelay;
                feedback = "SEMUA BATU BARA TERKUMPUL";
            }
            return true;
        }

        private int CoalBroken()
        {
            int n = 0;
            foreach (var lump in coalLumps) if (lump.hits >= lump.required) n++;
            return n;
        }

        // A few small chunks that burst out and fall, then clean themselves up.
        private void SpawnCoalDebris(CoalLump lump)
        {
            var r = lump.visual != null ? lump.visual.GetComponentInChildren<Renderer>() : null;
            for (int i = 0; i < 8; i++)
            {
                var chip = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Destroy(chip.GetComponent<Collider>());
                if (r != null) chip.GetComponent<Renderer>().sharedMaterial = r.sharedMaterial;
                chip.transform.position = lump.root.position + Random.insideUnitSphere * lump.size * 0.3f;
                chip.transform.rotation = Random.rotation;
                chip.transform.localScale = Vector3.one * lump.size * Random.Range(0.12f, 0.22f);
                var body = chip.AddComponent<Rigidbody>();
                body.linearVelocity = (Random.insideUnitSphere + Vector3.up * 0.8f) * 1.6f;
                body.angularVelocity = Random.insideUnitSphere * 10f;
                Destroy(chip, 0.9f);
            }
        }

        private void UpdateCoal()
        {
            foreach (var lump in coalLumps)
            {
                if (lump.root == null) continue;
                // Short squash on each hit so the lump reacts to the pickaxe.
                float k = Mathf.Clamp01((lump.shakeUntil - Time.time) / 0.18f);
                lump.root.localScale = Vector3.one * (1f - 0.14f * k);
            }
        }

        // Tapping the gold deposit scoops sand and gold into the pan; the player then levels the phone.
        private void StartGoldPan()
        {
            goldPhase = GoldPhase.Leveling;
            goldPhaseAt = Time.time;
            goldFlatHold = 0f;
            goldSpillTimer = 0f;
            goldProgress = 0f;
            goldRough = false;
            goldSwirl = 0f;
            goldLastAngle = float.NaN;
            goldTilt = Vector2.zero;
            goldBubble = Vector2.zero;
            goldSand = new Vector2[GoldSandGrains];
            for (int i = 0; i < goldSand.Length; i++) goldSand[i] = Random.insideUnitCircle;
            goldPos = new Vector2[GoldPieces];
            goldVel = new Vector2[GoldPieces];
            goldHome = new Vector2[GoldPieces];
            goldLostDir = new Vector2[GoldPieces];
            goldSize = new float[GoldPieces];
            goldLostAt = new float[GoldPieces];
            goldLost = new bool[GoldPieces];
            for (int i = 0; i < GoldPieces; i++)
            {
                goldHome[i] = goldPos[i] = Random.insideUnitCircle * 0.5f;
                goldSize[i] = i < 4 ? Random.Range(6.5f, 8.5f) : Random.Range(3f, 5f); // a few nuggets, mostly flakes
            }
            target.SetActive(false); // the deposit has been scooped; keep the object so nothing respawns
            feedback = "LETAKKAN HP MENDATAR";
        }

        private bool GoldWarning() => goldPhase == GoldPhase.Failed || (goldPhase == GoldPhase.Panning && goldRough);

        private int GoldLeft()
        {
            int n = 0;
            if (goldLost != null) foreach (var lost in goldLost) if (!lost) n++;
            return n;
        }

        private static Vector2? PointerScreen()
        {
            foreach (var touch in EnhancedTouch.activeTouches) return touch.screenPosition;
#if UNITY_EDITOR
            if (Mouse.current != null && Mouse.current.leftButton.isPressed) return Mouse.current.position.ReadValue();
#endif
            return null;
        }

        private static bool ReadGravity(out Vector3 a)
        {
            a = Vector3.zero;
            var acc = Accelerometer.current;
            if (acc == null || !acc.enabled) return false;
            a = acc.acceleration.ReadValue();
            return a.sqrMagnitude > 1e-6f;
        }

        // Tilt of the pan relative to the calibrated level (x right, y toward the top of the screen) in
        // units of g, plus how hard the phone was jolted. Falls back to the finger/mouse without an accelerometer.
        private void ReadGoldTilt(out Vector2 tilt, out float jolt)
        {
            tilt = Vector2.zero;
            jolt = 0f;
            if (ReadGravity(out Vector3 a))
            {
                if (goldBaseline == Vector3.zero) { goldBaseline = a.normalized; goldGravity = a.magnitude; }
                // Slowly re-level so a drifting grip does not read as a constant tilt.
                goldBaseline = Vector3.Slerp(goldBaseline, a.normalized, Time.deltaTime * 0.15f).normalized;
                jolt = Mathf.Abs(a.magnitude / goldGravity - 1f);
                Vector3 e1 = Vector3.ProjectOnPlane(Vector3.right, goldBaseline).normalized;
                Vector3 e2 = Vector3.ProjectOnPlane(Vector3.up, goldBaseline);
                if (e2.sqrMagnitude < 0.05f) e2 = Vector3.ProjectOnPlane(Vector3.back, goldBaseline); // phone held upright
                e2.Normalize();
                Vector3 d = a.normalized - goldBaseline;
                tilt = new Vector2(Vector3.Dot(d, e1), Vector3.Dot(d, e2));
                return;
            }

            // No accelerometer (editor): dragging around the pan acts as tilting it.
            Vector2? pointer = PointerScreen();
            if (pointer == null) return;
            GoldPanRect(Screen.width / HudScale, Screen.height / HudScale, out Vector2 c, out float r);
            Vector2 centrePx = new Vector2(c.x, Screen.height / HudScale - c.y) * HudScale;
            tilt = Vector2.ClampMagnitude((pointer.Value - centrePx) / (r * HudScale) * 0.35f, 0.5f);
        }

        private void UpdateGoldPan()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            switch (goldPhase)
            {
                case GoldPhase.Leveling:
                {
                    // Spirit level: the phone counts as flat when gravity points out of the back of the screen.
                    bool flat;
                    if (ReadGravity(out Vector3 a))
                    {
                        Vector3 n = a.normalized;
                        flat = Vector3.Angle(n, Vector3.back) <= GoldFlatDeg;
                        goldBubble = Vector2.Lerp(goldBubble, new Vector2(-n.x, n.y), 1f - Mathf.Exp(-10f * dt)); // bubble floats uphill
                    }
                    else flat = PointerScreen() == null; // editor: counts as level while not dragging
                    goldFlatHold = flat ? goldFlatHold + dt : Mathf.Max(0f, goldFlatHold - dt * 2f);
                    if (goldFlatHold >= GoldFlatHoldSec) { goldPhase = GoldPhase.Countdown; goldPhaseAt = Time.time; }
                    break;
                }
                case GoldPhase.Countdown:
                    if (Time.time - goldPhaseAt >= GoldCountdownSec)
                    {
                        goldPhase = GoldPhase.Panning;
                        goldPhaseAt = Time.time;
                        goldBaseline = Vector3.zero; // "level" is however the player holds it right now
                        if (SFXManager.Instance != null) SFXManager.Instance.PlayAliranAir();
                        feedback = "PUTAR HP PERLAHAN SEPERTI MENDULANG";
                    }
                    break;
                case GoldPhase.Panning:
                    ReadGoldTilt(out Vector2 raw, out float jolt);
                    goldTilt = Vector2.Lerp(goldTilt, raw, 1f - Mathf.Exp(-12f * dt));
                    goldTiltMag = goldTilt.magnitude;
                    goldSwirl = StepSwirl(goldSwirl, ref goldLastAngle, goldTilt, dt);
                    goldProgress = StepGoldProgress(goldProgress, goldSwirl, goldTiltMag, jolt, dt, out goldRough);
                    StepGoldPieces(dt, goldRough, new Vector2(goldTilt.x, -goldTilt.y));
                    if (SFXManager.Instance != null)
                        SFXManager.Instance.SetAliranAirVolume(0.25f + 0.6f * Mathf.Clamp01(Mathf.Abs(goldSwirl) / GoldMaxSwirl));
                    CheckGoldOutcome();
                    break;
            }
        }

        // Gold is heavy: it slides a little downhill and settles back to where it lay. While the motion is
        // too rough it is pushed toward the rim, and every GoldSpillEverySec the piece nearest the low rim
        // flies out of the pan. `downhill` is the tilt in screen orientation (y down), units of g.
        private void StepGoldPieces(float dt, bool rough, Vector2 downhill)
        {
            Vector2 dir = downhill.sqrMagnitude > 1e-6f ? downhill.normalized : Vector2.zero;
            for (int i = 0; i < GoldPieces; i++)
            {
                if (goldLost[i]) continue;
                Vector2 acc = downhill * 2.2f - (goldPos[i] - goldHome[i]) * 2.5f - goldVel[i] * 1.6f;
                if (rough) acc += dir * 1.2f;
                goldVel[i] += acc * dt;
                goldPos[i] = Vector2.ClampMagnitude(goldPos[i] + goldVel[i] * dt, 0.92f);
            }

            if (!rough) { goldSpillTimer = Mathf.Max(0f, goldSpillTimer - dt); return; }
            goldSpillTimer += dt;
            if (goldSpillTimer < GoldSpillEverySec) return;
            goldSpillTimer = 0f;
            int best = -1;
            float bestScore = float.NegativeInfinity;
            for (int i = 0; i < GoldPieces; i++)
            {
                if (goldLost[i]) continue;
                float score = dir == Vector2.zero ? goldPos[i].magnitude : Vector2.Dot(goldPos[i], dir);
                if (score > bestScore) { bestScore = score; best = i; }
            }
            if (best < 0) return;
            goldLost[best] = true;
            goldLostAt[best] = Time.time;
            Vector2 fly = goldPos[best] + dir;
            goldLostDir[best] = fly.sqrMagnitude > 1e-6f ? fly.normalized : Vector2.up;
        }

        private void CheckGoldOutcome()
        {
            if (GoldLeft() < Mathf.CeilToInt(GoldPieces * GoldFailBelow))
            {
                goldPhase = GoldPhase.Failed;
                goldPhaseAt = Time.time;
                if (SFXManager.Instance != null) SFXManager.Instance.StopAliranAir();
                feedback = "EMAS TERBUANG!";
            }
            else if (goldProgress >= 1f)
            {
                goldPhase = GoldPhase.Done;
                goldPhaseAt = Time.time;
                finishAt = Time.time + FinishDelay;
                feedback = "EMAS TERPISAH DARI PASIR";
            }
        }

        // After a failure: drop the pan and hide a fresh deposit somewhere around the player.
        private void RetryGoldAtNewDeposit()
        {
            goldPhase = GoldPhase.None;
            if (SFXManager.Instance != null) SFXManager.Instance.StopAliranAir();
            if (target != null) Destroy(target);
            target = null;
            goldRespawnFar = true;
            nextSpawnTime = Time.time + 0.3f;
            feedback = "CARI ENDAPAN EMAS BARU";
        }

        private static void GoldPanRect(float sw, float sh, out Vector2 centre, out float radius)
        {
            centre = new Vector2(sw * 0.5f, sh * 0.6f);
            radius = Mathf.Min(sw * 0.4f, 160f);
        }

        // The pan: wooden rim, water that sloshes with the tilt, gold under a sand bed that washes out as
        // progress rises, gold flying over the rim when spilled, plus the level / countdown / fail overlays.
        private void DrawGoldPan(float sw, float sh)
        {
            GoldPanRect(sw, sh, out Vector2 c, out float R);
            Rect Circle(Vector2 p, float r) => new Rect(p.x - r, p.y - r, r * 2f, r * 2f);
            // Visual slosh, deliberately exaggerated so a small tilt already looks lively (screen y points down).
            Vector2 slosh = Vector2.ClampMagnitude(new Vector2(goldTilt.x, -goldTilt.y) * GoldSloshGain, 0.9f) * R;
            var mint = new Color(0.18f, 0.95f, 0.72f, 1f);

            EnerGoUI.DrawPanel(Circle(c + new Vector2(0f, 8f), R), new Color(0f, 0f, 0f, 0.35f), Color.clear, R);
            EnerGoUI.DrawPanel(Circle(c, R), new Color(0.55f, 0.36f, 0.18f, 1f), new Color(0.30f, 0.18f, 0.08f, 1f), R, 3f);
            EnerGoUI.DrawPanel(Circle(c, R * 0.84f), new Color(0.30f, 0.21f, 0.13f, 1f), new Color(0.22f, 0.14f, 0.07f, 1f), R * 0.84f, 2f);
            EnerGoUI.DrawPanel(Circle(c + slosh * 0.12f, R * 0.78f), new Color(0.30f, 0.58f, 0.80f, 0.18f), Color.clear, R * 0.78f);
            // Water wave crest piling up on the low side.
            if (slosh.sqrMagnitude > 1f)
                EnerGoUI.DrawPanel(Circle(c + slosh * 0.55f, R * 0.32f), new Color(0.55f, 0.80f, 0.95f, 0.22f), Color.clear, R * 0.32f);

            // Gold in the pan, drawn under the sand so washing the sand away is what reveals it.
            for (int i = 0; i < GoldPieces; i++)
            {
                if (goldLost[i]) continue;
                DrawGoldPiece(c + goldPos[i] * R * 0.78f, goldSize[i] + 0.6f * Mathf.Sin(Time.time * 5f + i), 1f);
            }

            // Wet sand bed that thins and shrinks as it washes out, plus loose grains riding the water.
            float left = 1f - goldProgress;
            float bedR = R * 0.62f * Mathf.Sqrt(left);
            if (bedR > 2f)
                EnerGoUI.DrawPanel(Circle(c + Vector2.ClampMagnitude(slosh * 0.6f, R * 0.84f - bedR), bedR), new Color(0.58f, 0.44f, 0.27f, 0.9f * left + 0.1f), Color.clear, bedR);
            int sand = Mathf.RoundToInt(GoldSandGrains * left);
            var sandCol = new Color(0.72f, 0.57f, 0.37f, 1f);
            for (int i = 0; i < sand; i++)
            {
                // Loose grains ride the water further than the bed, with a little per-grain jitter.
                Vector2 jitter = new Vector2(Mathf.Sin(Time.time * 7f + i * 1.3f), Mathf.Cos(Time.time * 6f + i * 0.7f)) * slosh.magnitude * 0.06f;
                Vector2 p = Vector2.ClampMagnitude(goldSand[i] * R * 0.72f + slosh * 1.1f + jitter, R * 0.78f);
                EnerGoUI.DrawPanel(Circle(c + p, 3f), sandCol, Color.clear, 3f);
            }

            // Spilled gold arcs out over the rim and fades.
            for (int i = 0; i < GoldPieces; i++)
            {
                if (!goldLost[i]) continue;
                float t = (Time.time - goldLostAt[i]) / 0.6f;
                if (t >= 1f) continue;
                Vector2 p = c + goldLostDir[i] * R * (0.8f + t * 0.9f) + Vector2.up * (-40f * t + 60f * t * t);
                DrawGoldPiece(p, goldSize[i], 1f - t);
            }

            if (goldPhase >= GoldPhase.Panning)
                GUI.Label(new Rect(c.x - 80f, c.y - R - 26f, 160f, 20f), $"EMAS {GoldLeft()}/{GoldPieces}", styleGoldVal);

            var big = new GUIStyle(styleRadarLocked) { fontSize = 46 };
            if (goldPhase == GoldPhase.Leveling)
            {
                // Spirit level: centre the bubble in the ring.
                bool flat = goldFlatHold > 0f;
                float ringR = R * 0.2f;
                EnerGoUI.DrawPanel(Circle(c, ringR), new Color(0f, 0f, 0f, 0.25f), flat ? mint : new Color(1f, 1f, 1f, 0.8f), ringR, 2.5f);
                Vector2 b = Vector2.ClampMagnitude(goldBubble * R * 1.6f, R * 0.7f);
                EnerGoUI.DrawPanel(Circle(c + b, 11f), new Color(1f, 1f, 1f, 0.85f), flat ? mint : Color.white, 11f, 2f);
            }
            else if (goldPhase == GoldPhase.Countdown)
            {
                int n = Mathf.Clamp(Mathf.CeilToInt(GoldCountdownSec - (Time.time - goldPhaseAt)), 1, 3);
                var card = new Rect(c.x - 100f, c.y - 62f, 200f, 124f);
                EnerGoUI.DrawPanel(card, new Color(0.04f, 0.10f, 0.15f, 0.92f), mint, 16f);
                GUI.Label(new Rect(card.x, card.y + 12f, card.width, 20f), "SIAP?", styleRadarLocked);
                GUI.Label(new Rect(card.x, card.y + 36f, card.width, 70f), n.ToString(), big);
            }
            else if (goldPhase == GoldPhase.Panning && Time.time - goldPhaseAt < 0.8f)
            {
                GUI.color = new Color(1f, 1f, 1f, 1f - (Time.time - goldPhaseAt) / 0.8f);
                GUI.Label(new Rect(c.x - 150f, c.y - 35f, 300f, 70f), "MULAI!", big);
                GUI.color = Color.white;
            }
            else if (goldPhase == GoldPhase.Failed)
            {
                var amber = new Color(1f, 0.78f, 0.22f, 1f);
                float w = Mathf.Min(300f, sw * 0.84f);
                var card = new Rect(c.x - w * 0.5f, c.y - 90f, w, 180f);
                EnerGoUI.DrawPanel(card, new Color(0.04f, 0.10f, 0.15f, 0.96f), amber, 16f);
                var title = new GUIStyle(styleRadarAlert) { fontSize = 22 };
                var body = new GUIStyle(styleStatusSub) { wordWrap = true };
                GUI.Label(new Rect(card.x + 16f, card.y + 16f, card.width - 32f, 30f), "EMAS TERBUANG!", title);
                GUI.Label(new Rect(card.x + 16f, card.y + 50f, card.width - 32f, 50f), "Gerakanmu terlalu kencang, emasnya tumpah dari dulang.", body);
                if (DrawBtn(new Rect(card.x + 16f, card.yMax - 60f, card.width - 32f, 44f), "CARI ENDAPAN BARU", styleBtnText))
                {
                    if (SFXManager.Instance != null) SFXManager.Instance.PlayButtonClick();
                    RetryGoldAtNewDeposit();
                }
            }
        }

        private static void DrawGoldPiece(Vector2 p, float r, float alpha)
        {
            EnerGoUI.DrawPanel(new Rect(p.x - r, p.y - r, r * 2f, r * 2f), new Color(1f, 0.80f, 0.10f, alpha), new Color(1f, 0.97f, 0.7f, alpha * 0.8f), r, 1.5f);
            float h = r * 0.3f;
            EnerGoUI.DrawPanel(new Rect(p.x - r * 0.35f - h, p.y - r * 0.35f - h, h * 2f, h * 2f), new Color(1f, 1f, 1f, 0.85f * alpha), Color.clear, h);
        }

        // Where the radar arrow points: the remaining coal lump closest to the view, else the target.
        private Vector3 RadarTargetPosition()
        {
            if (resourceId != ResourceIds.Coal) return target.transform.position;
            Transform best = null;
            float bestAngle = float.PositiveInfinity;
            foreach (var lump in coalLumps)
            {
                if (lump.root == null) continue;
                float a = Vector3.Angle(arCamera.transform.forward, lump.root.position - arCamera.transform.position);
                if (a < bestAngle) { bestAngle = a; best = lump.root; }
            }
            return best != null ? best.position : target.transform.position;
        }

        // Generous on-screen hit box around a model (mobile fingers are imprecise).
        private bool ScreenHit(GameObject model, Vector2 screenPosition, out float dist)
        {
            dist = float.PositiveInfinity;
            if (model == null || !TryGetBounds(model, out var bounds)) return false;
            float scale = HudScale;
            Vector2 min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
            Vector2 max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
            for (int corner = 0; corner < 8; corner++)
            {
                Vector3 offset = Vector3.Scale(bounds.extents, new Vector3(
                    (corner & 1) == 0 ? -1 : 1,
                    (corner & 2) == 0 ? -1 : 1,
                    (corner & 4) == 0 ? -1 : 1));
                Vector3 projected = arCamera.WorldToScreenPoint(bounds.center + offset);
                if (projected.z <= arCamera.nearClipPlane) return false;
                min = Vector2.Min(min, (Vector2)projected);
                max = Vector2.Max(max, (Vector2)projected);
            }
            Vector2 halfSize = Vector2.Max((max - min) * 0.5f, Vector2.one * (38f * scale));
            Vector2 centre = (min + max) * 0.5f;
            dist = Vector2.Distance(centre, screenPosition);
            Vector2 pad = Vector2.one * (18f * scale);
            return Rect.MinMaxRect(centre.x - halfSize.x - pad.x, centre.y - halfSize.y - pad.y,
                centre.x + halfSize.x + pad.x, centre.y + halfSize.y + pad.y).Contains(screenPosition);
        }

        private void SpawnGold(bool isRandomAngle)
        {
            if (resourceId == ResourceIds.Coal) { SpawnCoalField(); return; }
            if (goldPanning && SFXManager.Instance != null) SFXManager.Instance.StopAliranAir();
            goldPhase = GoldPhase.None;
            goldProgress = 0f;
            goldRespawnFar = false;
            target = new GameObject("Target " + ResourceIds.Name(resourceId));
            target.transform.position = Vector3.zero;
            target.transform.rotation = Quaternion.identity;

            GameObject visual;
            if (resourceId == ResourceIds.Gold) visual = Instantiate(goldModel, target.transform);
            else if (resourceId == ResourceIds.Sun && HasSunModel())
            {
                visual = Instantiate(sunModel, target.transform);
                MakeUnlit(visual); // these scenes have no light, and the sun should glow anyway
            }
            else
            {
                visual = GameObject.CreatePrimitive(resourceId == ResourceIds.Sun ? PrimitiveType.Sphere : PrimitiveType.Cube);
                visual.transform.SetParent(target.transform, false);
                var renderer = visual.GetComponent<Renderer>();
                var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Standard");
                var material = new Material(shader);
                material.color = resourceId == ResourceIds.Sun ? new Color(1f, 0.68f, 0.12f) : new Color(0.23f, 0.27f, 0.31f);
                renderer.material = material;
            }
            if (!TryGetBounds(visual, out var bounds) || bounds.size.magnitude < 0.00001f)
            {
                Destroy(target);
                target = null;
                nextSpawnTime = float.PositiveInfinity;
                feedback = "MODEL MINERAL TIDAK MEMILIKI MESH VALID";
                Debug.LogError("EnerGo: invalid gold mesh bounds.");
                return;
            }

            float longestSide = Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z));
            // The sun must fit inside the aiming ring, so it is smaller than the tap targets.
            float size = resourceId == ResourceIds.Sun ? 0.2f : 0.38f;
            target.transform.localScale = Vector3.one * (size / longestSide);
            TryGetBounds(visual, out bounds);
            visual.transform.position -= bounds.center;

            if (resourceId == ResourceIds.Gold && goldMaterial != null)
            {
                foreach (var renderer in visual.GetComponentsInChildren<Renderer>())
                {
                    var materials = renderer.sharedMaterials;
                    for (int i = 0; i < materials.Length; i++) materials[i] = goldMaterial;
                    renderer.sharedMaterials = materials;
                }
            }

            if (!isRandomAngle)
            {
                // ARCore Standard Spawning: 1.2m directly in front of camera
                target.transform.position = arCamera.transform.position + arCamera.transform.forward * 1.2f;
                target.transform.rotation = Quaternion.LookRotation(arCamera.transform.forward, Vector3.up);
            }
            else
            {
                // Gyro 360 Fallback: Spawns in room at random angle around player
                // Random horizontal angle between -135° and +135°
                float randYaw = Random.Range(-135f, 135f);
                float randPitch = Random.Range(-12f, 18f);
                float randDist = Random.Range(1.8f, 2.3f);

                Quaternion spawnRot = Quaternion.Euler(randPitch, randYaw, 0f);
                Vector3 forwardDir = arCamera.transform.forward;
                forwardDir.y = 0f;
                if (forwardDir.sqrMagnitude < 0.01f) forwardDir = Vector3.forward;
                forwardDir.Normalize();

                Vector3 spawnOffset = (Quaternion.LookRotation(forwardDir) * spawnRot) * Vector3.forward * randDist;
                target.transform.position = arCamera.transform.position + spawnOffset;
                target.transform.rotation = Quaternion.LookRotation(-spawnOffset.normalized, Vector3.up);
            }

            if (resourceId == ResourceIds.Sun)
            {
                Vector3 fwd = arCamera.transform.forward;
                sunBaseYaw = Mathf.Atan2(fwd.x, fwd.z) * Mathf.Rad2Deg;
                sunSeed = Random.Range(0f, 100f);
                sunSpawnTime = Time.time;
                sunProgress = 0f;
                UpdateSun();
            }

            feedback = isRandomAngle ? "TARGET TERDETEKSI // PUTAR PERANGKAT" : ResourceIds.Name(resourceId) + " SIAP DIKUMPULKAN";
        }

        private bool HasSunModel()
        {
            if (sunModel == null) sunModel = Resources.Load<GameObject>("Sun/sun");
            return sunModel != null && sunModel.GetComponentInChildren<Renderer>(true) != null;
        }

        // Swap every material for an unlit copy of the scene's energy material, keeping colour and texture.
        private void MakeUnlit(GameObject model)
        {
            if (energyMaterial == null) return;
            foreach (var r in model.GetComponentsInChildren<Renderer>(true))
            {
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    var src = mats[i];
                    var m = new Material(energyMaterial) { name = "Sun unlit" };
                    Color col = src != null && src.HasProperty("_BaseColor") ? src.GetColor("_BaseColor")
                        : src != null && src.HasProperty("_Color") ? src.GetColor("_Color") : new Color(1f, 0.68f, 0.12f);
                    // Neutral white/grey parts (the core) get the sun's yellow so it stays visible on a bright camera feed.
                    Color.RGBToHSV(col, out _, out float sat, out float val);
                    if (sat < 0.15f) col = new Color(1f, 0.74f, 0.16f, col.a) * Mathf.Max(val, 0.9f);
                    m.SetColor("_BaseColor", col);
                    Texture tex = src != null && src.HasProperty("_BaseMap") ? src.GetTexture("_BaseMap")
                        : src != null && src.HasProperty("_MainTex") ? src.GetTexture("_MainTex") : null;
                    m.SetTexture("_BaseMap", tex);
                    mats[i] = m;
                }
                r.sharedMaterials = mats;
            }
        }

        private static bool TryGetBounds(GameObject model, out Bounds bounds)
        {
            var renderers = model.GetComponentsInChildren<Renderer>();
            bounds = default;
            if (renderers.Length == 0) return false;
            bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            return true;
        }

        private void InitStyles()
        {
            if (stylesInitialized && styleHeaderTag != null) return;

            styleHeaderTag = new GUIStyle(GUI.skin.label)
            {
                fontSize = 9,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.45f, 0.88f, 1f) }
            };

            styleHeaderVal = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };

            styleGoldVal = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(1f, 0.80f, 0.25f) }
            };

            styleRadarAlert = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(1f, 0.78f, 0.22f) }
            };

            styleRadarLocked = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.18f, 0.95f, 0.72f) }
            };

            styleTapsCounter = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = Color.white }
            };

            styleBtnText = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.80f, 0.92f, 0.98f) }
            };

            styleBtnWarn = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(1f, 0.70f, 0.30f) }
            };

            styleStatusSub = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Normal,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.84f, 0.93f, 0.98f) }
            };

            EnerGoUI.ApplyFont(styleHeaderTag, styleHeaderVal, styleGoldVal, styleRadarAlert, styleRadarLocked,
                styleTapsCounter, styleBtnText, styleBtnWarn, styleStatusSub);
            stylesInitialized = true;
        }

        private void OnGUI()
        {
            InitStyles();

            if (texPill == null) LoadTextures();
            if (texWhite == null) CreateSolidTextures();

            var prevMatrix = GUI.matrix;
            var prevColor = GUI.color;

            float scale = HudScale;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));

            float sw = Screen.width / scale;
            float sh = Screen.height / scale;

            float safeTop = Mathf.Max(12f, (Screen.height - Screen.safeArea.yMax) / scale + 6f);

            // --- 1. TOP TELEMETRY BAR ---
            float pillH = 44f;

            // Return to Lobby Button (Left)
            var lobbyRect = new Rect(14f, safeTop, 90f, pillH);
            if (EnerGoProgress.Current.pendingCapture == null && DrawBtn(lobbyRect, "‹  LOBBY", styleBtnText))
            {
                if (SFXManager.Instance != null) SFXManager.Instance.PlayButtonBack();
                SceneManager.LoadScene("LobbyScene");
                return;
            }

            // Mode Indicator / Switcher (Center)
            string modeName = isUsingGyro ? (attitudeSensor != null || angularGyro != null ? "SENSOR 360" : "DRAG 360") : "ARCORE";
            float modeW = 160f;
            var modeRect = new Rect((sw - modeW) * 0.5f, safeTop, modeW, pillH);
            bool hasARSession = FindFirstObjectByType<ARSession>() != null;
            if (!hasARSession) DrawPill(modeRect, "MODE TANPA ARCORE", modeName, styleHeaderTag, styleHeaderVal);
            if (EnerGoProgress.Current.pendingCapture == null && hasARSession && DrawBtn(modeRect, modeName, isUsingGyro ? styleBtnWarn : styleBtnText))
            {
                if (SFXManager.Instance != null) SFXManager.Instance.PlayButtonClick();
                // Toggle mode manually
                ApplyTrackingModeState(!isUsingGyro);
                if (target != null) Destroy(target);
                target = null;
                nextSpawnTime = Time.time + 0.3f;
                feedback = isUsingGyro ? "BERALIH KE SENSOR 360" : "BERALIH KE ARCORE";
            }

            // Resource Badge (Right)
            float oreW = 105f;
            var oreRect = new Rect(sw - 14f - oreW, safeTop, oreW, pillH);
            DrawPill(oreRect, ResourceIds.Name(resourceId).ToUpperInvariant(), $"{EnerGoProgress.Current.Get(resourceId).Total} UNIT", styleHeaderTag, styleGoldVal);

            if (resourceId == ResourceIds.Sun && target != null && EnerGoProgress.Current.pendingCapture == null) DrawSunReticle(sw, sh);
            if (resourceId == ResourceIds.Coal && target != null && EnerGoProgress.Current.pendingCapture == null) DrawCoalPips(scale);
            if (goldPanning && EnerGoProgress.Current.pendingCapture == null) DrawGoldPan(sw, sh);

            // --- 2. RADAR & TARGET DIRECTION HUD ---
            DrawRadarHUD(sw, safeTop + pillH + 10f);

            // --- 3. BOTTOM CONTROL BAR ---
            float botY = sh - 52f;
            float botBtnW = Mathf.Min(260f, sw - 32f);
            var resetRect = new Rect((sw - botBtnW) * 0.5f, botY, botBtnW, 40f);

            string resetLabel = isUsingGyro ? "RESET POSISI KE HADAPAN" : "MUNCULKAN ULANG TARGET";
            if (EnerGoProgress.Current.pendingCapture == null && DrawBtn(resetRect, resetLabel, styleBtnText))
            {
                if (target != null) Destroy(target);
                target = null;
                lookPitch = 0f;
                lookYaw = 0f;
                if (arCamera != null) arCamera.transform.localRotation = Quaternion.identity;
                nextSpawnTime = Time.time + 0.2f;
                feedback = "TARGET DIPOSISIKAN TEPAT DI HADAPAN";
            }

            // Hit Spark Ripple
            if (Time.time - lastHitTime < 0.35f)
            {
                // Expanding, fading ring where the finger landed.
                float t = (Time.time - lastHitTime) / 0.35f;
                Vector2 hpos = new Vector2(lastHitScreenPos.x, Screen.height - lastHitScreenPos.y) / scale;
                float r = Mathf.Lerp(12f, 34f, t);
                EnerGoUI.DrawPanel(new Rect(hpos.x - r, hpos.y - r, r * 2f, r * 2f), Color.clear, new Color(0.12f, 0.95f, 0.75f, 1f - t), r, 3f);
            }

            if (EnerGoProgress.Current.pendingCapture != null) DrawAnalysis(sw, sh);
            GUI.color = prevColor;
            GUI.matrix = prevMatrix;
        }

        private void InitCardStyles()
        {
            if (cardStylesInit) return;
            styleSwipeBenar = new GUIStyle(GUI.skin.label)
            {
                fontSize       = 28,
                fontStyle      = FontStyle.Bold,
                alignment      = TextAnchor.MiddleCenter,
                wordWrap       = false,
                normal         = { textColor = new Color(0.12f, 0.95f, 0.62f) }
            };
            styleSwipeSalah = new GUIStyle(GUI.skin.label)
            {
                fontSize       = 28,
                fontStyle      = FontStyle.Bold,
                alignment      = TextAnchor.MiddleCenter,
                wordWrap       = false,
                normal         = { textColor = new Color(0.98f, 0.32f, 0.32f) }
            };
            styleCardStatement = new GUIStyle(GUI.skin.label)
            {
                fontSize       = 15,
                fontStyle      = FontStyle.Normal,
                alignment      = TextAnchor.MiddleCenter,
                wordWrap       = true,
                normal         = { textColor = Color.white }
            };
            styleCardHint = new GUIStyle(GUI.skin.label)
            {
                fontSize       = 11,
                fontStyle      = FontStyle.Normal,
                alignment      = TextAnchor.MiddleCenter,
                wordWrap       = false,
                normal         = { textColor = new Color(0.76f, 0.87f, 0.95f) }
            };
            EnerGoUI.ApplyFont(styleSwipeBenar, styleSwipeSalah, styleCardStatement, styleCardHint);
            cardStylesInit = true;
        }

        private void DrawAnalysis(float sw, float sh)
        {
            InitCardStyles();

            var session  = EnerGoProgress.Current.pendingCapture;
            var question = QuizBank.Find(session.questionId, session.resourceId);
            if (question == null) return;

            // ── Dark full-screen backdrop ──────────────────────────────────
            GUI.color = new Color(0.03f, 0.07f, 0.11f, 0.96f);
            GUI.DrawTexture(new Rect(0, 0, sw, sh), texWhite);
            GUI.color = Color.white;

            var body    = new GUIStyle(styleStatusSub)   { alignment = TextAnchor.UpperLeft,  fontSize = 14, wordWrap = true };

            var mint  = new Color(0.18f, 0.95f, 0.72f, 1f);
            var amber = new Color(1f, 0.78f, 0.22f, 1f);
            float w = Mathf.Min(sw - 40f, 360f), x = (sw - w) * 0.5f;
            float pad = 20f, resW = w - pad * 2f;
            var explain = new GUIStyle(body) { alignment = TextAnchor.UpperCenter };

            // ── FEEDBACK after each answer: right/wrong + the explanation ──
            if (session.state == "feedback")
            {
                float explainH = explain.CalcHeight(new GUIContent(question.explanation), resW);
                float cardH = 16f + 18f + 40f + 22f + 16f + explainH + 20f + 48f + pad;
                float y = (sh - cardH) * 0.5f;
                Color col = session.correct ? mint : amber;
                EnerGoUI.DrawPanel(new Rect(x, y, w, cardH), new Color(0.06f, 0.14f, 0.22f, 0.98f), col, 16f);

                float cy2 = y + 16f;
                GUI.Label(new Rect(x + pad, cy2, resW, 18f), "SOAL " + (session.questionIndex + 1) + "/" + session.QuestionCount, styleHeaderTag);
                cy2 += 18f;
                var verdict = new GUIStyle(styleRadarLocked) { fontSize = 24, normal = { textColor = col } };
                GUI.Label(new Rect(x + pad, cy2, resW, 40f), session.correct ? "BENAR!" : "SALAH", verdict);
                cy2 += 40f;
                GUI.Label(new Rect(x + pad, cy2, resW, 22f), "Pernyataan ini " + (question.correctAnswer ? "BENAR" : "SALAH"), styleStatusSub);
                cy2 += 22f + 16f;
                GUI.Label(new Rect(x + pad, cy2, resW, explainH), question.explanation, explain);
                cy2 += explainH + 20f;

                // A wrong answer holds the button briefly so the explanation actually gets read.
                bool canContinue = session.correct || Time.time - resultShownAt >= 2f;
                string next = session.IsLastQuestion ? "LIHAT HASIL" : "SOAL BERIKUTNYA";
                if (canContinue && DrawBtn(new Rect(x + pad, cy2, resW, 48f), next, styleBtnText))
                {
                    if (SFXManager.Instance != null) SFXManager.Instance.PlayButtonClick();
                    if (EnerGoProgress.NextQuestion())
                    {
                        cardOffset    = Vector2.zero;
                        cardFlicking  = false;
                        cardReturning = false;
                        isDraggingCard = false;
                    }
                }
                else if (!canContinue)
                    GUI.Label(new Rect(x + pad, cy2, resW, 48f), "Baca penjelasan dulu...", styleStatusSub);

                if (!string.IsNullOrEmpty(EnerGoProgress.Error))
                    GUI.Label(new Rect(x, sh - 80f, w, 60f), EnerGoProgress.Error, explain);
                return;
            }

            // ── FINAL RESULT after the last question ──
            if (session.state == "result")
            {
                int count = session.QuestionCount;
                // Saves from the 1-question version have no correctCount; their single answer is in `correct`.
                int right = session.questionIds != null && session.questionIds.Length > 0 ? session.correctCount : (session.correct ? 1 : 0);
                bool pure = right == count;
                string message = pure ? "Sempurna! Semua jawaban tepat."
                    : right == 0 ? "Belum ada yang tepat. Coba lagi di tangkapan berikutnya."
                    : "Bagus! Jawab semua dengan tepat untuk hasil Murni.";
                float msgH = explain.CalcHeight(new GUIContent(message), resW);
                float cardH = 16f + 18f + 44f + 22f + 16f + msgH + 20f + 48f + pad;
                float y = (sh - cardH) * 0.5f;
                Color col = pure ? mint : amber;
                EnerGoUI.DrawPanel(new Rect(x, y, w, cardH), new Color(0.06f, 0.14f, 0.22f, 0.98f), col, 16f);

                float cy2 = y + 16f;
                GUI.Label(new Rect(x + pad, cy2, resW, 18f), "HASIL ANALISIS", styleHeaderTag);
                cy2 += 18f;
                var verdict = new GUIStyle(styleRadarLocked) { fontSize = 26, normal = { textColor = col } };
                GUI.Label(new Rect(x + pad, cy2, resW, 44f), right + "/" + count + " BENAR", verdict);
                cy2 += 44f;
                GUI.Label(new Rect(x + pad, cy2, resW, 22f), "+" + session.rewardQuantity + " unit · Kualitas: " + (pure ? "Murni" : "Normal"), styleStatusSub);
                cy2 += 22f + 16f;
                GUI.Label(new Rect(x + pad, cy2, resW, msgH), message, explain);
                cy2 += msgH + 20f;

                if (DrawBtn(new Rect(x + pad, cy2, resW, 48f), "LANJUT KE LOBBY", styleBtnText))
                {
                    if (SFXManager.Instance != null) SFXManager.Instance.PlayButtonClick();
                    if (EnerGoProgress.ClearResult())
                    {
                        cardOffset   = Vector2.zero;
                        cardFlicking = false;
                        cardReturning = false;
                        SceneManager.LoadScene("LobbyScene");
                    }
                }

                if (!string.IsNullOrEmpty(EnerGoProgress.Error))
                    GUI.Label(new Rect(x, sh - 80f, w, 60f), EnerGoProgress.Error, explain);
                return;
            }

            // ── SWIPE CARD QUIZ ───────────────────────────────────────────
            if (cardFlicking && Mathf.Abs(cardOffset.x) > sw * 1.1f)
            {
                // Card has fully left the screen – stop animating
                cardFlicking  = false;
                cardOffset    = Vector2.zero;
                cardReturning = false;
                return;
            }

            // Card dimensions
            float cw = Mathf.Min(sw - 40f, 360f);
            float ch = Mathf.Min(sh * 0.55f, 320f);
            float cx = (sw - cw) * 0.5f + cardOffset.x;
            float cy = (sh - ch) * 0.5f + cardOffset.y;

            // Rotation (clamped to ±18°)
            float rotDeg = Mathf.Clamp(cardOffset.x * 0.08f, -18f, 18f);
            float rotRad = rotDeg * Mathf.Deg2Rad;

            // Save matrix and apply rotation around card centre
            var prevMatrix = GUI.matrix;
            Vector2 pivot  = new Vector2(cx + cw * 0.5f, cy + ch * 0.5f);
            GUIUtility.RotateAroundPivot(rotDeg, pivot);

            // ── Card shadow ──
            float shadowAlpha = 0.25f + Mathf.Abs(cardOffset.x) / sw * 0.25f;
            EnerGoUI.DrawPanel(new Rect(cx + 6f, cy + 8f, cw, ch), new Color(0f, 0f, 0f, shadowAlpha), Color.clear, 16f);

            // ── Card face ── border tint changes with swipe direction: right = BENAR (green), left = SALAH (red)
            float swipeRatio = Mathf.Clamp01(Mathf.Abs(cardOffset.x) / SwipeThreshold);
            bool  goingRight = cardOffset.x > 0f;
            Color borderCol  = Mathf.Abs(cardOffset.x) < 4f ? HudOutline : goingRight
                ? Color.Lerp(HudOutline, new Color(0.12f, 0.95f, 0.62f, 1f), swipeRatio)
                : Color.Lerp(HudOutline, new Color(0.98f, 0.32f, 0.32f, 1f), swipeRatio);
            EnerGoUI.DrawPanel(new Rect(cx, cy, cw, ch), new Color(0.06f, 0.14f, 0.22f, 0.98f), borderCol, 16f);

            // ── Resource tag ──
            float innerX = cx + 16f;
            float innerW = cw - 32f;
            GUI.Label(new Rect(innerX, cy + 16f, innerW, 16f),
                "ANALISIS · " + ResourceIds.Name(resourceId).ToUpperInvariant() + " · SOAL " + (session.questionIndex + 1) + "/" + session.QuestionCount, styleHeaderTag);

            // ── Statement text ──
            GUI.Label(new Rect(innerX, cy + 38f, innerW, ch - 120f), question.statement, styleCardStatement);

            // ── Hint ──
            GUI.Label(new Rect(innerX, cy + ch - 36f, innerW, 20f),
                "‹  geser kiri: SALAH      geser kanan: BENAR  ›", styleCardHint);

            GUI.matrix = prevMatrix;

            // ── SALAH overlay (left swipe) ──
            if (cardOffset.x < -4f)
            {
                float a = Mathf.Clamp01(-cardOffset.x / SwipeThreshold);
                Color salahCol = new Color(0.98f, 0.32f, 0.32f, a);
                // Left badge
                EnerGoUI.DrawPanel(new Rect(cx, cy, cw * 0.5f, ch), new Color(salahCol.r, salahCol.g, salahCol.b, a * 0.18f), Color.clear, 16f);
                styleSwipeSalah.normal.textColor = salahCol;
                GUI.Label(new Rect(cx + 8f, cy, cw * 0.45f, ch), "SALAH", styleSwipeSalah);
            }

            // ── BENAR overlay (right swipe) ──
            if (cardOffset.x > 4f)
            {
                float a = Mathf.Clamp01(cardOffset.x / SwipeThreshold);
                Color benarCol = new Color(0.12f, 0.95f, 0.62f, a);
                // Right badge
                EnerGoUI.DrawPanel(new Rect(cx + cw * 0.5f, cy, cw * 0.5f, ch), new Color(benarCol.r, benarCol.g, benarCol.b, a * 0.18f), Color.clear, 16f);
                styleSwipeBenar.normal.textColor = benarCol;
                GUI.Label(new Rect(cx + cw * 0.55f, cy, cw * 0.45f, ch), "BENAR", styleSwipeBenar);
            }

            if (!string.IsNullOrEmpty(EnerGoProgress.Error))
            {
                GUI.Label(new Rect(x, sh - 80f, w, 60f), EnerGoProgress.Error, body);
            }
        }

        private static readonly Color HudFill = new Color(0.04f, 0.10f, 0.15f, 0.90f);
        private static readonly Color HudOutline = new Color(0.23f, 0.43f, 0.52f, 1f);

        private void DrawRadarHUD(float sw, float hudY)
        {
            float hudW = sw - 32f;
            float hudX = 16f;
            Color accent = HudOutline;
            string line1 = null, line2 = null;
            GUIStyle style1 = styleStatusSub;
            bool locked = false;

            if (goldPanning)
            {
                bool warn = GoldWarning();
                line1 = goldPhase == GoldPhase.Leveling ? "LETAKKAN HP MENDATAR"
                    : goldPhase == GoldPhase.Countdown ? "SIAP-SIAP MENDULANG"
                    : goldPhase == GoldPhase.Done ? "EMAS TERPISAH DARI PASIR!"
                    : goldPhase == GoldPhase.Failed ? "EMAS TERBUANG!"
                    : goldRough ? "TERLALU KENCANG! EMAS BISA TUMPAH" : "PUTAR HP PERLAHAN SEPERTI MENDULANG";
                style1 = warn ? styleRadarAlert : styleRadarLocked;
                accent = warn ? new Color(1f, 0.78f, 0.22f, 0.9f) : new Color(0.18f, 0.95f, 0.72f, 0.9f);
                locked = true;
            }
            else if (target != null && arCamera != null)
            {
                Vector3 toTarget = RadarTargetPosition() - arCamera.transform.position;
                Vector3 flatToTarget = new Vector3(toTarget.x, 0, toTarget.z).normalized;
                Vector3 flatCamFwd = new Vector3(arCamera.transform.forward.x, 0, arCamera.transform.forward.z).normalized;

                float signedAngle = Vector3.SignedAngle(flatCamFwd, flatToTarget, Vector3.up);
                int deg = Mathf.RoundToInt(Mathf.Abs(signedAngle));

                // Is target roughly within view frustum?
                if (Mathf.Abs(signedAngle) > 28f)
                {
                    line1 = signedAngle < 0 ? $"‹  PUTAR KE KIRI  {deg}°" : $"PUTAR KE KANAN  {deg}°  ›";
                    line2 = "Mineral di luar bidang pandang";
                    style1 = styleRadarAlert;
                    accent = new Color(1f, 0.78f, 0.22f, 0.9f);
                }
                else if (resourceId == ResourceIds.Sun)
                {
                    line1 = sunInside ? "CAHAYA TERKUNCI · TAHAN POSISI HP" : "ARAHKAN LINGKARAN KE CAHAYA";
                    style1 = sunInside ? styleRadarLocked : styleRadarAlert;
                    accent = sunInside ? new Color(0.18f, 0.95f, 0.72f, 0.9f) : new Color(1f, 0.78f, 0.22f, 0.9f);
                    locked = true;
                }
                else if (resourceId == ResourceIds.Coal)
                {
                    line1 = "PECAHKAN BATU BARA · KETUK BONGKAHNYA";
                    style1 = styleRadarLocked;
                    accent = new Color(0.18f, 0.95f, 0.72f, 0.9f);
                    locked = true;
                }
                else
                {
                    line1 = "ENDAPAN EMAS DITEMUKAN";
                    line2 = "Ketuk endapannya untuk menyendok pasir";
                    style1 = styleRadarLocked;
                    accent = new Color(0.18f, 0.95f, 0.72f, 0.9f);
                }
            }
            else
            {
                line1 = feedback;
            }

            bool twoLines = line2 != null || locked;
            var box = new Rect(hudX, hudY, hudW, twoLines ? 64f : 42f);
            EnerGoUI.DrawPanel(box, HudFill, accent);

            GUI.Label(new Rect(hudX + 10, hudY + (twoLines ? 8f : 11f), hudW - 20, 20), line1, style1);
            if (line2 != null) GUI.Label(new Rect(hudX + 10, hudY + 34, hudW - 20, 18), line2, styleStatusSub);
            var mint = new Color(0.18f, 0.95f, 0.72f, 1f);
            if (locked && resourceId == ResourceIds.Sun)
                DrawHudBar(hudX + 20f, hudY + 36f, hudW - 40f, sunProgress, sunInside ? mint : new Color(1f, 0.78f, 0.22f, 1f), Mathf.FloorToInt(sunProgress * 100f) + "%");
            else if (locked && goldPanning)
            {
                // While levelling the bar shows how long the phone has been held flat.
                float v = goldPhase == GoldPhase.Leveling ? goldFlatHold / GoldFlatHoldSec : goldProgress;
                DrawHudBar(hudX + 20f, hudY + 36f, hudW - 40f, v, GoldWarning() ? new Color(1f, 0.78f, 0.22f, 1f) : mint, Mathf.FloorToInt(Mathf.Clamp01(v) * 100f) + "%");
            }
            else if (locked && resourceId == ResourceIds.Coal)
                DrawHudBar(hudX + 20f, hudY + 36f, hudW - 40f, CoalHitProgress(), mint, CoalBroken() + "/" + coalTotal);
        }

        // Fraction of all pickaxe hits needed for the whole coal field.
        private float CoalHitProgress()
        {
            int done = 0, total = 0;
            foreach (var lump in coalLumps) { done += Mathf.Min(lump.hits, lump.required); total += lump.required; }
            return total > 0 ? (float)done / total : 0f;
        }

        // Horizontal progress bar with a short label (percentage or count) on the right.
        private void DrawHudBar(float x, float y, float w, float progress, Color fillCol, string label)
        {
            float labelW = 44f;
            var track = new Rect(x, y + 3f, w - labelW - 8f, 12f);
            EnerGoUI.DrawPanel(track, new Color(1f, 1f, 1f, 0.08f), new Color(1f, 1f, 1f, 0.25f), 6f);
            if (progress > 0.01f)
                EnerGoUI.DrawPanel(new Rect(track.x, track.y, Mathf.Max(12f, track.width * progress), track.height), fillCol, Color.clear, 6f);
            GUI.Label(new Rect(track.xMax + 8f, y, labelW, 18f), label, styleTapsCounter);
        }

        // Remaining-hit pips floating above each coal lump: filled = hits landed.
        private void DrawCoalPips(float scale)
        {
            var done = new Color(0.18f, 0.95f, 0.72f, 1f);
            const float size = 9f, gap = 4f;
            foreach (var lump in coalLumps)
            {
                if (lump.root == null) continue;
                Vector3 sp = arCamera.WorldToScreenPoint(lump.root.position + Vector3.up * lump.size * 0.75f);
                if (sp.z <= arCamera.nearClipPlane) continue;
                float cx = sp.x / scale, y = (Screen.height - sp.y) / scale - size;
                float x = cx - (lump.required * size + (lump.required - 1) * gap) * 0.5f;
                for (int i = 0; i < lump.required; i++)
                {
                    bool hit = i < lump.hits;
                    EnerGoUI.DrawPanel(new Rect(x + i * (size + gap), y, size, size),
                        hit ? done : new Color(0f, 0f, 0f, 0.35f), hit ? done : new Color(1f, 1f, 1f, 0.8f), 3f);
                }
            }
        }

        // Fixed aiming ring in the middle of the screen; glows and pulses while the light is inside.
        private void DrawSunReticle(float sw, float sh)
        {
            float r = ReticleRadius * (sunInside ? 1f + 0.04f * Mathf.Sin(Time.time * 8f) : 1f);
            var rect = new Rect(sw * 0.5f - r, sh * 0.5f - r, r * 2f, r * 2f);
            Color ring = sunInside ? new Color(0.18f, 0.95f, 0.72f, 1f) : new Color(1f, 1f, 1f, 0.85f);
            Color fill = sunInside ? new Color(0.18f, 0.95f, 0.72f, 0.14f) : new Color(1f, 1f, 1f, 0.05f);
            EnerGoUI.DrawPanel(rect, fill, ring, r, 3f);
            // Small centre dot to aim with
            EnerGoUI.DrawPanel(new Rect(sw * 0.5f - 3f, sh * 0.5f - 3f, 6f, 6f), ring, Color.clear, 3f);
        }


        private void DrawPill(Rect rect, string tag, string val, GUIStyle sTag, GUIStyle sVal)
        {
            EnerGoUI.DrawPanel(rect, HudFill, HudOutline);
            GUI.Label(new Rect(rect.x + 6, rect.y + 5, rect.width - 12, 15), tag, sTag);
            GUI.Label(new Rect(rect.x + 6, rect.y + 20, rect.width - 12, 20), val, sVal);
        }

        private bool DrawBtn(Rect rect, string text, GUIStyle style)
        {
            bool isPressed = rect.Contains(Event.current.mousePosition) && Mouse.current != null && Mouse.current.leftButton.isPressed;

            GUI.color = isPressed ? new Color(0.75f, 0.75f, 0.75f, 1f) : Color.white;
            EnerGoUI.DrawSecondaryButton(rect);

            GUI.color = Color.white;
            GUI.Label(rect, text, style);

            if (ButtonHit(rect))
            {
                return true;
            }

            return false;
        }

        private bool ButtonHit(Rect rect) => EnerGoUI.TapHit(rect, HudScale);
    }
}
