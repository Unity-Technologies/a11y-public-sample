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

        // const float m_NarratorStatusCheckInterval = 1.0f;
        // const float m_TimeSinceLastNarratorStatusCheck = 0.0f;

        /// <summary>
        /// The static instance of this class that allows other scripts to update the accessibility hierarchy as
        /// necessary.
        /// </summary>
        static AccessibilityManager s_Instance;

        // public static event Action<bool> narratorStatusChanged;

        AccessibilityHierarchy m_Hierarchy;

        /// <summary>
        /// The current accessibility hierarchy.
        /// </summary>
        public static AccessibilityHierarchy hierarchy => s_Instance.m_Hierarchy ??= new AccessibilityHierarchy();

        /// <summary>
        /// Event triggered when the hierarchy is refreshed to allow components to be able to execute actions when that
        /// happens (e.g. focusing the dropdown after it opens).
        /// </summary>
        public static event Action hierarchyRefreshed;

        /// <summary>
        /// Mapping from the AccessibilityNode from the accessibility hierarchy to the MonoBehavior instance it was
        /// created from. This is often necessary to access information about the node, like its transform to calculate
        /// positions, for example.
        /// </summary>
        Dictionary<AccessibilityNode, AccessibleElement> m_NodeToElement = new();

        bool m_IsNarratorEnabled;

        /// <summary>
        /// Tracks the previous screen orientation (portrait/landscape) to allow the layout to be recalculated on
        /// orientation changes. This is necessary for the calculated accessibility frames to be correct.
        /// </summary>
        ScreenOrientation m_PreviousOrientation;

        void OnEnable()
        {
            s_Instance = this;

            DontDestroyOnLoad(gameObject);

            // As scenes get loaded/unloaded, the accessibility hierarchy must be updated.
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;

            // No-performance-impact alternative to the Narrator status polling workaround in Update().
            if (Application.platform == RuntimePlatform.WindowsPlayer)
            {
                AssistiveSupport.screenReaderStatusOverride = AssistiveSupport.ScreenReaderStatusOverride.ForceEnabled;
            }

            // The accessibility hierarchy must be created when the screen reader is turned on and destroyed when the
            // screen reader is turned off.
            AssistiveSupport.screenReaderStatusChanged += OnScreenReaderStatusChanged;
            // narratorStatusChanged += OnScreenReaderStatusChanged;

            // Generate the accessibility hierarchy for the current scene and set it to AssistiveSupport.activeHierarchy
            // so that the screen reader can use it.
            RebuildHierarchy();
        }

        void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneUnloaded -= OnSceneUnloaded;

            AssistiveSupport.screenReaderStatusChanged -= OnScreenReaderStatusChanged;
            // narratorStatusChanged -= OnScreenReaderStatusChanged;

            AssistiveSupport.activeHierarchy = null;

            s_Instance = null;
        }

        void Update()
        {
#if UNITY_6000_3_OR_NEWER
            // Poll Narrator's status because it does not send AssistiveSupport.screenReaderStatusChanged events (low
            // performance).

            //if (Application.platform == RuntimePlatform.WindowsPlayer)
            //{
            //    m_TimeSinceLastNarratorStatusCheck += Time.deltaTime;

            //    if (m_TimeSinceLastNarratorStatusCheck >= m_NarratorStatusCheckInterval)
            //    {
            //        if (m_IsNarratorEnabled != AssistiveSupport.isScreenReaderEnabled)
            //        {
            //            m_IsNarratorEnabled = AssistiveSupport.isScreenReaderEnabled;

            //            narratorStatusChanged.Invoke(m_IsNarratorEnabled);
            //        }

            //        m_TimeSinceLastNarratorStatusCheck = 0.0f;
            //    }
            //}
#endif // UNITY_6000_3_OR_NEWER

            // Rebuild the hierarchy on orientation change.
            if (m_PreviousOrientation != Screen.orientation)
            {
                if (m_PreviousOrientation != 0)
                {
                    OnOrientationChanged();
                }

                m_PreviousOrientation = Screen.orientation;
            }
        }

        static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            RebuildHierarchy();
        }

        static void OnSceneUnloaded(Scene scene)
        {
            AssistiveSupport.activeHierarchy = null;
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

        static void OnOrientationChanged()
        {
            RebuildHierarchy();
        }

        static void OnScreenReaderStatusChanged(bool on)
        {
            if (on)
            {
                // If the screen reader was turned on, generate and set the accessibility hierarchy.
                RebuildHierarchy();
            }
            else
            {
                // If the screen reader was turned off, remove the accessibility hierarchy.
                AssistiveSupport.activeHierarchy = null;
            }
        }

        public static AccessibleElement GetAccessibleElementForNode(AccessibilityNode node)
        {
            return s_Instance.m_NodeToElement.GetValueOrDefault(node);
        }

        public static void AddToHierarchy(AccessibleElement element, AccessibilityNode parent, int index = -1)
        {
            var node = hierarchy.InsertNode(index, element.label, parent);

            element.node = node;
            s_Instance.m_NodeToElement[node] = element;
        }

        public static void RemoveFromHierarchy(AccessibleElement element, bool removeChildren = true)
        {
            if (removeChildren)
            {
                foreach (var child in element.node.children)
                {
                    s_Instance.m_NodeToElement.Remove(child);
                }
            }

            s_Instance.m_NodeToElement.Remove(element.node);

            hierarchy.RemoveNode(element.node, removeChildren);
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

        public static void RebuildHierarchy()
        {
            if (!Application.isEditor && !AssistiveSupport.isScreenReaderEnabled)
            {
                return;
            }

            var lastLoadedScene = GetLastLoadedScene();

            if (lastLoadedScene.IsValid())
            {
                s_Instance.StartCoroutine(s_Instance.DelayRebuildHierarchy(lastLoadedScene));
            }
        }

        IEnumerator DelayRebuildHierarchy(Scene scene)
        {
            // Wait for the end frame before generating the hierarchy to make sure the layout has been computed.
            yield return new WaitForEndOfFrame();

            GenerateHierarchy(scene);

            if (AssistiveSupport.activeHierarchy == null)
            {
                AssistiveSupport.activeHierarchy = hierarchy;
            }
            else
            {
                AssistiveSupport.notificationDispatcher.SendScreenChanged();
            }

            hierarchyRefreshed?.Invoke();
        }

        void GenerateHierarchy(Scene scene)
        {
            hierarchy.Clear();
            m_NodeToElement.Clear();

            var components = FindObjectsByType<AccessibleElement>(FindObjectsInactive.Include,
                FindObjectsSortMode.None);

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

                // If this is a root element, or it's the first of its ancestors to be an AccessibleElement, add it as a
                // root node of the hierarchy.
                if (elementObject.parent == null ||
                    elementObject.parent.GetComponentInParent<AccessibleElement>() == null)
                {
                    node = hierarchy.AddNode(element.label);
                }
                else if (hierarchyStack.Count > 0)
                {
                    var item = hierarchyStack.Pop();

                    // Pop until we empty the hierarchy stack or find a pair with one of the element's ancestors.
                    while (hierarchyStack.Count > 0 && !elementObject.IsChildOf(item.transform))
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

                    element.node = node;
                    m_NodeToElement[node] = element;
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

                var component = currentObject.GetComponent<AccessibleElement>();

                // If the node is an AccessibleElement, add it to the list.
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

            if (canvas == null)
            {
                return default;
            }

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
    }
}
