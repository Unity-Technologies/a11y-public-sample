using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Accessibility;

namespace Unity.Samples.ScreenReader
{
    /// <summary>
    /// Component attached to the game objects that should be picked up by the screen reader.
    /// </summary>
    [AddComponentMenu("Accessibility/Accessible Element")]
    [ExecuteAlways]
    public class AccessibleElement : MonoBehaviour
    {
        public bool isActive = true;
        public string label;
        public string value;
        public string hint;
        public AccessibilityRole role;
        public bool allowsDirectInteraction;
        public AccessibilityState state;
        public Func<Rect> getFrame;

        AccessibilityNode m_Node;
        public AccessibilityNode node
        {
            get => m_Node;
            set
            {
                if (m_Node == value)
                {
                    return;
                }

                DisconnectNodeFromSelected();
                DisconnectNodeFromDismissed();

                m_Node = value;

                ConnectNodeToSelected();
                ConnectNodeToDismissed();
            }
        }

        public event Action nodePropertiesChanged;
        public event Action focusChanged;

        event Func<bool> m_Selected;
        public event Func<bool> selected
        {
            add
            {
                m_Selected += value;
                ConnectNodeToSelected();
            }
            remove
            {
                m_Selected -= value;
                if (m_Selected == null)
                {
                    DisconnectNodeFromSelected();
                }
            }
        }

        public event Action incremented;
        public event Action decremented;

        event Func<bool> m_Dismissed;
        public event Func<bool> dismissed
        {
            add
            {
                m_Dismissed += value;
                ConnectNodeToDismissed();
            }
            remove
            {
                m_Dismissed -= value;
                if (m_Dismissed == null)
                {
                    DisconnectNodeFromDismissed();
                }
            }
        }

        void OnEnable()
        {
            if (node != null)
            {
                node.isActive = isActive && gameObject.activeInHierarchy;
            }

            StartCoroutine(DelayBindToControl());
        }

        void OnDisable()
        {
            if (node != null)
            {
                node.isActive = isActive && gameObject.activeInHierarchy;
            }

            UnbindFromControl();
        }

        protected virtual void BindToControl()
        {
        }

        IEnumerator DelayBindToControl()
        {
            yield return new WaitForEndOfFrame();

            BindToControl();
        }

        protected virtual void UnbindFromControl()
        {
        }

        void ConnectNodeToSelected()
        {
            // Implementing the selected event tells the screen reader that the node is selectable, which may lead to
            // a specific behaviour. Therefore, we don't implement the node's selected event unless we actually need it.
            if (node == null || m_Selected == null)
            {
                return;
            }

            node.invoked -= InvokeSelected;
            node.invoked += InvokeSelected;
        }

        void ConnectNodeToDismissed()
        {
            // Implementing the dismissed event tells the screen reader that the node is dismissible, which may lead to
            // a specific behaviour. Therefore, we don't implement the node's dismissed event unless we actually need
            // it.
            if (node == null || m_Dismissed == null)
            {
                return;
            }

            node.dismissed -= InvokeDismissed;
            node.dismissed += InvokeDismissed;
        }

        void DisconnectNodeFromSelected()
        {
            if (node == null)
            {
                return;
            }

            // Disconnect even if selected is implemented when the node is unset.
            node.invoked -= InvokeSelected;
        }

        void DisconnectNodeFromDismissed()
        {
            if (node == null)
            {
                return;
            }

            // Disconnect even if dismissed is implemented when the node is unset.
            node.dismissed -= InvokeDismissed;
        }

        internal void InvokeFocusChanged(AccessibilityNode accessibilityNode, bool isFocused)
        {
            if (isFocused)
            {
                focusChanged?.Invoke();
            }
        }

        bool InvokeSelected()
        {
            var success = m_Selected != null && m_Selected.Invoke();

            node.value = value;
            node.state = state;

            return success;
        }

        internal void InvokeIncremented()
        {
            incremented?.Invoke();
        }

        internal void InvokeDecremented()
        {
            decremented?.Invoke();
        }

        bool InvokeDismissed()
        {
            return m_Dismissed != null && m_Dismissed.Invoke();
        }

        public void SetNodeProperties()
        {
            if (node == null)
            {
                return;
            }

            node.isActive = isActive && gameObject.activeInHierarchy;
            node.label = label;
            node.value = value;
            node.hint = hint;
            node.role = role;
            node.state = state;

            // AccessibilityNode.allowsDirectInteraction is not supported on Android.
            if (Application.isEditor || Application.platform == RuntimePlatform.IPhonePlayer)
            {
                node.allowsDirectInteraction = allowsDirectInteraction;
            }

            nodePropertiesChanged?.Invoke();
        }

        public Rect GetFrame()
        {
            return getFrame?.Invoke() ?? UGuiAccessibilityService.GetFrame(transform as RectTransform);
        }
    }
}
