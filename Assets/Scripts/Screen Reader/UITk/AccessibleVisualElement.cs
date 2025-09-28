using Unity.Properties;
using UnityEngine.UIElements;

namespace Unity.Samples.ScreenReader
{
    [UxmlElement]
    public partial class AccessibleVisualElement : VisualElement
    {
        public const string ussClassName = "unity-accessible-overrides-element";

        AccessibleProperties m_Accessible;

        [UxmlObjectReference("accessible"), CreateProperty]
        public AccessibleProperties accessible
        {
            get => m_Accessible;
            set
            {
                if (m_Accessible == value)
                {
                    return;
                }

                if (m_Accessible != null)
                {
                    m_Accessible.owner = null;
                }

                m_Accessible = value;

                if (m_Accessible != null)
                {
                    m_Accessible.owner = this;
                }
            }
        }

        public AccessibleVisualElement()
        {
            AddToClassList(ussClassName);
            accessible = new AccessibleProperties();

            // RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            // RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }

        void OnAttachToPanel(AttachToPanelEvent evt)
        {
            var updater = evt.destinationPanel.GetAccessibilityUpdater();
            updater?.OnVersionChanged(parent ?? this, VersionChangeType.Hierarchy);
        }

        void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            var updater = evt.originPanel?.GetAccessibilityUpdater();
            updater?.OnVersionChanged(null, VersionChangeType.Hierarchy);
        }
    }
}
