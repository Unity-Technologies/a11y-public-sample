using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Accessibility;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Unity.Samples.LetterSpell
{
    public class FontManager : MonoBehaviour
    {
        const float k_DefaultFontSize = 64f;
        const float k_DefaultHeaderFontSize = 75f;
        const float k_DefaultLetterCardFontSize = 132f;

        static FontManager s_Instance;

        static TMP_FontAsset s_UguiDefaultFont;
        static TMP_FontAsset s_UguiBoldFont;
        static TMP_FontAsset uguiDefaultFont => s_UguiDefaultFont ??= Resources.Load<TMP_FontAsset>("Fonts/Inter-Regular");
        static TMP_FontAsset uguiBoldFont => s_UguiBoldFont ??= Resources.Load<TMP_FontAsset>("Fonts/Inter-Bold");

        List<TMP_Text> m_UguiTexts = new();
        List<TMP_Text> m_UguiHeaderTexts = new();
        List<TMP_Text> m_UguiLetterCardTexts = new();

        List<UIDocument> m_UitkDocuments = new();

        bool m_UseUIToolkit;

        void Awake()
        {
            if (s_Instance != null && s_Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            s_Instance = this;

            // Keep the Font Manager alive across scenes.
            DontDestroyOnLoad(gameObject);
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
            m_UseUIToolkit = PlayerPrefs.GetInt(UISystemToggler.useUIToolkitPreference) == 1;

            SetupUI();

            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;

            // Note: AccessibilitySettings.boldTextStatusChanged is only available on iOS.
            // On Android, the app restarts when AccessibilitySettings.isBoldTextEnabled changes.
            AccessibilitySettings.boldTextStatusChanged += UpdateFontStyle;
            AccessibilitySettings.fontScaleChanged += UpdateFontScale;
        }

        void OnDisable()
        {
            CleanupUI();

            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneUnloaded -= OnSceneUnloaded;

            AccessibilitySettings.boldTextStatusChanged -= UpdateFontStyle;
            AccessibilitySettings.fontScaleChanged -= UpdateFontScale;
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            SetupUI();
        }

        void OnSceneUnloaded(Scene scene)
        {
            CleanupUI();
        }

        void SetupUI()
        {
            if (m_UseUIToolkit)
            {
                m_UitkDocuments.AddRange(FindObjectsByType<UIDocument>(FindObjectsSortMode.None));
            }
            else
            {
                var texts = FindObjectsByType<TMP_Text>(FindObjectsSortMode.None);

                foreach (var text in texts)
                {
                    switch (text.fontSize)
                    {
                        case k_DefaultLetterCardFontSize:
                        {
                            m_UguiLetterCardTexts.Add(text);
                            break;
                        }
                        case k_DefaultHeaderFontSize:
                        {
                            m_UguiHeaderTexts.Add(text);
                            break;
                        }
                        default:
                        {
                            m_UguiTexts.Add(text);
                            break;
                        }
                    }
                }
            }

            // Note: On Android, AccessibilitySettings.isBoldTextEnabled requires at least Android 12 (API level 31).
            UpdateFontStyle(AccessibilitySettings.isBoldTextEnabled);
            UpdateFontScale(AccessibilitySettings.fontScale);
        }

        void CleanupUI()
        {;
            m_UguiTexts.Clear();
            m_UguiHeaderTexts.Clear();
            m_UguiLetterCardTexts.Clear();
            m_UitkDocuments.Clear();
        }

        void UpdateFontStyle(bool bold)
        {
            if (m_UseUIToolkit)
            {
                var fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;

                foreach (var document in m_UitkDocuments)
                {
                    if (document.rootVisualElement != null)
                    {
                        document.rootVisualElement.style.unityFontStyleAndWeight = fontStyle;
                    }
                }
            }
            else
            {
                var font = bold ? uguiBoldFont : uguiDefaultFont;

                foreach (var text in m_UguiLetterCardTexts)
                {
                    text.font = font;
                }

                foreach (var text in m_UguiHeaderTexts)
                {
                    text.font = font;
                }

                foreach (var text in m_UguiTexts)
                {
                    text.font = font;
                }
            }
        }

        void UpdateFontScale(float fontScale)
        {
            if (m_UseUIToolkit)
            {
                foreach (var document in m_UitkDocuments)
                {
                    if (document.rootVisualElement != null)
                    {
                        document.rootVisualElement.style.fontSize = k_DefaultFontSize * fontScale;
                    }
                }
            }
            else
            {
                foreach (var text in m_UguiLetterCardTexts)
                {
                    text.fontSize = k_DefaultLetterCardFontSize * fontScale;
                }

                foreach (var text in m_UguiHeaderTexts)
                {
                    text.fontSize = k_DefaultHeaderFontSize * fontScale;
                }

                foreach (var text in m_UguiTexts)
                {
                    text.fontSize = k_DefaultFontSize * fontScale;
                }
            }
        }
    }
}
