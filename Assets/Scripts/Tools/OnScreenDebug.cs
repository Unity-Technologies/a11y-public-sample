using System;
using System.Collections.Generic;
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
        private ShapeView m_ShapeView;
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
            m_ShapeView = new ShapeView() { pickingMode = PickingMode.Ignore, style = {position = Position.Absolute}};
            m_ShapeView.StretchToParentSize();
            root.Add(m_ShapeView);
        }

        void Update()
        {
            if (m_LastVersion != OnScreenDebug.version)
            {
                if (m_LogText != null)
                {
                    m_LogText.text = OnScreenDebug.GetLogMessages();
                    // scroll to the bottom
                   // m_ScrollView.ScrollTo(m_LogText);
                    m_ScrollView.scrollOffset = new Vector2(0, m_ScrollView.verticalScroller.highValue);
                    //Debug.Log(m_LogText);
                }
                m_ShapeView.rects = OnScreenDebug.GetRects();
                m_ShapeView.screenRects = OnScreenDebug.GetScreenRects();
                m_ShapeView.MarkDirtyRepaint();
                m_LastVersion = OnScreenDebug.version;
            }
        }
    }
    
    public class ShapeView : VisualElement
    {
        public List<Rect> rects { get; set; }
        public List<Rect> screenRects { get; set; }
        
        public ShapeView()
        {
            generateVisualContent += OnGenerateVisualContent;
        }
        
        private void OnGenerateVisualContent(MeshGenerationContext mgc)
        {
            var painter = mgc.painter2D;
            painter.strokeColor = Color.white;
            painter.lineWidth = 1;

            if (rects != null && rects.Count > 0)
            {
                painter.strokeColor = Color.red;
                painter.BeginPath();
                foreach (var rect in rects)
                {
                    DrawRect(painter, rect);
                }
                foreach (var rect in screenRects)
                {
                    var scale = panel.scaledPixelsPerPoint;
                    DrawRect(painter, new Rect(rect.position / scale, rect.size / scale));
                }
                painter.ClosePath();
                painter.Stroke();
            }
        }

        void DrawRect(Painter2D painter, Rect r)
        {
            // Draw rectangle
            painter.MoveTo(new Vector2(r.xMin, r.yMin));
            painter.LineTo(new Vector2(r.xMax, r.yMin));
            painter.LineTo(new Vector2(r.xMax, r.yMax));
            painter.LineTo(new Vector2(r.xMin, r.yMax));
            painter.LineTo(new Vector2(r.xMin, r.yMin));
        }
    }
    
    public static class OnScreenDebug
    {
        [RuntimeInitializeOnLoadMethod]
        static void Initialize()
        {
            // Uncomment if you want to enable the on-screen debug by default
            /*
            var gameObject = new GameObject("On Screen Debug");
            var onScreenDebugUI = Resources.Load<VisualTreeAsset>("UITk/OnScreenDebug/OnScreenDebugUI");
            var uiDocument = gameObject.AddComponent<UIDocument>();
            var panelSettings = Resources.Load<PanelSettings>("UITk/OnScreenDebug/OnScreenDebugPanelSettings");
            uiDocument.panelSettings = panelSettings;
            uiDocument.visualTreeAsset = onScreenDebugUI;
            gameObject.AddComponent<OnScreenDebugBehavior>();*/
        }
        
        private static int s_Version = -1;
        private static StringBuilder s_LogMessageBuilder = new StringBuilder();
        private static List<Rect> s_Rects = new List<Rect>();
        private static List<Rect> s_ScreenRects = new List<Rect>();
        
        public static int version => s_Version;
        
        public static string GetLogMessages()
        {
            return s_LogMessageBuilder.ToString();
        }

        public static List<Rect> GetRects()
        {
            return s_Rects;
        }
        
        public static List<Rect> GetScreenRects()
        {
            return s_ScreenRects;
        }
        
        public static void Log(string message)
        {
            s_LogMessageBuilder.AppendLine($"{DateTime.Now}: " + message);
            s_Version++;
        }

        public static void Clear()
        {
            ClearShapes();
            s_LogMessageBuilder.Clear();
            s_Version++;
        }
        
        public static void DrawRect(float x, float y, float width, float height)
        {
            s_Rects.Add(new Rect(x, y, width, height));
            s_Version++;
        }
        
        public static void DrawScreenRect(float x, float y, float width, float height)
        {
            s_ScreenRects.Add(new Rect(x, y, width, height));
            s_Version++;
        }
        
        public static void DrawRect(Rect rect)
        {
            DrawRect(rect.x, rect.y, rect.width, rect.height);
        }
        
        public static void DrawScreenRect(Rect rect)
        {
            DrawScreenRect(rect.x, rect.y, rect.width, rect.height);
        }

        public static void ClearShapes()
        {
            s_Rects.Clear();
        }
    }
}
