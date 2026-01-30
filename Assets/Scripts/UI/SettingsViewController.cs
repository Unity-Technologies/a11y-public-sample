using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Samples.LetterSpell
{
    public class SettingsViewController : MonoBehaviour
    {
        [Header("uGUI References")]
        public UnityEngine.UI.Button uguiBackButton;

        [Header("UI Toolkit References")]
        public UIDocument uitkDocument;

        PlayerSettingsDataSource m_UitkDataSource;
        Button m_UitkBackButton;

        bool m_UseUIToolkit;

        void OnEnable()
        {
            Gameplay.instance?.PauseGame();

            m_UseUIToolkit = PlayerPrefs.GetInt(UISystemToggler.useUIToolkitPreference) == 1;

            SetupUI();
        }

        void OnDisable()
        {
            CleanupUI();

            Gameplay.instance?.ResumeGame();
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

                root.dataSource = m_UitkDataSource = new PlayerSettingsDataSource();

                m_UitkBackButton = root.Q<Button>("back-button");
                m_UitkBackButton.clicked += SceneTransitionManager.UnloadSettingsScene;
            }
            else
            {
                if (uguiBackButton == null)
                {
                    Debug.LogError($"{nameof(uguiBackButton)} is not assigned for {GetType().Name}.");
                }

                uguiBackButton?.onClick.AddListener(SceneTransitionManager.UnloadSettingsScene);
            }
        }

        void CleanupUI()
        {
            if (m_UseUIToolkit)
            {
                m_UitkDataSource?.Dispose();
                uitkDocument.rootVisualElement.dataSource = null;

                m_UitkBackButton.clicked -= SceneTransitionManager.UnloadSettingsScene;
            }
            else
            {
                uguiBackButton?.onClick.RemoveListener(SceneTransitionManager.UnloadSettingsScene);
            }
        }
    }
}
