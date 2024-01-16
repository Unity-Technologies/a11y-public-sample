using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Unity.Samples.Accessibility
{
    /// <summary>
    /// Component attached to the UI GameObjects that are considered as input fields by the screen reader.
    /// </summary>
    [AddComponentMenu("Accessibility/Accessible Input"), DisallowMultipleComponent]
    [ExecuteAlways]
    public sealed class AccessibleInput : AccessibleElement
    {
        TMP_InputField m_TMPInputField;
        InputField m_InputField;

        string m_PlaceholderText;

        void Start()
        {
            m_TMPInputField = GetComponentInChildren<TMP_InputField>();
            m_InputField = GetComponentInChildren<InputField>();

            if (m_TMPInputField != null)
            {
                m_PlaceholderText = (m_TMPInputField.placeholder as TMP_Text)?.text;
                UpdateValue(m_TMPInputField.text);
            }
            else if (m_InputField != null)
            {
                m_PlaceholderText = (m_InputField.placeholder as Text)?.text;
                UpdateValue(m_InputField.text);
            }

            hint = "Double tap to edit.";
        }

        protected override void BindToControl()
        {
            if (m_TMPInputField != null)
            {
                m_TMPInputField.onValueChanged.AddListener(UpdateValue);
            }
            else if (m_InputField != null)
            {
                m_InputField.onValueChanged.AddListener(UpdateValue);
            }
        }

        protected override void UnbindFromControl()
        {
            if (m_TMPInputField != null)
            {
                m_TMPInputField.onValueChanged.RemoveListener(UpdateValue);
            }
            else if (m_InputField != null)
            {
                m_InputField.onValueChanged.RemoveListener(UpdateValue);
            }
        }

        void UpdateValue(string newValue)
        {
            value = string.IsNullOrEmpty(newValue) ? m_PlaceholderText : newValue;
            SetNodeProperties();
        }
    }
}
