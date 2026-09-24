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
            DebugModal,
            TransitioningToAR
        }

        [Header("State")]
        public ScreenState currentState = ScreenState.Splash;

        // Static flag so Splash Screen only shows once per app session
        public static bool hasShownSplash = false;

        // UI Textures
        private Texture2D texLogo;
        private Texture2D texBg;
        private Texture2D texBtnSec;
        private Texture2D texCard;
        private Texture2D texPill;
        private Texture2D texWhite;
        private Texture2D texProgressBar;
        private Texture2D texScrim;

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
        private const string PrefUseARCore = "EnerGo.UseARCore";
        private const string PrefResource = "EnerGo.SelectedResource";
        private const string RandomResource = "random"; // settings choice: a different SDA picked on every PLAY
        private static readonly string[] AllResources = { ResourceIds.Sun, ResourceIds.Coal, ResourceIds.Gold };

        // Custom Styles
        private GUIStyle styleSplashTitle;
        private GUIStyle styleSplashTitleShadow;
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

        private GUIStyle styleTitleShadow;
        private GUIStyle styleTargetTag;
        private GUIStyle styleDebugTag;
        private GUIStyle styleDebugDesc;
        private GUIStyle styleDebugOptionActive;
        private GUIStyle styleDebugOptionInactive;

        private bool stylesInitialized = false;

        private float HudScale => Mathf.Max(1f, Mathf.Min(Screen.width / 480f, Screen.height / 800f));

        // Mini-games are played by tilting and aiming, often without touching the screen, so never let it dim.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void KeepScreenAwake() => Screen.sleepTimeout = SleepTimeout.NeverSleep;

        private void OnEnable() { EnhancedTouchSupport.Enable(); }
        private void OnDisable() { EnhancedTouchSupport.Disable(); }
        private void OnApplicationPause(bool paused) { if (paused && EnerGoProgress.Current != null) EnerGoProgress.Commit(EnerGoProgress.Current); }

        private void Awake()
        {
            Application.targetFrameRate = 60;

            // Settings survive going to the capture scene and restarting the app.
            useARCore = PlayerPrefs.GetInt(PrefUseARCore, 0) == 1;
            string savedResource = PlayerPrefs.GetString(PrefResource, ResourceIds.Gold);
            selectedResource = savedResource == ResourceIds.Sun || savedResource == ResourceIds.Coal || savedResource == RandomResource
                ? savedResource : ResourceIds.Gold;

            if (hasShownSplash)
            {
                currentState = ScreenState.Lobby;
            }
            else
            {
                // Play splash screen sound on first launch
                if (SFXManager.Instance != null) SFXManager.Instance.PlaySplash();
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
            else if (currentState == ScreenState.Lobby)
            {
                // Start lobby music immediately if splash already shown
                if (SFXManager.Instance != null) SFXManager.Instance.PlayLobbyMusic();
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

            if (texScrim == null)
            {
                // Vertical fade: transparent at the top, dark navy at the bottom (y=0 is the bottom row).
                const int h = 32;
                texScrim = new Texture2D(1, h) { wrapMode = TextureWrapMode.Clamp };
                for (int y = 0; y < h; y++)
                    texScrim.SetPixel(0, y, new Color(0.01f, 0.04f, 0.07f, 0.85f * (1f - y / (h - 1f))));
                texScrim.Apply();
            }
        }

        private void OnDestroy()
        {
            if (texWhite != null) Destroy(texWhite);
            if (texProgressBar != null) Destroy(texProgressBar);
            if (texScrim != null) Destroy(texScrim);
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
                    if (SFXManager.Instance != null) SFXManager.Instance.PlayLobbyMusic();
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
                        if (SFXManager.Instance != null) SFXManager.Instance.PlayLobbyMusic();
                    }
                }
            }
            else if (currentState == ScreenState.TransitioningToAR)
            {
                transitionTimer += Time.deltaTime;
                if (transitionTimer >= TransitionDuration)
                {
                    if (SFXManager.Instance != null) SFXManager.Instance.StopLobbyMusic();
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

            styleSplashTitleShadow = new GUIStyle(styleSplashTitle)
            {
                normal = { textColor = new Color(0f, 0.03f, 0.06f, 0.75f) }
            };

            styleSplashSub = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.72f, 0.92f, 1f) }
            };

            styleSplashStatus = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                richText = true,
                normal = { textColor = Color.white }
            };

            styleSplashHint = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.90f, 0.96f, 1f) }
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
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.72f, 0.92f, 1f) }
            };

            // CTA Button
            stylePlayBtnText = new GUIStyle(GUI.skin.label)
            {
                fontSize = 26,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.01f, 0.16f, 0.11f) } // High-contrast dark ink on vibrant mint
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

            styleTitleShadow = new GUIStyle(styleLobbyTitle)
            {
                normal = { textColor = new Color(0f, 0.03f, 0.06f, 0.75f) }
            };

            styleTargetTag = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.18f, 0.94f, 0.74f) }
            };

            styleDebugTag = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(1f, 0.72f, 0.28f) }
            };

            styleDebugDesc = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.55f, 0.75f, 0.88f) }
            };

            styleDebugOptionActive = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = Color.white }
            };

            styleDebugOptionInactive = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Normal,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = new Color(0.55f, 0.72f, 0.85f) }
            };

            EnerGoUI.ApplyFont(styleSplashTitle, styleSplashTitleShadow, styleSplashSub, styleSplashStatus, styleSplashHint,
                styleHudLabel, styleHudValue, styleHudGold, styleLobbyTitle, styleLobbySub, stylePlayBtnText,
                styleCardTag, styleCardTagRight, styleCardHeader, styleCardRowLabel, styleCardRowVal,
                styleSecBtn, styleSecBtnWarn, styleModalTag, styleModalTitle, styleModalStepNum,
                styleModalStepTitle, styleModalStepDesc, styleTitleShadow, styleTargetTag, styleDebugTag,
                styleDebugDesc, styleDebugOptionActive, styleDebugOptionInactive);
            stylesInitialized = true;
        }

        private void OnGUI()
        {
            InitStyles();

            if (texLogo == null) LoadTextures();
            if (texWhite == null || texProgressBar == null || texScrim == null) CreateSolidTextures();

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
                GUI.DrawTexture(new Rect(0, 0, sw, sh), texBg, ScaleMode.StretchToFill);
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
                if (currentState == ScreenState.DebugModal)
                {
                    DrawDebugModal(sw, sh);
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

            // Dark scrim from mid-screen down: the bright sky/forest background washes out the text.
            float scrimY = sh * 0.45f;
            GUI.color = new Color(1f, 1f, 1f, alpha);
            GUI.DrawTexture(new Rect(0, scrimY, sw, sh - scrimY), texScrim, ScaleMode.StretchToFill);

            // Typography
            float textY = logoY + logoH + 16f;
            GUI.Label(new Rect(22, textY + 2, sw - 40, 48), "ENERGO", styleSplashTitleShadow);
            GUI.Label(new Rect(20, textY, sw - 40, 48), "ENERGO", styleSplashTitle);

            const string tagline = "JELAJAHI SUMBER DAYA";
            float tagW = styleSplashSub.CalcSize(new GUIContent(tagline)).x + 32f;
            var tagRect = new Rect((sw - tagW) * 0.5f, textY + 48f, tagW, 26f);
            if (texBtnSec != null) GUI.DrawTexture(tagRect, texBtnSec, ScaleMode.StretchToFill);
            GUI.Label(tagRect, tagline, styleSplashSub);

            // Loading Track & Bar
            float barW = Mathf.Min(260f, sw - 90f);
            float barH = 6f;
            float barX = (sw - barW) * 0.5f;
            float barY = sh * 0.73f;

            // Track background
            GUI.color = new Color(0.02f, 0.06f, 0.09f, 0.85f * alpha);
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
            GUI.color = new Color(1f, 1f, 1f, alpha);
            GUI.Label(new Rect(20, barY + 16, sw - 40, 22), $"{statusText}  <color=#2EF0B4>{percent}%</color>", styleSplashStatus);

            // Hint, gently pulsing so it reads as an instruction
            if (splashTimer > 0.35f)
            {
                GUI.color = new Color(1f, 1f, 1f, alpha * (0.75f + 0.25f * Mathf.Sin(splashTimer * 4f)));
                GUI.Label(new Rect(20, sh - Screen.safeArea.yMin / HudScale - 52f, sw - 40, 22), "SENTUH LAYAR UNTUK MELANJUTKAN", styleSplashHint);
            }

            GUI.color = Color.white;
        }

        private void DrawLobbyScreen(float sw, float sh)
        {
            // --- HERO / BRANDING (centered) ---
            float floatAnim = Mathf.Sin(Time.time * 2.0f) * 6f;
            float logoSize = Mathf.Min(148f, sw * 0.34f);
            float logoX = (sw - logoSize) * 0.5f;
            float logoY = sh * 0.20f + floatAnim;

            GUI.color = Color.white;
            if (texLogo != null)
            {
                GUI.DrawTexture(new Rect(logoX, logoY, logoSize, logoSize * 1.06f), texLogo, ScaleMode.ScaleToFit);
            }

            float titleY = logoY + (logoSize * 1.06f) + 10f;
            GUI.Label(new Rect(21, titleY + 1, sw - 40, 38), "ENERGO", styleTitleShadow);
            GUI.Label(new Rect(20, titleY, sw - 40, 38), "ENERGO", styleLobbyTitle);
            // Dark pill behind the tagline: thin text is unreadable on the bright sky/forest background.
            const string tagline = "JELAJAHI SUMBER DAYA · TEMUKAN ENERGI";
            float tagW = Mathf.Min(sw - 40f, styleLobbySub.CalcSize(new GUIContent(tagline)).x + 28f);
            var tagRect = new Rect((sw - tagW) * 0.5f, titleY + 38f, tagW, 24f);
            if (texBtnSec != null)
            {
                GUI.color = new Color(1f, 1f, 1f, 0.85f);
                GUI.DrawTexture(tagRect, texBtnSec, ScaleMode.StretchToFill);
                GUI.color = Color.white;
            }
            GUI.Label(tagRect, tagline, styleLobbySub);

            // --- PRIMARY ACTION: PLAY BUTTON ---
            float pulse = 1f + 0.018f * Mathf.Sin(Time.time * 3.2f);
            float btnW = Mathf.Min(290f, sw - 48f) * pulse;
            float btnH = 68f * pulse;
            float btnX = (sw - btnW) * 0.5f;
            float btnY = titleY + 78f;
            var playBtnRect = new Rect(btnX, btnY, btnW, btnH);

            bool playHover = playBtnRect.Contains(Event.current.mousePosition);
            bool playPressed = playHover && Mouse.current != null && Mouse.current.leftButton.isPressed;

            GUI.color = playPressed ? new Color(0.82f, 0.82f, 0.82f, 1f) : Color.white;
            EnerGoUI.DrawPrimaryButton(playBtnRect);
            GUI.color = Color.white;
            GUI.Label(new Rect(btnX, btnY + (btnH - 32f) * 0.5f, btnW, 32f), "PLAY", stylePlayBtnText);

            if (inputCooldownTimer <= 0f && ButtonHit(playBtnRect))
            {
                if (currentState == ScreenState.Lobby)
                {
                    if (SFXManager.Instance != null) SFXManager.Instance.PlayButtonClick();
                    ResourceIds.Selected = selectedResource == RandomResource
                        ? AllResources[Random.Range(0, AllResources.Length)] : selectedResource;
                    currentState = ScreenState.TransitioningToAR;
                    transitionTimer = 0f;
                }
            }

            // --- SETTINGS BUTTON (secondary pill, below PLAY) ---
            float setW = Mathf.Min(220f, sw - 80f);
            var settingsRect = new Rect((sw - setW) * 0.5f, btnY + btnH + 16f, setW, 46f);

            if (DrawSecondaryBtn(settingsRect, "SETTING", styleSecBtn))
            {
                if (currentState == ScreenState.Lobby)
                {
                    if (SFXManager.Instance != null) SFXManager.Instance.PlayButtonClick();
                    currentState = ScreenState.DebugModal;
                    inputCooldownTimer = 0.2f;
                }
            }
        }

        private void DrawDebugModal(float sw, float sh)
        {
            // 1. Dark Backdrop
            GUI.color = new Color(0.02f, 0.05f, 0.08f, 0.88f);
            GUI.DrawTexture(new Rect(0, 0, sw, sh), texWhite);
            GUI.color = Color.white;

            // 2. Modal Panel dimensions (compute first so backdrop hit-test can exclude it)
            float mw = Mathf.Min(430f, sw - 28f);
            float mh = Mathf.Min(560f, sh - 40f);
            float mx = (sw - mw) * 0.5f;
            float my = (sh - mh) * 0.5f;
            var modalRect = new Rect(mx, my, mw, mh);

            // Close when tapping OUTSIDE the modal panel
            if (inputCooldownTimer <= 0f && Event.current.type == EventType.MouseDown
                && !modalRect.Contains(Event.current.mousePosition))
            {
                if (SFXManager.Instance != null) SFXManager.Instance.PlayButtonBack();
                currentState = ScreenState.Lobby;
                inputCooldownTimer = 0.2f;
                Event.current.Use();
            }
            // Touch support for outside-tap close
            if (inputCooldownTimer <= 0f && Event.current.type == EventType.Repaint)
            {
                foreach (var touch in EnhancedTouch.activeTouches)
                {
                    if (touch.phase != UnityEngine.InputSystem.TouchPhase.Began) continue;
                    var pt = new Vector2(touch.screenPosition.x / HudScale, (Screen.height - touch.screenPosition.y) / HudScale);
                    if (!modalRect.Contains(pt))
                    {
                        if (SFXManager.Instance != null) SFXManager.Instance.PlayButtonBack();
                        currentState = ScreenState.Lobby;
                        inputCooldownTimer = 0.2f;
                        break;
                    }
                }
            }

            // Modal background
            GUI.color = Color.white;
            if (texCard != null)
            {
                GUI.DrawTexture(modalRect, texCard, ScaleMode.StretchToFill);
            }
            else
            {
                GUI.color = new Color(0.05f, 0.12f, 0.18f, 0.98f);
                GUI.DrawTexture(modalRect, texWhite);
                GUI.color = Color.white;
            }

            // Glowing border
            GUI.color = new Color(0.20f, 0.75f, 0.95f, 0.35f);
            GUI.DrawTexture(new Rect(mx, my, mw, 2f), texWhite);
            GUI.DrawTexture(new Rect(mx, my + mh - 2f, mw, 2f), texWhite);
            GUI.DrawTexture(new Rect(mx, my, 2f, mh), texWhite);
            GUI.DrawTexture(new Rect(mx + mw - 2f, my, 2f, mh), texWhite);
            GUI.color = Color.white;

            float curY = my + 14f;

            // Header
            GUI.Label(new Rect(mx + 20, curY, mw - 40, 16), "PENGATURAN PENGEMBANG & PENGUJIAN", styleDebugTag);
            curY += 20f;
            GUI.Label(new Rect(mx + 20, curY, mw - 40, 26), "MODE DEBUG", styleModalTitle);
            // card_panel.png header strip is 62/400 of its height; start the sections below it.
            curY = Mathf.Max(curY + 34f, my + mh * (62f / 400f) + 14f);

            // Section 1: PILIH TARGET SUMBER DAYA
            GUI.Label(new Rect(mx + 20, curY, mw - 40, 18), "1. PILIH TARGET SUMBER DAYA (SDA)", styleCardTag);
            curY += 22f;

            float sdaBtnH = 34f;
            float btnGap = 8f;

            DrawDebugResourceOption(mx + 20, curY, mw - 40, sdaBtnH, ResourceIds.Sun, "Esensi Matahari", $"{sunCount} unit");
            curY += sdaBtnH + btnGap;

            DrawDebugResourceOption(mx + 20, curY, mw - 40, sdaBtnH, ResourceIds.Coal, "Batu Bara", $"{coalCount} unit");
            curY += sdaBtnH + btnGap;

            DrawDebugResourceOption(mx + 20, curY, mw - 40, sdaBtnH, ResourceIds.Gold, "Emas (Koleksi)", $"{goldCount} ore");
            curY += sdaBtnH + btnGap;

            DrawDebugResourceOption(mx + 20, curY, mw - 40, sdaBtnH, RandomResource, "Acak (Random)", "3 SDA");
            curY += sdaBtnH + btnGap + 8f;

            // Section 2: MODE AR (on = ARCore scene, off = Sensor 360), remembered across sessions
            GUI.Label(new Rect(mx + 20, curY, mw - 40, 18), "2. MODE KAMERA", styleCardTag);
            curY += 22f;

            string arCaption = useARCore ? "Aktif · butuh Google Play Services for AR" : "Mati · pakai Sensor 360 (gyroscope)";
            if (DrawSwitchRow(new Rect(mx + 20, curY, mw - 40, 52f), "MODE AR (ARCORE)", arCaption, useARCore))
            {
                useARCore = !useARCore;
                PlayerPrefs.SetInt(PrefUseARCore, useARCore ? 1 : 0);
                PlayerPrefs.Save();
            }
            curY += 52f + 16f;

            // Section 3: RESET PROGRES
            GUI.Label(new Rect(mx + 20, curY, mw - 40, 18), "3. PENGELOLAAN DATA SAVES", styleCardTag);
            curY += 22f;

            string resetText = isConfirmingReset ? "TEKAN LAGI UNTUK RESET SEMUA DATA" : "RESET DATA PROGRES";
            var resetRect = new Rect(mx + 20, curY, mw - 40, 36f);
            if (DrawSecondaryBtn(resetRect, resetText, isConfirmingReset ? styleSecBtnWarn : styleSecBtn))
            {
                if (SFXManager.Instance != null) SFXManager.Instance.PlayButtonClick();
                if (!isConfirmingReset)
                {
                    isConfirmingReset = true;
                    resetConfirmTimer = 3.5f;
                }
                else if (EnerGoProgress.Reset())
                {
                    RefreshStats();
                    isConfirmingReset = false;
                }
            }

            // Close Button
            var closeRect = new Rect(mx + 20, my + mh - 50f, mw - 40, 40f);
            if (DrawSecondaryBtn(closeRect, "SIMPAN & TUTUP", styleSecBtn))
            {
                if (SFXManager.Instance != null) SFXManager.Instance.PlayButtonBack();
                currentState = ScreenState.Lobby;
                inputCooldownTimer = 0.2f;
            }
        }

        private void DrawDebugResourceOption(float x, float y, float w, float h, string id, string label, string count)
        {
            bool isSelected = selectedResource == id;
            var rect = new Rect(x, y, w, h);

            GUI.color = isSelected ? new Color(0.10f, 0.32f, 0.32f, 0.95f) : new Color(0.06f, 0.14f, 0.20f, 0.85f);
            GUI.DrawTexture(rect, texWhite);

            // Left accent bar
            GUI.color = isSelected ? new Color(0.12f, 0.90f, 0.70f, 1f) : new Color(0.20f, 0.35f, 0.45f, 0.5f);
            GUI.DrawTexture(new Rect(rect.x, rect.y, 4f, rect.height), texWhite);
            GUI.color = Color.white;

            GUI.Label(new Rect(x + 12f, y + 6f, w - 100f, 22f), label, isSelected ? styleDebugOptionActive : styleDebugOptionInactive);
            GUI.Label(new Rect(x + w - 90f, y + 6f, 80f, 22f), count, styleCardRowVal);

            if (inputCooldownTimer <= 0f && ButtonHit(rect))
            {
                if (SFXManager.Instance != null) SFXManager.Instance.PlayButtonClick();
                selectedResource = id;
                PlayerPrefs.SetString(PrefResource, id);
                PlayerPrefs.Save();
                inputCooldownTimer = 0.15f;
            }
        }

        // Settings row with an on/off switch on the right; the whole row is the tap target.
        private bool DrawSwitchRow(Rect rect, string title, string caption, bool on)
        {
            var mint = new Color(0.12f, 0.90f, 0.70f, 1f);
            EnerGoUI.DrawPanel(rect, new Color(0.06f, 0.14f, 0.20f, 0.85f), on ? mint : new Color(0.20f, 0.35f, 0.45f, 0.6f), 10f);

            GUI.Label(new Rect(rect.x + 14f, rect.y + 6f, rect.width - 90f, 22f), title, styleDebugOptionActive);
            GUI.Label(new Rect(rect.x + 14f, rect.y + 27f, rect.width - 90f, 18f), caption, styleCardRowLabel);

            var track = new Rect(rect.xMax - 14f - 50f, rect.center.y - 14f, 50f, 28f);
            EnerGoUI.DrawPanel(track, on ? mint : new Color(0.22f, 0.32f, 0.40f, 1f), Color.clear, 14f);
            const float knob = 22f;
            var knobRect = new Rect(on ? track.xMax - 3f - knob : track.x + 3f, track.y + 3f, knob, knob);
            EnerGoUI.DrawPanel(knobRect, Color.white, Color.clear, knob * 0.5f);

            if (inputCooldownTimer <= 0f && ButtonHit(rect))
            {
                if (SFXManager.Instance != null) SFXManager.Instance.PlayButtonClick();
                inputCooldownTimer = 0.15f;
                return true;
            }
            return false;
        }

        private bool DrawDebugPill(Rect rect, string text)
        {
            bool isPressed = rect.Contains(Event.current.mousePosition) && Mouse.current != null && Mouse.current.leftButton.isPressed;
            GUI.color = isPressed ? new Color(0.15f, 0.25f, 0.30f, 0.98f) : new Color(0.08f, 0.16f, 0.22f, 0.92f);
            GUI.DrawTexture(rect, texWhite);

            // Amber top border for tech vibe
            GUI.color = new Color(1f, 0.72f, 0.28f, 0.85f);
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, 2f), texWhite);
            GUI.color = Color.white;

            GUI.Label(rect, text, styleDebugTag);

            if (inputCooldownTimer <= 0f && ButtonHit(rect))
            {
                return true;
            }
            return false;
        }

        private void DrawResourceChip(Rect rect, string label, string value, Color accentColor)
        {
            GUI.color = new Color(0.05f, 0.11f, 0.16f, 0.90f);
            GUI.DrawTexture(rect, texWhite);

            GUI.color = accentColor;
            GUI.DrawTexture(new Rect(rect.x, rect.y, 3f, rect.height), texWhite);
            GUI.color = Color.white;

            // 1-line compact chip: label left, value right
            GUI.Label(new Rect(rect.x + 6f, rect.y + 7f, rect.width - 40f, 18f), label, styleHudLabel);
            GUI.Label(new Rect(rect.x + rect.width - 36f, rect.y + 7f, 30f, 18f), value, styleHudValue);
        }

        private bool DrawTargetChip(Rect rect, string text)
        {
            bool isPressed = rect.Contains(Event.current.mousePosition) && Mouse.current != null && Mouse.current.leftButton.isPressed;
            GUI.color = isPressed ? new Color(0.10f, 0.24f, 0.28f, 0.95f) : new Color(0.05f, 0.12f, 0.18f, 0.88f);
            GUI.DrawTexture(rect, texWhite);

            // Subtle border
            GUI.color = new Color(0.18f, 0.90f, 0.72f, 0.45f);
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, 1f), texWhite);
            GUI.DrawTexture(new Rect(rect.x, rect.y + rect.height - 1f, rect.width, 1f), texWhite);
            GUI.color = Color.white;

            GUI.Label(rect, text, styleTargetTag);

            if (inputCooldownTimer <= 0f && ButtonHit(rect))
            {
                return true;
            }
            return false;
        }

        private void DrawInventoryModal(float sw, float sh)
        {
            // 1. Dark Backdrop
            GUI.color = new Color(0.02f, 0.05f, 0.08f, 0.88f);
            var backdropRect = new Rect(0, 0, sw, sh);
            GUI.DrawTexture(backdropRect, texWhite);
            GUI.color = Color.white;

            if (inputCooldownTimer <= 0f && ButtonHit(backdropRect))
            {
                if (SFXManager.Instance != null) SFXManager.Instance.PlayButtonBack();
                currentState = ScreenState.Lobby;
                inputCooldownTimer = 0.2f;
            }

            // 2. Modal Card
            float mw = Mathf.Min(430f, sw - 28f);
            float mh = Mathf.Min(470f, sh - 50f);
            float mx = (sw - mw) * 0.5f;
            float my = (sh - mh) * 0.5f;
            var modalRect = new Rect(mx, my, mw, mh);

            GUI.Button(modalRect, GUIContent.none, GUIStyle.none);

            GUI.color = Color.white;
            if (texCard != null)
            {
                GUI.DrawTexture(modalRect, texCard, ScaleMode.StretchToFill);
            }
            else
            {
                GUI.color = new Color(0.05f, 0.12f, 0.18f, 0.98f);
                GUI.DrawTexture(modalRect, texWhite);
                GUI.color = Color.white;
            }

            // Header
            float topY = my + 12f;
            GUI.Label(new Rect(mx + 20, topY, mw - 40, 16), "DATA KOLEKSI & MINERAL", styleModalTag);
            GUI.Label(new Rect(mx + 20, topY + 24, mw - 40, 26), "INVENTORI SUMBER DAYA", styleModalTitle);

            // Resource Cards
            var save = EnerGoProgress.Current;
            string[] ids = { ResourceIds.Sun, ResourceIds.Coal, ResourceIds.Gold };
            Color[] colors = { new Color(1f, 0.82f, 0.28f), new Color(0.40f, 0.80f, 0.95f), new Color(0.98f, 0.82f, 0.25f) };

            float startY = topY + 62f;
            float rowH = 58f;
            for (int i = 0; i < ids.Length; i++)
            {
                var entry = save.Get(ids[i]);
                float rowY = startY + i * (rowH + 8f);
                var rowRect = new Rect(mx + 20, rowY, mw - 40, rowH);

                GUI.color = new Color(0.06f, 0.14f, 0.20f, 0.85f);
                GUI.DrawTexture(rowRect, texWhite);

                GUI.color = colors[i];
                GUI.DrawTexture(new Rect(rowRect.x, rowRect.y, 3f, rowRect.height), texWhite);
                GUI.color = Color.white;

                string resTitle = ResourceIds.Name(ids[i]);
                GUI.Label(new Rect(rowRect.x + 12, rowY + 8f, rowRect.width - 120f, 22f), resTitle, styleCardHeader);
                GUI.Label(new Rect(rowRect.x + rowRect.width - 110f, rowY + 8f, 100f, 22f), $"{entry.Total} unit", styleCardRowVal);
                GUI.Label(new Rect(rowRect.x + 12, rowY + 30f, rowRect.width - 24f, 20f), $"Normal: {entry.normalQuantity}  ·  Murni: {entry.pureQuantity}", styleCardRowLabel);
            }

            GUI.Label(new Rect(mx + 20, startY + 3 * (rowH + 8f) + 6f, mw - 40, 36f), "Emas adalah mineral koleksi; belum dipakai untuk membangun.", styleModalStepDesc);

            // Close Button
            var closeRect = new Rect(mx + 20, my + mh - 50f, mw - 40, 40f);
            if (DrawSecondaryBtn(closeRect, "TUTUP INVENTORI", styleSecBtn))
            {
                if (SFXManager.Instance != null) SFXManager.Instance.PlayButtonBack();
                currentState = ScreenState.Lobby;
                inputCooldownTimer = 0.2f;
            }
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
            EnerGoUI.DrawSecondaryButton(rect);

            GUI.color = Color.white;
            GUI.Label(rect, text, style);

            if (inputCooldownTimer <= 0f && ButtonHit(rect))
            {
                return true;
            }

            return false;
        }

        private bool ButtonHit(Rect rect) => EnerGoUI.TapHit(rect, HudScale);

        private void DrawHelpModal(float sw, float sh)
        {
            // Dark Backdrop
            GUI.color = new Color(0.02f, 0.05f, 0.08f, 0.85f);
            var backdropRect = new Rect(0, 0, sw, sh);
            GUI.DrawTexture(backdropRect, texWhite);
            GUI.color = Color.white;

            if (inputCooldownTimer <= 0f && ButtonHit(backdropRect))
            {
                if (SFXManager.Instance != null) SFXManager.Instance.PlayButtonBack();
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
                if (SFXManager.Instance != null) SFXManager.Instance.PlayButtonBack();
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
