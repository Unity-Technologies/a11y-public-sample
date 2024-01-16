using TMPro;
using UnityEngine;
using UnityEngine.Accessibility;
using UnityEngine.UI;

namespace Unity.Samples.Accessibility
{
    /// <summary>
    /// Component attached to the UI GameObjects that are considered as texts by the screen reader.
    /// </summary>
    [AddComponentMenu("Accessibility/Accessible Text"), DisallowMultipleComponent]
    [ExecuteAlways]
    public sealed class AccessibleText : AccessibleElement
    {
        Text m_Text;
        TMP_Text m_TMPText;

        void Start()
        {
            role |= AccessibilityRole.StaticText;

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
    }
}
