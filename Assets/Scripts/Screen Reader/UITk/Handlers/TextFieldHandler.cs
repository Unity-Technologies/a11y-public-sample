using UnityEngine.Scripting;
using UnityEngine.UIElements;

namespace Unity.Samples.ScreenReader
{
    [Preserve]
    class TextFieldFieldHandler : BaseFieldHandler<string>
    {
        public TextFieldFieldHandler()
        {
            OnSelect += () =>
            {
                var textField = ownerElement as TextField;
                textField.Focus();
                return true;
            };
        }
    }
}
