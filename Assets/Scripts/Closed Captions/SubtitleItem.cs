using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Unity.Samples.ClosedCaptions
{
    /// <summary>
    /// This class is the model for a single subtitle item.
    /// </summary>
    [Serializable]
    public class SubtitleItem
    {
        public UnityTimeSpan startTime;
        public UnityTimeSpan endTime;
        [Multiline]
        public string text;
        public List<string> lines => string.IsNullOrEmpty(text) ? null : text.Split(Environment.NewLine).ToList();

        public override string ToString()
        {
            return $"{startTime.ToString()} --> {endTime.ToString()}: {text}";
        }
    }
}
