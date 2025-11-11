using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Accessibility;
using UnityEngine.UIElements;
using Unity.Samples.ScreenReader;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.SmartFormat.PersistentVariables;

namespace Unity.Samples.LetterSpell
{
    class UITkLetterCard : AccessibleVisualElement
    {
        Label m_TextElement;
        Vector2 m_Start;
        protected bool m_Active;
        VisualElement m_Element;

        CardListView cardListView => parent as CardListView;

        // [RegisterAccessible]
        // class LetterCardAccessibleElement : AccessibleElement
        // {
        //     public override string GetLabel() => (owner as UITkLetterCard).text;
        // }

        bool m_Selected;

        public bool selected
        {
            get => m_Selected;
            set
            {
                if (m_Selected == value)
                {
                    return;
                }

                m_Selected = value;

                if (value)
                {
                    Focus();
                }

                UpdateSelectedState();
            }
        }

        void UpdateSelectedState()
        {
            EnableInClassList("selected", selected);
        }

        public string text
        {
            get => m_TextElement.text;
            set
            {
                m_TextElement.text = value;
                accessible.label = value;
            }
        }

        public event Action<int, int> dropped;

        public void Select()
        {
            if (cardListView.selectedCard != this)
            {
                cardListView.selectedCard = this;

                accessible.hint = LocalizationSettings.StringDatabase.GetLocalizedString("Game Text", "LETTER_CARD_HINT_SELECTED");
            }
        }

        public void Unselect()
        {
            if (this == cardListView.selectedCard)
            {
                cardListView.selectedCard = null;

                accessible.hint = LocalizationSettings.StringDatabase.GetLocalizedString("Game Text", "LETTER_CARD_HINT_UNSELECTED");
            }
        }

        public UITkLetterCard()
        {
            m_TextElement = new Label();
            Add(m_TextElement);
            AddToClassList("lsp-letter-card");
            AddToClassList("lsp-card-view-item");

            focusable = true;

            style.position = Position.Absolute;

            RegisterCallbacksOnTarget();
            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);

            // Accessibility
            m_TextElement.GetOrCreateAccessibleProperties().ignored = true;

            accessible.hint = LocalizationSettings.StringDatabase.GetLocalizedString("Game Text", "LETTER_CARD_HINT_UNSELECTED");
            accessible.selected += () =>
            {
                if (!selected)
                {
                    Select();
                }
                else
                {
                    Unselect();
                }

                return true;
            };
        }

        bool m_Animated;

        bool animated
        {
            get => m_Animated;
            set
            {
                if (m_Animated == value)
                {
                    return;
                }

                m_Animated = value;
                EnableInClassList( "animated", animated);
            }
        }

        void OnAttachToPanel(AttachToPanelEvent e)
        {
            cardListView.DoLayout();

            // Make them animated.
            // TODO: FIX ANIMATION WHEN STARTING A GAME
            // schedule.Execute(() => animated = true).ExecuteLater(500);
        }

        protected Rect CalculatePosition(float x, float y, float width, float height)
        {
            var rect = new Rect(x, y, width, height);

            /*if (clampToParentEdges)
            {
                Rect shadowRect = hierarchy.parent.layout.;
                rect.x = Mathf.Clamp(rect.x, shadowRect.xMin, Mathf.Abs(shadowRect.xMax - rect.width));
                rect.y = Mathf.Clamp(rect.y, shadowRect.yMin, Mathf.Abs(shadowRect.yMax - rect.height));

                // Reset size, we never intended to change them in the first place.
                rect.width = width;
                rect.height = height;
            }*/

            return rect;
        }

        protected void RegisterCallbacksOnTarget()
        {
            RegisterCallback<MouseDownEvent>(OnMouseDown);
            RegisterCallback<MouseMoveEvent>(OnMouseMove);
            RegisterCallback<MouseUpEvent>(OnMouseUp);
        }

        protected  void UnregisterCallbacksFromTarget()
        {
            UnregisterCallback<MouseDownEvent>(OnMouseDown);
            UnregisterCallback<MouseMoveEvent>(OnMouseMove);
            UnregisterCallback<MouseUpEvent>(OnMouseUp);
        }

        bool m_Dragging;

        int m_DraggingDirection;

        int draggingDirection
        {
            get => m_DraggingDirection;
            set
            {
                if (m_DraggingDirection == value)
                {
                    return;
                }

                m_DraggingDirection = value;
                EnableInClassList("dragging--left", m_DraggingDirection < 0);
                EnableInClassList("dragging--right", m_DraggingDirection > 0);
            }
        }

        public bool dragging
        {
            get => m_Dragging;
            set
            {
                if (m_Dragging == value)
                {
                    return;
                }

                m_Dragging = value;
                animated = !m_Dragging;
                EnableInClassList("dragged", m_Dragging);

                if (value)
                {
                    cardListView.StartDragging(this);
                }
                else
                {
                    cardListView.FinishDragging(this);
                    Unselect();
                }

                cardListView.DoLayout();
            }
        }

        public void OnDrop(int oldIndex, int newIndex)
        {
            dropped?.Invoke(oldIndex, newIndex);
        }

        protected void OnMouseDown(MouseDownEvent e)
        {
            if (!cardListView.canPlayCards || m_Active)
            {
                e.StopImmediatePropagation();
                return;
            }

            if (panel?.GetCapturingElement(PointerId.mousePointerId) != null)
            {
                return;
            }

            if (e.button == (int)MouseButton.LeftMouse)
            {
                m_Start = e.localMousePosition;

                m_Active = true;

                // style.backgroundColor = Color.red;
                this.CaptureMouse();
                e.StopPropagation();
            }
        }

        protected void OnMouseMove(MouseMoveEvent e)
        {
            if (m_Active)
            {
                // Ensure the card is selected when we start dragging it.
                Select();

                var diff = e.localMousePosition - m_Start;

                if (!dragging && Math.Abs(diff.x) > 5)
                {
                    dragging = true;
                    return;
                }

                if (!dragging)
                {
                    return;
                }

                var targetScale = transform.scale;
                diff.x *= targetScale.x;
                diff.y *= targetScale.y;

                var rect = CalculatePosition(layout.x + diff.x, layout.y + diff.y, layout.width, layout.height);

                /*if (this.isLayoutManual)
                {
                    target.layout = rect;
                }
                else if (target.resolvedStyle.position == Position.Absolute)
                {*/

                var oldLeft = style.left.value.value;
                style.left = rect.x;
                draggingDirection = oldLeft < style.left.value.value ? -1 : 1;

                //     this.style.top = rect.y;
                // }

                cardListView.Drag(this);
                e.StopPropagation();
            }
        }

        protected void OnMouseUp(MouseUpEvent e)
        {
            if (m_Active)
            {
                if (e.button == (int)MouseButton.LeftMouse)
                {
                    // Select or unselect the card if we didn't drag it.
                    if (!m_Dragging)
                    {
                        if (selected)
                        {
                            Unselect();
                        }
                        else
                        {
                            Select();
                        }
                    }
                    m_Active = false;
                    dragging = false;

                    // style.backgroundColor = Color.white;

                    this.ReleaseMouse();
                    e.StopPropagation();
                }
            }
        }

        public bool MoveLeft(int count = 1)
        {
            if (parent == null)
            {
                return false;
            }

            if (parent.IndexOf(this) - count < 0)
            {
                return false;
            }

            MoveToIndex(parent.IndexOf(this) - count);

            return true;
        }

        public bool MoveRight(int count = 1)
        {
            if (parent == null)
            {
                return false;
            }

            if (parent.IndexOf(this) + count >= parent.childCount)
            {
                return false;
            }

            MoveToIndex(parent.IndexOf(this) + count);

            return true;
        }

        void MoveToIndex(int index)
        {
            var oldIndex = parent.IndexOf(this);

            if (index == parent.childCount - 1)
            {
                PlaceInFront(parent[index]);
            }
            else
            {
                if (index < oldIndex)
                {
                    PlaceBehind(parent[index]);
                }
                else
                {
                    PlaceInFront(parent[index]);
                }
            }

            cardListView.DoLayout();
            dropped?.Invoke(oldIndex, index);
        }
    }

    [UxmlElement]
    partial class CardListView : VisualElement
    {
        static public int defaultCardSize = 208;
        static public float s_FontScale = 1.0f;
        static public int cardSize = 208;

        public float fontScale
        {
            get => s_FontScale;
            set
            {
                s_FontScale = value;
                style.fontSize = value * 130;
                cardSize = (int)(defaultCardSize * value);
            }
        }

        public int spacing = 30;
        VisualElement m_InsertionPlaceholder;

        public bool canPlayCards { get; set; }

        public CardListView()
        {
            m_InsertionPlaceholder = new VisualElement();
            m_InsertionPlaceholder.AddToClassList("lsp-card-view-item");
            m_InsertionPlaceholder.AddToClassList("lsp-insertion-placeholder");
            // m_InsertionPlaceholder.style.backgroundColor = new Color(0, 0, 1, 0.5f);

            RegisterCallbacksOnTarget();
        }

        void ComputeInsertionIndex(UITkLetterCard card)
        {
            var newIndex = 0;
            var cc = childCount;
            var i = 0;

            foreach (var child in Children())
            {
                // Do not lay out the card being dragged.
                if (child is UITkLetterCard { dragging: true })
                {
                    cc--;
                    break;
                }
            }

            var startX = (layout.width - (cc * cardSize + (cc + 1) * spacing)) / 2;

            foreach (var child in Children())
            {
                // Do not lay out the card being dragged.
                if (card == child)
                {
                    continue;
                }

                var r = new Rect
                {
                    xMin = startX + i * cardSize + (i + 1) * spacing,
                    width = cardSize,
                    height = cardSize,
                    yMin = (layout.height - cardSize) / 2
                };

                if (card.layout.center.x > r.xMax)
                {
                    newIndex++;
                }

                i++;
            }

            insertionIndex = Math.Clamp(newIndex, 0, cc - 1);
        }

        int m_InsertionIndex = -1;

        int insertionIndex
        {
            get => m_InsertionIndex;
            set
            {
                if (m_InsertionIndex == value)
                {
                    return;
                }

                m_InsertionIndex = value;
                MoveToIndex(m_InsertionPlaceholder, value);
            }
        }

        void MoveToIndex(VisualElement ve, int index)
        {
            var currentIndex = ve.parent.IndexOf(ve);

            if (index == childCount - 1)
            {

                ve.PlaceInFront(this[index]);
            }
            else
            {
                if (index < currentIndex)
                {
                    ve.PlaceBehind(this[index]);
                }
                else
                {
                    ve.PlaceInFront(this[index]);
                }
            }

            DoLayout();
        }

        void OnGeometryChanged(GeometryChangedEvent e)
        {
            DoLayout();
        }

        int m_StartIndex;

        public void StartDragging(UITkLetterCard card)
        {
            m_StartIndex = IndexOf(card);
            // style.position = Position.Absolute;
            card.PlaceInFront(Children().Last());
            Insert(m_StartIndex, m_InsertionPlaceholder);
            ComputeInsertionIndex(card);
            DoLayout();

            schedule.Execute(() =>
            {
                m_InsertionPlaceholder.AddToClassList("animated");
            }).ExecuteLater(400);
        }

        public void FinishDragging(UITkLetterCard card)
        {
            m_InsertionPlaceholder.RemoveFromClassList("animated");
            m_InsertionPlaceholder.RemoveFromHierarchy();
            var newIndex = m_InsertionIndex;

            if (newIndex == childCount - 1)
            {
                card.PlaceInFront(this[newIndex - 1]);
            }
            else
            {
                card.PlaceBehind(this[newIndex]);
            }

            card.OnDrop(m_StartIndex, newIndex);
            DoLayout();
        }

        public void Drag(UITkLetterCard card)
        {
            ComputeInsertionIndex(card);
        }

        public void DoLayout()
        {
            var i = 0;

            var cc = childCount;

            foreach (var child in Children())
            {
                // Do not lay out the card being dragged.
                if (child is UITkLetterCard { dragging: true })
                {
                    cc--;
                    break;
                }
            }

            var startX = (layout.width - (cc * cardSize + (cc + 1) * spacing)) / 2;

            foreach (var child in Children())
            {
                // Do not lay out the card being dragged.
                if (child is UITkLetterCard { dragging: true })
                {
                    continue;
                }

                child.style.left = startX + i * cardSize + (i + 1) * spacing;
                child.style.width = cardSize;
                child.style.height = cardSize;
                child.style.top = 0; // DON'T CENTER ANYMORE, (layout.height - cardSize) / 2;
                i++;
            }
        }

        protected void RegisterCallbacksOnTarget()
        {
            RegisterCallback<MouseDownEvent>(OnMouseDown);
            RegisterCallback<MouseMoveEvent>(OnMouseMove);
            RegisterCallback<MouseUpEvent>(OnMouseUp);
            RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }

        bool m_Active;
        Vector2 m_Start;
        bool m_Dragging;
        bool m_SwipeLeft;

        UITkLetterCard m_SelectedCard;

        public UITkLetterCard selectedCard
        {
            get => m_SelectedCard;
            set
            {
                if (m_SelectedCard == value)
                {
                    return;
                }

                if (m_SelectedCard != null)
                {
                    m_SelectedCard.selected = false;
                }

                m_SelectedCard = value;

                if (m_SelectedCard != null)
                {
                    m_SelectedCard.selected = true;
                }
            }
        }

        void OnMouseDown(MouseDownEvent e)
        {
            m_Active = true;
            m_Start = e.localMousePosition;
        }

        void OnMouseMove(MouseMoveEvent e)
        {
            if (m_Active)
            {
                var diff = e.localMousePosition - m_Start;
                m_SwipeLeft = diff.x < 0;

                if (!m_Dragging && Math.Abs(diff.x) > 5)
                {
                    m_Dragging = true;
                }
            }
        }

        void OnMouseUp(MouseUpEvent e)
        {
            if (m_Dragging)
            {
                if (m_SwipeLeft)
                {
                    OnSwipeLeft();
                }
                else
                {
                    OnSwipeRight();
                }
            }

            m_Active = true;
            m_Dragging = true;
        }

        public void OnSwipeLeft()
        {
            MoveCard(true);
        }

        public void OnSwipeRight()
        {
            MoveCard(false);
        }

        void MoveCard(bool shouldMoveLeft)
        {
            var draggable = m_SelectedCard;

            if (draggable == null)
            {
                return;
            }

            // var accElement = draggable.transform.GetComponent<AccessibleElement>();

            if (shouldMoveLeft ? draggable.MoveLeft() : draggable.MoveRight())
            {
                var index = IndexOf(draggable);
                var otherSiblingIndex = shouldMoveLeft ? index + 1 : index - 1;
                var otherSibling = this[otherSiblingIndex];

                // Announce that the card was moved.
                var localizedString = new LocalizedString
                {
                    TableReference = "Game Text",
                    TableEntryReference = "ANNOUNCEMENT_CARD_MOVED"
                };

                var selectedLetter = new StringVariable
                {
                    Value = draggable.name
                };

                var moveLeft = new BoolVariable
                {
                    Value = shouldMoveLeft
                };

                var otherLetter = new StringVariable
                {
                    Value = otherSibling.name
                };

                localizedString.Add("selectedLetter", selectedLetter);
                localizedString.Add("shouldMoveLeft", moveLeft);
                localizedString.Add("otherLetter", otherLetter);

                localizedString.StringChanged += announcement =>
                    AssistiveSupport.notificationDispatcher.SendAnnouncement(announcement);

                // AssistiveSupport.defaultHierarchy.MoveNode(accElement.node, accElement.node.parent,
                //     accElement.transform.GetSiblingIndex());

                // // Only refresh the frames for now to leave the announcement request to be handled.
                // this.ManualRectRefresh();

                // AssistiveSupport.notificationDispatcher.SendLayoutChanged(accElement.node);
                // this.schedule.Execute(()=>AssistiveSupport.notificationDispatcher.SendLayoutChanged()).ExecuteLater(500);
            }
        }

        protected void UnregisterCallbacksFromTarget()
        {
            UnregisterCallback<MouseDownEvent>(OnMouseDown);
            UnregisterCallback<MouseMoveEvent>(OnMouseMove);
            UnregisterCallback<MouseUpEvent>(OnMouseUp);
        }
    }
}
