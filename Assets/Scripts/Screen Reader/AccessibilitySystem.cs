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
            // Enable it for UITk
            CreateManager<UITkAccessibilityManager>();
            
            // Enable it for UGui
            // CreateManager<UGuiAccessibilityManager>();
        }
        
        static void CreateManager<T>() where T : AccessibilityManager
        {
            if (!GameObject.Find(typeof(T).Name))
            {
                var gameObject = new GameObject(typeof(T).Name);
                gameObject.AddComponent<T>();
            }
        }
    }
}
