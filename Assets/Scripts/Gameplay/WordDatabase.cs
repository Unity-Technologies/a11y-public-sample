using System;
using UnityEngine;

namespace Unity.Samples.LetterSpell
{
    [Serializable]
    public struct WordData
    {
        [SerializeField] public string clue;
        [SerializeField] public string word;
    }

    [Serializable]
    public struct WordCollection
    {
        [SerializeField] public WordData[] easy;
        [SerializeField] public WordData[] hard;
    }
    
    [CreateAssetMenu(menuName = "LetterSpell/WordDatabase")]
    public class WordDatabase : ScriptableObject
    {
        public WordCollection words;
    }
}
