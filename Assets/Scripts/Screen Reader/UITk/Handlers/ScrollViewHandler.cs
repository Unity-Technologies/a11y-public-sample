using Unity.Samples.LetterSpell;
using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.UIElements;

namespace Unity.Samples.ScreenReader
{
    [Preserve]
    public class ScrollViewHandler : VisualElementAccessibilityHandler
    {
        protected override void BindToElement(VisualElement ve)
        {
            var scrollView = ve as ScrollView;
            
            scrollView.verticalScroller.slider.RegisterValueChangedCallback(OnScrollValueChanged);
            scrollView.contentViewport.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            scrollView.contentContainer.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }
        
        protected override void UnbindFromElement(VisualElement ve)
        { 
            var scrollView = ve as ScrollView;
            
            scrollView.verticalScroller.slider.UnregisterValueChangedCallback(OnScrollValueChanged);
            scrollView.contentViewport.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            scrollView.contentContainer.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }

        void OnScrollValueChanged(ChangeEvent<float> evt)
        {
            //OnScreenDebug.Log("ScrollView value changed " + evt.newValue);
            NotifyChange(VersionChangeType.Transform | VersionChangeType.Layout);
        }
        
        void OnGeometryChanged(GeometryChangedEvent evt)
        {
            //OnScreenDebug.Log("Geometry Changed " + evt.target + " old: " + (evt.target as VisualElement).layout);

            //OnScreenDebug.Log("Geometry Changed " + evt.target + " old: " + evt.oldRect + " new: " + evt.newRect);
            NotifyChange(VersionChangeType.Transform | VersionChangeType.Layout);
        }
    }
}
