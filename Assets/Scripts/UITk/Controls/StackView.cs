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
        private VisualElement m_ActiveView;

        [UxmlAttribute]
        public int index
        {
            get => m_ActiveView != null ? IndexOf(m_ActiveView) : -1;
            set
            {
                if (value > this.childCount - 1)
                    return;
                activeView = value >= 0 ? this[value] : null;
            }
        }

        public VisualElement activeView
        {
            get => m_ActiveView;
            set
            {
                if (m_ActiveView == value)
                    return;
                m_ActiveView = value;
                UpdateViews();
                activeViewChanged?.Invoke();
            }
        }

        public event Action activeViewChanged;

        public StackView()
        {
            RegisterCallback<GeometryChangedEvent>((e)=>UpdateViews());
        }

        public void UpdateViews()
        {
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
