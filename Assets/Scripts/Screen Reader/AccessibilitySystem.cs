using UnityEngine;

namespace Unity.Samples.ScreenReader
{
    /// <summary>
    /// This class loads on initialization and creates the AccessibilityManager instance in the scene.
    /// </summary>
    static class AccessibilitySystem
    {
        [RuntimeInitializeOnLoadMethod]
        static void Initialize()
        {
            if (!GameObject.Find(nameof(AccessibilityManager)))
            {
                var gameObject = new GameObject(nameof(AccessibilityManager));
                gameObject.AddComponent<AccessibilityManager>();
            }
        }
    }
}
