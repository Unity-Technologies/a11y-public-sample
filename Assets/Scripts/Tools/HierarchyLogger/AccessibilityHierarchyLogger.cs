using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Accessibility;

namespace Unity.Samples.LetterSpell
{
    public abstract class AccessibilityHierarchyLogger : MonoBehaviour
    {
        static AccessibilityHierarchyLogger s_Instance;
        
        protected virtual void Awake()
        {
            if (s_Instance != null && s_Instance != this)
            {
                Debug.LogWarning($"There should only be one {nameof(AccessibilityHierarchyLogger)} instance per scene. Destroying the new one.");
                Destroy(this);
                return;
            }
            s_Instance = this;
        }
        
        class Visitor : AccessibilityHierarchyVisitor
        {
            private AccessibilityHierarchyLogger m_Logger;
            
            public Visitor(AccessibilityHierarchyLogger logger)
            {
                m_Logger = logger;
            }
            protected override void BeginVisit(AccessibilityNode node) => m_Logger.BeginLogging(node);
            protected override void VisitNode(AccessibilityNode node) => m_Logger.Log(node);
            protected override void EndVisit(AccessibilityNode node) => m_Logger.EndLogging(node);
        }

        private Visitor m_Visitor;
        private StringBuilder m_StringBuilder;
        private List<Rect> m_NodeFrames = new List<Rect>();
        private int m_Level;
        private const int k_IndentWidth = 4;

        /// <summary>
        /// Logs the nodes of the specified hierarchy
        /// </summary>
        public static void Log(AccessibilityHierarchy hierarchy)
        {
            s_Instance?.LogHierarchy(hierarchy);
        }
        
        void LogHierarchy(AccessibilityHierarchy hierarchy)
        {
            m_Visitor ??= new Visitor(this);
            m_StringBuilder ??= new StringBuilder();
            m_Level = -1;
            m_StringBuilder.AppendLine("Accessibility Hierarchy:");
            try
            {
                m_NodeFrames.Clear();
                OnScreenDebug.ClearShapes();
                m_Visitor.Visit(hierarchy);
                foreach (var frame in m_NodeFrames)
                    OnScreenDebug.DrawScreenRect(frame);
                OnScreenDebug.Log(m_StringBuilder.ToString());
               // WriteLog(m_StringBuilder.ToString());
            }
            finally
            {
                m_StringBuilder.Clear();
            }
        }

        void BeginLogging(AccessibilityNode node)
        {
            m_Level++;
        }
        
        void Log(AccessibilityNode node)
        {
            m_StringBuilder.Append(' ', m_Level * k_IndentWidth);
            m_StringBuilder.AppendLine($"{node.id} \"{node.label}\" Role:{node.role} State:{node.state} Frame:{node.frame}");
            m_NodeFrames.Add(node.frame);
        }
        
        void EndLogging(AccessibilityNode node)
        {
            m_Level--;
        }

        protected abstract void WriteLog(string log);
    }

    public static class AccessibilityHierarchyLogExtensions
    {
        public static void Log(this AccessibilityHierarchy hierarchy)
        {
            AccessibilityHierarchyLogger.Log(hierarchy);
        }
    }
}