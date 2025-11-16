using System;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using Unity.Properties;
using UnityEngine;
using UnityEngine.Accessibility;
using UnityEngine.UIElements;
using Unity.Samples.ScreenReader;
using UnityEngine.Localization;
using UnityEngine.Localization.SmartFormat.Extensions;
using UnityEngine.Localization.SmartFormat.PersistentVariables;
using UnityEngine.Localization.Settings;

namespace Unity.Samples.LetterSpell
{
    class MainView : MonoBehaviour
    {
        PlayerSettingsData m_PlayerSettings = new();
        StackView m_StackView;
        VisualElement m_MainView;
        VisualElement m_SplashView;
        VisualElement m_LoginView;
        Button m_LoginButton;
        Button m_EasyButton;
        Button m_HardButton;
        VisualElement m_MainMenu;
        VisualElement m_GameView;
        Label m_ClueLabel;
        VisualElement m_SuccessPill;
        LetterCardView m_LetterCardView;
        Button m_PauseGameButton;
        Button m_NextWordButton;
        Button m_ExitGameButton;
        Button m_ResumeGameButton;
        Popup m_ExitGamePopup;
        Popup m_ScreenResult;
        Label m_ResultLabel;
        Button m_ScreenResultMainMenuButton;
        Button m_ScreenResultPlayAgainButton;

        VisualElement m_SettingsView;
        Button m_CloseSettingsButton;
        Button m_SettingsButton;
        Button m_InGameSettingsButton;
        TextField m_SearchField;
        Label m_GameplayHeader;
        Label m_AudioHeader;
        Label m_AppearanceHeader;
        Label m_SettingsHeader;
        Label m_BoldTextLabel;
        Label m_FontScaleLabel;

        VisualElement m_LastView;
        LetterCardListModel m_Model = new();
        Gameplay.DifficultyLevel m_SelectedDifficultyLevel = Gameplay.DifficultyLevel.Hard;
        LetterCardViewItem m_AccessibilityFocusedCard; // The card that has the accessibility focus.
        
        Gameplay.DifficultyLevel selectedDifficultyLevel
        {
            get => m_SelectedDifficultyLevel;
            set
            {
                m_SelectedDifficultyLevel = value;
                UpdateChoiceButtons();
            }
        }

        void UpdateChoiceButtons()
        {
            m_HardButton.EnableInClassList("selected", m_SelectedDifficultyLevel == Gameplay.DifficultyLevel.Hard);
            m_EasyButton.EnableInClassList("selected", m_SelectedDifficultyLevel == Gameplay.DifficultyLevel.Easy);
        }

        LetterCardViewItem accessibilityFocusedCard
        {
            get => m_AccessibilityFocusedCard;
            set
            {
                if (m_AccessibilityFocusedCard == value)
                {
                    return;
                }

                m_AccessibilityFocusedCard?.Blur();
                m_AccessibilityFocusedCard = value;

                // Focus on the card that has the accessibility focus if no card is currently selected.
                // This can happen when the user is not dragging a card and just navigating the screen reader
                // focus using swipe gestures.
                // Note: we don't want to steal the focus if the user is dragging a card.
                if (m_AccessibilityFocusedCard != null && m_LetterCardView.selectedCard == null)
                {
                    m_AccessibilityFocusedCard.Focus();
                }
            }
        }

        public readonly float splashScreenDuration = 8; // 4000;

        /// <summary>
        /// The Gameplay manager.
        /// </summary>
        public Gameplay gameplay;

        void Start()
        {
            SetupUI();
        }

        void SetupUI()
        {
            var uiDoc = GetComponent<UIDocument>();
            var root = uiDoc.rootVisualElement;

            // Uncomment to enable the on-screen debug log.

            /*var debugPanel = new VisualElement() { name = "debugPanel" };
            debugPanel.style.position = Position.Absolute;
            debugPanel.style.bottom = 0;
            debugPanel.style.right = 0;
            debugPanel.style.paddingLeft = 5;
            debugPanel.style.paddingRight = 5;
            debugPanel.style.paddingTop = 2;
            debugPanel.style.paddingBottom = 2;
            debugPanel.style.backgroundColor = new Color(0, 0, 0, 0.5f);
            debugPanel.style.alignItems = Align.Center;
            debugPanel.style.flexDirection = FlexDirection.Row;
            debugPanel.style.justifyContent = Justify.SpaceBetween;
            debugPanel.AddToClassList("lsp-debug-view");
            var clearLogButton = new Button(() => OnScreenDebug.Clear());
            clearLogButton.text = "Clear Log";

            var logHierarchyButton = new Button(() => AssistiveSupport.activeHierarchy.Log());
            logHierarchyButton.text = "Dump Hierarchy";

            debugPanel.Add(clearLogButton);
            debugPanel.Add(logHierarchyButton);

            root.Add(debugPanel);
            */

            m_MainView = root.Q("root");

            // m_Logo = root.Q("logo");
            // m_Logo.style.display = DisplayStyle.None;

            m_StackView = root.Q<StackView>();

            m_SplashView = m_StackView.Q("splashView");

            // Disable screen reader for the label in the splash screen.
            m_SplashView.Q<Label>().GetOrCreateAccessibleProperties().ignored = true;

            m_LoginView = m_StackView.Q("loginView");
            m_LoginView.dataSource = m_PlayerSettings;
            
            m_LoginButton = m_LoginView.Q<Button>("nextButton");
            m_LoginButton.clicked += ShowLevelChoiceView;

            m_MainMenu = m_StackView.Q("mainMenu");

            m_EasyButton = m_MainMenu.Q<Button>("easyButton");
            m_EasyButton.clicked += () => ShowGameView(Gameplay.DifficultyLevel.Easy);

            m_HardButton = m_MainMenu.Q<Button>("hardButton");
            m_HardButton.clicked += () => ShowGameView(Gameplay.DifficultyLevel.Hard);

            UpdateChoiceButtons();

            m_GameView = m_StackView.Q("gameView");

            m_ClueLabel = m_GameView.Q<Label>("clueLabel");

            m_SuccessPill = m_GameView.Q("successPill");
            m_SuccessPill.GetOrCreateAccessibleProperties().ignored = true;
            m_SuccessPill.style.opacity = 0;

            m_LetterCardView = m_GameView.Q<LetterCardView>("letterCardView");
            m_LetterCardView.letterReordered += (_, oldIndex, newIndex) => { gameplay.ReorderLetter(oldIndex, newIndex); };

            m_PauseGameButton = m_GameView.Q<Button>("pauseGameButton");
            m_PauseGameButton.clicked += ShowExitGamePopup;
            var localizedPause = new LocalizedString
            {
                TableReference = "Game Text",
                TableEntryReference = "PAUSE_LABEL"
            };
            localizedPause.StringChanged += s => m_PauseGameButton.GetOrCreateAccessibleProperties().label = s;

            m_NextWordButton = m_GameView.Q<Button>("nextWordButton");
            m_NextWordButton.clicked += ShowNextWord;

            m_ScreenResult = root.Q<Popup>("resultPopup");
            m_ResultLabel = m_ScreenResult.Q<Label>("resultLabel");

            m_ScreenResultMainMenuButton = m_ScreenResult.Q<Button>("resultMainMenuButton");
            m_ScreenResultMainMenuButton.clicked += ExitGame;

            m_ScreenResultPlayAgainButton = m_ScreenResult.Q<Button>("resultPlayAgainButton");
            m_ScreenResultPlayAgainButton.clicked += StartGame;

            m_ExitGamePopup = root.Q<Popup>("exitGamePopup");

            m_ExitGameButton = m_ExitGamePopup.Q<Button>("exitGameButton");
            m_ExitGameButton.clicked += ExitGame;

            m_ResumeGameButton = m_ExitGamePopup.Q<Button>("resumeGameButton");
            m_ResumeGameButton.clicked += ResumeGame;

            m_SettingsView = m_StackView.Q("settingsView");
            m_SettingsView.dataSource = m_PlayerSettings;

            var settingsScrollView = m_SettingsView.Q<ScrollView>("settingsScrollView");
            var localizedSettings = new LocalizedString
            {
                TableReference = "Game Text",
                TableEntryReference = "BUTTON_OPTIONS"
            };
            localizedSettings.StringChanged += s => settingsScrollView.GetOrCreateAccessibleProperties().label = s;

            m_SearchField = m_SettingsView.Q<TextField>("settingsSearchField");
            m_SearchField.GetOrCreateAccessibleProperties().role = AccessibilityRole.SearchField;
            m_SearchField.RegisterValueChangedCallback(e => UpdateSearchField());

            m_GameplayHeader = m_SettingsView.Q<Label>("gameplayHeader");
            m_GameplayHeader.GetOrCreateAccessibleProperties().role = AccessibilityRole.Header;

            m_AudioHeader = m_SettingsView.Q<Label>("audioHeader");
            m_AudioHeader.GetOrCreateAccessibleProperties().role = AccessibilityRole.Header;

            m_AppearanceHeader = m_SettingsView.Q<Label>("appearanceHeader");
            m_AppearanceHeader.GetOrCreateAccessibleProperties().role = AccessibilityRole.Header;

            m_SettingsHeader = m_SettingsView.Q<Label>("settingsHeader");
            m_SettingsHeader.GetOrCreateAccessibleProperties().role = AccessibilityRole.Header;

            m_BoldTextLabel = m_SettingsView.Q<Label>("boldTextLabel");
            m_FontScaleLabel = m_SettingsView.Q<Label>("fontScaleLabel");

            m_CloseSettingsButton = m_SettingsView.Q<Button>("closeSettingsButton");
            m_CloseSettingsButton.clicked += CloseSettings;

            m_SettingsButton = root.Q<Button>("optionsButton");
            m_SettingsButton.clicked += ShowSettings;

            m_InGameSettingsButton = root.Q<Button>("inGameSettingsButton");
            m_InGameSettingsButton.clicked += ShowSettings;

            m_StackView.activeViewChanged += AccessibilityManager.RebuildHierarchy;

            // Initialize the values for the read-only settings.
            OnBoldTextStatusChanged(AccessibilitySettings.isBoldTextEnabled);
            OnClosedCaptioningStatusChanged(AccessibilitySettings.isClosedCaptioningEnabled);
            OnFontScaleValueChanged(AccessibilitySettings.fontScale);

            LocalizationSettings.SelectedLocaleChanged += loc =>
            {
                // Trigger the bound strings to update.
                m_PlayerSettings.Notify("boldTextEnabledText");
                m_PlayerSettings.Notify("closedCaptionsEnabledText");

                UpdateLangDirection(root);
            };

            UpdateLangDirection(root);
            ShowSplash();

            //root.Add(m_AnswerLabel = new Label());
            //m_AnswerLabel.style.position = Position.Absolute;

        }

        void UpdateLangDirection(VisualElement root)
        {
            if (root.panel == null)
                return;

            bool isRightToLeft = LocalizationSettings.SelectedLocale?.Identifier.CultureInfo.TextInfo.IsRightToLeft ?? false;

            // Update text direction
            root.languageDirection = isRightToLeft ? LanguageDirection.RTL : LanguageDirection.LTR;
            root.panel.visualTree.EnableInClassList("lsp-dir-ltr", !isRightToLeft);
            root.panel.visualTree.EnableInClassList("lsp-dir-rtl", isRightToLeft);
            gameplay.rightToLeft = isRightToLeft;
        }

        void OnEnable()
        {
            m_Model.letterCardsChanged += OnLetterCardsChanged;
            m_Model.gameplay = gameplay;

            // Update clue text is the clue setting changes.
            gameplay.stateChanged.AddListener(OnGameStateChanged);

            AssistiveSupport.nodeFocusChanged += OnNodeFocusChanged;
            AccessibilitySettings.boldTextStatusChanged += OnBoldTextStatusChanged;
            AccessibilitySettings.closedCaptioningStatusChanged += OnClosedCaptioningStatusChanged;
            AccessibilitySettings.fontScaleChanged += OnFontScaleValueChanged;
        }

        void OnDisable()
        {
            gameplay?.stateChanged.RemoveListener(OnGameStateChanged);
            m_Model.letterCardsChanged -= OnLetterCardsChanged;
            m_Model.gameplay = null;
            accessibilityFocusedCard = null;

            AccessibilitySettings.boldTextStatusChanged -= OnBoldTextStatusChanged;
            AccessibilitySettings.closedCaptioningStatusChanged -= OnClosedCaptioningStatusChanged;
            AccessibilitySettings.fontScaleChanged -= OnFontScaleValueChanged;

            AssistiveSupport.nodeFocusChanged -= OnNodeFocusChanged;
        }

        void OnBoldTextStatusChanged(bool boldTextStatus)
        {
            // Do it inline because using a USS class does not work (like :root.bold-text).
            // m_MainView.panel.visualTree.style.unityFontStyleAndWeight = boldTextStatus ? FontStyle.Bold : FontStyle.Normal;

            m_MainView.panel.visualTree.EnableInClassList("bold-text", boldTextStatus);

            m_BoldTextLabel.text = LocalizationSettings.StringDatabase.GetLocalizedString("Game Text", boldTextStatus ? "SETTING_ON" : "SETTING_OFF");
        }

        void OnClosedCaptioningStatusChanged(bool closedCaptioningStatus)
        {
        }

        void OnFontScaleValueChanged(float fontScale)
        {
            m_MainView.panel.visualTree.style.fontSize = 64 * fontScale;
            m_LetterCardView.ApplyFontScale();
            m_FontScaleLabel.text = $"{fontScale:0.00}";
        }

        void OnGameStateChanged(Gameplay.State state)
        {
            ShowOrHideClue();
        }

        public void ShowNextWord()
        {
            // m_SuccessImage.style.display = DisplayStyle.None;
            m_SuccessPill.style.opacity = 0;
            accessibilityFocusedCard = null;

            // if (gameplay.IsGameComplete())
            if (gameplay.IsShowingLastWord())
            {
                m_LetterCardView.interactable = false;
                AudioManager.instance.PlayResult(gameplay.reorderedWordCount == gameplay.words.Count);
                gameplay.StopGame();
                ShowResults(gameplay.reorderedWordCount, gameplay.words.Count);
            }
            else
            {
                m_LetterCardView.interactable = true;
                gameplay.ShowNextWord();
                DelayStateLetters();
            }

            //m_AnswerLabel.text = gameplay.currentWord.word;
            AccessibilityManager.RebuildHierarchy();
        }

        void ShowResults(int orderedWordCount, int totalWordCount)
        {
            var localizedString = m_ResultLabel.GetBinding("text") as LocalizedString;
            var orderedWordCountValue = localizedString?["orderedWordCount"] as IntVariable;
            var totalWordCountValue = localizedString?["totalWordCount"] as IntVariable;

            PersistentVariablesSource.BeginUpdating();

            if (orderedWordCountValue != null)
            {
                orderedWordCountValue.Value = orderedWordCount;
            }

            if (totalWordCountValue != null)
            {
                totalWordCountValue.Value = totalWordCount;
            }

            PersistentVariablesSource.EndUpdating();

            m_ScreenResult.Show();

            // Ensure the clue label always the same space in the view so do not hide it.
            m_ClueLabel.text = "";
            m_ClueLabel.style.visibility = Visibility.Hidden;
        }

        public void OnCurrentWordIndexChanged(int index)
        {
            var clue = gameplay.currentWord.clue;

            m_ClueLabel.text = clue;
            //m_ClueLabel.GetOrCreateAccessibleProperties().label = clue;

            ShowOrHideClue();
        }

        void ShowOrHideClue()
        {
            m_ClueLabel.style.visibility = m_PlayerSettings.showSpellingClues ?
                Visibility.Visible : Visibility.Hidden;
        }

        public void StartGame()
        {
            m_ScreenResult.Close();
            m_LetterCardView.interactable = true;
            gameplay.StartGame();

            AccessibilityManager.RebuildHierarchy();
            DelayStateLetters();
        }

        public void PauseGame()
        {
            gameplay.PauseGame();
        }

        void UpdateSearchField()
        {
            var searchText = m_SearchField.text.Trim().ToLowerInvariant();

            foreach (var label in m_SettingsView.Query<Label>(className: "unity-base-field__label").ToList())
            {
                if (string.IsNullOrEmpty(searchText))
                {
                    label.parent.style.display = DisplayStyle.Flex;
                    continue;
                }

                if (label.text.ToLowerInvariant().Contains(searchText))
                {
                    label.parent.style.display = DisplayStyle.Flex;
                }
                else if (label.parent is not RadioButton)
                {
                    label.parent.style.display = DisplayStyle.None;
                }
            }
        }

        /// <summary>
        /// Regenerates all the letter cards.
        /// </summary>
        void OnLetterCardsChanged()
        {
            accessibilityFocusedCard = null;

            m_LetterCardView.letters = m_Model.letterCards.Select((letterCard) => letterCard.letter).ToArray();
        }

        void DelayStateLetters()
        {
            m_MainView.schedule.Execute(StateLetters).ExecuteLater(1000);
        }

        void StateLetters()
        {
            var cultureInfo = LocalizationSettings.SelectedLocale?.Identifier.CultureInfo ?? CultureInfo.CurrentUICulture;
            var letterList = m_Model.letterCards.Select(c =>
                "\"" + char.ToUpper(c.letter, cultureInfo) + "\""
            ).ToArray();

            if (gameplay.rightToLeft)
            {
                letterList = letterList.Reverse().ToArray();
            }

            var localizedString = new LocalizedString
            {
                TableReference = "Game Text",
                TableEntryReference = "ANNOUNCEMENT_LETTERS"
            };

            var letters = new StringVariable
            {
                Value = string.Join(", ", letterList)
            };

            localizedString.Add("letters", letters);

            localizedString.StringChanged += announcement =>
                AssistiveSupport.notificationDispatcher.SendAnnouncement(announcement);
        }

        public void OnWordReorderingCompleted()
        {
            m_LetterCardView.interactable = false;
            m_MainView.schedule.Execute(_ => AnnounceCorrectWord()).ExecuteLater(2000);

        }

        void AnnounceCorrectWord()
        {
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

            FadeSuccessImageIn();
        }

        void FadeSuccessImageIn()
        {
            m_SuccessPill.style.opacity = 1;

            m_MainView.schedule.Execute(_ => FadeSuccessImageOut()).ExecuteLater(5000);
        }

        void FadeSuccessImageOut()
        {
            ShowNextWord();
        }

        void OnNodeFocusChanged(AccessibilityNode node)
        {
            if (node == null)
            {
                return;
            }

            var element = UITkAccessibilityManager.instance?.GetVisualElementForNode(m_MainView.panel, node);

            accessibilityFocusedCard = element as LetterCardViewItem;

            MoveSelectedCardOnAssistedFocus();
        }

        void MoveSelectedCardOnAssistedFocus()
        {
            if (!AssistiveSupport.isScreenReaderEnabled ||
                m_LetterCardView.selectedCard == null ||
                accessibilityFocusedCard == null)
            {
                return;
            }

            // If we reach this code, it means we're dragging the card.
            var selectedCardIndex = m_LetterCardView.IndexOf(m_LetterCardView.selectedCard);
            var focusedCardIndex = m_LetterCardView.IndexOf(accessibilityFocusedCard);

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

        public void OnSwipeLeft()
        {
            MoveCard(true);
        }

        public void OnSwipeRight()
        {
            MoveCard(false);
        }

        void MoveCard(bool shouldMoveLeft, int count = 1)
        {
            var draggable = accessibilityFocusedCard;

            if (draggable == null || count == 0)
            {
                return;
            }
            
            bool moved = m_LetterCardView.MoveSelectedCard(shouldMoveLeft, count);

            if (moved)
            {
                var node = UITkAccessibilityManager.instance.GetNodeForVisualElement(m_LetterCardView.selectedCard);
                
                AssistiveSupport.notificationDispatcher.SendLayoutChanged(node);
            }
        }

        void ShowSplash()
        {
            m_StackView.index = 0;

            Invoke(nameof(DelayShowLogin), splashScreenDuration);
        }

        void DelayShowLogin()
        {
            // m_SettingsButton.style.display = DisplayStyle.None;
            // m_Logo.style.display = DisplayStyle.Flex;

            m_StackView.activeView = m_LoginView;
        }

        void ShowLevelChoiceView()
        {
            m_StackView.activeView = m_MainMenu;
            // m_SettingsButton.style.display = DisplayStyle.Flex;
        }

        void ShowGameView(Gameplay.DifficultyLevel level)
        {
            PlayerPrefs.SetInt("GameDifficulty", (int)level);

            m_StackView.activeView = m_GameView;
            // m_SettingsButton.style.display = DisplayStyle.None;
            m_LetterCardView.interactable = true;
            // CardListView.cardSize = level == Gameplay.DifficultyLevel.Easy ? 208 : 100;
            gameplay.StartGame();
            DelayStateLetters();
        }

        void ShowExitGamePopup()
        {
            m_ExitGamePopup.Show();
        }

        void CloseExitGamePopup()
        {
            m_ExitGamePopup.Close();
        }

        void ResumeGame()
        {
            CloseExitGamePopup();
            gameplay.ResumeGame();
            m_StackView.activeView = m_GameView;
        }


        void ExitGame()
        {
            gameplay.StopGame();
            m_ScreenResult.Close();
            CloseExitGamePopup();
            ShowLevelChoiceView();
        }

        void ShowSettings()
        {
            // AssistiveSupport.activeHierarchy?.Log();
            m_LastView = m_StackView.activeView;
            m_StackView.activeView = m_SettingsView;
            // m_Logo.style.display = DisplayStyle.None;
            // m_SettingsButton.style.display = DisplayStyle.None;
        }

        void CloseSettings()
        {
            m_StackView.activeView = m_LastView;
            // m_Logo.style.display = DisplayStyle.Flex;
            // m_SettingsButton.style.display = (m_LastView == m_LevelChoiceView) ? DisplayStyle.Flex : DisplayStyle.None;

            if (m_LastView == m_GameView)
            {
                ShowOrHideClue();
            }
        }
    }
}
