using System;
using UnityEngine.Accessibility;
using UnityEngine.Scripting;
using UnityEngine.UIElements;

namespace Unity.Samples.ScreenReader
{
    public class VisualElementAccessibilityHandler
    {
        public const VersionChangeType k_AccessibilityChange = (VersionChangeType)0x10000000;
        private AccessibilityNode m_Node;
        private VisualElement m_OwnerElement;

        public int nextInsertionIndex;
        public VersionChangeType change { get; set; }
        public AccessibilityNode node
        {
            get => m_Node;
            set
            {
                if (m_Node == value)
                    return;
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
                    return;

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
                    //m_OwnerElement.RegisterCallback<PropertyChangedEvent>(OnPropertyChanged);
                    BindToElement(m_OwnerElement);
                }
            }
        }

        void OnGeometryChanged(GeometryChangedEvent e)
        {
            NotifyChange(VersionChangeType.Transform);
        }
        
       /* void OnPropertyChanged(PropertyChangedEvent e)
        {
            MarkAsDirty();
        }*/

        public bool isActive
        {
            get
            {
                if (m_OwnerElement != null)
                {
                    // if the node is disable or hidden then it is set as inactive regardless of
                    // AccessibilityProperties.isActive
                    if (IsElementVisible(m_OwnerElement))
                    {
                        var acc = m_OwnerElement.GetAccessibleProperties();

                        if (acc != null && acc.m_IsActive.defined)
                        {
                            return acc.active;
                        }

                        return true;
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
                    return true;

                var acc = m_OwnerElement.GetAccessibleProperties();

                if (acc != null)
                {
                    return acc.ignored;
                }
                return false;
            }
        }
        
        public bool isModal
        {
            get
            {
                if (m_OwnerElement == null)
                    return false;

                var acc = m_OwnerElement.GetAccessibleProperties();

                if (acc != null && acc.m_Modal.defined)
                {
                    return acc.modal;
                }

                return IsModal();
            }
        }

        public virtual bool IsModal()
        {
            if (ownerElement == null)
                return false;
            
            return ownerElement.ClassListContains(GenericDropdownMenu.ussClassName);
        }

        public string label
        {
            get
            {
                if (m_OwnerElement == null)
                    return null;

                var acc = m_OwnerElement.GetAccessibleProperties();

                if (acc != null && acc.m_Label.defined)
                {
                    return acc.label;
                }

                return GetLabel();
            }
        }

        public virtual string GetLabel() => "";

        public string value
        {
            get
            {
                if (m_OwnerElement == null)
                    return null;

                var acc = m_OwnerElement.GetAccessibleProperties();

                if (acc != null && acc.m_Value.defined)
                {
                    return acc.value;
                }

                return GetValue();
            }
        }

        public virtual string GetValue() => "";

        public string hint
        {
            get
            {
                if (m_OwnerElement == null)
                    return null;

                var acc = m_OwnerElement.GetAccessibleProperties();

                if (acc != null && acc.m_Hint.defined)
                {
                    return acc.hint;
                }

                return GetHint();
            }
        }

        public virtual string GetHint() => "";

        public AccessibilityRole role
        {
            get
            {
                if (m_OwnerElement == null)
                    return AccessibilityRole.None;

                var acc = m_OwnerElement.GetAccessibleProperties();

                if (acc != null && acc.m_Role.defined)
                {
                    return acc.role;
                }

                return GetRole();
            }
        }

        public virtual AccessibilityRole GetRole() => AccessibilityRole.None;

        public virtual  AccessibilityState state
        {
            get
            {
                if (m_OwnerElement is {enabledSelf: false})
                    return AccessibilityState.Disabled;
                return AccessibilityState.None;
            }
        }

        public VisualElementAccessibilityHandler()
        {
        }

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
                return;

            node.invoked -= OnNodeSelected;
            node.invoked += OnNodeSelected;
        }

        void DisconnectFromNodeSelected()
        {
            if (node == null)
                return;

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
            bool selected = false;

            if (m_OwnerElement.GetAccessibleProperties() != null)
            {
                selected = m_OwnerElement.GetAccessibleProperties().InvokeSelected();
            }

            return (m_OnSelect != null && m_OnSelect.Invoke()) || selected;
        }

        private event Func<bool> m_OnSelect;

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

        private event Action m_OnIncrement;

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
                return;

            node.incremented -= OnNodeIncremented;
            node.incremented += OnNodeIncremented;
        }

        void DisconnectFromNodeIncrement()
        {
            if (node == null)
                return;

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

        private event Action m_OnDecrement;

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
                return;

            node.decremented -= OnNodeDecremented;
            node.decremented += OnNodeDecremented;
        }

        void DisconnectFromNodeDecrement()
        {
            if (node == null)
                return;

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
                return false;

            if (element.resolvedStyle.display == DisplayStyle.None || element.resolvedStyle.opacity == 0)
                return false;

            if (float.IsNaN(element.resolvedStyle.width) ||
                element.resolvedStyle.width <= 0 ||
                float.IsNaN(element.resolvedStyle.height) ||
                element.resolvedStyle.height <= 0)
                return false;

            return true;
        }
    }
}