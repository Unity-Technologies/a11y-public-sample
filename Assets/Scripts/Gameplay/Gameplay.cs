using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Accessibility;
using UnityEngine.Events;
using UnityEngine.Localization;
using UnityEngine.Pool;

namespace Unity.Samples.LetterSpell
{
    /// <summary>
    /// The gameplay manager.
    /// </summary>
    class Gameplay : MonoBehaviour
    {
        /// <summary>
        /// The state of the game.
        /// </summary>
        public enum State
        {
            Stopped,
            Playing,
            Paused
        }

        /// <summary>
        /// The difficulty level of the game.
        /// </summary>
        public enum DifficultyLevel
        {
            Easy,
            Hard
        }

        public static Gameplay instance;

        WordDatabase m_WordDatabase;

        /// <summary>
        /// The database of words.
        /// </summary>
        public LocalizedAsset<WordDatabase> localizedWordDatabase;

        List<WordData> m_Words = new();

        /// <summary>
        /// The list of words to complete.
        /// </summary>
        public IReadOnlyList<WordData> words => m_Words.AsReadOnly();

        int m_CurrentWordIndex = -1;

        /// <summary>
        /// The current word to reorder.
        /// </summary>
        public WordData currentWord => m_CurrentWordIndex != -1 ? m_Words[m_CurrentWordIndex] : default;

        /// <summary>
        /// The current state of the word being reordered.
        /// </summary>
        public char[] currentWordState { get; private set; }

        /// <summary>
        /// The number of words that were successfully reordered.
        /// </summary>
        public int reorderedWordCount { get; private set; }

        /// <summary>
        /// Sent when the current word has been changed.
        /// </summary>
        public UnityEvent<int> wordIndexChanged = new();

        /// <summary>
        /// Sent when the current word has been reordered.
        /// </summary>
        public UnityEvent<int, int> wordReordered = new();

        /// <summary>
        /// Sent when the current word has been completed.
        /// </summary>
        public UnityEvent wordCompleted = new();

        /// <summary>
        /// Sent when the game has started.
        /// </summary>
        public UnityEvent gameStarted = new();

        /// <summary>
        /// Sent when the game has finished.
        /// </summary>
        public UnityEvent gameEnded = new();

        State m_State;

        /// <summary>
        /// The state of the game.
        /// </summary>
        public State state
        {
            get => m_State;
            private set
            {
                if (m_State == value)
                {
                    return;
                }

                m_State = value;
                stateChanged?.Invoke(value);
            }
        }

        /// <summary>
        /// Sent when the state of the game has changed.
        /// </summary>
        public UnityEvent<State> stateChanged = new();

        /// <summary>
        /// The difficulty level of the game.
        /// </summary>
        static DifficultyLevel difficultyLevel =>
            PlayerPrefs.GetInt(PlayerSettings.difficultyPreference, (int)DifficultyLevel.Easy) switch
        {
            (int)DifficultyLevel.Easy => DifficultyLevel.Easy,
            (int)DifficultyLevel.Hard => DifficultyLevel.Hard,
            _ => DifficultyLevel.Easy
        };

        System.Random m_Randomizer = new();

        void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
        }

        void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }

        void OnEnable()
        {
            localizedWordDatabase.AssetChanged += UpdateWordDatabase;
        }

        void OnDisable()
        {
            localizedWordDatabase.AssetChanged -= UpdateWordDatabase;
        }

        void UpdateWordDatabase(WordDatabase database)
        {
            m_WordDatabase = database;

            StopGame();
            StartGame();
        }

        /// <summary>
        /// Starts a new game.
        /// </summary>
        public void StartGame()
        {
            if (state != State.Stopped)
            {
                return;
            }

            reorderedWordCount = 0;
            state = State.Playing;

            RebuildWords();
            ShowNextWord();

            gameStarted?.Invoke();
        }

        /// <summary>
        /// Stops the current game.
        /// </summary>
        public void StopGame()
        {
            if (state == State.Stopped)
            {
                return;
            }

            state = State.Stopped;
            SetWordIndex(-1);

            gameEnded?.Invoke();
        }

        /// <summary>
        /// Pauses the current game.
        /// </summary>
        public void PauseGame()
        {
            if (state == State.Playing)
            {
                state = State.Paused;
            }
        }

        /// <summary>
        /// Resumes the current game.
        /// </summary>
        public void ResumeGame()
        {
            if (state == State.Paused)
            {
                state = State.Playing;
            }
        }

        /// <summary>
        /// Generates the list of words to reorder.
        /// </summary>
        void RebuildWords()
        {
            m_Words.Clear();

            using var _ = HashSetPool<int>.Get(out var indexesAlreadyAdded);
            var wordsSource = difficultyLevel == DifficultyLevel.Easy
                ? m_WordDatabase.words.easy
                : m_WordDatabase.words.hard;

            var wordCount = (PlayerPrefs.GetInt(PlayerSettings.wordsPreference, 0) + 1) * 3;

            // Randomly pick words in the database.
            while (m_Words.Count < wordCount)
            {
                var index = m_Randomizer.Next(0, wordsSource.Length);

                if (indexesAlreadyAdded.Contains(index))
                {
                    continue;
                }

                indexesAlreadyAdded.Add(index);
                m_Words.Add(wordsSource[index]);
            }
        }

        /// <summary>
        /// Shows the next word to reorder.
        /// </summary>
        public void ShowNextWord()
        {
            if (IsShowingLastWord())
            {
                StopGame();
            }
            else
            {
                SetWordIndex(m_CurrentWordIndex + 1);
            }
        }

        void SetWordIndex(int index)
        {
            if (m_CurrentWordIndex == index)
            {
                return;
            }

            m_CurrentWordIndex = index;
            InitializeCurrentWordState();

            wordIndexChanged?.Invoke(index);
        }

        void InitializeCurrentWordState()
        {
            if (!string.IsNullOrEmpty(currentWord.word))
            {
                do
                {
                    currentWordState = new char[currentWord.word.Length];

                    // Shuffle the letters.
                    currentWord.word.CopyTo(0, currentWordState, 0, currentWord.word.Length);

                    for (var n = currentWordState.Length; n > 1;)
                    {
                        var k = m_Randomizer.Next(n);
                        --n;
                        (currentWordState[n], currentWordState[k]) = (currentWordState[k], currentWordState[n]);
                    }
                }

                // Make sure it is not the original word.
                while (IsWordComplete());
            }
            else
            {
                currentWordState = null;
            }
        }

        /// <summary>
        /// Moves the letter from the old index to a new index.
        /// </summary>
        /// <param name="oldIndex">The old location of the letter to move</param>
        /// <param name="newIndex">The new location of the letter to move</param>
        public void ReorderLetter(int oldIndex, int newIndex)
        {
            if (newIndex == oldIndex)
            {
                return;
            }

            MoveLetter(currentWordState, oldIndex, newIndex);
            wordReordered?.Invoke(oldIndex, newIndex);

            if (!AssistiveSupport.isScreenReaderEnabled)
            {
                CheckWordComplete();
            }

            return;

            void MoveLetter(char[] word, int oldIndex, int newIndex)
            {
                if (oldIndex == newIndex)
                {
                    return;
                }

                var tmp = word[oldIndex];
                if (newIndex < oldIndex)
                {
                    Array.Copy(word, newIndex, word, newIndex + 1, oldIndex - newIndex);
                }
                else
                {
                    Array.Copy(word, oldIndex + 1, word, oldIndex, newIndex - oldIndex);
                }

                word[newIndex] = tmp;

                AudioManager.PlayMoveTile();
            }
        }

        public void CheckWordComplete()
        {
            if (IsWordComplete())
            {
                reorderedWordCount++;
                wordCompleted?.Invoke();
            }
        }

        /// <summary>
        /// Indicates whether the reordering of the current word is completed.
        /// </summary>
        public bool IsWordComplete()
        {
            var word = currentWord.word;

            if (word.Length != currentWordState.Length)
            {
                return false;
            }

            for (var i = 0; i < word.Length; i++)
            {
                if (word[i] != currentWordState[i])
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Indicates whether the game is showing the last word.
        /// </summary>
        public bool IsShowingLastWord()
        {
            return m_CurrentWordIndex == m_Words.Count - 1;
        }
    }
}
