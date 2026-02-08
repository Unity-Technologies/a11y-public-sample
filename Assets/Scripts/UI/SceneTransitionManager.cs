using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.UIElements;

namespace Unity.Samples.LetterSpell
{
    public class SceneTransitionManager : MonoBehaviour
    {
        static SceneTransitionManager s_Instance;

        UnityEngine.UI.Image m_UguiTransitionImage;
        UnityEngine.UIElements.Image m_UitkTransitionImage;

        static readonly Color k_FadeColor = new(2f / 255f, 197f / 255f, 132f / 255f, 1f);

        const float k_TransitionDelay = 1.85f;
        const float k_TransitionDuration = 0.15f;

        bool m_UseUIToolkit;
        bool m_SettingsSceneLoaded;

        void Awake()
        {
            if (s_Instance != null && s_Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            s_Instance = this;

            // Keep the Scene Transition Manager alive across scenes.
            DontDestroyOnLoad(gameObject);
        }

        void OnDestroy()
        {
            if (s_Instance == this)
            {
                s_Instance = null;
            }
        }

        void Start()
        {
            m_UseUIToolkit = PlayerPrefs.GetInt(UISystemToggler.useUIToolkitPreference) == 1;

            CreateTransitionCanvas();

            if (SceneManager.GetActiveScene().name == "Splash Scene")
            {
                Invoke(nameof(TransitionToFirstIntroScene), k_TransitionDelay);
            }
        }

        void CreateTransitionCanvas()
        {
            if (m_UseUIToolkit)
            {
                var documentObject = new GameObject("Scene Transition Document");
                documentObject.transform.SetParent(transform, false);

                var document = documentObject.AddComponent<UIDocument>();
                document.panelSettings = Resources.Load<PanelSettings>("UI Toolkit/PanelSettings");
                document.sortingOrder = short.MaxValue; // Ensure the transition document is on top of all other UI.

                m_UitkTransitionImage = new UnityEngine.UIElements.Image
                {
                    pickingMode = PickingMode.Ignore, // Allow input to pass through.
                    style =
                    {
                        position = Position.Absolute,
                        left = 0f,
                        top = 0f,
                        right = 0f,
                        bottom = 0f,
                        backgroundColor = k_FadeColor,
                        opacity = 0f,
                        transitionProperty = new List<StylePropertyName>
                        {
                            new("opacity")
                        },
                        transitionDuration = new List<TimeValue>
                        {
                            new(k_TransitionDuration, TimeUnit.Second)
                        },
                        transitionTimingFunction = new List<EasingFunction>
                        {
                            new(EasingMode.EaseInOut)
                        }
                    }
                };

                document.rootVisualElement.Add(m_UitkTransitionImage);
            }
            else
            {
                var canvasObject = new GameObject("Scene Transition Canvas");
                canvasObject.transform.SetParent(transform, false);
                canvasObject.SetActive(false);

                var canvas = canvasObject.AddComponent<Canvas>();
                // Ensure the transition canvas is on top of all other UI.
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = short.MaxValue;

                canvasObject.AddComponent<CanvasScaler>();
                canvasObject.AddComponent<GraphicRaycaster>(); // Required for accepting input.

                m_UguiTransitionImage = canvasObject.AddComponent<UnityEngine.UI.Image>();
                m_UguiTransitionImage.color = k_FadeColor;
                m_UguiTransitionImage.canvasRenderer.SetAlpha(0f);
                m_UguiTransitionImage.raycastTarget = true; // Block input.

                var rt = m_UguiTransitionImage.rectTransform;
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
            }
        }

        void TransitionToFirstIntroScene()
        {
            StartCoroutine(TransitionToScene("Intro 1 Scene"));
        }

        public static void TransitionToSecondIntroScene()
        {
            s_Instance.StartCoroutine(s_Instance.TransitionToScene("Intro 2 Scene"));
        }

        public static void PlayEasy()
        {
            PlayerPrefs.SetInt(PlayerSettings.difficultyPreference, (int)Gameplay.DifficultyLevel.Easy);

            s_Instance.StartCoroutine(s_Instance.TransitionToScene("Gameplay Scene"));
        }

        public static void PlayHard()
        {
            PlayerPrefs.SetInt(PlayerSettings.difficultyPreference, (int)Gameplay.DifficultyLevel.Hard);

            s_Instance.StartCoroutine(s_Instance.TransitionToScene("Gameplay Scene"));
        }

        public static void LoadSettingsScene()
        {
            if (s_Instance.m_SettingsSceneLoaded)
            {
                return;
            }

            SceneManager.LoadScene("Settings Scene", LoadSceneMode.Additive);

            s_Instance.m_SettingsSceneLoaded = true;
        }

        public static void UnloadSettingsScene()
        {
            if (!s_Instance.m_SettingsSceneLoaded)
            {
                return;
            }

            SceneManager.UnloadSceneAsync("Settings Scene");

            s_Instance.m_SettingsSceneLoaded = false;
        }

        IEnumerator TransitionToScene(string sceneName)
        {
            if (m_UseUIToolkit)
            {
                m_UitkTransitionImage.pickingMode = PickingMode.Position; // Block input.
                m_UitkTransitionImage.style.opacity = 1f;
                yield return new WaitForSeconds(k_TransitionDuration);

                yield return SceneManager.LoadSceneAsync(sceneName);

                m_UitkTransitionImage.style.opacity = 0f;
                yield return new WaitForSeconds(k_TransitionDuration);
                m_UitkTransitionImage.pickingMode = PickingMode.Ignore; // Allow input to pass through.
            }
            else
            {
                m_UguiTransitionImage.gameObject.SetActive(true);
                m_UguiTransitionImage.CrossFadeAlpha(1f, k_TransitionDuration, false);
                yield return new WaitForSeconds(k_TransitionDuration);

                yield return SceneManager.LoadSceneAsync(sceneName);

                m_UguiTransitionImage.CrossFadeAlpha(0f, k_TransitionDuration, false);
                yield return new WaitForSeconds(k_TransitionDuration);
                m_UguiTransitionImage.gameObject.SetActive(false);
            }
        }
    }
}
