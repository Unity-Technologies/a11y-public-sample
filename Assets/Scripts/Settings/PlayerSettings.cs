using System;
using Unity.Samples.ScreenReader;
using TMPro;
using UnityEngine;
using UnityEngine.Accessibility;
using UnityEngine.UI;

namespace Unity.Samples.LetterSpell
{
    public class PlayerSettings : MonoBehaviour
    {
        public const string usernamePreference = "Username";
        public const string difficultyPreference = "DifficultyLevel";
        public const string wordsPreference = "WordNumber";
        public const string cluePreference = "ShowClues";
        public const string sfxPreference = "SoundEffectsVolume";
        public const string musicPreference = "MusicVolume";
        public const string colorThemePreference = "ColorTheme";
        public const string displaySizePreference = "DisplaySize";

        public const string settingOn = "On";
        public const string settingOff = "Off";

        public Button backButton;

        public TMP_InputField usernameInputField;
        public ToggleGroup difficultyToggleGroup;
        public Toggle easyDifficultyToggle;
        public Toggle hardDifficultyToggle;
        public ToggleGroup wordsToggleGroup;
        public Toggle threeWordsToggle;
        public Toggle sixWordsToggle;
        public Toggle clueToggle;
        public Slider sfxSlider;
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

        void Update()
        {
            // Close this screen when the device's Back button is pressed. (This only applies to Android.)
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                OnDismissed();
            }
        }

        void OnEnable()
        {
            // Close this screen when the screen reader user performs the dismiss gesture.
            backButton.GetComponent<AccessibleButton>().dismissed += OnDismissed;

            // Load and apply the saved player preferences.
            LoadInputFieldState(usernameInputField, usernamePreference);
            LoadToggleGroupState(difficultyToggleGroup, difficultyPreference);
            LoadToggleGroupState(wordsToggleGroup, wordsPreference);
            LoadToggleState(clueToggle, cluePreference, 1);
            LoadSliderState(sfxSlider, sfxPreference, 0.5f);
            LoadSliderState(musicSlider, musicPreference, 0.5f);
            LoadDropdownState(colorThemeDropdown, colorThemePreference);
            LoadSliderState(displaySizeSlider, displaySizePreference, 1f);

            usernameInputField.onValueChanged.AddListener(OnUsernameValueChanged);
            easyDifficultyToggle.onValueChanged.AddListener(OnDifficultyValueChanged);
            hardDifficultyToggle.onValueChanged.AddListener(OnDifficultyValueChanged);
            threeWordsToggle.onValueChanged.AddListener(OnWordsValueChanged);
            sixWordsToggle.onValueChanged.AddListener(OnWordsValueChanged);
            clueToggle.onValueChanged.AddListener(OnClueValueChanged);
            sfxSlider.onValueChanged.AddListener(OnSfxValueChanged);
            musicSlider.onValueChanged.AddListener(OnMusicValueChanged);
            colorThemeDropdown.onValueChanged.AddListener(OnColorThemeValueChanged);
            displaySizeSlider.onValueChanged.AddListener(OnDisplaySizeValueChanged);

            // Disable the settings that can't be changed during active gameplay.
            if (Gameplay.instance != null && Gameplay.instance.state != Gameplay.State.Stopped)
            {
                EnableToggleGroup(difficultyToggleGroup, false);
                EnableToggleGroup(wordsToggleGroup, false);
            }

            AccessibilitySettings.fontScaleChanged += OnFontScaleValueChanged;
            AccessibilitySettings.boldTextStatusChanged += OnBoldTextStatusChanged;
            AccessibilitySettings.closedCaptioningStatusChanged += OnClosedCaptioningStatusChanged;

            // Initialize the values for the read-only settings.
            OnFontScaleValueChanged(AccessibilitySettings.fontScale);
            OnBoldTextStatusChanged(AccessibilitySettings.isBoldTextEnabled);
            OnClosedCaptioningStatusChanged(AccessibilitySettings.isClosedCaptioningEnabled);
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
            sfxSlider.onValueChanged.RemoveListener(OnSfxValueChanged);
            musicSlider.onValueChanged.RemoveListener(OnMusicValueChanged);
            colorThemeDropdown.onValueChanged.RemoveListener(OnColorThemeValueChanged);
            displaySizeSlider.onValueChanged.RemoveListener(OnDisplaySizeValueChanged);

            AccessibilitySettings.fontScaleChanged -= OnFontScaleValueChanged;
            AccessibilitySettings.boldTextStatusChanged -= OnBoldTextStatusChanged;
            AccessibilitySettings.closedCaptioningStatusChanged -= OnClosedCaptioningStatusChanged;
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
            // Find the selected toggle in the ToggleGroup and save its index in the player preferences.
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

        static void OnSfxValueChanged(float value)
        {
            PlayerPrefs.SetFloat(sfxPreference, value);
        }

        static void OnMusicValueChanged(float value)
        {
            PlayerPrefs.SetFloat(musicPreference, value);

            AudioManager.SetMusicVolume(value);
        }

        static void OnColorThemeValueChanged(int value)
        {
            PlayerPrefs.SetInt(colorThemePreference, value);
        }

        static void OnDisplaySizeValueChanged(float value)
        {
            PlayerPrefs.SetFloat(displaySizePreference, value);
        }

        void OnFontScaleValueChanged(float fontScale)
        {
            var fontScaleText = fontScale.ToString("0.00");

            fontScaleAccessibleElement.value = fontScaleText;
            fontScaleValue.text = fontScaleText;

            fontScaleAccessibleElement.SetNodeProperties();
        }

        void OnBoldTextStatusChanged(bool boldTextStatus)
        {
            if (boldTextStatus)
            {
                boldTextAccessibleElement.value = settingOn;
                boldTextValue.text = settingOn;
            }
            else
            {
                boldTextAccessibleElement.value = settingOff;
                boldTextValue.text = settingOff;
            }

            boldTextAccessibleElement.SetNodeProperties();
        }

        void OnClosedCaptioningStatusChanged(bool closedCaptioningStatus)
        {
            if (closedCaptioningStatus)
            {
                closedCaptionAccessibleElement.value = settingOn;
                closedCaptionValue.text = settingOn;
            }
            else
            {
                closedCaptionAccessibleElement.value = settingOff;
                closedCaptionValue.text = settingOff;
            }

            closedCaptionAccessibleElement.SetNodeProperties();
        }
    }
}
