using UnityEngine.Accessibility;
using UnityEngine.Scripting;
using UnityEngine.UIElements;

namespace Unity.Samples.ScreenReader
{
    [Preserve]
    class TextFieldFieldHandler : BaseFieldHandler<string>
    {
        public override AccessibilityRole GetRole()
        {
            return AccessibilityRole.TextField;
        }

        public TextFieldFieldHandler()
        {
            OnSelect += () =>
            {
                var textField = ownerElement as TextField;
                textField?.Focus();

                return true;
            };
        }
    }
}
