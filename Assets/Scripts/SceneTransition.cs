using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Unity.Samples.LetterSpell
{
    public class SceneTransition : MonoBehaviour
    {
        public TMP_InputField usernameInputField;
        public Image transitionImage;
        public bool autoTransitionOut;
        public string nextSceneName;

        const float k_TransitionDelay = 1.85f;
        const float k_TransitionDuration = 0.15f;

        void Start()
        {
            TransitionIn();
        }

        public void TransitionToNextScene()
        {
            if (transitionImage == null)
            {
                LoadScene();
                return;
            }

            TransitionOut();
            Invoke(nameof(LoadScene), k_TransitionDuration);
        }

        public void TransitionToDifficultyScene()
        {
            if (usernameInputField != null && usernameInputField.text.Length > 0)
            {
                PlayerPrefs.SetString(PlayerSettings.usernamePreference, usernameInputField.text);
            }

            if (transitionImage == null)
            {
                LoadDifficultyScene();
                return;
            }

            TransitionOut();
            Invoke(nameof(LoadDifficultyScene), k_TransitionDuration);
        }

        public void PlayEasy()
        {
            PlayerPrefs.SetInt(PlayerSettings.difficultyPreference, (int)Gameplay.DifficultyLevel.Easy);
            TransitionToGameplayScene();
        }

        public void PlayHard()
        {
            PlayerPrefs.SetInt(PlayerSettings.difficultyPreference, (int)Gameplay.DifficultyLevel.Hard);
            TransitionToGameplayScene();
        }

        public void LoadSettingsScene()
        {
            SceneManager.LoadScene("Settings Scene", LoadSceneMode.Additive);
        }

        public void UnloadSettingsScene()
        {
            SceneManager.UnloadSceneAsync("Settings Scene");
        }

        void TransitionIn()
        {
            if (transitionImage == null)
            {
                return;
            }

            transitionImage.gameObject.SetActive(true);
            transitionImage.CrossFadeAlpha(0f, k_TransitionDuration, false);
            Invoke(nameof(FinishTransitioningIn), k_TransitionDuration);

            if (autoTransitionOut)
            {
                Invoke(nameof(TransitionToNextScene), k_TransitionDelay);
            }
        }

        void FinishTransitioningIn()
        {
            if (transitionImage == null)
            {
                return;
            }

            transitionImage.gameObject.SetActive(false);
        }

        void TransitionOut()
        {
            if (transitionImage == null)
            {
                return;
            }

            transitionImage.gameObject.SetActive(true);
            transitionImage.CrossFadeAlpha(1f, k_TransitionDuration, false);
        }

        void TransitionToGameplayScene()
        {
            if (transitionImage == null)
            {
                LoadGameplayScene();
                return;
            }

            TransitionOut();
            Invoke(nameof(LoadGameplayScene), k_TransitionDuration);
        }

        void LoadScene()
        {
            SceneManager.LoadScene(nextSceneName);
        }

        void LoadDifficultyScene()
        {
            SceneManager.LoadScene("Intro 2 Scene");
        }

        void LoadGameplayScene()
        {
            SceneManager.LoadScene("Gameplay Scene");
        }
    }
}