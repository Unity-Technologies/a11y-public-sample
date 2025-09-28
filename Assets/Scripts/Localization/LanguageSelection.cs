using System.Collections.Generic;
using UnityEngine.Localization.Pseudo;
using UnityEngine.Localization.Settings;
using UnityEngine.UIElements;

namespace UnityEngine.Localization
{
    [UxmlElement]
    public partial class LanguageSelection : PopupField<Locale>
    {
        [UxmlAttribute]
        public string tableName;

        [UxmlAttribute]
        public bool showPseudoLocales;

        public LanguageSelection()
        {
            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
        }

        void OnAttachToPanel(AttachToPanelEvent evt)
        {
            formatListItemCallback = FormatLocaleName;
            formatSelectedValueCallback = FormatLocaleName;

            if (LocalizationSettings.InitializationOperation.IsDone)
            {
                SetupLocalization();
            }
            else
            {
                LocalizationSettings.InitializationOperation.Completed += _ => SetupLocalization();
            }

            RegisterCallback<ChangeEvent<Locale>>(evt =>
            {
                LocalizationSettings.SelectedLocale = evt.newValue;
            });

            LocalizationSettings.SelectedLocaleChanged += _ => SetValueWithoutNotify(LocalizationSettings.SelectedLocale);
        }

        void SetupLocalization()
        {
            var choices = new List<Locale>();
            foreach (var locale in LocalizationSettings.AvailableLocales.Locales)
            {
                if (locale is PseudoLocale)
                {
                    if (showPseudoLocales)
                    {
                        choices.Add(locale);
                    }
                }
                else
                {
                    choices.Add(locale);
                }
            }

            this.choices = choices;

            // Schedule to avoid the rendering update method exception when called from addressables event.
            schedule.Execute(() => SetValueWithoutNotify(LocalizationSettings.SelectedLocale));
        }

        string FormatLocaleName(Locale locale)
        {
            if (locale == null)
            {
                return "None";
            }

            if (string.IsNullOrEmpty(tableName))
            {
                return locale.LocaleName;
            }

            return LocalizationSettings.StringDatabase.GetLocalizedString(tableName, "LANGUAGE_" + locale.LocaleName.ToUpper());
        }
    }
}
