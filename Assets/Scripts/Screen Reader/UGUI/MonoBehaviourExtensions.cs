using System.Collections;
using UnityEngine;
using UnityEngine.Accessibility;

namespace Unity.Samples.ScreenReader
{
    static class MonoBehaviourExtensions
    {
        /// <summary>
        /// Recalculates all the accessibility node frames (i.e. screen positions) in the accessibility hierarchy.
        /// </summary>
        public static IEnumerator RefreshNodeFrames(this MonoBehaviour behaviour)
        {
            yield return new WaitForEndOfFrame();

            AccessibilityManager.hierarchy?.RefreshNodeFrames();
        }

        /// <summary>
        /// Sends the layout changed notification with the new node to focus on.
        /// </summary>
        public static IEnumerator FocusOnNode(this MonoBehaviour behaviour, int nodeId)
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
