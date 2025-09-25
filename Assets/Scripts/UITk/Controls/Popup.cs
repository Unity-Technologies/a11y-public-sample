using System;
using Unity.Properties;
using Unity.Samples.ScreenReader;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;

namespace Unity.Samples.LetterSpell
{
    /// <summary>
    /// Represents a popup window that can display content over other UI elements.
    /// The popup window includes an overlay that dims the background and centers the content.
    /// It provides methods to show and close the popup.
    /// </summary>
    [UxmlElement]
    public partial class Popup : VisualElement
    {
        [Flags]
        public enum ClosePolicy
        {
            NoAutoClose,
            CloseOnPressOutside = 1,
            CloseOnEscape = 2,
        }
        
        class PopupOverlay : VisualElement
        {
            private PopupContent m_ContentContainer;
            
            public PopupContent popupContent => m_ContentContainer;
            
            public override VisualElement contentContainer => m_ContentContainer;
            
            public Popup popup { get; set; }
            
            public PopupOverlay()
            {
                style.position = Position.Absolute;
                style.alignItems = Align.Center;
                style.justifyContent = Justify.Center;
                style.top = 0;
                style.left = 0;
                style.right = 0;
                style.bottom = 0;
                style.backgroundColor = new Color(0, 0, 0, 0.7f);

                m_ContentContainer = new PopupContent();
                hierarchy.Add(m_ContentContainer);
                
                var styleSheet = Resources.Load<StyleSheet>("UITk/Themes/LetterSpellTheme");
                styleSheets.Add(styleSheet);
                
                RegisterCallback<PointerDownEvent>(OnPointerDown);
            }

            void OnPointerDown(PointerDownEvent evt)
            {
                if (!popup.closePolicy.HasFlag(ClosePolicy.CloseOnPressOutside))
                    return;
                evt.StopImmediatePropagation();
                popup.Close();
            }
        }

        class PopupContent : AccessibleVisualElement
        {
            public PopupContent()
            {
                AddToClassList("lsp-popup");
                accessible.modal = true;
            }
        }
        
        PopupOverlay m_Overlay;
        
        ClosePolicy m_ClosePolicy = ClosePolicy.CloseOnPressOutside | ClosePolicy.CloseOnEscape;

        [UxmlAttribute, CreateProperty]
        public ClosePolicy closePolicy
        {
            get => m_ClosePolicy;
            set => m_ClosePolicy = value;
        }
        
        public Popup()
        {
            m_Overlay = new PopupOverlay();
            m_Overlay.popup = this;
            style.position = Position.Absolute;
            style.display = DisplayStyle.None;
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }
        
        void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            Close();
        }
        
        public void Show()
        {
            MoveChildrenToContentContainer();
            panel.visualTree.Add(m_Overlay);
            var updater = panel.GetAccessibilityUpdater();
            updater?.OnVersionChanged(panel.visualTree, VersionChangeType.Hierarchy);
        }

        public void Close()
        {
            m_Overlay?.RemoveFromHierarchy();
            var updater = panel?.GetAccessibilityUpdater();
            updater?.OnVersionChanged(panel.visualTree, VersionChangeType.Hierarchy);
        }
        
        void MoveChildrenToContentContainer()
        {
            while (hierarchy.childCount > 0)
            {
                var child = hierarchy[0];
                child.RemoveFromHierarchy();
                m_Overlay.contentContainer.Add(child);
            }
        }
    }
}
