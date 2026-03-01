using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UIElements;

namespace Unity.Samples.LetterSpell
{
    public class SettingsViewController : MonoBehaviour
    {
        [Header("uGUI References")]
        public UnityEngine.UI.Button uguiBackButton;

        [Header("UI Toolkit References")]
        public UIDocument uitkDocument;

        Button m_UitkBackButton;
        DropdownField m_UitkColorThemeDropdown;

        bool m_UseUIToolkit;

        void OnEnable()
        {
            Gameplay.instance?.PauseGame();

            m_UseUIToolkit = PlayerPrefs.GetInt(UISystemToggler.useUIToolkitPreference) == 1;

            SetupUI();

            LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
        }

        void OnDisable()
        {
            CleanupUI();

            LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;

            Gameplay.instance?.ResumeGame();
        }

        void SetupUI()
        {
            if (m_UseUIToolkit)
            {
                if (uitkDocument == null)
                {
                    Debug.LogError($"{nameof(uitkDocument)} is not assigned for {GetType().Name}.");
                    return;
                }

                var root = uitkDocument.rootVisualElement;

                root.dataSource = PlayerSettingsDataSource.Acquire();

                m_UitkBackButton = root.Q<Button>("back-button");
                m_UitkBackButton.clicked += SceneTransitionManager.UnloadSettingsScene;

                m_UitkColorThemeDropdown = root.Q<DropdownField>("color-theme-dropdown");
                UpdateColorThemeChoices();
            }
            else
            {
                if (uguiBackButton == null)
                {
                    Debug.LogError($"{nameof(uguiBackButton)} is not assigned for {GetType().Name}.");
                }

                uguiBackButton?.onClick.AddListener(SceneTransitionManager.UnloadSettingsScene);
            }
        }

        void CleanupUI()
        {
            if (m_UseUIToolkit)
            {
                PlayerSettingsDataSource.Release();

                m_UitkBackButton.clicked -= SceneTransitionManager.UnloadSettingsScene;
            }
            else
            {
                uguiBackButton?.onClick.RemoveListener(SceneTransitionManager.UnloadSettingsScene);
            }
        }

        void OnLocaleChanged(Locale locale)
        {
            UpdateColorThemeChoices();
        }

        void UpdateColorThemeChoices()
        {
            var index = m_UitkColorThemeDropdown.index;

            m_UitkColorThemeDropdown.choices = new List<string>
            {
                PlayerSettings.colorThemeOriginal,
                PlayerSettings.colorThemeHighContrast
            };

            m_UitkColorThemeDropdown.index = index;
        }
    }
}
