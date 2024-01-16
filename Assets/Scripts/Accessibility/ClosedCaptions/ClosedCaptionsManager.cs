using System.Collections.Generic;
using Unity.Samples.LetterSpell;
using UnityEngine;
using UnityEngine.Accessibility;
using UnityEngine.UIElements;

namespace Unity.Samples.Accessibility
{
    /// <summary>
    /// This singleton manages audio subtitles in Unity, linking audio clips to
    /// subtitles and displaying them when audio plays.
    /// </summary>
    public class ClosedCaptionsManager : MonoBehaviour
    {
        public Subtitle[] subtitles;
        SubtitleDisplaySettings m_DisplaySettings;
        SubtitlePlayer m_SubtitlePlayer;
        SubtitleViewer m_SubtitleViewer;
        Dictionary<string, Subtitle> m_SubtitleMap = new Dictionary<string, Subtitle>();

        static ClosedCaptionsManager s_Instance;

        // Initializes the singleton instance and creates the necessary components to display subtitles.
        void Awake()
        {
            if (s_Instance != null && s_Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            s_Instance = this;
            DontDestroyOnLoad(gameObject);

            m_SubtitlePlayer = gameObject.AddComponent<SubtitlePlayer>();
            m_SubtitleViewer = gameObject.AddComponent<SubtitleViewer>();

            m_SubtitleViewer.player = m_SubtitlePlayer;
            m_SubtitleViewer.surface = GetComponent<UIDocument>();
            m_SubtitleViewer.displaySettings = m_DisplaySettings;
        }
        
        void Start()
        {
            MusicManager.audioPlayingStatusChanged += MusicManagerOnAudioPlayingStatusChanged;

            // Create a map of audio clip names to subtitles.
            m_SubtitleMap.Add(MusicManager.instance.welcomeEffect.name, subtitles[0]);
            m_SubtitleMap.Add(MusicManager.instance.successEffect.name, subtitles[1]);
            m_SubtitleMap.Add(MusicManager.instance.failureEffect.name, subtitles[2]);
        }

        void OnDestroy()
        {
            s_Instance = null;
            MusicManager.audioPlayingStatusChanged -= MusicManagerOnAudioPlayingStatusChanged;
        }

        // This method displays the corresponding subtitle when an audio clip plays.
        void MusicManagerOnAudioPlayingStatusChanged(AudioSource audioSource)
        {
            if (!AccessibilitySettings.isClosedCaptioningEnabled) return;

            if (audioSource.isPlaying)
            {
                if (m_SubtitleMap.TryGetValue(audioSource.clip.name, out Subtitle subtitle))
                {
                    m_SubtitlePlayer.subtitle = subtitle;
                    m_SubtitlePlayer.Play();
                }
            }
            else
            {
                m_SubtitlePlayer.Stop();
            }
        }
    }
}