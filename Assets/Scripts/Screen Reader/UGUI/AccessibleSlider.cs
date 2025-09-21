using TMPro;
using UnityEngine;
using UnityEngine.Accessibility;
using UnityEngine.UI;

namespace Unity.Samples.ScreenReader
{
    /// <summary>
    /// Component attached to the UI game objects that should be considered sliders by the screen reader.
    /// </summary>
    [AddComponentMenu("Accessibility/Accessible Slider"), DisallowMultipleComponent]
    [ExecuteAlways]
    public sealed class AccessibleSlider : AccessibleElement
    {
        Slider m_Slider;
        Text m_Text;
        TMP_Text m_TMPText;

        void Start()
        {
            role |= AccessibilityRole.Slider;

            m_Slider = GetComponentInChildren<Slider>();
            m_Text = GetComponentInChildren<Text>();
            m_TMPText = GetComponentInChildren<TMP_Text>();

            if (m_Text != null)
            {
                label ??= m_Text.text;
            }
            else if (m_TMPText != null)
            {
                label ??= m_TMPText.text;
            }

            if (m_Slider != null)
            {
                UpdateValue(m_Slider.value);
            }
        }
        
        protected override void BindToControl()
        {
            if (m_Slider != null)
            {
                m_Slider.onValueChanged.AddListener(UpdateValue);

                incremented += OnIncremented;
                decremented += OnDecremented;
            }
        }
        
        protected override void UnbindFromControl()
        {
            if (m_Slider != null)
            {
                m_Slider.onValueChanged.RemoveListener(UpdateValue);

                incremented -= OnIncremented;
                decremented -= OnDecremented;
            }
        }

        void OnIncremented()
        {
            if (m_Slider.IsActive() && m_Slider.IsInteractable())
            {
                var step = (m_Slider.maxValue - m_Slider.minValue) / 10f;

                m_Slider.value += step;
            }
        }

        void OnDecremented()
        {
            if (m_Slider.IsActive() && m_Slider.IsInteractable())
            {
                var step = (m_Slider.maxValue - m_Slider.minValue) / 10f;

                m_Slider.value -= step;
            }
        }

        void UpdateValue(float newValue)
        {
            value = $"{newValue:P0}";
            SetNodeProperties();
        }
    }
}
