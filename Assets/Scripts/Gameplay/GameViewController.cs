using System.Collections;
using Unity.Samples.Accessibility;
using TMPro;
using UnityEngine;
using UnityEngine.Accessibility;
using UnityEngine.UI;

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

        /// <summary>
        /// Keeps track of whether the hierarchy was refreshed using AccessibilityManager.RefreshHierarchy();
        /// </summary>
        bool m_WasHierarchyRefreshed;

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
            clueText.GetComponent<AccessibleElement>().value = clue;

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
            foreach (var letterCardModel in m_Model.letterCards)
            {
                var card = Instantiate(letterCardTemplate, letterCardContainer);
                card.GetComponentInChildren<TextMeshProUGUI>().text = letterCardModel.letter.ToString();
                card.name = letterCardModel.letter.ToString();
                card.GetComponent<LetterCard>().dropped += (oldIndex, newIndex) =>
                {
                    gameplay.ReorderLetter(oldIndex, newIndex);
                };

                var element = card.AddComponent<AccessibleElement>();
                element.label = letterCardModel.letter.ToString();
                element.hint = "Double tap to start moving.";
                element.selected += OnLetterCardSelected;
            }

            if (Gameplay.instance != null && Gameplay.instance.state != Gameplay.State.Stopped)
            {
                this.DelayRefreshHierarchy();

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
                AccessibilityManager.ActivateOtherAccessibilityNodes(false, letterCardContainer);
                letterCard.SetDraggingVisuals(true);
                SetLetterCardsAccessibilityLabel(false);
            }
            else
            {
                m_AccessibilitySelectedCard = null;
                AccessibilityManager.ActivateOtherAccessibilityNodes(true, letterCardContainer);
                letterCard.SetDraggingVisuals(false);
                SetLetterCardsAccessibilityLabel(true);
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
            StartCoroutine(DelayWordReorderingCompleted());
            return;
            
            // This delay is needed to ensure that the screen reader has enough time to announce the word reordering.
            // It also ensures that the announcement is not ignored by the screen reader.
            IEnumerator DelayWordReorderingCompleted()
            {
                const float fadeDuration = 0.3f;
                FadeSuccessImageIn(fadeDuration);

                const float announcementDelay = 1f;
                const string successAnnouncement = "Bravo! You found the correct word.";
                yield return new WaitForSeconds(announcementDelay);
                AssistiveSupport.notificationDispatcher.SendAnnouncement(successAnnouncement);

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
                var element = AccessibilityManager.GetAccessibleElementForNode(node);
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
            if (!AssistiveSupport.isScreenReaderEnabled || m_AccessibilitySelectedCard == null || m_AccessibilityFocusedCard == null)
            {
                return;
            }
            
            // Don't move the card if the focus change occurred because of a hierarchy rebuild.
            if (m_WasHierarchyRefreshed)
            {
                m_WasHierarchyRefreshed = false;
                return;
            }

            // If we reach this code, it means we're dragging the card.
            int selectedCardIndex = m_AccessibilitySelectedCard.transform.GetSiblingIndex();
            int focusedCardIndex = m_AccessibilityFocusedCard.transform.GetSiblingIndex();

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
        
        void SetLetterCardsAccessibilityLabel(bool hasLabel)
        {
            foreach (Transform letterCardTransform in letterCardContainer)
            {
                var element = letterCardTransform.GetComponent<AccessibleElement>();
                element.label = hasLabel ? letterCardTransform.name : null;
                element.hint = hasLabel ? "Double tap to start moving." : null;
                element.SetNodeProperties();
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
                
                // Make the letter uppercase to ensure correct phonetic pronunciation.
                var message = $"Moved {draggable.name.ToUpper()} {(shouldMoveLeft ? "before" : "after")} {otherSibling.name.ToUpper()}";

                // Announce that the card was moved.
                AssistiveSupport.notificationDispatcher.SendAnnouncement(message);

                if (Application.platform == RuntimePlatform.IPhonePlayer)
                {
                    AccessibilityManager.RefreshHierarchy();
                    m_WasHierarchyRefreshed = true;
                    
                    // Add the node count to the element ID to match the ID of the node in the refreshed hierarchy,
                    // ensuring consistent focus even after rebuilding.
                    int nodeToFocusId = element.node.id + AccessibilityManager.hierarchy.rootNodes.Count;
                    nodeToFocusId += (shouldMoveLeft ? -count : count);

                    this.DelayFocusOnNode(nodeToFocusId);   
                }
                else
                {
                    AccessibilityManager.hierarchy.MoveNode(element.node, element.node.parent,
                        element.transform.GetSiblingIndex());

                    // Only refresh the frames for now to leave the announcement request to be handled.
                    this.DelayRefreshNodeFrames();

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
