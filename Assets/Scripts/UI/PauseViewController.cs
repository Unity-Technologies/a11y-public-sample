using System;
using System.Collections;
using Unity.Samples.ScreenReader;
using TMPro;
using UnityEngine;
using UnityEngine.Accessibility;
using UnityEngine.UIElements;

namespace Unity.Samples.LetterSpell
{
    public class PauseViewController : MonoBehaviour
    {
        const float k_FadeDuration = 0.2f;

        static PauseViewController s_Instance;

        [Header("uGUI References")]
        public GameObject uguiPauseScreen;
        public UnityEngine.UI.Button uguiPauseSubmitButton;
        public UnityEngine.UI.Button uguiPauseDismissButton;

        public GameObject uguiResultsScreen;
        public TMP_Text uguiResultsLabel;
        public UnityEngine.UI.Button uguiResultsSubmitButton;
        public UnityEngine.UI.Button uguiResultsDismissButton;

        [Header("UI Toolkit References")]
        public UIDocument uitkDocument;

        VisualElement m_UitkPauseScreen;
        VisualElement m_UitkResultsScreen;

        bool m_UseUIToolkit;

        void Awake()
        {
            if (s_Instance != null && s_Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            s_Instance = this;
        }

        void OnDestroy()
        {
            if (s_Instance == this)
            {
                s_Instance = null;
            }
        }

        void OnEnable()
        {
            m_UseUIToolkit = PlayerPrefs.GetInt(UISystemToggler.useUIToolkitPreference) == 1;

            SetupUI();
        }

        void OnDisable()
        {
            CleanupUI();
        }

        void Update()
        {
            // Close this screen when the device's Back button is pressed. (This only applies to Android.)
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                OnDismissed();
            }
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

                m_UitkPauseScreen = root.Q<VisualElement>("pause-screen");
                m_UitkPauseScreen.Q<Button>("submit-button").clicked += SceneTransitionManager.TransitionToSecondIntroScene;
                m_UitkPauseScreen.Q<Button>("dismiss-button").clicked += ResumeGame;

                m_UitkResultsScreen = root.Q<VisualElement>("results-screen");
                m_UitkResultsScreen.Q<Button>("submit-button").clicked += SceneTransitionManager.TransitionToSecondIntroScene;
                m_UitkResultsScreen.Q<Button>("dismiss-button").clicked += StartGame;
            }
            else
            {
                if (uguiPauseScreen == null)
                {
                    Debug.LogError($"{nameof(uguiPauseScreen)} is not assigned for {GetType().Name}.");
                }

                if (uguiPauseSubmitButton == null)
                {
                    Debug.LogError($"{nameof(uguiPauseSubmitButton)} is not assigned for {GetType().Name}.");
                }

                if (uguiPauseDismissButton == null)
                {
                    Debug.LogError($"{nameof(uguiPauseDismissButton)} is not assigned for {GetType().Name}.");
                }

                if (uguiResultsScreen == null)
                {
                    Debug.LogError($"{nameof(uguiResultsScreen)} is not assigned for {GetType().Name}.");
                }

                if (uguiResultsLabel == null)
                {
                    Debug.LogError($"{nameof(uguiResultsLabel)} is not assigned for {GetType().Name}.");
                }

                if (uguiResultsSubmitButton == null)
                {
                    Debug.LogError($"{nameof(uguiResultsSubmitButton)} is not assigned for {GetType().Name}.");
                }

                if (uguiResultsDismissButton == null)
                {
                    Debug.LogError($"{nameof(uguiResultsDismissButton)} is not assigned for {GetType().Name}.");
                }

                uguiPauseSubmitButton?.onClick.AddListener(SceneTransitionManager.TransitionToSecondIntroScene);
                uguiPauseDismissButton?.onClick.AddListener(ResumeGame);
                uguiResultsSubmitButton?.onClick.AddListener(SceneTransitionManager.TransitionToSecondIntroScene);
                uguiResultsDismissButton?.onClick.AddListener(StartGame);

                if (uguiPauseDismissButton != null)
                {
                    // Close this screen when the screen reader user performs the dismiss gesture.
                    uguiPauseDismissButton.GetComponent<AccessibleButton>().dismissed += OnDismissed;
                }
            }
        }

        void CleanupUI()
        {
            if (m_UseUIToolkit)
            {
                m_UitkPauseScreen.Q<Button>("submit-button").clicked -= SceneTransitionManager.TransitionToSecondIntroScene;
                m_UitkPauseScreen.Q<Button>("dismiss-button").clicked -= ResumeGame;
                m_UitkResultsScreen.Q<Button>("submit-button").clicked -= SceneTransitionManager.TransitionToSecondIntroScene;
                m_UitkResultsScreen.Q<Button>("dismiss-button").clicked -= StartGame;
            }
            else
            {
                uguiPauseSubmitButton?.onClick.RemoveListener(SceneTransitionManager.TransitionToSecondIntroScene);
                uguiPauseDismissButton?.onClick.RemoveListener(ResumeGame);
                uguiResultsSubmitButton?.onClick.RemoveListener(SceneTransitionManager.TransitionToSecondIntroScene);
                uguiResultsDismissButton?.onClick.RemoveListener(StartGame);

                if (uguiPauseDismissButton != null)
                {
                    uguiPauseDismissButton.GetComponent<AccessibleButton>().dismissed -= OnDismissed;
                }
            }
        }

        bool OnDismissed()
        {
            uguiPauseSubmitButton?.onClick.Invoke();

            return true;
        }

        void StartGame()
        {
            if (Gameplay.instance == null)
            {
                Debug.LogError($"{nameof(Gameplay)} instance is not assigned.");
                return;
            }

            StartCoroutine(Hide());
            Gameplay.instance.StartGame();
        }

        public static void PauseGame()
        {
            if (Gameplay.instance == null)
            {
                Debug.LogError($"{nameof(Gameplay)} instance is not assigned.");
                return;
            }

            Gameplay.instance.PauseGame();
            s_Instance.StartCoroutine(s_Instance.Show());
        }

        void ResumeGame()
        {
            if (Gameplay.instance == null)
            {
                Debug.LogError($"{nameof(Gameplay)} instance is not assigned.");
                return;
            }

            StartCoroutine(Hide());
            Gameplay.instance.ResumeGame();
        }

        public static void EndGame(int completedWords, int totalWords)
        {
            if (Gameplay.instance == null)
            {
                Debug.LogError($"{nameof(Gameplay)} instance is not assigned.");
                return;
            }

            Gameplay.instance.StopGame();

            var text = $"The game is over!\n\nYou found {completedWords} out of {totalWords} words.";

            if (s_Instance.m_UseUIToolkit)
            {
                s_Instance.m_UitkResultsScreen.Q<Label>("results-label").text = text;
            }
            else
            {
                s_Instance.uguiResultsLabel.text = text;

                var accessibleText = s_Instance.uguiResultsLabel.GetComponent<AccessibleText>();
                accessibleText.label = s_Instance.uguiResultsLabel.text;
                accessibleText.SetNodeProperties();
            }

            s_Instance.StartCoroutine(s_Instance.Show());
        }

        IEnumerator Show()
        {
            if (m_UseUIToolkit)
            {
                var screen = Gameplay.instance.state == Gameplay.State.Paused ?
                    m_UitkPauseScreen : m_UitkResultsScreen;
                screen.style.display = DisplayStyle.Flex;
                screen.style.opacity = 1f;
            }
            else
            {
                var screen = Gameplay.instance.state == Gameplay.State.Paused ?
                    uguiPauseScreen : uguiResultsScreen;
                screen.SetActive(true);
                StartCoroutine(Fade(screen, 1f));

                yield return new WaitForSeconds(k_FadeDuration);

                // The pause screen is presented over the gameplay screen like a modal view, so all accessibility nodes
                // outside the pause screen should be deactivated while it is open.
                AccessibilityManager.ActivateOtherAccessibilityNodes(false, transform);

                // When the pause screen opens, move the accessibility focus to its status text (which is also the first
                // accessibility node on the pause screen).
                var nodeToFocus = screen.GetComponentInChildren<AccessibleText>().node;
                AssistiveSupport.notificationDispatcher.SendLayoutChanged(nodeToFocus);
            }
        }

        IEnumerator Hide()
        {
            if (m_UseUIToolkit)
            {
                var screen = Gameplay.instance.state == Gameplay.State.Paused ?
                    m_UitkPauseScreen : m_UitkResultsScreen;
                screen.style.opacity = 0f;
                yield return new WaitForSeconds(k_FadeDuration);
                screen.style.display = DisplayStyle.None;
            }
            else
            {
                AccessibilityManager.ActivateOtherAccessibilityNodes(true, transform);

                var screen = Gameplay.instance.state == Gameplay.State.Paused ?
                    uguiPauseScreen : uguiResultsScreen;
                StartCoroutine(Fade(screen, 0f));
                yield return new WaitForSeconds(k_FadeDuration);
                screen.SetActive(false);
            }
        }

        static IEnumerator Fade(GameObject screen, float targetAlpha)
        {
            var canvasGroup = screen.GetComponent<CanvasGroup>();

            var startAlpha = canvasGroup.alpha;
            var timePassed = 0f;

            while (timePassed < k_FadeDuration)
            {
                timePassed += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, timePassed / k_FadeDuration);
                yield return null;
            }

            canvasGroup.alpha = targetAlpha;
        }
    }
}
