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
            incremented += () => Step(true);
            decremented += () => Step(false);
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

        public override string GetValue()
        {
            return ownerElement is Slider field ? $"{field.value:P0}" : "";
        }

#if UNITY_2023_3_OR_NEWER
        public override AccessibilityRole GetRole() => AccessibilityRole.Slider;
#endif // UNITY_2023_3_OR_NEWER
    }
}
