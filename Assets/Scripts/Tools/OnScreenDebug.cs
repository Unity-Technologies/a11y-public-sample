using System;
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Samples.LetterSpell
{
    class OnScreenDebugBehavior : MonoBehaviour
    {
        static OnScreenDebugBehavior s_Instance;
        private ScrollView m_ScrollView;
        private TextElement m_LogText;
        private int m_LastVersion = -1;
        
        void Awake()
        {
            if (s_Instance != null && s_Instance != this)
            {
                Debug.LogWarning($"There should only be one {nameof(OnScreenDebugBehavior)} instance per scene. Destroying the new one.");
                Destroy(this);
                return;
            }
            s_Instance = this;
        }

        void OnEnable()
        {
            var uiDocument = GetComponent<UIDocument>();
            var root = uiDocument.rootVisualElement;
            m_LogText = root.Q<TextElement>("LogText");
            m_ScrollView = root.Q<ScrollView>("LogScrollView");
            m_ScrollView.Q(classes: ScrollView.contentAndVerticalScrollUssClassName).pickingMode = PickingMode.Ignore;
            m_ScrollView.Q(classes: ScrollView.contentUssClassName).pickingMode = PickingMode.Ignore;
        }

        void Update()
        {
            if (m_LogText != null && m_LastVersion != OnScreenDebug.version)
            {
                m_LogText.text = OnScreenDebug.GetLogMessages();
                // scroll to the bottom
                m_ScrollView.ScrollTo(m_LogText);
                m_LastVersion = OnScreenDebug.version;
                Debug.Log(m_LogText);
            }
        }
    }
    
    public static class OnScreenDebug
    {
        [RuntimeInitializeOnLoadMethod]
        static void Initialize()
        {
            var gameObject = new GameObject("On Screen Debug");
            var onScreenDebugUI = Resources.Load<VisualTreeAsset>("UITk/OnScreenDebug/OnScreenDebugUI");
            var uiDocument = gameObject.AddComponent<UIDocument>();
            var panelSettings = Resources.Load<PanelSettings>("UITk/OnScreenDebug/OnScreenDebugPanelSettings");
            uiDocument.panelSettings = panelSettings;
            uiDocument.visualTreeAsset = onScreenDebugUI;
            gameObject.AddComponent<OnScreenDebugBehavior>();
        }
        
        private static int s_Version = -1;
        private static StringBuilder s_LogMessageBuilder = new StringBuilder();
        
        public static int version => s_Version;
        
        public static string GetLogMessages()
        {
            return s_LogMessageBuilder.ToString();
        }
        
        public static void Log(string message)
        {
            s_LogMessageBuilder.AppendLine($"{DateTime.Now}: " + message);
            s_Version++;
        }
    }
}
