using UnityEngine;
using UnityEngine.TextCore.Text;

namespace Unity.Samples.Accessibility
{
    /// <summary>
    /// This class is used to configure the appearance of subtitles.
    /// </summary>
    [CreateAssetMenu(menuName = "Accessibility/Subtitle Display Settings")]
    class SubtitleDisplaySettings : ScriptableObject
    {
        public Color color = Color.white;
        public Color backgroundColor = Color.black;
        [Range(0, 1)]
        public float opactity = 0.8f;
        public FontAsset font;
        public int fontSize = 12;
        public FontStyle fontStyle = FontStyle.Normal;
        public bool useDropShadow;
        public Color dropShadowColor = Color.black;

        static SubtitleDisplaySettings m_Default;

        public static SubtitleDisplaySettings GetDefault()
        {
            if (m_Default == null)
            {
                m_Default = CreateInstance<SubtitleDisplaySettings>();
            }

            return m_Default;
        }
    }
}
