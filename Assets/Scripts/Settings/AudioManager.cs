using System;
using UnityEngine;

namespace Unity.Samples.LetterSpell
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager instance;

        public AudioClip backgroundMusic;
        public AudioClip moveTileEffect;
        public AudioClip successEffect;
        public AudioClip failureEffect;
        public AudioClip welcomeEffect;

        AudioSource m_MusicSource;
        AudioSource m_MoveTileSource;
        AudioSource m_ResultSource;

        public static event Action<AudioSource> audioPlayingStatusChanged;

        void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);

            m_MusicSource = gameObject.AddComponent<AudioSource>();
            SetMusicVolume(PlayerPrefs.GetFloat(PlayerSettings.musicPreference, 0.5f));

            m_MoveTileSource = gameObject.AddComponent<AudioSource>();
            m_MoveTileSource.clip = moveTileEffect;

            m_ResultSource = gameObject.AddComponent<AudioSource>();
        }

        void Start()
        {
            Invoke(nameof(PlayWelcomeMusic), 1f);
        }

        /// <summary>
        /// Play the welcome music after some delay.
        /// </summary>
        void PlayWelcomeMusic()
        {
            m_MusicSource.clip = welcomeEffect;
            m_MusicSource.Play();

            audioPlayingStatusChanged?.Invoke(m_MusicSource);
            Invoke(nameof(PlayBackgroundMusic), 3f);
        }

        /// <summary>
        /// Play the background music after some delay.
        /// </summary>
        public void PlayBackgroundMusic()
        {
            m_MusicSource.clip = backgroundMusic;
            m_MusicSource.loop = true;

            m_MusicSource.Play();

            audioPlayingStatusChanged?.Invoke(m_MusicSource);
        }

        public void SetMusicVolume(float value)
        {
            m_MusicSource.volume = value;
        }

        public void PlayMoveTile()
        {
            m_MoveTileSource.volume = PlayerPrefs.GetFloat(PlayerSettings.soundEffectsPreference, 0.5f);
            m_MoveTileSource.Play();
        }

        public void PlayResult(bool success)
        {
            m_ResultSource.clip = success ? successEffect : failureEffect;
            m_ResultSource.volume = PlayerPrefs.GetFloat(PlayerSettings.soundEffectsPreference, 0.5f);
            m_ResultSource.Play();

            audioPlayingStatusChanged?.Invoke(m_ResultSource);
        }
    }
}
