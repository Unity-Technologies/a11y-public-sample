using System.Collections;
using UnityEngine;
using UnityEngine.Accessibility;

namespace Unity.Samples.ScreenReader
{
    static class MonoBehaviourExtensions
    {
        /// <summary>
        /// Recreates the accessibility hierarchy (i.e. if there was a previous one, it is destroyed and a new one is
        /// created).
        /// </summary>
        public static void DelayRefreshHierarchy(this MonoBehaviour behaviour)
        {
            behaviour.StartCoroutine(RefreshHierarchy());
            return;

            IEnumerator RefreshHierarchy()
            {
                yield return new WaitForEndOfFrame();
                AccessibilityManager.RefreshHierarchy();
            }
        }

        /// <summary>
        /// Recalculates all the accessibility node frames (i.e. screen positions) in the accessibility hierarchy.
        /// </summary>
        public static void DelayRefreshNodeFrames(this MonoBehaviour behaviour)
        {
            behaviour.StartCoroutine(RefreshNodeFrames());
            return;

            IEnumerator RefreshNodeFrames()
            {
                yield return new WaitForEndOfFrame();
                AccessibilityManager.hierarchy?.RefreshNodeFrames();
            }
        }

        /// <summary>
        /// Sends the layout changed notification with the new node to focus on.
        /// </summary>
        public static void DelayFocusOnNode(this MonoBehaviour behaviour, int nodeId)
        {
            behaviour.StartCoroutine(FocusOnNode());
            return;

            IEnumerator FocusOnNode()
            {
                // Wait for the next frame to ensure that nodes exist in the hierarchy.
                yield return new WaitForEndOfFrame();
                
                // Find the new node to focus on.
                AccessibilityManager.hierarchy.TryGetNode(nodeId, out var node);

                // Move the accessibility focus to the node.
                AssistiveSupport.notificationDispatcher.SendLayoutChanged(node);
            }
        }
    }
}
