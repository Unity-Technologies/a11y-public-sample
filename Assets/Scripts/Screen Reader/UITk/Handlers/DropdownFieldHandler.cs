using UnityEngine.Scripting;
using UnityEngine.UIElements;

namespace Unity.Samples.ScreenReader
{
    [Preserve]
    class DropdownFieldHandler : BaseFieldHandler<string>
    {
        public DropdownFieldHandler()
        {
            OnSelect += () =>
            {
                using var evt = NavigationSubmitEvent.GetPooled();
                ownerElement.SendEvent(evt);
                return true;
            };
        }
    }
}
