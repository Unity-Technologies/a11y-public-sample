using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Unity.Properties;
using UnityEngine;
using UnityEngine.Accessibility;
using UnityEngine.UIElements;
using Unity.Samples.ScreenReader;
using UnityEngine.Localization;
using UnityEngine.Localization.SmartFormat.Extensions;
using UnityEngine.Localization.SmartFormat.PersistentVariables;

namespace Unity.Samples.LetterSpell
{
    /// <summary>
    /// Holds player settings data and persists it using PlayerPrefs.
    /// </summary>
    class PlayerSettingsData : INotifyBindablePropertyChanged
    {
        const string k_UsernamePref = "Username";
        const string k_DifficultyPref = "GameDifficulty";
        const string k_WordsPref = "GameWords";
        public const string k_CluePref = "ShowClues";
        public const string k_SoundEffectsPref = "SoundEffectsVolume";
        public const string k_MusicPref = "MusicVolume";
        const string k_ColorThemePref = "ColorTheme";
        const string k_DisplaySizePref = "DisplaySize";
        const string k_SettingOn = "On";
        const string k_SettingOff = "Off";

        /// <summary>
        /// The player's username.
        /// </summary>
        [CreateProperty]
        public string userName
        {
            get => PlayerPrefs.GetString(k_UsernamePref);
            set
            {
                if (userName == value)
                {
                    return;
                }

                PlayerPrefs.SetString(k_UsernamePref, value);
                Notify();
            }
        }

        /// <summary>
        /// The game's difficulty level, from 0 (easiest) to 1 (hardest).
        /// </summary>
        [CreateProperty]
        public int difficultyLevel
        {
            get => PlayerPrefs.GetInt(k_DifficultyPref, 0);
            set
            {
                if (difficultyLevel == value)
                {
                    return;
                }

                PlayerPrefs.SetInt(k_DifficultyPref, value);
                Notify();
            }
        }

        /// <summary>
        /// Indicates whether the game uses three-word (0) or six-word (1) puzzles.
        /// </summary>
        [CreateProperty]
        public bool isThreeWords
        {
            get => wordsCountChoiceIndex == 0;
            set => wordsCountChoiceIndex = value ? 0 : 1;
        }

        /// <summary>
        /// Indicates whether the game uses six-word (1) or three-word (0) puzzles.
        /// </summary>
        [CreateProperty]
        public bool isSixWords
        {
            get => wordsCountChoiceIndex == 1;
            set => wordsCountChoiceIndex = value ? 1 : 0;
        }

        /// <summary>
        /// The number of words per puzzle, either 0 (three words) or 1 (six words).
        /// </summary>
        [CreateProperty]
        public int wordsCountChoiceIndex
        {
            get => PlayerPrefs.GetInt(k_WordsPref, 0);
            set
            {
                if (wordsCountChoiceIndex == value)
                {
                    return;
                }

                PlayerPrefs.SetInt(k_WordsPref, value);
                Notify(nameof(isSixWords));
                Notify(nameof(isThreeWords));
            }
        }

        /// <summary>
        /// Indicates whether spelling clues are shown during gameplay.
        /// </summary>
        [CreateProperty]
        public bool showsSpellingClues
        {
            get => PlayerPrefs.GetInt(k_CluePref, 0) == 1;
            set
            {
                if (showsSpellingClues == value)
                {
                    return;
                }

                PlayerPrefs.SetInt(k_CluePref, value ? 1 : 0);
                Notify();
            }
        }

        /// <summary>
        /// The volume of sound effects, from 0 (silent) to 1 (full volume).
        /// </summary>
        [CreateProperty]
        public float soundEffectVolume
        {
            get => PlayerPrefs.GetFloat(k_SoundEffectsPref, 0.5f);
            set
            {
                if (Mathf.Approximately(soundEffectVolume, value))
                {
                    return;
                }

                PlayerPrefs.SetFloat(k_SoundEffectsPref, value);
                Notify();
            }
        }

        /// <summary>
        /// The volume of music, from 0 (silent) to 1 (full volume).
        /// </summary>
        [CreateProperty]
        public float musicVolume
        {
            get => PlayerPrefs.GetFloat(k_MusicPref, 0.5f);
            set
            {
                if (Mathf.Approximately(musicVolume, value))
                {
                    return;
                }

                PlayerPrefs.SetFloat(k_MusicPref, value);
                Notify();
            }
        }

        /// <summary>
        /// The color theme, either 0 (light) or 1 (dark).
        /// </summary>
        [CreateProperty]
        public int colorTheme
        {
            get => PlayerPrefs.GetInt(k_ColorThemePref, 0);
            set
            {
                if (colorTheme == value)
                {
                    return;
                }

                PlayerPrefs.SetInt(k_ColorThemePref, value);
                Notify();
            }
        }

        /// <summary>
        /// The display size, from 0 (smallest) to 1 (largest).
        /// </summary>
        [CreateProperty]
        public float displaySize
        {
            get => PlayerPrefs.GetFloat(k_DisplaySizePref, 0.5f);
            set
            {
                if (Mathf.Approximately(displaySize, value))
                {
                    return;
                }

                PlayerPrefs.SetFloat(k_DisplaySizePref, value);
                Notify();
            }
        }

        /// <summary>
        /// Return "On" is closed captions are enabled in system settings, otherwise "Off".
        /// </summary>
        [CreateProperty]
        public string closedCaptionsEnabledTextAsString =>
            AccessibilitySettings.isClosedCaptioningEnabled ? k_SettingOn : k_SettingOff;

        /// <summary>
        /// Return "On" is bold text is enabled in system settings, otherwise "Off".
        /// </summary>
        [CreateProperty]
        public string boldTextEnabledTextAsString => AccessibilitySettings.isBoldTextEnabled ? k_SettingOn : k_SettingOff;

        /// <summary>
        /// The accessibility font scale from system settings.
        /// </summary>
        [CreateProperty] public float fontScale => AccessibilitySettings.fontScale;

        /// <summary>
        /// Event raised when a property changes.
        /// </summary>
        public event EventHandler<BindablePropertyChangedEventArgs> propertyChanged;

        /// <summary>
        /// Notifies that a property has changed.
        /// </summary>
        /// <param name="property"></param>
        void Notify([CallerMemberName] string property = "")
        {
            propertyChanged?.Invoke(this, new BindablePropertyChangedEventArgs(property));
        }
    }
}