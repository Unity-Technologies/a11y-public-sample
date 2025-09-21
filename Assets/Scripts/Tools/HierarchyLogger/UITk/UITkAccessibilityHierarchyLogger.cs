using UnityEngine.UIElements;

namespace Unity.Samples.LetterSpell
{
    public class UITkAccessibilityHierarchyLogger : AccessibilityHierarchyLogger
    {
        private Label m_LogLabel;
        
        protected override void WriteLog(string log)
        {
            if (m_LogLabel == null)
            {
                var uiDocument = GetComponent<UIDocument>();
                
                if (uiDocument == null)
                    return;
                
                m_LogLabel = uiDocument.rootVisualElement.Q<Label>("accessibilityHierarchyLogLabel");

                if (m_LogLabel == null)
                {
                    m_LogLabel = new Label() { name = "accessibilityHierarchyLogLabel", style = { position = Position.Absolute } };
                    uiDocument.rootVisualElement.Add(m_LogLabel);
                }
            }
            
            m_LogLabel.text = log;
        }
    }
}