using UnityEngine.Accessibility;
using UnityEngine.Scripting;
using UnityEngine.UIElements;

namespace Unity.Samples.ScreenReader
{
    [Preserve]
    class LabelHandler : VisualElementAccessibilityHandler
    {
        public override AccessibilityRole GetRole() => AccessibilityRole.StaticText;
        public override string GetLabel()
        {
            var label = ownerElement as Label;
            return label.text;
        }
    }
}
