using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using EnhancedTouch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace EnerGo
{
    public class LobbyController : MonoBehaviour
    {
        public enum ScreenState
        {
            Splash,
            Lobby,
            HelpModal,
            InventoryModal,
            TransitioningToAR
        }

        [Header("State")]
        public ScreenState currentState = ScreenState.Splash;

        // Static flag so Splash Screen only shows once per app session
        public static bool hasShownSplash = false;

        // UI Textures
        private Texture2D texLogo;
        private Texture2D texBg;
        private Texture2D texBtnPlay;
        private Texture2D texBtnSec;
        private Texture2D texCard;
        private Texture2D texPill;
        private Texture2D texWhite;
        private Texture2D texProgressBar;

        // Splash Timers & Animation
        private float splashTimer = 0f;
        private const float SplashDuration = 2.4f;
        private const float SplashFadeIn = 0.5f;
        private const float SplashFadeOut = 0.4f;

        // Transition Timer
        private float transitionTimer = 0f;
        private const float TransitionDuration = 0.4f;

        // Input Cooldown
        private float inputCooldownTimer = 0.2f;

        // Reset Confirmation State
        private bool isConfirmingReset = false;
        private float resetConfirmTimer = 0f;

        // Player Data
        private int goldCount;
        private int sunCount;
        private int coalCount;
        private string selectedResource = ResourceIds.Gold;
        private bool useARCore;
        private int lastTouchButtonFrame = -1;

        // Custom Styles
        private GUIStyle styleSplashTitle;
        private GUIStyle styleSplashSub;
        private GUIStyle styleSplashStatus;
        private GUIStyle styleSplashHint;

        private GUIStyle styleHudLabel;
        private GUIStyle styleHudValue;
        private GUIStyle styleHudGold;

        private GUIStyle styleLobbyTitle;
        private GUIStyle styleLobbySub;

        private GUIStyle stylePlayBtnText;

        private GUIStyle styleCardTag;
        private GUIStyle styleCardTagRight;
        private GUIStyle styleCardHeader;
        private GUIStyle styleCardRowLabel;
        private GUIStyle styleCardRowVal;

        private GUIStyle styleSecBtn;
        private GUIStyle styleSecBtnWarn;

        private GUIStyle styleModalTag;
        private GUIStyle styleModalTitle;
        private GUIStyle styleModalStepNum;
        private GUIStyle styleModalStepTitle;
        private GUIStyle styleModalStepDesc;

        private bool stylesInitialized = false;

        private float HudScale => Mathf.Max(1f, Mathf.Min(Screen.width / 480f, Screen.height / 800f));

        private void OnEnable() { EnhancedTouchSupport.Enable(); }
        private void OnDisable() { EnhancedTouchSupport.Disable(); }
        private void OnApplicationPause(bool paused) { if (paused && EnerGoProgress.Current != null) EnerGoProgress.Commit(EnerGoProgress.Current); }

        private void Awake()
        {
            Application.targetFrameRate = 60;

            if (hasShownSplash)
            {
                currentState = ScreenState.Lobby;
            }

            LoadTextures();
            CreateSolidTextures();
        }

        private void Start()
        {
            RefreshStats();
            if (EnerGoProgress.Current.pendingCapture != null)
            {
                ResourceIds.Selected = EnerGoProgress.Current.pendingCapture.resourceId;
                hasShownSplash = true;
                currentState = ScreenState.TransitioningToAR;
            }
        }

        public void RefreshStats()
        {
            var save = EnerGoProgress.Current;
            goldCount = save.Get(ResourceIds.Gold).Total;
            sunCount = save.Get(ResourceIds.Sun).Total;
            coalCount = save.Get(ResourceIds.Coal).Total;
        }

        private void LoadTextures()
        {
            texLogo = Resources.Load<Texture2D>("UI/logo_game");
            texBg = Resources.Load<Texture2D>("UI/bg_lobby");
            texBtnPlay = Resources.Load<Texture2D>("UI/btn_play");
            texBtnSec = Resources.Load<Texture2D>("UI/btn_secondary");
            texCard = Resources.Load<Texture2D>("UI/card_panel");
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

            if (texProgressBar == null)
            {
                texProgressBar = new Texture2D(2, 2);
                Color barColor = new Color(0.08f, 0.92f, 0.65f, 1f);
                texProgressBar.SetPixels(new[] { barColor, barColor, barColor, barColor });
                texProgressBar.Apply();
            }
        }

        private void OnDestroy()
        {
            if (texWhite != null) Destroy(texWhite);
            if (texProgressBar != null) Destroy(texProgressBar);
        }

        private void Update()
        {
            if (inputCooldownTimer > 0f)
            {
                inputCooldownTimer -= Time.deltaTime;
            }

            if (isConfirmingReset)
            {
                resetConfirmTimer -= Time.deltaTime;
                if (resetConfirmTimer <= 0f)
                {
                    isConfirmingReset = false;
                }
            }

            if (currentState == ScreenState.Splash)
            {
                splashTimer += Time.deltaTime;
                if (splashTimer >= SplashDuration)
                {
                    hasShownSplash = true;
                    currentState = ScreenState.Lobby;
                    inputCooldownTimer = 0.25f;
                }

                if (splashTimer > 0.35f)
                {
                    bool tapped = false;
                    foreach (var touch in EnhancedTouch.activeTouches)
                    {
                        if (touch.phase == UnityEngine.InputSystem.TouchPhase.Began) { tapped = true; break; }
                    }
#if UNITY_EDITOR
                    if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) tapped = true;
#endif
                    if (tapped)
                    {
                        hasShownSplash = true;
                        currentState = ScreenState.Lobby;
                        inputCooldownTimer = 0.25f;
                    }
                }
            }
            else if (currentState == ScreenState.TransitioningToAR)
            {
                transitionTimer += Time.deltaTime;
                if (transitionTimer >= TransitionDuration)
                {
                    SceneManager.LoadScene(useARCore ? "ARPrototype" : "Sensor360Scene");
                }
            }
        }

        private void InitStyles()
        {
            if (stylesInitialized && styleCardTagRight != null && styleCardRowLabel != null && styleSecBtn != null) return;

            // Splash
            styleSplashTitle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 42,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };

            styleSplashSub = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Normal,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.55f, 0.78f, 0.92f) }
            };

            styleSplashStatus = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Normal,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.18f, 0.94f, 0.72f) }
            };

            styleSplashHint = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                fontStyle = FontStyle.Normal,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.48f, 0.65f, 0.75f) }
            };

            // Top HUD Telemetry
            styleHudLabel = new GUIStyle(GUI.skin.label)
            {
                fontSize = 9,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.UpperLeft,
                normal = { textColor = new Color(0.35f, 0.85f, 0.96f) } // Crisp Cyan Accent
            };

            styleHudValue = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.LowerLeft,
                normal = { textColor = Color.white }
            };

            styleHudGold = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.LowerLeft,
                normal = { textColor = new Color(1f, 0.82f, 0.28f) } // Vivid Gold Ore
            };

            // Lobby Brand
            styleLobbyTitle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 32,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };

            styleLobbySub = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Normal,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.48f, 0.72f, 0.85f) }
            };

            // CTA Button
            stylePlayBtnText = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.02f, 0.16f, 0.12f) } // High-contrast dark ink on vibrant mint
            };

            // Card
            styleCardTag = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = new Color(0.12f, 0.88f, 0.68f) }
            };

            styleCardTagRight = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleRight,
                normal = { textColor = new Color(0.12f, 0.88f, 0.68f) }
            };

            styleCardHeader = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = Color.white }
            };

            styleCardRowLabel = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Normal,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = new Color(0.50f, 0.68f, 0.80f) }
            };

            styleCardRowVal = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleRight,
                normal = { textColor = new Color(0.90f, 0.95f, 0.98f) }
            };

            // Secondary Buttons
            styleSecBtn = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.75f, 0.88f, 0.96f) }
            };

            styleSecBtnWarn = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(1f, 0.40f, 0.40f) }
            };

            // Modal
            styleModalTag = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.12f, 0.88f, 0.68f) }
            };

            styleModalTitle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 17,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };

            styleModalStepNum = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.UpperLeft,
                normal = { textColor = new Color(0.12f, 0.88f, 0.68f) }
            };

            styleModalStepTitle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.UpperLeft,
                normal = { textColor = Color.white }
            };

            styleModalStepDesc = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                wordWrap = true,
                alignment = TextAnchor.UpperLeft,
                normal = { textColor = new Color(0.65f, 0.80f, 0.90f) }
            };

            stylesInitialized = true;
        }

        private void OnGUI()
        {
            InitStyles();

            if (texLogo == null) LoadTextures();
            if (texWhite == null || texProgressBar == null) CreateSolidTextures();

            var prevMatrix = GUI.matrix;
            var prevColor = GUI.color;

            float scale = HudScale;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));

            float sw = Screen.width / scale;
            float sh = Screen.height / scale;

            // 1. Background
            GUI.color = Color.white;
            if (texBg != null)
            {
                GUI.DrawTexture(new Rect(0, 0, sw, sh), texBg, ScaleMode.ScaleAndCrop);
            }
            else
            {
                GUI.color = new Color(0.04f, 0.08f, 0.12f, 1f);
                GUI.DrawTexture(new Rect(0, 0, sw, sh), texWhite);
                GUI.color = Color.white;
            }

            // 2. States
            if (currentState == ScreenState.Splash)
            {
                DrawSplashScreen(sw, sh);
            }
            else
            {
                DrawLobbyScreen(sw, sh);

                if (currentState == ScreenState.HelpModal)
                {
                    DrawHelpModal(sw, sh);
                }
                if (currentState == ScreenState.InventoryModal)
                {
                    DrawInventoryModal(sw, sh);
                }

                if (currentState == ScreenState.TransitioningToAR)
                {
                    float alpha = Mathf.Clamp01(transitionTimer / TransitionDuration);
                    GUI.color = new Color(0.03f, 0.07f, 0.10f, alpha);
                    GUI.DrawTexture(new Rect(0, 0, sw, sh), texWhite);

                    if (alpha > 0.25f)
                    {
                        GUI.color = new Color(0.12f, 0.88f, 0.68f, alpha);
                        GUI.Label(new Rect(20, sh * 0.48f, sw - 40, 24), "INISIALISASI SENSOR SPASIAL...", styleSplashStatus);
                    }
                    GUI.color = Color.white;
                }
            }

            GUI.color = prevColor;
            GUI.matrix = prevMatrix;
        }

        private void DrawSplashScreen(float sw, float sh)
        {
            float alpha = 1f;
            if (splashTimer < SplashFadeIn)
                alpha = Mathf.Clamp01(splashTimer / SplashFadeIn);
            else if (splashTimer > (SplashDuration - SplashFadeOut))
                alpha = Mathf.Clamp01((SplashDuration - splashTimer) / SplashFadeOut);

            GUI.color = new Color(1f, 1f, 1f, alpha);

            // Animated Logo
            float entrance = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(splashTimer / 0.7f));
            float breathe = 1f + 0.015f * Mathf.Sin(splashTimer * 3.5f);
            float logoScale = Mathf.Lerp(0.85f, 1.0f, entrance) * breathe;

            float baseLogoSize = Mathf.Min(190f, sw * 0.45f);
            float logoW = baseLogoSize * logoScale;
            float logoH = (baseLogoSize * 1.06f) * logoScale;
            float logoX = (sw - logoW) * 0.5f;
            float logoY = sh * 0.27f - (logoH - baseLogoSize) * 0.5f;

            if (texLogo != null)
            {
                GUI.DrawTexture(new Rect(logoX, logoY, logoW, logoH), texLogo, ScaleMode.ScaleToFit);
            }

            // Typography
            float textY = logoY + logoH + 16f;
            GUI.Label(new Rect(20, textY, sw - 40, 48), "ENERGO", styleSplashTitle);
            GUI.Label(new Rect(20, textY + 44, sw - 40, 20), "JELAJAHI SUMBER DAYA", styleSplashSub);

            // Loading Track & Bar
            float barW = Mathf.Min(260f, sw - 90f);
            float barH = 4f; // Clean thin modern telemetry bar
            float barX = (sw - barW) * 0.5f;
            float barY = sh * 0.73f;

            // Track background
            GUI.color = new Color(0.12f, 0.25f, 0.32f, 0.7f * alpha);
            GUI.DrawTexture(new Rect(barX, barY, barW, barH), texWhite);

            // Progress Fill
            float fillProgress = Mathf.Clamp01(splashTimer / (SplashDuration - 0.2f));
            float smoothFill = Mathf.SmoothStep(0f, 1f, fillProgress);
            GUI.color = new Color(0.12f, 0.88f, 0.68f, alpha);
            GUI.DrawTexture(new Rect(barX, barY, barW * smoothFill, barH), texProgressBar);

            // Telemetry Readout Status
            string statusText = "MENYIAPKAN PERMAINAN...";
            if (fillProgress > 0.75f) statusText = "SIAP BERMAIN";
            else if (fillProgress > 0.40f) statusText = "MEMUAT SUMBER DAYA...";

            int percent = Mathf.RoundToInt(smoothFill * 100f);
            GUI.color = new Color(0.12f, 0.88f, 0.68f, alpha * 0.9f);
            GUI.Label(new Rect(20, barY + 14, sw - 40, 20), $"{statusText}  [{percent}%]", styleSplashStatus);

            // Hint
            if (splashTimer > 0.35f)
            {
                GUI.color = new Color(0.40f, 0.55f, 0.65f, alpha * 0.65f);
                GUI.Label(new Rect(20, sh - 44f, sw - 40, 20), "SENTUH LAYAR UNTUK MELANJUTKAN", styleSplashHint);
            }

            GUI.color = Color.white;
        }

        private void DrawLobbyScreen(float sw, float sh)
        {
            float safeTop = Mathf.Max(14f, (Screen.height - Screen.safeArea.yMax) / HudScale + 8f);

            // --- 1. TELEMETRY TOP BAR ---
            float pillH = 42f;
            float padX = 16f;

            // Surveyor Badge (Left)
            DrawPillCustom(new Rect(padX, safeTop, 130, pillH), "ENERGO", "DEMO 2026", styleHudLabel, styleHudValue);

            // Resources (Right)
            float oreW = 105f;
            float energyW = 100f;
            float rightX = sw - padX;

            DrawPillCustom(new Rect(rightX - oreW, safeTop, oreW, pillH), "MINERAL (Au)", $"{goldCount} ORE", styleHudLabel, styleHudGold);
            DrawPillCustom(new Rect(rightX - oreW - 8f - energyW, safeTop, energyW, pillH), "MATAHARI", $"{sunCount} UNIT", styleHudLabel, styleHudValue);

            // --- 2. HERO / BRANDING ---
            float floatAnim = Mathf.Sin(Time.time * 2.0f) * 5f;
            float logoSize = Mathf.Min(140f, sw * 0.34f);
            float logoX = (sw - logoSize) * 0.5f;
            float headerStartY = safeTop + pillH + 20f;
            float logoY = headerStartY + floatAnim;

            GUI.color = Color.white;
            if (texLogo != null)
            {
                GUI.DrawTexture(new Rect(logoX, logoY, logoSize, logoSize * 1.06f), texLogo, ScaleMode.ScaleToFit);
            }

            float titleY = logoY + (logoSize * 1.06f) + 8f;
            GUI.Label(new Rect(20, titleY, sw - 40, 36), "ENERGO", styleLobbyTitle);
            GUI.Label(new Rect(20, titleY + 32, sw - 40, 20), "JELAJAHI SUMBER DAYA · PILIH ENERGI", styleLobbySub);

            // --- 3. PRIMARY ACTION: SCAN ENVIRONMENT ---
            float pulse = 1f + 0.018f * Mathf.Sin(Time.time * 3.2f);
            float btnW = Mathf.Min(320f, sw - 44f) * pulse;
            float btnH = 68f * pulse;
            float btnX = (sw - btnW) * 0.5f;
            float btnY = Mathf.Max(titleY + 64f, sh * 0.47f - (btnH - 68f) * 0.5f);
            var playBtnRect = new Rect(btnX, btnY, btnW, btnH);

            bool playHover = playBtnRect.Contains(Event.current.mousePosition);
            bool playPressed = playHover && Mouse.current != null && Mouse.current.leftButton.isPressed;

            GUI.color = playPressed ? new Color(0.85f, 0.85f, 0.85f, 1f) : Color.white;

            if (texBtnPlay != null)
            {
                GUI.DrawTexture(playBtnRect, texBtnPlay, ScaleMode.StretchToFill);
            }
            else
            {
                GUI.color = new Color(0.08f, 0.92f, 0.65f, 1f);
                GUI.DrawTexture(playBtnRect, texWhite);
            }

            GUI.color = Color.white;
            // Clean, confident typographic CTA with dark ink on mint
            GUI.Label(new Rect(btnX, btnY + (btnH - 28f) * 0.5f, btnW, 28f), "TANGKAP " + ResourceIds.Name(selectedResource).ToUpperInvariant(), stylePlayBtnText);

            if (inputCooldownTimer <= 0f && ButtonHit(playBtnRect))
            {
                if (currentState == ScreenState.Lobby)
                {
                    ResourceIds.Selected = selectedResource;
                    currentState = ScreenState.TransitioningToAR;
                    transitionTimer = 0f;
                }
            }

            // --- 4. FIELD SCAN RESEARCH PANEL ---
            float cardW = sw - 32f;
            float cardH = 168f;
            float cardX = 16f;
            float cardY = btnY + btnH + 18f;

            float maxCardY = sh - 76f - cardH;
            if (cardY > maxCardY) cardY = maxCardY;

            var cardRect = new Rect(cardX, cardY, cardW, cardH);

            GUI.color = Color.white;
            if (texCard != null)
            {
                GUI.DrawTexture(cardRect, texCard, ScaleMode.StretchToFill);
            }
            else
            {
                GUI.color = new Color(0.05f, 0.12f, 0.18f, 0.92f);
                GUI.DrawTexture(cardRect, texWhite);
                GUI.color = Color.white;
            }

            // Card Header Bar
            float headY = cardY + 7f;
            GUI.Label(new Rect(cardX + 24, headY, 160, 20), "PILIH SUMBER DAYA", styleCardTag);
            GUI.Label(new Rect(cardX + cardW - 160, headY, 144, 20), "5 KETUKAN + KUIS", styleCardTagRight);

            // Card Telemetry Rows
            float rowStartY = cardY + 38f;
            float rowGap = 24f;

            DrawResourceRow(cardX + 16, rowStartY, cardW - 32, ResourceIds.Sun, "Esensi Matahari", sunCount);
            DrawResourceRow(cardX + 16, rowStartY + rowGap, cardW - 32, ResourceIds.Coal, "Batu Bara", coalCount);
            DrawResourceRow(cardX + 16, rowStartY + rowGap * 2, cardW - 32, ResourceIds.Gold, "Emas · koleksi", goldCount);
            var modeRect = new Rect(cardX + 16, rowStartY + rowGap * 3 + 7f, cardW - 32, 27f);
            GUI.Label(modeRect, useARCore ? "MODE: ARCORE (BUTUH HP DIDUKUNG)  ·  GANTI" : "MODE: SENSOR 360 (TANPA ARCORE)  ·  GANTI", styleCardRowLabel);
            if (currentState == ScreenState.Lobby && inputCooldownTimer <= 0f && ButtonHit(modeRect)) useARCore = !useARCore;

            // --- 5. BOTTOM UTILITY CONTROLS ---
            float botY = sh - 56f;
            float botBtnW = (sw - 42f) * 0.5f;
            float botBtnH = 42f;

            // Field Guide Button
            var helpRect = new Rect(16f, botY, botBtnW, botBtnH);
            if (currentState == ScreenState.Lobby && DrawSecondaryBtn(helpRect, "PANDUAN", styleSecBtn))
            {
                currentState = ScreenState.HelpModal;
                inputCooldownTimer = 0.2f;
            }

            var inventoryRect = new Rect(16f + botBtnW + 10f, botY, botBtnW, botBtnH);
            if (currentState == ScreenState.Lobby && DrawSecondaryBtn(inventoryRect, "INVENTORI", styleSecBtn))
            {
                currentState = ScreenState.InventoryModal;
                inputCooldownTimer = 0.2f;
            }
        }

        private void DrawResourceRow(float x, float y, float w, string id, string label, int count)
        {
            GUI.Label(new Rect(x, y, w * 0.7f, 22f), (selectedResource == id ? "● " : "○ ") + label, styleCardRowLabel);
            GUI.Label(new Rect(x + w * 0.65f, y, w * 0.35f, 22f), count + " unit", styleCardRowVal);
            if (currentState == ScreenState.Lobby && inputCooldownTimer <= 0f && ButtonHit(new Rect(x, y, w, 22f))) selectedResource = id;
        }

        private void DrawInventoryModal(float sw, float sh)
        {
            GUI.color = new Color(0.02f, 0.06f, 0.09f, 0.96f);
            GUI.DrawTexture(new Rect(0, 0, sw, sh), texWhite);
            GUI.color = Color.white;
            float x = 24f, w = sw - 48f, y = Mathf.Max(95f, sh * 0.2f);
            GUI.Label(new Rect(x, y, w, 35f), "INVENTORI", styleModalTitle);
            var save = EnerGoProgress.Current;
            string[] ids = { ResourceIds.Sun, ResourceIds.Coal, ResourceIds.Gold };
            for (int i = 0; i < ids.Length; i++)
            {
                var entry = save.Get(ids[i]);
                GUI.Label(new Rect(x, y + 55f + i * 72f, w, 28f), ResourceIds.Name(ids[i]) + "  ·  " + entry.Total + " unit", styleCardHeader);
                GUI.Label(new Rect(x, y + 80f + i * 72f, w, 25f), "Normal " + entry.normalQuantity + "   /   Murni " + entry.pureQuantity, styleCardRowLabel);
            }
            GUI.Label(new Rect(x, y + 280f, w, 40f), "Emas adalah mineral koleksi; belum dipakai untuk membangun.", styleModalStepDesc);
            if (DrawSecondaryBtn(new Rect(x, sh - 155f, w, 42f), "KEMBALI", styleSecBtn))
            {
                currentState = ScreenState.Lobby;
                inputCooldownTimer = 0.2f;
            }
            string resetText = isConfirmingReset ? "KONFIRMASI RESET SEMUA PROGRES" : "RESET PROGRES";
            if (DrawSecondaryBtn(new Rect(x, sh - 100f, w, 42f), resetText, isConfirmingReset ? styleSecBtnWarn : styleSecBtn))
            {
                if (!isConfirmingReset) { isConfirmingReset = true; resetConfirmTimer = 3.5f; }
                else if (EnerGoProgress.Reset()) { RefreshStats(); isConfirmingReset = false; }
            }
            if (!string.IsNullOrEmpty(EnerGoProgress.Error)) GUI.Label(new Rect(x, sh - 45f, w, 35f), EnerGoProgress.Error, styleModalStepDesc);
        }

        private void DrawPillCustom(Rect rect, string label, string value, GUIStyle sLabel, GUIStyle sVal)
        {
            GUI.color = new Color(0.06f, 0.15f, 0.22f, 0.94f);
            GUI.DrawTexture(rect, texWhite);
            GUI.color = new Color(0.19f, 0.78f, 0.68f, 1f);
            GUI.DrawTexture(new Rect(rect.x, rect.y, 3f, rect.height), texWhite);
            GUI.color = Color.white;

            // 2-line layout: micro-label above, bold value below
            GUI.Label(new Rect(rect.x + 10, rect.y + 5, rect.width - 20, 14), label, sLabel);
            GUI.Label(new Rect(rect.x + 10, rect.y + 19, rect.width - 20, 18), value, sVal);
        }

        private void DrawTelemetryRow(float x, float y, float w, string label, string val)
        {
            GUI.Label(new Rect(x, y, w * 0.45f, 20), label, styleCardRowLabel);
            GUI.Label(new Rect(x + w * 0.40f, y, w * 0.60f, 20), val, styleCardRowVal);
            // Subtle dotted/solid divider
            GUI.color = new Color(0.18f, 0.35f, 0.45f, 0.25f);
            GUI.DrawTexture(new Rect(x, y + 21, w, 1), texWhite);
            GUI.color = Color.white;
        }

        private bool DrawSecondaryBtn(Rect rect, string text, GUIStyle style)
        {
            bool isPressed = rect.Contains(Event.current.mousePosition) && Mouse.current != null && Mouse.current.leftButton.isPressed;

            GUI.color = isPressed ? new Color(0.75f, 0.75f, 0.75f, 1f) : Color.white;

            if (texBtnSec != null)
            {
                GUI.DrawTexture(rect, texBtnSec, ScaleMode.StretchToFill);
            }
            else
            {
                GUI.color = new Color(0.08f, 0.18f, 0.26f, 0.90f);
                GUI.DrawTexture(rect, texWhite);
            }

            GUI.color = Color.white;
            GUI.Label(rect, text, style);

            if (inputCooldownTimer <= 0f && ButtonHit(rect))
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

        private void DrawHelpModal(float sw, float sh)
        {
            // Dark Backdrop
            GUI.color = new Color(0.02f, 0.05f, 0.08f, 0.85f);
            var backdropRect = new Rect(0, 0, sw, sh);
            GUI.DrawTexture(backdropRect, texWhite);
            GUI.color = Color.white;

            if (inputCooldownTimer <= 0f && ButtonHit(backdropRect))
            {
                currentState = ScreenState.Lobby;
                inputCooldownTimer = 0.2f;
            }

            // Modal Card
            float mw = Mathf.Min(430f, sw - 32f);
            float mh = 410f;
            float mx = (sw - mw) * 0.5f;
            float my = (sh - mh) * 0.45f;
            var modalRect = new Rect(mx, my, mw, mh);

            GUI.Button(modalRect, GUIContent.none, GUIStyle.none);

            GUI.color = Color.white;
            if (texCard != null)
            {
                GUI.DrawTexture(modalRect, texCard, ScaleMode.StretchToFill);
            }
            else
            {
                GUI.color = new Color(0.06f, 0.14f, 0.20f, 0.98f);
                GUI.DrawTexture(modalRect, texWhite);
                GUI.color = Color.white;
            }

            // Modal Header
            float topY = my + 8f;
            GUI.Label(new Rect(mx + 20, topY, mw - 40, 16), "DOKUMENTASI OPERASIONAL", styleModalTag);
            GUI.Label(new Rect(mx + 20, topY + 28, mw - 40, 26), "PROTOKOL EKSPLORASI AR", styleModalTitle);

            // Modal Step Content (Clean technical briefing, zero emojis)
            float stepStartY = my + 72f;
            float stepH = 56f;

            DrawProtocolStep(mx + 20, stepStartY, mw - 40, "01", "PERSIAPAN BIDANG", "Arahkan kamera ke permukaan lantai atau meja bertekstur dengan pencahayaan memadai.");
            DrawProtocolStep(mx + 20, stepStartY + stepH, mw - 40, "02", "PELACAKAN SPASIAL", "Gerakkan kamera perlahan ke samping hingga sensor AR mengunci orientasi ruangan.");
            DrawProtocolStep(mx + 20, stepStartY + stepH * 2, mw - 40, "03", "EKSTRAKSI MINERAL", "Objek emas 3D akan melayang di depan kamera. Ketuk objek emas 5 kali untuk menambang.");
            DrawProtocolStep(mx + 20, stepStartY + stepH * 3, mw - 40, "04", "PENYIMPANAN DATA", "Tekan tombol beranda di kiri atas tampilan AR untuk kembali menyimpan progres mineral.");

            // Close Button
            float cbW = 180f;
            float cbH = 42f;
            var closeRect = new Rect(mx + (mw - cbW) * 0.5f, my + mh - 54f, cbW, cbH);
            if (DrawSecondaryBtn(closeRect, "TUTUP PANDUAN", styleSecBtn))
            {
                currentState = ScreenState.Lobby;
                inputCooldownTimer = 0.2f;
            }
        }

        private void DrawProtocolStep(float x, float y, float w, string num, string title, string desc)
        {
            GUI.Label(new Rect(x, y, 26, 18), num, styleModalStepNum);
            GUI.Label(new Rect(x + 30, y, w - 30, 18), title, styleModalStepTitle);
            GUI.Label(new Rect(x + 30, y + 16, w - 30, 36), desc, styleModalStepDesc);
        }
    }
}
