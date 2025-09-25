using System;
using System.Runtime.CompilerServices;
using Unity.Properties;
using Unity.Samples.LetterSpell;
using UnityEngine;
using UnityEngine.Accessibility;
using UnityEngine.UIElements;
using Unity.Samples.ScreenReader;

namespace Unity.Samples.LetterSpell
{
    class PlayerSettingsData : INotifyBindablePropertyChanged
    {
        private long m_Hash = 0;
        const string k_UsernamePref = "Username";
        const string k_DifficultyPref = "GameDifficulty";
        const string k_WordsPref = "GameWords";
        public const string k_CluePref = "ShowClues";
        public const string k_SoundEffectsPref = "SoundEffectsVolume";
        public const string k_MusicPref = "MusicVolume";
        const string k_ColorThemePref = "ColorTheme";
        const string k_DisplaySizePref = "DisplaySize";
        const string k_SettingOn = "On";
        const string k_SettingOff = "Off";

        [CreateProperty]
        public string userName
        {
            get => PlayerPrefs.GetString(k_UsernamePref);
            set
            {
                if (userName == value)
                    return;
                PlayerPrefs.SetString(k_UsernamePref, value);
                Notify();
            }
        }

        [CreateProperty]
        public int difficultyLevel
        {
            get => PlayerPrefs.GetInt(k_DifficultyPref, 0);
            set
            {
                if (difficultyLevel == value)
                    return;
                PlayerPrefs.SetInt(k_DifficultyPref, value);
                Notify();
            }
        }

        [CreateProperty]
        public bool isThreeWords
        {
            get => wordsCount == 0;
            set { wordsCount = value ? 0 : 1; }
        }

        [CreateProperty]
        public bool isSixWords
        {
            get => wordsCount == 1;
            set { wordsCount = value ? 1 : 0; }
        }

        [CreateProperty]
        public int wordsCount
        {
            get => PlayerPrefs.GetInt(k_WordsPref, 0);
            set
            {
                if (wordsCount == value)
                    return;
                PlayerPrefs.SetInt(k_WordsPref, value);
                Notify(nameof(isSixWords));
                Notify(nameof(isThreeWords));
            }
        }

        [CreateProperty]
        public bool showsSpellingClues
        {
            get => PlayerPrefs.GetInt(k_CluePref, 0) == 1;
            set
            {
                if (showsSpellingClues == value)
                    return;
                PlayerPrefs.SetInt(k_CluePref, value ? 1 : 0);
                Notify();
            }
        }

        [CreateProperty]
        public float soundEffectVolume
        {
            get => PlayerPrefs.GetFloat(k_SoundEffectsPref, 0.5f);
            set
            {
                if (Mathf.Approximately(soundEffectVolume, value))
                    return;
                PlayerPrefs.SetFloat(k_SoundEffectsPref, value);
                Notify();
            }
        }

        [CreateProperty]
        public float musicVolume
        {
            get => PlayerPrefs.GetFloat(k_MusicPref, 0.5f);
            set
            {
                if (Mathf.Approximately(musicVolume, value))
                    return;
                PlayerPrefs.SetFloat(k_MusicPref, value);
                Notify();
            }
        }

        [CreateProperty]
        public int colorTheme
        {
            get => PlayerPrefs.GetInt(k_ColorThemePref, 0);
            set
            {
                if (colorTheme == value)
                    return;
                PlayerPrefs.SetInt(k_ColorThemePref, value);
                Notify();
            }
        }

        [CreateProperty]
        public float displaySize
        {
            get => PlayerPrefs.GetFloat(k_DisplaySizePref, 0.5f);
            set
            {
                if (Mathf.Approximately(displaySize, value))
                    return;
                PlayerPrefs.SetFloat(k_DisplaySizePref, value);
                Notify();
            }
        }

        [CreateProperty]
        public string closedCaptionsEnabledText
        {
            get
            {
                return AccessibilitySettings.isClosedCaptioningEnabled ? k_SettingOn : k_SettingOff;
            }
        }
        
        
        [CreateProperty]
        public string boldTextEnabledText
        {
            get
            {
                return AccessibilitySettings.isBoldTextEnabled ? k_SettingOn : k_SettingOff;;
            }
        }

        public event EventHandler<BindablePropertyChangedEventArgs> propertyChanged;

        void Notify([CallerMemberName] string property = "")
        {
            propertyChanged?.Invoke(this, new BindablePropertyChangedEventArgs(property));
        }
    }

    class MainView : MonoBehaviour
    {
        private PlayerSettingsData m_PlayerSettings = new PlayerSettingsData();
        private StackView m_StackView;
        private VisualElement m_MainView;
        private VisualElement m_Logo;
        private VisualElement m_SplashView;
        private VisualElement m_LoginView;
        private Button m_LoginButton;
        private Button m_EasyButton;
        private Button m_HardButton;
        private Button m_StartGameButton;
        private VisualElement m_LevelChoiceView;

        private VisualElement m_GameView;
        private Label m_ClueLabel;
        private VisualElement m_SuccessImage;
        private CardListView m_LetterCardContainer;
        private Button m_PauseGameButton;
        private Button m_NextWordButtton;
        private Button m_ExitGameButton;
        private Button m_ResumeGameButton;
        private Popup m_ExitGamePopup;
        private Popup m_ScreenResult;
        private Label m_ResultLabel;
        private Button m_ScreenResultMainMenuButton;
        private Button m_ScreenResultPlayAgainButton;
        private VisualElement m_SettingsView;
        private Button m_CloseSettingsButton;
        private Button m_SettingsButton;
        private Button m_InGameSettingsButton;
        private VisualElement m_LastView;
        LetterCardListModel m_Model = new();

        private Gameplay.DifficultyLevel m_SelectedDifficultyLevel = Gameplay.DifficultyLevel.Hard;

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

        /// <summary>
        /// The focused card.
        /// </summary>
        UITkLetterCard m_AccessibilityFocusedCard;
   
        /// <summary>
        /// Keeps track of whether the hierarchy was refreshed using AccessibilityManager.RefreshHierarchy();
        /// </summary>
        bool m_WasHierarchyRefreshed;
        
        public int splashScreenDuration = 2000;

        /// <summary>
        /// The Gameplay manager.
        /// </summary>
        public Gameplay gameplay;
        
        // Start is called before the first frame update
        void Start()
        {
            SetupUI();
        }

        void SetupUI()
        {
            var uiDoc = GetComponent<UIDocument>();
            var root = uiDoc.rootVisualElement;
            
            var debugPanel = new VisualElement() { name = "debugPanel" };
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
            m_MainView = root.Q("root");
            m_Logo = root.Q("logo");
            m_Logo.style.display = DisplayStyle.None;
            m_StackView = root.Q<StackView>();
            m_SplashView = m_StackView.Q("splashView");
            m_LoginView = m_StackView.Q("loginView");
            m_LoginView.dataSource = m_PlayerSettings;
            m_LoginButton = m_LoginView.Q<Button>("loginButton");
            m_LoginButton.clicked += ShowLevelChoiceView;
            m_LevelChoiceView = m_StackView.Q("levelChoiceView");
            m_EasyButton = m_LevelChoiceView.Q<Button>("easyButton");
            m_EasyButton.clicked += () => ShowGameView(Gameplay.DifficultyLevel.Easy);//selectedDifficultyLevel = Gameplay.DifficultyLevel.Easy;
            m_HardButton = m_LevelChoiceView.Q<Button>("hardButton");
            m_HardButton.clicked += () => ShowGameView(Gameplay.DifficultyLevel.Hard);//selectedDifficultyLevel = Gameplay.DifficultyLevel.Hard;
            m_StartGameButton = m_LevelChoiceView.Q<Button>("startGameButton");
            m_StartGameButton.clicked += () => ShowGameView(selectedDifficultyLevel);
            m_StartGameButton.style.display = DisplayStyle.None;
            UpdateChoiceButtons();
            
            m_GameView = m_StackView.Q("gameView");
            m_ClueLabel = m_GameView.Q<Label>("clueLabel");
            m_SuccessImage = m_GameView.Q("successImage");
            m_SuccessImage.style.display = DisplayStyle.None;
            m_LetterCardContainer = m_GameView.Q<LetterSpell.CardListView>("letterCardContainer");
            m_PauseGameButton = m_GameView.Q<Button>("pauseGameButton");
            m_PauseGameButton.clicked += ShowExitGamePopup;

            m_NextWordButtton = m_GameView.Q<Button>("nextWordButton");
            m_NextWordButtton.clicked += ShowNextWord;

            m_ScreenResult = root.Q<Popup>("resultScreen");
            m_ScreenResult.AddToClassList("unity-modal");
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

            //m_SettingsPopup = new PopupWindow();
            //m_SettingsPopup.content = m_SettingsView;
            
            m_CloseSettingsButton = m_SettingsView.Q<Button>("closeSettingsButton");
            m_CloseSettingsButton.clicked += CloseSettings;
            m_SettingsButton = root.Q<Button>("settingsButton");
            m_SettingsButton.style.display = DisplayStyle.None;
            m_SettingsButton.clicked += ShowSettings;

            m_InGameSettingsButton = root.Q<Button>("inGameSettingsButton");
            m_InGameSettingsButton.clicked += ShowSettings;
            
            m_StackView.activeViewChanged += DelayRefreshHierarchy;
            
            
            // Initialize the values for the read-only settings.
            OnBoldTextStatusChanged(AccessibilitySettings.isBoldTextEnabled);
            OnClosedCaptioningStatusChanged(AccessibilitySettings.isClosedCaptioningEnabled);
            OnFontScaleValueChanged(AccessibilitySettings.fontScale);
            
            ShowSplash();
        }

        void OnEnable()
        {
            m_Model.letterCardsChanged += OnLetterCardsChanged;
            m_Model.gameplay = gameplay;

            // Update clue text is the clue setting changes.
            gameplay.stateChanged.AddListener(OnGameStateChanged);

            AssistiveSupport.nodeFocusChanged += OnNodeFocusChanged;
            OnScreenDebug.Log("MainWindow.OnEnable");
            
            AccessibilitySettings.boldTextStatusChanged += OnBoldTextStatusChanged;
            AccessibilitySettings.closedCaptioningStatusChanged += OnClosedCaptioningStatusChanged;
            AccessibilitySettings.fontScaleChanged += OnFontScaleValueChanged;
        }

        void OnDisable()
        {
            gameplay?.stateChanged.RemoveListener(OnGameStateChanged);
            m_Model.letterCardsChanged -= OnLetterCardsChanged;
            m_Model.gameplay = null;
            m_AccessibilityFocusedCard = null;

            AccessibilitySettings.boldTextStatusChanged -= OnBoldTextStatusChanged;
            AccessibilitySettings.closedCaptioningStatusChanged -= OnClosedCaptioningStatusChanged;
            AccessibilitySettings.fontScaleChanged -= OnFontScaleValueChanged;
            
            AssistiveSupport.nodeFocusChanged -= OnNodeFocusChanged;
        }

        void OnBoldTextStatusChanged(bool boldTextStatus)
        {
            // Do it inline because using a USS class does not work (like :root.bold-text).
            //m_MainView.panel.visualTree.style.unityFontStyleAndWeight = boldTextStatus ? FontStyle.Bold : FontStyle.Normal;
            
            m_MainView.panel.visualTree.EnableInClassList("bold-text", true);
        }

        void OnClosedCaptioningStatusChanged(bool closedCaptioningStatus)
        {
        }

        void OnFontScaleValueChanged(float fontScale)
        {
        }

        void OnGameStateChanged(Gameplay.State state)
        {
            ShowOrHideClue();
        }
        
        public void ShowNextWord()
        {
            m_SuccessImage.style.display = DisplayStyle.None;

            //if (gameplay.IsGameComplete())
            if (gameplay.IsShowingLastWord())
            {
                AudioManager.instance.PlayResult(gameplay.reorderedWordCount == gameplay.words.Count);

                m_MainView.schedule.Execute(() =>
                {
                    AssistiveSupport.notificationDispatcher.SendAnnouncement(
                        $"The game is over! you found {gameplay.reorderedWordCount} words out of {gameplay.words.Count}");
                }).ExecuteLater(2000);

                gameplay.StopGame();
                ShowResults(gameplay.reorderedWordCount, gameplay.words.Count);
            }
            else
            {
                gameplay.ShowNextWord();
            }

            DelayRefreshHierarchy();
        }
        
        void DelayRefreshHierarchy()
        {
            var service = AccessibilityManager.GetService<UITkAccessibilityService>();

            if (service != null)
            {
                this.DelayRefreshHierarchy(service);
            }
        }

        void ShowResults(int orderedWordCount, int totalWordCount)
        {
            m_ResultLabel.text = $"{orderedWordCount} of {totalWordCount} correct";
            m_ScreenResult.Show();
            //m_ClueLabel.style.display = DisplayStyle.None;
            // Ensure the clue label always the same space in the view
            // so do not hide it
            m_ClueLabel.text = " ";
            m_ClueLabel.style.visibility = Visibility.Hidden;
        }

        public void OnCurrentWordIndexChanged(int index)
        {
            var clue = gameplay.currentWord.clue;

            m_ClueLabel.text = clue;
            ShowOrHideClue();
        }

        void ShowOrHideClue()
        {
            if (m_PlayerSettings.showsSpellingClues)
            {
                m_ClueLabel.style.visibility = Visibility.Visible;
            }
            else
            {
                m_ClueLabel.style.visibility = Visibility.Hidden;
            }
        }

        public void StartGame()
        {
            m_ScreenResult.Close();
            Gameplay.instance.StartGame();
            DelayRefreshHierarchy();
        }

        public void PauseGame()
        {
            Gameplay.instance.PauseGame();
        }

        /// <summary>
        /// Regenerates all the letter cards.
        /// </summary>
        void OnLetterCardsChanged()
        {
            m_AccessibilityFocusedCard = null;

            // Remove all cards.
            m_LetterCardContainer.Clear();

            // Generate new cards.
            foreach (var letterCard in m_Model.letterCards)
            {
                var card = new UITkLetterCard();
                m_LetterCardContainer.Add(card);
                card.text = letterCard.letter.ToString();
                card.name = letterCard.letter.ToString();
                card.GetOrCreateAccessibleProperties().label = card.text;
                card.dropped += (oldIndex, newIndex) => { gameplay.ReorderLetter(oldIndex, newIndex); };
            }

            m_MainView.schedule.Execute(() => FocusOnClue()).ExecuteLater(1000);
            return;

            void FocusOnClue()
            {
                // var clueAcc = clueText.GetComponent<AccessibleElement>();
                //  AssistiveSupport.notificationDispatcher.SendLayoutChanged(clueAcc.node);
            }
        }

        public void OnWordReorderingCompleted()
        {
            AssistiveSupport.notificationDispatcher.SendAnnouncement(
                $"You found the correct word! It was {gameplay.currentWord.word}.");

            FadeSuccessImageIn();

            /*Invoke(nameof(FadeSuccessImageOut), 1.7f);
            Invoke(nameof(ShowNextWord), 2f);*/
        }

        void FadeSuccessImageIn()
        {
            m_SuccessImage.style.display = DisplayStyle.Flex;
            m_MainView.schedule.Execute((t) => FadeSuccessImageOut()).ExecuteLater(3000);
        }

        void FadeSuccessImageOut()
        {
            ShowNextWord();
        }

        void OnNodeFocusChanged(AccessibilityNode node)
        {
            if (node != null)
            {
                var service = AccessibilityManager.GetService<UITkAccessibilityService>();
                var element = service.GetVisualElementForNode(m_MainView.panel, node);

                m_AccessibilityFocusedCard = element as UITkLetterCard;
                MoveSelectedCardOnAssistedFocus();
            }
            else
            {
                m_AccessibilityFocusedCard = null;
            }
        }

        void MoveSelectedCardOnAssistedFocus()
        {
            if (!AssistiveSupport.isScreenReaderEnabled
                || m_LetterCardContainer.selectedCard == null
                || m_AccessibilityFocusedCard == null)
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
            var selectedCardIndex = m_LetterCardContainer.IndexOf(m_LetterCardContainer.selectedCard);
            var focusedCardIndex = m_LetterCardContainer.IndexOf(m_AccessibilityFocusedCard);

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
            var draggable = m_AccessibilityFocusedCard;
            if (draggable == null || count == 0)
            {
                return;
            }

            //OnScreenDebug.Log("MoveCard " + (shouldMoveLeft ? "left" : "right" + " count " + count));
            if (shouldMoveLeft)
                m_LetterCardContainer.selectedCard.MoveLeft(count);
            else
            {
                m_LetterCardContainer.selectedCard.MoveRight(count);
            }

            var updater = m_LetterCardContainer.selectedCard.panel.GetAccessibilityUpdater();
            var node = updater.GetNodeForVisualElement(m_LetterCardContainer.selectedCard);
            AssistiveSupport.notificationDispatcher.SendLayoutChanged(node);
            /*var accElement = draggable.transform.GetComponent<AccessibleElement>();
            if (shouldMoveLeft ? draggable.MoveLeft() : draggable.MoveRight())
            {
                var index = draggable.transform.GetSiblingIndex();
                var otherSiblingIndex = shouldMoveLeft ? index + 1 : index - 1;
                var otherSibling = draggable.transform.parent.GetChild(otherSiblingIndex);
                var message = $"Moved {draggable.name} {(shouldMoveLeft ? "before" : "after")} {otherSibling.name}";

                // Announce that the card was moved.
                AssistiveSupport.notificationDispatcher.SendAnnouncement(message);

                AccessibilityManager.hierarchy.MoveNode(accElement.node, accElement.node.parent,
                    accElement.transform.GetSiblingIndex());

                // Only refresh the frames for now to leave the announcement request to be handled.
                this.ManualRectRefresh();

                AssistiveSupport.notificationDispatcher.SendLayoutChanged(accElement.node);
            }*/
        }

        void ShowSplash()
        {
            m_StackView.index = 0;
            m_StackView.schedule.Execute(() =>
            {
                m_SettingsButton.style.display = DisplayStyle.None;
                m_Logo.style.display = DisplayStyle.Flex;
                m_StackView.activeView = m_LoginView;
            }).ExecuteLater(splashScreenDuration);
        }

        void ShowLevelChoiceView()
        {
            m_StackView.activeView = m_LevelChoiceView;
            m_SettingsButton.style.display = DisplayStyle.Flex;
        }

        void ShowGameView(Gameplay.DifficultyLevel level)
        {
            PlayerPrefs.SetInt("GameDifficulty", (int)level);

            m_StackView.activeView = m_GameView;
            m_SettingsButton.style.display = DisplayStyle.None;
           // CardListView.cardSize = level == Gameplay.DifficultyLevel.Easy ? 208 : 100;
            gameplay.StartGame();
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
            Gameplay.instance.ResumeGame();
            m_StackView.activeView = m_GameView;
        }


        void ExitGame()
        {
            Gameplay.instance.StopGame();
            m_ScreenResult.Close();
            CloseExitGamePopup();
            ShowLevelChoiceView();
        }

        void ShowSettings()
        {
            //AssistiveSupport.activeHierarchy?.Log();
            m_LastView = m_StackView.activeView;
            m_StackView.activeView = m_SettingsView;
            m_Logo.style.display = DisplayStyle.None;
            m_SettingsButton.style.display = DisplayStyle.None;
        }

        void CloseSettings()
        {
            m_StackView.activeView = m_LastView;
            m_Logo.style.display = DisplayStyle.Flex;
            m_SettingsButton.style.display = (m_LastView == m_LevelChoiceView) ? DisplayStyle.Flex : DisplayStyle.None;
            if (m_LastView == m_GameView)
                ShowOrHideClue();
        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}
