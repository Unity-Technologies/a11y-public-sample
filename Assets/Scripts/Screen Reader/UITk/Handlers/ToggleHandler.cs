using UnityEngine.Accessibility;
using UnityEngine.Scripting;
using UnityEngine.UIElements;

namespace Unity.Samples.ScreenReader
{
    [Preserve]
    class ToggleHandler : BaseFieldHandler<bool>
    {
        public ToggleHandler()
        {
            selected += () =>
            {
                using var evt = NavigationSubmitEvent.GetPooled();
                evt.target = ownerElement;
                ownerElement.SendEvent(evt);

                return true;
            };
        }

        public override string GetValue() => "";

#if UNITY_2023_3_OR_NEWER
        public override AccessibilityRole GetRole() => AccessibilityRole.Toggle;
#endif // UNITY_2023_3_OR_NEWER

        public override AccessibilityState GetState() => ownerElement is Toggle { value: true }
            ? AccessibilityState.Selected
            : AccessibilityState.None;
    }
}
