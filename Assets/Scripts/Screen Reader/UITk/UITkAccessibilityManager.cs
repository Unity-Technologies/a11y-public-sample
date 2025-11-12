using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Accessibility;
using UnityEngine.Pool;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Unity.Samples.ScreenReader
{
    /// <summary>
    /// This class is added to the scene in order to detect the screen reader being turned on/off and automatically
    /// convert the VisualElement hierarchy of a UIDocument into the accessibility hierarchy (data model) that the screen
    /// reader needs.
    /// The accessibility hierarchy order reflects the order of the VisualElement hierarchy.
    /// </summary>
    public class UITkAccessibilityManager : AccessibilityManager
    {
        private VisualTreeAccessibilityUpdater m_AccessibilityUpdater;

        UIDocument m_UIDocument;
        
        /// <summary>
        /// Static instance of the UITkAccessibilityManager in the scene.
        /// </summary>
        public new static UITkAccessibilityManager instance => AccessibilityManager.instance as UITkAccessibilityManager;
        
        public VisualTreeAccessibilityUpdater accessiblityUpdater => m_AccessibilityUpdater;
        
        private void Awake()
        {
            AssistiveSupport.nodeFocusChanged += OnNodeFocusChanged;
            
            // Look for the main UIDocument in the scene.
            var uiDocuments = Object.FindObjectsByType<UIDocument>(FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            if (uiDocuments == null || uiDocuments.Length == 0)
            {
                Debug.LogError("UI Documents not found");
                return;
            }
            
            m_UIDocument = uiDocuments[0];
            
            if (m_UIDocument != null)
            {
                CreateVisualTreeUpdaterForPanel(m_UIDocument.runtimePanel);
            }
        }

        private void OnDestroy()
        {
            m_AccessibilityUpdater?.Dispose();
            m_AccessibilityUpdater = null;
            AssistiveSupport.nodeFocusChanged -= OnNodeFocusChanged;
        }
        
        void CreateVisualTreeUpdaterForPanel(IPanel panel)
        {
            m_AccessibilityUpdater?.Dispose();
            
            var newPd = new VisualTreeAccessibilityUpdater
            (
                panel,
                panel.visualTree,
                this
            );

            m_AccessibilityUpdater = newPd;
        }

        protected override void UpdateManager()
        {
            m_AccessibilityUpdater?.Update();
        }
        
        void OnNodeFocusChanged(AccessibilityNode node)
        {
            // Scroll to the focused node if it is inside scroll views.
            // Scroll recursively to support nested scroll views.
            var element = GetVisualElementForNode(node);

            if (element != null)
            {
                // Scroll recursively to support nested scroll views.
                var ancestor = element.hierarchy.parent;

                while (ancestor != null)
                {
                    if (ancestor is ScrollView scrollView)
                    {
                        scrollView.ScrollTo(element);
                    }

                    ancestor = ancestor.hierarchy.parent;
                }
            }
        }
        
        public VisualElement GetVisualElementForNode(AccessibilityNode node)
        {
            return m_AccessibilityUpdater.GetVisualElementForNode(node);
        }

        public VisualElement GetVisualElementForNode(IPanel panel, AccessibilityNode node)
        {
            return m_AccessibilityUpdater.GetVisualElementForNode(node);
        }

        protected override void GenerateHierarchy(Scene scene)
        {
            m_AccessibilityUpdater?.CleanUp();
        }
    }
}
