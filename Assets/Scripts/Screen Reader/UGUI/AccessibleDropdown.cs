using System.Collections;
using Unity.Samples.Controls;
using TMPro;
using UnityEngine;
using UnityEngine.Accessibility;
using UnityEngine.UI;

namespace Unity.Samples.ScreenReader
{
    /// <summary>
    /// Component attached to the UI game objects that should be considered dropdowns by the screen reader.
    /// </summary>
    [AddComponentMenu("Accessibility/Accessible Dropdown"), DisallowMultipleComponent]
    [ExecuteAlways]
    public sealed class AccessibleDropdown : AccessibleElement
    {
        MultiSelectDropdown m_MultiSelectDropdown;
        TMP_Dropdown m_TMPDropdown;
        Dropdown m_Dropdown;

        bool m_DropdownExists;

        Coroutine m_ActiveCoroutine;
        AccessibleElement[] m_Options;

        void Start()
        {
#if UNITY_6000_3_OR_NEWER
            role = AccessibilityRole.Dropdown;
#endif // UNITY_6000_3_OR_NEWER

            m_MultiSelectDropdown = GetComponentInChildren<MultiSelectDropdown>();
            m_TMPDropdown = GetComponentInChildren<TMP_Dropdown>();
            m_Dropdown = GetComponentInChildren<Dropdown>();

            m_DropdownExists = m_MultiSelectDropdown != null || m_TMPDropdown != null || m_Dropdown != null;

            if (m_MultiSelectDropdown != null)
            {
                UpdateValue(0);
            }
            else if (m_TMPDropdown != null)
            {
                UpdateValue(m_TMPDropdown.value);
            }
            else if (m_Dropdown != null)
            {
                UpdateValue(m_Dropdown.value);
            }
        }

        protected override void BindToControl()
        {
            base.BindToControl();

            m_ActiveCoroutine = StartCoroutine(WaitForDropdownOpen());

            if (m_MultiSelectDropdown != null)
            {
                m_MultiSelectDropdown.onValueChanged.AddListener(UpdateValue);
            }
            else if (m_TMPDropdown != null)
            {
                m_TMPDropdown.onValueChanged.AddListener(UpdateValue);
            }
            else if (m_Dropdown != null)
            {
                m_Dropdown.onValueChanged.AddListener(UpdateValue);
            }

            if (m_DropdownExists)
            {
                selected += OnSelected;
            }
        }

        protected override void UnbindFromControl()
        {
            base.UnbindFromControl();

            if (m_ActiveCoroutine != null)
            {
                StopCoroutine(m_ActiveCoroutine);
            }

            if (m_MultiSelectDropdown != null)
            {
                m_MultiSelectDropdown.onValueChanged.RemoveListener(UpdateValue);
            }
            else if (m_TMPDropdown != null)
            {
                m_TMPDropdown.onValueChanged.RemoveListener(UpdateValue);
            }
            else if (m_Dropdown != null)
            {
                m_Dropdown.onValueChanged.RemoveListener(UpdateValue);
            }

            if (m_DropdownExists)
            {
                selected -= OnSelected;
            }
        }

        IEnumerator WaitForDropdownOpen()
        {
            yield return new WaitUntil(IsDropdownOpen);
            yield return new WaitForEndOfFrame();

#if UNITY_6000_3_OR_NEWER
            state |= AccessibilityState.Expanded;
            SetNodeProperties();
#endif // UNITY_6000_3_OR_NEWER

            m_Options = gameObject.GetComponentsInChildren<AccessibleElement>();

            // Add the dropdown options to the accessibility hierarchy and notify the screen reader to focus on the
            // first option of the dropdown.
            AddOptionsToHierarchy(node);
            AssistiveSupport.notificationDispatcher.SendLayoutChanged(m_Options[0].node);

            m_ActiveCoroutine = StartCoroutine(WaitForDropdownClose(node));
        }

        IEnumerator WaitForDropdownClose(AccessibilityNode parentNode)
        {
            yield return new WaitUntil(() => !IsDropdownOpen());

#if UNITY_6000_3_OR_NEWER
            state &= ~AccessibilityState.Expanded;
            SetNodeProperties();
#endif // UNITY_6000_3_OR_NEWER

            // Remove the dropdown options from the accessibility hierarchy and notify the screen reader to focus on the
            // dropdown.
            RemoveOptionsFromHierarchy();
            AssistiveSupport.notificationDispatcher.SendLayoutChanged(parentNode);

            m_ActiveCoroutine = StartCoroutine(WaitForDropdownOpen());
        }

        bool IsDropdownOpen()
        {
            if (m_MultiSelectDropdown != null)
            {
                return m_MultiSelectDropdown.IsExpanded;
            }

            if (m_TMPDropdown != null)
            {
                return m_TMPDropdown.IsExpanded;
            }

            if (m_Dropdown != null)
            {
                return m_Dropdown.transform.childCount != 3;
            }

            return false;
        }

        void AddOptionsToHierarchy(AccessibilityNode parent)
        {
            foreach (var option in m_Options)
            {
                UGuiAccessibilityManager.instance.AddToHierarchy(option, parent);
            }
        }

        void RemoveOptionsFromHierarchy()
        {
            foreach (var option in m_Options)
            {
                UGuiAccessibilityManager.instance.RemoveFromHierarchy(option);
            }
        }

        void UpdateValue(int index)
        {
            var valueAsText = "";

            if (index != -1)
            {
                if (m_MultiSelectDropdown != null)
                {
                    valueAsText = m_MultiSelectDropdown.valuesAsString;
                }
                else if (m_TMPDropdown != null)
                {
                    valueAsText = m_TMPDropdown.options != null ? m_TMPDropdown.options[index].text : "";
                }
                else if (m_Dropdown != null)
                {
                    valueAsText = m_Dropdown.options != null ? m_Dropdown.options[index].text : "";
                }
            }

            value = valueAsText;
            SetNodeProperties();
        }

        bool OnSelected()
        {
            if (m_MultiSelectDropdown != null && m_MultiSelectDropdown.IsActive() && m_MultiSelectDropdown.IsInteractable())
            {
                if (m_MultiSelectDropdown.IsExpanded)
                {
                    m_MultiSelectDropdown.Hide();
                }
                else
                {
                    m_MultiSelectDropdown.Show();
                }

                return true;
            }

            if (m_TMPDropdown != null && m_TMPDropdown.IsActive() && m_TMPDropdown.IsInteractable())
            {
                if (m_TMPDropdown.IsExpanded)
                {
                    m_TMPDropdown.Hide();
                }
                else
                {
                    m_TMPDropdown.Show();
                }

                return true;
            }

            if (m_Dropdown != null && m_Dropdown.IsActive() && m_Dropdown.IsInteractable())
            {
                if (m_Dropdown.transform.childCount != 3)
                {
                    m_Dropdown.Hide();
                }
                else
                {
                    m_Dropdown.Show();
                }

                return true;
            }

            return false;
        }
    }
}
