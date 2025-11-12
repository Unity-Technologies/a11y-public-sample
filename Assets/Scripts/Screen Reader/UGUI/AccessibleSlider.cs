using TMPro;
using UnityEngine;
using UnityEngine.Accessibility;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Unity.Samples.ScreenReader
{
    /// <summary>
    /// Component attached to the UI game objects that should be considered sliders by the screen reader.
    /// </summary>
    [AddComponentMenu("Accessibility/Accessible Slider"), DisallowMultipleComponent]
    [ExecuteAlways]
    public class AccessibleSlider : AccessibleElement
    {
        protected Slider m_Slider;

        Text m_Text;
        TMP_Text m_TMPText;

        void Start()
        {
#if UNITY_2023_3_OR_NEWER
            role = AccessibilityRole.Slider;
#endif // UNITY_2023_3_OR_NEWER

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

                if (Application.platform == RuntimePlatform.WindowsPlayer)
                {
                    focused += OnSliderFocused;
                }
            }
        }

        protected override void UnbindFromControl()
        {
            if (m_Slider != null)
            {
                m_Slider.onValueChanged.RemoveListener(UpdateValue);

                incremented -= OnIncremented;
                decremented -= OnDecremented;

                if (Application.platform == RuntimePlatform.WindowsPlayer)
                {
                    focused -= OnSliderFocused;
                }
            }
        }

        void OnSliderFocused(bool isFocused)
        {
            if (isFocused)
            {
                if (m_Slider != null && m_Slider.IsActive() && m_Slider.IsInteractable())
                {
                    m_Slider.Select();
                }
            }
            else if (!EventSystem.current.alreadySelecting)
            {
                EventSystem.current.SetSelectedGameObject(null);
            }
        }

        protected virtual void OnIncremented()
        {
            if (m_Slider.IsActive() && m_Slider.IsInteractable())
            {
                var step = (m_Slider.maxValue - m_Slider.minValue) / 10f;

                m_Slider.value += step;
            }
        }

        protected virtual void OnDecremented()
        {
            if (m_Slider.IsActive() && m_Slider.IsInteractable())
            {
                var step = (m_Slider.maxValue - m_Slider.minValue) / 10f;

                m_Slider.value -= step;
            }
        }

        protected virtual void UpdateValue(float newValue)
        {
            value = $"{newValue:P0}";
            SetNodeProperties();
        }
    }
}
