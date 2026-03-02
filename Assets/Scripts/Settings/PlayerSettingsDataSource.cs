using System;
using Unity.Properties;
using UnityEngine;
using UnityEngine.Accessibility;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UIElements;

namespace Unity.Samples.LetterSpell
{
    /// <summary>
    /// A data source for UI Toolkit bindings that wraps PlayerPrefs values.
    /// </summary>
    public class PlayerSettingsDataSource : IDisposable, INotifyBindablePropertyChanged
    {
        static PlayerSettingsDataSource s_Instance;
        static int s_ReferenceCount;

        public event EventHandler<BindablePropertyChangedEventArgs> propertyChanged;

        PlayerSettingsDataSource()
        {
            AccessibilitySettings.fontScaleChanged += OnFontScaleChanged;
            AccessibilitySettings.boldTextStatusChanged += OnBoldTextStatusChanged;
            AccessibilitySettings.closedCaptioningStatusChanged += OnClosedCaptioningStatusChanged;

            LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
        }

        public static PlayerSettingsDataSource Acquire()
        {
            s_Instance ??= new PlayerSettingsDataSource();
            s_ReferenceCount++;
            return s_Instance;
        }

        public static void Release()
        {
            s_ReferenceCount--;

            if (s_ReferenceCount == 0)
            {
                s_Instance.Dispose();
                s_Instance = null;
            }

            if (s_ReferenceCount < 0)
            {
                s_ReferenceCount = 0;
            }
        }

        public void Dispose()
        {
            AccessibilitySettings.fontScaleChanged -= OnFontScaleChanged;
            AccessibilitySettings.boldTextStatusChanged -= OnBoldTextStatusChanged;
            AccessibilitySettings.closedCaptioningStatusChanged -= OnClosedCaptioningStatusChanged;

            LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
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

        void OnLocaleChanged(Locale locale)
        {
            NotifyPropertyChanged(nameof(languageDirection));
            NotifyPropertyChanged(nameof(boldText));
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
        public int language
        {
            get => PlayerPrefs.GetInt(PlayerSettings.languagePreference, 0);
            set
            {
                if (language != value)
                {
                    PlayerPrefs.SetInt(PlayerSettings.languagePreference, value);
                    NotifyPropertyChanged(nameof(language));

                    var locales = LocalizationSettings.AvailableLocales.Locales;
                    if (value >= 0 && value < locales.Count)
                    {
                        LocalizationSettings.SelectedLocale = locales[value];
                    }
                }
            }
        }

        [CreateProperty]
        public LanguageDirection languageDirection => LocalizationSettings.SelectedLocale.Identifier.Code == "ar" ?
                LanguageDirection.RTL : LanguageDirection.LTR;

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

    /// <summary>
    /// Registers a converter from bool to DisplayStyle for UI Toolkit bindings.
    /// </summary>
    public static class BoolToDisplayStyleConverter
    {
#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
#else
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
#endif
        static void Register()
        {
            ConverterGroups.RegisterGlobalConverter((ref bool value) =>
                new StyleEnum<DisplayStyle>(value ? DisplayStyle.Flex : DisplayStyle.None));
        }
    }

    /// <summary>
    /// Registers converters from LanguageDirection to various style properties for UI Toolkit bindings.
    /// </summary>
    public static class LanguageDirectionConverters
    {
#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
#else
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
#endif
        static void Register()
        {
            var leftGroup = new ConverterGroup("LanguageDirectionToLeft");

            leftGroup.AddConverter((ref LanguageDirection value) => value == LanguageDirection.LTR ?
                new StyleLength(new Length(60f)) : new StyleLength(StyleKeyword.Auto));

            var rightGroup = new ConverterGroup("LanguageDirectionToRight");

            rightGroup.AddConverter((ref LanguageDirection value) => value == LanguageDirection.LTR ?
                new StyleLength(StyleKeyword.Auto) : new StyleLength(new Length(60f)));

            var ltrBorderGroup = new ConverterGroup("LanguageDirectionToBorderLTR");

            ltrBorderGroup.AddConverter((ref LanguageDirection value) => value == LanguageDirection.LTR ?
                new StyleFloat(10f) : new StyleFloat(0f));

            var rtlBorderGroup = new ConverterGroup("LanguageDirectionToBorderRTL");

            rtlBorderGroup.AddConverter((ref LanguageDirection value) => value == LanguageDirection.LTR ?
                new StyleFloat(0f) : new StyleFloat(10f));

            var flexDirectionReverseGroup = new ConverterGroup("LanguageDirectionToFlexDirectionReverse");

            flexDirectionReverseGroup.AddConverter((ref LanguageDirection value) => value == LanguageDirection.LTR ?
                new StyleEnum<FlexDirection>(FlexDirection.RowReverse) :
                new StyleEnum<FlexDirection>(FlexDirection.Row));

            var alignSelfStartGroup = new ConverterGroup("LanguageDirectionToAlignSelfStart");

            alignSelfStartGroup.AddConverter((ref LanguageDirection value) => value == LanguageDirection.LTR ?
                new StyleEnum<Align>(Align.FlexStart) : new StyleEnum<Align>(Align.FlexEnd));

            var alignSelfEndGroup = new ConverterGroup("LanguageDirectionToAlignSelfEnd");

            alignSelfEndGroup.AddConverter((ref LanguageDirection value) => value == LanguageDirection.LTR ?
                new StyleEnum<Align>(Align.FlexEnd) : new StyleEnum<Align>(Align.FlexStart));

            var invertedGroup = new ConverterGroup("LanguageDirectionToInverted");

            invertedGroup.AddConverter((ref LanguageDirection value) => value == LanguageDirection.RTL);

            ConverterGroups.RegisterConverterGroup(leftGroup);
            ConverterGroups.RegisterConverterGroup(rightGroup);
            ConverterGroups.RegisterConverterGroup(ltrBorderGroup);
            ConverterGroups.RegisterConverterGroup(rtlBorderGroup);
            ConverterGroups.RegisterConverterGroup(flexDirectionReverseGroup);
            ConverterGroups.RegisterConverterGroup(alignSelfStartGroup);
            ConverterGroups.RegisterConverterGroup(alignSelfEndGroup);
            ConverterGroups.RegisterConverterGroup(invertedGroup);

            ConverterGroups.RegisterGlobalConverter((ref LanguageDirection value) => value == LanguageDirection.LTR ?
                new StyleEnum<FlexDirection>(FlexDirection.Row) :
                new StyleEnum<FlexDirection>(FlexDirection.RowReverse));

            ConverterGroups.RegisterGlobalConverter((ref LanguageDirection value) => value == LanguageDirection.LTR ?
                new StyleEnum<TextAnchor>(TextAnchor.MiddleLeft) :
                new StyleEnum<TextAnchor>(TextAnchor.MiddleRight));
        }
    }
}
