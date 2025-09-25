using System.Collections.Generic;
using Unity.Properties;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Samples.LetterSpell
{
    [UxmlElement]
    public partial class ButtonStripField : BaseField<int>
    {
        public const string ussClassName = "lsp-button-strip-field";
        public const string buttonContainerUssClassName = ussClassName + "__button-container";
        public const string buttonUssClassName = ussClassName + "__button";
        public const string buttonLeftUssClassName = buttonUssClassName + "--left";
        public const string buttonMiddleUssClassName = buttonUssClassName + "--middle";
        public const string buttonRightUssClassName = buttonUssClassName + "--right";
        
        class ButtonBar : VisualElement
        {
            public ButtonBar()
            {
                AddToClassList(buttonContainerUssClassName);
            }
        }
        
        List<string> m_Choices = new List<string>();
        private ButtonBar m_ButtonBar;
        private IVisualElementScheduledItem m_ScheduledRecreateButtonsItem;
        
        [UxmlAttribute, CreateProperty]
        public List<string> choices
        {
            get { return m_Choices; }
            set
            {
                if (value == null)
                    throw new System.ArgumentNullException(nameof(value));
                m_Choices = value;
                DelayRecreateButtons();
            }
        }
        
        public ButtonStripField() : base(null, new ButtonBar())
        {
            m_ButtonBar = this.Q<ButtonBar>();
            choices = new List<string>();
            AddToClassList(ussClassName);
            value = -1;
        }
        
        Button CreateButton(string text)
        {
            var button = new Button { name = "buttonStripButton__" + text, text = text };
            button.AddToClassList(buttonUssClassName);
            button.RegisterCallback<DetachFromPanelEvent>(OnButtonDetachFromPanel);
            button.clicked += () => { value = m_ButtonBar.IndexOf(button); };
            return button;
        }
        
        void OnButtonDetachFromPanel(DetachFromPanelEvent evt)
        {
            var button = evt.target as Button;
            button.UnregisterCallback<DetachFromPanelEvent>(OnButtonDetachFromPanel);
            UpdateButtonsStyling();
        }
        
        public override void SetValueWithoutNotify(int newValue)
        {
            base.SetValueWithoutNotify(newValue);
            UpdateButtonsState();
        }

        void DelayRecreateButtons()
        {
            m_ScheduledRecreateButtonsItem ??= schedule.Execute(RecreateButtons);
            m_ScheduledRecreateButtonsItem.Resume();
        }

        void RecreateButtons()
        {
            m_ButtonBar.Clear();

            foreach (var choice in m_Choices)
            {
                var button = CreateButton(choice);
                m_ButtonBar.Add(button);
            }
            UpdateButtonsState();
            UpdateButtonsStyling();
        }
        
        void UpdateButtonsState()
        {
            for (int i = 0; i < m_ButtonBar.childCount; ++i)
            {
                m_ButtonBar[i].SetCheckedPseudoState(i == value);
            }
        }
        
        void UpdateButtonsStyling()
        {
            for (var i = 0; i < m_ButtonBar.childCount; i++)
            {
                var button = m_ButtonBar[i] as Button;
                button.RemoveFromClassList(buttonLeftUssClassName);
                button.RemoveFromClassList(buttonMiddleUssClassName);
                button.RemoveFromClassList(buttonRightUssClassName);

                if (i == 0)
                {
                    button.AddToClassList(buttonLeftUssClassName);
                }
                else if (i == m_ButtonBar.childCount - 1)
                {
                    button.AddToClassList(buttonRightUssClassName);
                }
                else
                {
                    button.AddToClassList(buttonMiddleUssClassName);
                }
            }
        }
    }
}