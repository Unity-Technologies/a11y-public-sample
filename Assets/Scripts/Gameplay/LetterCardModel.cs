using System;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.Samples.LetterSpell
{
    /// <summary>
    /// A model representation of a letter card.
    /// </summary>
    class LetterCardModel
    {
        /// <summary>
        /// The letter associated with the card.
        /// </summary>
        public char letter { get; }

        /// <summary>
        /// Constructs a card.
        /// </summary>
        /// <param name="letter">The letter associated with the card</param>
        public LetterCardModel(char letter)
        {
            this.letter = letter;
        }
    }
    
    /// <summary>
    /// A model representation of a collection of cards.
    /// </summary>
    class LetterCardListModel
    {
        /// <summary>
        /// The letter cards.
        /// </summary>
        public IEnumerable<LetterCardModel> letterCards => m_LetterCards;
        List<LetterCardModel> m_LetterCards = new();

        public Gameplay gameplay
        {
            get => m_Gameplay;
            set
            {
                if (m_Gameplay == value)
                {
                    return;
                }

                if (m_Gameplay != null)
                {
                    m_Gameplay.currentWordIndexChanged.RemoveListener(OnCurrentWordIndexChanged);
                    m_Gameplay.wordReordered.RemoveListener(OnWordReordered);
                }

                m_Gameplay = value;

                if (m_Gameplay != null)
                {
                    m_Gameplay.currentWordIndexChanged.AddListener(OnCurrentWordIndexChanged);
                    m_Gameplay.wordReordered.AddListener(OnWordReordered);
                }
            }
        }

        Gameplay m_Gameplay;

        void OnCurrentWordIndexChanged(int wordIndex)
        {
            SetCurrentWord(m_Gameplay.currentWordState);
        }
        
        /// <summary>
        /// Called when the letter cards have been recreated.
        /// </summary>
        public event Action letterCardsChanged;
        
        /// <summary>
        /// Called when the letter cards have been reordered.
        /// </summary>
        public event Action letterCardsReordered;
        
        public void SetCurrentWord(char[] wordState)
        {
            m_LetterCards.Clear();

            if (wordState != null)
            {
                foreach (var letter in wordState)
                {
                    m_LetterCards.Add(new LetterCardModel(letter));
                }
            }

            letterCardsChanged?.Invoke();
        }
        
        void OnWordReordered(int oldIndex, int newIndex)
        {
            var item = m_LetterCards[oldIndex];

            m_LetterCards.Remove(item);
            m_LetterCards.Insert(newIndex, item);

            letterCardsReordered?.Invoke();
        }
    }
}
