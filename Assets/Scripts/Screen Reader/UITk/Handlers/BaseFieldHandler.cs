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
            return ownerElement is BaseField<TValueType> field ? $"{field.value}" : "";
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
            if (ownerElement is Toggle or RadioButton or Slider)
            {
                var updater = ownerElement.panel.GetAccessibilityUpdater();
                updater?.UpdateNode(this);
            }
            else
            {
                NotifyChange();
            }
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
}
