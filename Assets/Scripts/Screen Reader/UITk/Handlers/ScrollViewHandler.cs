using UnityEngine;
using UnityEngine.Accessibility;
using UnityEngine.Scripting;
using UnityEngine.UIElements;

namespace Unity.Samples.ScreenReader
{
    [Preserve]
    public class ScrollViewHandler : VisualElementAccessibilityHandler
    {
#if UNITY_6000_3_OR_NEWER
        public override AccessibilityRole GetRole() => AccessibilityRole.ScrollView;
#endif // UNITY_6000_3_OR_NEWER

        protected override void BindToElement(VisualElement ve)
        {
            var scrollView = ve as ScrollView;

            scrollView?.verticalScroller.slider.RegisterValueChangedCallback(OnScrollValueChanged);
            scrollView?.contentViewport.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            scrollView?.contentContainer.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }

        protected override void UnbindFromElement(VisualElement ve)
        {
            var scrollView = ve as ScrollView;

            scrollView?.verticalScroller.slider.UnregisterValueChangedCallback(OnScrollValueChanged);
            scrollView?.contentViewport.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            scrollView?.contentContainer.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }

        void OnScrollValueChanged(ChangeEvent<float> evt)
        {
            NotifyChange(VersionChangeType.Transform | VersionChangeType.Layout);
        }

        void OnGeometryChanged(GeometryChangedEvent evt)
        {
            NotifyChange(VersionChangeType.Transform | VersionChangeType.Layout);
        }
    }
}
