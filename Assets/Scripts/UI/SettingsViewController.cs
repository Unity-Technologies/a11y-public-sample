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
        TextField m_UitkSearchTextField;
        ToggleButtonGroup m_UitkDifficultyToggleGroup;
        RadioButtonGroup m_UitkWordsRadioGroup;
        DropdownField m_UitkColorThemeDropdown;
        DropdownField m_UitkLanguageDropdown;
        TextField m_UitkUsernameTextField;

        LanguageDirection m_LanguageDirection = LanguageDirection.Inherit;

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

                m_UitkSearchTextField = root.Q<TextField>("search-text-field");
                m_UitkDifficultyToggleGroup = root.Q<ToggleButtonGroup>("difficulty-toggle-group");
                m_UitkWordsRadioGroup = root.Q<RadioButtonGroup>("words-radio-group");
                m_UitkUsernameTextField = root.Q<TextField>("username-text-field");
                m_UitkColorThemeDropdown = root.Q<DropdownField>("color-theme-dropdown");
                m_UitkLanguageDropdown = root.Q<DropdownField>("language-dropdown");

                m_UitkColorThemeDropdown?.RegisterCallback<PointerDownEvent>(OnDropdownOpened, TrickleDown.TrickleDown);
                m_UitkLanguageDropdown?.RegisterCallback<PointerDownEvent>(OnDropdownOpened, TrickleDown.TrickleDown);

                UpdateLayoutDirection();
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

                m_UitkColorThemeDropdown?.UnregisterCallback<PointerDownEvent>(OnDropdownOpened, TrickleDown.TrickleDown);
                m_UitkLanguageDropdown?.UnregisterCallback<PointerDownEvent>(OnDropdownOpened, TrickleDown.TrickleDown);
            }
            else
            {
                uguiBackButton?.onClick.RemoveListener(SceneTransitionManager.UnloadSettingsScene);
            }
        }

        void OnLocaleChanged(Locale locale)
        {
            UpdateLayoutDirection();
            UpdateColorThemeChoices();
        }

        void OnDropdownOpened(PointerDownEvent evt)
        {
            if (evt.currentTarget is not DropdownField dropdown)
            {
                return;
            }

            // The popup is added to a separate panel's visual tree during this event.
            // Poll every frame until the popup container is attached, then apply the direction.
            dropdown.schedule.Execute(() =>
            {
                var popup = dropdown.panel.visualTree.Q(className: "unity-base-dropdown");

                if (popup == null)
                {
                    return;
                }

                foreach (var item in popup.Query(className: "unity-base-dropdown__item").ToList())
                {
                    item.style.flexDirection =
                        m_LanguageDirection == LanguageDirection.LTR ? FlexDirection.Row : FlexDirection.RowReverse;

                    item.Q(className: "unity-base-dropdown__item-content").style.flexDirection =
                        m_LanguageDirection == LanguageDirection.LTR ? FlexDirection.Row : FlexDirection.RowReverse;
                }
            }).Until(() => dropdown.panel.visualTree.Q(className: "unity-base-dropdown") != null);
        }

        void UpdateLayoutDirection()
        {
            var dataSource = uitkDocument.rootVisualElement.dataSource as PlayerSettingsDataSource;

            if (dataSource!.languageDirection == m_LanguageDirection)
            {
                return;
            }

            m_LanguageDirection = dataSource.languageDirection;

            m_UitkSearchTextField.Q("unity-text-input").style.unityTextAlign =
                m_LanguageDirection == LanguageDirection.LTR ? TextAnchor.MiddleLeft : TextAnchor.MiddleRight;

            m_UitkDifficultyToggleGroup.contentContainer.style.flexDirection =
                m_LanguageDirection == LanguageDirection.LTR ? FlexDirection.Row : FlexDirection.RowReverse;

            m_UitkWordsRadioGroup.contentContainer.style.flexDirection =
                m_LanguageDirection == LanguageDirection.LTR ? FlexDirection.Row : FlexDirection.RowReverse;
            m_UitkWordsRadioGroup.contentContainer.style.alignSelf =
                m_LanguageDirection == LanguageDirection.LTR ? Align.FlexStart : Align.FlexEnd;

            m_UitkColorThemeDropdown.Q(className: "unity-base-field__input").style.flexDirection =
                m_LanguageDirection == LanguageDirection.LTR ? FlexDirection.Row : FlexDirection.RowReverse;
            m_UitkColorThemeDropdown.Q(className: "unity-text-element").style.unityTextAlign =
                m_LanguageDirection == LanguageDirection.LTR ? TextAnchor.MiddleLeft : TextAnchor.MiddleRight;

            m_UitkLanguageDropdown.Q(className: "unity-base-field__input").style.flexDirection =
                m_LanguageDirection == LanguageDirection.LTR ? FlexDirection.Row : FlexDirection.RowReverse;
            m_UitkLanguageDropdown.Q(className: "unity-text-element").style.unityTextAlign =
                        m_LanguageDirection == LanguageDirection.LTR ? TextAnchor.MiddleLeft : TextAnchor.MiddleRight;

            m_UitkUsernameTextField.Q("unity-text-input").style.unityTextAlign =
                m_LanguageDirection == LanguageDirection.LTR ? TextAnchor.MiddleLeft : TextAnchor.MiddleRight;
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
