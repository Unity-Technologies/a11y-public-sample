using System;
using Unity.Samples.Accessibility;
using TMPro;
using UnityEngine;
using UnityEngine.Accessibility;
using UnityEngine.UI;

namespace Unity.Samples.LetterSpell
{
    public class PlayerSettings : MonoBehaviour
    {
        public Button backButton;

        public TMP_InputField usernameInputField;
        public ToggleGroup difficultyToggleGroup;
        public Toggle easyDifficultyToggle;
        public Toggle hardDifficultyToggle;
        public ToggleGroup wordsToggleGroup;
        public Toggle threeWordsToggle;
        public Toggle sixWordsToggle;
        public Toggle clueToggle;
        public Slider soundEffectsSlider;
        public Slider musicSlider;
        public TMP_Dropdown colorThemeDropdown;
        public Slider displaySizeSlider;
        
        // Read-only settings
        public TMP_Text boldTextValue;
        public AccessibleElement boldTextAccessibleElement;
        public TMP_Text closedCaptionValue;
        public AccessibleElement closedCaptionAccessibleElement;
        public TMP_Text fontScaleValue;
        public AccessibleElement fontScaleAccessibleElement;

        public const string usernamePreference = "Username";
        public const string difficultyPreference = "GameDifficulty";
        public const string wordsPreference = "GameWords";
        public const string cluePreference = "ShowClues";
        public const string soundEffectsPreference = "SoundEffectsVolume";
        public const string musicPreference = "MusicVolume";
        const string k_ColorThemePreference = "ColorTheme";
        const string k_DisplaySizePreference = "DisplaySize";
        const string k_SettingOn = "On";
        const string k_SettingOff = "Off";

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                OnDismissed();
            }
        }
        
        void OnEnable()
        {
            if (Gameplay.instance != null)
            {
                Gameplay.instance.PauseGame();
            }

            backButton.GetComponent<AccessibleButton>().dismissed += OnDismissed;
            
            // Load and apply the saved player preferences.
            LoadInputFieldState(usernameInputField, usernamePreference);
            LoadToggleGroupState(difficultyToggleGroup, difficultyPreference);
            LoadToggleGroupState(wordsToggleGroup, wordsPreference);
            LoadToggleState(clueToggle, cluePreference, 1);
            LoadSliderState(soundEffectsSlider, soundEffectsPreference, 0.5f);
            LoadSliderState(musicSlider, musicPreference, 0.5f);
            LoadDropdownState(colorThemeDropdown, k_ColorThemePreference);
            LoadSliderState(displaySizeSlider, k_DisplaySizePreference, 1f);

            usernameInputField.onValueChanged.AddListener(OnUsernameValueChanged);
            easyDifficultyToggle.onValueChanged.AddListener(OnDifficultyValueChanged);
            hardDifficultyToggle.onValueChanged.AddListener(OnDifficultyValueChanged);
            threeWordsToggle.onValueChanged.AddListener(OnWordsValueChanged);
            sixWordsToggle.onValueChanged.AddListener(OnWordsValueChanged);
            clueToggle.onValueChanged.AddListener(OnClueValueChanged);
            soundEffectsSlider.onValueChanged.AddListener(OnSoundEffectsValueChanged);
            musicSlider.onValueChanged.AddListener(OnMusicValueChanged);
            colorThemeDropdown.onValueChanged.AddListener(OnColorThemeValueChanged);
            displaySizeSlider.onValueChanged.AddListener(OnDisplaySizeValueChanged);
            
            // Disable the settings that can't be changed during active gameplay.
            if (Gameplay.instance != null && Gameplay.instance.state != Gameplay.State.Stopped)
            {
                EnableToggleGroup(difficultyToggleGroup, false);
                EnableToggleGroup(wordsToggleGroup, false);
            }
            
            AccessibilitySettings.boldTextStatusChanged += OnBoldTextStatusChanged;
            AccessibilitySettings.closedCaptioningStatusChanged += OnClosedCaptioningStatusChanged;
            AccessibilitySettings.fontScaleChanged += OnFontScaleValueChanged;
            
            // Initialize the values for the read-only settings.
            OnBoldTextStatusChanged(AccessibilitySettings.isBoldTextEnabled);
            OnClosedCaptioningStatusChanged(AccessibilitySettings.isClosedCaptioningEnabled);
            OnFontScaleValueChanged(AccessibilitySettings.fontScale);
        }

        void OnDisable()
        {
            backButton.GetComponent<AccessibleButton>().dismissed -= OnDismissed;

            usernameInputField.onValueChanged.RemoveListener(OnUsernameValueChanged);
            easyDifficultyToggle.onValueChanged.RemoveListener(OnDifficultyValueChanged);
            hardDifficultyToggle.onValueChanged.RemoveListener(OnDifficultyValueChanged);
            threeWordsToggle.onValueChanged.RemoveListener(OnWordsValueChanged);
            sixWordsToggle.onValueChanged.RemoveListener(OnWordsValueChanged);
            clueToggle.onValueChanged.RemoveListener(OnClueValueChanged);
            soundEffectsSlider.onValueChanged.RemoveListener(OnSoundEffectsValueChanged);
            musicSlider.onValueChanged.RemoveListener(OnMusicValueChanged);
            colorThemeDropdown.onValueChanged.RemoveListener(OnColorThemeValueChanged);
            displaySizeSlider.onValueChanged.RemoveListener(OnDisplaySizeValueChanged);
            
            AccessibilitySettings.boldTextStatusChanged -= OnBoldTextStatusChanged;
            AccessibilitySettings.closedCaptioningStatusChanged -= OnClosedCaptioningStatusChanged;
            AccessibilitySettings.fontScaleChanged -= OnFontScaleValueChanged;
            
            if (Gameplay.instance != null)
            {
                Gameplay.instance.ResumeGame();
            }
        }

        bool OnDismissed()
        {
            backButton.onClick.Invoke();

            return true;
        }

        static void EnableToggleGroup(ToggleGroup toggleGroup, bool enable)
        {
            var toggles = toggleGroup.GetComponentsInChildren<Toggle>();

            foreach (var toggle in toggles)
            {
                toggle.interactable = enable;
            }
        }

        static void LoadInputFieldState(TMP_InputField inputField, string prefName, string defaultValue = null)
        {
            inputField.text = PlayerPrefs.GetString(prefName, defaultValue);
        }

        static void LoadToggleState(Toggle toggle, string prefName, int defaultValue = 0)
        {
            toggle.isOn = PlayerPrefs.GetInt(prefName, defaultValue) == 1;
        }

        static void LoadToggleGroupState(ToggleGroup toggleGroup, string prefName, int defaultValue = 0)
        {
            var savedToggleState = PlayerPrefs.GetInt(prefName, defaultValue);
            var toggles = toggleGroup.GetComponentsInChildren<Toggle>();
            toggles[savedToggleState].isOn = true;
        }

        static void LoadSliderState(Slider slider, string prefName, float defaultValue = 0)
        {
            slider.value = PlayerPrefs.GetFloat(prefName, defaultValue);
        }

        static void LoadDropdownState(TMP_Dropdown dropdown, string prefName, int defaultValue = 0)
        {
            dropdown.value = PlayerPrefs.GetInt(prefName, defaultValue);
        }

        static void OnUsernameValueChanged(string value)
        {
            PlayerPrefs.SetString(usernamePreference, value);
        }

        void OnDifficultyValueChanged(bool value)
        {
            if (value)
            {
                SaveToggleGroupState(difficultyToggleGroup, difficultyPreference);
            }
        }

        void OnWordsValueChanged(bool value)
        {
            if (value)
            {
                SaveToggleGroupState(wordsToggleGroup, wordsPreference);
            }
        }

        static void SaveToggleGroupState(ToggleGroup toggleGroup, string prefName)
        {
            // Find the selected toggle in the ToggleGroup and save its index in
            // the player preferences.
            var toggles = toggleGroup.GetComponentsInChildren<Toggle>();

            for (var i = 0; i < toggles.Length; i++)
            {
                if (toggles[i].isOn)
                {
                    PlayerPrefs.SetInt(prefName, i);
                    break;
                }
            }
        }

        static void OnClueValueChanged(bool value)
        {
            PlayerPrefs.SetInt(cluePreference, value ? 1 : 0);
        }

        static void OnSoundEffectsValueChanged(float value)
        {
            PlayerPrefs.SetFloat(soundEffectsPreference, value);
        }

        static void OnMusicValueChanged(float value)
        {
            PlayerPrefs.SetFloat(musicPreference, value);

            if (AudioManager.instance != null)
            {
                AudioManager.instance.SetMusicVolume(value);
            }
        }

        static void OnColorThemeValueChanged(int value)
        {
            PlayerPrefs.SetInt(k_ColorThemePreference, value);
        }

        static void OnDisplaySizeValueChanged(float value)
        {
            PlayerPrefs.SetFloat(k_DisplaySizePreference, value);
        }
        
        void OnBoldTextStatusChanged(bool boldTextStatus)
        {
            if (boldTextStatus)
            {
                boldTextAccessibleElement.value = k_SettingOn;
                boldTextValue.text = k_SettingOn;
            }
            else
            {
                boldTextAccessibleElement.value = k_SettingOff;
                boldTextValue.text = k_SettingOff;
            }
            boldTextAccessibleElement.SetNodeProperties();
        }
        
        void OnClosedCaptioningStatusChanged(bool closedCaptioningStatus)
        {
            if (closedCaptioningStatus)
            {
                closedCaptionAccessibleElement.value = k_SettingOn;
                closedCaptionValue.text = k_SettingOn;
            }
            else
            {
                closedCaptionAccessibleElement.value = k_SettingOff;
                closedCaptionValue.text = k_SettingOff;
            }
            closedCaptionAccessibleElement.SetNodeProperties();
            
        }

        void OnFontScaleValueChanged(float fontScale)
        {
            string fontScaleText = fontScale.ToString("0.00");
            fontScaleAccessibleElement.value = fontScaleText;
            fontScaleValue.text = fontScaleText;
            fontScaleAccessibleElement.SetNodeProperties();
        }
    }
}
