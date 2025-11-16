using UnityEngine.Accessibility;
using UnityEngine.Scripting;
using UnityEngine.UIElements;

namespace Unity.Samples.ScreenReader
{
    [Preserve]
    class LabelHandler : VisualElementAccessibilityHandler
    {
        public override string GetLabel()
        {
            return ownerElement is Label lbl ? lbl.text : base.GetLabel();
        }

        public override AccessibilityRole GetRole() => AccessibilityRole.StaticText;
    }
}
