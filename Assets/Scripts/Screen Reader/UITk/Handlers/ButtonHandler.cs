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
                using var evt = NavigationSubmitEvent.GetPooled();
                evt.target = ownerElement;
                ownerElement.SendEvent(evt);

                return true;
            };
        }

        public override string GetLabel()
        {
            return ownerElement is Button button ? button.text : base.GetLabel();
        }

        public override AccessibilityRole GetRole() => AccessibilityRole.Button;

        public override AccessibilityState GetState() => ownerElement is Button { hasCheckedPseudoState: true }
            ? AccessibilityState.Selected
            : AccessibilityState.None;
    }
}
