using TMPro;
using UnityEngine;
using UnityEngine.Accessibility;
using UnityEngine.UI;

namespace Unity.Samples.ScreenReader
{
    /// <summary>
    /// Component attached to the UI game objects that should be considered buttons by the screen reader.
    /// </summary>
    [AddComponentMenu("Accessibility/Accessible Button"), DisallowMultipleComponent]
    [ExecuteAlways]
    public sealed class AccessibleButton : AccessibleElement
    {
        Button m_Button;
        Text m_Text;
        TMP_Text m_TMPText;

        void Start()
        {
            role |= AccessibilityRole.Button;

            m_Button = GetComponentInChildren<Button>();
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
        }

        protected override void BindToControl()
        {
            if (m_Button != null)
            {
                // By default, when the screen reader is on, the double-tap gesture sends a tap event to the center of
                // the focused node's accessibility frame. Therefore, implementing the selected event (which is
                // triggered on double-tap) is not necessary for the button to be clicked. However, implementing this
                // event explicitly tells the screen reader that the node is selectable, which may lead to better
                // behaviour.
                selected += OnSelected;
            }
        }

        protected override void UnbindFromControl()
        {
            if (m_Button != null)
            {
                selected -= OnSelected;
            }
        }

        bool OnSelected()
        {
            if (m_Button.IsActive() && m_Button.IsInteractable())
            {
                m_Button.onClick.Invoke();
                return true;
            }

            return false;
        }
    }
}
