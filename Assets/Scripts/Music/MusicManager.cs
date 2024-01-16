using System;
using UnityEngine;

namespace Unity.Samples.LetterSpell
{
    public class MusicManager : MonoBehaviour
    {
        public static MusicManager instance;

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
            m_MusicSource.clip = welcomeEffect;
            m_MusicSource.Play();
            audioPlayingStatusChanged?.Invoke(m_MusicSource);
            Invoke("PlayBackgroundMusic", 3f);
        }
        
        //play the background music after some delay
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
            if (success)
            {
                m_ResultSource.clip = successEffect;
                m_ResultSource.volume = PlayerPrefs.GetFloat(PlayerSettings.soundEffectsPreference, 0.5f);
                m_ResultSource.Play();
                audioPlayingStatusChanged?.Invoke(m_ResultSource);
            }
            else
            {
                m_ResultSource.clip = failureEffect;
                m_ResultSource.volume = PlayerPrefs.GetFloat(PlayerSettings.soundEffectsPreference, 0.5f);
                m_ResultSource.Play();
                audioPlayingStatusChanged?.Invoke(m_ResultSource);
            }
        }

    }
}
