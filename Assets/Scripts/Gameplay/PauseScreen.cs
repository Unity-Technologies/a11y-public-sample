using System;
using System.Collections;
using Unity.Samples.Accessibility;
using TMPro;
using UnityEngine;
using UnityEngine.Accessibility;
using UnityEngine.UI;

namespace Unity.Samples.LetterSpell
{
    public class PauseScreen : MonoBehaviour
    {
        public TMP_Text statusText;
        public Button dismissButton;

        const float k_FadeDuration = 0.2f;

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                OnDismissed();
            }
        }

        void OnEnable()
        {
            AccessibilityManager.ActivateOtherAccessibilityNodes(false, transform);

            var nodeToFocus = statusText.GetComponent<AccessibleElement>().node;
            AssistiveSupport.notificationDispatcher.SendLayoutChanged(nodeToFocus);

            dismissButton.GetComponent<AccessibleButton>().dismissed += OnDismissed;
        }

        void OnDisable()
        {
            AccessibilityManager.ActivateOtherAccessibilityNodes(true, transform);

            dismissButton.GetComponent<AccessibleButton>().dismissed -= OnDismissed;
        }

        bool OnDismissed()
        {
            dismissButton.onClick.Invoke();

            return true;
        }

        public void StartGame()
        {
            Hide();
            Gameplay.instance.StartGame();
        }

        public void PauseGame()
        {
            Gameplay.instance.PauseGame();
            Show();
        }

        public void ResumeGame()
        {
            Hide();
            Gameplay.instance.ResumeGame();
        }

        public void EndGame()
        {
            Hide();
            Gameplay.instance.StopGame();
        }

        public void Show()
        {
            gameObject.SetActive(true);
            StartCoroutine(Fade(1f));
        }

        public void ShowResults(int completedWords, int totalWords)
        {
            statusText.text = $"The game is over!\n\nYou found {completedWords} words out of {totalWords}.";

            var accessibleText = statusText.GetComponent<AccessibleText>();
            accessibleText.label = statusText.text;
            accessibleText.SetNodeProperties();

            Show();
        }

        public void Hide()
        {
            StartCoroutine(Fade(0f));
        }

        IEnumerator Fade(float targetAlpha)
        {
            var canvasGroup = GetComponent<CanvasGroup>();

            var startAlpha = canvasGroup.alpha;
            var timePassed = 0f;

            while (timePassed < k_FadeDuration)
            {
                timePassed += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, timePassed / k_FadeDuration);
                yield return null;
            }

            canvasGroup.alpha = targetAlpha;

            if (targetAlpha == 0f)
            {
                canvasGroup.gameObject.SetActive(false);
            }
        }
    }
}
