using UnityEngine.Accessibility;
using UnityEngine.UIElements;

namespace Unity.Samples.ScreenReader
{
    // TabView
    class TabHandler : VisualElementAccessibilityHandler
    {
        TabView m_TabView;
        Tab m_Tab;

        protected override void BindToElement(VisualElement ve)
        {
            m_TabView = ownerElement.GetFirstOfType<TabView>();
            m_Tab = m_TabView[ownerElement.parent.IndexOf(ownerElement)] as Tab;
            // field.RegisterValueChangedCallback(OnValueChanged);
        }

        protected override void UnbindFromElement(VisualElement ve)
        {
            m_TabView = null;
            m_Tab = null;
        }

        public override string GetLabel()
        {
            return m_Tab.label;
        }

#if UNITY_6000_3_OR_NEWER
        public override AccessibilityRole GetRole() => AccessibilityRole.TabButton;
#endif // UNITY_6000_3_OR_NEWER
    }

    class TabHandlerCreator : VisualElementAccessibilityHandlerFactory.ICreator
    {
        public bool CanCreate(VisualElement element)
        {
            return element.ClassListContains(Tab.tabHeaderUssClassName);
        }

        public VisualElementAccessibilityHandler Create(VisualElement element)
        {
            return new TabHandler();
        }
    }
}
