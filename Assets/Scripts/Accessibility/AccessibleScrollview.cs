using System.Collections;
using UnityEngine;
using UnityEngine.Accessibility;
using UnityEngine.UI;

namespace Unity.Samples.Accessibility
{
    /// <summary>
    /// Component attached to the UI game objects that should be considered scroll views by the screen reader.
    /// </summary>
    [AddComponentMenu("Accessibility/Accessible Scroll View"), DisallowMultipleComponent]
    [ExecuteAlways]
    public sealed class AccessibleScrollView : AccessibleElement
    {
        ScrollRect m_ScrollRect;

        const float k_ExtraOffset = 50f;
        const float k_ScrollDuration = 0.2f;

        void Start()
        {
            // The scroll view should not be accessible (i.e. focusable with the screen reader). The purpose of this
            // component is to automatically scroll to the focused node inside the scroll view if it is not fully
            // visible.
            isActive = false;

            m_ScrollRect = GetComponentInChildren<ScrollRect>();
        }

        void Update()
        {
            // Update the node frames when scrolling.
            if (m_ScrollRect && m_ScrollRect.content.hasChanged)
            {
                m_ScrollRect.content.hasChanged = false;
                AssistiveSupport.activeHierarchy?.RefreshNodeFrames();
            }
        }

        protected override void BindToControl()
        {
            if (m_ScrollRect != null)
            {
                AssistiveSupport.nodeFocusChanged += OnNodeFocusChanged;
            }
        }

        protected override void UnbindFromControl()
        {
            if (m_ScrollRect != null)
            {
                AssistiveSupport.nodeFocusChanged -= OnNodeFocusChanged;
            }
        }

        void OnNodeFocusChanged(AccessibilityNode accessibilityNode)
        {
            // If the focused node in inside this scroll view and is not fully visible, then automatically scroll the
            // scroll view to make it fully visible.
            if (IsInsideScrollView(accessibilityNode) && !IsFullyVisibleInScrollView(accessibilityNode))
            {
                ScrollIntoView(accessibilityNode);
            }
        }

        bool IsInsideScrollView(AccessibilityNode accessibilityNode)
        {
            var element = AccessibilityManager.GetAccessibleElementForNode(accessibilityNode);
            var currentTransform = element.transform.parent;

            // Traverse up the parent hierarchy.
            while (currentTransform != null)
            {
                // Check if the current parent has the ScrollRect component.
                if (currentTransform.GetComponent<ScrollRect>() == m_ScrollRect)
                {
                    return true; // Found the scroll view.
                }

                // Move to the next parent.
                currentTransform = currentTransform.parent;
            }

            return false; // No scroll view found.
        }

        bool IsFullyVisibleInScrollView(AccessibilityNode accessibilityNode)
        {
            var scrollViewFrame = node.frame;
            var nodeFrame = accessibilityNode.frame;

            return
                scrollViewFrame.Contains(new Vector2(nodeFrame.xMin, nodeFrame.yMin)) && // Bottom left corner
                scrollViewFrame.Contains(new Vector2(nodeFrame.xMax, nodeFrame.yMin)) && // Bottom right corner
                scrollViewFrame.Contains(new Vector2(nodeFrame.xMin, nodeFrame.yMax)) && // Top left corner
                scrollViewFrame.Contains(new Vector2(nodeFrame.xMax, nodeFrame.yMax));   // Top right corner
        }

        void ScrollIntoView(AccessibilityNode accessibilityNode)
        {
            Canvas.ForceUpdateCanvases();

            var element = AccessibilityManager.GetAccessibleElementForNode(accessibilityNode);

            var viewportRect = m_ScrollRect.viewport.rect;
            var contentRect = m_ScrollRect.content.rect;

            // Calculate the bounds of the element and the viewport.
            var itemBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(m_ScrollRect.viewport, element.transform as RectTransform);
            var viewportBounds = new Bounds(viewportRect.center, viewportRect.size);

            // Calculate the offset required to make the element visible.
            var offset = Vector2.zero;

            // Check and calculate the vertical offset.
            if (itemBounds.max.y > viewportBounds.max.y)
            {
                offset.y = itemBounds.max.y - viewportBounds.max.y + k_ExtraOffset;
            }
            else if (itemBounds.min.y < viewportBounds.min.y)
            {
                offset.y = itemBounds.min.y - viewportBounds.min.y - k_ExtraOffset;
            }

            // Check and calculate the horizontal offset.
            if (itemBounds.max.x > viewportBounds.max.x)
            {
                offset.x = itemBounds.max.x - viewportBounds.max.x + k_ExtraOffset;
            }
            else if (itemBounds.min.x < viewportBounds.min.x)
            {
                offset.x = itemBounds.min.x - viewportBounds.min.x - k_ExtraOffset;
            }

            // Convert the offset to a normalized position.
            var normalizedOffset = new Vector2(
                offset.x / (contentRect.width - viewportRect.width),
                offset.y / (contentRect.height - viewportRect.height));

            // Adjust the scroll view's normalized position.
            var targetPosition = new Vector2(
                Mathf.Clamp01(m_ScrollRect.horizontalNormalizedPosition + normalizedOffset.x),
                Mathf.Clamp01(m_ScrollRect.verticalNormalizedPosition + normalizedOffset.y));

            StartCoroutine(SmoothScroll(targetPosition));
        }

        IEnumerator SmoothScroll(Vector2 targetPosition)
        {
            var time = 0f;
            var startPosition = m_ScrollRect.normalizedPosition;

            while (time < k_ScrollDuration)
            {
                time += Time.deltaTime;

                var x = Mathf.Lerp(startPosition.x, targetPosition.x, time / k_ScrollDuration);
                var y = Mathf.Lerp(startPosition.y, targetPosition.y, time / k_ScrollDuration);
                m_ScrollRect.normalizedPosition = new Vector2(x, y);

                yield return null;
            }

            m_ScrollRect.normalizedPosition = targetPosition;
        }
    }
}
