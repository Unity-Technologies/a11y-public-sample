using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Accessibility;
using UnityEngine.Pool;

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
        /// The static instance of this class that allows other scripts to update the accessibility hierarchy as
        /// necessary.
        /// </summary>
        static AccessibilityManager s_Instance;

        /// <summary>
        /// The current accessibility hierarchy.
        /// </summary>
        AccessibilityHierarchy m_Hierarchy;

        /// <summary>
        /// Tracks the previous screen orientation (portrait/landscape) to allow the layout to be recalculated on
        /// orientation changes. This is necessary for the calculated accessibility frames to be correct.
        /// </summary>
        ScreenOrientation m_PreviousOrientation;

        /// <summary>
        /// The list of registered accessibility services that can contribute to the accessibility hierarchy.
        /// </summary>
        List<AccessibilityService> m_RegisteredServices = new();

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

        /// <summary>
        /// Recreates the whole accessibility hierarchy.
        /// </summary>
        static void RefreshHierarchy()
        {
            s_Instance.RebuildHierarchy();
        }

        /// <summary>
        /// Recreates the whole accessibility sub hierarchy associated to the specified service.
        /// </summary>
        public static void RefreshHierarchy(AccessibilityService service)
        {
            s_Instance.RebuildHierarchy(service);

            /*
            AssistiveSupport.activeHierarchy = hierarchy;
            service.CleanUp();
            RegenerateNodes(service);
            */
        }

        /// <summary>
        /// Returns the registered accessibility service of the specified type or null if no such service was registered.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T GetService<T>() where T : AccessibilityService
        {
            if (s_Instance == null)
            {
                return null;
            }

            foreach (var system in s_Instance.m_RegisteredServices)
            {
                if (system is T foundSystem)
                {
                    return foundSystem;
                }
            }

            return null;
        }

        /// <summary>
        /// Adds a new accessibility service to the list of registered services.
        /// </summary>
        /// <param name="service"></param>
        public static void AddService(AccessibilityService service)
        {
            if (s_Instance == null)
            {
                throw new InvalidOperationException("The AccessibilityManager instance is not available. Make sure " +
                    "there is an AccessibilityManager component in the scene.");
            }

            if (service == null)
            {
                throw new ArgumentNullException(nameof(service));
            }

            if (s_Instance.m_RegisteredServices.Contains(service))
            {
                throw new ArgumentException("The specified service is already registered.", nameof(service));
            }

            s_Instance.m_RegisteredServices.Add(service);
        }

        /// <summary>
        /// Removes an accessibility service from the list of registered services.
        /// </summary>
        /// <param name="service"></param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public static void RemoveService(AccessibilityService service)
        {
            if (s_Instance == null)
            {
                throw new InvalidOperationException("The AccessibilityManager instance is not available. Make sure " +
                    "there is an AccessibilityManager component in the scene.");
            }

            if (service == null)
            {
                throw new ArgumentNullException(nameof(service));
            }

            if (!s_Instance.m_RegisteredServices.Contains(service))
            {
                throw new ArgumentException("The specified service is not registered.", nameof(service));
            }

            service.CleanUp();
            service.hierarchy.Dispose();
            s_Instance.m_RegisteredServices.Remove(service);
        }

        /// <summary>
        /// Called every frame to check for orientation changes.
        /// </summary>
        void Update()
        {
            // Rebuild the hierarchy on orientation change.
            if (m_PreviousOrientation != Screen.orientation)
            {
                m_PreviousOrientation = Screen.orientation;

                StartCoroutine(OnOrientationChanged());
            }

            // Update the services.
            foreach (var service in m_RegisteredServices)
            {
                service.Update();
            }
        }

        void OnEnable()
        {
            s_Instance = this;

            AssistiveSupport.screenReaderStatusOverride = AssistiveSupport.ScreenReaderStatusOverride.ForceEnabled;
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
            // Wait until the end of the frame to make sure all the GUI positions have been calculated.
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

        void RebuildHierarchy(AccessibilityService service)
        {
            AssistiveSupport.activeHierarchy = null;

            var lastLoadedScene = GetLastLoadedScene();

            if (lastLoadedScene.IsValid())
            {
                StartCoroutine(DelayRebuildHierarchy(service, lastLoadedScene));
            }
        }

        IEnumerator DelayRebuildHierarchy( AccessibilityService service, Scene scene)
        {
            // Always wait for the end of the frame to guarantee that all the GUI positions have been set.
            yield return new WaitForEndOfFrame();

            service.CleanUp();
            RegenerateNodes(service, scene);
            AssistiveSupport.activeHierarchy = hierarchy;

            hierarchyRefreshed?.Invoke();
        }

        void GenerateHierarchy(Scene scene)
        {
            LoadServices();

            // Clears all nodes
            hierarchy.Clear();

            using var _ = ListPool<AccessibilityService>.Get(out var sortedServices);

            sortedServices.AddRange(m_RegisteredServices);
            sortedServices.Sort((a, b) => b.servicePriority.CompareTo(a.servicePriority));

            // Release all root nodes created by registered services in the previous hierarchy generation.
            foreach (var service in sortedServices)
            {
                service.CleanUp();
                service.hierarchy.Dispose();
            }

            foreach (var service in sortedServices)
            {
                CreateSubHierarchyForService(service, hierarchy);
            }

            foreach (var system in sortedServices)
            {
                RegenerateNodes(system, scene);
            }
        }

        void CreateSubHierarchyForService(AccessibilityService service, AccessibilityHierarchy hierarchy)
        {
            var rootNode = hierarchy.AddNode(service.serviceName);
            rootNode.role = AccessibilityRole.Container;
            rootNode.isActive = (Application.platform == RuntimePlatform.OSXPlayer);//false;

            service.hierarchy = new AccessibilitySubHierarchy(hierarchy, rootNode);
        }

        void LoadServices()
        {
            if (m_RegisteredServices.Count > 0)
            {
                return;
            }

            AddService(new UGuiAccessibilityService());
            AddService(new UITkAccessibilityService());
        }

        static void RegenerateNodes(AccessibilityService service, Scene scene)
        {
            service.hierarchy.Clear();
            service.SetUp(scene);
        }
    }
}
