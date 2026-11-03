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
        private GuideViewer guide; // pre-game guide overlay; null = closed
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
        private const float GoldSloshGain = 2.4f;        // how far sand/water visibly slide per unit of tilt
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
        private float goldTiltMag;
        private float goldLastAngle = float.NaN;
        private Vector2 goldTilt;      // smoothed, drives the sloshing grains
        private Vector2 goldBubble;    // spirit-level bubble offset (screen space, units of g) while levelling
        private Vector3 goldBaseline;  // gravity direction the player holds as "level"
        private float goldGravity;     // accelerometer magnitude at rest (units differ per platform)
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
        private GUIStyle styleCardStatement;
        private GUIStyle styleCardHint;
        private bool cardStylesInit = false;

        // Visual Feedback
        private string feedback = "Memulai inisialisasi sensor spasial...";
        private float lastHitTime = -10f;
        private Vector2 lastHitScreenPos;

        // UI Textures
        private Texture2D texWhite;

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
            if (!ResourceIds.IsPlayable(resourceId)) resourceId = ResourceIds.Gold;
            if (save.pendingCapture != null && save.pendingCapture.state != "awaiting-analysis") resultShownAt = Time.time;
            compass = gameObject.AddComponent<CompassAnchor>();
            compass.cam = arCamera;
            // A bearing only counts together with a radar spawn id; practice mode (or a stale bearing) gets a random one.
            captureBearing = ExploreSession.PendingSpawnId != null && ExploreSession.PendingBearingDeg.HasValue
                ? ExploreSession.PendingBearingDeg.Value : Random.Range(0f, 360f);

            CreateSolidTextures();

            // Set camera background fallback color
            if (arCamera != null)
            {
                arCamera.backgroundColor = EnerGoUI.Ink;
            }
            if (save.pendingCapture == null && GuideViewer.ShouldShow(resourceId)) guide = new GuideViewer(resourceId, withStart: true);
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
                    arCamera.backgroundColor = EnerGoUI.Ink;
                }
                else
                {
                    arCamera.clearFlags = CameraClearFlags.Depth;
                }
            }
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
            if (texWhite != null) Destroy(texWhite);
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
            if (EnerGoProgress.Current.pendingCapture == null && guide == null && arCamera != null && canSpawn && target == null && Time.time >= nextSpawnTime && compass.ReadyToPlace)
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

        private float QuizTimeLeft() => Mathf.Max(0f, QuizSecondsPerQuestion - (Time.time - quizShownAt));

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
            if (guide != null) return; // the overlay handles its own taps
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

            float scale = HudScale;

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
            if (Mathf.Abs(cardOffset.x) >= SwipeThreshold) CommitAnswer(cardOffset.x > 0f);
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
                EnerGoUI.BlockTaps(0.35f); // the feedback card draws this frame; its button must not take this touch
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
            goldTilt = Vector2.zero;
            goldBubble = Vector2.zero;
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
                    if (goldRough && Time.time - goldLastRumbleAt >= 0.3f) { Haptics.Pulse(10); goldLastRumbleAt = Time.time; }
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

        // Overlays on the 3D pan: gold count, spirit level, countdown, MULAI! and the fail card.
        private void DrawGoldPan(float sw, float sh)
        {
            GoldPanRect(sw, sh, out Vector2 c, out float R);
            Rect Circle(Vector2 p, float r) => new Rect(p.x - r, p.y - r, r * 2f, r * 2f);
            var mint = HudOk;

            // The pan itself, its water, sand and gold are the 3D Dulang (ARCapturePrototype.Dulang.cs); this draws the overlays.

            if (goldPhase >= GoldPhase.Panning)
            {
                DrawHudPill(new Rect(c.x - 70f, c.y - R - 50f, 140f, 40f), $"EMAS {GoldLeft()}/{GoldPieces}", Color.white);
            }

            var big = EnerGoUI.Title(60, Color.white);
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
                var card = new Rect(c.x - 100f, c.y - 62f, 200f, 132f);
                EnerGoUI.DrawCard(card);
                EnerGoUI.DrawRibbon(card, "SIAP?", ButtonKind.Orange);
                EnerGoUI.DrawStrokedLabel(new Rect(card.x, card.y + 34f, card.width, 80f), n.ToString(), big, EnerGoUI.M(3f), EnerGoUI.M(3f));
            }
            else if (goldPhase == GoldPhase.Panning && Time.time - goldPhaseAt < 0.8f)
            {
                GUI.color = new Color(1f, 1f, 1f, 1f - (Time.time - goldPhaseAt) / 0.8f);
                EnerGoUI.DrawStrokedLabel(new Rect(c.x - 150f, c.y - 40f, 300f, 80f), "MULAI!", big, EnerGoUI.M(3f), EnerGoUI.M(3f));
                GUI.color = Color.white;
            }
            else if (goldPhase == GoldPhase.Failed)
            {
                InitCardStyles();
                float w = Mathf.Min(340f, sw * 0.86f);
                var card = new Rect(c.x - w * 0.5f, c.y - 90f, w, 196f);
                EnerGoUI.DrawCard(card);
                EnerGoUI.DrawRibbon(card, "EMAS TERBUANG!", ButtonKind.Bad);
                GUI.Label(new Rect(card.x + 18f, card.y + 46f, card.width - 36f, 56f), "Gerakanmu terlalu kencang, emasnya tumpah dari dulang.", styleCardHint);
                if (DrawPrimaryBtn(new Rect(card.x + 18f, card.yMax - 78f, card.width - 36f, 60f), "CARI ENDAPAN BARU"))
                {
                    if (SFXManager.Instance != null) SFXManager.Instance.PlayButtonClick();
                    RetryGoldAtNewDeposit();
                }
            }
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

        private void InitStyles()
        {
            if (stylesInitialized && styleHeaderTag != null) return;

            styleHeaderTag = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = EnerGoUI.Sun }
            };

            styleHeaderVal = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };

            styleGoldVal = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };

            styleRadarAlert = new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = EnerGoUI.OrangeTop }
            };

            styleRadarLocked = new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = EnerGoUI.OkTop }
            };

            styleTapsCounter = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = Color.white }
            };

            styleBtnText = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = EnerGoUI.Ink }
            };

            styleBtnWarn = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = EnerGoUI.OrangeLip }
            };

            styleStatusSub = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Normal,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(1f, 1f, 1f, 0.85f) }
            };

            EnerGoUI.ApplyFont(styleHeaderTag, styleHeaderVal, styleGoldVal, styleRadarAlert, styleRadarLocked,
                styleTapsCounter, styleBtnText, styleBtnWarn, styleStatusSub);
            stylesInitialized = true;
        }

        private void OnGUI()
        {
            InitStyles();

            if (texWhite == null) CreateSolidTextures();

            var prevMatrix = GUI.matrix;
            var prevColor = GUI.color;

            float scale = HudScale;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));

            float sw = Screen.width / scale;
            float sh = Screen.height / scale;

            float safeTop = EnerGoUI.SafeTop(scale);

            // --- 1. TOP TELEMETRY BAR ---
            float pillH = 50f;

            // Return to Lobby Button (Left)
            var lobbyRect = new Rect(14f, safeTop, 90f, pillH);
            bool fromRadar = ExploreSession.PendingSpawnId != null;
            if (EnerGoProgress.Current.pendingCapture == null && DrawBtn(lobbyRect, fromRadar ? "‹  RADAR" : "‹  LOBBY", styleBtnText))
            {
                if (SFXManager.Instance != null) SFXManager.Instance.PlayButtonBack();
                ExploreSession.PendingSpawnId = null; // leaving without catching: the spawn stays on the radar
                SceneManager.LoadScene(fromRadar ? "ExploreScene" : "LobbyScene");
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
                uraniumPhase = UraniumPhase.None;
                oilPhase = OilPhase.None;
                nextSpawnTime = Time.time + 0.3f;
                feedback = isUsingGyro ? "BERALIH KE SENSOR 360" : "BERALIH KE ARCORE";
            }

            // Resource Badge (Right)
            float oreW = 105f;
            var oreRect = new Rect(sw - 14f - oreW, safeTop, oreW, pillH);
            DrawPill(oreRect, ResourceIds.Name(resourceId).ToUpperInvariant(), $"{EnerGoProgress.Current.Get(resourceId).Total} UNIT", styleHeaderTag, styleGoldVal);

            if (guide == null)
            {
            if (resourceId == ResourceIds.Sun && target != null && EnerGoProgress.Current.pendingCapture == null) DrawSunReticle(sw, sh);
            if (resourceId == ResourceIds.Coal && target != null && EnerGoProgress.Current.pendingCapture == null) DrawCoalPips(scale);
            if (goldPanning && EnerGoProgress.Current.pendingCapture == null) DrawGoldPan(sw, sh);
            if (uraniumPhase != UraniumPhase.None && EnerGoProgress.Current.pendingCapture == null) DrawUraniumOverlay(sw, sh);
            if (oilPhase != OilPhase.None && EnerGoProgress.Current.pendingCapture == null) DrawOilOverlay(sw, sh);

            // --- 2. RADAR & TARGET DIRECTION HUD ---
            DrawRadarHUD(sw, safeTop + pillH + 10f);
            if (EnerGoProgress.Current.pendingCapture == null) DrawCompassStrip(sw, safeTop + pillH + 10f + 72f);
            }

            // --- 3. BOTTOM CONTROL BAR ---
            float botY = sh - 52f;
            float botBtnW = Mathf.Min(260f, sw - 32f);
            var resetRect = new Rect((sw - botBtnW) * 0.5f, botY, botBtnW, 40f);

            string resetLabel = "MUNCULKAN ULANG TARGET";
            if (guide == null && EnerGoProgress.Current.pendingCapture == null && DrawBtn(resetRect, resetLabel, styleBtnText))
            {
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

            // Hit Spark Ripple
            if (Time.time - lastHitTime < 0.35f)
            {
                // Expanding, fading ring where the finger landed.
                float t = (Time.time - lastHitTime) / 0.35f;
                Vector2 hpos = new Vector2(lastHitScreenPos.x, Screen.height - lastHitScreenPos.y) / scale;
                float r = Mathf.Lerp(12f, 34f, t);
                EnerGoUI.DrawPanel(new Rect(hpos.x - r, hpos.y - r, r * 2f, r * 2f), Color.clear, EnerGoUI.Alpha(EnerGoUI.Orange, 1f - t), r, 3f);
            }

            if (guide != null && guide.Draw(sw, sh, scale) == GuideViewer.Result.Start)
            {
                guide = null;
                nextSpawnTime = Time.time; // the mini-game (and the sun timer) starts now
            }
            if (EnerGoProgress.Current.pendingCapture != null) DrawAnalysis(sw, sh);
            GUI.color = prevColor;
            GUI.matrix = prevMatrix;
        }

        private void InitCardStyles()
        {
            if (cardStylesInit) return;
            styleCardStatement = EnerGoUI.Text(20, true, EnerGoUI.Ink, TextAnchor.MiddleCenter, wrap: true);
            styleCardHint = EnerGoUI.Text(16, false, EnerGoUI.Muted, TextAnchor.UpperCenter, wrap: true);
            styleChip = EnerGoUI.Text(14, true, EnerGoUI.Ink);
            cardStylesInit = true;
        }

        // Right/wrong per question of the current capture, filled in as each feedback card shows (null = not seen).
        private string quizMarksSession;
        private readonly bool?[] quizMarks = new bool?[8];
        private GUIStyle styleChip;

        // ✕ (false, left) and ✓ (true, right) under the quiz card, in GUI units.
        private Rect QuizButtonRect(bool answer, float sw, float sh)
        {
            float h = EnerGoUI.M(56f) * 0.75f, gap = 16f, w = (Mathf.Min(sw - 40f, 420f) - gap) * 0.5f;
            float x0 = (sw - w * 2f - gap) * 0.5f;
            return new Rect(answer ? x0 + w + gap : x0, sh - EnerGoUI.SafeBottom(HudScale) - h, w, h);
        }

        // Rounded white chip; returns its width. Measure-only when `draw` is false.
        private float DrawChip(float x, float y, string text, string icon, bool draw = true)
        {
            float iconW = icon != null ? 22f : 0f, h = 30f;
            float w = styleChip.CalcSize(new GUIContent(text)).x + iconW + 22f;
            if (!draw) return w;
            var r = new Rect(x, y, w, h);
            EnerGoUI.DrawPanel(new Rect(r.x, r.y + 3f, r.width, r.height), EnerGoUI.Ink, Color.clear, 10f);
            EnerGoUI.DrawPanel(r, Color.white, EnerGoUI.Ink, 10f, 2.5f);
            if (icon != null) EnerGoUI.DrawIcon(new Rect(r.x + 9f, r.y + 5f, 20f, 20f), icon);
            GUI.Label(new Rect(r.x + 9f + iconW, r.y, w - 18f - iconW, h), text, styleChip);
            return w;
        }

        // A centred row of chips, wrapping onto a second line when it is wider than `maxW`. Returns the bottom edge.
        private float DrawChipRow(float cx, float y, float maxW, string[] texts, string[] icons)
        {
            const float gap = 8f, rowH = 38f;
            int start = 0;
            while (start < texts.Length)
            {
                float rowW = 0f; int end = start;
                while (end < texts.Length)
                {
                    float cw = DrawChip(0, 0, texts[end], icons?[end], false);
                    if (end > start && rowW + gap + cw > maxW) break;
                    rowW += (end > start ? gap : 0f) + cw; end++;
                }
                float x = cx - rowW * 0.5f;
                for (int i = start; i < end; i++) x += DrawChip(x, y, texts[i], icons?[i]) + gap;
                y += rowH; start = end;
            }
            return y;
        }

        // Three progress dots (current = Sun, right = Ok ✓, wrong = Bad ✕) and the SOAL n/3 pill.
        private void DrawQuizTop(CaptureSession session, float sw, float top)
        {
            const float d = 30f, gap = 8f;
            for (int i = 0; i < session.QuestionCount && i < quizMarks.Length; i++)
            {
                var r = new Rect(20f + i * (d + gap), top + 8f, d, d);
                bool? mark = quizMarks[i];
                bool current = i == session.questionIndex && session.state == "awaiting-analysis";
                Color fill = mark == true ? EnerGoUI.Success : mark == false ? EnerGoUI.Danger : current ? EnerGoUI.Sun : EnerGoUI.DarkTop;
                EnerGoUI.DrawPanel(r, fill, EnerGoUI.Ink, d * 0.5f, 3f);
                if (mark != null) EnerGoUI.DrawIcon(new Rect(r.x + 7f, r.y + 7f, d - 14f, d - 14f), mark.Value ? "check" : "cross");
            }
            DrawHudPill(new Rect(sw - 20f - 130f, top, 130f, 46f), "SOAL " + (Mathf.Min(session.questionIndex + 1, session.QuestionCount)) + "/" + session.QuestionCount, EnerGoUI.Sun);
        }

        private void DrawAnalysis(float sw, float sh)
        {
            InitCardStyles();

            var session  = EnerGoProgress.Current.pendingCapture;
            var question = QuizBank.Find(session.questionId, session.resourceId);
            if (question == null) return;

            if (quizMarksSession != session.sessionId)
            {
                quizMarksSession = session.sessionId;
                for (int i = 0; i < quizMarks.Length; i++) quizMarks[i] = null;
            }
            if (session.state != "awaiting-analysis" && session.questionIndex < quizMarks.Length) quizMarks[session.questionIndex] = session.correct;

            EnerGoUI.DrawWorld(sw, sh, 0.45f);
            float top = EnerGoUI.SafeTop(HudScale);
            DrawQuizTop(session, sw, top);

            float w = Mathf.Min(sw - 40f, 400f), x = (sw - w) * 0.5f;
            float pad = 22f, resW = w - pad * 2f;
            string icon = EnerGoUI.ResourceIcon(session.resourceId);

            // ── FEEDBACK after each answer: right/wrong + the explanation ──
            if (session.state == "feedback")
            {
                float explainH = styleCardHint.CalcHeight(new GUIContent(question.explanation), resW);
                float cardH = 48f + 34f + 12f + 64f + 12f + explainH + (session.correct ? 46f : 0f) + 22f + 64f + pad;
                float y = Mathf.Max(top + 80f, (sh - cardH) * 0.5f);
                var card = new Rect(x, y, w, cardH);
                EnerGoUI.DrawCard(card);
                EnerGoUI.DrawRibbon(card, session.correct ? "BENAR!" : session.timedOut ? "WAKTU HABIS" : "SALAH", session.correct ? ButtonKind.Ok : ButtonKind.Bad);

                float cy2 = y + 48f;
                string verdict = question.correctAnswer ? "<color=#2F8F24>BENAR</color>" : "<color=#D42A45>SALAH</color>";
                DrawChipRow(card.center.x, cy2, resW, new[] { "Pernyataan ini " + verdict }, null);
                cy2 += 34f + 12f;
                EnerGoUI.DrawIcon(new Rect(card.center.x - 32f, cy2, 64f, 64f), icon);
                cy2 += 64f + 12f;
                GUI.Label(new Rect(x + pad, cy2, resW, explainH), question.explanation, styleCardHint);
                cy2 += explainH;
                if (session.correct) { DrawChipRow(card.center.x, cy2 + 14f, resW, new[] { "+1 unit" }, new[] { icon }); cy2 += 46f; }
                cy2 += 22f;

                // A wrong answer holds the button briefly so the explanation actually gets read.
                float held = session.correct ? 1f : Mathf.Clamp01((Time.time - resultShownAt) / 2f);
                string next = session.IsLastQuestion ? "LIHAT HASIL" : "SOAL BERIKUTNYA";
                var btn = new Rect(x + pad, cy2, resW, 64f);
                if (held >= 1f && DrawPrimaryBtn(btn, next))
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
                else if (held < 1f)
                {
                    var face = EnerGoUI.DrawButton(btn, ButtonKind.Off);
                    EnerGoUI.DrawPanel(new Rect(face.x + 4f, face.y + 4f, (face.width - 8f) * held, face.height - 8f), new Color(1f, 1f, 1f, 0.45f), Color.clear, 12f);
                    EnerGoUI.ButtonLabel(face, "BACA DULU...", 20, ButtonKind.Off);
                }

                if (!string.IsNullOrEmpty(EnerGoProgress.Error))
                    GUI.Label(new Rect(x, sh - 80f, w, 60f), EnerGoProgress.Error, styleCardHint);
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
                float msgH = styleCardHint.CalcHeight(new GUIContent(message), resW);
                string[] chips = { "Dasar +1", "Kuis +" + right, MiniGameName(session.resourceId) + " " + EnerGoProgress.GradeFor(session.performanceBonus) + " +" + session.performanceBonus };
                float cardH = 46f + 60f + 46f + 60f + 84f + msgH + 20f + 64f + pad;
                float y = Mathf.Max(top + 128f, (sh - cardH) * 0.5f + 30f);
                var card = new Rect(x, y, w, cardH);

                EnerGoUI.DrawStrokedLabel(new Rect(0, y - 122f, sw, 56f), UiLayout.ResultTitle(right, count), EnerGoUI.Title(46, Color.white), EnerGoUI.M(3f), EnerGoUI.M(4f));
                EnerGoUI.DrawCard(card);
                EnerGoUI.DrawRibbon(card, "HASIL ANALISIS", ButtonKind.Orange);

                float cy2 = y + 46f;
                int stars = UiLayout.Stars(right);
                for (int i = 0; i < 3; i++)
                {
                    float s = i == 1 ? 60f : 46f;
                    float sx = card.center.x + (i - 1) * 58f - s * 0.5f;
                    EnerGoUI.DrawIcon(new Rect(sx, cy2 + (i == 1 ? 0f : 12f), s, s), i < stars ? "star" : "star_empty");
                }
                cy2 += 60f;
                GUI.Label(new Rect(x, cy2, w, 46f), right + "/" + count + " BENAR", EnerGoUI.Title(36, EnerGoUI.Ink));
                cy2 += 46f;

                // Reward: icon, a big stroked +N, the quality chip.
                string plus = "+" + session.rewardQuantity;
                var plusStyle = EnerGoUI.Title(40, Color.white, TextAnchor.MiddleLeft);
                string quality = pure ? "Murni" : "Normal";
                float plusW = plusStyle.CalcSize(new GUIContent(plus)).x, qW = DrawChip(0, 0, quality, null, false);
                float rowW = 50f + 10f + plusW + 12f + qW, rx = card.center.x - rowW * 0.5f;
                EnerGoUI.DrawIcon(new Rect(rx, cy2 + 5f, 50f, 50f), icon);
                EnerGoUI.DrawStrokedLabel(new Rect(rx + 60f, cy2, plusW + 8f, 60f), plus, plusStyle, EnerGoUI.M(2.5f), EnerGoUI.M(2f));
                DrawChip(rx + 60f + plusW + 12f, cy2 + 15f, quality, null);
                cy2 += 60f + 8f;

                // Where the units came from: 1 base + 1 per correct answer + the mini-game bonus.
                cy2 = DrawChipRow(card.center.x, cy2, resW, chips, null) + 8f;
                GUI.Label(new Rect(x + pad, cy2, resW, msgH), message, styleCardHint);
                cy2 += msgH + 20f;

                bool backToRadar = !string.IsNullOrEmpty(session.spawnId);
                if (DrawPrimaryBtn(new Rect(x + pad, cy2, resW, 64f), backToRadar ? "LANJUT BERBURU" : "LANJUT KE LOBBY"))
                {
                    if (SFXManager.Instance != null) SFXManager.Instance.PlayButtonClick();
                    if (EnerGoProgress.ClearResult())
                    {
                        cardOffset   = Vector2.zero;
                        cardFlicking = false;
                        cardReturning = false;
                        SceneManager.LoadScene(backToRadar ? "ExploreScene" : "LobbyScene");
                    }
                }

                if (!string.IsNullOrEmpty(EnerGoProgress.Error))
                    GUI.Label(new Rect(x, sh - 80f, w, 60f), EnerGoProgress.Error, styleCardHint);
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

            // Title: BENAR ATAU SALAH?, never tilted.
            var tWhite = EnerGoUI.Title(34, Color.white, TextAnchor.MiddleLeft);
            var tOrange = EnerGoUI.Title(34, EnerGoUI.TitleOrange, TextAnchor.MiddleLeft);
            float w1 = tWhite.CalcSize(new GUIContent("BENAR ")).x, w2 = tOrange.CalcSize(new GUIContent("ATAU ")).x, w3 = tWhite.CalcSize(new GUIContent("SALAH?")).x;
            float tx = (sw - w1 - w2 - w3) * 0.5f, ty = top + 62f;
            EnerGoUI.DrawStrokedLabel(new Rect(tx, ty, w1 + 4f, 46f), "BENAR ", tWhite, EnerGoUI.M(2.5f), EnerGoUI.M(3f));
            EnerGoUI.DrawStrokedLabel(new Rect(tx + w1, ty, w2 + 4f, 46f), "ATAU ", tOrange, EnerGoUI.M(2.5f), EnerGoUI.M(3f));
            EnerGoUI.DrawStrokedLabel(new Rect(tx + w1 + w2, ty, w3 + 4f, 46f), "SALAH?", tWhite, EnerGoUI.M(2.5f), EnerGoUI.M(3f));

            // Answer buttons: the same answer path as a swipe (input is read in HandleCardSwipeInput).
            for (int i = 0; i < 2; i++)
            {
                bool answer = i == 1;
                var br = QuizButtonRect(answer, sw, sh);
                var kind = answer ? ButtonKind.Ok : ButtonKind.Bad;
                var face = EnerGoUI.DrawButton(br, kind, !cardFlicking && EnerGoUI.IsHeld(br, HudScale));
                float iconS = face.height * 0.42f;
                var label = EnerGoUI.Title(24, Color.white, TextAnchor.MiddleLeft);
                string text = answer ? "BENAR" : "SALAH";
                float lw = label.CalcSize(new GUIContent(text)).x, gx = face.center.x - (iconS + 8f + lw) * 0.5f;
                EnerGoUI.DrawIcon(new Rect(gx, face.center.y - iconS * 0.5f, iconS, iconS), answer ? "check" : "cross");
                EnerGoUI.DrawStrokedLabel(new Rect(gx + iconS + 8f, face.y, lw + 6f, face.height - 2f), text, label, EnerGoUI.M(2f), EnerGoUI.M(1.5f));
            }

            // Card dimensions: between the title and the answer buttons.
            float btnTop = QuizButtonRect(true, sw, sh).y;
            float cw = Mathf.Min(sw - 40f, 400f);
            float ch = Mathf.Min(btnTop - 36f - (ty + 66f), 440f);
            float baseX = (sw - cw) * 0.5f, baseY = ty + 66f + (btnTop - 36f - (ty + 66f) - ch) * 0.5f;

            // The next card peeking out behind: the only resting tilt on this screen.
            var stackMatrix = GUI.matrix;
            GUIUtility.RotateAroundPivot(3f, new Vector2(baseX + cw * 0.5f, baseY + ch * 0.5f));
            EnerGoUI.DrawPanel(new Rect(baseX + 8f, baseY + 10f, cw - 16f, ch), EnerGoUI.Ink, Color.clear, EnerGoUI.M(24f));
            EnerGoUI.DrawPanel(new Rect(baseX + 8f, baseY + 4f, cw - 16f, ch - 4f), EnerGoUI.PaperBot, EnerGoUI.Ink, EnerGoUI.M(24f), EnerGoUI.M(5f));
            GUI.matrix = stackMatrix;

            // Screen edge tints toward the side the card is going.
            float swipeRatio = Mathf.Clamp01(Mathf.Abs(cardOffset.x) / SwipeThreshold);
            if (Mathf.Abs(cardOffset.x) > 4f)
            {
                bool right = cardOffset.x > 0f;
                Color edge = EnerGoUI.Alpha(right ? EnerGoUI.Success : EnerGoUI.Danger, 0.35f * swipeRatio);
                EnerGoUI.DrawPanel(new Rect(right ? sw * 0.5f : 0f, 0f, sw * 0.5f, sh), edge, Color.clear, 0f);
            }

            float cx = baseX + cardOffset.x;
            float cy = baseY + cardOffset.y;

            // Rotation while dragged (clamped to ±18°)
            float rotDeg = Mathf.Clamp(cardOffset.x * 0.08f, -18f, 18f);
            var prevMatrix = GUI.matrix;
            GUIUtility.RotateAroundPivot(rotDeg, new Vector2(cx + cw * 0.5f, cy + ch * 0.5f));

            var cardRect = new Rect(cx, cy, cw, ch);
            Color? border = Mathf.Abs(cardOffset.x) < 4f ? (Color?)null
                : Color.Lerp(EnerGoUI.Ink, cardOffset.x > 0f ? EnerGoUI.Success : EnerGoUI.Danger, swipeRatio);
            EnerGoUI.DrawCard(cardRect, -1f, border);

            // ── Timer: clock, bar and seconds; turns red for the last 5 s ──
            float innerX = cx + pad, innerW = cw - pad * 2f;
            float left = QuizTimeLeft();
            bool hurry = left <= 5f;
            EnerGoUI.DrawIcon(new Rect(innerX, cy + 20f, 26f, 26f), "clock");
            EnerGoUI.DrawBar(new Rect(innerX + 34f, cy + 24f, innerW - 34f - 40f, 18f), left / QuizSecondsPerQuestion,
                hurry ? EnerGoUI.BadTop : EnerGoUI.OrangeTop, hurry ? EnerGoUI.BadBot : EnerGoUI.OrangeBot);
            GUI.Label(new Rect(innerX + innerW - 34f, cy + 18f, 34f, 30f), Mathf.CeilToInt(left).ToString(), EnerGoUI.Title(22, hurry ? EnerGoUI.Danger : EnerGoUI.Ink));

            // ── Resource icon and statement ──
            float iconSize = Mathf.Min(EnerGoUI.M(74f) * 0.75f, ch * 0.24f);
            EnerGoUI.DrawIcon(new Rect(cx + cw * 0.5f - iconSize * 0.5f, cy + 66f, iconSize, iconSize), icon);
            float stTop = cy + 66f + iconSize + 12f;
            GUI.Label(new Rect(innerX, stTop, innerW, cy + ch - 22f - stTop), question.statement, styleCardStatement);

            // ── BENAR / SALAH stamp while dragged ──
            if (Mathf.Abs(cardOffset.x) > 4f)
            {
                bool right = cardOffset.x > 0f;
                Color sc = right ? EnerGoUI.Success : EnerGoUI.Danger;
                var stampStyle = EnerGoUI.Title(36, sc); // fixed colour: Title() caches per colour, fade with GUI.color
                var prevTint = GUI.color;
                GUI.color = new Color(1f, 1f, 1f, swipeRatio);
                string st = right ? "BENAR" : "SALAH";
                float sw2 = stampStyle.CalcSize(new GUIContent(st)).x + 28f;
                var sr = new Rect(right ? cx + 20f : cx + cw - 20f - sw2, cy + 56f, sw2, 56f);
                GUIUtility.RotateAroundPivot(right ? -14f : 14f, sr.center);
                EnerGoUI.DrawPanel(sr, new Color(1f, 1f, 1f, 0.8f), sc, 12f, 5f);
                GUI.Label(sr, st, stampStyle);
                GUI.color = prevTint;
            }

            GUI.matrix = prevMatrix;

            if (!string.IsNullOrEmpty(EnerGoProgress.Error))
            {
                GUI.Label(new Rect(x, sh - 80f, w, 60f), EnerGoProgress.Error, styleCardHint);
            }
        }

        private static readonly Color HudFill = EnerGoUI.PillBg;
        private static readonly Color HudOutline = EnerGoUI.Ink;
        private static readonly Color HudWarn = EnerGoUI.Orange;
        private static readonly Color HudOk = EnerGoUI.Success;

        private void DrawRadarHUD(float sw, float hudY)
        {
            float hudW = sw - 32f;
            float hudX = 16f;
            Color accent = HudOutline;
            string line1 = null, line2 = null;
            GUIStyle style1 = styleStatusSub;
            bool locked = false;
            if (uraniumPhase != UraniumPhase.None) { DrawUraniumHud(hudX, hudY, hudW); return; }
            if (oilPhase != OilPhase.None) { DrawOilHud(hudX, hudY, hudW); return; }

            if (goldPanning)
            {
                bool warn = GoldWarning();
                line1 = goldPhase == GoldPhase.Leveling ? "LETAKKAN HP MENDATAR"
                    : goldPhase == GoldPhase.Countdown ? "SIAP-SIAP MENDULANG"
                    : goldPhase == GoldPhase.Done ? "EMAS TERPISAH DARI PASIR!"
                    : goldPhase == GoldPhase.Failed ? "EMAS TERBUANG!"
                    : goldRough ? "TERLALU KENCANG! EMAS BISA TUMPAH" : "PUTAR HP PERLAHAN SEPERTI MENDULANG";
                style1 = warn ? styleRadarAlert : styleRadarLocked;
                accent = warn ? HudWarn : HudOk;
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
                if (resourceId == ResourceIds.Sun && Mathf.Abs(signedAngle) > 28f)
                {
                    line1 = signedAngle < 0 ? $"‹  PUTAR KE KIRI  {deg}°" : $"PUTAR KE KANAN  {deg}°  ›";
                    line2 = "SISA " + Mathf.Max(0, Mathf.CeilToInt(SunTimeLeft())) + " DTK";
                    style1 = styleRadarAlert;
                    accent = HudWarn;
                }
                else if (resourceId == ResourceIds.Sun)
                {
                    int left = Mathf.Max(0, Mathf.CeilToInt(SunTimeLeft()));
                    line1 = (sunInside ? "CAHAYA TERKUNCI" : "ARAHKAN KE CAHAYA") + " · SISA " + left + " DTK";
                    bool hurry = left <= 5 || !sunInside;
                    style1 = hurry ? styleRadarAlert : styleRadarLocked;
                    accent = hurry ? HudWarn : HudOk;
                    locked = true;
                }
                else if (resourceId == ResourceIds.Coal)
                {
                    line1 = "PECAHKAN BATU BARA · KETUK BONGKAHNYA";
                    style1 = styleRadarLocked;
                    accent = HudOk;
                    locked = true;
                }
                else
                {
                    line1 = "ENDAPAN EMAS DITEMUKAN";
                    line2 = "Ketuk endapannya untuk menyendok pasir";
                    style1 = styleRadarLocked;
                    accent = HudOk;
                }
            }
            else
            {
                line1 = feedback;
            }

            bool twoLines = line2 != null || locked;
            var box = new Rect(hudX, hudY, hudW, twoLines ? 64f : 42f);
            DrawHudBox(box, accent == HudOutline ? (Color?)null : accent);

            GUI.Label(new Rect(hudX + 10, hudY + (twoLines ? 8f : 11f), hudW - 20, 20), line1, style1);
            if (line2 != null) GUI.Label(new Rect(hudX + 10, hudY + 34, hudW - 20, 18), line2, styleStatusSub);
            var mint = HudOk;
            if (locked && resourceId == ResourceIds.Sun)
                DrawHudBar(hudX + 20f, hudY + 36f, hudW - 40f, sunProgress, sunInside ? mint : HudWarn, Mathf.FloorToInt(sunProgress * 100f) + "%");
            else if (locked && goldPanning)
            {
                // While levelling the bar shows how long the phone has been held flat.
                float v = goldPhase == GoldPhase.Leveling ? goldFlatHold / GoldFlatHoldSec : goldProgress;
                DrawHudBar(hudX + 20f, hudY + 36f, hudW - 40f, v, GoldWarning() ? HudWarn : mint, Mathf.FloorToInt(Mathf.Clamp01(v) * 100f) + "%");
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

        private GUIStyle stripLabelStyle;

        // HUD compass: ticks every 15°, U/TL/T/... every 45°, the view direction in the centre, and the SDA marker
        // (a dot, or for uranium only a rough green sector). Off-strip markers show "‹ 75°" / "75° ›" at the edge.
        private void DrawCompassStrip(float sw, float y)
        {
            if (compass == null || arCamera == null) return;
            if (stripLabelStyle == null) stripLabelStyle = new GUIStyle(styleTapsCounter) { alignment = TextAnchor.MiddleCenter };
            var r = new Rect(16f, y, sw - 32f, 34f);
            DrawHudBox(r);
            float half = CompassMath.StripHalfSpanDeg, heading = compass.HeadingDeg;
            float X(float x01) => r.center.x + x01 * (r.width * 0.5f - 14f);

            if (!compass.HasCompass)
                GUI.Label(new Rect(r.x, r.y, r.width, r.height), "KOMPAS TIDAK TERSEDIA", stripLabelStyle);
            else
                for (int d = 0; d < 360; d += 15)
                {
                    float x01 = CompassMath.StripX(d, heading, half);
                    if (Mathf.Abs(x01) > 1f) continue;
                    if (d % 45 == 0) GUI.Label(new Rect(X(x01) - 16f, r.y + 2f, 32f, r.height - 4f), CompassMath.DirectionLabel(d), stripLabelStyle);
                    else EnerGoUI.DrawPanel(new Rect(X(x01) - 1f, r.y + 12f, 2f, 10f), new Color(1f, 1f, 1f, 0.5f), Color.clear, 1f);
                }

            if (resourceId == ResourceIds.Uranium)
            {
                if (uraniumPhase == UraniumPhase.Search) // fragments are found with the RAD meter, not the strip
                {
                    float c = CompassMath.StripX(captureBearing, heading, half), w = CompassMath.UraniumSectorHalfDeg / half;
                    float lo = Mathf.Max(-1f, c - w), hi = Mathf.Min(1f, c + w);
                    if (hi > lo) EnerGoUI.DrawPanel(new Rect(X(lo), r.y + 4f, X(hi) - X(lo), r.height - 8f), EnerGoUI.Alpha(EnerGoUI.Success, 0.3f), Color.clear, 6f);
                    else StripEdge(r, c, Mathf.Abs(Mathf.DeltaAngle(heading, captureBearing)) - CompassMath.UraniumSectorHalfDeg);
                }
            }
            else if (resourceId != ResourceIds.Sun && target != null)
            {
                Vector3 to = RadarTargetPosition() - arCamera.transform.position;
                float bearing = CompassMath.CameraHeading(Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg, compass.OffsetDeg);
                float x01 = CompassMath.StripX(bearing, heading, half);
                if (Mathf.Abs(x01) <= 1f) EnerGoUI.DrawPanel(new Rect(X(x01) - 6f, r.center.y - 6f, 12f, 12f), EnerGoUI.Orange, EnerGoUI.Ink, 6f, 1.5f);
                else StripEdge(r, x01, Mathf.Abs(Mathf.DeltaAngle(heading, bearing)));
            }
            if (compass.HasCompass) EnerGoUI.DrawPanel(new Rect(r.center.x - 1.5f, r.y + 3f, 3f, r.height - 6f), EnerGoUI.Sun, Color.clear, 1.5f); // view direction
        }

        private void StripEdge(Rect r, float x01, float deg)
        {
            string t = x01 < 0f ? "‹ " + Mathf.RoundToInt(deg) + "°" : Mathf.RoundToInt(deg) + "° ›";
            var cell = x01 < 0f ? new Rect(r.x + 4f, r.y, 64f, r.height) : new Rect(r.xMax - 68f, r.y, 64f, r.height);
            EnerGoUI.DrawPanel(new Rect(cell.x, r.y + 5f, cell.width, r.height - 10f), EnerGoUI.Orange, Color.clear, 8f);
            GUI.Label(cell, t, stripLabelStyle);
        }

        // Horizontal progress bar with a short label (percentage or count) on the right.
        private void DrawHudBar(float x, float y, float w, float progress, Color fillCol, string label)
        {
            float labelW = 48f;
            var track = new Rect(x, y, w - labelW - 8f, 18f);
            if (fillCol == HudOk) EnerGoUI.DrawBar(track, progress, EnerGoUI.OkTop, EnerGoUI.OkBot);
            else if (fillCol == HudWarn) EnerGoUI.DrawBar(track, progress, EnerGoUI.OrangeTop, EnerGoUI.OrangeBot);
            else EnerGoUI.DrawBar(track, progress, fillCol);
            EnerGoUI.DrawStrokedLabel(new Rect(track.xMax + 8f, y - 1f, labelW, 20f), label, EnerGoUI.Title(16, Color.white, TextAnchor.MiddleLeft), 1.5f);
        }

        // Dark rounded box with an Ink outline: the base for every HUD panel over the camera.
        // `accent` (warn/ok) draws a thin coloured ring just inside the outline.
        private static void DrawHudBox(Rect r, Color? accent = null)
        {
            float rad = Mathf.Min(EnerGoUI.M(16f), r.height * 0.5f), w = EnerGoUI.M(2.5f);
            EnerGoUI.DrawPanel(r, HudFill, Color.clear, rad);
            if (accent != null)
                EnerGoUI.DrawPanel(new Rect(r.x + w, r.y + w, r.width - w * 2f, r.height - w * 2f), Color.clear, accent.Value, rad - w, 2.5f);
            EnerGoUI.DrawPanel(r, Color.clear, EnerGoUI.Ink, rad, w);
        }

        // One-line pill over the camera; the text shrinks to fit instead of clipping.
        private void DrawHudPill(Rect r, string text, Color textColor)
        {
            var inner = EnerGoUI.DrawPill(r);
            var style = EnerGoUI.Title(Mathf.RoundToInt(r.height * 0.42f), textColor);
            int fit = EnerGoUI.FitSize(style, text, inner.width, 11);
            GUI.Label(inner, text, fit == style.fontSize ? style : EnerGoUI.Title(fit, textColor));
        }

        // Remaining-hit pips floating above each coal lump: filled = hits landed.
        private void DrawCoalPips(float scale)
        {
            var done = EnerGoUI.Orange;
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
                        hit ? done : EnerGoUI.Alpha(EnerGoUI.Paper, 0.9f), hit ? done : EnerGoUI.Ink, 3f);
                }
            }
        }

        // Fixed aiming ring in the middle of the screen: a dotted white ring with Ink edges; Sun glow while the light is inside.
        private void DrawSunReticle(float sw, float sh)
        {
            float r = ReticleRadius * (sunInside ? 1f + 0.04f * Mathf.Sin(Time.time * 8f) : 1f);
            var c = new Vector2(sw * 0.5f, sh * 0.5f);
            if (sunInside) EnerGoUI.DrawPanel(new Rect(c.x - r, c.y - r, r * 2f, r * 2f), EnerGoUI.Alpha(EnerGoUI.Sun, 0.35f), Color.clear, r);
            Color dot = sunInside ? EnerGoUI.Sun : Color.white;
            const int dots = 28;
            const float d = 8f;
            for (int i = 0; i < dots; i++)
            {
                float a = i * Mathf.PI * 2f / dots;
                var p = c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
                EnerGoUI.DrawPanel(new Rect(p.x - d * 0.5f, p.y - d * 0.5f, d, d), dot, EnerGoUI.Ink, d * 0.5f, 2f);
            }
            // Small centre dot to aim with
            EnerGoUI.DrawPanel(new Rect(c.x - 5f, c.y - 5f, 10f, 10f), dot, EnerGoUI.Ink, 5f, 2f);
        }


        // Dark pill with a small Sun tag over a white value.
        private void DrawPill(Rect rect, string tag, string val, GUIStyle sTag, GUIStyle sVal)
        {
            var inner = EnerGoUI.DrawPill(rect);
            GUI.Label(new Rect(inner.x, rect.y + 5f, inner.width, 15f), tag, sTag);
            var v = EnerGoUI.Title(17, Color.white);
            int fit = EnerGoUI.FitSize(v, val, inner.width, 11);
            GUI.Label(new Rect(inner.x, rect.y + 19f, inner.width, rect.height - 23f), val, fit == v.fontSize ? v : EnerGoUI.Title(fit, Color.white));
        }

        // Small Paper button with an Ink Lilita label (lobby/radar back, mode switch, respawn).
        private bool DrawBtn(Rect rect, string text, GUIStyle style)
        {
            var face = EnerGoUI.DrawButton(rect, ButtonKind.Paper, EnerGoUI.IsHeld(rect, HudScale));
            EnerGoUI.ButtonLabel(face, text, 16, ButtonKind.Paper);
            return ButtonHit(rect);
        }

        private static string MiniGameName(string id) =>
            id == ResourceIds.Sun ? "Ekstraksi" : id == ResourceIds.Coal ? "Tambang" : id == ResourceIds.Uranium ? "Pemburu Geiger" : id == ResourceIds.Oil ? "Bor & Pompa" : "Dulang";

        private bool DrawPrimaryBtn(Rect rect, string text)
        {
            EnerGoUI.ButtonLabel(EnerGoUI.DrawPrimaryButton(rect, EnerGoUI.IsHeld(rect, HudScale)), text, 20, ButtonKind.Orange);
            return ButtonHit(rect);
        }

        private bool ButtonHit(Rect rect) => EnerGoUI.TapHit(rect, HudScale);
    }
}
