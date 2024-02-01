using System;
using System.Collections;
using Unity.Samples.ScreenReader;
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
            // Close this screen when the device's Back button is pressed. (This only applies to Android.)
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                OnDismissed();
            }
        }

        void OnEnable()
        {
            // The pause screen is presented over the gameplay screen like a modal view, so all accessibility nodes
            // outside the pause screen should be deactivated while it is open.
            AccessibilityManager.ActivateOtherAccessibilityNodes(false, transform);

            // When the pause screen opens, move the accessibility focus to its status text (which is also the first
            // accessibility node on the pause screen).
            var nodeToFocus = statusText.GetComponent<AccessibleElement>().node;
            AssistiveSupport.notificationDispatcher.SendLayoutChanged(nodeToFocus);

            // Close this screen when the screen reader user performs the dismiss gesture.
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
