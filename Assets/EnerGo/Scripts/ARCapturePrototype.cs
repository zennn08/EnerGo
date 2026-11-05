using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.XR.ARFoundation;
using EnhancedTouch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace EnerGo
{
    public partial class ARCapturePrototype : MonoBehaviour
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
        private string resourceId;
        private bool guideOpen; // the pre-game guide overlay is showing
        private CompassAnchor compass;
        private float captureBearing; // compass bearing of this capture's SDA (radar), random in practice mode
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
        private float sunLastTickAt = -10f;   // soft tick while the light is in the reticle
        private bool sunInside;
        private float sunBaseYaw;
        private float sunSeed;
        private float sunSpawnTime;
        private float sunTimeLimit = SunMath.MaxSec; // random per sun, SunMath.MinSec..MaxSec
        private bool sunDone;                        // time is up or the bar is full: stop updating, the quiz is opening

        // Coal "Pemicu" mini-game: 3-5 lumps in an arc in front; each needs 2-5 hits (bigger = more)
        // and steps through the four break-stage models (Resources/Coal/bara_1..4) before shattering.
        private const int CoalMinLumps = 3, CoalMaxLumps = 5;
        private const int CoalMinHits = 2, CoalMaxHits = 5;
        private const float CoalArcDeg = 75f;           // lumps land anywhere within ±75° of a randomly shifted centre
        private const float CoalMinGapDeg = 20f;        // ...but at least this far apart so they never overlap
        private const float QuizSecondsPerQuestion = 15f; // running out counts as a wrong answer
        // Mini-game score (0..1, turned into bonus units): at or under par = 1, 2.5x par or slower = 0.
        private const float CoalParPerHit = 0.45f, CoalParPerLump = 1.5f;
        private float capturePerformance;
        private float coalStartedAt;
        private float quizShownAt;
        private string quizShownKey;
        private const float FinishDelay = 0.6f;         // let the last break / gold reveal play before the quiz opens
        // Fit of the longest side per break stage. Stages 3-4 are low, wide piles, so their longest side is the width:
        // fitting them larger keeps them about as wide as the lump they came from instead of shrinking away.
        private static readonly float[] CoalStageFit = { 1f, 0.95f, 1.2f, 1.3f };
        private class CoalLump
        {
            public Transform root;
            public GameObject visual;
            public int hits, required, stage = -1;
            public float size, shakeUntil;
        }
        private readonly List<CoalLump> coalLumps = new List<CoalLump>();
        private GameObject[] coalStageModels;
        private GameObject coalChipModel, pickaxeModel; // Resources/Coal/serpihan, Resources/Coal/pickaxe (both optional)
        private Transform pickaxe;                       // swings in from the lower right on every hit
        private float pickSwingAt = -10f;
        private Vector3 pickTarget;
        private const float PickSwingSec = 0.26f, PickScale = 0.55f; // 0.74 m model -> ~0.4 m in hand
        private const float PickImpactSec = 0.07f;                    // when the head lands; hit feedback waits for it
        // The two sharp ends of the pick head in model space (measured in Art/energo_gaya.blend, sda_pickaxe):
        // 0.26 m either side of the handle, 0.64 m above the grip.
        private static readonly Vector3[] PickTips = { new Vector3(0.26f, 0.64f, 0f), new Vector3(-0.26f, 0.64f, 0f) };
        private Light modelLight; // the Blender models (coal, gold) use lit materials
        private int coalTotal;
        private float finishAt = -1f; // shared by coal and gold: when to open the quiz

        // Gold "Stabilizer" mini-game (mendulang): tap the deposit to scoop sand, then swirl the phone's tilt
        // in slow circles (accelerometer) so the sand washes over the rim and the gold flakes stay behind.
        // The rules and their tuning knobs are in DulangMath.
        public enum GoldPhase { None, Leveling, Countdown, Panning, Done, Failed }
        private const float GoldFlatDeg = 12f;           // "datar": screen facing up within this many degrees
        private const float GoldFlatHoldSec = 1f;        // hold it level this long before the countdown
        private const float GoldCountdownSec = 3f;       // 3-2-1, then "MULAI!"
        private const int GoldPieces = 18;               // gold flakes and nuggets in the pan
        private const float GoldFailBelow = 0.4f;        // fewer than 40% of the gold left = thrown away, fail
        private const float GoldSpillEverySec = 0.35f;   // while too rough, one piece flies out this often
        private GoldPhase goldPhase;
        private bool goldPanning => goldPhase != GoldPhase.None; // the pan is on screen
        private float goldPhaseAt;
        private float goldFlatHold;
        private float goldSpillTimer;
        private bool goldRespawnFar;   // after a failure the next deposit spawns somewhere else around the player
        private float goldProgress;
        private bool goldRough;
        private float goldLastRumbleAt = -10f; // warning rumble while panning too roughly
        private float goldSwirl;       // smoothed signed deg/s of the tilt direction
        private float goldTiltSpeed, goldRoughFor; // how fast the tilt moves; how long the motion has been too much
        private Vector2 goldTiltBefore;
        private float goldTiltMag;
        private float goldLastAngle = float.NaN;
        private Vector2 goldTilt;      // smoothed, drives the sloshing grains
        private Vector2 goldBubble;    // spirit-level bubble offset (screen space, units of g) while levelling
        private Vector3 goldBaseline;  // gravity direction the player holds as "level"
        private float goldGravity;     // accelerometer magnitude at rest (units differ per platform)
        // Gold pieces, in pan units (1 = inner rim), screen-oriented (y down).
        private Vector2[] goldPos, goldVel, goldLostDir;
        private float[] goldSize, goldLostAt; // goldLostAt: when the piece flew out (for the fly-out animation)
        private bool[] goldLost;

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
        private const float FlickSpeed       = 2800f;      // pixels/sec exit speed
        private const float ReturnSmoothing  = 12f;        // lerp speed for snap-back

        // Visual Feedback
        private string feedback = "Memulai inisialisasi sensor spasial...";
        private float lastHitTime = -10f;
        private Vector2 lastHitScreenPos;

        private static readonly Color CameraFallback = new Color32(0x14, 0x21, 0x3A, 0xff);

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
            if (!ResourceIds.IsPlayable(resourceId)) resourceId = ResourceIds.Gold;
            if (save.pendingCapture != null && save.pendingCapture.state != "awaiting-analysis") resultShownAt = Time.time;
            compass = gameObject.AddComponent<CompassAnchor>();
            compass.cam = arCamera;
            // A bearing only counts together with a radar spawn id; practice mode (or a stale bearing) gets a random one.
            captureBearing = ExploreSession.PendingSpawnId != null && ExploreSession.PendingBearingDeg.HasValue
                ? ExploreSession.PendingBearingDeg.Value : Random.Range(0f, 360f);

            // Set camera background fallback color
            if (arCamera != null)
            {
                arCamera.backgroundColor = CameraFallback;
            }
            if (save.pendingCapture == null && GuideViewer.ShouldShow(resourceId)) guideOpen = true;
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
                    arCamera.backgroundColor = CameraFallback;
                }
                else
                {
                    arCamera.clearFlags = CameraClearFlags.Depth;
                }
            }
        }

        private void OnDestroy()
        {
            if (target != null) Destroy(target);
            if (SFXManager.Instance != null) // manager outlives the scene
            {
                SFXManager.Instance.StopMatahari();
                SFXManager.Instance.StopEkstrakMatahari();
                SFXManager.Instance.StopAliranAir();
                SFXManager.Instance.StopSungai();
            }
            if (modelLight != null) Destroy(modelLight.gameObject);
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
            if (EnerGoProgress.Current.pendingCapture == null && !guideOpen && arCamera != null && canSpawn && target == null && Time.time >= nextSpawnTime && compass.ReadyToPlace)
            {
                // Place the first target in view so sensor-mode users see an immediate response.
                // A failed gold pan hides the next deposit somewhere around the player instead of in front.
                SpawnGold(isRandomAngle: goldRespawnFar);
            }

            compass.TargetPlaced = target != null; // CompassAnchor runs in LateUpdate, after this
            AnimatePickaxe();

            // 4. Idle Target Animation
            if (target != null)
            {
                // The coal field is a container at the origin; spinning it would orbit every lump.
                // The flat sun model is billboarded in UpdateSun instead: spinning it on Y would hide its rays edge-on.
                if (resourceId == ResourceIds.Gold) target.transform.Rotate(0f, 28f * Time.deltaTime, 0f, Space.Self);
                if (resourceId == ResourceIds.Sun && EnerGoProgress.Current.pendingCapture == null) UpdateSun();
                if (resourceId == ResourceIds.Coal) UpdateCoal();
                if (resourceId == ResourceIds.Uranium && EnerGoProgress.Current.pendingCapture == null) UpdateUranium();
                if (resourceId == ResourceIds.Oil && EnerGoProgress.Current.pendingCapture == null) UpdateOil();
                if (goldPanning && EnerGoProgress.Current.pendingCapture == null) UpdateGoldPan();
            }

            if (finishAt > 0f && Time.time >= finishAt)
            {
                finishAt = -1f;
                if (!CompleteCapture()) finishAt = Time.time + 1f; // retry; the error is shown in the HUD
            }

            UpdateQuizTimer();

            // 5. Input Handling (Touch & Mouse)
            HandleInputs();
        }

        // Each question has QuizSecondsPerQuestion; when it runs out the question is answered as wrong.
        private void UpdateQuizTimer()
        {
            var pending = EnerGoProgress.Current.pendingCapture;
            if (pending == null || pending.state != "awaiting-analysis") return;
            string key = pending.sessionId + "/" + pending.questionIndex;
            if (quizShownKey != key) { quizShownKey = key; quizShownAt = Time.time; }
            if (cardFlicking || Time.time - quizShownAt < QuizSecondsPerQuestion) return;
            if (EnerGoProgress.Answer(null))
            {
                isDraggingCard = false;
                cardReturning = false;
                cardOffset = Vector2.zero;
                if (SFXManager.Instance != null) SFXManager.Instance.PlayQuizSalah();
                Haptics.Pulse(200);
                resultShownAt = Time.time;
            }
        }

        private float QuizTimeLeftNow() => Mathf.Max(0f, QuizSecondsPerQuestion - (Time.time - quizShownAt));

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
            if (guideOpen) return; // the overlay handles its own taps
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
                else if (touch.phase == UnityEngine.InputSystem.TouchPhase.Moved && isDraggingLook && isUsingGyro && !goldPanning && !uraniumGripHeld)
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
                if (Mouse.current.rightButton.isPressed && isUsingGyro && !uraniumGripHeld)
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

            // The card offset is in panel units, so the swipe threshold is the same distance on every phone.
            float scale = Screen.width / CaptureScreens.PanelWidth;

            // ── TOUCH ──
            foreach (var touch in EnhancedTouch.activeTouches)
            {
                if (touch.phase == UnityEngine.InputSystem.TouchPhase.Began && !isDraggingCard)
                {
                    if (TryAnswerButton(touch.screenPosition)) return;
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
                    if (TryAnswerButton(Mouse.current.position.ReadValue())) return;
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
            // Commit answer: slide kanan = true (BENAR), swipe kiri = false (SALAH)
            if (CaptureScreens.PastThreshold(cardOffset.x)) CommitAnswer(cardOffset.x > 0f);
            else cardReturning = true;
        }

        // One answer, from a swipe or from the ✕/✓ buttons.
        private void CommitAnswer(bool answer)
        {
            cardFlicking = true;
            if (!EnerGoProgress.Answer(answer)) return;
            var session = EnerGoProgress.Current.pendingCapture;
            if (session != null)
            {
                Haptics.Pulse(session.correct ? 30 : 200);
                if (SFXManager.Instance != null)
                {
                    if (session.correct) SFXManager.Instance.PlayQuizBenar();
                    else SFXManager.Instance.PlayQuizSalah();
                }
            }
            resultShownAt = Time.time;
        }

        // A press on ✕/✓ answers right away and flies the card out; it never starts a drag.
        private bool TryAnswerButton(Vector2 screenPos)
        {
            float scale = HudScale, sw = Screen.width / scale, sh = Screen.height / scale;
            var p = new Vector2(screenPos.x / scale, (Screen.height - screenPos.y) / scale);
            for (int i = 0; i < 2; i++)
            {
                bool answer = i == 1;
                if (!QuizButtonRect(answer, sw, sh).Contains(p)) continue;
                isDraggingCard = false;
                cardReturning = false;
                cardOffset = new Vector2(answer ? 4f : -4f, 0f);
                CommitAnswer(answer);
                return true;
            }
            return false;
        }

        // Drift the sun around its spawn direction and fill/drain the bar depending on the reticle.
        private void UpdateSun()
        {
            if (arCamera == null) return;
            if (sunDone) return;
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
            if (sunInside && Time.time - sunLastTickAt >= 0.25f) { Haptics.Pulse(8); sunLastTickAt = Time.time; }

            // Extraction sound: playback position tracks sunProgress so the 8-second clip
            // maps 1:1 to the 0→100 % bar.  Pauses and seeks back when the sun escapes.
            if (SFXManager.Instance != null)
                SFXManager.Instance.SyncEkstrakMatahari(sunProgress, sunInside);

            // Whoosh loop: loud while the light is getting away, soft once it is locked,
            // panned toward the sun so it also hints where to turn.
            if (SFXManager.Instance != null)
            {
                SFXManager.Instance.PlayMatahari();
                float pan = Mathf.Clamp(arCamera.transform.InverseTransformDirection(dir).x * 1.5f, -1f, 1f);
                SFXManager.Instance.SetMatahariMix(sunInside ? 0.3f : 0.9f, pan);
            }

            switch (SunMath.SunOutcome(sunProgress, SunTimeLeft()))
            {
                case SunMath.Outcome.Captured:
                    // Full bar = Hebat; at time-out the share above 50 % decides the bonus. finishAt opens the quiz once (and retries on a save error).
                    capturePerformance = sunProgress >= 1f ? 1f : SunMath.SunPerformance(sunProgress);
                    Haptics.Pulse(80);
                    sunDone = true;
                    finishAt = Time.time;
                    break;
                case SunMath.Outcome.TooLittle:
                    if (SFXManager.Instance != null) { SFXManager.Instance.StopMatahari(); SFXManager.Instance.StopEkstrakMatahari(); }
                    Haptics.Pulse(300);
                    Destroy(target);
                    target = null;
                    nextSpawnTime = Time.time + SunMath.FailShowSec; // HUD shows `feedback` while there is no target
                    feedback = "CAHAYA TERLALU SEDIKIT";
                    break;
            }
        }

        private float SunTimeLeft() => sunTimeLimit - (Time.time - sunSpawnTime);

        private static float SpeedScore(float elapsed, float par) => Mathf.InverseLerp(par * 2.5f, par, elapsed);

        public static float StepSunProgress(float progress, bool inside, float dt) =>
            Mathf.Clamp01(progress + (inside ? SunFillPerSec : -SunDrainPerSec) * dt);

        // Perlin noise mapped to roughly [-1, 1].
        private static float Noise(float seed, float t) => Mathf.Clamp((Mathf.PerlinNoise(seed, t) - 0.5f) * 2.6f, -1f, 1f);

        // Shared finish for every mini-game: open the quiz, or report why it could not.
        private bool CompleteCapture()
        {
            if (EnerGoProgress.BeginAnalysis(resourceId, capturePerformance, ExploreSession.PendingSpawnId ?? ""))
            {
                ExploreSession.PendingSpawnId = null;
                if (SFXManager.Instance != null) { SFXManager.Instance.StopMatahari(); SFXManager.Instance.StopEkstrakMatahari(); SFXManager.Instance.StopAliranAir(); }
                goldPhase = GoldPhase.None;
                uraniumPhase = UraniumPhase.None;
                oilPhase = OilPhase.None;
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
            if (resourceId == ResourceIds.Uranium) return false; // found by listening, picked up with JEPIT
            if (resourceId == ResourceIds.Oil) return false;     // drilled and pumped by holding, not tapping
            if (resourceId == ResourceIds.Sun) return false; // sun is caught by aiming, so touches only drag the view
            if (goldPanning) return false;                    // the pan is controlled by tilting, not tapping
            touches++;

            // Ignore taps on UI top or bottom regions
            float scale = HudScale;
            if (HudTapZones.Blocks((Screen.height - screenPosition.y) / scale, Screen.height / scale)) return false;

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

        // The Blender models use lit materials and these scenes have no light, so they would render near-black.
        private void EnsureModelLight()
        {
            if (modelLight != null) return;
            modelLight = new GameObject("Model light").AddComponent<Light>();
            modelLight.type = LightType.Directional;
            modelLight.intensity = 1.2f;
            modelLight.color = new Color(1f, 0.96f, 0.9f);
            modelLight.transform.rotation = Quaternion.Euler(45f, -30f, 0f);
        }

        private void SpawnCoalField()
        {
            if (coalStageModels == null)
            {
                coalStageModels = new GameObject[4];
                for (int i = 0; i < 4; i++) coalStageModels[i] = Resources.Load<GameObject>("Coal/bara_" + (i + 1));
                coalChipModel = Resources.Load<GameObject>("Coal/serpihan");
                pickaxeModel = Resources.Load<GameObject>("Coal/pickaxe");
            }
            if (System.Array.IndexOf(coalStageModels, null) >= 0)
            {
                nextSpawnTime = float.PositiveInfinity;
                feedback = "Aset model batu bara belum tersedia di Resources.";
                Debug.LogError("EnerGo: missing Resources/Coal/bara_1..4.");
                return;
            }

            EnsureModelLight();

            target = new GameObject("Coal field");
            coalLumps.Clear();
            finishAt = -1f;
            float baseYaw = CompassMath.WorldYaw(captureBearing, compass.OffsetDeg);
            coalTotal = Random.Range(CoalMinLumps, CoalMaxLumps + 1);
            coalStartedAt = Time.time;
            // A different layout every round: random centre, random spots on the arc (kept apart), height and distance.
            float centreYaw = baseYaw + Random.Range(-10f, 10f);
            var yaws = new List<float>();
            for (int i = 0; i < coalTotal; i++)
            {
                float yaw = centreYaw;
                for (int tries = 0; tries < 40; tries++)
                {
                    yaw = centreYaw + Random.Range(-CoalArcDeg, CoalArcDeg);
                    if (yaws.TrueForAll(y => Mathf.Abs(Mathf.DeltaAngle(y, yaw)) >= CoalMinGapDeg)) break;
                }
                yaws.Add(yaw);
                float pitch = Random.Range(-6f, 26f); // from a little above eye level to well below it
                float dist = Random.Range(1.2f, 2.0f);
                int required = Random.Range(CoalMinHits, CoalMaxHits + 1);

                var root = new GameObject("Coal lump").transform;
                root.SetParent(target.transform, false);
                root.position = arCamera.transform.position + Quaternion.Euler(pitch, yaw, 0f) * Vector3.forward * dist;
                root.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                var lump = new CoalLump { root = root, required = required, size = 0.16f + 0.05f * (required - CoalMinHits) };
                coalLumps.Add(lump);
                SetCoalStage(lump, 0);
            }
            target.transform.SetParent(compass.North, true);
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
            pickSwingAt = Time.time;
            pickTarget = best.root.position;
            // The count (and the score) change on the tap; what the player sees, hears and feels waits for the
            // pickaxe head to land, so the swing and the hit read as one blow.
            var lumpHit = best;
            if (best.hits < best.required)
            {
                int stage = CoalStage(best.hits, best.required);
                AtPickImpact(() =>
                {
                    if (lumpHit.root == null) return; // field respawned during the swing
                    if (SFXManager.Instance != null) SFXManager.Instance.PlayBaraPukul();
                    Haptics.Pulse(20);
                    SetCoalStage(lumpHit, stage);
                    lumpHit.shakeUntil = Time.time + 0.18f;
                });
                return true;
            }

            AtPickImpact(() =>
            {
                if (lumpHit.root == null) return;
                if (SFXManager.Instance != null) SFXManager.Instance.PlayBaraHancur();
                Haptics.Pulse(50);
                SpawnCoalDebris(lumpHit);
                Destroy(lumpHit.root.gameObject);
            });
            if (CoalBroken() == coalTotal)
            {
                int hits = 0;
                foreach (var lump in coalLumps) hits += lump.required;
                capturePerformance = SpeedScore(Time.time - coalStartedAt, hits * CoalParPerHit + coalTotal * CoalParPerLump);
                finishAt = Time.time + FinishDelay;
                feedback = "SEMUA BATU BARA TERKUMPUL";
                AtPickImpact(() => // after the last shatter has started, so the two sounds do not mask each other
                {
                    if (SFXManager.Instance != null) SFXManager.Instance.PlayTambangSelesai();
                    Haptics.Pulse(120);
                }, 0.25f);
            }
            return true;
        }

        // Run `action` when the pickaxe head lands (plus `extra`), or right away when there is no pickaxe model.
        private void AtPickImpact(System.Action action, float extra = 0f)
        {
            float delay = (pickaxeModel != null ? PickImpactSec : 0f) + extra;
            if (delay <= 0f) action();
            else StartCoroutine(After(delay, action));
        }

        private static System.Collections.IEnumerator After(float seconds, System.Action action)
        {
            yield return new WaitForSeconds(seconds);
            action();
        }

        private int CoalBroken()
        {
            int n = 0;
            foreach (var lump in coalLumps) if (lump.hits >= lump.required) n++;
            return n;
        }

        // A few small coal chips that burst out and fall, then clean themselves up.
        private void SpawnCoalDebris(CoalLump lump)
        {
            var r = lump.visual != null ? lump.visual.GetComponentInChildren<Renderer>() : null;
            for (int i = 0; i < 8; i++)
            {
                GameObject chip;
                if (coalChipModel != null) chip = Instantiate(coalChipModel); // 1 m model, scaled below
                else
                {
                    chip = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    Destroy(chip.GetComponent<Collider>());
                    if (r != null) chip.GetComponent<Renderer>().sharedMaterial = r.sharedMaterial;
                }
                chip.transform.position = lump.root.position + Random.insideUnitSphere * lump.size * 0.3f;
                chip.transform.rotation = Random.rotation;
                chip.transform.localScale = Vector3.one * lump.size * Random.Range(0.12f, 0.22f);
                var body = chip.AddComponent<Rigidbody>();
                body.linearVelocity = (Random.insideUnitSphere + Vector3.up * 0.8f) * 1.6f;
                body.angularVelocity = Random.insideUnitSphere * 10f;
                Destroy(chip, 0.9f);
            }
        }

        // First-person pickaxe: on each hit it appears raised at the lower right, chops down at the lump
        // (the hit lands ~0.07 s in), recoils and drops out of view. Runs every frame, independent of the
        // coal field, so it never stays on screen when the target is respawned.
        private void AnimatePickaxe()
        {
            float t = Time.time - pickSwingAt;
            bool swinging = resourceId == ResourceIds.Coal && arCamera != null && pickaxeModel != null && t < PickSwingSec;
            if (!swinging)
            {
                if (pickaxe != null && pickaxe.gameObject.activeSelf) pickaxe.gameObject.SetActive(false);
                return;
            }
            if (pickaxe == null)
            {
                pickaxe = Instantiate(pickaxeModel, arCamera.transform, false).transform;
                pickaxe.localScale = Vector3.one * PickScale;
            }
            pickaxe.gameObject.SetActive(true);

            // Everything in camera space: +Z forward, +Y up, +X right.
            // At the hit the sharp tip of the pick, not the middle of the head, must sit on the line of sight to
            // the lump: find the point on that ray one grip-to-tip length away, then turn the handle back by the
            // fixed angle between handle and tip so the tip lands exactly there.
            var grip = new Vector3(0.14f, -0.25f, 0.3f);
            float tipLen = PickTips[0].magnitude * PickScale;
            Vector3 ray = arCamera.transform.InverseTransformPoint(pickTarget).normalized;
            float rg = Vector3.Dot(ray, grip), disc = rg * rg - grip.sqrMagnitude + tipLen * tipLen;
            Vector3 hitPoint = disc > 0f ? ray * (rg + Mathf.Sqrt(disc)) : arCamera.transform.InverseTransformPoint(pickTarget);
            Vector3 toTip = (hitPoint - grip).normalized;
            Vector3 axis = Vector3.Cross(toTip, Vector3.up).normalized;
            if (axis.sqrMagnitude < 0.01f) axis = Vector3.right;

            const float RaisedDeg = 75f, StrikeAt = PickImpactSec, HoldUntil = 0.11f;
            // The tip that leads the chop is the one that hits: the one pointing along the swing's motion.
            var probe = Quaternion.LookRotation(axis, toTip);
            Vector3 motion = toTip - Quaternion.AngleAxis(RaisedDeg, axis) * toTip;
            Vector3 tip = Vector3.Dot(probe * PickTips[0], motion) >= Vector3.Dot(probe * PickTips[1], motion) ? PickTips[0] : PickTips[1];
            float tipFromHandle = Vector3.SignedAngle(toTip, probe * tip, axis);
            Vector3 aim = Quaternion.AngleAxis(-tipFromHandle, axis) * toTip; // handle direction at the hit
            float angle, drop = 0f;
            if (t < StrikeAt) { float k = t / StrikeAt; angle = RaisedDeg * (1f - k * k); }            // speeds up into the hit
            else if (t < HoldUntil) angle = 6f * Mathf.Sin((t - StrikeAt) / (HoldUntil - StrikeAt) * Mathf.PI); // small recoil
            else { float k = (t - HoldUntil) / (PickSwingSec - HoldUntil); angle = 25f * k; drop = 0.25f * k * k; } // out of view

            Vector3 handle = Quaternion.AngleAxis(angle, axis) * aim;
            pickaxe.localPosition = grip + Vector3.down * drop;
            pickaxe.localRotation = Quaternion.LookRotation(axis, handle); // model: handle along +Y, head across X
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
            goldTilt = goldTiltBefore = Vector2.zero;
            goldTiltSpeed = goldRoughFor = 0f;
            goldBubble = Vector2.zero;
            goldPos = new Vector2[GoldPieces];
            goldVel = new Vector2[GoldPieces];
            goldLostDir = new Vector2[GoldPieces];
            goldSize = new float[GoldPieces];
            goldLostAt = new float[GoldPieces];
            goldLost = new bool[GoldPieces];
            for (int i = 0; i < GoldPieces; i++)
            {
                goldPos[i] = Random.insideUnitCircle * 0.55f;
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
                    if (goldFlatHold >= GoldFlatHoldSec) { goldPhase = GoldPhase.Countdown; goldPhaseAt = Time.time; Haptics.Pulse(40); }
                    break;
                }
                case GoldPhase.Countdown:
                    if (Time.time - goldPhaseAt >= GoldCountdownSec)
                    {
                        goldPhase = GoldPhase.Panning;
                        goldPhaseAt = Time.time;
                        goldBaseline = Vector3.zero; // "level" is however the player holds it right now
                        if (SFXManager.Instance != null) SFXManager.Instance.PlayAliranAir();
                        feedback = "MIRINGKAN HP BERPUTAR PELAN";
                    }
                    break;
                case GoldPhase.Panning:
                    ReadGoldTilt(out Vector2 raw, out float jolt);
                    goldTilt = Vector2.Lerp(goldTilt, raw, 1f - Mathf.Exp(-12f * dt));
                    goldTiltMag = goldTilt.magnitude;
                    goldSwirl = DulangMath.StepSwirl(goldSwirl, ref goldLastAngle, goldTilt, dt);
                    goldTiltSpeed = DulangMath.StepTiltSpeed(goldTiltSpeed, goldTiltBefore, goldTilt, dt);
                    goldTiltBefore = goldTilt;
                    goldProgress = DulangMath.StepProgress(goldProgress, goldSwirl, goldTiltSpeed, goldTiltMag, jolt, ref goldRoughFor, dt, out goldRough);
                    StepGoldPieces(dt, goldRough, new Vector2(goldTilt.x, -goldTilt.y));
                    if (goldRough && Time.time - goldLastRumbleAt >= 0.3f) { Haptics.Pulse(10); goldLastRumbleAt = Time.time; }
                    if (SFXManager.Instance != null)
                        SFXManager.Instance.SetAliranAirVolume(0.25f + 0.6f * Mathf.Clamp01(goldTiltSpeed / DulangMath.MaxTiltSpeed));
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
                // Heavy: it slides downhill, the bowl's slope brings it back toward the middle, and it stops quickly.
                Vector2 acc = downhill * 1.7f - goldPos[i] * 1.1f - goldVel[i] * 2.6f;
                if (rough) acc += dir * 1.2f;
                goldVel[i] += acc * dt;
                goldPos[i] = Vector2.ClampMagnitude(goldPos[i] + goldVel[i] * dt, 0.92f);
            }
            // Pieces lie side by side instead of inside each other.
            for (int i = 0; i < GoldPieces; i++)
            {
                if (goldLost[i]) continue;
                for (int j = i + 1; j < GoldPieces; j++)
                {
                    if (goldLost[j]) continue;
                    Vector2 gap = goldPos[j] - goldPos[i];
                    float min = (i < 4 ? 0.13f : 0.07f) + (j < 4 ? 0.13f : 0.07f), d = gap.magnitude;
                    if (d >= min || d < 1e-4f) continue;
                    Vector2 push = gap / d * (min - d) * 0.5f;
                    goldPos[i] -= push; goldPos[j] += push;
                }
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
            Haptics.Pulse(120);
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
                Haptics.Pulse(300);
                feedback = "EMAS TERBUANG!";
            }
            else if (goldProgress >= 1f)
            {
                goldPhase = GoldPhase.Done;
                goldPhaseAt = Time.time;
                capturePerformance = Mathf.InverseLerp(GoldFailBelow, 1f, GoldLeft() / (float)GoldPieces);
                finishAt = Time.time + FinishDelay;
                Haptics.Pulse(80);
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

        // World point at this capture's bearing (plus a yaw offset), tilted by pitch, `dist` metres from the camera.
        private Vector3 AtBearing(float yawOffsetDeg, float pitchDeg, float dist) =>
            arCamera.transform.position + compass.Direction(captureBearing + yawOffsetDeg, pitchDeg) * dist;

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
            if (resourceId == ResourceIds.Uranium) { SpawnUranium(); return; }
            if (resourceId == ResourceIds.Oil) { SpawnOil(); return; }
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

            if (resourceId == ResourceIds.Gold)
            {
                EnsureModelLight();
                var sparkle = visual.transform.Find("sda_emas_kilau");
                if (sparkle != null) sparkle.gameObject.AddComponent<Melayang>();
            }

            // Gold sits at the radar bearing; after a spilled pan the next deposit hides a little to the side.
            float side = isRandomAngle ? Random.Range(25f, 50f) * (Random.value < 0.5f ? -1f : 1f) : 0f;
            target.transform.position = AtBearing(side, Random.Range(-12f, 10f), Random.Range(1.4f, 2.0f));
            target.transform.rotation = Quaternion.LookRotation(arCamera.transform.position - target.transform.position, Vector3.up);

            if (resourceId == ResourceIds.Sun)
            {
                Vector3 fwd = arCamera.transform.forward;
                sunBaseYaw = Mathf.Atan2(fwd.x, fwd.z) * Mathf.Rad2Deg;
                sunSeed = Random.Range(0f, 100f);
                sunSpawnTime = Time.time;
                sunProgress = 0f;
                sunTimeLimit = SunMath.SunTimeLimit(Random.value);
                sunDone = false;
                UpdateSun();
            }
            if (resourceId != ResourceIds.Sun) target.transform.SetParent(compass.North, true);

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
                bool corona = IsCorona(r);
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    var src = mats[i];
                    var m = new Material(energyMaterial) { name = "Sun unlit" };
                    Color col = src != null && src.HasProperty("_BaseColor") ? src.GetColor("_BaseColor")
                        : src != null && src.HasProperty("_Color") ? src.GetColor("_Color") : new Color(1f, 0.68f, 0.12f);
                    // Neutral white/grey parts (the core) get the sun's yellow so it stays visible on a bright camera feed.
                    Texture tex = src != null && src.HasProperty("_BaseMap") ? src.GetTexture("_BaseMap")
                        : src != null && src.HasProperty("_MainTex") ? src.GetTexture("_MainTex") : null;
                    // Textured parts keep their own colours (white tint); only an untextured grey core is tinted yellow.
                    Color.RGBToHSV(col, out _, out float sat, out float val);
                    if (tex != null) col = Color.white;
                    else if (sat < 0.15f) col = new Color(1f, 0.74f, 0.16f, col.a) * Mathf.Max(val, 0.9f);
                    m.SetColor("_BaseColor", col);
                    m.SetTexture("_BaseMap", tex);
                    if (corona) MakeTransparent(m);
                    mats[i] = m;
                }
                r.sharedMaterials = mats;
            }
        }

        // The sun's corona is a camera-facing glow plane far wider than the sun itself, so sizing ignores it.
        private static bool IsCorona(Renderer r) => r.name.EndsWith("_korona");

        // Alpha-blended, both faces, no depth write: the corona's soft rays fade into the camera feed.
        private static void MakeTransparent(Material m)
        {
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 0f);
            m.SetFloat("_Cull", 0f);
            m.SetFloat("_ZWrite", 0f);
            m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.SetOverrideTag("RenderType", "Transparent");
            m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }

        private static bool TryGetBounds(GameObject model, out Bounds bounds)
        {
            bounds = default;
            bool any = false;
            foreach (var r in model.GetComponentsInChildren<Renderer>())
            {
                if (IsCorona(r)) continue;
                if (any) bounds.Encapsulate(r.bounds); else { bounds = r.bounds; any = true; }
            }
            return any;
        }

        // State and actions for CaptureView.
        public CaptureScreen CurrentScreen => CaptureScreens.For(EnerGoProgress.Current.pendingCapture != null, EnerGoProgress.Current.pendingCapture?.state);
        public bool GuideOpen => guideOpen;
        public bool HasARSession => FindFirstObjectByType<ARSession>() != null;
        public string ResourceId => resourceId;
        public string BackLabel => ExploreSession.PendingSpawnId != null ? "‹ RADAR" : "‹ LOBBY";
        public string ModeLabel => isUsingGyro ? (attitudeSensor != null || angularGyro != null ? "SENSOR 360" : "DRAG 360") : "ARCORE";
        public string ResourceUnitsLabel => EnerGoProgress.Current.Get(resourceId).Total + " UNIT";
        public float LastHitTime => lastHitTime;
        public Vector2 LastHitScreenPos => lastHitScreenPos;

        public void BackFromCapture()
        {
            if (EnerGoProgress.Current.pendingCapture != null) return;
            bool fromRadar = ExploreSession.PendingSpawnId != null;
            if (SFXManager.Instance != null) SFXManager.Instance.PlayButtonBack();
            ExploreSession.PendingSpawnId = null; // leaving without catching: the spawn stays on the radar
            SceneManager.LoadScene(fromRadar ? "ExploreScene" : "LobbyScene");
        }

        public void ToggleTrackingMode()
        {
            if (EnerGoProgress.Current.pendingCapture != null) return;
            if (SFXManager.Instance != null) SFXManager.Instance.PlayButtonClick();
            ApplyTrackingModeState(!isUsingGyro);
            if (target != null) Destroy(target);
            target = null;
            uraniumPhase = UraniumPhase.None;
            oilPhase = OilPhase.None;
            nextSpawnTime = Time.time + 0.3f;
            feedback = isUsingGyro ? "BERALIH KE SENSOR 360" : "BERALIH KE ARCORE";
        }

        public void RespawnTarget()
        {
            if (guideOpen || EnerGoProgress.Current.pendingCapture != null) return;
            if (target != null) Destroy(target);
            target = null;
            uraniumPhase = UraniumPhase.None;
            oilPhase = OilPhase.None;
            lookPitch = 0f;
            lookYaw = 0f;
            if (arCamera != null) arCamera.transform.localRotation = Quaternion.identity;
            nextSpawnTime = Time.time + 0.2f;
            feedback = resourceId == ResourceIds.Sun ? "CARI CAHAYA DI DEPAN" : "IKUTI PITA KOMPAS";
        }

        public void FinishGuide(bool dontRemind)
        {
            if (dontRemind) { PlayerPrefs.SetInt(GuideViewer.SkipKey(resourceId), 1); PlayerPrefs.Save(); }
            guideOpen = false;
            nextSpawnTime = Time.time; // the mini-game (and the sun timer) starts now
        }

        // Right/wrong per question of the current capture, filled in as each feedback card shows (null = not seen).
        private string quizMarksSession;
        private readonly bool?[] quizMarks = new bool?[8];

        // ✕ (false, left) and ✓ (true, right) under the quiz card, in GUI units.
        private Rect QuizButtonRect(bool answer, float sw, float sh)
        {
            float h = 67.2f, gap = 16f, w = (Mathf.Min(sw - 40f, 420f) - gap) * 0.5f;
            float x0 = (sw - w * 2f - gap) * 0.5f;
            return new Rect(answer ? x0 + w + gap : x0, sh - UiLayout.SafeBottom(Screen.safeArea.yMin, HudScale) - h, w, h);
        }

        // State and actions for the quiz screens.
        public CaptureSession QuizSession => EnerGoProgress.Current.pendingCapture;
        public QuestionDefinition QuizQuestion => QuizSession == null ? null : QuizBank.Find(QuizSession.questionId, QuizSession.resourceId);
        public float QuizTimeLeft => QuizTimeLeftNow();
        public float QuizSeconds => QuizSecondsPerQuestion;
        public Vector2 CardOffset => cardOffset;
        public float SecondsSinceFeedback => Time.time - resultShownAt;
        public Rect AnswerButtonRect(bool answer) => QuizButtonRect(answer, UnityEngine.Screen.width / HudScale, UnityEngine.Screen.height / HudScale);
        public float GuiToPanel(float panelWidth) => HudScale * panelWidth / UnityEngine.Screen.width;

        public bool AnswerButtonHeld(bool answer)
        {
            Vector2? p = PointerScreen();
            if (p == null || cardFlicking) return false;
            return AnswerButtonRect(answer).Contains(new Vector2(p.Value.x, UnityEngine.Screen.height - p.Value.y) / HudScale);
        }

        public bool?[] ReadQuizMarks()
        {
            var session = QuizSession;
            if (session == null) return quizMarks;
            if (quizMarksSession != session.sessionId)
            {
                quizMarksSession = session.sessionId;
                for (int i = 0; i < quizMarks.Length; i++) quizMarks[i] = null;
            }
            if (session.state != "awaiting-analysis" && session.questionIndex < quizMarks.Length) quizMarks[session.questionIndex] = session.correct;
            return quizMarks;
        }

        public void NextQuizQuestion()
        {
            if (SFXManager.Instance != null) SFXManager.Instance.PlayButtonClick();
            if (!EnerGoProgress.NextQuestion()) return;
            cardOffset = Vector2.zero;
            cardFlicking = false;
            cardReturning = false;
            isDraggingCard = false;
        }

        public void FinishQuizResult()
        {
            var session = QuizSession;
            if (session == null) return;
            bool backToRadar = !string.IsNullOrEmpty(session.spawnId);
            if (SFXManager.Instance != null) SFXManager.Instance.PlayButtonClick();
            if (!EnerGoProgress.ClearResult()) return;
            cardOffset = Vector2.zero;
            cardFlicking = false;
            cardReturning = false;
            SceneManager.LoadScene(backToRadar ? "ExploreScene" : "LobbyScene");
        }

        // Where the units came from: 1 base + 1 per correct answer + the mini-game bonus.
        public string[] ResultChips(CaptureSession session, int right) => new[] { "Dasar +1", "Kuis +" + right, MiniGameName(session.resourceId) + " " + EnerGoProgress.GradeFor(session.performanceBonus) + " +" + session.performanceBonus };

        // What the status box says right now.
        public HudStatus ReadHud()
        {
            if (uraniumPhase != UraniumPhase.None) return ReadUraniumHud();
            if (oilPhase != OilPhase.None) return ReadOilHud();

            if (goldPanning)
            {
                string line = goldPhase == GoldPhase.Leveling ? "LETAKKAN HP MENDATAR"
                    : goldPhase == GoldPhase.Countdown ? "SIAP-SIAP MENDULANG"
                    : goldPhase == GoldPhase.Done ? "EMAS TERPISAH DARI PASIR!"
                    : goldPhase == GoldPhase.Failed ? "EMAS TERBUANG!"
                    : goldRough ? "TERLALU KENCANG! EMAS BISA TUMPAH" : "MIRINGKAN HP BERPUTAR PELAN";
                // While levelling the bar shows how long the phone has been held flat.
                float v = goldPhase == GoldPhase.Leveling ? goldFlatHold / GoldFlatHoldSec : goldProgress;
                string label = Mathf.FloorToInt(Mathf.Clamp01(v) * 100f) + "%";
                return goldPhase == GoldPhase.Failed ? HudStatus.Failed(line, v, label) : HudStatus.WithBar(line, GoldWarning(), v, label);
            }
            if (target == null || arCamera == null) return HudStatus.Plain(feedback);

            Vector3 toTarget = RadarTargetPosition() - arCamera.transform.position;
            Vector3 flatToTarget = new Vector3(toTarget.x, 0, toTarget.z).normalized;
            Vector3 flatCamFwd = new Vector3(arCamera.transform.forward.x, 0, arCamera.transform.forward.z).normalized;
            float signedAngle = Vector3.SignedAngle(flatCamFwd, flatToTarget, Vector3.up);
            int deg = Mathf.RoundToInt(Mathf.Abs(signedAngle));

            if (resourceId == ResourceIds.Sun && Mathf.Abs(signedAngle) > 28f)
                return HudStatus.Text(signedAngle < 0 ? $"‹  PUTAR KE KIRI  {deg}°" : $"PUTAR KE KANAN  {deg}°  ›", true, "SISA " + Mathf.Max(0, Mathf.CeilToInt(SunTimeLeft())) + " DTK");
            if (resourceId == ResourceIds.Sun)
            {
                int left = Mathf.Max(0, Mathf.CeilToInt(SunTimeLeft()));
                return HudStatus.WithBar((sunInside ? "CAHAYA TERKUNCI" : "ARAHKAN KE CAHAYA") + " · SISA " + left + " DTK", left <= 5 || !sunInside, sunProgress, Mathf.FloorToInt(sunProgress * 100f) + "%");
            }
            if (resourceId == ResourceIds.Coal)
                return HudStatus.WithBar("PECAHKAN BATU BARA · KETUK BONGKAHNYA", false, CoalHitProgress(), CoalBroken() + "/" + coalTotal);
            return HudStatus.Text("ENDAPAN EMAS DITEMUKAN", false, "Ketuk endapannya untuk menyendok pasir");
        }

        // Fraction of all pickaxe hits needed for the whole coal field.
        private float CoalHitProgress()
        {
            int done = 0, total = 0;
            foreach (var lump in coalLumps) { done += Mathf.Min(lump.hits, lump.required); total += lump.required; }
            return total > 0 ? (float)done / total : 0f;
        }

        // The compass ruler: the view direction and the SDA, as a dot, an edge marker, or for uranium only a rough sector.
        public EnerGo.UI.PitaKompas.State ReadCompassStrip()
        {
            var st = new EnerGo.UI.PitaKompas.State();
            if (compass == null || arCamera == null) return st;
            float half = CompassMath.StripHalfSpanDeg, heading = compass.HeadingDeg;
            st.HasCompass = compass.HasCompass;
            st.Heading = heading;
            st.Half = half;
            if (resourceId == ResourceIds.Uranium)
            {
                if (uraniumPhase == UraniumPhase.Search) // fragments are found with the RAD meter, not the strip
                {
                    float c = CompassMath.StripX(captureBearing, heading, half), w = CompassMath.UraniumSectorHalfDeg / half;
                    st.Lo01 = Mathf.Max(-1f, c - w);
                    st.Hi01 = Mathf.Min(1f, c + w);
                    st.ShowSector = st.Hi01 > st.Lo01;
                    if (!st.ShowSector) { st.EdgeSide = c < 0f ? -1 : 1; st.EdgeDeg = Mathf.RoundToInt(Mathf.Abs(Mathf.DeltaAngle(heading, captureBearing)) - CompassMath.UraniumSectorHalfDeg); }
                }
            }
            else if (resourceId != ResourceIds.Sun && target != null)
            {
                Vector3 to = RadarTargetPosition() - arCamera.transform.position;
                float bearing = CompassMath.CameraHeading(Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg, compass.OffsetDeg);
                float x01 = CompassMath.StripX(bearing, heading, half);
                st.ShowDot = Mathf.Abs(x01) <= 1f;
                st.Dot01 = x01;
                if (!st.ShowDot) { st.EdgeSide = x01 < 0f ? -1 : 1; st.EdgeDeg = Mathf.RoundToInt(Mathf.Abs(Mathf.DeltaAngle(heading, bearing))); }
            }
            return st;
        }


        private static string MiniGameName(string id) =>
            id == ResourceIds.Sun ? "Ekstraksi" : id == ResourceIds.Coal ? "Tambang" : id == ResourceIds.Uranium ? "Pemburu Geiger" : id == ResourceIds.Oil ? "Bor & Pompa" : "Dulang";

    }
}
