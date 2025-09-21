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
            var listViewItem = ownerElement;
            var label = listViewItem.Q<Label>();

            if (label != null)
            {
                return label.text;
            }

            return null;
        }
    }

    class ListViewItemHandlerCreator : VisualElementAccessibilityHandlerFactory.ICreator
    {
        public bool CanCreate(VisualElement element)
        {
            return element.ClassListContains(ListView.itemUssClassName);
        }

        public VisualElementAccessibilityHandler Create(VisualElement element)
        {
            return new ListViewItemHandler();
        }
    }
}
