using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Samples.LetterSpell
{
    public class Intro2ViewController : MonoBehaviour
    {
        [Header("uGUI References")]
        public UnityEngine.UI.Button uguiOptionsButton;
        public UnityEngine.UI.Button uguiPlayEasyButton;
        public UnityEngine.UI.Button uguiPlayHardButton;

        [Header("UI Toolkit References")]
        public UIDocument uitkDocument;

        Button m_UitkOptionsButton;
        Button m_UitkPlayEasyButton;
        Button m_UitkPlayHardButton;

        bool m_UseUIToolkit;

        void OnEnable()
        {
            m_UseUIToolkit = PlayerPrefs.GetInt(UISystemToggler.useUIToolkitPreference) == 1;

            SetupUI();
        }

        void OnDisable()
        {
            CleanupUI();
        }

        void SetupUI()
        {
            if (m_UseUIToolkit)
            {
                if (uitkDocument == null)
                {
                    Debug.LogError($"{nameof(uitkDocument)} is not assigned for {GetType().Name}.");
                    return;
                }

                var root = uitkDocument.rootVisualElement;

                root.dataSource = PlayerSettingsDataSource.Acquire();

                m_UitkOptionsButton = root.Q<Button>("options-button");
                m_UitkOptionsButton.clicked += SceneTransitionManager.LoadSettingsScene;

                m_UitkPlayEasyButton = root.Q<Button>("play-easy-button");
                m_UitkPlayEasyButton.clicked += SceneTransitionManager.PlayEasy;

                m_UitkPlayHardButton = root.Q<Button>("play-hard-button");
                m_UitkPlayHardButton.clicked += SceneTransitionManager.PlayHard;
            }
            else
            {
                if (uguiOptionsButton == null)
                {
                    Debug.LogError($"{nameof(uguiOptionsButton)} is not assigned for {GetType().Name}.");
                }

                if (uguiPlayEasyButton == null)
                {
                    Debug.LogError($"{nameof(uguiPlayEasyButton)} is not assigned for {GetType().Name}.");
                }

                if (uguiPlayHardButton == null)
                {
                    Debug.LogError($"{nameof(uguiPlayHardButton)} is not assigned for {GetType().Name}.");
                }

                uguiOptionsButton?.onClick.AddListener(SceneTransitionManager.LoadSettingsScene);
                uguiPlayEasyButton?.onClick.AddListener(SceneTransitionManager.PlayEasy);
                uguiPlayHardButton?.onClick.AddListener(SceneTransitionManager.PlayHard);
            }
        }

        void CleanupUI()
        {
            if (m_UseUIToolkit)
            {
                PlayerSettingsDataSource.Release();

                m_UitkOptionsButton.clicked -= SceneTransitionManager.LoadSettingsScene;
                m_UitkPlayEasyButton.clicked -= SceneTransitionManager.PlayEasy;
                m_UitkPlayHardButton.clicked -= SceneTransitionManager.PlayHard;
            }
            else
            {
                uguiOptionsButton?.onClick.RemoveListener(SceneTransitionManager.LoadSettingsScene);
                uguiPlayEasyButton?.onClick.RemoveListener(SceneTransitionManager.PlayEasy);
                uguiPlayHardButton?.onClick.RemoveListener(SceneTransitionManager.PlayHard);
            }
        }
    }
}
