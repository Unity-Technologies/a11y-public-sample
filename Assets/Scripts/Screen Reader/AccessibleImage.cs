using UnityEngine;
using UnityEngine.Accessibility;

namespace Unity.Samples.ScreenReader
{
    /// <summary>
    /// Component attached to the UI game objects that should be considered images by the screen reader.
    /// </summary>
    [AddComponentMenu("Accessibility/Accessible Image"), DisallowMultipleComponent]
    [ExecuteAlways]
    public class AccessibleImage : AccessibleElement
    {
        void Start()
        {
            role = AccessibilityRole.Image;
        }
    }
}
