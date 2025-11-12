using System.Collections;
using System.Globalization;
using Unity.Samples.ScreenReader;
using TMPro;
using UnityEngine;
using UnityEngine.Accessibility;
using UnityEngine.Localization;
using UnityEngine.UI;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.SmartFormat.PersistentVariables;

namespace Unity.Samples.LetterSpell
{
    /// <summary>
    /// Controls the content of the game view.
    /// </summary>
    class GameViewController : MonoBehaviour
    {
        /// <summary>
        /// The Gameplay manager.
        /// </summary>
        public Gameplay gameplay;

        /// <summary>
        /// The template used to create visual instances of letter cards.
        /// </summary>
        public GameObject letterCardTemplate;

        /// <summary>
        /// The container of the letter cards.
        /// </summary>
        public Transform letterCardContainer;

        public TMP_Text clueText;
        public Image successImage;
        public PauseScreen pauseScreen;
        public PauseScreen resultsScreen;

        LetterCardListModel m_Model = new();

        /// <summary>
        /// The focused card.
        /// </summary>
        LetterCard m_AccessibilityFocusedCard;

        /// <summary>
        /// The card that is being dragged by the screen reader.
        /// </summary>
        LetterCard m_AccessibilitySelectedCard;

        void OnEnable()
        {
            m_Model.letterCardsChanged += OnLetterCardsChanged;
            m_Model.gameplay = gameplay;

            gameplay.stateChanged.AddListener(ShowOrHideClue);

            AssistiveSupport.nodeFocusChanged += OnNodeFocusChanged;
            AssistiveSupport.screenReaderStatusChanged += OnScreenReaderStatusChanged;
        }

        void OnDisable()
        {
            m_Model.letterCardsChanged -= OnLetterCardsChanged;
            m_Model.gameplay = null;
            m_AccessibilityFocusedCard = null;

            AssistiveSupport.nodeFocusChanged -= OnNodeFocusChanged;
            AssistiveSupport.screenReaderStatusChanged -= OnScreenReaderStatusChanged;
        }

        public void ShowNextWord()
        {
            successImage.gameObject.SetActive(false);

            if (gameplay.IsShowingLastWord())
            {
                if (AudioManager.instance != null)
                {
                    AudioManager.instance.PlayResult(gameplay.reorderedWordCount == gameplay.words.Count);
                }

                gameplay.StopGame();
                resultsScreen.ShowResults(gameplay.reorderedWordCount, gameplay.words.Count);
            }
            else
            {
                gameplay.ShowNextWord();
            }
        }

        public void OnCurrentWordIndexChanged(int index)
        {
            var clue = gameplay.currentWord.clue;

            clueText.GetComponent<TextMeshProUGUI>().text = clue;
            clueText.GetComponent<AccessibleElement>().label = clue;

            ShowOrHideClue(Gameplay.State.Playing);
        }

        void ShowOrHideClue(Gameplay.State newState)
        {
            if (PlayerPrefs.GetInt(PlayerSettings.cluePreference, 1) == 1)
            {
                clueText.GetComponent<TextMeshProUGUI>().enabled = true;
                clueText.GetComponent<AccessibleElement>().enabled = true;
            }
            else
            {
                clueText.GetComponent<TextMeshProUGUI>().enabled = false;
                clueText.GetComponent<AccessibleElement>().enabled = false;
            }
        }

        /// <summary>
        /// Regenerates all the letter cards.
        /// </summary>
        void OnLetterCardsChanged()
        {
            m_AccessibilityFocusedCard = null;

            // Remove all cards.
            foreach (Transform letterCardTransform in letterCardContainer)
            {
                Destroy(letterCardTransform.gameObject);
            }

            // Generate new cards.
            foreach (var letterCard in m_Model.letterCards)
            {
                var cultureInfo = LocalizationSettings.SelectedLocale?.Identifier.CultureInfo ?? CultureInfo.CurrentUICulture;
                var letter = letterCard.letter.ToString().ToUpper(cultureInfo);

                var card = Instantiate(letterCardTemplate, letterCardContainer);
                card.GetComponentInChildren<TextMeshProUGUI>().text = letter;
                card.name = letter;
                card.GetComponent<LetterCard>().dropped += (oldIndex, newIndex) =>
                {
                    gameplay.ReorderLetter(oldIndex, newIndex);
                };

                var element = card.AddComponent<AccessibleElement>();
                element.label = letter;
                element.hint = LocalizationSettings.StringDatabase.GetLocalizedString("Game Text", "LETTER_CARD_HINT_UNSELECTED");
                element.selected += OnLetterCardSelected;
            }

            if (gameplay != null && gameplay.state != Gameplay.State.Stopped)
            {
                AccessibilityManager.RebuildHierarchy();

                Invoke(nameof(MoveAccessibilityFocusOnClue), 1f);
            }
        }

        /// <summary>
        /// Toggles the ability of the focused letter card to be reordered using the screen reader.
        /// </summary>
        bool OnLetterCardSelected()
        {
            var letterCard = m_AccessibilityFocusedCard.GetComponent<LetterCard>();

            if (m_AccessibilitySelectedCard == null)
            {
                m_AccessibilitySelectedCard = letterCard;

                // When a letter card is selected, deactivate all accessibility nodes except the ones corresponding to
                // the letter cards to allow the selected card to be moved correctly.
                UGuiAccessibilityManager.instance.ActivateOtherAccessibilityNodes(false, letterCardContainer);

                letterCard.SetDraggingVisuals(true);

                var element = m_AccessibilityFocusedCard.GetComponent<AccessibleElement>();
                element.hint = LocalizationSettings.StringDatabase.GetLocalizedString("Game Text", "LETTER_CARD_HINT_SELECTED");
                element.SetNodeProperties();
            }
            else
            {
                m_AccessibilitySelectedCard = null;

                UGuiAccessibilityManager.instance.ActivateOtherAccessibilityNodes(true, letterCardContainer);

                letterCard.SetDraggingVisuals(false);

                var element = m_AccessibilityFocusedCard.GetComponent<AccessibleElement>();
                element.hint = LocalizationSettings.StringDatabase.GetLocalizedString("Game Text", "LETTER_CARD_HINT_UNSELECTED");
                element.SetNodeProperties();
            }

            return true;
        }

        void MoveAccessibilityFocusOnClue()
        {
            var nodeToFocus = clueText.GetComponent<AccessibleElement>().node;
            AssistiveSupport.notificationDispatcher.SendLayoutChanged(nodeToFocus);
        }

        public void OnWordReorderingCompleted()
        {
            m_AccessibilitySelectedCard?.SetDraggingVisuals(false);
            m_AccessibilitySelectedCard = null;

            StartCoroutine(DelayWordReorderingCompleted());
            return;

            // This delay is needed to ensure that the screen reader has enough time to announce the word reordering.
            // It also ensures that the announcement is not ignored by the screen reader.
            IEnumerator DelayWordReorderingCompleted()
            {
                const float fadeDuration = 0.3f;
                FadeSuccessImageIn(fadeDuration);

                const float announcementDelay = 1f;

                yield return new WaitForSeconds(announcementDelay);

                var localizedString = new LocalizedString
                {
                    TableReference = "Game Text",
                    TableEntryReference = "ANNOUNCEMENT_WORD_FOUND"
                };

                var word = new StringVariable
                {
                    Value = gameplay.currentWord.word
                };

                localizedString.Add("word", word);

                localizedString.StringChanged += announcement =>
                    AssistiveSupport.notificationDispatcher.SendAnnouncement(announcement);

                const float imageDuration = 2f;
                const float fadeOutDelay = imageDuration - announcementDelay - fadeDuration;
                yield return new WaitForSeconds(fadeOutDelay);
                FadeSuccessImageOut(fadeDuration);

                const float announcementDuration = 2.5f;
                const float nextWordDelay = announcementDuration - fadeOutDelay;
                yield return new WaitForSeconds(nextWordDelay);
                ShowNextWord();
            }
        }

        /// <summary>
        /// Resets the selected card when the screen reader status changes.
        /// </summary>
        void OnScreenReaderStatusChanged(bool isScreenReaderEnabled)
        {
            if (m_AccessibilitySelectedCard != null)
            {
                m_AccessibilitySelectedCard.SetDraggingVisuals(false);
                m_AccessibilitySelectedCard = null;
            }
        }

        void OnNodeFocusChanged(AccessibilityNode node)
        {
            if (node != null)
            {
                var element = UGuiAccessibilityManager.instance.GetAccessibleElementForNode(node);
                m_AccessibilityFocusedCard = element != null ? element.GetComponent<LetterCard>() : null;
                MoveSelectedCard();
            }
            else
            {
                m_AccessibilityFocusedCard = null;
            }
        }

        void MoveSelectedCard()
        {
            if (!AssistiveSupport.isScreenReaderEnabled ||
                m_AccessibilitySelectedCard == null ||
                m_AccessibilityFocusedCard == null)
            {
                return;
            }

            // If we reach this code, it means we're dragging the card.
            var selectedCardIndex = m_AccessibilitySelectedCard.transform.GetSiblingIndex();
            var focusedCardIndex = m_AccessibilityFocusedCard.transform.GetSiblingIndex();

            // Move the card to the new position.
            if (selectedCardIndex > focusedCardIndex)
            {
                MoveCard(true, selectedCardIndex - focusedCardIndex);
            }
            else if (selectedCardIndex < focusedCardIndex)
            {
                MoveCard(false, focusedCardIndex - selectedCardIndex);
            }
        }

        void MoveCard(bool shouldMoveLeft, int count)
        {
            var draggable = m_AccessibilitySelectedCard;
            if (draggable == null)
            {
                return;
            }

            var element = draggable.transform.GetComponent<AccessibleElement>();

            if (shouldMoveLeft ? draggable.MoveLeft(count) : draggable.MoveRight(count))
            {
                var index = draggable.transform.GetSiblingIndex();
                var otherSiblingIndex = shouldMoveLeft ? index + 1 : index - 1;
                var otherSibling = draggable.transform.parent.GetChild(otherSiblingIndex);

                // Announce that the card was moved.
                var localizedString = new LocalizedString
                {
                    TableReference = "Game Text",
                    TableEntryReference = "ANNOUNCEMENT_CARD_MOVED"
                };

                var selectedLetter = new StringVariable
                {
                    Value = draggable.name
                };

                var moveLeft = new BoolVariable
                {
                    Value = shouldMoveLeft
                };

                var otherLetter = new StringVariable
                {
                    Value = otherSibling.name
                };

                localizedString.Add("selectedLetter", selectedLetter);
                localizedString.Add("shouldMoveLeft", moveLeft);
                localizedString.Add("otherLetter", otherLetter);

                localizedString.StringChanged += announcement =>
                    AssistiveSupport.notificationDispatcher.SendAnnouncement(announcement);

                AssistiveSupport.activeHierarchy.MoveNode(element.node, element.node.parent,
                    element.transform.GetSiblingIndex());

                // After the move, the screen reader will refocus on the other card, but with a little delay. Move the
                // focus to the selected card, but wait a bit to let the first focus change complete. Otherwise, the
                // screen reader will focus on the selected card first, then still on the other card, triggering an
                // infinite swap of the two cards.
                StartCoroutine(DelaySendLayoutChanged());
                return;

                IEnumerator DelaySendLayoutChanged()
                {
                    yield return new WaitForEndOfFrame();

                    AssistiveSupport.notificationDispatcher.SendLayoutChanged(element.node);
                }
            }
        }

        void FadeSuccessImageIn(float duration)
        {
            successImage.gameObject.SetActive(true);
            StartCoroutine(FadeGraphic(successImage, 1f, duration));
        }

        void FadeSuccessImageOut(float duration)
        {
            StartCoroutine(FadeGraphic(successImage, 0f, duration));
        }

        static IEnumerator FadeGraphic(Graphic graphic, float targetAlpha, float duration)
        {
            var startAlpha = graphic.color.a;
            var time = 0f;

            while (time < duration)
            {
                time += Time.deltaTime;
                var normalizedTime = time / duration;
                var alpha = Mathf.Lerp(startAlpha, targetAlpha, normalizedTime);
                graphic.color = new Color(graphic.color.r, graphic.color.g, graphic.color.b, alpha);
                yield return null;
            }

            graphic.color = new Color(graphic.color.r, graphic.color.g, graphic.color.b, targetAlpha);
        }
    }
}
