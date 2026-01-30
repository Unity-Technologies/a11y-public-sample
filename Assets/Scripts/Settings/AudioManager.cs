using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Unity.Samples.LetterSpell
{
    public class AudioManager : MonoBehaviour
    {
        static AudioManager s_Instance;

        static AudioClip s_BackgroundMusic;
        static AudioClip s_MoveTileEffect;
        static AudioClip s_SuccessEffect;
        static AudioClip s_FailureEffect;
        static AudioClip s_WelcomeEffect;

        static AudioClip backgroundMusic => s_BackgroundMusic ??= Resources.Load<AudioClip>("Audio/background-music");
        static AudioClip moveTileEffect => s_MoveTileEffect ??= Resources.Load<AudioClip>("Audio/tile-effect");
        public static AudioClip successEffect => s_SuccessEffect ??= Resources.Load<AudioClip>("Audio/success-effect");
        public static AudioClip failureEffect => s_FailureEffect ??= Resources.Load<AudioClip>("Audio/failure-effect");
        public static AudioClip welcomeEffect => s_WelcomeEffect ??= Resources.Load<AudioClip>("Audio/welcome");

        AudioSource m_MusicSource;
        AudioSource m_MoveTileSource;
        AudioSource m_ResultSource;

        public static event Action<AudioSource> audioPlayingStatusChanged;

        void Awake()
        {
            if (s_Instance != null && s_Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            s_Instance = this;

            // Keep the Audio Manager alive across scenes.
            DontDestroyOnLoad(gameObject);

            m_MusicSource = gameObject.AddComponent<AudioSource>();
            m_MusicSource.volume = PlayerPrefs.GetFloat(PlayerSettings.musicPreference, 0.5f);

            m_MoveTileSource = gameObject.AddComponent<AudioSource>();
            m_MoveTileSource.clip = moveTileEffect;

            m_ResultSource = gameObject.AddComponent<AudioSource>();
        }

        void OnDestroy()
        {
            if (s_Instance == this)
            {
                s_Instance = null;
            }
        }

        void Start()
        {
            if (SceneManager.GetActiveScene().name == "Splash Scene")
            {
                PlayWelcome();
                Invoke(nameof(PlayBackgroundMusic), 3f);
            }
            else
            {
                PlayBackgroundMusic();
            }
        }

        public static void SetMusicVolume(float value)
        {
            if (Mathf.Approximately(s_Instance.m_MusicSource.volume, value))
            {
                return;
            }

            s_Instance.m_MusicSource.volume = value;

            if (s_Instance.m_MusicSource.isPlaying && value == 0)
            {
                s_Instance.m_MusicSource.Stop();
            }
            else if (!s_Instance.m_MusicSource.isPlaying && value > 0)
            {
                s_Instance.PlayBackgroundMusic();
            }
        }

        void PlayWelcome()
        {
            if (m_MusicSource.volume == 0)
            {
                return;
            }

            m_MusicSource.clip = welcomeEffect;
            m_MusicSource.Play();

            audioPlayingStatusChanged?.Invoke(m_MusicSource);
        }

        void PlayBackgroundMusic()
        {
            if (m_MusicSource.volume == 0)
            {
                return;
            }

            m_MusicSource.clip = backgroundMusic;
            m_MusicSource.loop = true;
            m_MusicSource.Play();
        }

        public static void PlayMoveTile()
        {
            s_Instance.m_MoveTileSource.volume = PlayerPrefs.GetFloat(PlayerSettings.soundEffectsPreference, 0.5f);
            s_Instance.m_MoveTileSource.Play();
        }

        public static void PlayResult(bool success)
        {
            s_Instance.m_ResultSource.clip = success ? successEffect : failureEffect;
            s_Instance.m_ResultSource.volume = PlayerPrefs.GetFloat(PlayerSettings.soundEffectsPreference, 0.5f);
            s_Instance.m_ResultSource.Play();

            audioPlayingStatusChanged?.Invoke(s_Instance.m_ResultSource);
        }
    }
}
