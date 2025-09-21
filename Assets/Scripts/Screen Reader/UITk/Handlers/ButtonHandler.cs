using UnityEngine.Accessibility;
using UnityEngine.Scripting;
using UnityEngine.UIElements;

namespace Unity.Samples.ScreenReader
{
    [Preserve]
    class ButtonHandler : VisualElementAccessibilityHandler
    {
        public override string GetLabel()
        {
            var button = ownerElement as Button;
            return button.text;
        }

        protected override void BindToElement(VisualElement ve)
        {
        }

        public override AccessibilityRole GetRole() => AccessibilityRole.Button;
    }
}
