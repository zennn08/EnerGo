using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using EnhancedTouch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using static EnerGo.EnerGoUI;

namespace EnerGo
{
    // Splash, lobby (resource picker + start) and the settings sheet, drawn with IMGUI in the Solar Clean style.
    public class LobbyController : MonoBehaviour
    {
        public enum ScreenState
        {
            Splash,
            Lobby,
            Settings,
            TransitioningToAR
        }

        [Header("State")]
        public ScreenState currentState = ScreenState.Splash;

        // Static flag so Splash Screen only shows once per app session
        public static bool hasShownSplash = false;

        private Texture2D texSymbol;
        private Texture2D texWordmark;

        private float splashTimer = 0f;
        private const float SplashDuration = 2.4f;
        private const float SplashFadeIn = 0.5f;
        private const float SplashFadeOut = 0.4f;

        private float transitionTimer = 0f;
        private const float TransitionDuration = 0.45f;

        private float inputCooldownTimer = 0.2f;

        private bool isConfirmingReset = false;
        private float resetConfirmTimer = 0f;

        private string selectedResource = ResourceIds.Gold;
        private bool useARCore;
        private const string PrefUseARCore = "EnerGo.UseARCore";
        private const string PrefResource = "EnerGo.SelectedResource";
        private const string RandomResource = "random"; // a different SDA picked on every PLAY
        private static readonly string[] AllResources = { ResourceIds.Sun, ResourceIds.Coal, ResourceIds.Gold };

        private GUIStyle sTitle, sBody, sMuted, sSection, sCardTitle, sCardSub, sCount, sPrimary, sSecondary, sDanger, sHint;

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

            if (hasShownSplash) currentState = ScreenState.Lobby;
            else if (SFXManager.Instance != null) SFXManager.Instance.PlaySplash();

            LoadTextures();
        }

        private void Start()
        {
            if (EnerGoProgress.Current.pendingCapture != null)
            {
                ResourceIds.Selected = EnerGoProgress.Current.pendingCapture.resourceId;
                hasShownSplash = true;
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
                    SceneManager.LoadScene(useARCore ? "ARPrototype" : "Sensor360Scene");
                }
            }
        }

        private void InitStyles()
        {
            if (sTitle != null) return;
            sTitle = Text(22, true, Ink);
            sBody = Text(14, false, Ink);
            sMuted = Text(13, false, Muted);
            sSection = Text(11, true, Muted, TextAnchor.MiddleLeft);
            sCardTitle = Text(15, true, Ink, TextAnchor.MiddleLeft);
            sCardSub = Text(12, false, Muted, TextAnchor.MiddleLeft);
            sCount = Text(22, true, Ink, TextAnchor.MiddleLeft);
            sPrimary = Text(18, true, Paper);
            sSecondary = Text(14, true, Ink);
            sDanger = Text(14, true, Danger);
            sHint = Text(12, true, Muted);
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

            GUI.color = Cream;
            GUI.DrawTexture(new Rect(0, 0, sw, sh), Texture2D.whiteTexture);
            GUI.color = Color.white;

            if (currentState == ScreenState.Splash)
            {
                DrawSplash(sw, sh);
            }
            else
            {
                DrawLobby(sw, sh);
                if (currentState == ScreenState.Settings) DrawSettings(sw, sh);
                if (currentState == ScreenState.TransitioningToAR) DrawTransition(sw, sh);
            }

            GUI.color = prevColor;
            GUI.matrix = prevMatrix;
        }

        private float SafeTop(float scale) => Mathf.Max(16f, (Screen.height - Screen.safeArea.yMax) / scale + 12f);
        private float SafeBottom(float scale) => Mathf.Max(20f, Screen.safeArea.yMin / scale + 16f);

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
                float h = symbolH * 0.34f, w = h * texWordmark.width / texWordmark.height;
                GUI.DrawTexture(new Rect((sw - w) * 0.5f, y, w, h), texWordmark, ScaleMode.ScaleToFit);
                y += h;
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
            float bottom = DrawBrand(sw, sh * 0.30f + (150f - symbolH) * 0.5f, symbolH, alpha);

            GUI.color = new Color(1f, 1f, 1f, alpha);
            GUI.Label(new Rect(20, bottom + 14f, sw - 40, 22), "Jelajahi sumber daya, temukan energinya.", sMuted);

            float barW = Mathf.Min(220f, sw - 100f);
            float fill = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(splashTimer / (SplashDuration - 0.2f)));
            DrawBar(new Rect((sw - barW) * 0.5f, sh * 0.72f, barW, 6f), fill, Orange);

            if (splashTimer > 0.35f)
            {
                GUI.color = new Color(1f, 1f, 1f, alpha * (0.6f + 0.4f * Mathf.Sin(splashTimer * 4f)));
                GUI.Label(new Rect(20, sh - SafeBottom(HudScale) - 24f, sw - 40, 22), "SENTUH LAYAR UNTUK MULAI", sHint);
            }
            GUI.color = Color.white;
        }

        private void DrawLobby(float sw, float sh)
        {
            float scale = HudScale;
            bool interactive = currentState == ScreenState.Lobby && inputCooldownTimer <= 0f;
            float pad = 20f, contentW = Mathf.Min(sw - pad * 2f, 440f), x = (sw - contentW) * 0.5f;

            // Big faint crystal bleeding off the bottom-right corner: brand texture without noise.
            if (texSymbol != null)
            {
                GUI.color = new Color(1f, 1f, 1f, 0.06f);
                float h = sh * 0.55f;
                GUI.DrawTexture(new Rect(sw - h * 0.55f, sh - h * 0.62f, h * 0.866f, h), texSymbol, ScaleMode.ScaleToFit);
                GUI.color = Color.white;
            }

            // Settings (top-right): round button with a three-line glyph.
            float top = SafeTop(scale);
            var setRect = new Rect(x + contentW - 48f, top, 48f, 52f);
            var face = DrawSecondaryButton(setRect, IsHeld(setRect, scale));
            for (int i = -1; i <= 1; i++)
                DrawPanel(new Rect(face.center.x - 9f, face.center.y - 1.25f + i * 6f, 18f, 2.5f), Ink, Color.clear, 1.25f);
            if (interactive && ButtonHit(setRect))
            {
                if (SFXManager.Instance != null) SFXManager.Instance.PlayButtonClick();
                currentState = ScreenState.Settings;
                inputCooldownTimer = 0.2f;
            }

            // Layout from the bottom up: start button, mode line, resource cards; the brand is centred in what is left.
            float btnH = 66f, gap = 12f, cardW = (contentW - gap) * 0.5f, cardH = 76f;
            var playRect = new Rect(x, sh - SafeBottom(scale) - btnH, contentW, btnH);
            float cardsTop = playRect.y - 44f - (cardH * 2f + gap);
            float symbolH = Mathf.Min(112f, sh * 0.13f);
            float brandH = symbolH * 1.56f + 32f;
            float brandTop = top + 52f + Mathf.Max(0f, (cardsTop - 26f - 24f - (top + 52f) - brandH) * 0.45f);

            float y = DrawBrand(sw, brandTop, symbolH);
            GUI.Label(new Rect(x, y + 10f, contentW, 22f), "Jelajahi sumber daya, temukan energinya.", sMuted);

            // Resource picker: 2x2 cards, the inventory lives on the cards.
            y = cardsTop - 26f;
            GUI.Label(new Rect(x, y, contentW, 18f), "PILIH TARGET BURUAN", sSection);
            y += 26f;
            var save = EnerGoProgress.Current;
            string[] ids = { ResourceIds.Sun, ResourceIds.Coal, ResourceIds.Gold, RandomResource };
            for (int i = 0; i < ids.Length; i++)
            {
                var rect = new Rect(x + (i % 2) * (cardW + gap), y + (i / 2) * (cardH + gap), cardW, cardH);
                bool random = ids[i] == RandomResource;
                string title = random ? "Acak" : ResourceIds.Name(ids[i]);
                if (DrawResourceCard(rect, ids[i], title, random ? -1 : save.Get(ids[i]).Total, interactive))
                {
                    if (SFXManager.Instance != null) SFXManager.Instance.PlayButtonClick();
                    selectedResource = ids[i];
                    PlayerPrefs.SetString(PrefResource, ids[i]);
                    PlayerPrefs.Save();
                    inputCooldownTimer = 0.15f;
                }
            }

            // Start
            GUI.Label(new Rect(x, playRect.y - 30f, contentW, 20f),
                useARCore ? "Mode kamera: AR (ARCore)" : "Mode kamera: Sensor 360", sHint);
            var playFace = DrawPrimaryButton(playRect, IsHeld(playRect, scale));
            GUI.Label(playFace, "MULAI BERBURU", sPrimary);
            if (interactive && ButtonHit(playRect))
            {
                if (SFXManager.Instance != null) SFXManager.Instance.PlayButtonClick();
                ResourceIds.Selected = selectedResource == RandomResource
                    ? AllResources[Random.Range(0, AllResources.Length)] : selectedResource;
                currentState = ScreenState.TransitioningToAR;
                transitionTimer = 0f;
            }
        }

        // `count` < 0 = no inventory to show (the "Acak" card).
        private bool DrawResourceCard(Rect rect, string id, string title, int count, bool interactive)
        {
            bool selected = selectedResource == id;
            bool held = interactive && IsHeld(rect, HudScale);
            var r = held ? new Rect(rect.x, rect.y + 2f, rect.width, rect.height) : rect;
            DrawCard(r, Radius, selected ? Ink : Line, selected ? 2.5f : 1.5f);

            // Left: the brand crystal in the resource's colours ("Acak" gets the brand colours), centred vertically.
            const float iconW = 30f, iconH = 35f;
            var icon = CrystalIcon(id);
            var iconRect = new Rect(r.x + 14f, r.center.y - iconH * 0.5f, iconW, iconH);
            if (icon != null) GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit);
            else DrawPanel(iconRect, Resource(id), Color.clear, 8f);

            if (selected) // orange dot = current pick
                DrawPanel(new Rect(r.xMax - 22f, r.y + 12f, 10f, 10f), Orange, Color.clear, 5f);

            // Right: name on top, then the count as a big number with a small "unit" after it.
            float tx = iconRect.xMax + 12f, tw = r.xMax - 14f - tx;
            GUI.Label(new Rect(tx, r.y + 14f, tw - 12f, 20f), title, sCardTitle);
            if (count < 0)
                GUI.Label(new Rect(tx, r.y + 38f, tw, 22f), "Kejutan tiap main", sCardSub);
            else
            {
                string n = count.ToString();
                float nw = sCount.CalcSize(new GUIContent(n)).x;
                GUI.Label(new Rect(tx, r.y + 34f, nw, 28f), n, sCount);
                GUI.Label(new Rect(tx + nw + 5f, r.y + 41f, tw - nw - 5f, 18f), "unit", sCardSub);
            }
            return interactive && ButtonHit(rect);
        }

        private readonly System.Collections.Generic.Dictionary<string, Texture2D> crystals = new System.Collections.Generic.Dictionary<string, Texture2D>();

        private Texture2D CrystalIcon(string id)
        {
            if (!crystals.TryGetValue(id, out var tex) || tex == null)
                crystals[id] = tex = Resources.Load<Texture2D>("UI/crystal_" + id);
            return tex;
        }

        private void DrawTransition(float sw, float sh)
        {
            float a = Mathf.Clamp01(transitionTimer / TransitionDuration);
            GUI.color = Alpha(Cream, a);
            GUI.DrawTexture(new Rect(0, 0, sw, sh), Texture2D.whiteTexture);
            if (texSymbol != null)
            {
                GUI.color = new Color(1f, 1f, 1f, a);
                GUI.DrawTexture(new Rect((sw - 52f) * 0.5f, sh * 0.44f, 52f, 60f), texSymbol, ScaleMode.ScaleToFit);
            }
            GUI.color = new Color(1f, 1f, 1f, a);
            GUI.Label(new Rect(20, sh * 0.44f + 72f, sw - 40, 22), "Menyiapkan kamera...", sMuted);
            GUI.color = Color.white;
        }

        // Bottom sheet: AR mode switch and progress reset. Tap outside (or SELESAI) to close.
        private void DrawSettings(float sw, float sh)
        {
            float scale = HudScale;
            GUI.color = Alpha(Ink, 0.45f);
            GUI.DrawTexture(new Rect(0, 0, sw, sh), Texture2D.whiteTexture);
            GUI.color = Color.white;

            float sheetH = 330f + SafeBottom(scale);
            var sheet = new Rect(0, sh - sheetH, sw, sheetH + 30f); // extra height hides the bottom corners
            bool ready = inputCooldownTimer <= 0f;

            if (ready && OutsideTap(sheet, scale))
            {
                if (SFXManager.Instance != null) SFXManager.Instance.PlayButtonBack();
                currentState = ScreenState.Lobby;
                inputCooldownTimer = 0.2f;
                return;
            }

            DrawPanel(sheet, Cream, Color.clear, 24f);
            float pad = 20f, w = Mathf.Min(sw - pad * 2f, 440f), x = (sw - w) * 0.5f, y = sheet.y + 12f;
            DrawPanel(new Rect((sw - 40f) * 0.5f, y, 40f, 4f), Line, Color.clear, 2f);
            y += 20f;
            GUI.Label(new Rect(x, y, w, 30f), "Pengaturan", new GUIStyle(sTitle) { alignment = TextAnchor.MiddleLeft });
            y += 46f;

            // AR mode switch (on = ARCore scene, off = Sensor 360), remembered across sessions
            var row = new Rect(x, y, w, 68f);
            DrawCard(row);
            GUI.Label(new Rect(row.x + 16f, row.y + 14f, row.width - 100f, 20f), "Mode AR (ARCore)", sCardTitle);
            GUI.Label(new Rect(row.x + 16f, row.y + 36f, row.width - 100f, 18f),
                useARCore ? "Butuh Google Play Services for AR" : "Mati: pakai Sensor 360 (giroskop)", sCardSub);
            var track = new Rect(row.xMax - 16f - 52f, row.center.y - 15f, 52f, 30f);
            DrawPanel(track, useARCore ? Orange : Line, Color.clear, 15f);
            DrawPanel(new Rect(useARCore ? track.xMax - 27f : track.x + 3f, track.y + 3f, 24f, 24f), Paper, Color.clear, 12f);
            if (ready && ButtonHit(row))
            {
                if (SFXManager.Instance != null) SFXManager.Instance.PlayButtonClick();
                useARCore = !useARCore;
                PlayerPrefs.SetInt(PrefUseARCore, useARCore ? 1 : 0);
                PlayerPrefs.Save();
                inputCooldownTimer = 0.15f;
            }
            y += 68f + 14f;

            // Reset progress: two taps within 3.5 s
            var resetRect = new Rect(x, y, w, 54f);
            var resetFace = DrawSecondaryButton(resetRect, IsHeld(resetRect, scale));
            GUI.Label(resetFace, isConfirmingReset ? "TEKAN LAGI UNTUK MENGHAPUS" : "RESET PROGRES", sDanger);
            if (ready && ButtonHit(resetRect))
            {
                if (SFXManager.Instance != null) SFXManager.Instance.PlayButtonClick();
                if (!isConfirmingReset) { isConfirmingReset = true; resetConfirmTimer = 3.5f; }
                else if (EnerGoProgress.Reset()) isConfirmingReset = false;
            }
            y += 54f + 14f;

            var doneRect = new Rect(x, y, w, 58f);
            GUI.Label(DrawDarkButton(doneRect, IsHeld(doneRect, scale)), "SELESAI", new GUIStyle(sPrimary) { fontSize = 15 });
            if (ready && ButtonHit(doneRect))
            {
                if (SFXManager.Instance != null) SFXManager.Instance.PlayButtonBack();
                currentState = ScreenState.Lobby;
                inputCooldownTimer = 0.2f;
            }
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
