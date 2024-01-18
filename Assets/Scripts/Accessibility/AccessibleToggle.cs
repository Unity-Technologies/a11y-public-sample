using Sample.Controls;
using TMPro;
using UnityEngine;
using UnityEngine.Accessibility;
using UnityEngine.UI;

namespace Unity.Samples.Accessibility
{
    /// <summary>
    /// Component attached to the UI game objects that should be considered toggles by the screen reader.
    /// </summary>
    [AddComponentMenu("Accessibility/Accessible Toggle"), DisallowMultipleComponent]
    [ExecuteAlways]
    public sealed class AccessibleToggle : AccessibleElement
    {
        Toggle m_Toggle;
        Text m_Text;
        TMP_Text m_TMPText;

        bool m_IsInsideDropdown;

        void Start()
        {
            role |= AccessibilityRole.Toggle;

            m_Toggle = GetComponentInChildren<Toggle>();
            m_Text = GetComponentInChildren<Text>();
            m_TMPText = GetComponentInChildren<TMP_Text>();

            m_IsInsideDropdown = IsInsideDropdown();

            if (m_Text != null)
            {
                if (m_IsInsideDropdown)
                {
                    label = m_Text.text;
                }
                else
                {
                    label ??= m_Text.text;
                }
            }
            else if (m_TMPText != null)
            {
                if (m_IsInsideDropdown)
                {
                    label = m_TMPText.text;
                }
                else
                {
                    label ??= m_TMPText.text;
                }
            }

            if (m_Toggle != null)
            {
                UpdateValue(m_Toggle.isOn);
            }
        }

        protected override void BindToControl()
        {
            if (m_Toggle != null)
            {
                m_Toggle.onValueChanged.AddListener(UpdateValue);
                selected += OnSelected;
            }
        }

        protected override void UnbindFromControl()
        {
            if (m_Toggle != null)
            {
                selected -= OnSelected;
                m_Toggle.onValueChanged.RemoveListener(UpdateValue);
            }
        }

        bool OnSelected()
        {
            if (m_Toggle.IsActive() && m_Toggle.IsInteractable())
            {
                m_Toggle.isOn = !m_Toggle.isOn;
                return true;
            }

            return false;
        }

        void UpdateValue(bool newValue)
        {
            if (newValue)
            {
                state |= AccessibilityState.Selected;
            }
            else
            {
                state &= ~AccessibilityState.Selected;
            }

            SetNodeProperties();
        }

        bool IsInsideDropdown()
        {
            var currentTransform = transform.parent;

            // Traverse up the parent hierarchy.
            while (currentTransform != null)
            {
                // Check if the current parent has a dropdown component.
                if (currentTransform.GetComponent<MultiSelectDropdown>() != null ||
                    currentTransform.GetComponent<TMP_Dropdown>() != null ||
                    currentTransform.GetComponent<Dropdown>() != null)
                {
                    return true; // Found the dropdown.
                }

                // Move to the next parent.
                currentTransform = currentTransform.parent;
            }

            return false; // No dropdown found.
        }
    }
}
