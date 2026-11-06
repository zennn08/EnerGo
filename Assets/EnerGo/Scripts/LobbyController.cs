using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using EnhancedTouch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace EnerGo
{
    // Splash, lobby (collection + start + dock) and the settings, practice and guide sheets.
    // Owns the state, timers and side effects; the UI Toolkit LobbyView draws it and calls the actions below.
    // Each action is the body of the matching IMGUI button from before the UI Toolkit move, unchanged.
    public class LobbyController : MonoBehaviour
    {
        public enum ScreenState
        {
            Splash,
            Lobby,
            Settings,
            Practice,
            Guides,
            TransitioningToAR,
            Prolog,
            Desa
        }

        [Header("State")]
        public ScreenState currentState = ScreenState.Splash;

        // Static flag so Splash Screen only shows once per app session
        public static bool hasShownSplash = false;

        // Set by the radar's "Main mode Latihan" so the lobby opens straight onto the practice picker.
        public static bool OpenPracticeOnLoad;

        public static string CaptureSceneName() => PlayerPrefs.GetInt(PrefUseARCore, 0) == 1 ? "ARPrototype" : "Sensor360Scene";

        // Fired whenever something the view shows changes through this controller.
        public event System.Action StateChanged;

        private float splashTimer = 0f;
        private const float SplashDuration = 2.4f;
        public const float SplashFadeIn = 0.5f;
        public const float SplashFadeOut = 0.4f;

        private float transitionTimer = 0f;
        private string transitionScene;
        private const float TransitionDuration = 0.45f;

        private float inputCooldownTimer = 0.2f;

        private float guidesResetAt = -10f;

        private bool isConfirmingReset = false;
        private float resetConfirmTimer = 0f;
        public const float ResetConfirmSeconds = 3.5f;

        private string selectedResource = ResourceIds.Gold;
        private bool useARCore;
        public const string PrefUseARCore = "EnerGo.UseARCore";
        private const string PrefResource = "EnerGo.SelectedResource";
        public const string RandomResource = "random"; // a different SDA picked on every PLAY
        public static readonly string[] AllResources = { ResourceIds.Sun, ResourceIds.Coal, ResourceIds.Gold, ResourceIds.Oil, ResourceIds.Uranium };

        // ── State the view reads ──────────────────────────────────────
        public ScreenState State => currentState;
        public bool InputReady => inputCooldownTimer <= 0f && Time.unscaledTime >= tapBlockedUntil;

        // Every tap is swallowed for 0.35 s after one lands, so one physical tap presses one button and a quick double
        // tap cannot, say, confirm RESET PROGRES at once.
        private float tapBlockedUntil;
        private void BlockTaps() => tapBlockedUntil = Time.unscaledTime + 0.35f;
        public float SplashTimer => splashTimer;
        public float SplashSeconds => SplashDuration;
        public float TransitionProgress => Mathf.Clamp01(transitionTimer / TransitionDuration);
        public string TransitionText => transitionScene == "ExploreScene" ? "Menyalakan radar..." : "Menyiapkan kamera...";
        public bool UseARCore => useARCore;
        public string SelectedPractice => selectedResource;
        public bool IsConfirmingReset => isConfirmingReset;
        public float ResetConfirmRemaining => isConfirmingReset ? Mathf.Max(0f, resetConfirmTimer) : 0f;
        public bool GuidesResetNoticeVisible => Time.time - guidesResetAt < 2f;
        public string OpenGuideId { get; private set; } // guide card open over the PANDUAN sheet, or null
        public bool NeedsProlog => !EnerGoProgress.Current.prologSeen;
        public long NowUtc => System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        public long PlantRemaining => DesaMath.Remaining(EnerGoProgress.Current.plantEndsUtc, NowUtc);
        public DesaStatus PlantStatus => DesaMath.StatusOf(PlantRemaining);

        // The lobby's village button draws the eye when the first fill is waiting, or the stock runs low.
        public bool DesaMarked
        {
            get
            {
                var save = EnerGoProgress.Current;
                if (save.plantEverFilled) return PlantStatus == DesaStatus.Menipis;
                foreach (var id in AllResources)
                    if (save.Get(id).Total > 0) return true;
                return false;
            }
        }

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
            Changed();
        }

        private void Update()
        {
            if (inputCooldownTimer > 0f) inputCooldownTimer -= Time.deltaTime;

            if (isConfirmingReset)
            {
                resetConfirmTimer -= Time.deltaTime;
                if (resetConfirmTimer <= 0f) { isConfirmingReset = false; Changed(); }
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
                    Changed();
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

        private void Changed() => StateChanged?.Invoke();
        // Every button action plays one of these, so they also start the 0.35 s tap block.
        private void Click() { BlockTaps(); if (SFXManager.Instance != null) SFXManager.Instance.PlayButtonClick(); }
        private void Back() { BlockTaps(); if (SFXManager.Instance != null) SFXManager.Instance.PlayButtonBack(); }

        // ── Lobby ─────────────────────────────────────────────────────
        public void StartHunt()
        {
            if (currentState != ScreenState.Lobby || !InputReady) return;
            Click();
            ExploreSession.PendingSpawnId = null;
            transitionScene = "ExploreScene";
            currentState = ScreenState.TransitioningToAR;
            transitionTimer = 0f;
            Changed();
        }

        // Dock: Panduan, Latihan, Pengaturan. One way in per feature.
        public void OpenSheet(ScreenState sheet)
        {
            if (currentState != ScreenState.Lobby || !InputReady) return;
            if (sheet != ScreenState.Guides && sheet != ScreenState.Practice && sheet != ScreenState.Settings) return;
            Click();
            currentState = sheet;
            inputCooldownTimer = 0.2f;
            Changed();
        }

        // Tap outside the card, or SELESAI.
        public void CloseSheet()
        {
            if (!IsSheet(currentState) || !InputReady) return;
            Back();
            currentState = ScreenState.Lobby;
            inputCooldownTimer = 0.2f;
            Changed();
        }

        private static bool IsSheet(ScreenState s) => s == ScreenState.Settings || s == ScreenState.Practice || s == ScreenState.Guides;

        // ── Desa ──────────────────────────────────────────────────────
        public void OpenDesa()
        {
            if (currentState != ScreenState.Lobby || !InputReady) return;
            Click();
            currentState = ScreenState.Desa;
            inputCooldownTimer = 0.2f;
            Changed();
        }

        public void CloseDesa()
        {
            if (currentState != ScreenState.Desa || !InputReady) return;
            Back();
            currentState = ScreenState.Lobby;
            inputCooldownTimer = 0.2f;
            Changed();
            Resources.UnloadUnusedAssets();
        }

        // ── Prolog ────────────────────────────────────────────────────
        public void FinishProlog(string rawName)
        {
            if (currentState != ScreenState.Prolog || !InputReady) return;
            Click();
            if (EnerGoProgress.FinishProlog(rawName))
            {
                currentState = ScreenState.Lobby;
                inputCooldownTimer = 0.25f;
                Resources.UnloadUnusedAssets();
            }
            Changed();
        }

        public void PrologTap() { BlockTaps(); if (SFXManager.Instance != null) SFXManager.Instance.PlayButtonClick(); }

        // ── Pengaturan ────────────────────────────────────────────────
        // Toggle rows: the whole row is the button; it clicks and holds input for 0.15 s before the effect.
        private bool ToggleRowTap()
        {
            if (currentState != ScreenState.Settings || !InputReady) return false;
            Click();
            inputCooldownTimer = 0.15f;
            return true;
        }

        // AR mode switch (on = ARCore scene, off = Sensor 360), remembered across sessions
        public void ToggleARMode()
        {
            if (!ToggleRowTap()) return;
            useARCore = !useARCore;
            PlayerPrefs.SetInt(PrefUseARCore, useARCore ? 1 : 0);
            PlayerPrefs.Save();
            Changed();
        }

        // Master volume: follows the finger while held; saved and previewed on release.
        public void PreviewVolume(float volume01)
        {
            if (currentState != ScreenState.Settings || !InputReady) return;
            AudioListener.volume = Mathf.Clamp01(volume01);
        }

        public void CommitVolume()
        {
            SFXManager.SetVolume(AudioListener.volume);
            Click();
        }

        public void ToggleVibration()
        {
            if (!ToggleRowTap()) return;
            Haptics.SetEnabled(!Haptics.Enabled);
            Haptics.Pulse(60); // feel it when switching on
            Changed();
        }

        public void OpenPracticeFromSettings()
        {
            if (currentState != ScreenState.Settings || !InputReady) return;
            Click();
            currentState = ScreenState.Practice;
            inputCooldownTimer = 0.2f;
            Changed();
        }

        // Reset progress: two taps within 3.5 s
        public void TapReset()
        {
            if (currentState != ScreenState.Settings || !InputReady) return;
            Click();
            if (!isConfirmingReset) { isConfirmingReset = true; resetConfirmTimer = ResetConfirmSeconds; }
            else if (EnerGoProgress.Reset()) isConfirmingReset = false;
            Changed();
        }

        // ── Latihan ───────────────────────────────────────────────────
        // Practice: pick any SDA (or Acak) and go straight into its mini-game, no GPS.
        public void SelectPractice(string id)
        {
            if (currentState != ScreenState.Practice || !InputReady) return;
            if (!ResourceIds.IsPlayable(id) && id != RandomResource) return;
            Click();
            selectedResource = id;
            PlayerPrefs.SetString(PrefResource, id);
            PlayerPrefs.Save();
            inputCooldownTimer = 0.15f;
            Changed();
        }

        public void StartPractice()
        {
            if (currentState != ScreenState.Practice || !InputReady) return;
            Click();
            ResourceIds.Selected = selectedResource == RandomResource
                ? AllResources[Random.Range(0, AllResources.Length)] : selectedResource;
            ExploreSession.PendingSpawnId = null;
            transitionScene = CaptureSceneName();
            currentState = ScreenState.TransitioningToAR;
            transitionTimer = 0f;
            Changed();
        }

        // ── Panduan ───────────────────────────────────────────────────
        // PANDUAN: the 5 SDA as tiles; tap one to read its guide. Also resets every "Jangan ingatkan lagi".
        public void OpenGuide(string id)
        {
            if (currentState != ScreenState.Guides || OpenGuideId != null || !InputReady) return;
            if (GuideBook.For(id).Length == 0) return;
            Click();
            OpenGuideId = id;
            inputCooldownTimer = 0.15f;
            Changed();
        }

        public void CloseGuide()
        {
            if (OpenGuideId == null) return;
            BlockTaps();
            OpenGuideId = null;
            inputCooldownTimer = 0.2f;
            Changed();
        }

        public void ResetGuides()
        {
            if (currentState != ScreenState.Guides || OpenGuideId != null || !InputReady) return;
            Click();
            GuideViewer.ResetAll();
            guidesResetAt = Time.time;
            Changed();
        }
    }
}
