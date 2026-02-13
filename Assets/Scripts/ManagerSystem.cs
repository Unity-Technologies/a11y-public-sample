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
            if (!GameObject.Find(nameof(AccessibilityManager)))
            {
                var gameObject = new GameObject(nameof(AccessibilityManager));
                gameObject.AddComponent<AccessibilityManager>();
            }

            if (!GameObject.Find(nameof(AudioManager)))
            {
                var gameObject = new GameObject(nameof(AudioManager));
                gameObject.AddComponent<AudioManager>();
            }

            if (!GameObject.Find(nameof(FontManager)))
            {
                var gameObject = new GameObject(nameof(FontManager));
                gameObject.AddComponent<FontManager>();
            }

            if (!GameObject.Find(nameof(ClosedCaptionsManager)))
            {
                var gameObject = new GameObject(nameof(ClosedCaptionsManager));
                gameObject.AddComponent<ClosedCaptionsManager>();
            }

            if (!GameObject.Find(nameof(SceneTransitionManager)))
            {
                var gameObject = new GameObject(nameof(SceneTransitionManager));
                gameObject.AddComponent<SceneTransitionManager>();
            }
        }
    }
}
