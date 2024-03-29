using UnityEngine;
using UnityEngine.Accessibility;

namespace Unity.Samples.Accessibility
{
    /// <summary>
    /// Component attached to the UI GameObjects that are considered as images by the screen reader.
    /// </summary>
    [AddComponentMenu("Accessibility/Accessible Image"), DisallowMultipleComponent]
    [ExecuteAlways]
    public class AccessibleImage : AccessibleElement
    {
        void Start()
        {
            role |= AccessibilityRole.Image;
        }
    }
}
