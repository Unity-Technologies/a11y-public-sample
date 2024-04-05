using Unity.Samples.Controls;
using TMPro;
using UnityEngine;
using UnityEngine.Accessibility;
using UnityEngine.UI;

namespace Unity.Samples.ScreenReader
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
                    // Dropdown items are created dynamically, so we need to set the label here.
                    label = m_Text.text;
                }
                else
                {
                    // Do not override the label if it was set in the Editor.
                    label ??= m_Text.text;
                }
            }
            else if (m_TMPText != null)
            {
                if (m_IsInsideDropdown)
                {
                    // Dropdown items are created dynamically, so we need to set the label here.
                    label = m_TMPText.text;
                }
                else
                {
                    // Do not override the label if it was set in the Editor.
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

                // By default, when the screen reader is on, the double-tap gesture sends a tap event to the center of
                // the focused node's accessibility frame. This means that if the selectable game object is not in the
                // center of its accessibility frame (which could also include the control's label, for example), it
                // will not receive the tap event. To make sure the game object is selected by the screen reader no
                // matter where it is in its accessibility frame, we implement the selected event, which is triggered on
                // double-tap.
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
