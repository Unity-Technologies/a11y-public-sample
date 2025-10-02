using System;
using System.Linq;
using Unity.Properties;
using UnityEngine;
using UnityEngine.Accessibility;
using UnityEngine.UIElements;

namespace Unity.Samples.LetterSpell
{
    /// <summary>
    /// This class represents the view of letter cards.
    /// </summary>
    [UxmlElement]
    public partial class LetterCardView : VisualElement
    {
        internal static readonly BindingId itemSizeProperty = nameof(itemSize);
        internal static readonly BindingId itemSpacingProperty = nameof(itemSpacing);
        internal static readonly BindingId lettersProperty = nameof(letters);
        //internal static readonly BindingId reorderedLettersProperty = nameof(reorderedLetters);
        
        /// <summary>
        /// USS class name of the letter card view.
        /// </summary>
        public static readonly string ussClassName = "lsp-letter-card-view";
        
        /// <summary>
        /// USS class name of the letter card view.
        /// </summary>
        public static readonly string animatedUssClassName = ussClassName + "--animated";
        
        /// <summary>
        /// USS class name of an item in the letter card view.
        /// </summary>
        public static readonly string itemUssClassName = ussClassName + "__item";
        
        /// <summary>
        /// USS class name of a selected item in the letter card view.
        /// </summary>
        public static readonly string selectedItemUssClassName = itemUssClassName + "--selected";
        
        /// <summary>
        /// USS class name of a focused item in the letter card view.
        /// </summary>
        public static readonly string focusItemUssClassName = itemUssClassName + "--focused";
        
        /// <summary>
        /// USS class name of a dragged item in the letter card view.
        /// </summary>
        public static readonly string draggedItemUssClassName = itemUssClassName + "--dragged";
        
        /// <summary>
        /// USS class name of a dragged item that is being moved to the left in the letter card view.
        /// </summary>
        public static readonly string draggedLeftItemUssClassName = itemUssClassName + "--dragged-left";
        
        /// <summary>
        /// USS class name of a dragged item that is being moved to the right in the letter card view.
        /// </summary>
        public static readonly string draggedRightItemUssClassName = itemUssClassName + "--dragged-right";
        
        /// <summary>
        /// USS class name of an animated item in the letter card view.
        /// </summary>
        public static readonly string animatedItemUssClassName = itemUssClassName + "--animated";
        
        /// <summary>
        /// USS class name of the insertion placeholder item in the letter card view.
        /// </summary>
        public static readonly string insertionPlaceholderItemUssClassName = ussClassName + "__insertion-placeholder-item";
        
        static CustomStyleProperty<float> s_ItemSizeProperty = new("--item-size");
        static CustomStyleProperty<float> s_ItemSpacingProperty = new("--item-spacing");
        
        const int kDefaultItemSize = 208;
        const int k_DefaultItemSpacing = 30;
        static public int cardSize = 208;

        float m_ItemSize = kDefaultItemSize;
        float m_ItemSpacing = k_DefaultItemSpacing;
        bool m_ItemSizeIsSetInline;
        bool m_ItemSpacingIsSetInline;
        
        VisualElement m_InsertionPlaceholder;
        LetterCardViewItem m_SelectedCard;

        private char[] m_Letters;
        
        bool m_Animated;
        
        // Interaction
        int m_StartIndex;
        int m_InsertionIndex = -1;
        bool m_Active;
        Vector2 m_StartMousePos;
        bool m_Dragging;
        bool m_SwipeLeft;

        /// <summary>
        /// The size of each item in the letter card view.
        /// </summary>
        [CreateProperty]
        public float itemSize
        {
            get => m_ItemSize;
            set
            {
                if (Mathf.Approximately(m_ItemSize, value))
                    return;
                
                m_ItemSizeIsSetInline = true;
                m_ItemSize = value;
                NotifyPropertyChanged(itemSizeProperty);
            }
        }

        /// <summary>
        /// The spacing between items in the letter card view.
        /// </summary>
        [CreateProperty]
        public float itemSpacing
        {
            get => m_ItemSpacing;
            set
            {
                if (Mathf.Approximately(m_ItemSpacing, value))
                    return;
                
                m_ItemSpacingIsSetInline = true;
                m_ItemSpacing = value;
                NotifyPropertyChanged(itemSpacingProperty);
            }
        }
        
        /// <summary>
        /// The selected item in the letter card view.
        /// </summary>
        public LetterCardViewItem selectedCard
        {
            get => m_SelectedCard;
            set
            {
                if (m_SelectedCard == value)
                {
                    return;
                }

                if (m_SelectedCard != null)
                    m_SelectedCard.selected = false;
                
                m_SelectedCard = value;
                
                if (m_SelectedCard != null)
                    m_SelectedCard.selected = true;
            }
        }

        /* void ApplyFontScale()
         {
             style.fontSize = fontScale * 130;
             cardSize = (int)(itemSize * fontScale);
         }

         public float fontScale
         {
             get => s_FontScale;
             set
             {
                 s_FontScale = value;
                 style.fontSize = value * 130;
                 cardSize = (int)(itemSize * value);
             }
         }

         public int spacing = 30;*/

        /// <summary>
        /// Indicates whether the user can interact with the letter cards.
        /// </summary>
        public bool interactable { get; set; } = true;

        /// <summary>
        /// The view model of letter cards.
        /// </summary>
        [CreateProperty, UxmlAttribute]
        public char[] letters
        {
            get => m_Letters;
            set
            {
                if (m_Letters == value)
                    return;

                m_Letters = value;
                NotifyLettersChanged();
                RebuildItems();
            }
        }
        
        void NotifyLettersChanged()
        {
            NotifyPropertyChanged(lettersProperty);
            lettersChanged?.Invoke(this);
        }

        /// <summary>
        /// Indicates whether the letter cards should animate when they change position.
        /// </summary>
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
                EnableInClassList(animatedUssClassName, animated);
            }
        }
        
        /// <summary>
        /// The index where the dragged card will be inserted once dropped.
        /// </summary>
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

        /// <summary>
        /// Event sent when a letter card is reordered.
        /// </summary>
        public event Action<LetterCardView, int, int> letterReordered;
        
        /// <summary>
        /// Event sent when the letters property changes.
        /// </summary>
        public event Action<LetterCardView> lettersChanged;
    
        /// <summary>
        /// Constructs a letter card view.
        /// </summary>
        public LetterCardView()
        {
            AddToClassList(ussClassName);
            
            m_InsertionPlaceholder = new VisualElement();
            m_InsertionPlaceholder.AddToClassList(insertionPlaceholderItemUssClassName);

            RegisterCallback<MouseDownEvent>(OnMouseDown);
            RegisterCallback<MouseMoveEvent>(OnMouseMove);
            RegisterCallback<MouseUpEvent>(OnMouseUp);
            RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);
        }

        void OnCustomStyleResolved(CustomStyleResolvedEvent e)
        {
            ReadCustomProperties(e.customStyle);
        }
        
        void ReadCustomProperties(ICustomStyle customStyleProvider)
        {
            if (!m_ItemSizeIsSetInline)
            {
                if (customStyleProvider.TryGetValue(s_ItemSizeProperty, out var itemSize))
                {
                    m_ItemSize = itemSize;
                }
                else
                {
                    m_ItemSize = kDefaultItemSize;
                }
            }
            
            if (!m_ItemSpacingIsSetInline)
            {
                if (customStyleProvider.TryGetValue(s_ItemSpacingProperty, out var itemSpacing))
                {
                    m_ItemSpacing = itemSpacing;
                }
                else
                {
                    m_ItemSpacing = k_DefaultItemSpacing;
                }
            }
        }

        /// <summary>
        /// Regenerates all the letter cards.
        /// </summary>
        void RebuildItems()
        {
            selectedCard = null;
            //accessibilityFocusedCard = null;

            // Remove all cards.
            Clear();

            // Generate new card items.
            foreach (var letter in letters)
            {
                var cardItem = new LetterCardViewItem(letter);
                Add(cardItem);
                cardItem.dropped += (oldIndex, newIndex) => { letterReordered?.Invoke(this, oldIndex, newIndex); };
            }
        }

        void ComputeInsertionIndex(LetterCardViewItem card)
        {
            var newIndex = 0;
            var cc = childCount;
            var i = 0;

            foreach (var child in Children())
            {
                // Do not lay out the card being dragged.
                if (child is LetterCardViewItem { dragged: true })
                {
                    cc--;
                    break;
                }
            }

            var startX = (layout.width - (cc * cardSize + (cc + 1) * itemSpacing)) / 2;

            foreach (var child in Children())
            {
                // Do not lay out the card being dragged.
                if (card == child)
                {
                    continue;
                }

                var r = new Rect();
                r.xMin = startX + i * cardSize + (i + 1) * itemSpacing;
                r.width = cardSize;
                r.height = cardSize;
                r.yMin = (layout.height - cardSize)/2;

                if (card.layout.center.x > r.xMax)
                {
                    newIndex++;
                }

                i++;
            }

            insertionIndex = Math.Clamp(newIndex, 0, cc - 1);
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

        public void StartDragging(LetterCardViewItem card)
        {
            m_StartIndex = IndexOf(card);
            // style.position = Position.Absolute;
            card.PlaceInFront(this.Children().Last());
            Insert(m_StartIndex, m_InsertionPlaceholder);
            ComputeInsertionIndex(card);
            DoLayout();

            schedule.Execute(() =>
            {
                m_InsertionPlaceholder.AddToClassList(animatedItemUssClassName);
            }).ExecuteLater(400);
        }

        public void FinishDragging(LetterCardViewItem card)
        {
            m_InsertionPlaceholder.RemoveFromClassList(animatedItemUssClassName);
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

            MoveData(m_StartIndex, newIndex);
            card.OnDrop(m_StartIndex, newIndex);
            DoLayout();
        }

        public void Drag(LetterCardViewItem card)
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
                if (child is LetterCardViewItem { dragged: true })
                {
                    cc--;
                    break;
                }
            }

            var startX = (layout.width - (cc * cardSize + (cc + 1) * itemSpacing)) / 2;

            foreach (var child in Children())
            {
                // Do not lay out the card being dragged.
                if (child is LetterCardViewItem { dragged: true })
                {
                    continue;
                }

                child.style.left = startX + i * cardSize + (i + 1) * itemSpacing;
                child.style.width = cardSize;
                child.style.height = cardSize;
                child.style.top = 0;
                i++;
            }
        }

        void OnMouseDown(MouseDownEvent e)
        {
            m_Active = true;
            m_StartMousePos = e.localMousePosition;
            // Debug.Log("Mouse Down");
        }

        void OnMouseMove(MouseMoveEvent e)
        {
            if (m_Active)
            {
                var diff = e.localMousePosition - m_StartMousePos;
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

        void OnSwipeLeft()
        {
            MoveSelectedCardLeft();
        }

        void OnSwipeRight()
        {
            MoveSelectedCardRight();
        }
        
        internal void ApplyFontScale()
        {
           // style.fontSize = fontScale * 130;
           // cardSize = (int)(itemSize * fontScale);
        }

        /// <summary>
        /// Moves the selected card to the left by the specified count.
        /// </summary>
        /// <param name="count">The amount of steps the card should move</param>
        /// <returns>True if the card was successfully moved</returns>
        public bool MoveSelectedCardLeft(int count = 1)
        {
            return MoveSelectedCard(true, count);
        }
        
        /// <summary>
        /// Moves the selected card to the right by the specified count.
        /// </summary>
        /// <param name="count">The amount of steps the card should move</param>
        /// <returns>True if the card was successfully moved</returns>
        public bool MoveSelectedCardRight(int count = 1)
        {
            return MoveSelectedCard(false, count);
        }
        
        /// <summary>
        /// Moves the selected card to the left or right by the specified count.
        /// If no card is selected, it will not move and return false.
        /// If the selected card is already at the leftmost or rightmost position, it will not move and return false.
        /// If the card is successfully moved, it will return true.
        /// </summary>
        /// <param name="shouldMoveLeft">Indicate whther the card should be left or right</param>
        /// <param name="count">The amount of steps by which the selected card should be moved</param>
        /// <returns>True if the card was successfully moved</returns>
        public bool MoveSelectedCard(bool shouldMoveLeft, int count = 1)
        {
            var draggable = m_SelectedCard;

            if (draggable == null)
            {
                return false;
            }

            // var accElement = draggable.transform.GetComponent<AccessibleElement>();

            if (shouldMoveLeft ? MoveCardLeft(draggable, count) : MoveCardRight(draggable, count))
            {
                var index = IndexOf(draggable);
                var otherSiblingIndex = shouldMoveLeft ? index + count : index - count;
                var otherSibling = this[otherSiblingIndex] as LetterCardViewItem;
                var message = $"Moved {draggable.letter} {(shouldMoveLeft ? "before" : "after")} {otherSibling.letter}";

                // Announce that the card was moved.
                AssistiveSupport.notificationDispatcher.SendAnnouncement(message);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Moves the specified card to the left by the specified count.
        /// If the card is already at the leftmost position, it will not move and return false.
        /// If the card is not part of a LetterCardView, it will not move and return false.
        /// If the card is successfully moved, it will return true.
        /// </summary>
        /// <param name="card">The card to move</param>
        /// <param name="count">The amount of steps the card should move</param>
        /// <returns>Returns true if the card was moved</returns>
        public bool MoveCardLeft(LetterCardViewItem card, int count = 1)
        {
            if (IndexOf(card) - count < 0)
            {
                return false;
            }

            MoveToIndex(card, IndexOf(card) - count);

            return true;
        }

        /// <summary>
        /// Moves the specified card to the right by the specified count.
        /// If the card is already at the rightmost position, it will not move and return false.
        /// If the card is not part of a LetterCardView, it will not move and return false.
        /// If the card is successfully moved, it will return true.
        /// </summary>
        /// <param name="card">The card to move</param>
        /// <param name="count">The amount of steps the card should move</param>
        /// <returns>Returns true if the card was moved</returns>
        public bool MoveCardRight(LetterCardViewItem card, int count = 1)
        {
            if (parent.IndexOf(this) + count >= parent.childCount)
            {
                return false;
            }

            MoveToIndex(card, IndexOf(card) + count);

            return true;
        }

        void MoveToIndex(LetterCardViewItem card, int index)
        {
            var oldIndex = IndexOf(card);
            
            MoveData(oldIndex, index);

            if (index == childCount - 1)
            {
                card.PlaceInFront(this[index]);
            }
            else
            {
                if (index < oldIndex)
                {
                    card.PlaceBehind(this[index]);
                }
                else
                {
                    card.PlaceInFront(this[index]);
                }
            }
            DoLayout();
            card.OnDrop(oldIndex, index);
        }

        void MoveData(int oldIndex, int newIndex)
        {
            if (m_Letters == null || oldIndex < 0 || oldIndex >= m_Letters.Length || newIndex < 0 || newIndex >= m_Letters.Length)
            {
                return;
            }
            
            // Move the letter in-place in the char array without copying the entire array and
            // just moving data.
            var letter = m_Letters[oldIndex];
            
            if (oldIndex < newIndex)
            {
                Array.Copy(m_Letters, oldIndex + 1, m_Letters, oldIndex, newIndex - oldIndex);
            }
            else if (oldIndex > newIndex)
            {
                Array.Copy(m_Letters, newIndex, m_Letters, newIndex + 1, oldIndex - newIndex);
            }
            m_Letters[newIndex] = letter;
            
            NotifyLettersChanged();
        }
    }
}
