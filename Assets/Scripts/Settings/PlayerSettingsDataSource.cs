using System;
using Unity.Properties;
using UnityEngine;
using UnityEngine.Accessibility;
using UnityEngine.UIElements;

namespace Unity.Samples.LetterSpell
{
    /// <summary>
    /// A data source for UI Toolkit bindings that wraps PlayerPrefs values.
    /// </summary>
    public class PlayerSettingsDataSource : IDisposable, INotifyBindablePropertyChanged
    {
        public event EventHandler<BindablePropertyChangedEventArgs> propertyChanged;

        public PlayerSettingsDataSource()
        {
            AccessibilitySettings.fontScaleChanged += OnFontScaleChanged;
            AccessibilitySettings.boldTextStatusChanged += OnBoldTextStatusChanged;
            AccessibilitySettings.closedCaptioningStatusChanged += OnClosedCaptioningStatusChanged;
        }

        public void Dispose()
        {
            AccessibilitySettings.fontScaleChanged -= OnFontScaleChanged;
            AccessibilitySettings.boldTextStatusChanged -= OnBoldTextStatusChanged;
            AccessibilitySettings.closedCaptioningStatusChanged -= OnClosedCaptioningStatusChanged;
        }

        void OnFontScaleChanged(float _)
        {
            NotifyPropertyChanged(nameof(fontScale));
        }

        void OnBoldTextStatusChanged(bool _)
        {
            NotifyPropertyChanged(nameof(boldText));
        }

        void OnClosedCaptioningStatusChanged(bool _)
        {
            NotifyPropertyChanged(nameof(closedCaptions));
        }

        [CreateProperty]
        public string username
        {
            get => PlayerPrefs.GetString(PlayerSettings.usernamePreference, string.Empty);
            set
            {
                if (username != value)
                {
                    PlayerPrefs.SetString(PlayerSettings.usernamePreference, value);
                    NotifyPropertyChanged(nameof(username));
                }
            }
        }

        [CreateProperty]
        public bool difficultyEnabled => Gameplay.instance == null ||
            Gameplay.instance.state == Gameplay.State.Stopped;

        [CreateProperty]
        public ToggleButtonGroupState difficulty
        {
            get
            {
                var index = PlayerPrefs.GetInt(PlayerSettings.difficultyPreference, 0);
                return new ToggleButtonGroupState(1UL << index, 2);
            }
            set
            {
                if (difficulty != value)
                {
                    var index = value.GetActiveOptions(stackalloc int[value.length])[0];
                    PlayerPrefs.SetInt(PlayerSettings.difficultyPreference, index);
                    NotifyPropertyChanged(nameof(difficulty));
                }
            }
        }

        [CreateProperty]
        public bool wordsEnabled => Gameplay.instance == null || Gameplay.instance.state == Gameplay.State.Stopped;

        [CreateProperty]
        public int words
        {
            get => PlayerPrefs.GetInt(PlayerSettings.wordsPreference, 0);
            set
            {
                if (words != value)
                {
                    PlayerPrefs.SetInt(PlayerSettings.wordsPreference, value);
                    NotifyPropertyChanged(nameof(words));
                }
            }
        }

        [CreateProperty]
        public bool showClues
        {
            get => PlayerPrefs.GetInt(PlayerSettings.cluePreference, 1) == 1;
            set
            {
                if (showClues != value)
                {
                    PlayerPrefs.SetInt(PlayerSettings.cluePreference, value ? 1 : 0);
                    NotifyPropertyChanged(nameof(showClues));
                }
            }
        }

        [CreateProperty]
        public float sfxVolume
        {
            get => PlayerPrefs.GetFloat(PlayerSettings.sfxPreference, 0.5f);
            set
            {
                if (!Mathf.Approximately(sfxVolume, value))
                {
                    PlayerPrefs.SetFloat(PlayerSettings.sfxPreference, value);
                    NotifyPropertyChanged(nameof(sfxVolume));
                }
            }
        }

        [CreateProperty]
        public float musicVolume
        {
            get => PlayerPrefs.GetFloat(PlayerSettings.musicPreference, 0.5f);
            set
            {
                if (!Mathf.Approximately(musicVolume, value))
                {
                    PlayerPrefs.SetFloat(PlayerSettings.musicPreference, value);
                    NotifyPropertyChanged(nameof(musicVolume));

                    AudioManager.SetMusicVolume(value);
                }
            }
        }

        [CreateProperty]
        public int colorTheme
        {
            get => PlayerPrefs.GetInt(PlayerSettings.colorThemePreference, 0);
            set
            {
                if (colorTheme != value)
                {
                    PlayerPrefs.SetInt(PlayerSettings.colorThemePreference, value);
                    NotifyPropertyChanged(nameof(colorTheme));
                }
            }
        }

        [CreateProperty]
        public float displaySize
        {
            get => PlayerPrefs.GetFloat(PlayerSettings.displaySizePreference, 1f);
            set
            {
                if (!Mathf.Approximately(displaySize, value))
                {
                    PlayerPrefs.SetFloat(PlayerSettings.displaySizePreference, value);
                    NotifyPropertyChanged(nameof(displaySize));
                }
            }
        }

        [CreateProperty]
        public string fontScale => AccessibilitySettings.fontScale.ToString("0.00");

        [CreateProperty]
        public string boldText => AccessibilitySettings.isBoldTextEnabled ?
            PlayerSettings.settingOn : PlayerSettings.settingOff;

        [CreateProperty]
        public string closedCaptions => AccessibilitySettings.isClosedCaptioningEnabled ?
            PlayerSettings.settingOn : PlayerSettings.settingOff;

        void NotifyPropertyChanged(string propertyName)
        {
            propertyChanged?.Invoke(this, new BindablePropertyChangedEventArgs(propertyName));
        }
    }
}
