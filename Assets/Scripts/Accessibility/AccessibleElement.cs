using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Accessibility;

namespace Unity.Samples.Accessibility
{
    /// <summary>
    /// Base class GameObjects that we want to be picked up by the screen reader.
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

                DisconnectFromSelected();
                DisconnectFromDismissed();

                m_Node = value;

                ConnectToSelected();
                ConnectToDismissed();
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
                ConnectToSelected();
            }
            remove
            {
                m_Selected -= value;
                if (m_Selected == null)
                {
                    DisconnectFromSelected();
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
                ConnectToDismissed();
            }
            remove
            {
                m_Dismissed -= value;
                if (m_Dismissed == null)
                {
                    DisconnectFromDismissed();
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

        void ConnectToSelected()
        {
            // Do not connect if the node does not exist or selected is not yet implemented.
            if (node == null || m_Selected == null)
            {
                return;
            }

            node.selected -= InvokeSelected;
            node.selected += InvokeSelected;
        }

        void ConnectToDismissed()
        {
            // Do not connect if the node does not exist or dismissed is not yet implemented.
            if (node == null || m_Dismissed == null)
            {
                return;
            }

            node.dismissed -= InvokeDismissed;
            node.dismissed += InvokeDismissed;
        }

        void DisconnectFromSelected()
        {
            if (node == null)
            {
                return;
            }

            // Disconnect even if selected is implemented when the node is unset.
            node.selected -= InvokeSelected;
        }

        void DisconnectFromDismissed()
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

            if (Application.isEditor || Application.platform == RuntimePlatform.IPhonePlayer)
            {
                node.allowsDirectInteraction = allowsDirectInteraction;
            }

            nodePropertiesChanged?.Invoke();
        }

        public Rect GetFrame()
        {
            return getFrame?.Invoke() ?? AccessibilityManager.GetFrame(transform as RectTransform);
        }
    }
}
