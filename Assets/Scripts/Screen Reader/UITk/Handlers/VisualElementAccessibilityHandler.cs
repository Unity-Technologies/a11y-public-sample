using System;
using UnityEngine.Accessibility;
using UnityEngine.UIElements;

namespace Unity.Samples.ScreenReader
{
    public class VisualElementAccessibilityHandler
    {
        public const VersionChangeType k_AccessibilityChange = (VersionChangeType)0x10000000;
        AccessibilityNode m_Node;
        VisualElement m_OwnerElement;

        public int nextInsertionIndex;
        public VersionChangeType change { get; set; }

        public AccessibilityNode node
        {
            get => m_Node;
            set
            {
                if (m_Node == value)
                {
                    return;
                }

                DisconnectFromNode();
                m_Node = value;
                ConnectToNode();
            }
        }

        public VisualElement ownerElement
        {
            get => m_OwnerElement;
            internal set
            {
                if (m_OwnerElement == value)
                {
                    return;
                }

                if (m_OwnerElement != null)
                {
                    UnbindFromElement(m_OwnerElement);
                    m_OwnerElement.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
                   // m_OwnerElement.UnregisterCallback<PropertyChangedEvent>(OnPropertyChanged);
                }

                m_OwnerElement = value;

                if (m_OwnerElement != null)
                {
                    m_OwnerElement.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
                    // m_OwnerElement.RegisterCallback<PropertyChangedEvent>(OnPropertyChanged);
                    BindToElement(m_OwnerElement);
                }
            }
        }

        void OnGeometryChanged(GeometryChangedEvent e)
        {
            NotifyChange(VersionChangeType.Transform);
        }

        /*void OnPropertyChanged(PropertyChangedEvent e)
        {
            MarkAsDirty();
        }*/

        public bool isActive
        {
            get
            {
                if (m_OwnerElement != null)
                {
                    // If the node is disabled or hidden, then it is set as inactive regardless of
                    // AccessibilityProperties.isActive.
                    if (IsElementVisible(m_OwnerElement))
                    {
                        var acc = m_OwnerElement.GetAccessibleProperties();

                        return acc is not { m_IsActive: { defined: true } } || acc.active;
                    }
                }

                return false;
            }
        }

        public bool isIgnored
        {
            get
            {
                if (m_OwnerElement == null)
                {
                    return true;
                }

                if (m_OwnerElement.parent.parent is Toggle)
                {
                    return true;
                }

                var acc = m_OwnerElement.GetAccessibleProperties();

                return acc is { ignored: true };
            }
        }

        public bool isModal
        {
            get
            {
                if (m_OwnerElement == null)
                {
                    return false;
                }

                var acc = m_OwnerElement.GetAccessibleProperties();

                return acc is { m_Modal: { defined: true } } ? acc.modal : IsModal();
            }
        }

        public virtual bool IsModal()
        {
            return ownerElement != null && ownerElement.ClassListContains(GenericDropdownMenu.ussClassName);
        }

        public string label
        {
            get
            {
                if (m_OwnerElement == null)
                {
                    return null;
                }

                var acc = m_OwnerElement.GetAccessibleProperties();

                return acc is { m_Label: { defined: true } } ? acc.label : GetLabel();
            }
        }

        public virtual string GetLabel() => "";

        public string value
        {
            get
            {
                if (m_OwnerElement == null)
                {
                    return null;
                }

                var acc = m_OwnerElement.GetAccessibleProperties();

                return acc is { m_Value: { defined: true } } ? acc.value : GetValue();
            }
        }

        public virtual string GetValue() => "";

        public string hint
        {
            get
            {
                if (m_OwnerElement == null)
                {
                    return null;
                }

                var acc = m_OwnerElement.GetAccessibleProperties();

                return acc is { m_Hint: { defined: true } } ? acc.hint : GetHint();
            }
        }

        public virtual string GetHint() => "";

        public AccessibilityRole role
        {
            get
            {
                if (m_OwnerElement == null)
                {
                    return AccessibilityRole.None;
                }

                var acc = m_OwnerElement.GetAccessibleProperties();

                return acc is { m_Role: { defined: true } } ? acc.role : GetRole();
            }
        }

        public virtual AccessibilityRole GetRole() => AccessibilityRole.None;

        public AccessibilityState state
        {
            get
            {
                if (m_OwnerElement == null)
                {
                    return AccessibilityState.None;
                }

                var acc = m_OwnerElement.GetAccessibleProperties();

                var states = acc is { m_State: { defined: true } } ? acc.state : GetState();

                if (m_OwnerElement is { enabledSelf: false })
                {
                    states |= AccessibilityState.Disabled;
                }

                return states;
            }
        }

        public virtual AccessibilityState GetState() => AccessibilityState.None;

        public Action<VisualElement, VersionChangeType> onVersionChanged;

        public void NotifyChange(VersionChangeType changeType = k_AccessibilityChange)
        {
            onVersionChanged?.Invoke(ownerElement, changeType);
        }

        protected virtual void BindToElement(VisualElement ve)
        {
        }

        protected virtual void UnbindFromElement(VisualElement ve)
        {
        }

        public event Action<bool> focused;

        event Func<bool> m_Selected;
        public event Func<bool> selected
        {
            add
            {
                m_Selected += value;
                ConnectToSelected();
            }
            remove
            {
                m_Selected -= value;

                if (!m_Selectable)
                {
                    DisconnectFromSelected();
                }
            }
        }

        public event Action incremented;
        public event Action decremented;

        event Func<AccessibilityScrollDirection, bool> m_Scrolled;
        public event Func<AccessibilityScrollDirection, bool> scrolled
        {
            add
            {
                m_Scrolled += value;
                ConnectToScrolled();
            }
            remove
            {
                m_Scrolled -= value;

                if (!m_Scrollable)
                {
                    DisconnectFromScrolled();
                }
            }
        }

        event Func<bool> m_Dismissed;
        public event Func<bool> dismissed
        {
            add
            {
                m_Dismissed += value;
                ConnectToDismissed();
            }
            remove
            {
                m_Dismissed -= value;

                if (!m_Dismissable)
                {
                    DisconnectFromDismissed();
                }
            }
        }

        bool m_Selectable
        {
            get
            {
                var selectable = m_Selected != null;
                var acc = m_OwnerElement?.GetAccessibleProperties();

                if (acc != null)
                {
                    selectable |= acc.selectable;
                }

                return selectable;
            }
        }

        bool m_Scrollable
        {
            get
            {
                var scrollable = m_Scrolled != null;
                var acc = m_OwnerElement?.GetAccessibleProperties();

                if (acc != null)
                {
                    scrollable |= acc.scrollable;
                }

                return scrollable;
            }
        }

        bool m_Dismissable
        {
            get
            {
                var dismissable = m_Dismissed != null;
                var acc = m_OwnerElement?.GetAccessibleProperties();

                if (acc != null)
                {
                    dismissable |= acc.dismissable;
                }

                return dismissable;
            }
        }

        void ConnectToNode()
        {
            ConnectToFocusChanged();
            ConnectToSelected();
            ConnectToIncremented();
            ConnectToDecremented();
            ConnectToScrolled();
            ConnectToDismissed();
        }

        void DisconnectFromNode()
        {
            DisconnectFromFocusChanged();
            DisconnectFromSelected();
            DisconnectFromIncremented();
            DisconnectFromDecremented();
            DisconnectFromScrolled();
            DisconnectFromDismissed();
        }

        void ConnectToFocusChanged()
        {
            if (node == null)
            {
                return;
            }

            node.focusChanged -= InvokeFocused;
            node.focusChanged += InvokeFocused;
        }

        void DisconnectFromFocusChanged()
        {
            if (node == null)
            {
                return;
            }

            node.focusChanged -= InvokeFocused;
        }

        void InvokeFocused(AccessibilityNode accessibilityNode, bool isFocused)
        {
            m_OwnerElement?.GetAccessibleProperties()?.InvokeFocused(accessibilityNode, isFocused);
            focused?.Invoke(isFocused);
        }

        void ConnectToSelected()
        {
            // Implementing the selected event tells the screen reader that the node is selectable, which may lead to
            // a specific behaviour. Therefore, we don't implement the node's selected event unless we actually need it.
            if (node == null || !m_Selectable)
            {
                return;
            }

#if UNITY_6000_3_OR_NEWER
            node.invoked -= InvokeSelected;
            node.invoked += InvokeSelected;
#else // UNITY_6000_3_OR_NEWER
            node.selected -= InvokeSelected;
            node.selected += InvokeSelected;
#endif // UNITY_6000_3_OR_NEWER
        }

        void DisconnectFromSelected()
        {
            if (node == null)
            {
                return;
            }

#if UNITY_6000_3_OR_NEWER
            node.invoked -= InvokeSelected;
#else // UNITY_6000_3_OR_NEWER
            node.selected -= InvokeSelected;
#endif // UNITY_6000_3_OR_NEWER
        }

        bool InvokeSelected()
        {
            var success = false;

            if (m_OwnerElement.GetAccessibleProperties() != null)
            {
                success = m_OwnerElement.GetAccessibleProperties().InvokeSelected();
            }

            success |= m_Selected?.Invoke() ?? false;

            return success;
        }

        void ConnectToIncremented()
        {
            if (node == null)
            {
                return;
            }

            node.incremented -= InvokeIncremented;
            node.incremented += InvokeIncremented;
        }

        void DisconnectFromIncremented()
        {
            if (node == null)
            {
                return;
            }

            node.incremented -= InvokeIncremented;
        }

        void InvokeIncremented()
        {
            m_OwnerElement?.GetAccessibleProperties()?.InvokeIncremented();
            incremented?.Invoke();
        }

        void ConnectToDecremented()
        {
            if (node == null)
            {
                return;
            }

            node.decremented -= InvokeDecremented;
            node.decremented += InvokeDecremented;
        }

        void DisconnectFromDecremented()
        {
            if (node == null)
            {
                return;
            }

            node.decremented -= InvokeDecremented;
        }

        void InvokeDecremented()
        {
            m_OwnerElement?.GetAccessibleProperties()?.InvokeDecremented();
            decremented?.Invoke();
        }

        void ConnectToScrolled()
        {
            if (node == null || !m_Scrollable)
            {
                return;
            }


#if UNITY_6000_3_OR_NEWER
            node.scrolled -= InvokeScrolled;
            node.scrolled += InvokeScrolled;
#endif // UNITY_6000_3_OR_NEWER
        }

        void DisconnectFromScrolled()
        {
            if (node == null)
            {
                return;
            }

#if UNITY_6000_3_OR_NEWER
            node.scrolled -= InvokeScrolled;
#endif // UNITY_6000_3_OR_NEWER
        }

        bool InvokeScrolled(AccessibilityScrollDirection direction)
        {
            var success = false;

            if (m_OwnerElement.GetAccessibleProperties() != null)
            {
                success = m_OwnerElement.GetAccessibleProperties().InvokeScrolled(direction);
            }

            success |= m_Scrolled?.Invoke(direction) ?? false;

            return success;
        }

        void ConnectToDismissed()
        {
            // Implementing the dismissed event tells the screen reader that the node is dismissible, which may lead to
            // a specific behaviour. Therefore, we don't implement the node's dismissed event unless we actually need
            // it.
            if (node == null || !m_Dismissable)
            {
                return;
            }

#if UNITY_2023_3_OR_NEWER
            node.dismissed -= InvokeDismissed;
            node.dismissed += InvokeDismissed;
#endif // UNITY_2023_3_OR_NEWER
        }

        void DisconnectFromDismissed()
        {
            if (node == null)
            {
                return;
            }

#if UNITY_2023_3_OR_NEWER
            node.dismissed -= InvokeDismissed;
#endif // UNITY_2023_3_OR_NEWER
        }

        bool InvokeDismissed()
        {
            var success = false;

            if (m_OwnerElement.GetAccessibleProperties() != null)
            {
                success = m_OwnerElement.GetAccessibleProperties().InvokeDismissed();
            }

            success |= m_Dismissed?.Invoke() ?? false;

            return success;
        }

        public static bool IsElementVisible(VisualElement element)
        {
            if (!element.enabledSelf || !element.visible)
            {
                return false;
            }

            if (element.resolvedStyle.display == DisplayStyle.None || element.resolvedStyle.opacity == 0)
            {
                return false;
            }

            return !float.IsNaN(element.resolvedStyle.width) &&
                !(element.resolvedStyle.width <= 0) &&
                !float.IsNaN(element.resolvedStyle.height) &&
                !(element.resolvedStyle.height <= 0);
        }
    }
}
