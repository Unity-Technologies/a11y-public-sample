using UnityEngine.Accessibility;
using UnityEngine.Scripting;
using UnityEngine.UIElements;

namespace Unity.Samples.ScreenReader
{
    [Preserve]
    class BaseFieldHandler<TValueType> : VisualElementAccessibilityHandler
    {
        public override string GetLabel()
        {
            return (ownerElement as BaseField<TValueType>)?.label;
        }

        public override string GetValue()
        {
            var field = ownerElement as BaseField<TValueType>;
            return field == null ? "" : $"{field.value}";
        }

        protected override void BindToElement(VisualElement ve)
        {
            var field = ownerElement as BaseField<TValueType>;
            field.RegisterValueChangedCallback(OnValueChanged);
            EnsureLabelIsNotAccessible();
        }

        protected override void UnbindFromElement(VisualElement ve)
        {
            var field = ownerElement as BaseField<TValueType>;
            field.UnregisterValueChangedCallback(OnValueChanged);
        }

        void OnValueChanged(ChangeEvent<TValueType> e)
        {
            EnsureLabelIsNotAccessible();
            NotifyChange();
        }

        void EnsureLabelIsNotAccessible()
        {
            var field = ownerElement as BaseField<TValueType>;

            if (field?.labelElement != null)
            {
                field.labelElement.GetOrCreateAccessibleProperties().ignored = true;
            }
        }
    }

    [Preserve]
    class ToggleHandler : BaseFieldHandler<bool>
    {
        public override AccessibilityRole GetRole()
        {
            return AccessibilityRole.Toggle;
        }
    }
    
    [Preserve]
    class RadioButtonHandler : BaseFieldHandler<bool>
    {
        public override AccessibilityRole GetRole()
        {
            return AccessibilityRole.Toggle;
        }
    }
}
