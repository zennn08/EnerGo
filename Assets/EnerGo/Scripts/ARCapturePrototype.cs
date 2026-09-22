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
            if (EnerGoProgress.Current.pendingCapture != null) return;
            // Touch inputs
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

            // Mouse inputs (Editor / Desktop)
#if UNITY_EDITOR
            if (Mouse.current != null)
            {
                if (Mouse.current.leftButton.wasPressedThisFrame)
                {
                    Vector2 mPos = Mouse.current.position.ReadValue();
                    CheckTapOnTarget(mPos);
                }

                // Right click drag to look around
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

        private void DrawAnalysis(float sw, float sh)
        {
            var session = EnerGoProgress.Current.pendingCapture;
            var question = QuizBank.Find(session.questionId, session.resourceId);
            if (question == null) return;
            GUI.color = new Color(0.02f, 0.06f, 0.09f, 0.97f);
            GUI.DrawTexture(new Rect(0, 0, sw, sh), texWhite);
            GUI.color = Color.white;
            float x = 24f, w = sw - 48f, y = Mathf.Max(150f, sh * 0.28f);
            var heading = new GUIStyle(styleRadarLocked) { alignment = TextAnchor.MiddleLeft, fontSize = 17, wordWrap = true };
            var body = new GUIStyle(styleStatusSub) { alignment = TextAnchor.UpperLeft, fontSize = 14, wordWrap = true };
            GUI.Label(new Rect(x, y - 45f, w, 30f), session.state == "result" ? "HASIL ANALISIS" : "ANALISIS · " + ResourceIds.Name(resourceId).ToUpperInvariant(), heading);
            if (session.state == "awaiting-analysis")
            {
                GUI.Label(new Rect(x, y, w, 100f), question.statement, body);
                if (DrawBtn(new Rect(x, y + 120f, w, 48f), "BENAR", styleBtnText) && EnerGoProgress.Answer(true)) resultShownAt = Time.time;
                if (DrawBtn(new Rect(x, y + 180f, w, 48f), "SALAH", styleBtnText) && EnerGoProgress.Answer(false)) resultShownAt = Time.time;
            }
            else
            {
                GUI.Label(new Rect(x, y, w, 45f), (session.correct ? "TEPAT · MURNI" : "BELUM TEPAT · NORMAL") + "  +" + session.rewardQuantity + " unit", heading);
                GUI.Label(new Rect(x, y + 55f, w, 120f), question.explanation, body);
                bool canContinue = session.correct || Time.time - resultShownAt >= 3f;
                if (canContinue && DrawBtn(new Rect(x, y + 190f, w, 48f), "LANJUT KE LOBBY", styleBtnText))
                {
                    if (EnerGoProgress.ClearResult()) SceneManager.LoadScene("LobbyScene");
                }
                else if (!canContinue) GUI.Label(new Rect(x, y + 190f, w, 40f), "Baca penjelasan sebelum melanjutkan...", body);
            }
            if (!string.IsNullOrEmpty(EnerGoProgress.Error)) GUI.Label(new Rect(x, sh - 80f, w, 60f), EnerGoProgress.Error, body);
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
