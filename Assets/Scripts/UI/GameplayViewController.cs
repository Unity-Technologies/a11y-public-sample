using System.Collections;
using Unity.Samples.ScreenReader;
using TMPro;
using UnityEngine;
using UnityEngine.Accessibility;
using UnityEngine.UIElements;

namespace Unity.Samples.LetterSpell
{
    /// <summary>
    /// Controls the content of the game view.
    /// </summary>
    class GameplayViewController : MonoBehaviour
    {
        const string k_SuccessAnnouncement = "Bravo! You found the correct word.";

        /// <summary>
        /// The template used to create visual instances of letter cards.
        /// </summary>
        [Header("uGUI References")]
        public GameObject uguiLetterCardTemplate;

        /// <summary>
        /// The container of the letter cards.
        /// </summary>
        public Transform uguiLetterCardContainer;

        public TMP_Text uguiClueLabel;
        public UnityEngine.UI.Image uguiSuccessImage;

        public UnityEngine.UI.Button uguiPauseButton;
        public UnityEngine.UI.Button uguiOptionsButton;
        public UnityEngine.UI.Button uguiNextButton;

        [Header("UI Toolkit References")]
        public UIDocument uitkDocument;

        Label m_UitkClueLabel;
        VisualElement m_UitkSuccessImage;
        VisualElement m_UitkLetterCardContainer;

        Button m_UitkPauseButton;
        Button m_UitkOptionsButton;
        Button m_UitkNextButton;

        bool m_UseUIToolkit;

        LetterCardListModel m_Model = new();

        /// <summary>
        /// The focused card.
        /// </summary>
        UGUILetterCard m_AccessibilityFocusedCard;

        /// <summary>
        /// The card that is being dragged by the screen reader.
        /// </summary>
        UGUILetterCard m_AccessibilitySelectedCard;

        void OnEnable()
        {
            m_UseUIToolkit = PlayerPrefs.GetInt(UISystemToggler.useUIToolkitPreference) == 1;

            SetupUI();

            m_Model.Setup();
            m_Model.letterCardsChanged += OnLetterCardsChanged;

            AssistiveSupport.nodeFocusChanged += OnNodeFocusChanged;
            AssistiveSupport.screenReaderStatusChanged += OnScreenReaderStatusChanged;
        }

        void OnDisable()
        {
            CleanupUI();

            m_Model.letterCardsChanged -= OnLetterCardsChanged;
            m_Model.Cleanup();

            m_AccessibilityFocusedCard = null;

            AssistiveSupport.nodeFocusChanged -= OnNodeFocusChanged;
            AssistiveSupport.screenReaderStatusChanged -= OnScreenReaderStatusChanged;
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

                m_UitkClueLabel = root.Q<Label>("clue-label");
                m_UitkSuccessImage = root.Q<VisualElement>("success-image");
                m_UitkLetterCardContainer = root.Q<VisualElement>("letter-card-container");

                m_UitkPauseButton = root.Q<Button>("pause-button");
                m_UitkPauseButton.clicked += PauseViewController.PauseGame;

                m_UitkOptionsButton = root.Q<Button>("options-button");
                m_UitkOptionsButton.clicked += SceneTransitionManager.LoadSettingsScene;

                m_UitkNextButton = root.Q<Button>("next-button");
                m_UitkNextButton.clicked += ShowNextWord;
            }
            else
            {
                if (uguiClueLabel == null)
                {
                    Debug.LogError($"{nameof(uguiClueLabel)} is not assigned for {GetType().Name}.");
                }

                if (uguiSuccessImage == null)
                {
                    Debug.LogError($"{nameof(uguiSuccessImage)} is not assigned for {GetType().Name}.");
                }

                if (uguiPauseButton == null)
                {
                    Debug.LogError($"{nameof(uguiPauseButton)} is not assigned for {GetType().Name}.");
                }

                if (uguiOptionsButton == null)
                {
                    Debug.LogError($"{nameof(uguiOptionsButton)} is not assigned for {GetType().Name}.");
                }

                if (uguiNextButton == null)
                {
                    Debug.LogError($"{nameof(uguiNextButton)} is not assigned for {GetType().Name}.");
                }

                uguiSuccessImage?.canvasRenderer.SetAlpha(0f);

                uguiPauseButton?.onClick.AddListener(PauseViewController.PauseGame);
                uguiOptionsButton?.onClick.AddListener(SceneTransitionManager.LoadSettingsScene);
                uguiNextButton?.onClick.AddListener(ShowNextWord);
            }
        }

        void CleanupUI()
        {
            if (m_UseUIToolkit)
            {
                PlayerSettingsDataSource.Release();

                m_UitkPauseButton.clicked -= PauseViewController.PauseGame;
                m_UitkOptionsButton.clicked -= SceneTransitionManager.LoadSettingsScene;
                m_UitkNextButton.clicked -= ShowNextWord;
            }
            else
            {
                uguiPauseButton?.onClick.RemoveListener(PauseViewController.PauseGame);
                uguiOptionsButton?.onClick.RemoveListener(SceneTransitionManager.LoadSettingsScene);
                uguiNextButton?.onClick.RemoveListener(ShowNextWord);
            }
        }

        void ShowNextWord()
        {
            if (m_UseUIToolkit)
            {
                m_UitkSuccessImage.style.opacity = 0f;
            }
            else
            {
                uguiSuccessImage?.gameObject.SetActive(false);
            }

            if (Gameplay.instance.IsShowingLastWord())
            {
                AudioManager.PlayResult(Gameplay.instance.reorderedWordCount == Gameplay.instance.words.Count);

                PauseViewController.EndGame(Gameplay.instance.reorderedWordCount, Gameplay.instance.words.Count);
            }
            else
            {
                Gameplay.instance.ShowNextWord();
            }
        }

        public void OnGameStateChanged(Gameplay.State state)
        {
            if (state != Gameplay.State.Playing)
            {
                return;
            }

            if (!m_UseUIToolkit)
            {
                var showClue = PlayerPrefs.GetInt(PlayerSettings.cluePreference, 1) == 1;

                uguiClueLabel.GetComponent<TextMeshProUGUI>().enabled = showClue;
                uguiClueLabel.GetComponent<AccessibleElement>().enabled = showClue;
            }
        }

        public void OnWordIndexChanged(int _)
        {
            var clue = Gameplay.instance.currentWord.clue;

            if (m_UseUIToolkit)
            {
                m_UitkClueLabel.text = clue;
            }
            else
            {
                uguiClueLabel.GetComponent<TextMeshProUGUI>().text = clue;
                uguiClueLabel.GetComponent<AccessibleElement>().value = clue;
            }
        }

        public void OnWordCompleted()
        {
            if (!m_UseUIToolkit)
            {
                m_AccessibilitySelectedCard = null;
            }

            StartCoroutine(DelayWordCompleted());
            return;

            IEnumerator DelayWordCompleted()
            {
                const float fadeDuration = 0.2f;
                FadeSuccessImageIn(fadeDuration);

                // This delay is needed to ensure that the screen reader has enough time to announce the word reordering.
                // It also ensures that the announcement is not ignored by the screen reader.
                const float announcementDelay = 1f;
                yield return new WaitForSeconds(announcementDelay);
                AssistiveSupport.notificationDispatcher.SendAnnouncement(k_SuccessAnnouncement);

                const float fadeOutDelay = 1f;
                yield return new WaitForSeconds(fadeOutDelay);
                FadeSuccessImageOut(fadeDuration);

                const float announcementDuration = 2.5f;
                const float nextWordDelay = announcementDuration - fadeOutDelay;
                yield return new WaitForSeconds(nextWordDelay);
                ShowNextWord();
            }

            void FadeSuccessImageIn(float duration)
            {
                if (m_UseUIToolkit)
                {
                    m_UitkSuccessImage.style.opacity = 1f;
                }
                else
                {
                    uguiSuccessImage?.gameObject.SetActive(true);
                    uguiSuccessImage?.CrossFadeAlpha(1f, duration, false);
                }
            }

            void FadeSuccessImageOut(float duration)
            {
                if (m_UseUIToolkit)
                {
                    m_UitkSuccessImage.style.opacity = 0f;
                }
                else
                {
                    uguiSuccessImage?.CrossFadeAlpha(0f, duration, false);
                }
            }
        }

        /// <summary>
        /// Regenerates all the letter cards.
        /// </summary>
        void OnLetterCardsChanged()
        {
            if (m_UseUIToolkit)
            {
                // Remove all cards.
                m_UitkLetterCardContainer.Clear();

                // Generate new cards.
                foreach (var letterCard in m_Model.letterCards)
                {
                    var card = new UITKLetterCard
                    {
                        letter = letterCard.letter.ToString().ToUpper()
                    };

                    m_UitkLetterCardContainer.Add(card);
                }
            }
            else
            {
                m_AccessibilityFocusedCard = null;

                // Remove all cards.
                foreach (Transform card in uguiLetterCardContainer)
                {
                    Destroy(card.gameObject);
                }

                // Generate new cards.
                foreach (var letterCardModel in m_Model.letterCards)
                {
                    var card = Instantiate(uguiLetterCardTemplate, uguiLetterCardContainer);
                    card.GetComponent<UGUILetterCard>().letter = letterCardModel.letter.ToString().ToUpper();
                    card.GetComponent<AccessibleElement>().selected += OnLetterCardSelected;
                }
            }

            if (Gameplay.instance != null && Gameplay.instance.state != Gameplay.State.Stopped)
            {
                AccessibilityManager.RefreshHierarchy();

                Invoke(nameof(MoveAccessibilityFocusOnClue), 1f);
            }
        }

        void MoveAccessibilityFocusOnClue()
        {
            if (!m_UseUIToolkit)
            {
                var nodeToFocus = uguiClueLabel.GetComponent<AccessibleElement>().node;
                AssistiveSupport.notificationDispatcher.SendLayoutChanged(nodeToFocus);
            }
        }

        /// <summary>
        /// Toggles the ability of the focused letter card to be reordered using the screen reader.
        /// </summary>
        bool OnLetterCardSelected()
        {
            if (!m_UseUIToolkit)
            {
                var letterCard = m_AccessibilityFocusedCard.GetComponent<UGUILetterCard>();

                if (m_AccessibilitySelectedCard == null)
                {
                    m_AccessibilitySelectedCard = letterCard;

                    // When a letter card is selected, deactivate all accessibility nodes except the ones corresponding to
                    // the letter cards to allow the selected card to be moved correctly.
                    AccessibilityManager.ActivateOtherAccessibilityNodes(false, uguiLetterCardContainer);
                }
                else
                {
                    m_AccessibilitySelectedCard = null;

                    AccessibilityManager.ActivateOtherAccessibilityNodes(true, uguiLetterCardContainer);
                }
            }

            return true;
        }

        /// <summary>
        /// Resets the selected card when the screen reader status changes.
        /// </summary>
        void OnScreenReaderStatusChanged(bool _)
        {
            if (!m_UseUIToolkit)
            {
                m_AccessibilitySelectedCard = null;
            }
        }

        void OnNodeFocusChanged(AccessibilityNode node)
        {
            if (!m_UseUIToolkit)
            {
                if (node != null)
                {
                    var element = AccessibilityManager.GetAccessibleElementForNode(node);
                    m_AccessibilityFocusedCard = element != null ? element.GetComponent<UGUILetterCard>() : null;
                    MoveSelectedCard();
                }
                else
                {
                    m_AccessibilityFocusedCard = null;
                }
            }
        }

        void MoveSelectedCard()
        {
            if (!AssistiveSupport.isScreenReaderEnabled)
            {
                return;
            }

            if (!m_UseUIToolkit)
            {
                if (m_AccessibilitySelectedCard == null || m_AccessibilityFocusedCard == null)
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
        }

        void MoveCard(bool shouldMoveLeft, int count)
        {
            if (!m_UseUIToolkit)
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
                    var announcement = $"Moved \"{draggable.name.ToUpper()}\" {(shouldMoveLeft ? "before" : "after")} \"{otherSibling.name.ToUpper()}\"";

                    // Announce that the card was moved.
                    AssistiveSupport.notificationDispatcher.SendAnnouncement(announcement);

                    AccessibilityManager.hierarchy.MoveNode(element.node, element.node.parent,
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
        }
    }
}
