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
        private int lastTouchButtonFrame = -1;
        private float modeInitTimer = 0f;
        private const float ArCheckTimeout = 2.0f;

        // Target & Spawning
        private GameObject target;
        private GameObject goldModel;
        private Material goldMaterial;
        private int taps = 0;
        private string resourceId;
        private float resultShownAt;
        private int touches = 0;
        private float nextSpawnTime = 0f;
        private const int RequiredTaps = 5;

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
        private Texture2D texBtnSec;

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
        }

        private void OnApplicationPause(bool paused) { if (paused && EnerGoProgress.Current != null) EnerGoProgress.Commit(EnerGoProgress.Current); }

        private void Awake()
        {
            Application.targetFrameRate = 60;
            var save = EnerGoProgress.Current;
            resourceId = save.pendingCapture != null ? save.pendingCapture.resourceId : ResourceIds.Selected;
            if (resourceId != ResourceIds.Sun && resourceId != ResourceIds.Coal && resourceId != ResourceIds.Gold) resourceId = ResourceIds.Gold;
            if (save.pendingCapture != null && save.pendingCapture.state == "result") resultShownAt = Time.time;

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
            texBtnSec = Resources.Load<Texture2D>("UI/btn_secondary");
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
        }

        private void Update()
        {
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
                SpawnGold(isRandomAngle: false);
            }

            // 4. Idle Target Animation
            if (target != null)
            {
                target.transform.Rotate(0f, 28f * Time.deltaTime, 0f, Space.Self);
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
                else if (touch.phase == UnityEngine.InputSystem.TouchPhase.Moved && isDraggingLook && isUsingGyro)
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
                // Commit answer: left = true (BENAR), right = false (SALAH)
                bool answer = cardOffset.x < 0f;
                cardFlicking = true;
                // Slight delay via coroutine is not available in OnGUI; we'll
                // commit immediately and let the flick animation play for one frame.
                if (EnerGoProgress.Answer(answer))
                {
                    resultShownAt = Time.time;
                }
            }
            else
            {
                cardReturning = true;
            }
        }

        private bool CheckTapOnTarget(Vector2 screenPosition)
        {
            if (EnerGoProgress.Current.pendingCapture != null) return false;
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

            if (!TryGetBounds(target, out var bounds)) return false;

            Vector3 center = arCamera.WorldToScreenPoint(bounds.center);
            if (center.z <= arCamera.nearClipPlane || !arCamera.pixelRect.Contains((Vector2)center))
            {
                feedback = "TARGET DI LUAR JANGKAUAN // IKUTI ARAH RADAR";
                return false;
            }

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

            // Generous tap target for mobile touchscreens
            Vector2 halfSize = Vector2.Max((max - min) * 0.5f, Vector2.one * (38f * scale));
            Vector2 screenCenter = (min + max) * 0.5f;
            min = screenCenter - halfSize - Vector2.one * (18f * scale);
            max = screenCenter + halfSize + Vector2.one * (18f * scale);

            if (Rect.MinMaxRect(min.x, min.y, max.x, max.y).Contains(screenPosition))
            {
                // Hit confirmed
                taps++;
                lastHitTime = Time.time;
                lastHitScreenPos = screenPosition;

                if (taps < RequiredTaps)
                {
                    feedback = $"EKSTRAKSI BERHASIL: {taps}/{RequiredTaps} PULSA";
                }
                else
                {
                    if (EnerGoProgress.BeginAnalysis(resourceId))
                    {
                        Destroy(target);
                        target = null;
                        feedback = "ANALISIS SIAP";
                    }
                    else
                    {
                        taps = RequiredTaps - 1;
                        feedback = EnerGoProgress.Error ?? "Gagal membuka analisis. Coba lagi.";
                    }
                }
                return true;
            }

            return false;
        }

        private void SpawnGold(bool isRandomAngle)
        {
            taps = 0;
            target = new GameObject("Target " + ResourceIds.Name(resourceId));
            target.transform.position = Vector3.zero;
            target.transform.rotation = Quaternion.identity;

            GameObject visual;
            if (resourceId == ResourceIds.Gold) visual = Instantiate(goldModel, target.transform);
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
            target.transform.localScale = Vector3.one * (0.38f / longestSide);
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

            feedback = isRandomAngle ? "TARGET TERDETEKSI // PUTAR PERANGKAT" : ResourceIds.Name(resourceId) + " SIAP DIKUMPULKAN";
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
                alignment = TextAnchor.UpperLeft,
                normal = { textColor = new Color(0.35f, 0.85f, 0.96f) }
            };

            styleHeaderVal = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.LowerLeft,
                normal = { textColor = Color.white }
            };

            styleGoldVal = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.LowerLeft,
                normal = { textColor = new Color(1f, 0.80f, 0.25f) }
            };

            styleRadarAlert = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.98f, 0.75f, 0.15f) }
            };

            styleRadarLocked = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.12f, 0.92f, 0.68f) }
            };

            styleTapsCounter = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };

            styleBtnText = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.80f, 0.92f, 0.98f) }
            };

            styleBtnWarn = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(1f, 0.70f, 0.30f) }
            };

            styleStatusSub = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                fontStyle = FontStyle.Normal,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.55f, 0.72f, 0.82f) }
            };

            stylesInitialized = true;
        }

        private void OnGUI()
        {
            InitStyles();

            if (texPill == null || texBtnSec == null) LoadTextures();
            if (texWhite == null) CreateSolidTextures();

            var prevMatrix = GUI.matrix;
            var prevColor = GUI.color;

            float scale = HudScale;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));

            float sw = Screen.width / scale;
            float sh = Screen.height / scale;

            float safeTop = Mathf.Max(12f, (Screen.height - Screen.safeArea.yMax) / scale + 6f);

            // --- 1. TOP TELEMETRY BAR ---
            float pillH = 38f;

            // Return to Lobby Button (Left)
            var lobbyRect = new Rect(14f, safeTop, 90f, pillH);
            if (EnerGoProgress.Current.pendingCapture == null && DrawBtn(lobbyRect, "← LOBBY", styleBtnText))
            {
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

            // --- 2. RADAR & TARGET DIRECTION HUD ---
            DrawRadarHUD(sw, sh);

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
                float hitAlpha = 1.0f - ((Time.time - lastHitTime) / 0.35f);
                Vector2 hpos = new Vector2(lastHitScreenPos.x, Screen.height - lastHitScreenPos.y) / scale;
                GUI.color = new Color(0.12f, 0.95f, 0.75f, hitAlpha);
                GUI.DrawTexture(new Rect(hpos.x - 22, hpos.y - 22, 44, 44), texWhite);
                GUI.color = Color.white;
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
                fontSize       = 10,
                fontStyle      = FontStyle.Normal,
                alignment      = TextAnchor.MiddleCenter,
                wordWrap       = false,
                normal         = { textColor = new Color(0.50f, 0.68f, 0.80f) }
            };
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

            var heading = new GUIStyle(styleRadarLocked) { alignment = TextAnchor.MiddleLeft, fontSize = 14, wordWrap = true };
            var body    = new GUIStyle(styleStatusSub)   { alignment = TextAnchor.UpperLeft,  fontSize = 14, wordWrap = true };

            // ── RESULT SCREEN (unchanged logic) ──────────────────────────
            if (session.state == "result")
            {
                float x = 24f, w = sw - 48f, y = Mathf.Max(120f, sh * 0.25f);
                GUI.Label(new Rect(x, y - 40f, w, 30f), "HASIL ANALISIS", heading);
                GUI.Label(new Rect(x, y, w, 45f),
                    (session.correct ? "TEPAT · MURNI" : "BELUM TEPAT · NORMAL") + "  +" + session.rewardQuantity + " unit", heading);
                GUI.Label(new Rect(x, y + 55f, w, 120f), question.explanation, body);
                bool canContinue = session.correct || Time.time - resultShownAt >= 3f;
                if (canContinue && DrawBtn(new Rect(x, y + 190f, w, 48f), "LANJUT KE LOBBY", styleBtnText))
                {
                    if (EnerGoProgress.ClearResult())
                    {
                        cardOffset   = Vector2.zero;
                        cardFlicking = false;
                        cardReturning = false;
                        SceneManager.LoadScene("LobbyScene");
                    }
                }
                else if (!canContinue)
                    GUI.Label(new Rect(x, y + 190f, w, 40f), "Baca penjelasan sebelum melanjutkan...", body);

                if (!string.IsNullOrEmpty(EnerGoProgress.Error))
                    GUI.Label(new Rect(x, sh - 80f, w, 60f), EnerGoProgress.Error, body);
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
            GUI.color = new Color(0f, 0f, 0f, shadowAlpha);
            GUI.DrawTexture(new Rect(cx + 6f, cy + 8f, cw, ch), texWhite);

            // ── Card face ──
            GUI.color = new Color(0.06f, 0.14f, 0.22f, 0.98f);
            GUI.DrawTexture(new Rect(cx, cy, cw, ch), texWhite);

            // Border tint changes with swipe direction
            float swipeRatio = Mathf.Clamp01(Mathf.Abs(cardOffset.x) / SwipeThreshold);
            bool  goingLeft  = cardOffset.x < 0f;
            Color borderCol  = goingLeft
                ? Color.Lerp(new Color(0.20f, 0.75f, 0.55f, 0.30f), new Color(0.12f, 0.95f, 0.62f, 0.90f), swipeRatio)
                : Color.Lerp(new Color(0.75f, 0.20f, 0.20f, 0.30f), new Color(0.98f, 0.32f, 0.32f, 0.90f), swipeRatio);
            GUI.color = borderCol;
            GUI.DrawTexture(new Rect(cx,          cy,          cw,   2f), texWhite);
            GUI.DrawTexture(new Rect(cx,          cy + ch - 2, cw,   2f), texWhite);
            GUI.DrawTexture(new Rect(cx,          cy,          2f, ch   ), texWhite);
            GUI.DrawTexture(new Rect(cx + cw - 2, cy,          2f, ch   ), texWhite);
            GUI.color = Color.white;

            // ── Resource tag ──
            float innerX = cx + 16f;
            float innerW = cw - 32f;
            GUI.Label(new Rect(innerX, cy + 14f, innerW, 16f),
                "ANALISIS · " + ResourceIds.Name(resourceId).ToUpperInvariant(), styleHeaderTag);

            // ── Statement text ──
            GUI.Label(new Rect(innerX, cy + 38f, innerW, ch - 120f), question.statement, styleCardStatement);

            // ── Hint ──
            GUI.Label(new Rect(innerX, cy + ch - 36f, innerW, 20f),
                "← geser kiri: BENAR   |   geser kanan: SALAH →", styleCardHint);

            GUI.matrix = prevMatrix;

            // ── BENAR overlay (left swipe) ──
            if (cardOffset.x < -4f)
            {
                float a = Mathf.Clamp01(-cardOffset.x / SwipeThreshold);
                Color benarCol = new Color(0.12f, 0.95f, 0.62f, a);
                // Left badge
                GUI.color = new Color(benarCol.r, benarCol.g, benarCol.b, a * 0.18f);
                GUI.DrawTexture(new Rect(cx, cy, cw * 0.5f, ch), texWhite);
                GUI.color = Color.white;
                styleSwipeBenar.normal.textColor = benarCol;
                GUI.Label(new Rect(cx + 8f, cy, cw * 0.45f, ch), "✓ BENAR", styleSwipeBenar);
            }

            // ── SALAH overlay (right swipe) ──
            if (cardOffset.x > 4f)
            {
                float a = Mathf.Clamp01(cardOffset.x / SwipeThreshold);
                Color salahCol = new Color(0.98f, 0.32f, 0.32f, a);
                GUI.color = new Color(salahCol.r, salahCol.g, salahCol.b, a * 0.18f);
                GUI.DrawTexture(new Rect(cx + cw * 0.5f, cy, cw * 0.5f, ch), texWhite);
                GUI.color = Color.white;
                styleSwipeSalah.normal.textColor = salahCol;
                GUI.Label(new Rect(cx + cw * 0.55f, cy, cw * 0.45f, ch), "✗ SALAH", styleSwipeSalah);
            }

            if (!string.IsNullOrEmpty(EnerGoProgress.Error))
            {
                float x = 24f, w = sw - 48f;
                GUI.Label(new Rect(x, sh - 80f, w, 60f), EnerGoProgress.Error, body);
            }
        }

        private void DrawRadarHUD(float sw, float sh)
        {
            float hudY = 62f;
            float hudW = sw - 32f;
            float hudX = 16f;

            // Reticle status box
            GUI.color = new Color(0.06f, 0.15f, 0.22f, 0.82f);
            GUI.DrawTexture(new Rect(hudX, hudY, hudW, 64f), texWhite);
            GUI.color = Color.white;

            if (target != null && arCamera != null)
            {
                Vector3 toTarget = target.transform.position - arCamera.transform.position;
                Vector3 flatToTarget = new Vector3(toTarget.x, 0, toTarget.z).normalized;
                Vector3 flatCamFwd = new Vector3(arCamera.transform.forward.x, 0, arCamera.transform.forward.z).normalized;

                float signedAngle = Vector3.SignedAngle(flatCamFwd, flatToTarget, Vector3.up);

                // Is target roughly within view frustum?
                if (Mathf.Abs(signedAngle) > 28f)
                {
                    string dirArrow = signedAngle < 0 ?
                        $"◄ PUTAR KIRI  //  JARAK RADAR {Mathf.RoundToInt(Mathf.Abs(signedAngle))}°" :
                        $"JARAK RADAR {Mathf.RoundToInt(signedAngle)}°  //  PUTAR KANAN ►";

                    GUI.Label(new Rect(hudX + 10, hudY + 12, hudW - 20, 20), dirArrow, styleRadarAlert);
                    GUI.Label(new Rect(hudX + 10, hudY + 34, hudW - 20, 18), "MINERAL DI LUAR BIDANG PANDANG", styleStatusSub);
                }
                else
                {
                    GUI.Label(new Rect(hudX + 10, hudY + 10, hudW - 20, 20), "● TARGET TERKUNCI // SIAP EKSTRAKSI", styleRadarLocked);
                    string tapPips = "";
                    for (int i = 0; i < RequiredTaps; i++)
                    {
                        tapPips += (i < taps) ? "■ " : "□ ";
                    }
                    GUI.Label(new Rect(hudX + 10, hudY + 32, hudW - 20, 22), $"EKSTRAKSI: [ {tapPips}]  ({taps}/{RequiredTaps})", styleTapsCounter);
                }
            }
            else
            {
                GUI.Label(new Rect(hudX + 10, hudY + 14, hudW - 20, 20), feedback, styleStatusSub);
            }
        }

        private void DrawPill(Rect rect, string tag, string val, GUIStyle sTag, GUIStyle sVal)
        {
            GUI.color = new Color(0.06f, 0.15f, 0.22f, 0.94f);
            GUI.DrawTexture(rect, texWhite);
            GUI.color = new Color(0.19f, 0.78f, 0.68f, 1f);
            GUI.DrawTexture(new Rect(rect.x, rect.y, 3f, rect.height), texWhite);
            GUI.color = Color.white;

            GUI.Label(new Rect(rect.x + 8, rect.y + 4, rect.width - 16, 13), tag, sTag);
            GUI.Label(new Rect(rect.x + 8, rect.y + 17, rect.width - 16, 17), val, sVal);
        }

        private bool DrawBtn(Rect rect, string text, GUIStyle style)
        {
            bool isPressed = rect.Contains(Event.current.mousePosition) && Mouse.current != null && Mouse.current.leftButton.isPressed;

            GUI.color = isPressed ? new Color(0.75f, 0.75f, 0.75f, 1f) : Color.white;

            if (texBtnSec != null)
            {
                GUI.DrawTexture(rect, texBtnSec, ScaleMode.StretchToFill);
            }
            else
            {
                GUI.color = new Color(0.08f, 0.20f, 0.28f, 0.92f);
                GUI.DrawTexture(rect, texWhite);
            }

            GUI.color = Color.white;
            GUI.Label(rect, text, style);

            if (ButtonHit(rect))
            {
                return true;
            }

            return false;
        }

        private bool ButtonHit(Rect rect)
        {
            if (GUI.Button(rect, GUIContent.none, GUIStyle.none)) { lastTouchButtonFrame = Time.frameCount; return true; }
            if (Event.current.type != EventType.Repaint || lastTouchButtonFrame == Time.frameCount) return false;
            foreach (var touch in EnhancedTouch.activeTouches)
            {
                if (touch.phase != UnityEngine.InputSystem.TouchPhase.Began) continue;
                var point = new Vector2(touch.screenPosition.x / HudScale, (Screen.height - touch.screenPosition.y) / HudScale);
                if (!rect.Contains(point)) continue;
                lastTouchButtonFrame = Time.frameCount;
                return true;
            }
            return false;
        }
    }
}
