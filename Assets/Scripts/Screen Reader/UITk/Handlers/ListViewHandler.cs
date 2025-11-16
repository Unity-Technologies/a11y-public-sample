using UnityEngine.Scripting;
using UnityEngine.UIElements;

namespace Unity.Samples.ScreenReader
{
    [Preserve]
    class ListViewHandler : VisualElementAccessibilityHandler
    {
    }

    [Preserve]
    class ListViewItemHandler : VisualElementAccessibilityHandler
    {
        public override string GetLabel()
        {
            return ownerElement.Q<Label>()?.text;
        }
    }

    class ListViewItemHandlerCreator : VisualElementAccessibilityHandlerFactory.ICreator
    {
        public bool CanCreate(VisualElement element)
        {
            return element.ClassListContains(BaseListView.itemUssClassName);
        }

        public VisualElementAccessibilityHandler Create(VisualElement element)
        {
            return new ListViewItemHandler();
        }
    }
}
