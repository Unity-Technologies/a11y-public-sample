using System.Linq;
using UnityEngine.Localization.Settings;
using UnityEngine.UIElements;

namespace UnityEngine.Localization
{
    [UxmlObject]
    public partial class LocalizedStringList : LocalizedString
    {
        [UxmlAttribute] public char separator = ',';

        protected override BindingResult Update(in BindingContext context)
        {
            if (IsEmpty)
            {
                return new BindingResult(BindingStatus.Success);
            }

#if UNITY_EDITOR
            // When not in playmode and not previewing a language we want to show something, so we revert to the project locale.
            if (!Application.isPlaying && LocaleOverride == null && LocalizationSettings.SelectedLocale == null)
            {
                LocaleOverride = LocalizationSettings.ProjectLocale;
            }
#endif

            if (!CurrentLoadingOperationHandle.IsDone)
            {
                return new BindingResult(BindingStatus.Pending);
            }

            var element = context.targetElement;
            var result = GetLocalizedString();
            var resultArray = result.Split(separator, System.StringSplitOptions.RemoveEmptyEntries).ToList();

            int index = -1;
            if (element is DropdownField)
            {
                index = ((DropdownField)element).index;
            }

            if (ConverterGroups.TrySetValueGlobal(ref element, context.bindingId, resultArray, out var errorCode))
            {
                if (element is DropdownField field && index != -1)
                {
                    // Workaround as the dropdown field value does not update when choices changes. (UUM-120183)
                    field.value = "";
                    field.index = index;
                }

                return new BindingResult(BindingStatus.Success);
            }

            return CreateErrorResult(context, errorCode, typeof(string));
        }
    }
}
