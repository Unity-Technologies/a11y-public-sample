using Unity.Samples.LetterSpell;
using UnityEngine.Accessibility;
using UnityEngine.Scripting;
using UnityEngine.UIElements;

namespace Unity.Samples.ScreenReader
{
    [Preserve]
    class BasePopupFieldHandler<TValue, TValueChoice> : BaseFieldHandler<TValue>
    {
        bool m_HasPendingCheck;

        public BasePopupFieldHandler()
        {
            selected += () =>
            {
                ownerElement.schedule.Execute(CheckForOpenedPopupMenu).ExecuteLater(300);

                using var evt = NavigationSubmitEvent.GetPooled();
                evt.target = ownerElement;
                ownerElement.SendEvent(evt);

                OnScreenDebug.Log("Submit event sent to " + ownerElement.name);

                return true;
            };
        }

#if UNITY_6000_3_OR_NEWER
        public override AccessibilityRole GetRole() => AccessibilityRole.Dropdown;
#endif // UNITY_6000_3_OR_NEWER

        protected override void BindToElement(VisualElement element)
        {
            base.BindToElement(element);
            element.RegisterCallback<PointerDownEvent>(OnPointerDown);
            element.RegisterCallback<NavigationSubmitEvent>(OnNavigationSubmit);
        }

        protected override void UnbindFromElement(VisualElement element)
        {
            element.UnregisterCallback<PointerDownEvent>(OnPointerDown);
            element.UnregisterCallback<NavigationSubmitEvent>(OnNavigationSubmit);
            base.UnbindFromElement(element);
        }

        void OnNavigationSubmit(NavigationSubmitEvent evt)
        {
            OnScreenDebug.Log("Submit event received by " + ownerElement.name);

            ScheduledCheckForOpenedPopupMenu();
        }

        void OnPointerDown(PointerDownEvent evt)
        {
            OnScreenDebug.Log("Pointer Down event " + ownerElement.name);

            ScheduledCheckForOpenedPopupMenu();
        }

        void ScheduledCheckForOpenedPopupMenu()
        {
            if (m_HasPendingCheck)
            {
                return;
            }

            m_HasPendingCheck = true;
            ownerElement.schedule.Execute(() =>
            {
                m_HasPendingCheck = false;
                CheckForOpenedPopupMenu();
            }).ExecuteLater(300);
        }

        void CheckForOpenedPopupMenu()
        {
            var panel = ownerElement.panel;

            if (panel == null)
            {
                return;
            }

            var panelRootVisualElement = panel.visualTree;
            var popupMenu = panelRootVisualElement.Q(classes: GenericDropdownMenu.ussClassName);

            OnScreenDebug.Log("Showing popup menu: " + (popupMenu != null));

            if (popupMenu != null)
            {
                var popupAcc = popupMenu.GetOrCreateAccessibleProperties();

#if UNITY_6000_3_OR_NEWER
                popupAcc.role = AccessibilityRole.Dropdown;
#endif // UNITY_6000_3_OR_NEWER
                popupAcc.active = false;
                popupAcc.modal = true;

                // Setup items in the popup menu
                var items = popupMenu.Query(classes: GenericDropdownMenu.itemUssClassName).ToList();
                var i = 0;

                OnScreenDebug.Log("Item count : " + items.Count);
                foreach (var item in items)
                {
                   // if (item.GetAccessibleProperties() != null)
                   //     continue;

                    var itemAcc = item.GetOrCreateAccessibleProperties();
                    var itemLabel = item.Q<Label>();

                    itemAcc.label = itemLabel != null ? itemLabel.text : $"Item {i}";
                    itemAcc.role = AccessibilityRole.Button;
                    itemAcc.state = item.hasCheckedPseudoState ? AccessibilityState.Selected : AccessibilityState.None;
                    itemAcc.selected += () =>
                    {
                        using var evt = NavigationSubmitEvent.GetPooled();
                        evt.target = item;
                        item.SendEvent(evt);

                        return true;
                    };

                    itemLabel.GetOrCreateAccessibleProperties().ignored = true;

                    i++;
                }

                // NotifyChange();
            }
        }
    }
}
