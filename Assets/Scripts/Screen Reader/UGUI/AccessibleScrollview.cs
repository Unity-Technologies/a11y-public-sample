using System.Collections;
using UnityEngine;
using UnityEngine.Accessibility;
using UnityEngine.UI;

namespace Unity.Samples.ScreenReader
{
    /// <summary>
    /// Component attached to the UI game objects that should be considered scroll views by the screen reader.
    /// </summary>
    [AddComponentMenu("Accessibility/Accessible Scroll View"), DisallowMultipleComponent]
    [ExecuteAlways]
    public sealed class AccessibleScrollView : AccessibleElement
    {
        ScrollRect m_ScrollRect;

        AccessibilityNode m_NodeToBeFocused;

        const float k_ScrollDuration = 0.2f;

        void Start()
        {
            // On macOS, container nodes should be active and have a label set in order to function properly.
            // If a container node is not active, the screen reader will not see neither the node nor its children.
            // If a container node has no label, the screen reader will skip the node and treat its children as
            // containers.
            label ??= "Scroll View";

#if UNITY_6000_3_OR_NEWER
            role = AccessibilityRole.ScrollView;
#else // UNITY_6000_3_OR_NEWER
            // The scroll view should not be accessible (i.e. focusable with the screen reader) on mobile platforms.
            // The purpose of this component is to automatically scroll to the focused node inside the scroll view if
            // it is not fully visible.
            if (Application.platform == RuntimePlatform.Android ||
                Application.platform == RuntimePlatform.IPhonePlayer)
            {
                isActive = false;
            }
#endif // UNITY_6000_3_OR_NEWER

            m_ScrollRect = GetComponentInChildren<ScrollRect>();
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
            // If there is a node we requested to be focused (for example, after the user manually scrolled), pause the
            // automatic scrolling until the node receives focus.
            // Note: On Android, when we request a node to be focused, we don't get a focus changed event when that node
            // receives focus, so the automatic scrolling will not be triggered.
            if (Application.platform != RuntimePlatform.Android)
            {
                if (m_NodeToBeFocused != null && accessibilityNode != m_NodeToBeFocused)
                {
                    return;
                }

                if (accessibilityNode == m_NodeToBeFocused)
                {
                    m_NodeToBeFocused = null;
                    return;
                }
            }

            var element = UGuiAccessibilityManager.instance.GetAccessibleElementForNode(accessibilityNode);

            if (element == null)
            {
                return;
            }

            // If the focused node in inside this scroll view and is not fully visible, then automatically scroll the
            // scroll view to make it fully visible.
            if (element.IsInsideScrollView(m_ScrollRect) && !element.IsFullyVisibleInScrollView(m_ScrollRect, out var offset))
            {
                ScrollIntoView(element, offset);
            }
        }

        void ScrollIntoView(AccessibleElement element, Vector2 offset)
        {
            if (element == null || offset == Vector2.zero)
            {
                return;
            }

            Canvas.ForceUpdateCanvases();

            var viewportRect = m_ScrollRect.viewport.rect;
            var contentRect = m_ScrollRect.content.rect;

            var widthDifference = contentRect.width - viewportRect.width;
            var heightDifference = contentRect.height - viewportRect.height;

            // Convert the offset to a normalized position.
            var normalizedOffset = new Vector2(
                widthDifference == 0 ? 0 : offset.x / widthDifference,
                heightDifference == 0 ? 0 : offset.y / heightDifference);

            // Adjust the scroll view's normalized position.
            var targetPosition = new Vector2(
                Mathf.Clamp01(m_ScrollRect.horizontalNormalizedPosition + normalizedOffset.x),
                Mathf.Clamp01(m_ScrollRect.verticalNormalizedPosition + normalizedOffset.y));

            const float floatTolerance = 0.01f;
            if (Mathf.Abs(normalizedOffset.x) >= floatTolerance || Mathf.Abs(normalizedOffset.y) >= floatTolerance)
            {
                StartCoroutine(SmoothScroll(targetPosition));
            }
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

            AssistiveSupport.activeHierarchy?.RefreshNodeFrames();
        }

        /// <summary>
        /// Pauses the automatic scrolling of the scroll view until the specified node receives accessibility focus.
        /// </summary>
        /// <param name="nodeToBeFocused">The node that should receive focus before the automatic scrolling resumes.
        /// </param>
        public void PauseAutomaticScrolling(AccessibilityNode nodeToBeFocused)
        {
            m_NodeToBeFocused = nodeToBeFocused;
        }
    }
}
