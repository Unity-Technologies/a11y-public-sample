using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
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
        
        /// <summary>
        /// The database of words.
        /// </summary>
        public WordDatabase wordDatabase;

        /// <summary>
        /// The list of words to complete.
        /// </summary>
        public IReadOnlyList<WordData> words => m_Words.AsReadOnly();
        List<WordData> m_Words = new();

        /// <summary>
        /// The current word to reorder.
        /// </summary>
        public WordData currentWord => m_CurrentWordIndex != -1 ? m_Words[m_CurrentWordIndex] : default;
        int m_CurrentWordIndex = -1;

        /// <summary>
        /// The current state of the word being reordered.
        /// </summary>
        public char[] currentWordState => m_CurrentWordState;
        char[] m_CurrentWordState;
        
        /// <summary>
        /// The number of words that were successfully reordered.
        /// </summary>
        public int reorderedWordCount { get; private set; }
        
        /// <summary>
        /// Sent when the current word has been changed.
        /// </summary>
        public UnityEvent<int> currentWordIndexChanged = new();

        /// <summary>
        /// Sent when the current word has been reordered.
        /// </summary>
        public UnityEvent<int, int> wordReordered = new();
        
        /// <summary>
        /// Sent when the current word has been completed.
        /// </summary>
        public UnityEvent wordReorderingCompleted = new();
        
        /// <summary>
        /// Sent when the game has started.
        /// </summary>
        public UnityEvent gameStarted = new();
        
        /// <summary>
        /// Sent when the game has finished.
        /// </summary>
        public UnityEvent gameEnded = new();

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
        State m_State;

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
            SetCurrentWordIndex(-1);

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
                ? wordDatabase.words.easy
                : wordDatabase.words.hard;

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
                SetCurrentWordIndex(m_CurrentWordIndex + 1);
            }
        }

        void SetCurrentWordIndex(int index)
        {
            if (m_CurrentWordIndex == index)
            {
                return;
            }

            m_CurrentWordIndex = index;
            InitializeCurrentWordState();

            currentWordIndexChanged?.Invoke(index);
        }

        void InitializeCurrentWordState()
        {
            if (!string.IsNullOrEmpty(currentWord.word))
            {
                do
                {
                    m_CurrentWordState = new char[currentWord.word.Length];

                    // Shuffle the letters.
                    currentWord.word.CopyTo(0, m_CurrentWordState, 0, currentWord.word.Length);

                    for (var n = m_CurrentWordState.Length; n > 1;)
                    {
                        var k = m_Randomizer.Next(n);
                        --n;
                        (m_CurrentWordState[n], m_CurrentWordState[k]) = (m_CurrentWordState[k], m_CurrentWordState[n]);
                    }
                }

                // Make sure it is not the original word.
                while (IsWordComplete());
            }
            else
            {
                m_CurrentWordState = null;
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

            MoveLetter(m_CurrentWordState, oldIndex, newIndex);
            wordReordered?.Invoke(oldIndex, newIndex);

            if (IsWordComplete())
            {
                reorderedWordCount++;
                wordReorderingCompleted?.Invoke();
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

                if (AudioManager.instance != null)
                {
                    AudioManager.instance.PlayMoveTile();
                }
            }
        }

        void Awake()
        {
            if (instance == null)
            {
                instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        void Start()
        {
            StartCoroutine(DelayStartGame());
        }

        IEnumerator DelayStartGame()
        {
            yield return new WaitForEndOfFrame();

            StartGame();
        }

        /// <summary>
        /// Indicates whether the reordering of the current word is completed.
        /// </summary>
        public bool IsWordComplete()
        {
            return m_Words[m_CurrentWordIndex].word.SequenceEqual(m_CurrentWordState);
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
