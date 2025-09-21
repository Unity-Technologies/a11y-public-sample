using UnityEngine.Accessibility;
using UnityEngine.SceneManagement;

namespace Unity.Samples.ScreenReader
{
    /// <summary>
    /// Base class for Accessibility services that provide accessibility support for a specific system (e.g. UI Toolkit, legacy UI, custom systems).
    /// </summary>
    public abstract class AccessibilityService
    {
        /// <summary>
        /// The accessibility sub hierarchy that this service operates on.
        /// </summary>
        public AccessibilitySubHierarchy hierarchy { get; set; }

        /// <summary>
        /// The priority of the service. Higher priority services will have their nodes added to the accessibility
        /// hierarchy first.
        /// </summary>
        public int servicePriority { get; protected set; } = 0;

        /// <summary>
        /// The name of the system
        /// </summary>
        public string serviceName { get; protected set; }

        /// <summary>
        /// Constructor for the accessibility service.
        /// </summary>
        /// <param name="serviceName">The name of the service</param>
        /// <param name="servicePriority">The priority of the service</param>
        public AccessibilityService(string serviceName, int servicePriority = 0)
        {
            this.serviceName = serviceName;
            this.servicePriority = servicePriority;
        }
        
        /// <summary>
        /// Set up the services passing the specified scene.
        /// This is called when the accessibility hierarchy needs to be rebuilt (e.g. when the
        /// screen reader is enabled, or when the scene changes).
        /// </summary>
        /// <param name="activeScene"></param>
        public abstract void SetUp(Scene activeScene);
        
        /// <summary>
        /// Cleans up any internal resources of the service.
        /// </summary>
        public abstract void CleanUp();

        /// <summary>
        /// Refreshes the accessibility hierarchy by calling the AccessibilityManager to rebuild it.
        /// </summary>
        public void RebuildHierarchy()
        {
            AccessibilityManager.RefreshHierarchy(this);
        }
    }
}