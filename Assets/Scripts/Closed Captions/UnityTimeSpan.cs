using System;
using UnityEngine;

namespace Unity.Samples.Accessibility
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
        private int m_Milliseconds;
        
        public int Milliseconds
        {
            get => m_Milliseconds;
        }

        public static UnityTimeSpan FromMilliseconds(int ms)
        {
            return new UnityTimeSpan() {m_Milliseconds = ms};
        }

        public override string ToString()
        {
            return TimeSpan.FromMilliseconds(m_Milliseconds).ToString("c");
        }

        public bool Equals(UnityTimeSpan other)
        {
            return m_Milliseconds == other.m_Milliseconds;
        }

        public override int GetHashCode()
        {
            return m_Milliseconds;
        }
    }
}
