using System.Collections;
using UnityEngine;
using UnityEngine.Accessibility;

namespace Unity.Samples.Accessibility
{
    static class MonoBehaviourExtensions
    {
        // Causes the screen reader hierarchy to be re-created (i.e. if there was a previous one, it gets
        // destroyed and a new one is created).
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

        // Causes the screen reader hierarchy to have all its frames (i.e. screen positions) recalculated.
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

        // Delays sending the layout changed notification with the new node to focus on.
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
                
                AssistiveSupport.notificationDispatcher.SendLayoutChanged(node);
            }
        }
    }
}
