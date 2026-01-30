using Unity.Samples.ClosedCaptions;
using Unity.Samples.ScreenReader;
using UnityEngine;

namespace Unity.Samples.LetterSpell
{
    /// <summary>
    /// This class loads on initialization and creates the necessary manager instances in the scene.
    /// </summary>
    static class ManagerSystem
    {
        [RuntimeInitializeOnLoadMethod]
        static void Initialize()
        {
            var accessibilityManagerObject = new GameObject("Accessibility Manager");
            accessibilityManagerObject.AddComponent<AccessibilityManager>();

            var audioManagerObject = new GameObject("Audio Manager");
            audioManagerObject.AddComponent<AudioManager>();

            var ccManagerObject = new GameObject("Closed Captions Manager");
            ccManagerObject.AddComponent<ClosedCaptionsManager>();

            var sceneTransitionManagerObject = new GameObject("Scene Transition Manager");
            sceneTransitionManagerObject.AddComponent<SceneTransitionManager>();
        }
    }
}
