using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Samples.LetterSpell
{
    [UxmlElement]
    public partial class StackView : VisualElement
    {
        VisualElement m_ContentContainer;
        VisualElement m_ActiveView;
        bool m_FirstGeometryChange = true;
        int m_Index = -1;

        public override VisualElement contentContainer => m_ContentContainer;


        [UxmlAttribute]
        public int index
        {
            get => m_Index;
            set
            {
                if (m_Index == value)
                {
                    return;
                }

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
                var needToEnsureValid = m_ActiveView == null && m_Index != -1 && childCount > 0 ||
                    m_ActiveView != null && (!Contains(m_ActiveView) || IndexOf(m_ActiveView) != m_Index);

                if (needToEnsureValid)
                {
                    UpdateActiveViewFromIndex();
                }

                return m_ActiveView;
            }
            set
            {
                var oldView = m_ActiveView;

                if (m_ActiveView == value)
                {
                    return;
                }

                m_ActiveView = value;
                index = IndexOf(m_ActiveView);
                if (panel != null)
                {
                    StartTransition(oldView, m_ActiveView);
                }
                else
                {
                    UpdateViews();
                    activeViewChanged?.Invoke();
                }
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
            if (from != null)
            {
                var fadeIn = from.experimental.animation.Start(
                    element => element.style.opacity.value,
                    0, 400, (element, value) =>
                    {
                        element.style.opacity = value;
                    });

                fadeIn.onAnimationCompleted += () =>
                {
                    // Hide the old view
                    from.style.display = DisplayStyle.None;

                    // Show the new view
                    if (to != null)
                    {
                        to.style.display = DisplayStyle.Flex;
                        to.style.opacity = 0.0f;

                        var fadeOut = to.experimental.animation.Start(
                            element => element.style.opacity.value,
                            1, 400, (element, value) => element.style.opacity = value);

                        fadeOut.onAnimationCompleted += () =>
                        {
                            activeViewChanged?.Invoke();
                        };
                    }
                    else
                    {
                        activeViewChanged?.Invoke();
                    }
                };
            }
            else if (to != null)
            {
                // If there is no from, just show the
                to.style.display = DisplayStyle.Flex;
                to.style.opacity = 1;
                activeViewChanged?.Invoke();
            }
        }

        void OnGeometryChanged(GeometryChangedEvent evt)
        {
            if (m_FirstGeometryChange)
            {
                m_FirstGeometryChange = false;
                UpdateViews();
                UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            }
        }

        public void UpdateViews()
        {
            UpdateActiveViewFromIndex();

            foreach (var view in Children())
            {
                view.style.display = m_ActiveView != view ? DisplayStyle.None : DisplayStyle.Flex;
            }
        }
    }
}
