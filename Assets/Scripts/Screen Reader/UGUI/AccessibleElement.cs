using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Accessibility;
using UnityEngine.UI;

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
        public Func<Rect> frameGetter;

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

                DisconnectFromNode();
                m_Node = value;
                SetNodeProperties();
                ConnectToNode();
            }
        }

        ScrollRect m_ScrollView;
        const float k_ScrollStep = 0.25f;
        const float k_ScrollDuration = 0.2f;

        public event Action nodePropertiesChanged;
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

                if (m_Selected == null)
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

                if (m_Scrolled == null)
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

                if (m_Dismissed == null)
                {
                    DisconnectFromDismissed();
                }
            }
        }

        void Awake()
        {
            frameGetter = () => UGuiAccessibilityManager.GetFrame(gameObject.transform as RectTransform);
        }

        void OnEnable()
        {
            StartCoroutine(DelayBindToControl());
        }

        void OnDisable()
        {
            UnbindFromControl();
        }

        protected virtual void BindToControl()
        {
            if (node != null)
            {
                node.isActive = isActive && gameObject.activeInHierarchy;
            }

#if UNITY_6000_3_OR_NEWER
            if (IsInsideScrollView(out m_ScrollView))
            {
                scrolled += OnScrolled;
            }
#endif // UNITY_6000_3_OR_NEWER
        }

        IEnumerator DelayBindToControl()
        {
            yield return new WaitForEndOfFrame();

            BindToControl();
        }

        protected virtual void UnbindFromControl()
        {
            if (node != null)
            {
                node.isActive = isActive && gameObject.activeInHierarchy;
            }

#if UNITY_6000_3_OR_NEWER
            if (IsInsideScrollView(out _))
            {
                scrolled -= OnScrolled;
            }
#endif // UNITY_6000_3_OR_NEWER
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
            focused?.Invoke(isFocused);
        }

        void ConnectToSelected()
        {
            // Implementing the selected event tells the screen reader that the node is selectable, which may lead to
            // a specific behaviour. Therefore, we don't implement the node's selected event unless we actually need it.
            if (node == null || m_Selected == null)
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
            return m_Selected?.Invoke() ?? false;
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
            decremented?.Invoke();
        }

        void ConnectToScrolled()
        {
            if (node == null || m_Scrolled == null)
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
            return m_Scrolled?.Invoke(direction) ?? false;
        }

        void ConnectToDismissed()
        {
            // Implementing the dismissed event tells the screen reader that the node is dismissible, which may lead to
            // a specific behaviour. Therefore, we don't implement the node's dismissed event unless we actually need
            // it.
            if (node == null || m_Dismissed == null)
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
            return m_Dismissed?.Invoke() ?? false;
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
            node.frameGetter = frameGetter;

#if UNITY_6000_3_OR_NEWER
            node.allowsDirectInteraction = allowsDirectInteraction;
#else // UNITY_6000_3_OR_NEWER
            // AccessibilityNode.allowsDirectInteraction is not supported on Android.
            if (Application.isEditor || Application.platform == RuntimePlatform.IPhonePlayer)
            {
                node.allowsDirectInteraction = allowsDirectInteraction;
            }
#endif // UNITY_6000_3_OR_NEWER

            nodePropertiesChanged?.Invoke();
        }

        bool IsInsideScrollView(out ScrollRect scrollView)
        {
            scrollView = null;

            var currentTransform = transform.parent;

            // Traverse up the parent hierarchy.
            while (currentTransform != null)
            {
                // Check if the current parent has a ScrollRect component.
                var scrollRect = currentTransform.GetComponent<ScrollRect>();
                if (scrollRect != null)
                {
                    scrollView = scrollRect;
                    return true; // Found a scroll view.
                }

                // Move to the next parent.
                currentTransform = currentTransform.parent;
            }

            return false; // No scroll view found in the hierarchy.
        }

        public bool IsInsideScrollView(ScrollRect scrollView)
        {
            var currentTransform = transform.parent;

            // Traverse up the parent hierarchy.
            while (currentTransform != null)
            {
                // Check if the current parent has the specified ScrollRect component.
                if (currentTransform.GetComponent<ScrollRect>() == scrollView)
                {
                    return true; // Found the scroll view.
                }

                // Move to the next parent.
                currentTransform = currentTransform.parent;
            }

            return false; // The scroll view was not found in the hierarchy.
        }

        bool IsVisibleInScrollView()
        {
            if (m_ScrollView == null)
            {
                throw new InvalidOperationException($"The node {node} is not inside a scroll view.");
            }

            Canvas.ForceUpdateCanvases();

            var viewportRect = m_ScrollView.viewport.rect;

            // Calculate the bounds of the element and the viewport.
            var elementBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(
                m_ScrollView.viewport, transform as RectTransform);
            var viewportBounds = new Bounds(viewportRect.center, viewportRect.size);

            const float minimumVisibleDistance = 20f;

            return elementBounds.min.x < viewportBounds.max.x - minimumVisibleDistance &&
                   elementBounds.max.x > viewportBounds.min.x + minimumVisibleDistance &&
                   elementBounds.min.y < viewportBounds.max.y - minimumVisibleDistance &&
                   elementBounds.max.y > viewportBounds.min.y + minimumVisibleDistance;
        }

        public bool IsFullyVisibleInScrollView(ScrollRect scrollView, out Vector2 offset)
        {
            if (scrollView == null)
            {
                throw new InvalidOperationException($"The node {node} is not inside a scroll view.");
            }

            Canvas.ForceUpdateCanvases();

            var viewportRect = scrollView.viewport.rect;

            // Calculate the bounds of the element and the viewport.
            var elementBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(
                scrollView.viewport, transform as RectTransform);
            var viewportBounds = new Bounds(viewportRect.center, viewportRect.size);

            // Calculate the offset required to make the element visible.
            offset = Vector2.zero;

            // Check and calculate the horizontal offset.
            if (elementBounds.max.x > viewportBounds.max.x)
            {
                offset.x = elementBounds.max.x - viewportBounds.max.x;
            }
            else if (elementBounds.min.x < viewportBounds.min.x)
            {
                offset.x = elementBounds.min.x - viewportBounds.min.x;
            }

            // Check and calculate the vertical offset.
            if (elementBounds.max.y > viewportBounds.max.y)
            {
                offset.y = elementBounds.max.y - viewportBounds.max.y;
            }
            else if (elementBounds.min.y < viewportBounds.min.y)
            {
                offset.y = elementBounds.min.y - viewportBounds.min.y;
            }

            const float floatTolerance = 0.1f;
            return Mathf.Abs(offset.x) < floatTolerance && Mathf.Abs(offset.y) < floatTolerance;
        }

#if UNITY_6000_3_OR_NEWER
        bool OnScrolled(AccessibilityScrollDirection direction)
        {
            var viewportSize = m_ScrollView.viewport.rect.size;
            var contentSize = m_ScrollView.content.rect.size;

            if (m_ScrollView.horizontal && viewportSize.x >= contentSize.x)
            {
                return false;
            }

            if (m_ScrollView.vertical && viewportSize.y >= contentSize.y)
            {
                return false;
            }

            const float floatTolerance = 0.05f;

            switch (direction)
            {
                case AccessibilityScrollDirection.Forward:
                {
                    if (m_ScrollView.horizontal)
                    {
                        if (1 - m_ScrollView.horizontalNormalizedPosition < floatTolerance)
                        {
                            return false;
                        }

                        var targetPosition = Mathf.Clamp01(m_ScrollView.horizontalNormalizedPosition + k_ScrollStep);
                        StartCoroutine(SmoothHorizontalScroll(targetPosition, direction));
                    }
                    else
                    {
                        if (m_ScrollView.verticalNormalizedPosition < floatTolerance)
                        {
                            return false;
                        }

                        var targetPosition = Mathf.Clamp01(m_ScrollView.verticalNormalizedPosition - k_ScrollStep);
                        StartCoroutine(SmoothVerticalScroll(targetPosition, direction));
                    }

                    break;
                }
                case AccessibilityScrollDirection.Backward:
                {
                    if (m_ScrollView.horizontal)
                    {
                        if (m_ScrollView.horizontalNormalizedPosition < floatTolerance)
                        {
                            return false;
                        }

                        var targetPosition = Mathf.Clamp01(m_ScrollView.horizontalNormalizedPosition - k_ScrollStep);
                        StartCoroutine(SmoothHorizontalScroll(targetPosition, direction));
                    }
                    else
                    {
                        if (1 - m_ScrollView.verticalNormalizedPosition < floatTolerance)
                        {
                            return false;
                        }

                        var targetPosition = Mathf.Clamp01(m_ScrollView.verticalNormalizedPosition + k_ScrollStep);
                        StartCoroutine(SmoothVerticalScroll(targetPosition, direction));
                    }

                    break;
                }
            }

            return true;
        }

        IEnumerator SmoothHorizontalScroll(float targetPosition, AccessibilityScrollDirection direction)
        {
            var time = 0f;
            var startPosition = m_ScrollView.horizontalNormalizedPosition;

            while (time < k_ScrollDuration)
            {
                time += Time.deltaTime;
                m_ScrollView.horizontalNormalizedPosition = Mathf.Lerp(
                    startPosition, targetPosition, time / k_ScrollDuration);
                yield return null;
            }

            m_ScrollView.horizontalNormalizedPosition = targetPosition;

            AssistiveSupport.activeHierarchy?.RefreshNodeFrames();

            FocusOnFirstVisibleSiblingAfterScrolling(direction);
        }

        IEnumerator SmoothVerticalScroll(float targetPosition, AccessibilityScrollDirection direction)
        {
            var time = 0f;
            var startPosition = m_ScrollView.verticalNormalizedPosition;

            while (time < k_ScrollDuration)
            {
                time += Time.deltaTime;
                m_ScrollView.verticalNormalizedPosition = Mathf.Lerp(
                    startPosition, targetPosition, time / k_ScrollDuration);
                yield return null;
            }

            m_ScrollView.verticalNormalizedPosition = targetPosition;

            AssistiveSupport.activeHierarchy?.RefreshNodeFrames();

            FocusOnFirstVisibleSiblingAfterScrolling(direction);
        }

        void FocusOnFirstVisibleSiblingAfterScrolling(AccessibilityScrollDirection direction)
        {
            if (direction == AccessibilityScrollDirection.Unknown)
            {
                return;
            }

            var nodeToFocus = GetFirstVisibleSiblingAfterScrolling(direction);

            if (nodeToFocus == null)
            {
                return;
            }

            // On iOS, the screen reader automatically moves the accessibility focus to another node after scrolling.
            // If that node is not fully visible within the scroll view, the AccessibleScrollView will automatically
            // scroll to make it fully visible. We don't want to mix the automatic scrolling with the manual scrolling
            // the user just did, so we pause the automatic scrolling until the node we've chosen to focus receives
            // accessibility focus.
            if (Application.platform == RuntimePlatform.IPhonePlayer)
            {
                var accessibleScrollView = m_ScrollView.GetComponentInParent<AccessibleScrollView>();

                if (accessibleScrollView != null)
                {
                    accessibleScrollView.PauseAutomaticScrolling(nodeToFocus);
                }
            }

            var scrollPosition = m_ScrollView.horizontal ?
                Mathf.RoundToInt(m_ScrollView.horizontalNormalizedPosition * 100) :
                Mathf.RoundToInt((1 - m_ScrollView.verticalNormalizedPosition) * 100);

            AssistiveSupport.notificationDispatcher.SendPageScrolledAnnouncement(
                $"Scrolled to {scrollPosition}% of the content", nodeToFocus);
        }

        AccessibilityNode GetFirstVisibleSiblingAfterScrolling(AccessibilityScrollDirection direction)
        {
            // If this node is fully visible in the scroll view, return it.
            if (IsFullyVisibleInScrollView(m_ScrollView, out _))
            {
                return node;
            }

            if (direction == AccessibilityScrollDirection.Unknown)
            {
                return null;
            }

            var siblings = node.parent != null ?
                node.parent.children :
                AssistiveSupport.activeHierarchy.rootNodes;

            var siblingIndex = IndexOf(node, siblings);
            if (siblingIndex == -1)
            {
                throw new IndexOutOfRangeException($"The node {node} was not found.");
            }

            var activeSiblingIndexes = new List<int>();

            switch (direction)
            {
                case AccessibilityScrollDirection.Forward:
                {
                    while (siblingIndex < siblings.Count - 1)
                    {
                        // Go to the next sibling.
                        var sibling = siblings[++siblingIndex];
                        var siblingElement = UGuiAccessibilityManager.instance.GetAccessibleElementForNode(sibling);

                        if (siblingElement == null)
                        {
                            continue;
                        }

                        // If the next sibling is not in the same scroll view, and we haven't found a fully visible
                        // sibling until now, stop searching.
                        if (siblingElement.m_ScrollView != m_ScrollView &&
                            !siblingElement.IsInsideScrollView(m_ScrollView))
                        {
                            break;
                        }

                        // Inactive nodes are not taken into account (they are not focusable).
                        if (!sibling.isActive)
                        {
                            continue;
                        }

                        activeSiblingIndexes.Add(siblingIndex);

                        // If the next sibling is fully visible in the scroll view, return it.
                        if (siblingElement.IsFullyVisibleInScrollView(m_ScrollView, out _))
                        {
                            return sibling;
                        }
                    }

                    break;
                }
                case AccessibilityScrollDirection.Backward:
                {
                    while (siblingIndex > 0)
                    {
                        // Go to the previous sibling.
                        var sibling = siblings[--siblingIndex];
                        var siblingElement = UGuiAccessibilityManager.instance.GetAccessibleElementForNode(sibling);

                        if (siblingElement == null)
                        {
                            continue;
                        }

                        // If the previous sibling is not in the same scroll view, and we haven't found a fully visible
                        // sibling until now, stop searching.
                        if (siblingElement.m_ScrollView != m_ScrollView &&
                            !siblingElement.IsInsideScrollView(m_ScrollView))
                        {
                            break;
                        }

                        // Inactive nodes are not taken into account (they are not focusable).
                        if (!sibling.isActive)
                        {
                            continue;
                        }

                        activeSiblingIndexes.Add(siblingIndex);

                        // If the previous sibling is fully visible in the scroll view, return it.
                        if (siblingElement.IsFullyVisibleInScrollView(m_ScrollView, out _))
                        {
                            return sibling;
                        }
                    }

                    break;
                }
            }

            // If no fully visible sibling was found (may happen if the siblings are larger than half of the scroll
            // view), look for the first partially visible sibling.
            foreach (var index in activeSiblingIndexes)
            {
                var sibling = siblings[index];
                var siblingElement = UGuiAccessibilityManager.instance.GetAccessibleElementForNode(sibling);

                if (siblingElement != null && siblingElement.IsVisibleInScrollView())
                {
                    return sibling;
                }
            }

            return null;

            static int IndexOf<T>(T elementToFind, IReadOnlyList<T> list)
            {
                var index = 0;

                foreach (var element in list)
                {
                    if (Equals(element, elementToFind))
                    {
                        return index;
                    }

                    index++;
                }

                return -1;
            }
        }
#endif // UNITY_6000_3_OR_NEWER
    }
}
