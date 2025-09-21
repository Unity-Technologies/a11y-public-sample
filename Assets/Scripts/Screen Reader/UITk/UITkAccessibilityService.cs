using System;
using System.Collections.Generic;
using Unity.Samples.LetterSpell;
using Unity.Samples.ScreenReader;
using UnityEngine;
using UnityEngine.Accessibility;
using UnityEngine.Pool;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

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
        List<VisualTreeAccessibilityUpdater> m_AccessibilityUpdaters = new List<VisualTreeAccessibilityUpdater>();

        /// <summary>
        /// Constructor for the UITkAccessibilityService class.
        /// </summary>
        public UITkAccessibilityService() : base("UITk", 100)
        {
        }
        
        VisualTreeAccessibilityUpdater GetVisualTreeUpdater(IPanel panel)
        {
            foreach (var pd in m_AccessibilityUpdaters)
            {
                if (pd.panel == panel)
                    return pd;
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
            var uiDocuments = MonoBehaviour.FindObjectsByType<UIDocument>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            if (uiDocuments == null || uiDocuments.Length == 0)
            {
                return;
            }
            
            var panels = new List<IPanel>();
            var visualTrees = new List<VisualElement>();

            foreach (var uiDocument in uiDocuments)
            {
                // Ignore the On Screen Debug UI Document
                if (uiDocument.GetComponent<OnScreenDebugBehavior>() != null)
                    continue;
                
                var panel = uiDocument.runtimePanel;
                var rootAcc = uiDocument.rootVisualElement.GetOrCreateAccessible();
                
                rootAcc.label = uiDocument.rootVisualElement.name;
                rootAcc.active = false;
                rootAcc.role = AccessibilityRole.Container;

                if (!panels.Contains(panel))
                {
                    panels.Add(panel);
                    visualTrees.Add(uiDocument.rootVisualElement.parent);
                }
            }
            
            //OnScreenDebug.Log("Generating UITk Nodes Panel " + panels.Count);
            for (int i = 0; i < panels.Count; i++)
            {
                var panel = panels[i];
                var visualTree = visualTrees[i];
                if (panel == null || visualTree == null)
                    continue;

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