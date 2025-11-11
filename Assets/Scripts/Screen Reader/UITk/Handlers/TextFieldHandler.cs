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

        public override string GetValue()
        {
            var textField = ownerElement as TextField;

            if (string.IsNullOrEmpty(textField?.value))
            {
                return ownerElement is TextField tf ? tf.textEdition.placeholder : null;
            }

            return base.GetValue();
        }

#if UNITY_6000_3_OR_NEWER
        public override AccessibilityRole GetRole() => AccessibilityRole.TextField;
#endif // UNITY_6000_3_OR_NEWER
    }
}
