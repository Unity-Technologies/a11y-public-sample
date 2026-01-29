using UnityEngine;

namespace Unity.Samples.LetterSpell
{
    static class SceneTransitionSystem
    {
        [RuntimeInitializeOnLoadMethod]
        static void Initialize()
        {
            var gameObject = new GameObject("Scene Transition Manager");
            gameObject.AddComponent<SceneTransitionManager>();
        }
    }
}
