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
            return (ownerElement as Button)?.text;
        }

        protected override void BindToElement(VisualElement ve)
        {
        }

        public override AccessibilityRole GetRole() => AccessibilityRole.Button;
    }
}
