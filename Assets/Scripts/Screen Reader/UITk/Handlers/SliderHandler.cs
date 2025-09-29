using UnityEngine.Accessibility;
using UnityEngine.Scripting;
using UnityEngine.UIElements;

namespace Unity.Samples.ScreenReader
{
    [Preserve]
    class BaseSliderHandler<TValue> : BaseFieldHandler<TValue>
    {
        public BaseSliderHandler()
        {
            OnIncrement += () => Step(true);
            OnDecrement += () => Step(false);
        }

        public void Step(bool incr)
        {
            if (ownerElement is not Slider slider)
            {
                return;
            }

            var step = (slider.highValue - slider.lowValue) / 10f;

            if (!incr)
            {
                step = -step;
            }

            slider.value += step;
        }

        public override AccessibilityRole GetRole() => AccessibilityRole.Slider;
    }
}
