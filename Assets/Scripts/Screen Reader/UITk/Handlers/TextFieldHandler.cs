using UnityEngine;
using UnityEngine.Accessibility;
using UnityEngine.Scripting;
using UnityEngine.UIElements;

namespace Unity.Samples.ScreenReader
{
    [Preserve]
    class TextFieldFieldHandler : BaseFieldHandler<string>
    {
        public TextFieldFieldHandler()
        {
            if (Application.platform == RuntimePlatform.OSXPlayer ||
                Application.platform == RuntimePlatform.WindowsPlayer)
            {
                focused += focused =>
                {
                    var textField = ownerElement as TextField;

                    if (focused)
                    {
                        textField?.Focus();
                    }
                    else
                    {
                        textField?.Blur();
                    }
                };
            }
        }

        public override string GetLabel()
        {
            return ownerElement is TextField textField ? textField.label : base.GetLabel();
        }

        public override string GetValue()
        {
            return ownerElement is TextField textField ?
                string.IsNullOrEmpty(textField.value) ? textField.textEdition.placeholder : textField.value :
                base.GetValue();
        }

#if UNITY_6000_3_OR_NEWER
        public override AccessibilityRole GetRole() => AccessibilityRole.TextField;
#endif // UNITY_6000_3_OR_NEWER
    }
}
