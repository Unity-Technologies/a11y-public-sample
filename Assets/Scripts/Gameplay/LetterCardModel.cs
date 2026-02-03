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

        /// <summary>
        /// Called when the letter cards have been recreated.
        /// </summary>
        public event Action letterCardsChanged;

        /// <summary>
        /// Called when the letter cards have been reordered.
        /// </summary>
        public event Action letterCardsReordered;

        public void Setup()
        {
            Gameplay.instance?.currentWordIndexChanged.AddListener(OnWordIndexChanged);
            Gameplay.instance?.wordReordered.AddListener(OnWordReordered);
        }

        public void Cleanup()
        {
            Gameplay.instance?.currentWordIndexChanged.RemoveListener(OnWordIndexChanged);
            Gameplay.instance?.wordReordered.RemoveListener(OnWordReordered);
        }

        void OnWordIndexChanged(int wordIndex)
        {
            m_LetterCards.Clear();

            var wordState = Gameplay.instance.currentWordState;

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
