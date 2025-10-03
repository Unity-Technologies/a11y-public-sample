using UnityEngine;
using UnityEngine.Accessibility;
using UnityEngine.Localization.Settings;
using UnityEngine.Scripting;
using UnityEngine.UIElements;

namespace Unity.Samples.ScreenReader
{
    [Preserve]
    class TextFieldFieldHandler : BaseFieldHandler<string>
    {
        public TextFieldFieldHandler()
        {
            selected += () =>
            {
                var textField = ownerElement as TextField;
                textField?.Focus();

                return true;
            };
        }

        public override string GetValue()
        {
            var textField = ownerElement as TextField;

            if (string.IsNullOrEmpty(textField?.value))
            {
                return ownerElement is TextField tf ? tf.textEdition.placeholder : null;
            }

            return base.GetValue();
        }

        public override string GetHint()
        {
            // if (Application.platform == RuntimePlatform.Android ||
            //     Application.platform == RuntimePlatform.IPhonePlayer)
            // {
            //     // TODO: "Double tap to edit." should be localized.
            //     LocalizationSettings.StringDatabase.GetLocalizedString("", "");
            // }

            return base.GetHint();
        }

#if UNITY_6000_3_OR_NEWER
        public override AccessibilityRole GetRole() => AccessibilityRole.TextField;
#endif // UNITY_6000_3_OR_NEWER
    }
}
