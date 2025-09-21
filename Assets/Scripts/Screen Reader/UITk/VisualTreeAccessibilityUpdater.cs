using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.Samples.LetterSpell;
using UnityEngine;
using UnityEngine.Accessibility;
using UnityEngine.UIElements;

namespace Unity.Samples.ScreenReader
{
    /// <summary>
    /// VisualTreeAccessibilityUpdater is responsible to keep the AccessibilityHierarchy in sync with the VisualElement tree.
    /// It scans the VisualElement tree, creates and updates AccessibilityNodes, and manages their lifecycle within the AccessibilitySubHierarchy.
    /// It also handles modality by tracking modal elements and updating the active state of nodes accordingly.
    /// </summary>
    public class VisualTreeAccessibilityUpdater
    {
        [Flags]
        enum NotificationType
        {
            None = 0,
            LayoutChanged = 1,
            ScreenChanged = 2
        }
        
        private uint m_Version = 0;
        private uint m_LastVersion = 0;
        private Dictionary<VisualElement, VisualElementAccessibilityHandler> m_HandlersForElements = new();
        private Dictionary<AccessibilityNode, VisualElementAccessibilityHandler> m_HandlersForNodes = new();
        
        private bool m_PaintedBeforeBlocker = true;
        private int m_RootNextInsertionIndex;
        
        private VisualElement m_VisualTree;
        private IVisualElementScheduledItem m_UpdateJob;
        private UITkAccessibilityService m_AccessibilityService;
        
        /// <summary>
        /// The sub hierarchy this updater is managing.
        /// </summary>
        public AccessibilitySubHierarchy hierarchy { get; set; }
        
        /// <summary>
        /// The panel of the visual tree being managed.
        /// </summary>
        public IPanel panel { get; set; }
        
        /// <summary>
        /// The visual tree being managed.
        /// </summary>
        public VisualElement visualTree
        {
            get => m_VisualTree;
            set
            {
                if (m_VisualTree == value)
                    return;
                
                m_VisualTree = value;
                
                if (m_UpdateJob != null)
                {
                    m_UpdateJob.Pause();
                    m_UpdateJob = null;
                }
                
                if (m_VisualTree != null)
                {
                    //OnScreenDebug.Log("m_VisualTree.visualTree");
                    m_UpdateJob = m_VisualTree.schedule.Execute(Update).Every(100);
                }
            }
        }
        
        /// <summary>
        /// Constructor for VisualTreeAccessibilityUpdater.
        /// </summary>
        /// <param name="panel"></param>
        /// <param name="visualTree"></param>
        public VisualTreeAccessibilityUpdater(IPanel panel, VisualElement visualTree, UITkAccessibilityService service)
        {
            this.panel = panel;
            this.visualTree = visualTree;
            hierarchy = default;
            m_RootNextInsertionIndex = 0;
            m_Version++;
            m_AccessibilityService = service;
        }

        void ResetAllInsertionIndex()
        {
            m_RootNextInsertionIndex = 0;
            foreach (var accessibleElement in m_HandlersForElements.Values)
            {
                if (accessibleElement != null)
                    accessibleElement.nextInsertionIndex = 0;
            }
        }

        /// <summary>
        /// Returns true if the specified VisualElement can be made accessible.
        /// </summary>
        /// <param name="element">The element to check</param>
        /// <returns>Returns true if the element has a handler factory</returns>
        public bool IsAccessible(VisualElement element)
        {
            return VisualElementAccessibilityHandlerFactory.HasFactory(element);
        }

        VisualElementAccessibilityHandler CreateHandler(VisualElement element, VisualElementAccessibilityHandler parentElement)
        {
            var accElement = VisualElementAccessibilityHandlerFactory.Create(element);

            m_HandlersForElements[element] = accElement;
            if (accElement != null)
            {
                accElement.change = VisualElementAccessibilityHandler.k_AccessibilityChange;
                element.RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
                accElement.onVersionChanged += OnVersionChanged;
            }

            return accElement;
        }

        void InsertNode(VisualElementAccessibilityHandler parentHandler, VisualElementAccessibilityHandler accHandler)
        {
            var p = parentHandler;
            var parentNode = p?.node;
            // Keep track of insertion index for each parent node
            var index = p != null ? p.nextInsertionIndex : m_RootNextInsertionIndex;
            var node = hierarchy.InsertNode(index, accHandler.ownerElement.name, parentNode);

            accHandler.node = node;
            accHandler.change = VisualElementAccessibilityHandler.k_AccessibilityChange;
            node.label = accHandler.label;
            node.role = accHandler.role;
            m_HandlersForNodes[node] = accHandler;
            if (p != null)
                p.nextInsertionIndex++;
            else
                m_RootNextInsertionIndex++;
            //OnScreenDebug.Log("InsertNode: " + node.id + " \"" + node.label + "\" Role:" + node.role + " State:" + node.state);
        }

        bool MoveNode(VisualElementAccessibilityHandler parentElement, VisualElementAccessibilityHandler accHandler)
        {
            var p = parentElement;
            var parentNode = p?.node;
            var index = p != null ? p.nextInsertionIndex : m_RootNextInsertionIndex;
            bool moved = hierarchy.MoveNode(accHandler.node, parentNode, index);

            if (p != null)
                p.nextInsertionIndex++;
            else
                m_RootNextInsertionIndex++;
            //OnScreenDebug.Log("MoveNode: " + accHandler.node.id + " \"" + accHandler.node.label + "\" At:" + index);
            return moved;
        }

        bool DestroyNode(VisualElementAccessibilityHandler acc)
        {
            bool ret = false;
            
            if (acc.node == null)
                return false;
            
            if (hierarchy.ContainsNode(acc.node))
            {
                hierarchy.RemoveNode(acc.node);
                ret = true;
            }

            m_HandlersForNodes.Remove(acc.node);
            acc.node = null;
            return ret;

            //TODO - DestroyNode recursively for child VisualElements
        }

        public void Dispose()
        {
            m_HandlersForElements.Clear();
            m_HandlersForNodes.Clear();
            m_UpdateJob?.Pause();
            hierarchy.Dispose();
            m_VisualTree = null;
        }

        void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            var ve = evt.target as VisualElement;
            DestroyAccessibleElement(ve);
        }

        void DestroyAccessibleElement(VisualElement ve)
        {
            if (m_HandlersForElements.TryGetValue(ve, out var acc))
            {
                m_HandlersForElements.Remove(ve);
                DestroyNode(acc);

                ve.UnregisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
            }
        }

        static Rect GetScreenPosition(VisualElement ve)
        {
            if (ve == null)
                return Rect.zero;

            var worldRect = ve.worldBound;
            var panel = ve.panel as IRuntimePanel; 
            var panelSettings = panel.panelSettings;

            float scale = panel.scaledPixelsPerPoint;
            return new Rect(worldRect.position * scale, worldRect.size * scale);
            /*
            float panelScale =  1.0f; // Default to 1 if no scaling is applied or calculated

            if (panelSettings != null && panelSettings.scaleMode == PanelScaleMode.ScaleWithScreenSize)
            {
                Vector2 referenceResolution = panelSettings.referenceResolution;
                float currentScreenRatio = (float)Screen.width / Screen.height;
                float referenceScreenRatio = referenceResolution.x / referenceResolution.y;

                if (panelSettings.match == 0) // Match Width
                {
                    panelScale = (float)Screen.width / referenceResolution.x;
                }
                else if (panelSettings.match == 1) // Match Height
                {
                    panelScale = (float)Screen.height / referenceResolution.y;
                }
                else // Match Width or Height (0.5)
                {
                    float widthScale = (float)Screen.width / referenceResolution.x;
                    float heightScale = (float)Screen.height / referenceResolution.y;
                    panelScale = Mathf.Lerp(widthScale, heightScale, panelSettings.match);
                }
            }

            // Convert the worldBound's top-left and bottom-right corners to screen space
            Vector2 screenMin = RuntimePanelUtils.PanelToScreen(ve.panel, worldRect.min);
            Vector2 screenMax = RuntimePanelUtils.PanelToScreen(ve.panel, worldRect.max);

            // Apply the scale factor to the screen coordinates
            // Note: Screen coordinates have Y increasing downwards, so adjust for Rect
            float screenX = screenMin.x * scaleFactor;
            float screenY = (Screen.height - screenMax.y) * scaleFactor; // Adjust for Y-axis
            float screenWidth = (screenMax.x - screenMin.x) * scaleFactor;
            float screenHeight = (screenMax.y - screenMin.y) * scaleFactor;

            Rect screenRect = new Rect(screenX, screenY, screenWidth, screenHeight);
            */
            /* Vector2 corner1 = new Vector2(
                 worldRect.x * panelScale,
                 (Screen.height - (worldRect.y + worldRect.height)) * panelScale // Invert Y and adjust for height
             );*/

            //RectTransformUtility.ScreenPointToLocalPointInRectangle()
            // Get scale of panel from reflection
            //var property = ve.GetType().GetProperty("scale", BindingFlags.NonPublic | BindingFlags.Instance);

            //var scale = (float)property.GetValue(ve.panel);//ve.panel.scale);// 1; //FIX - ve.panel.scale;

            // Convert worldRect to screen space
            //var screenRect = vepanel.context.visualTree.WorldToScreen(worldRect);

            //return new Rect(worldRect.position * scale, worldRect.size * scale);
            //return WorldToScreen(worldRect, Camera.main);//.WWorldToScreenRect(worldRect.center);
        }
        
        public static Rect WorldToScreen(Rect worldRect, Camera camera)
        {
            var elementCorners = new Vector3[4]
            {
                new(worldRect.xMin, worldRect.yMin, 0),
                new(worldRect.xMax, worldRect.yMin, 0),
                new(worldRect.xMax, worldRect.yMax, 0),
                new(worldRect.xMin, worldRect.yMax, 0) 
            };
            var screenCorners = new Vector3[4];

            for (var i = 0; i < elementCorners.Length; i++)
            {
                screenCorners[i] = camera.WorldToScreenPoint(elementCorners[i]);
            }

            GetMinMaxX(screenCorners, out var minX, out var maxX);
            GetMinMaxY(screenCorners, out var minY, out var maxY);

            return new Rect(minX, Screen.height - maxY, maxX - minX, maxY - minY);

            void GetMinMaxX(Vector3[] vector, out float min, out float max)
            {
                min = float.MaxValue;
                max = float.MinValue;

                for (var i = 0; i < vector.Length; ++i)
                {
                    var value = vector[i].x;

                    if (value < min)
                    {
                        min = value;
                    }

                    if (value > max)
                    {
                        max = value;
                    }
                }
            }

            void GetMinMaxY(Vector3[] vector, out float min, out float max)
            {
                min = float.MaxValue;
                max = float.MinValue;

                for (var i = 0; i < vector.Length; ++i)
                {
                    var value = vector[i].y;

                    if (value < min)
                    {
                        min = value;
                    }

                    if (value > max)
                    {
                        max = value;
                    }
                }
            }
        }

        
        void UpdateNode(VisualElementAccessibilityHandler accElement)
        {
            if (!IsNodeValid(accElement.node))
                return;
            accElement.node.isActive = accElement.isActive;
            accElement.node.label = accElement.label;
            accElement.node.role = accElement.role;
            accElement.node.value = accElement.value;
            accElement.node.hint = accElement.hint;
            accElement.node.frame = GetScreenPosition(accElement.ownerElement);
            accElement.node.state = accElement.state;
            accElement.change = 0;
        }

        public void OnVersionChanged(VisualElement ve, VersionChangeType versionChangeType)
        {
            if (!OnVersionChangedInternal(ve, versionChangeType))
                return;
            ++m_Version;
        }
        
        public bool OnVersionChangedInternal(VisualElement ve, VersionChangeType versionChangeType)
        {
            if ((versionChangeType & (VersionChangeType.Transform | VersionChangeType.Size | VersionChangeType.Overflow | VersionChangeType.Hierarchy | VersionChangeType.BorderWidth | VisualElementAccessibilityHandler.k_AccessibilityChange | VersionChangeType.DisableRendering)) == 0)
                return false;

            if (ve != null && m_HandlersForElements.TryGetValue(ve, out var acc))
            {
                if (acc != null)
                   acc.change |= versionChangeType;

               /* if (versionChangeType == VersionChangeType.DisableRendering)
                {
                    //call acc.change on all children recursively
                    foreach (var child in ve.hierarchy.Children())
                    {
                        child.IncrementVersion(VersionChangeType.DisableRendering);
                    }
                }*/
            }

            return true;
        }
        
        public void Update()
        {
            if (visualTree == null)
            {
                m_UpdateJob.Pause();
                return;
            }
            if (m_Version == m_LastVersion)
                return;

            m_LastVersion = m_Version;
            Update(visualTree);
        }
        
        string GetPanelName(VisualElement ve)
        {
            // Try to get the name of the panel using reflection
            var panel = ve.panel;
            if (panel == null)
                return null;
            var panelType = panel.GetType();
            var nameProperty = panelType.GetProperty("name");
            if (nameProperty != null)
                return nameProperty.GetValue(panel, null) as string;
            return null;
        }

        private NotificationType notification = NotificationType.None;
        void Update(VisualElement visualTree)
        {
            bool shouldSendNotification = false;

            if (!Application.isPlaying)
            {
                hierarchy.Dispose();
            }
            else if (m_AccessibilityService != null)
            {
                if (!hierarchy.isValid)
                {
                    var panelName = GetPanelName(visualTree);
                    var rootNode = m_AccessibilityService.hierarchy.AddNode(string.IsNullOrEmpty(panelName)
                        ? visualTree.name
                        : panelName);
                    rootNode.role = AccessibilityRole.Container;
                    rootNode.isActive = false;
                    hierarchy = new AccessibilitySubHierarchy(m_AccessibilityService.hierarchy.mainHierarchy, rootNode);
                    shouldSendNotification = true;
                }

                if (AssistiveSupport.activeHierarchy == null)
                    AssistiveSupport.activeHierarchy = m_AccessibilityService.hierarchy.mainHierarchy;

                m_PaintedBeforeBlocker = true;
                notification = NotificationType.None;
                ResetAllInsertionIndex();
                
                var modalElement = currentModalElement;
                
                UpdateAccessibilityHierarchyRecursively(visualTree, null, false, true);
                // If there is a current modal element or if the current model element has changed then update the active state of all nodes
                if (currentModalElement != null || (currentModalElement != modalElement))
                {
                    m_PaintedBeforeBlocker = true;
                    UpdateActiveStateFromModalityRecursively(visualTree);
                }
            }

            if (shouldSendNotification || notification.HasFlag(NotificationType.ScreenChanged))
            {
                visualTree.schedule.Execute(() => AssistiveSupport.notificationDispatcher.SendScreenChanged());
            }
            else if (notification.HasFlag(NotificationType.LayoutChanged))
            {
                visualTree.schedule.Execute(() => AssistiveSupport.notificationDispatcher.SendLayoutChanged());
            }
        }

        bool IsNodeValid(AccessibilityNode node)
        {
            return node != null && hierarchy.ContainsNode(node);
        }

        void UpdateActiveStateFromModalityRecursively(VisualElement element)
        {
            if (currentModalElement == element)
                m_PaintedBeforeBlocker = false;
            
            m_HandlersForElements.TryGetValue(element, out var accElement);

            // Use the last valid parent
            if (accElement != null)
            {
                if (accElement.node != null)
                {
                    accElement.node.isActive = !(hasBlocker && m_PaintedBeforeBlocker) && accElement.isActive;
                }
            }

            foreach (var child in element.hierarchy.Children())
            {
                UpdateActiveStateFromModalityRecursively(child);
            }
        }
            
        void UpdateAccessibilityHierarchyRecursively(VisualElement element,
            VisualElementAccessibilityHandler parentAccessible,
            bool forced,
            bool visible)
        {
            // Remove all the created nodes if this branch is not visible
            visible &= VisualElementAccessibilityHandler.IsElementVisible(element);

            if (!m_HandlersForElements.TryGetValue(element, out var accElement) && visible)
            {
                accElement = CreateHandler(element, parentAccessible);
            }

            bool shouldBeIgnored = !visible;

            // Use the last valid parent
            if (accElement != null)
            {
                shouldBeIgnored |= accElement.isIgnored;
                // If the branch is visible but the AccessibleElement does not have a node yet then create it.
                if (!shouldBeIgnored)
                {
                    if (!IsNodeValid(accElement.node))
                    {
                        notification |= NotificationType.ScreenChanged;
                        InsertNode(parentAccessible, accElement);
                    }
                    else
                    {
                        if (MoveNode(parentAccessible, accElement))
                            notification |= NotificationType.LayoutChanged;
                    }
                }
                else
                {
                    if (accElement.node != null)
                    {
                        DestroyNode(accElement);
                    }
                }
                
                if (accElement.change != 0 || forced)
                {
                    UpdateNode(accElement);
                    forced |= accElement.change.HasFlag(VersionChangeType.Transform | VersionChangeType.Size); // force update if it affect the geometry to recompute the screen positioning
                    notification |= NotificationType.LayoutChanged;
                }
                if (IsNodeValid(accElement.node))
                    parentAccessible = accElement;
            }

            // Update the model element
            if (!shouldBeIgnored && IsModal(element))
            {
                currentModalElement = element;
            }
            else if (currentModalElement == element)
            {
                currentModalElement = null;
            }

            foreach (var child in element.hierarchy.Children())
            {
                UpdateAccessibilityHierarchyRecursively(child, parentAccessible, forced, visible);
            }
        }

        private VisualElement m_CurrentModalElement;

        private bool hasBlocker => currentModalElement != null;
        private VisualElement currentModalElement
        {
            get => m_CurrentModalElement;
            set
            {
                if (m_CurrentModalElement == value)
                    return;

                m_CurrentModalElement?.UnregisterCallback<DetachFromPanelEvent>(OnCurrentModalElementDetachedFromPanel);
                m_CurrentModalElement = value;
                m_CurrentModalElement?.RegisterCallback<DetachFromPanelEvent>(OnCurrentModalElementDetachedFromPanel);
            }
        }

        //need to trigger a repaint
        void OnCurrentModalElementDetachedFromPanel(DetachFromPanelEvent ev)
        {
            currentModalElement = null;

            //loop through all element and mark them as dirty
            foreach (VisualElementAccessibilityHandler accHandler in m_HandlersForElements.Values)
            {
                //mark as dirty
                accHandler?.NotifyChange();
            }
        }

        bool IsModal(VisualElement element)
        {
            return element.ClassListContains(GenericDropdownMenu.ussClassName)
                || element.ClassListContains("unity-modal");
        }
    }
}