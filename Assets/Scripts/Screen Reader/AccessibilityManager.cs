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

        // bool m_IsNarratorEnabled;
        // float m_NarratorStatusCheckInterval = 1.0f;
        // float m_TimeSinceLastNarratorStatusCheck = 0.0f;

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

        public static AccessibilityHierarchy hierarchy => s_Instance.m_Hierarchy ??= new AccessibilityHierarchy();

        // public static event Action<bool> narratorStatusChanged;

        /// <summary>
        /// Event triggered when the hierarchy is refreshed to allow components to be able to execute actions when that
        /// happens (e.g. focusing the dropdown after it opens).
        /// </summary>
        public static event Action hierarchyRefreshed;

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

            // Update the services.
            foreach (var service in m_RegisteredServices)
            {
                service.Update();
            }
        }

        void OnEnable()
        {
            s_Instance = this;

            DontDestroyOnLoad(gameObject);

            // As scenes get loaded/unloaded, the accessibility hierarchy must be updated.
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;

            // No-performance-impact alternative to the Narrator status polling workaround in Update().
            // if (Application.platform == RuntimePlatform.WindowsPlayer)
            // {
                AssistiveSupport.screenReaderStatusOverride = AssistiveSupport.ScreenReaderStatusOverride.ForceEnabled;
            // }

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

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            RebuildHierarchyForScene(scene);
        }

        void OnSceneUnloaded(Scene scene)
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

        void OnOrientationChanged()
        {
            RebuildHierarchy();
        }

        void OnScreenReaderStatusChanged(bool on)
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

        static void RebuildHierarchy()
        {
            RebuildHierarchy(null);
        }

        /// <summary>
        /// Recreates the whole accessibility sub-hierarchy associated with the specified service.
        /// </summary>
        public static void RebuildHierarchy(AccessibilityService service)
        {
            var lastLoadedScene = GetLastLoadedScene();

            if (lastLoadedScene.IsValid())
            {
                s_Instance.RebuildHierarchyForScene(lastLoadedScene, service);
            }
        }

        void RebuildHierarchyForScene(Scene scene, AccessibilityService service = null)
        {
            if (!Application.isEditor && !AssistiveSupport.isScreenReaderEnabled)
            {
                return;
            }

            if (service == null)
            {
                // Rebuild the whole hierarchy (for all registered services).
                GenerateHierarchy(scene);
            }
            else
            {
                // Rebuild only the specified service's sub-hierarchy.
                service.CleanUp();
                GenerateSubHierarchyForService(scene, service);
            }

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
            // Clear all nodes.
            hierarchy.Clear();

            LoadServices();

            using var _ = ListPool<AccessibilityService>.Get(out var sortedServices);

            sortedServices.AddRange(m_RegisteredServices);
            sortedServices.Sort((a, b) => b.servicePriority.CompareTo(a.servicePriority));

            // Release all root nodes created by registered services in the previous hierarchy generation.
            foreach (var service in sortedServices)
            {
                RemoveSubHierarchyForService(service);
            }

            // Create a root node for each registered service.
            foreach (var service in sortedServices)
            {
                CreateSubHierarchyForService(service);
            }

            // Let each service populate its sub-hierarchy.
            foreach (var service in sortedServices)
            {
                GenerateSubHierarchyForService(scene, service);
            }
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

        static void RemoveSubHierarchyForService(AccessibilityService service)
        {
            service.CleanUp();
            service.hierarchy.Dispose();
        }

        static void CreateSubHierarchyForService(AccessibilityService service)
        {
            service.hierarchy = new AccessibilitySubHierarchy(hierarchy);
        }

        static void GenerateSubHierarchyForService(Scene scene, AccessibilityService service)
        {
            service.hierarchy.Clear();
            service.SetUp(scene);
        }
    }
}
