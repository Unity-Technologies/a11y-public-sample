using UnityEngine.Accessibility;
using UnityEngine.Scripting;
using UnityEngine.UIElements;

namespace Unity.Samples.ScreenReader
{
    [Preserve]
    class TextFieldFieldHandler : BaseFieldHandler<string>
    {
        public override string GetValue()
        {
            var textField = ownerElement as TextField;
            
            if (string.IsNullOrEmpty(textField.value))
                return ownerElement is TextField tf ? tf.textEdition.placeholder : textField.value;
            return base.GetValue();
        }
        
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
