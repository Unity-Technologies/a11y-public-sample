using System.Collections.Generic;
using Unity.Properties;
using Unity.Samples.ScreenReader;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Samples.LetterSpell
{
    [UxmlElement]
    public partial class ButtonStripField : BaseField<int>
    {
        const string k_USSClassName = "lsp-button-strip-field";
        const string k_ButtonContainerUssClassName = k_USSClassName + "__button-container";
        const string k_ButtonUssClassName = k_USSClassName + "__button";
        const string k_ButtonLeftUssClassName = k_ButtonUssClassName + "--left";
        const string k_ButtonMiddleUssClassName = k_ButtonUssClassName + "--middle";
        const string k_ButtonRightUssClassName = k_ButtonUssClassName + "--right";

        class ButtonBar : VisualElement
        {
            public ButtonBar()
            {
                AddToClassList(k_ButtonContainerUssClassName);
            }
        }

        List<string> m_Choices = new();
        ButtonBar m_ButtonBar;
        IVisualElementScheduledItem m_ScheduledRecreateButtonsItem;

        [UxmlAttribute, CreateProperty]
        public List<string> choices
        {
            get => m_Choices;
            set
            {
                m_Choices = value ?? throw new System.ArgumentNullException(nameof(value));
                DelayRecreateButtons();
            }
        }

        public ButtonStripField() : base(null, new ButtonBar())
        {
            m_ButtonBar = this.Q<ButtonBar>();
            choices = new List<string>();
            AddToClassList(k_USSClassName);
            value = -1;
        }

        Button CreateButton(string text)
        {
            var button = new Button
            {
                name = "buttonStripButton__" + text,
                text = text
            };

            button.AddToClassList(k_ButtonUssClassName);
            button.RegisterCallback<DetachFromPanelEvent>(OnButtonDetachFromPanel);
            button.clicked += () =>
            {
                value = m_ButtonBar.IndexOf(button);
            };

            return button;
        }

        void OnButtonDetachFromPanel(DetachFromPanelEvent evt)
        {
            var button = evt.target as Button;
            button?.UnregisterCallback<DetachFromPanelEvent>(OnButtonDetachFromPanel);
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
            for (var i = 0; i < m_ButtonBar.childCount; ++i)
            {
                m_ButtonBar[i].SetCheckedPseudoState(i == value);

                var updater = UITkAccessibilityManager.instance?.accessiblityUpdater;
                updater?.OnVersionChanged(m_ButtonBar[i], VisualElementAccessibilityHandler.k_AccessibilityChange);
            }
        }

        void UpdateButtonsStyling()
        {
            for (var i = 0; i < m_ButtonBar.childCount; i++)
            {
                var button = m_ButtonBar[i] as Button;
                button?.RemoveFromClassList(k_ButtonLeftUssClassName);
                button?.RemoveFromClassList(k_ButtonMiddleUssClassName);
                button?.RemoveFromClassList(k_ButtonRightUssClassName);

                if (i == 0)
                {
                    button?.AddToClassList(k_ButtonLeftUssClassName);
                }
                else if (i == m_ButtonBar.childCount - 1)
                {
                    button?.AddToClassList(k_ButtonRightUssClassName);
                }
                else
                {
                    button?.AddToClassList(k_ButtonMiddleUssClassName);
                }
            }
        }
    }
}
