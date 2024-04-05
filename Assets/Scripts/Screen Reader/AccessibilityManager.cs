using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Accessibility;

namespace Unity.Samples.ScreenReader
{
    /// <summary>
    /// This class is added to the scene in order to detect the screen reader being turned on/off and automatically
    /// convert the GUI from the game object hierarchy into the accessibility hierarchy (data model) that the screen
    /// reader needs.
    /// The accessibility hierarchy order reflects the order of the game object hierarchy.
    /// </summary>
    public class AccessibilityManager : MonoBehaviour
    {
        /// <summary>
        /// Utility struct to help translate the game object hierarchy into the accessibility hierarchy.
        /// </summary>
        struct HierarchyItem
        {
            public Transform transform;
            public AccessibilityNode node;
        }

        /// <summary>
        /// The static instance of this class that allows other scripts to update the accessibility hierarchy as
        /// necessary.
        /// </summary>
        static AccessibilityManager s_Instance;

        /// <summary>
        /// The current accessibility hierarchy.
        /// </summary>
        AccessibilityHierarchy m_Hierarchy;

        /// <summary>
        /// Mapping from the AccessibilityNode from the accessibility hierarchy to the MonoBehavior instance it was
        /// created from. This is often necessary to access information about the node, like its transform to calculate
        /// positions, for example.
        /// </summary>
        Dictionary<AccessibilityNode, AccessibleElement> m_ElementForNodeMap = new();

        /// <summary>
        /// Tracks the previous screen orientation (portrait/landscape) to allow the layout to be recalculated on
        /// orientation changes. This is necessary for the calculated accessibility frames to be correct.
        /// </summary>
        ScreenOrientation m_PreviousOrientation;

        public static AccessibilityHierarchy hierarchy
        {
            get
            {
                s_Instance.m_Hierarchy ??= new AccessibilityHierarchy();
                return s_Instance.m_Hierarchy;
            }
        }

        /// <summary>
        /// Event triggered when the hierarchy is refreshed to allow components to be able to execute actions when that
        /// happens (e.g. focusing the dropdown after it opens).
        /// </summary>
        public static event Action hierarchyRefreshed;

        public static AccessibleElement GetAccessibleElementForNode(AccessibilityNode node)
        {
            return s_Instance.m_ElementForNodeMap.TryGetValue(node, out var element) ? element : null;
        }

        /// <summary>
        /// Recreates the whole accessibility hierarchy.
        /// </summary>
        public static void RefreshHierarchy()
        {
            s_Instance.RebuildHierarchy();
        }

        /// <summary>
        /// Default method for calculating the Rect representing the frame for the given RectTransform, which comes from
        /// a GUI element (e.g. a Button instance). The screen reader uses this frame to highlight the area on the
        /// screen when the corresponding accessibility node is focused.
        /// </summary>
        /// <param name="rectTransform">The RectTransform of the GUI element.</param>
        /// <returns>The Rect representing the position of the GUI element on the screen.</returns>
        public static Rect GetFrame(RectTransform rectTransform)
        {
            var canvas = rectTransform.GetComponentInParent<Canvas>();
            var camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            var elementCorners = new Vector3[4];
            var screenCorners = new Vector3[4];

            rectTransform.GetWorldCorners(elementCorners);

            for (var i = 0; i < elementCorners.Length; i++)
            {
                screenCorners[i] = RectTransformUtility.WorldToScreenPoint(camera, elementCorners[i]);
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

        /// <summary>
        /// Deactivates or restores the active state of all accessibility nodes that are not children of the given
        /// transform.
        /// </summary>
        public static void ActivateOtherAccessibilityNodes(bool activate, Transform transform)
        {
            var elements = FindObjectsByType<AccessibleElement>(FindObjectsSortMode.None);

            foreach (var element in elements)
            {
                if (element.transform.IsChildOf(transform))
                {
                    continue;
                }

                element.node.isActive = activate && element.isActive;
            }
        }

        void Update()
        {
            // Rebuild the hierarchy on orientation change.
            if (m_PreviousOrientation != Screen.orientation)
            {
                m_PreviousOrientation = Screen.orientation;

                StartCoroutine(OnOrientationChanged());
            }
        }

        void OnEnable()
        {
            s_Instance = this;

            DontDestroyOnLoad(gameObject);
            StartCoroutine(DelayInitialize());
        }

        void OnDisable()
        {
            AssistiveSupport.activeHierarchy = null;

            s_Instance = null;
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            StartCoroutine(DelayRebuildHierarchy(scene));
        }

        static Scene GetLastLoadedScene()
        {
            Scene lastLoadedScene = default;

            for (var i = SceneManager.sceneCount - 1; i >= 0; i--)
            {
                var scene = SceneManager.GetSceneAt(i);

                if (scene.isLoaded)
                {
                    lastLoadedScene = scene;
                    break;
                }
            }

            return lastLoadedScene;
        }

        void OnSceneUnloaded(Scene scene)
        {
            AssistiveSupport.activeHierarchy = null;

            var lastLoadedScene = GetLastLoadedScene();
            
            if (lastLoadedScene.IsValid())
            {
                StartCoroutine(DelayRebuildHierarchy(lastLoadedScene));
            }
        }

        IEnumerator DelayInitialize()
        {
            // Wait until the end of the frame to make sure all of the GUI positions have been calculated.
            yield return new WaitForEndOfFrame();

            // As scenes get loaded/unloaded, the accessibility hierarchy must be updated.
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;

            // The accessibility hierarchy must be created when the screen reader is turned on and destroyed when the
            // screen reader is turned off.
            AssistiveSupport.screenReaderStatusChanged += OnScreenReaderStatusChanged;

            // Generate the accessibility hierarchy for the current scene and set it to AssistiveSupport.activeHierarchy
            // so that the screen reader can use it.
            var lastLoadedScene = GetLastLoadedScene();
            GenerateHierarchy(lastLoadedScene);
            AssistiveSupport.activeHierarchy = hierarchy;
        }

        IEnumerator OnOrientationChanged()
        {
            yield return new WaitForEndOfFrame();

            RebuildHierarchy();
        }

        void OnScreenReaderStatusChanged(bool status)
        {
            if (status)
            {
                // If the screen reader was turned on, generate and set the accessibility hierarchy.
                var lastLoadedScene = GetLastLoadedScene();

                if (lastLoadedScene.IsValid())
                {
                    OnSceneLoaded(lastLoadedScene, default);
                }
            }
            else
            {
                // If the screen reader was turned off, remove the accessibility hierarchy.
                AssistiveSupport.activeHierarchy = null;
            }
        }

        void RebuildHierarchy()
        {
            AssistiveSupport.activeHierarchy = null;

            var lastLoadedScene = GetLastLoadedScene();

            if (lastLoadedScene.IsValid())
            {
                OnSceneLoaded(lastLoadedScene, default);
            }
        }

        IEnumerator DelayRebuildHierarchy(Scene scene)
        {
            // Always wait for the end of the frame to guarantee that all the GUI positions have been set.
            yield return new WaitForEndOfFrame();

            GenerateHierarchy(scene);
            AssistiveSupport.activeHierarchy = hierarchy;

            hierarchyRefreshed?.Invoke();
        }

        static void SetNodeProperties(AccessibilityNode node, AccessibleElement element)
        {
            element.node = node;
            element.SetNodeProperties();

            node.frameGetter = element.GetFrame;

            node.focusChanged += element.InvokeFocusChanged;
            node.incremented += element.InvokeIncremented;
            node.decremented += element.InvokeDecremented;
        }

        void GenerateHierarchy(Scene scene)
        {
            hierarchy.Clear();
            m_ElementForNodeMap.Clear();

            var components = FindObjectsByType<AccessibleElement>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            if (components == null || components.Length == 0)
            {
                return;
            }

            var elements = new List<AccessibleElement>();
            HashSet<Transform> visitedObjects = new();

            // The order of the hierarchy of game objects in the scene is what determines the order of the accessibility
            // hierarchy. The order in the accessibility hierarchy is important to guarantee the navigation order when
            // using the screen reader.
            foreach (var component in components)
            {
                if (component.gameObject.scene != scene)
                {
                    continue;
                }

                // Start the recursion on each root in the scene.
                Traverse(component.transform.root);
            }

            Stack<HierarchyItem> hierarchyStack = new();

            foreach (var element in elements)
            {
                if (!element.enabled)
                {
                    continue;
                }

                var elementObject = element.transform;
                AccessibilityNode node = null;

                // If this is a root element or it is the first of its ancestors to be an AccessibleElement, add it as a
                // root node of the hierarchy.
                if (elementObject.parent == null || elementObject.parent.GetComponentInParent<AccessibleElement>() == null)
                {
                    node = hierarchy.AddNode(element.label);
                }
                else if (hierarchyStack.Count > 0)
                {
                    var item = hierarchyStack.Pop();

                    // Pop until we empty the hierarchy stack or we find a pair with one of the element's ancestors.
                    while (hierarchyStack.Count > 0 && elementObject.IsChildOf(item.transform) == false)
                    {
                        item = hierarchyStack.Pop();
                    }

                    if (elementObject.IsChildOf(item.transform))
                    {
                        // The AccessibleElement might have other descendants, so push it back to the stack.
                        hierarchyStack.Push(item);
                        node = hierarchy.AddNode(element.label, item.node);
                    }
                    else
                    {
                        node = hierarchy.AddNode(element.label);
                    }
                }

                // If we added a node to the hierarchy, push it to the hierarchy and set its properties.
                if (node != null)
                {
                    var item = new HierarchyItem
                    {
                        transform = elementObject,
                        node = node
                    };

                    hierarchyStack.Push(item);

                    SetNodeProperties(node, element);
                    m_ElementForNodeMap[node] = element;
                }
            }

            return;

            void Traverse(Transform currentObject)
            {
                // If we already traversed this node, break the recursion.
                if (visitedObjects.Contains(currentObject))
                {
                    return;
                }

                // Mark the node as visited.
                visitedObjects.Add(currentObject);

                // If the node is an AccessibleElement, add it to the list.
                var component = currentObject.GetComponent<AccessibleElement>();
                if (component != null)
                {
                    elements.Add(component);
                }

                var children = currentObject.GetComponentsInChildren<Transform>(true);

                // Recurse over all the children of the node.
                foreach (var child in children)
                {
                    Traverse(child);
                }
            }
        }
    }
}
