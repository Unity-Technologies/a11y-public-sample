using System.Collections.Generic;
using Unity.Samples.LetterSpell;
using UnityEngine;
using UnityEngine.Accessibility;
using UnityEngine.UIElements;

namespace Unity.Samples.ClosedCaptions
{
    /// <summary>
    /// This singleton manages audio subtitles in Unity, linking audio clips to subtitles and displaying them when audio
    /// plays.
    /// </summary>
    public class ClosedCaptionsManager : MonoBehaviour
    {
        public Subtitle[] subtitles;

        SubtitleDisplaySettings m_DisplaySettings;
        SubtitlePlayer m_SubtitlePlayer;
        SubtitleViewer m_SubtitleViewer;
        Dictionary<string, Subtitle> m_SubtitleMap = new();

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
            AudioManager.audioPlayingStatusChanged += OnAudioPlayingStatusChanged;

            // Create a map of audio clip names to subtitles.
            m_SubtitleMap.Add(AudioManager.instance.welcomeEffect.name, subtitles[0]);
            m_SubtitleMap.Add(AudioManager.instance.successEffect.name, subtitles[1]);
            m_SubtitleMap.Add(AudioManager.instance.failureEffect.name, subtitles[2]);
        }

        void OnDestroy()
        {
            s_Instance = null;

            AudioManager.audioPlayingStatusChanged -= OnAudioPlayingStatusChanged;
        }

        // Displays the corresponding subtitle when an audio clip plays.
        void OnAudioPlayingStatusChanged(AudioSource audioSource)
        {
            if (!AccessibilitySettings.isClosedCaptioningEnabled)
            {
                return;
            }

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
