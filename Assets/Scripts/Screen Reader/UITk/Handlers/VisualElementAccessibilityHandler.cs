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
                    // If the node is disabled or hidden then it is set as inactive regardless of
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

        public virtual AccessibilityState state => m_OwnerElement is {enabledSelf: false} ?
            AccessibilityState.Disabled : AccessibilityState.None;

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

        void ConnectToNode()
        {
            ConnectToNodeSelected();
            ConnectToNodeIncrement();
            ConnectToNodeDecrement();
        }

        void DisconnectFromNode()
        {
            DisconnectFromNodeDecrement();
            DisconnectFromNodeDecrement();
            DisconnectFromNodeSelected();
        }

        void ConnectToNodeSelected()
        {
            // Do not connect if the node does not exist or OnSelect is not yet implemented.
            if (node == null || m_OnSelect == null)
            {
                return;
            }

            node.invoked -= OnNodeSelected;
            node.invoked += OnNodeSelected;
        }

        void DisconnectFromNodeSelected()
        {
            if (node == null)
            {
                return;
            }

            // Disconnect even if OnSelect is implemented when the node is unset.
            node.invoked -= OnNodeSelected;
        }

        bool OnNodeSelected()
        {
            var sel = InvokeOnSelect();

            node.value = value;
            node.state = state;

            return sel;
        }

        internal void InvokeOnFocus(AccessibilityNode node, bool isFocused)
        {
            if (isFocused)
            {
                OnFocused?.Invoke();
            }
        }

        public event Action OnFocused;

        internal bool InvokeOnSelect()
        {
            var selected = false;

            if (m_OwnerElement.GetAccessibleProperties() != null)
            {
                selected = m_OwnerElement.GetAccessibleProperties().InvokeSelected();
            }

            return (m_OnSelect != null && m_OnSelect.Invoke()) || selected;
        }

        event Func<bool> m_OnSelect;

        public event Func<bool> OnSelect
        {
            add
            {
                m_OnSelect += value;
                ConnectToNodeSelected();
            }
            remove
            {
                m_OnSelect -= value;

                if (m_OnSelect == null)
                {
                    DisconnectFromNodeSelected();
                }
            }
        }

        event Action m_OnIncrement;

        public event Action OnIncrement
        {
            add
            {
                m_OnIncrement += value;
                ConnectToNodeIncrement();
            }
            remove
            {
                m_OnIncrement -= value;

                if (m_OnIncrement == null)
                {
                    DisconnectFromNodeIncrement();
                }
            }
        }

        void ConnectToNodeIncrement()
        {
            // Do not connect if the node does not exist or OnNodeIncremented is not yet implemented.
            if (node == null || m_OnIncrement == null)
            {
                return;
            }

            node.incremented -= OnNodeIncremented;
            node.incremented += OnNodeIncremented;
        }

        void DisconnectFromNodeIncrement()
        {
            if (node == null)
            {
                return;
            }

            // Disconnect even if OnNodeIncremented is implemented when the node is unset.
            node.incremented -= OnNodeIncremented;
        }

        internal void InvokeOnIncrement()
        {
            m_OwnerElement?.GetAccessibleProperties()?.InvokeIncremented();
            m_OnIncrement?.Invoke();
        }

        void OnNodeIncremented()
        {
            InvokeOnIncrement();

            node.value = value;
            node.state = state;
        }

        event Action m_OnDecrement;

        public event Action OnDecrement
        {
            add
            {
                m_OnDecrement += value;
                ConnectToNodeDecrement();
            }
            remove
            {
                m_OnDecrement -= value;

                if (m_OnDecrement == null)
                {
                    DisconnectFromNodeDecrement();
                }
            }
        }

        void ConnectToNodeDecrement()
        {
            // Do not connect if the node does not exist or OnNodeDecremented is not yet implemented.
            if (node == null || m_OnDecrement == null)
            {
                return;
            }

            node.decremented -= OnNodeDecremented;
            node.decremented += OnNodeDecremented;
        }

        void DisconnectFromNodeDecrement()
        {
            if (node == null)
            {
                return;
            }

            // Disconnect even if OnNodeDecremented is implemented when the node is unset.
            node.decremented -= OnNodeDecremented;
        }


        internal void InvokeOnDecrement()
        {
            m_OwnerElement?.GetAccessibleProperties()?.InvokeDecremented();
            m_OnDecrement?.Invoke();
        }

        void OnNodeDecremented()
        {
            InvokeOnDecrement();

            node.value = value;
            node.state = state;
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
