using System;
using System.Collections.Generic;
using Unity.Samples.LetterSpell;
using UnityEngine;
using UnityEngine.Accessibility;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Unity.Samples.ScreenReader
{
    /// <summary>
    /// Accessibility service for Unity UI Toolkit (UITk).
    /// This service integrates with the Unity UI Toolkit to provide accessibility features for UI elements.
    /// It scans the UI Toolkit visual tree, creates and updates accessibility nodes, and manages their
    /// lifecycle within the accessibility hierarchy.
    /// </summary>
    public class UITkAccessibilityService : AccessibilityService
    {
        List<VisualTreeAccessibilityUpdater> m_AccessibilityUpdaters = new();

        /// <summary>
        /// Constructor for the UITkAccessibilityService class.
        /// </summary>
        public UITkAccessibilityService() : base("UITk", 100)
        {
            AssistiveSupport.nodeFocusChanged += OnNodeFocusChanged;
        }

        bool m_NeedsToRecomputeRootFrames;

        public void DirtyRootFrames()
        {
            m_NeedsToRecomputeRootFrames = true;
        }

        public void UpdateRootAndPanelFrames()
        {
            m_NeedsToRecomputeRootFrames = true;

            foreach (var accessibilityUpdater in m_AccessibilityUpdaters)
            {
                accessibilityUpdater.UpdateRootFrame();
            }

            var serviceRootFrame = Rect.zero;

            foreach (var panelNode in hierarchy.rootNode.children)
            {
                // Calculate the union of all root nodes frames.
                if (panelNode.frame.size != Vector2.zero)
                {
                    Encompass(ref serviceRootFrame, panelNode.frame);
                }
            }

            hierarchy.rootNode.frame = serviceRootFrame;
            void Encompass(ref Rect a, Rect b)
            {
                a.xMin = Math.Min(a.xMin, b.xMin);
                a.yMin = Math.Min(a.yMin, b.yMin);
                a.xMax = Math.Max(a.xMax, b.xMax);
                a.yMax = Math.Max(a.yMax, b.yMax);
            }
        }

        public override void Update()
        {
            if (m_NeedsToRecomputeRootFrames)
            {
                UpdateRootAndPanelFrames();
                m_NeedsToRecomputeRootFrames = false;
            }

            foreach (var accessibilityUpdater in m_AccessibilityUpdaters)
            {
                accessibilityUpdater.Update();
            }
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
            foreach (var updater in m_AccessibilityUpdaters)
            {
                var element = updater?.GetVisualElementForNode(node);

                if (element != null)
                {
                    return element;
                }
            }

            return null;
        }

        public VisualElement GetVisualElementForNode(IPanel panel, AccessibilityNode node)
        {
            var updater = GetVisualTreeUpdater(panel);

            return updater?.GetVisualElementForNode(node);
        }

        public VisualTreeAccessibilityUpdater GetVisualTreeUpdater(IPanel panel)
        {
            foreach (var pd in m_AccessibilityUpdaters)
            {
                if (pd.panel == panel)
                {
                    return pd;
                }
            }

            return null;
        }

        void CreateVisualTreeUpdaterForPanel(IPanel panel, VisualElement visualTree, UITkAccessibilityService service)
        {
            var newPd = new VisualTreeAccessibilityUpdater
            (
                panel,
                visualTree,
                service
            );

            m_AccessibilityUpdaters.Add(newPd);
        }

        public override void SetUp(Scene scene)
        {
            // OnScreenDebug.Log("Start Setup UITkAccessibilityService for scene " + scene.name);

            var uiDocuments = Object.FindObjectsByType<UIDocument>(FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            if (uiDocuments == null || uiDocuments.Length == 0)
            {
                return;
            }

            var panels = new List<IPanel>();
            var visualTrees = new List<VisualElement>();

            foreach (var uiDocument in uiDocuments)
            {
                if (!uiDocument.enabled)
                    continue;
                        
                // Ignore the OnScreenDebug UI Document.
                if (uiDocument.GetComponent<OnScreenDebugBehavior>() != null)
                {
                    continue;
                }

                var panel = uiDocument.runtimePanel;
                var rootAcc = uiDocument.rootVisualElement.GetOrCreateAccessibleProperties();

                rootAcc.label = uiDocument.rootVisualElement.name;
                rootAcc.active = Application.platform == RuntimePlatform.OSXPlayer; // false;
                rootAcc.role = AccessibilityRole.Container;

                if (!panels.Contains(panel))
                {
                    panels.Add(panel);
                    visualTrees.Add(uiDocument.rootVisualElement.parent);
                }
            }

            // OnScreenDebug.Log("Generating UITk Nodes Panel " + panels.Count);

            for (var i = 0; i < panels.Count; i++)
            {
                var panel = panels[i];
                var visualTree = visualTrees[i];

                if (panel == null || visualTree == null)
                {
                    continue;
                }

                CreateVisualTreeUpdaterForPanel(panel, visualTree, this);
            }
        }

        public override void CleanUp()
        {
            foreach (var accessibilityUpdater in m_AccessibilityUpdaters)
            {
                accessibilityUpdater.Dispose();
            }

            m_AccessibilityUpdaters.Clear();
        }
    }
}
