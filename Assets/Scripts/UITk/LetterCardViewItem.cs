using System;
using System.Linq;
using Unity.Properties;
using UnityEngine;
using UnityEngine.Accessibility;
using UnityEngine.Scripting;
using UnityEngine.UIElements;
using Unity.Samples.ScreenReader;
using UnityEditor;
using Button = UnityEngine.UIElements.Button;

namespace Unity.Samples.LetterSpell
{
    /// <summary>
    /// This class represents a card in LetterSpell
    /// </summary>
    [UxmlElement]
    public partial class LetterCardViewItem : AccessibleVisualElement
    {
        internal static readonly BindingId selectedProperty = nameof(selected);
        internal static readonly BindingId focusedProperty = nameof(focused);
        internal static readonly BindingId draggingProperty = nameof(dragged);

        Label m_LetterLabel;
        Vector2 m_StartMousePos;
        bool m_Active;
        bool m_Selected;
        bool m_Dragged;
        int m_DraggingDirection;
        char m_Letter;

        /// <summary>
        /// The Card view that contains this card.
        /// </summary>
        LetterCardView cardView => parent as LetterCardView;

        /// <summary>
        /// Indicates whether this card is selected.
        /// </summary>
        [CreateProperty(ReadOnly = true)]
        public bool selected
        {
            get => m_Selected;
            internal set
            {
                if (m_Selected == value)
                    return;
                m_Selected = value;
                if (value)
                    Focus();
                UpdateStyleClasses();
                NotifyPropertyChanged(selectedProperty);
            }
        }

        /// <summary>
        /// Indicates whether this card has focus
        /// </summary>
        [CreateProperty]
        bool focused => focusController?.focusedElement == this;

        /// <summary>
        /// Idicates whether this card is being dragged.
        /// </summary>
        [CreateProperty, UxmlAttribute]
        public bool dragged
        {
            get => m_Dragged;
            private set
            {
                if (m_Dragged == value)
                {
                    return;
                }

                m_Dragged = value;
               
                if (cardView != null)
                {
                    if (value)
                    {
                        cardView.StartDragging(this);
                    }
                    else
                    {
                        cardView.FinishDragging(this);
                        Deselect();
                    }

                    cardView.DoLayout();
                }

                UpdateStyleClasses();
                NotifyPropertyChanged(draggingProperty);
            }
        }

        /// <summary>
        /// The direction the card is being dragged. Negative values indicate left, positive values indicate right.
        /// </summary>
        [CreateProperty, UxmlAttribute]
        public int draggingDirection
        {
            get => m_DraggingDirection;
            set
            {
                if (m_DraggingDirection == value)
                {
                    return;
                }

                m_DraggingDirection = value;
                UpdateStyleClasses();
            }
        }

        /// <summary>
        /// The letter displayed by the card
        /// </summary>
        [CreateProperty, UxmlAttribute]
        public char letter
        {
            get => m_Letter;
            set
            {
                if (m_Letter == value)
                    return;
                m_Letter = value;
                UpdateLabel();
            }
        }

        /// <summary>
        /// Event sent when the card is droppped.
        /// </summary>
        public event Action<int, int> dropped;

        /// <summary>
        /// Default constructor.
        /// </summary>
        public LetterCardViewItem() : this('\0')
        {
        }
        
        /// <summary>
        /// Constructs a visual card with a letter.
        /// </summary>
        public LetterCardViewItem(char letter)
        {
            AddToClassList(LetterCardView.itemUssClassName);
            
            // Make it focusable.
            focusable = true;

            m_LetterLabel = new Label();
            m_LetterLabel.GetOrCreateAccessibleProperties().ignored = true;
            Add(m_LetterLabel);

            style.position = Position.Absolute;

            RegisterCallback<MouseDownEvent>(OnMouseDown);
            RegisterCallback<MouseMoveEvent>(OnMouseMove);
            RegisterCallback<MouseUpEvent>(OnMouseUp);
            RegisterCallback<FocusInEvent>(OnFocusIn);
            RegisterCallback<BlurEvent>(OnBlur);
            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            
            this.letter = letter;
        }
        
        void UpdateLabel()
        {
            if (m_LetterLabel == null)
                return;
            
            m_LetterLabel.text = letter.ToString();
            accessible.label = letter.ToString();
        }

        /// <summary>
        /// Selects this card.
        /// </summary>
        public void Select()
        {
            if (cardView == null || selected)
                return;

            if (cardView.selectedCard != this)
            {
                cardView.selectedCard = this;

                // Announce that the card has been selected.
                // The announcement should only be done when the card is interactively selected via this method.
                AssistiveSupport.notificationDispatcher.SendAnnouncement(
                    $"Card {letter} selected. Swipe Left or Right to move the card." +
                    (focused ? "Or Double tap to unselect it." : ""));
            }
        }

        /// <summary>
        /// Unseletec this card if it is selected.
        /// </summary>
        public void Deselect()
        {
            if (cardView == null || !selected)
                return;

            // check whether we are focused or not
            cardView.selectedCard = null;

            // Announce that the card has been deselected.
            // The announcement should only be done when the card is interactively deselected via this method.
            AssistiveSupport.notificationDispatcher.SendAnnouncement($"Card {letter} deselected." +
                                                                     (focused ? "Double tap to select it." : ""));
        }

        void OnAttachToPanel(AttachToPanelEvent e)
        {
            cardView.DoLayout();
        }

        void OnFocusIn(FocusInEvent e)
        {
            AssistiveSupport.notificationDispatcher.SendAnnouncement(
                $"Double tap to select Card {letter} and start moving.");
            NotifyPropertyChanged(focusedProperty);
            e.StopPropagation();
        }

        void OnBlur(BlurEvent e)
        {
            NotifyPropertyChanged(focusedProperty);
            e.StopPropagation();
        }

        void UpdateStyleClasses()
        {
            EnableInClassList(LetterCardView.selectedItemUssClassName, selected);
            EnableInClassList(LetterCardView.draggedItemUssClassName, m_Dragged);
            EnableInClassList(LetterCardView.draggedLeftItemUssClassName, m_Dragged && m_DraggingDirection < 0);
            EnableInClassList(LetterCardView.draggedRightItemUssClassName, m_Dragged && m_DraggingDirection > 0);
        }

        Rect CalculatePosition(float x, float y, float width, float height)
        {
            var rect = new Rect(x, y, width, height);
            // TODO - Clamp to parent rect.
            return rect;
        }

        /// <summary>
        /// Called when the card is dropped.
        /// </summary>
        /// <param name="oldIndex">The old index</param>
        /// <param name="newIndex">The new index</param>
        internal void OnDrop(int oldIndex, int newIndex)
        {
            dropped?.Invoke(oldIndex, newIndex);
        }

        void OnMouseDown(MouseDownEvent e)
        {
            if (cardView == null || panel?.GetCapturingElement(PointerId.mousePointerId) != null)
                return;

            if (!cardView.interactable || m_Active)
            {
                e.StopImmediatePropagation();
                return;
            }

            if (e.button == (int)MouseButton.LeftMouse)
            {
                m_StartMousePos = e.localMousePosition;
                m_Active = true;
                this.CaptureMouse();
                e.StopPropagation();
            }
        }

        void OnMouseMove(MouseMoveEvent e)
        {
            if (m_Active)
            {
                // Ensure the card is selected when we start dragging it.
                Select();

                var diff = e.localMousePosition - m_StartMousePos;

                if (!dragged && Math.Abs(diff.x) > 5)
                {
                    dragged = true;
                    return;
                }

                if (!dragged)
                {
                    return;
                }

                var targetScale = transform.scale;
                diff.x *= targetScale.x;
                diff.y *= targetScale.y;

                var rect = CalculatePosition(layout.x + diff.x, layout.y + diff.y, layout.width, layout.height);
                var oldLeft = style.left.value.value;

                style.left = rect.x;
                draggingDirection = oldLeft < style.left.value.value ? -1 : 1;
                cardView.Drag(this);
                e.StopPropagation();
            }
        }

        void OnMouseUp(MouseUpEvent e)
        {
            if (m_Active)
            {
                if (e.button == (int)MouseButton.LeftMouse)
                {
                    // Select or unselect the card if we didn't drag it.
                    if (!m_Dragged)
                    {
                        if (selected)
                        {
                            Deselect();
                        }
                        else
                        {
                            Select();
                        }
                    }

                    m_Active = false;
                    dragged = false;
                    this.ReleaseMouse();
                    e.StopPropagation();
                }
            }
        }
    }
}
