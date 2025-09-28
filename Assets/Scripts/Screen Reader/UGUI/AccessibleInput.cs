using TMPro;
using UnityEngine;
using UnityEngine.Accessibility;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Unity.Samples.ScreenReader
{
    /// <summary>
    /// Component attached to the UI game objects that should be considered input fields by the screen reader.
    /// </summary>
    [AddComponentMenu("Accessibility/Accessible Input"), DisallowMultipleComponent]
    [ExecuteAlways]
    public sealed class AccessibleInput : AccessibleElement
    {
        public bool isSearchField;

        TMP_InputField m_TMPInputField;
        InputField m_InputField;

        bool m_InputFieldExists;

        string m_PlaceholderText;

        void Start()
        {
            if (isSearchField)
            {
                role = AccessibilityRole.SearchField;
            }
#if UNITY_6000_3_OR_NEWER
            else
            {
                role = AccessibilityRole.TextField;
            }
#endif // UNITY_6000_3_OR_NEWER

            m_TMPInputField = GetComponentInChildren<TMP_InputField>();
            m_InputField = GetComponentInChildren<InputField>();

            m_InputFieldExists = m_TMPInputField != null || m_InputField != null;

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
            base.BindToControl();

            if (m_TMPInputField != null)
            {
                m_TMPInputField.onValueChanged.AddListener(UpdateValue);
            }
            else if (m_InputField != null)
            {
                m_InputField.onValueChanged.AddListener(UpdateValue);
            }

            if ((Application.platform == RuntimePlatform.OSXPlayer ||
                    Application.platform == RuntimePlatform.WindowsPlayer) &&
                m_InputFieldExists)
            {
                focused += OnFocused;
            }
        }

        protected override void UnbindFromControl()
        {
            base.UnbindFromControl();

            if (m_TMPInputField != null)
            {
                m_TMPInputField.onValueChanged.RemoveListener(UpdateValue);
            }
            else if (m_InputField != null)
            {
                m_InputField.onValueChanged.RemoveListener(UpdateValue);
            }

            if ((Application.platform == RuntimePlatform.OSXPlayer ||
                    Application.platform == RuntimePlatform.WindowsPlayer) &&
                m_InputFieldExists)
            {
                focused -= OnFocused;
            }
        }

        void UpdateValue(string newValue)
        {
            value = string.IsNullOrEmpty(newValue) ? m_PlaceholderText : newValue;
            SetNodeProperties();
        }

        void OnFocused(bool isFocused)
        {
            if (isFocused)
            {
                if (m_TMPInputField != null && m_TMPInputField.IsActive() && m_TMPInputField.IsInteractable())
                {
                    m_TMPInputField.Select();
                }

                if (m_InputField != null && m_InputField.IsActive() && m_InputField.IsInteractable())
                {
                    m_InputField.Select();
                }
            }
            else if (!EventSystem.current.alreadySelecting)
            {
                EventSystem.current.SetSelectedGameObject(null);
            }
        }
    }
}
