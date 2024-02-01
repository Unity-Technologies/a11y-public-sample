using System;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.Samples.ClosedCaptions
{
    /// <summary>
    /// This class holds the list of subtitle items.
    /// </summary>
    public class Subtitle : ScriptableObject
    {
        public List<SubtitleItem> items;
    }
}
