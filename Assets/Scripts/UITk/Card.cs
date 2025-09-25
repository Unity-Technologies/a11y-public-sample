using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Accessibility;
using UnityEngine.Scripting;
using UnityEngine.UI;
using UnityEngine.UIElements;
using Unity.Samples.ScreenReader;
using Button = UnityEngine.UIElements.Button;

namespace Unity.Samples.LetterSpell
{
    class UITkLetterCard : VisualElement
    {
        private Label m_TextElement;
        private Vector2 m_Start;
        protected bool m_Active;
        private bool clampToParentEdges = true;
        private VisualElement m_Element;
        Button m_MoveLeftButton;
        Button m_MoveRightButton;
        Button m_UnselectCardButton;
        AccessibleVisualElement m_ButtonsContainer;
        
        CardListView cardListView => parent as CardListView;
        
        //[RegisterAccessible]
       // class LetterCardAccessibleElement : AccessibleElement
       // {
       //     public override string GetLabel() => (owner as UITkLetterCard).text;
       // }

       public bool selected => cardListView?.selectedCard == this;
       
        public string text
        {
            get => m_TextElement.text;
            set => m_TextElement.text = value;
        }

        public event Action<int, int> dropped;

        [RegisterAccessibilityHandler(typeof(UITkLetterCard))]
        [Preserve]
        class AccessibleLetterCardHandler : VisualElementAccessibilityHandler
        {
            private UITkLetterCard card => ownerElement as UITkLetterCard;
            
            public override string GetLabel() => card.text;
            protected override void BindToElement(VisualElement ve)
            {
                card.m_TextElement.GetOrCreateAccessibleProperties().ignored = true;
            }

            public AccessibleLetterCardHandler()
            {
                OnSelect += () =>
                {
                    var letter = ownerElement as UITkLetterCard;
                    
                    if (!letter.selected)
                        letter.Select();
                    else
                        letter.Unselect();
                    return true;
                };
            }
        }

        public void Select()
        {
            cardListView.selectedCard = this;
            OnScreenDebug.Log("Selected card: " + text);
        }
        
        public void Unselect()
        {
            if (this == cardListView.selectedCard)
                cardListView.selectedCard = null;
        }
        public UITkLetterCard()
        {
            m_TextElement = new Label();
            Add(m_TextElement);
            AddToClassList("lsp-letter-card");
            AddToClassList("lsp-card-view-item");
            //style.transitionProperty = new StyleList<StylePropertyName>(new List<StylePropertyName>{ new StylePropertyName("left")});
            //style.transitionDuration = new StyleList<TimeValue>(new List<TimeValue>{new(0.5f)});
           /* style.marginLeft = 4;
            style.marginTop = 4;
            style.marginRight = 4;
            style.marginBottom = 4;
            */
            /*style.fontSize = 40;
            style.alignItems = Align.Center;
            style.justifyContent = Justify.Center;

            style.borderBottomLeftRadius = 6;
            style.borderTopLeftRadius = 6;
            style.borderBottomRightRadius = 6;
            style.borderTopRightRadius = 6;*/
            
            //style.backgroundColor = Color.white;

            style.position = Position.Absolute;

            m_ButtonsContainer = new AccessibleVisualElement()
            {
                pickingMode = PickingMode.Ignore
            };
            m_ButtonsContainer.AddToClassList("lsp-letter-card__button-container");
            m_ButtonsContainer.accessible.modal = true;
            m_ButtonsContainer.style.display = DisplayStyle.None;
            m_MoveLeftButton = new Button { text = "<" };
            m_MoveLeftButton.AddToClassList("lsp-letter-card__button");
            m_MoveLeftButton.AddToClassList("lsp-letter-card__button--first");
            m_MoveLeftButton.clicked += ()=> MoveLeft();
            
            m_MoveRightButton = new Button { text = ">" };
            m_MoveRightButton.AddToClassList("lsp-letter-card__button");
            m_MoveRightButton.AddToClassList("lsp-letter-card__button--last");
            m_MoveRightButton.clicked += ()=> MoveRight();
            
            m_UnselectCardButton = new Button { name="cancelMoveButton", text = "X" };
            m_UnselectCardButton.AddToClassList("lsp-letter-card__button");
            //m_UnselectCardButton.AddToClassList("lsp-letter-card__button--flat");
            m_UnselectCardButton.clicked += Unselect;
            
            m_ButtonsContainer.Add(m_MoveLeftButton);
            m_ButtonsContainer.Add(m_MoveRightButton);
            m_ButtonsContainer.Add(m_UnselectCardButton);
            
            Add(m_ButtonsContainer);
            
            RegisterCallbacksOnTarget();
            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
        }

        private bool m_Animated;

        private bool animated
        {
            get => m_Animated;
            set
            {
                if (m_Animated == value)
                    return;
                m_Animated = value;
                EnableInClassList( "animated", animated);
            }
        }

        void OnAttachToPanel(AttachToPanelEvent e)
        {
            cardListView.DoLayout();
            // Make them animated
            schedule.Execute(() => animated = true).ExecuteLater(500);
        }
        protected Rect CalculatePosition(float x, float y, float width, float height)
        {
            var rect = new Rect(x, y, width, height);

           /* if (clampToParentEdges)
            {
                Rect shadowRect = hierarchy.parent.layout.;
                rect.x = Mathf.Clamp(rect.x, shadowRect.xMin, Mathf.Abs(shadowRect.xMax - rect.width));
                rect.y = Mathf.Clamp(rect.y, shadowRect.yMin, Mathf.Abs(shadowRect.yMax - rect.height));

                // Reset size, we never intended to change them in the first place
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

        private bool m_Dragging;

        public bool dragging
        {
            get => m_Dragging;
            set
            {
                if (m_Dragging == value)
                    return;
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
            if (e.ctrlKey)
            {
                if (cardListView.selectedCard == this)
                    cardListView.selectedCard = null;
                else
                    cardListView.selectedCard = this;
            }
            else
            {
                cardListView.selectedCard = this;
            }

            if (m_Active)
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
                Vector2 diff = e.localMousePosition - m_Start;

                if (dragging == false && Math.Abs(diff.x) > 5)
                {
                    dragging = true;
                    return;
                }
                
                if (dragging == false)
                    return;

                var targetScale = this.transform.scale;
                    diff.x *= targetScale.x;
                    diff.y *= targetScale.y;

                Rect rect = CalculatePosition(this.layout.x + diff.x, this.layout.y + diff.y, this.layout.width, this.layout.height);

                /*if (this.isLayoutManual)
                {
                    target.layout = rect;
                }
                else if (target.resolvedStyle.position == Position.Absolute)
                {*/
                    this.style.left = rect.x;
                    //this.style.top = rect.y;
                //}

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
                return false;
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
                return false;
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

            Debug.Log($"Move {this} To Index {index}");
            if (index == parent.childCount - 1)
            {
                this.PlaceInFront(this.parent[index]);
            }
            else
            {
                if (index < oldIndex)
                    this.PlaceBehind(parent[index]);
                else
                    this.PlaceInFront(parent[index]);
            }
            cardListView.DoLayout();
            UpdateButtonEnableState();
            dropped?.Invoke(oldIndex, index);
        }

        public void UpdateButtonEnableState()
        {
            m_MoveLeftButton.SetEnabled(parent.IndexOf(this) != 0);
            m_MoveRightButton.SetEnabled(parent.IndexOf(this) != parent.childCount - 1);
        }
    }

    [UxmlElement]
    partial class CardListView : VisualElement
    {
        static public int cardSize = 208;
        public int spacing = 30;
        private VisualElement m_InsertionPlaceholder;
        
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
             int newIndex = 0;
             int cc = childCount;
             int i = 0;
             
             foreach (var child in Children())
             {
                 var c = child as UITkLetterCard;
                 // Do not layout card being dragged
                 if (c != null && c.dragging)
                 {
                     cc--;
                     break;
                 }
             }
             var startX = (layout.width - (cc * cardSize + (cc + 1) * spacing)) / 2;
             
            foreach (var child in Children())
            {
                // Do not layout card being dragged
                if (card == child)
                    continue;

                var r = new Rect();
                r.xMin = startX + i * cardSize + (i + 1) *spacing;
                 r.width = cardSize;
                 r.height = cardSize;
                 r.yMin = (layout.height - cardSize)/2;
                
                if (card.layout.center.x > r.xMax)
                    newIndex++;
                i++;
            }
            insertionIndex = Math.Clamp(newIndex, 0, cc - 1);
         }

        private int m_InsertionIndex = -1;

        private int insertionIndex
        {
            get => m_InsertionIndex;
            set
            {
                if (m_InsertionIndex == value)
                    return;
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
                    ve.PlaceBehind(this[index]);
                else
                    ve.PlaceInFront(this[index]);
            }

            DoLayout();
        }

        void OnGeometryChanged(GeometryChangedEvent e)
        {
            DoLayout();
        }

        private int m_StartIndex;
        public void StartDragging(UITkLetterCard card)
        {
            m_StartIndex = IndexOf(card);
            // style.position = Position.Absolute;
            card.PlaceInFront(this.Children().Last());
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
                card.PlaceBehind(this[newIndex]);
            card.OnDrop(m_StartIndex, newIndex);
            DoLayout();
        }

        public void Drag(UITkLetterCard card)
        {
            ComputeInsertionIndex(card);
        }
        
        public void DoLayout()
        {
            int i = 0;
            
           // Debug.Log(" DoLayout()");

            bool hasDrag = false;
            var cc = childCount;
            foreach (var child in Children())
            {
                var card = child as UITkLetterCard;
                // Do not layout card being dragged
                if (card != null && card.dragging)
                {
                    hasDrag = true;
                    cc--;
                    break;
                }
            }

            var startX = (layout.width - (cc * cardSize + (cc + 1) * spacing)) / 2;
            foreach (var child in Children())
            {
                var card = child as UITkLetterCard;
                // Do not layout card being dragged
                if (card != null && card.dragging)
                    continue;
                child.style.left = startX + i * cardSize + (i + 1) *spacing;
                child.style.width = cardSize;
                child.style.height = cardSize;
                child.style.top = (layout.height - cardSize)/2;
                i++;
                
            }
            
        //    Debug.Log("EndDoLayout()");
        }
        
        protected void RegisterCallbacksOnTarget()
        {
            RegisterCallback<MouseDownEvent>(OnMouseDown);
            RegisterCallback<MouseMoveEvent>(OnMouseMove);
            RegisterCallback<MouseUpEvent>(OnMouseUp);
            RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }
        

        private bool m_Active;
        private Vector2 m_Start;
        private bool dragging;
        private bool swipeLeft;

        private UITkLetterCard m_SelectedCard;

        public UITkLetterCard selectedCard
        {
            get => m_SelectedCard;
            set
            {
                if (m_SelectedCard == value)
                    return;
                m_SelectedCard?.RemoveFromClassList("selected");
                m_SelectedCard = value;
                m_SelectedCard?.AddToClassList("selected");
                if (m_SelectedCard != null)
                    m_SelectedCard.UpdateButtonEnableState();
            }
        }
        
        void OnMouseDown(MouseDownEvent e)
        {
            m_Active = true;
            m_Start = e.localMousePosition;
           // Debug.Log("Mouse Down");
        }

        void OnMouseMove(MouseMoveEvent e)
        {
            if (m_Active)
            {
                Vector2 diff = e.localMousePosition - m_Start;
                swipeLeft = diff.x < 0;
                if (dragging == false && Math.Abs(diff.x) > 5)
                {
                    dragging = true;
                    return;
                }
            }
        }

        void OnMouseUp(MouseUpEvent e)
        {
            
            if (dragging)
            {
              //  Debug.Log("Mouse Up " + swipeLeft);
                if (swipeLeft)
                    OnSwipeLeft();
                else
                {
                    OnSwipeRight();
                }
            }

            m_Active = true;
            dragging = true;
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

            //  var accElement = draggable.transform.GetComponent<AccessibleElement>();
            if (shouldMoveLeft ? draggable.MoveLeft() : draggable.MoveRight())
            {
                var index = IndexOf(draggable);
                var otherSiblingIndex = shouldMoveLeft ? index + 1 : index - 1;
                var otherSibling = this[otherSiblingIndex];
                var message = $"Moved {draggable.name} {(shouldMoveLeft ? "before" : "after")} {otherSibling.name}";

                // Announce that the card was moved.
                AssistiveSupport.notificationDispatcher.SendAnnouncement(message);

                //    AssistiveSupport.defaultHierarchy.MoveNode(accElement.node, accElement.node.parent,
                //      accElement.transform.GetSiblingIndex());

                // Only refresh the frames for now to leave the announcement request to be handled.
                // this.ManualRectRefresh();

                //AssistiveSupport.notificationDispatcher.SendLayoutChanged(accElement.node);
                //this.schedule.Execute(()=>AssistiveSupport.notificationDispatcher.SendLayoutChanged()).ExecuteLater(500);
            }
        }

        protected  void UnregisterCallbacksFromTarget()
        {
            UnregisterCallback<MouseDownEvent>(OnMouseDown);
            UnregisterCallback<MouseMoveEvent>(OnMouseMove);
            UnregisterCallback<MouseUpEvent>(OnMouseUp);
        }
    }
}
