using UnityEngine.Accessibility;
using UnityEngine.Scripting;
using UnityEngine.UIElements;

namespace Unity.Samples.ScreenReader
{
    [Preserve]
    class ButtonHandler : VisualElementAccessibilityHandler
    {
        public ButtonHandler()
        {
            selected += () =>
            {
                if (ownerElement is not Button button)
                {
                    return false;
                }

                using var e = new NavigationSubmitEvent();
                e.target = button;
                button.SendEvent(e);

                return true;
            };
        }

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
