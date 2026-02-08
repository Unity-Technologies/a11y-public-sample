using System;
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
        static ClosedCaptionsManager s_Instance;

        SubtitlePlayer m_SubtitlePlayer;
        SubtitleViewer m_SubtitleViewer;

        Dictionary<string, Subtitle> m_SubtitleMap = new();

        void Awake()
        {
            if (s_Instance != null && s_Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            s_Instance = this;

            // Keep the Closed Captions Manager alive across scenes.
            DontDestroyOnLoad(gameObject);

            var document = gameObject.AddComponent<UIDocument>();
            document.panelSettings = Resources.Load<PanelSettings>("UI Toolkit/PanelSettings");
            document.sortingOrder = short.MaxValue; // Ensure that subtitles are displayed on top of all other UI.

            m_SubtitlePlayer = gameObject.AddComponent<SubtitlePlayer>();

            m_SubtitleViewer = gameObject.AddComponent<SubtitleViewer>();
            m_SubtitleViewer.player = m_SubtitlePlayer;
            m_SubtitleViewer.surface = document;
            m_SubtitleViewer.displaySettings = Resources.Load<SubtitleDisplaySettings>("Subtitles/DisplaySettings");

            // Create a map of audio clip names to subtitles.
            m_SubtitleMap.Add(AudioManager.welcomeEffect.name, Resources.Load<Subtitle>("Subtitles/WelcomeEffect"));
            m_SubtitleMap.Add(AudioManager.successEffect.name, Resources.Load<Subtitle>("Subtitles/SuccessEffect"));
            m_SubtitleMap.Add(AudioManager.failureEffect.name, Resources.Load<Subtitle>("Subtitles/FailEffect"));
        }

        void OnDestroy()
        {
            if (s_Instance == this)
            {
                s_Instance = null;
            }
        }

        void OnEnable()
        {
            AudioManager.audioPlayingStatusChanged += OnAudioPlayingStatusChanged;
        }

        void OnDisable()
        {
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
                if (m_SubtitleMap.TryGetValue(audioSource.clip.name, out var subtitle))
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
