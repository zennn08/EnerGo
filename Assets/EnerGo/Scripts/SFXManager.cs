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
        public AudioClip clipMatahari;
        public AudioClip clipBaraPukul;
        public AudioClip clipBaraHancur;
        public AudioClip clipSungai;
        public AudioClip clipEkstrakMatahari;

        [Header("Music")]
        public AudioClip clipLobbyMusic;

        private AudioSource sfxSource;
        private AudioSource loopSource;  // for looping SFX like aliran air
        private AudioSource musicSource; // for background music
        private AudioSource ambienceSource; // scene ambience that can run under a looping SFX (river)
        private AudioSource ekstrakSource;   // sun extraction SFX — seekable, syncs with sunProgress

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

            ambienceSource = gameObject.AddComponent<AudioSource>();
            ambienceSource.playOnAwake = false;
            ambienceSource.loop = true;

            ekstrakSource = gameObject.AddComponent<AudioSource>();
            ekstrakSource.playOnAwake = false;
            ekstrakSource.loop = false;

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
            clipMatahari = Resources.Load<AudioClip>("Audio/SFX/sfx_matahari");
            clipBaraPukul = Resources.Load<AudioClip>("Audio/SFX/sfx_bara_pukul");
            clipBaraHancur = Resources.Load<AudioClip>("Audio/SFX/sfx_bara_hancur");
            clipSungai = Resources.Load<AudioClip>("Audio/SFX/sfx_sungai");
            clipEkstrakMatahari = Resources.Load<AudioClip>("Audio/SFX/sfx_ekstrak_matahari");
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

        public void PlayBaraPukul() => PlayOneShot(clipBaraPukul, 1f);

        public void PlayBaraHancur() => PlayOneShot(clipBaraHancur, 1f);

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

        /// <summary>Glide the water loop's volume (e.g. louder while the gold pan swirls faster).</summary>
        public void SetAliranAirVolume(float volume)
        {
            if (loopSource != null && loopSource.clip == clipAliranAir)
                loopSource.volume = Mathf.MoveTowards(loopSource.volume, volume, Time.deltaTime * 1.5f);
        }

        /// <summary>Start the river ambience (gold capture scene). Safe to call repeatedly.</summary>
        public void PlaySungai(float volume = 0.35f)
        {
            if (clipSungai != null && ambienceSource != null && ambienceSource.clip != clipSungai)
            {
                ambienceSource.clip = clipSungai;
                ambienceSource.volume = volume;
                ambienceSource.Play();
            }
        }

        public void StopSungai()
        {
            if (ambienceSource != null && ambienceSource.clip == clipSungai)
            {
                ambienceSource.Stop();
                ambienceSource.clip = null;
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

        /// <summary>Start looping the drifting-sun sound (sun capture mini-game). Safe to call every frame.</summary>
        public void PlayMatahari()
        {
            if (clipMatahari != null && loopSource != null && loopSource.clip != clipMatahari)
            {
                loopSource.clip = clipMatahari;
                loopSource.volume = 0f; // faded in by SetMatahariMix
                loopSource.panStereo = 0f;
                loopSource.Play();
            }
        }

        /// <summary>Glide the sun loop toward a volume and stereo pan (-1 left .. 1 right).</summary>
        public void SetMatahariMix(float volume, float pan)
        {
            if (loopSource == null || loopSource.clip != clipMatahari) return;
            loopSource.volume = Mathf.MoveTowards(loopSource.volume, volume, Time.deltaTime * 1.5f);
            loopSource.panStereo = Mathf.MoveTowards(loopSource.panStereo, pan, Time.deltaTime * 3f);
        }

        /// <summary>Stop the sun loop.</summary>
        public void StopMatahari()
        {
            if (loopSource != null && loopSource.clip == clipMatahari)
            {
                loopSource.Stop();
                loopSource.clip = null;
                loopSource.panStereo = 0f;
            }
        }

        // ────────────────────────────────────────────
        // Sun extraction sound — plays in sync with sunProgress
        // ────────────────────────────────────────────

        /// <summary>
        /// Sync the extraction sound's playback position to the current sun capture progress.
        /// The clip's timeline maps 1:1 to progress (0 % → start, 100 % → end).
        /// While the sun is inside the reticle the clip plays forward normally;
        /// when it escapes, playback pauses and seeks back as progress drains.
        /// Call every frame from UpdateSun.
        /// </summary>
        public void SyncEkstrakMatahari(float progress, bool sunInside)
        {
            if (clipEkstrakMatahari == null || ekstrakSource == null) return;

            // Assign clip if not yet set.
            if (ekstrakSource.clip != clipEkstrakMatahari)
            {
                ekstrakSource.clip = clipEkstrakMatahari;
                ekstrakSource.volume = 1f;
            }

            float targetTime = Mathf.Clamp(progress, 0f, 0.999f) * clipEkstrakMatahari.length;

            if (progress <= 0f)
            {
                // Nothing captured yet: keep silent.
                if (ekstrakSource.isPlaying) ekstrakSource.Pause();
                ekstrakSource.time = 0f;
                return;
            }

            if (sunInside)
            {
                // Sun inside reticle: let the clip play forward.
                // Only seek if the playback head drifted noticeably from expected position
                // (happens after a drain or first entry).
                float drift = Mathf.Abs(ekstrakSource.time - targetTime);
                if (drift > 0.15f)
                    ekstrakSource.time = targetTime;

                if (!ekstrakSource.isPlaying)
                    ekstrakSource.Play();
            }
            else
            {
                // Sun outside reticle: pause and seek to the (lower) progress position.
                if (ekstrakSource.isPlaying) ekstrakSource.Pause();
                ekstrakSource.time = targetTime;
            }
        }

        /// <summary>Stop and reset the extraction sound (called when the mini-game ends).</summary>
        public void StopEkstrakMatahari()
        {
            if (ekstrakSource != null && ekstrakSource.clip == clipEkstrakMatahari)
            {
                ekstrakSource.Stop();
                ekstrakSource.clip = null;
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
