using UnityEngine;

namespace Unity.Samples.Accessibility
{
    /// <summary>
    /// This class loads on initialization and creates the AccessibilityManager instance in the scene.
    /// </summary>
    static class AccessibilitySystem
    {
        [RuntimeInitializeOnLoadMethod]
        static void Initialize()
        {
            var gameObject = new GameObject("Accessibility Manager");
            gameObject.AddComponent<AccessibilityManager>();
        }
    }
}
