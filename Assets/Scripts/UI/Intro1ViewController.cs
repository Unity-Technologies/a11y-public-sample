using System;
using UnityEngine;
using UnityEngine.UIElements;
using TMPro;

namespace Unity.Samples.LetterSpell
{
    public class Intro1ViewController : MonoBehaviour
    {
        [Header("uGUI References")]
        public TMP_InputField uguiUsernameInputField;
        public UnityEngine.UI.Button uguiContinueButton;

        [Header("UI Toolkit References")]
        public UIDocument uitkDocument;

        Button m_UitkContinueButton;

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

                m_UitkContinueButton = root.Q<Button>();
                m_UitkContinueButton.clicked += OnContinueButtonClicked;
            }
            else
            {
                if (uguiUsernameInputField == null)
                {
                    Debug.LogError($"{nameof(uguiUsernameInputField)} is not assigned for {GetType().Name}.");
                }

                if (uguiContinueButton == null)
                {
                    Debug.LogError($"{nameof(uguiContinueButton)} is not assigned for {GetType().Name}.");
                }

                uguiContinueButton?.onClick.AddListener(OnContinueButtonClicked);
            }
        }

        void CleanupUI()
        {
            if (m_UseUIToolkit)
            {
                PlayerSettingsDataSource.Release();

                m_UitkContinueButton.clicked -= OnContinueButtonClicked;
            }
            else
            {
                uguiContinueButton?.onClick.RemoveListener(OnContinueButtonClicked);
            }
        }

        void OnContinueButtonClicked()
        {
            if (!m_UseUIToolkit)
            {
                var username = uguiUsernameInputField?.text;

                if (!string.IsNullOrEmpty(username))
                {
                    PlayerPrefs.SetString(PlayerSettings.usernamePreference, username);
                }
            }

            SceneTransitionManager.TransitionToSecondIntroScene();
        }
    }
}
