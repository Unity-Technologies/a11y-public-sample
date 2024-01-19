using System;
using UnityEngine;

namespace Unity.Samples.ClosedCaptions
{
    /// <summary>
    /// This class represents a time span in milliseconds.
    /// It is used to represent the start and end time of a subtitle item.
    /// This class is serializable so that it can be used in the inspector.
    /// </summary>
    [Serializable]
    public struct UnityTimeSpan : IEquatable<UnityTimeSpan>
    {
        [SerializeField]
        int milliseconds;
        
        public int Milliseconds
        {
            get => milliseconds;
        }

        public static UnityTimeSpan FromMilliseconds(int ms)
        {
            return new UnityTimeSpan { milliseconds = ms };
        }

        public override string ToString()
        {
            return TimeSpan.FromMilliseconds(milliseconds).ToString("c");
        }

        public bool Equals(UnityTimeSpan other)
        {
            return milliseconds == other.milliseconds;
        }

        public override int GetHashCode()
        {
            return milliseconds;
        }
    }
}
