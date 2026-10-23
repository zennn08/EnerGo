using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using EnhancedTouch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using static EnerGo.EnerGoUI;

namespace EnerGo
{
    // Splash, lobby (collection + start + dock) and the settings, practice and guide popups, drawn with IMGUI in the Arcade Cerah style.
    public class LobbyController : MonoBehaviour
    {
        public enum ScreenState
        {
            Splash,
            Lobby,
            Settings,
            Practice,
            Guides,
            TransitioningToAR
        }

        [Header("State")]
        public ScreenState currentState = ScreenState.Splash;

        // Static flag so Splash Screen only shows once per app session
        public static bool hasShownSplash = false;

        // Set by the radar's "Main mode Latihan" so the lobby opens straight onto the practice picker.
        public static bool OpenPracticeOnLoad;

        public static string CaptureSceneName() => PlayerPrefs.GetInt(PrefUseARCore, 0) == 1 ? "ARPrototype" : "Sensor360Scene";

        private Texture2D texSymbol;
        private Texture2D texWordmark;

        private float splashTimer = 0f;
        private const float SplashDuration = 2.4f;
        private const float SplashFadeIn = 0.5f;
        private const float SplashFadeOut = 0.4f;

        private float transitionTimer = 0f;
        private string transitionScene;
        private const float TransitionDuration = 0.45f;

        private float inputCooldownTimer = 0.2f;
        private bool volumeDragging;

        private GuideViewer lobbyGuide;   // card viewer opened from the PANDUAN sheet
        private float guidesResetAt = -10f;

        private bool isConfirmingReset = false;
        private float resetConfirmTimer = 0f;

        private string selectedResource = ResourceIds.Gold;
        private bool useARCore;
        public const string PrefUseARCore = "EnerGo.UseARCore";
        private const string PrefResource = "EnerGo.SelectedResource";
        private const string RandomResource = "random"; // a different SDA picked on every PLAY
        private static readonly string[] AllResources = { ResourceIds.Sun, ResourceIds.Coal, ResourceIds.Gold, ResourceIds.Oil, ResourceIds.Uranium };

        private GUIStyle sTitle, sBody, sMuted, sCardTitle, sCardSub, sHint, sTagline, sDock;

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
            selectedResource = ResourceIds.IsPlayable(savedResource) || savedResource == RandomResource
                ? savedResource : ResourceIds.Gold;

            if (hasShownSplash) currentState = ScreenState.Lobby;
            else if (SFXManager.Instance != null) SFXManager.Instance.PlaySplash();
            if (OpenPracticeOnLoad) { OpenPracticeOnLoad = false; hasShownSplash = true; currentState = ScreenState.Practice; }

            LoadTextures();
        }

        private void Start()
        {
            if (EnerGoProgress.Current.pendingCapture != null)
            {
                ResourceIds.Selected = EnerGoProgress.Current.pendingCapture.resourceId;
                hasShownSplash = true;
                transitionScene = CaptureSceneName();
                currentState = ScreenState.TransitioningToAR;
            }
            else if (currentState == ScreenState.Lobby)
            {
                if (SFXManager.Instance != null) SFXManager.Instance.PlayLobbyMusic();
            }
        }

        private void LoadTextures()
        {
            texSymbol = Resources.Load<Texture2D>("UI/logo_game");
            texWordmark = Resources.Load<Texture2D>("UI/logo_wordmark");
        }

        private void Update()
        {
            if (inputCooldownTimer > 0f) inputCooldownTimer -= Time.deltaTime;

            if (isConfirmingReset)
            {
                resetConfirmTimer -= Time.deltaTime;
                if (resetConfirmTimer <= 0f) isConfirmingReset = false;
            }

            if (currentState == ScreenState.Splash)
            {
                splashTimer += Time.deltaTime;
                bool tapped = false;
                if (splashTimer > 0.35f)
                {
                    foreach (var touch in EnhancedTouch.activeTouches)
                        if (touch.phase == UnityEngine.InputSystem.TouchPhase.Began) { tapped = true; break; }
#if UNITY_EDITOR
                    if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) tapped = true;
#endif
                }
                if (tapped || splashTimer >= SplashDuration)
                {
                    hasShownSplash = true;
                    currentState = ScreenState.Lobby;
                    inputCooldownTimer = 0.25f;
                    if (SFXManager.Instance != null) SFXManager.Instance.PlayLobbyMusic();
                }
            }
            else if (currentState == ScreenState.TransitioningToAR)
            {
                transitionTimer += Time.deltaTime;
                if (transitionTimer >= TransitionDuration)
                {
                    if (SFXManager.Instance != null) SFXManager.Instance.StopLobbyMusic();
                    SceneManager.LoadScene(transitionScene ?? CaptureSceneName());
                }
            }
        }

        private void InitStyles()
        {
            if (sTitle != null) return;
            sTitle = Text(22, true, Ink);
            sBody = Text(14, false, Ink);
            sMuted = Text(14, false, Muted);
            sCardTitle = Text(16, true, Ink, TextAnchor.MiddleLeft);
            sCardSub = Text(13, false, Muted, TextAnchor.MiddleLeft);
            sHint = Text(13, true, Color.white);
            sTagline = Text(16, true, Color.white);
            sDock = Text(12, true, Color.white);
        }

        private void OnGUI()
        {
            InitStyles();
            if (texSymbol == null) LoadTextures();

            var prevMatrix = GUI.matrix;
            var prevColor = GUI.color;
            float scale = HudScale;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            float sw = Screen.width / scale;
            float sh = Screen.height / scale;

            DrawWorld(sw, sh);

            if (currentState == ScreenState.Splash)
            {
                DrawSplash(sw, sh);
            }
            else
            {
                DrawLobby(sw, sh);
                if (currentState == ScreenState.Settings) DrawSettings(sw, sh);
                if (currentState == ScreenState.Practice) DrawPractice(sw, sh);
                if (currentState == ScreenState.Guides) DrawGuides(sw, sh);
                if (currentState == ScreenState.TransitioningToAR) DrawTransition(sw, sh);
            }

            GUI.color = prevColor;
            GUI.matrix = prevMatrix;
        }

        // Symbol + wordmark centred at `y`; returns the bottom edge.
        private float DrawBrand(float sw, float y, float symbolH, float alpha = 1f)
        {
            GUI.color = new Color(1f, 1f, 1f, alpha);
            if (texSymbol != null)
            {
                float w = symbolH * texSymbol.width / texSymbol.height;
                GUI.DrawTexture(new Rect((sw - w) * 0.5f, y, w, symbolH), texSymbol, ScaleMode.ScaleToFit);
            }
            y += symbolH + symbolH * 0.22f;
            if (texWordmark != null)
            {
                // The wordmark is dark ink on transparent: sit it on a paper pill so it reads on the teal sky.
                float h = symbolH * 0.34f, w = h * texWordmark.width / texWordmark.height;
                var pill = new Rect((sw - w) * 0.5f - h * 0.6f, y - h * 0.3f, w + h * 1.2f, h * 1.6f);
                DrawButton(pill, ButtonKind.Paper);
                GUI.DrawTexture(new Rect((sw - w) * 0.5f, y, w, h), texWordmark, ScaleMode.ScaleToFit);
                y = pill.yMax;
            }
            GUI.color = Color.white;
            return y;
        }

        private void DrawSplash(float sw, float sh)
        {
            float alpha = 1f;
            if (splashTimer < SplashFadeIn) alpha = Mathf.Clamp01(splashTimer / SplashFadeIn);
            else if (splashTimer > SplashDuration - SplashFadeOut) alpha = Mathf.Clamp01((SplashDuration - splashTimer) / SplashFadeOut);

            // Symbol settles in from slightly smaller; the crystal "clicks" into place.
            float entrance = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(splashTimer / 0.6f));
            float symbolH = Mathf.Min(150f, sw * 0.34f) * Mathf.Lerp(0.88f, 1f, entrance);
            float bottom = DrawBrand(sw, sh * 0.24f + (150f - symbolH) * 0.5f, symbolH, alpha);

            GUI.color = new Color(1f, 1f, 1f, alpha);
            DrawStrokedLabel(new Rect(20, bottom + 18f, sw - 40, 26), "Jelajahi sumber daya, temukan energinya.", sTagline, M(1.5f));

            float barW = Mathf.Min(260f, sw - 100f);
            float fill = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(splashTimer / (SplashDuration - 0.2f)));
            DrawBar(new Rect((sw - barW) * 0.5f, sh * 0.70f, barW, M(14f)), fill, OrangeTop, OrangeBot);

            if (splashTimer > 0.35f)
            {
                GUI.color = new Color(1f, 1f, 1f, alpha * (0.6f + 0.4f * Mathf.Sin(splashTimer * 4f)));
                DrawStrokedLabel(new Rect(20, sh - SafeBottom(HudScale) - 34f, sw - 40, 30), "SENTUH LAYAR UNTUK MULAI", Title(20, Color.white), M(2f), M(1.5f));
            }
            GUI.color = Color.white;
        }

        private void DrawLobby(float sw, float sh)
        {
            float scale = HudScale;
            bool interactive = currentState == ScreenState.Lobby && inputCooldownTimer <= 0f;
            float pad = 20f, contentW = Mathf.Min(sw - pad * 2f, 440f), x = (sw - contentW) * 0.5f;
            float top = SafeTop(scale), bottom = sh - SafeBottom(scale);

            // Top: total units over every resource.
            var totals = new int[AllResources.Length];
            for (int i = 0; i < AllResources.Length; i++) totals[i] = EnerGoProgress.Current.Get(AllResources[i]).Total;
            string total = UiLayout.TotalUnits(totals) + " unit";
            var pillStyle = Title(20, Color.white, TextAnchor.MiddleLeft);
            float badge = M(26f), pillW = badge + pillStyle.CalcSize(new GUIContent(total)).x + M(22f);
            var pill = new Rect(x, top, pillW, M(28f));
            DrawPill(pill);
            DrawPanel(new Rect(pill.x + M(3f), pill.center.y - badge * 0.5f, badge, badge), Sun, Ink, badge * 0.5f, M(3f));
            DrawIcon(new Rect(pill.x + M(3f) + badge * 0.2f, pill.center.y - badge * 0.3f, badge * 0.6f, badge * 0.6f), "bolt");
            GUI.Label(new Rect(pill.x + badge + M(10f), pill.y, pill.width, pill.height), total, pillStyle);

            // Bottom up: dock, start button, camera-mode hint, tiles; the title is centred in what is left.
            float dockH = M(66f);
            var dock = new Rect(x, bottom - dockH, contentW, dockH);
            var playRect = new Rect(x + M(6f), dock.y - M(18f) - M(74f) * 0.75f, contentW - M(12f), M(74f) * 0.75f);
            float cellH = M(82f) * 0.75f, gap = M(10f);
            float gridH = cellH * 2f + gap;
            var grid = new Rect(x, playRect.y - M(16f) - 22f - gridH, contentW, gridH);
            var section = new Rect(x, grid.y - M(16f) - M(26f), contentW, M(26f));

            float titleH = 72f;
            float titleY = Mathf.Lerp(pill.yMax + 12f, section.y - titleH - 12f, 0.5f);
            var titleStyle = Title(Mathf.RoundToInt(M(56f) * 0.75f), Color.white);
            float enerW = titleStyle.CalcSize(new GUIContent("ENER")).x, goW = Title(titleStyle.fontSize, TitleOrange).CalcSize(new GUIContent("GO!")).x;
            float tx = (sw - enerW - goW) * 0.5f;
            DrawStrokedLabel(new Rect(tx, titleY, enerW + 4f, titleH), "ENER", Title(titleStyle.fontSize, Color.white, TextAnchor.MiddleLeft), M(3f), M(5f));
            DrawStrokedLabel(new Rect(tx + enerW, titleY, goW + 4f, titleH), "GO!", Title(titleStyle.fontSize, TitleOrange, TextAnchor.MiddleLeft), M(3f), M(5f));

            var sectionStyle = Title(Mathf.RoundToInt(M(13f)), Sun);
            float sectionW = sectionStyle.CalcSize(new GUIContent("KOLEKSI ENERGIMU")).x + M(36f);
            var sectionPill = new Rect((sw - sectionW) * 0.5f, section.y, sectionW, section.height);
            DrawPill(sectionPill);
            GUI.Label(sectionPill, "KOLEKSI ENERGIMU", sectionStyle);

            // Collection: what the player has caught. Targets come from the radar, so these are not buttons.
            var cells = UiLayout.Grid(grid, AllResources.Length, 3, gap, cellH);
            for (int i = 0; i < cells.Length; i++)
            {
                string id = AllResources[i];
                bool playable = ResourceIds.IsPlayable(id);
                GUI.color = new Color(1f, 1f, 1f, playable ? 1f : 0.45f);
                DrawTile(cells[i], id, EnerGoProgress.Current.Get(id).Total);
                GUI.color = Color.white;
                if (!playable) DrawLock(new Vector2(cells[i].center.x, cells[i].center.y), 16f);
            }

            DrawStrokedLabel(new Rect(x, playRect.y - 26f, contentW, 20f),
                useARCore ? "Mode kamera: AR (ARCore)" : "Mode kamera: Sensor 360", sHint, M(1.2f));
            var playFace = DrawButton(playRect, ButtonKind.Orange, IsHeld(playRect, scale));
            ButtonLabel(playFace, "BERBURU!", Mathf.RoundToInt(M(30f) * 0.9f), ButtonKind.Orange);
            if (interactive && ButtonHit(playRect))
            {
                if (SFXManager.Instance != null) SFXManager.Instance.PlayButtonClick();
                ExploreSession.PendingSpawnId = null;
                transitionScene = "ExploreScene";
                currentState = ScreenState.TransitioningToAR;
                transitionTimer = 0f;
            }

            // Dock: Panduan, Latihan, Pengaturan. One way in per feature.
            DrawPill(dock);
            string[] icons = { "book", "target", "gear" };
            string[] labels = { "Panduan", "Latihan", "Pengaturan" };
            ButtonKind[] kinds = { ButtonKind.Sun, ButtonKind.Info, ButtonKind.Paper };
            ScreenState[] targets = { ScreenState.Guides, ScreenState.Practice, ScreenState.Settings };
            float slot = dock.width / 3f, bs = M(40f);
            for (int i = 0; i < 3; i++)
            {
                var b = new Rect(dock.x + slot * i + (slot - bs) * 0.5f, dock.y + M(4f), bs, bs);
                var face = DrawButton(b, kinds[i], IsHeld(b, scale));
                DrawIcon(new Rect(face.x + face.width * 0.22f, face.y + face.height * 0.2f, face.width * 0.56f, face.height * 0.56f), icons[i]);
                DrawStrokedLabel(new Rect(dock.x + slot * i, b.yMax, slot, dock.yMax - b.yMax - M(2f)), labels[i], sDock, M(1.2f));
                var hit = new Rect(dock.x + slot * i, dock.y, slot, dock.height);
                if (interactive && ButtonHit(hit))
                {
                    if (SFXManager.Instance != null) SFXManager.Instance.PlayButtonClick();
                    currentState = targets[i];
                    inputCooldownTimer = 0.2f;
                }
            }
        }

        // Practice tile; true when tapped. The current pick gets an Orange ring.
        private bool DrawPracticeTile(Rect rect, string id, bool interactive)
        {
            bool held = interactive && IsHeld(rect, HudScale);
            if (selectedResource == id)
                DrawPanel(new Rect(rect.x - 5f, rect.y - 5f, rect.width + 10f, rect.height + 10f), Color.clear, Orange, M(22f), 5f);
            DrawTile(rect, id, id == RandomResource ? -1 : EnerGoProgress.Current.Get(id).Total, held);
            return interactive && ButtonHit(rect);
        }

        private void DrawTransition(float sw, float sh)
        {
            float a = Mathf.Clamp01(transitionTimer / TransitionDuration);
            DrawPanel(new Rect(0, 0, sw, sh), Alpha(Ink, a * 0.75f), Color.clear, 0f);
            if (texSymbol != null)
            {
                GUI.color = new Color(1f, 1f, 1f, a);
                GUI.DrawTexture(new Rect((sw - 52f) * 0.5f, sh * 0.44f, 52f, 60f), texSymbol, ScaleMode.ScaleToFit);
            }
            GUI.color = new Color(1f, 1f, 1f, a);
            DrawStrokedLabel(new Rect(20, sh * 0.44f + 72f, sw - 40, 30), transitionScene == "ExploreScene" ? "Menyalakan radar..." : "Menyiapkan kamera...", Title(20, Color.white), M(2f));
            GUI.color = Color.white;
        }

        // Popup frame shared by the three sheets: dimmed world, a paper card anchored to the bottom, a ribbon title.
        // Returns the card, or null when a tap outside closed it.
        private Rect? DrawSheet(float sw, float sh, float contentH, string title)
        {
            float scale = HudScale;
            DrawWorld(sw, sh, 0.45f);
            float w = Mathf.Min(sw - 32f, 448f);
            var card = new Rect((sw - w) * 0.5f, sh - SafeBottom(scale) - M(8f) - contentH, w, contentH);
            if (inputCooldownTimer <= 0f && OutsideTap(card, scale))
            {
                if (SFXManager.Instance != null) SFXManager.Instance.PlayButtonBack();
                currentState = ScreenState.Lobby;
                inputCooldownTimer = 0.2f;
                return null;
            }
            DrawCard(card);
            DrawRibbon(card, title, ButtonKind.Orange);
            return card;
        }

        // AR mode, volume, vibration, practice and progress reset. Tap outside (or SELESAI) to close.
        private void DrawSettings(float sw, float sh)
        {
            float scale = HudScale;
            const float rowH = 68f, btnH = 58f, gap = 12f;
            var sheet = DrawSheet(sw, sh, 44f + rowH * 3f + gap * 3f + btnH * 3f + gap * 2f + 20f, "PENGATURAN");
            if (sheet == null) return;
            var card = sheet.Value;
            bool ready = inputCooldownTimer <= 0f;
            float x = card.x + 18f, w = card.width - 36f, y = card.y + 44f;

            // AR mode switch (on = ARCore scene, off = Sensor 360), remembered across sessions
            if (DrawToggleRow(new Rect(x, y, w, rowH), "Mode AR (ARCore)",
                    useARCore ? "Butuh Google Play Services for AR" : "Mati: pakai Sensor 360 (giroskop)", useARCore, ready))
            {
                useARCore = !useARCore;
                PlayerPrefs.SetInt(PrefUseARCore, useARCore ? 1 : 0);
                PlayerPrefs.Save();
            }
            y += rowH + gap;

            // Master volume: drag along the bar; saved and previewed on release
            var vol = new Rect(x, y, w, rowH);
            DrawPanel(vol, Alpha(Ink, 0.06f), Alpha(Ink, 0.12f), 16f, 1.5f);
            GUI.Label(new Rect(vol.x + 16f, vol.y + 10f, vol.width - 100f, 22f), "Volume", sCardTitle);
            GUI.Label(new Rect(vol.xMax - 76f, vol.y + 10f, 60f, 22f), Mathf.RoundToInt(AudioListener.volume * 100f) + "%",
                new GUIStyle(sCardSub) { alignment = TextAnchor.MiddleRight });
            var bar = new Rect(vol.x + 16f, vol.y + 40f, vol.width - 32f, 16f);
            DrawBar(bar, AudioListener.volume, OrangeTop, OrangeBot);
            DrawPanel(new Rect(bar.x + bar.width * AudioListener.volume - 12f, bar.center.y - 12f, 24f, 24f), Paper, Ink, 12f, 3f);
            if (Event.current.type == EventType.Repaint)
            {
                Vector2? p = HeldPointer(scale);
                if (ready && p != null && (volumeDragging || vol.Contains(p.Value)))
                {
                    volumeDragging = true;
                    AudioListener.volume = Mathf.Clamp01((p.Value.x - bar.x) / bar.width);
                }
                else if (volumeDragging && p == null)
                {
                    volumeDragging = false;
                    SFXManager.SetVolume(AudioListener.volume);
                    if (SFXManager.Instance != null) SFXManager.Instance.PlayButtonClick();
                }
            }
            y += rowH + gap;

            if (DrawToggleRow(new Rect(x, y, w, rowH), "Getaran", Haptics.Enabled ? "HP bergetar saat main" : "Mati: tanpa getaran", Haptics.Enabled, ready))
            {
                Haptics.SetEnabled(!Haptics.Enabled);
                Haptics.Pulse(60); // feel it when switching on
            }
            y += rowH + gap;

            var practiceRect = new Rect(x, y, w, btnH);
            ButtonLabel(DrawButton(practiceRect, ButtonKind.Info, IsHeld(practiceRect, scale)), "LATIHAN (TANPA GPS)", 20, ButtonKind.Info);
            if (ready && ButtonHit(practiceRect))
            {
                if (SFXManager.Instance != null) SFXManager.Instance.PlayButtonClick();
                currentState = ScreenState.Practice;
                inputCooldownTimer = 0.2f;
                return;
            }
            y += btnH + gap;

            // Reset progress: two taps within 3.5 s
            var resetRect = new Rect(x, y, w, btnH);
            var resetFace = DrawButton(resetRect, isConfirmingReset ? ButtonKind.Bad : ButtonKind.Paper, IsHeld(resetRect, scale));
            if (isConfirmingReset) ButtonLabel(resetFace, "TEKAN LAGI UNTUK MENGHAPUS", 18, ButtonKind.Bad);
            else GUI.Label(resetFace, "RESET PROGRES", Title(18, Danger));
            if (ready && ButtonHit(resetRect))
            {
                if (SFXManager.Instance != null) SFXManager.Instance.PlayButtonClick();
                if (!isConfirmingReset) { isConfirmingReset = true; resetConfirmTimer = 3.5f; }
                else if (EnerGoProgress.Reset()) isConfirmingReset = false;
            }
            y += btnH + gap;

            var doneRect = new Rect(x, y, w, btnH);
            ButtonLabel(DrawButton(doneRect, ButtonKind.Orange, IsHeld(doneRect, scale)), "SELESAI", 20, ButtonKind.Orange);
            if (ready && ButtonHit(doneRect))
            {
                if (SFXManager.Instance != null) SFXManager.Instance.PlayButtonBack();
                currentState = ScreenState.Lobby;
                inputCooldownTimer = 0.2f;
            }
        }

        // Practice: pick any SDA (or Acak) and go straight into its mini-game, no GPS.
        private void DrawPractice(float sw, float sh)
        {
            float scale = HudScale;
            float gap = M(10f), cellH = M(82f) * 0.75f, goH = M(74f) * 0.75f * 0.85f;
            var sheet = DrawSheet(sw, sh, 44f + 48f + cellH * 2f + gap + 24f + goH + 20f, "LATIHAN");
            if (sheet == null) return;
            var card = sheet.Value;
            bool ready = inputCooldownTimer <= 0f;
            float x = card.x + 18f, w = card.width - 36f, y = card.y + 40f;
            GUI.Label(new Rect(x, y, w, 22f), "Pilih sumber daya, main tanpa GPS", sMuted);
            y += 52f; // room for the count badges that overhang the tiles
            string[] ids = { ResourceIds.Sun, ResourceIds.Coal, ResourceIds.Gold, ResourceIds.Oil, ResourceIds.Uranium, RandomResource };
            var cells = UiLayout.Grid(new Rect(x, y, w, cellH * 2f + gap), ids.Length, 3, gap, cellH);
            for (int i = 0; i < ids.Length; i++)
            {
                if (DrawPracticeTile(cells[i], ids[i], ready))
                {
                    if (SFXManager.Instance != null) SFXManager.Instance.PlayButtonClick();
                    selectedResource = ids[i];
                    PlayerPrefs.SetString(PrefResource, ids[i]);
                    PlayerPrefs.Save();
                    inputCooldownTimer = 0.15f;
                }
            }
            y += cellH * 2f + gap + 24f;
            var go = new Rect(x, y, w, goH);
            ButtonLabel(DrawButton(go, ButtonKind.Orange, IsHeld(go, scale)), "MULAI LATIHAN", 26, ButtonKind.Orange);
            if (ready && ButtonHit(go))
            {
                if (SFXManager.Instance != null) SFXManager.Instance.PlayButtonClick();
                ResourceIds.Selected = selectedResource == RandomResource
                    ? AllResources[Random.Range(0, AllResources.Length)] : selectedResource;
                ExploreSession.PendingSpawnId = null;
                transitionScene = CaptureSceneName();
                currentState = ScreenState.TransitioningToAR;
                transitionTimer = 0f;
            }
        }

        // PANDUAN: the 5 SDA as tiles; tap one to read its guide. Also resets every "Jangan ingatkan lagi".
        private void DrawGuides(float sw, float sh)
        {
            float scale = HudScale;
            if (lobbyGuide != null)
            {
                if (lobbyGuide.Draw(sw, sh, scale) != GuideViewer.Result.None) { lobbyGuide = null; inputCooldownTimer = 0.2f; }
                return;
            }
            float gap = M(10f), cellH = M(82f) * 0.75f, btnH = 58f;
            var sheet = DrawSheet(sw, sh, 44f + 24f + cellH * 2f + gap + 24f + btnH + 34f, "PANDUAN");
            if (sheet == null) return;
            var card = sheet.Value;
            bool ready = inputCooldownTimer <= 0f;
            float x = card.x + 18f, w = card.width - 36f, y = card.y + 40f;
            GUI.Label(new Rect(x, y, w, 22f), "Ketuk untuk membaca cara mainnya", sMuted);
            y += 28f;
            var cells = UiLayout.Grid(new Rect(x, y, w, cellH * 2f + gap), GuideBook.Ids.Length, 3, gap, cellH);
            for (int i = 0; i < GuideBook.Ids.Length; i++)
            {
                DrawTile(cells[i], GuideBook.Ids[i], -1, ready && IsHeld(cells[i], scale));
                if (ready && ButtonHit(cells[i]))
                {
                    if (SFXManager.Instance != null) SFXManager.Instance.PlayButtonClick();
                    lobbyGuide = new GuideViewer(GuideBook.Ids[i], withStart: false);
                    inputCooldownTimer = 0.15f;
                }
            }
            y += cellH * 2f + gap + 24f;
            var reset = new Rect(x, y, w, btnH);
            GUI.Label(DrawButton(reset, ButtonKind.Paper, IsHeld(reset, scale)), "TAMPILKAN SEMUA PANDUAN LAGI", Title(17, Ink));
            if (ready && ButtonHit(reset))
            {
                if (SFXManager.Instance != null) SFXManager.Instance.PlayButtonClick();
                GuideViewer.ResetAll();
                guidesResetAt = Time.time;
            }
            if (Time.time - guidesResetAt < 2f)
                GUI.Label(new Rect(x, reset.yMax + 6f, w, 22f), "Panduan akan muncul lagi", sMuted);
        }

        // Row with a title, a subtitle and an on/off switch; true when tapped.
        private bool DrawToggleRow(Rect row, string title, string sub, bool on, bool ready)
        {
            DrawPanel(row, Alpha(Ink, 0.06f), Alpha(Ink, 0.12f), 16f, 1.5f);
            GUI.Label(new Rect(row.x + 16f, row.y + 12f, row.width - 110f, 22f), title, sCardTitle);
            GUI.Label(new Rect(row.x + 16f, row.y + 36f, row.width - 110f, 20f), sub, sCardSub);
            var track = new Rect(row.xMax - 16f - 64f, row.center.y - 20f, 64f, 40f);
            var face = DrawButton(track, on ? ButtonKind.Ok : ButtonKind.Off);
            float k = face.height - 12f;
            DrawPanel(new Rect(on ? face.xMax - k - 6f : face.x + 6f, face.y + 6f, k, k), Paper, Ink, k * 0.5f, 3f);
            if (!ready || !ButtonHit(row)) return false;
            if (SFXManager.Instance != null) SFXManager.Instance.PlayButtonClick();
            inputCooldownTimer = 0.15f;
            return true;
        }

        // GUI-space position of a held finger or mouse button, or null.
        private static Vector2? HeldPointer(float scale)
        {
            foreach (var t in EnhancedTouch.activeTouches)
                return new Vector2(t.screenPosition.x / scale, (Screen.height - t.screenPosition.y) / scale);
            var mouse = UnityEngine.InputSystem.Mouse.current;
            return mouse != null && mouse.leftButton.isPressed ? Event.current.mousePosition : (Vector2?)null;
        }

        // A tap or click that started outside `rect`.
        private bool OutsideTap(Rect rect, float scale)
        {
            if (Event.current.type == EventType.MouseDown && Event.current.pointerType != PointerType.Touch
                && !rect.Contains(Event.current.mousePosition))
            {
                Event.current.Use();
                return true;
            }
            if (Event.current.type != EventType.Repaint) return false;
            foreach (var touch in EnhancedTouch.activeTouches)
            {
                if (touch.phase != UnityEngine.InputSystem.TouchPhase.Began) continue;
                var p = new Vector2(touch.screenPosition.x / scale, (Screen.height - touch.screenPosition.y) / scale);
                if (!rect.Contains(p)) return true;
            }
            return false;
        }

        private bool ButtonHit(Rect rect) => TapHit(rect, HudScale);
    }
}
