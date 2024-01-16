using System.Collections;
using Sample.Controls;
using TMPro;
using UnityEngine;
using UnityEngine.Accessibility;
using UnityEngine.UI;

namespace Unity.Samples.Accessibility
{
    /// <summary>
    /// Component attached to the UI GameObjects that are considered as dropdowns by the screen reader.
    /// </summary>
    [AddComponentMenu("Accessibility/Accessible Dropdown"), DisallowMultipleComponent]
    [ExecuteAlways]
    public sealed class AccessibleDropdown : AccessibleElement
    {
        MultiSelectDropdown m_MultiSelectDropdown;
        TMP_Dropdown m_TMPDropdown;
        Dropdown m_Dropdown;

        Coroutine m_ActiveCoroutine;
        bool m_WasDropdownOpenedOrClosed;

        const string k_DropdownClosedHint = "Double tap to expand options.";
        const string k_DropdownOpenHint = "Swipe left to navigate options. Double tap to close options.";

        void Start()
        {
            m_MultiSelectDropdown = GetComponentInChildren<MultiSelectDropdown>();
            m_TMPDropdown = GetComponentInChildren<TMP_Dropdown>();
            m_Dropdown = GetComponentInChildren<Dropdown>();

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

            hint = k_DropdownClosedHint;
        }

        protected override void BindToControl()
        {
            AccessibilityManager.hierarchyRefreshed += OnHierarchyRefreshed;

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
        }

        protected override void UnbindFromControl()
        {
            AccessibilityManager.hierarchyRefreshed -= OnHierarchyRefreshed;

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
        }

        void OnHierarchyRefreshed()
        {
            if (m_WasDropdownOpenedOrClosed)
            {
                var isDropdownOpen = IsDropdownOpen();

                // While the dropdown is open, the user should only be able to navigate within the dropdown itself.
                // Therefore, we deactivate any other accessibility nodes on screen.
                // After the dropdown is closed, we bring the other accessibility nodes back to their original active
                // state.
                AccessibilityManager.ActivateOtherAccessibilityNodes(!isDropdownOpen, transform);

                AssistiveSupport.notificationDispatcher.SendLayoutChanged(node);

                m_WasDropdownOpenedOrClosed = false;
            }
        }

        IEnumerator WaitForDropdownOpen()
        {
            yield return new WaitUntil(IsDropdownOpen);
            yield return new WaitForEndOfFrame();

            AccessibilityManager.RefreshHierarchy();
            hint = k_DropdownOpenHint;

            m_WasDropdownOpenedOrClosed = true;
            m_ActiveCoroutine = StartCoroutine(WaitForDropdownClose());
        }

        IEnumerator WaitForDropdownClose()
        {
            yield return new WaitUntil(() => !IsDropdownOpen());

            AccessibilityManager.RefreshHierarchy();
            hint = k_DropdownClosedHint;

            m_WasDropdownOpenedOrClosed = true;
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
                return m_Dropdown.gameObject.transform.childCount != 3;
            }

            return false;
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
    }
}
