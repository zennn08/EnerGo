using UnityEngine;

namespace EnerGo
{
    /// <summary>
    /// Singleton manager for all game sound effects and music.
    /// Loads AudioClips from Resources/Audio/SFX/ and provides
    /// simple Play methods for each game action.
    /// Persists across scene loads via DontDestroyOnLoad.
    /// </summary>
    public class SFXManager : MonoBehaviour
    {
        public static SFXManager Instance { get; private set; }

        [Header("Audio Clips — loaded from Resources at runtime")]
        public AudioClip clipSplash;
        public AudioClip clipButtonClick;
        public AudioClip clipButtonBack;
        public AudioClip clipDulangEmas;
        public AudioClip clipAliranAir;
        public AudioClip clipBatuJatuh;
        public AudioClip clipPickaxe;
        public AudioClip clipQuizBenar;
        public AudioClip clipQuizSalah;

        [Header("Music")]
        public AudioClip clipLobbyMusic;

        private AudioSource sfxSource;
        private AudioSource loopSource;  // for looping SFX like aliran air
        private AudioSource musicSource; // for background music

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Create AudioSources
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;

            loopSource = gameObject.AddComponent<AudioSource>();
            loopSource.playOnAwake = false;
            loopSource.loop = true;

            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.playOnAwake = false;
            musicSource.loop = true;
            musicSource.volume = 0.6f;

            LoadClips();
        }

        private void LoadClips()
        {
            clipSplash = Resources.Load<AudioClip>("Audio/SFX/sfx_splash");
            clipButtonClick = Resources.Load<AudioClip>("Audio/SFX/sfx_button_click");
            clipButtonBack = Resources.Load<AudioClip>("Audio/SFX/sfx_button_back");
            clipDulangEmas = Resources.Load<AudioClip>("Audio/SFX/sfx_dulang_emas");
            clipAliranAir = Resources.Load<AudioClip>("Audio/SFX/sfx_aliran_air");
            clipBatuJatuh = Resources.Load<AudioClip>("Audio/SFX/sfx_batu_jatuh");
            clipPickaxe = Resources.Load<AudioClip>("Audio/SFX/sfx_pickaxe");
            clipQuizBenar = Resources.Load<AudioClip>("Audio/SFX/sfx_quiz_benar");
            clipQuizSalah = Resources.Load<AudioClip>("Audio/SFX/sfx_quiz_salah");
            clipLobbyMusic = Resources.Load<AudioClip>("Audio/SFX/music_lobby");
        }

        /// <summary>Play a one-shot clip at the given volume.</summary>
        private void PlayOneShot(AudioClip clip, float volume = 1f)
        {
            if (clip != null && sfxSource != null)
            {
                sfxSource.PlayOneShot(clip, volume);
            }
        }

        // ────────────────────────────────────────────
        // Public API — SFX (all volumes cranked up)
        // ────────────────────────────────────────────

        public void PlaySplash() => PlayOneShot(clipSplash, 1f);

        public void PlayButtonClick() => PlayOneShot(clipButtonClick, 1f);

        public void PlayButtonBack() => PlayOneShot(clipButtonBack, 1f);

        public void PlayDulangEmas() => PlayOneShot(clipDulangEmas, 1f);

        public void PlayBatuJatuh() => PlayOneShot(clipBatuJatuh, 1f);

        public void PlayPickaxe() => PlayOneShot(clipPickaxe, 1f);

        public void PlayQuizBenar() => PlayOneShot(clipQuizBenar, 1f);

        public void PlayQuizSalah() => PlayOneShot(clipQuizSalah, 1f);

        /// <summary>Start looping the water flow ambient sound.</summary>
        public void PlayAliranAir()
        {
            if (clipAliranAir != null && loopSource != null && loopSource.clip != clipAliranAir)
            {
                loopSource.clip = clipAliranAir;
                loopSource.volume = 0.7f;
                loopSource.Play();
            }
        }

        /// <summary>Stop the looping ambient sound.</summary>
        public void StopAliranAir()
        {
            if (loopSource != null && loopSource.isPlaying && loopSource.clip == clipAliranAir)
            {
                loopSource.Stop();
                loopSource.clip = null;
            }
        }

        // ────────────────────────────────────────────
        // Public API — Music
        // ────────────────────────────────────────────

        /// <summary>Start playing lobby background music (looping).</summary>
        public void PlayLobbyMusic()
        {
            if (clipLobbyMusic != null && musicSource != null && !musicSource.isPlaying)
            {
                musicSource.clip = clipLobbyMusic;
                musicSource.volume = 0.6f;
                musicSource.Play();
            }
        }

        /// <summary>Stop the lobby background music.</summary>
        public void StopLobbyMusic()
        {
            if (musicSource != null && musicSource.isPlaying)
            {
                musicSource.Stop();
                musicSource.clip = null;
            }
        }

        // ────────────────────────────────────────────
        // Bootstrap — auto-create if not in scene
        // ────────────────────────────────────────────

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoCreate()
        {
            if (Instance == null)
            {
                var go = new GameObject("[SFXManager]");
                go.AddComponent<SFXManager>();
            }
        }
    }
}
