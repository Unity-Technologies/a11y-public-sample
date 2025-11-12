using UnityEngine;

namespace Unity.Samples.ScreenReader
{
    /// <summary>
    /// Component attached to the UI game objects that should be considered sliders by the screen reader.
    /// </summary>
    [AddComponentMenu("Accessibility/Accessible Slider With Steps"), DisallowMultipleComponent]
    [ExecuteAlways]
    public sealed class AccessibleSliderWithSteps : AccessibleSlider
    {
        protected override void OnIncremented()
        {
            if (m_Slider.IsActive() && m_Slider.IsInteractable() && m_Slider.value < 2)
            {
                m_Slider.value++;
            }
        }

        protected override void OnDecremented()
        {
            if (m_Slider.IsActive() && m_Slider.IsInteractable() && m_Slider.value > 0)
            {
                m_Slider.value--;
            }
        }

        protected override void UpdateValue(float newValue)
        {
            value = newValue switch
            {
                0 => "Small",
                1 => "Medium",
                2 => "Large",
                _ => "Medium"
            };

            SetNodeProperties();
        }
    }
}
