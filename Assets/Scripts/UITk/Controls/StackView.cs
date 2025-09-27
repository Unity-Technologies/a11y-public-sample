using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Samples.LetterSpell
{
    [UxmlElement]
    public partial class StackView : VisualElement
    {
        private VisualElement m_ContentContainer;
        private VisualElement m_ActiveView;
        private VisualElement m_TransitionElement;

        public override VisualElement contentContainer => m_ContentContainer;
        
        int m_Index = -1;
        
        [UxmlAttribute]
        public int index
        {
            get => m_Index;
            set
            {
                if (m_Index == value)
                    return;
                
                m_Index = value;
                UpdateActiveViewFromIndex();
                indexChanged?.Invoke(m_Index);
            }
        }
        
        void UpdateActiveViewFromIndex()
        {
            if (m_Index < 0 || m_Index >= childCount)
            {
                activeView = null;
                return;
            }
            activeView = this[m_Index];
        }

        public VisualElement activeView
        {
            get
            {
                // Ensure the active view is valid. This is because children can be added/removed without being notified.
                bool needToEnsureValid = false;
                
                if (m_ActiveView == null && m_Index != -1 && childCount > 0)
                    needToEnsureValid = true;
                else if (m_ActiveView != null && (!Contains(m_ActiveView) || IndexOf(m_ActiveView) != m_Index))
                    needToEnsureValid = true;
                
                if (needToEnsureValid)
                    UpdateActiveViewFromIndex();
                return m_ActiveView;
            }
            set
            {
                var oldView = m_ActiveView;
                
                if (m_ActiveView == value)
                    return;
                m_ActiveView = value;
                
                if (panel != null)
                    StartTransition(oldView, m_ActiveView);
                else
                    UpdateViews();

                index = IndexOf(m_ActiveView);
                activeViewChanged?.Invoke();
            }
        }

        public event Action<int> indexChanged;
        public event Action activeViewChanged;

        public StackView()
        {
            AddToClassList("lsp-stack-view");
            
            m_ContentContainer = new VisualElement();
            m_ContentContainer.AddToClassList("lsp-stack-view__content-container");
            m_ContentContainer.style.flexGrow = 1;
            hierarchy.Add(m_ContentContainer);
            RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }
        
        void StartTransition(VisualElement from, VisualElement to)
        {
            if (m_TransitionElement == null)
            {
                m_TransitionElement ??= new VisualElement();
                m_TransitionElement.AddToClassList("lsp-stack-view-Transition"); 
            }
            m_TransitionElement.style.opacity = 0;
            hierarchy.Add(m_TransitionElement);

            var fadeIn = m_TransitionElement.experimental.animation.Start(
                (element) => element.style.opacity.value,
                1f, 400, (element, value) =>
                {
                    element.style.opacity = value;
                    Debug.Log("op = " + value);
                });
            fadeIn.onAnimationCompleted += () =>
            {
                // Hide the old view
                if (from != null)
                    from.style.display = DisplayStyle.None;

                // Show the new view
                if (to != null)
                {
                    to.style.display = DisplayStyle.Flex;
                }

                var fadeOut = m_TransitionElement.experimental.animation.Start(
                    (element) => element.style.opacity.value,
                    0, 400, (element, value) => element.style.opacity = value);
                fadeOut.onAnimationCompleted += () =>
                {
                   m_TransitionElement.RemoveFromHierarchy();
                };
            };
        }
        
        bool firstGeometryChange = true;
        
        void OnGeometryChanged(GeometryChangedEvent evt)
        {
            if (firstGeometryChange )
            {
                firstGeometryChange = false;
                UpdateViews();
                UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            }
        }
        
        public void UpdateViews()
        {
            UpdateActiveViewFromIndex();
            foreach (var view in Children())
            {
                if (m_ActiveView != view)
                    view.style.display = DisplayStyle.None;
                else
                    view.style.display = DisplayStyle.Flex;
            }
        }
    }
}
