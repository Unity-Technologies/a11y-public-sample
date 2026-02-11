using System;
using UnityEngine;
using UnityEngine.Accessibility;
using UnityEngine.UIElements;

namespace Unity.Samples.LetterSpell
{
    /// <summary>
    /// UI Toolkit custom control version of LetterCard.
    /// Represents a draggable letter card in the gameplay UI.
    /// </summary>
    [UxmlElement]
    public partial class UITKLetterCard : VisualElement
    {
        const string k_SelectedClass = "letter-button-selected";

        string m_Letter = "A";

        /// <summary>
        /// The letter displayed on this card.
        /// </summary>
        [UxmlAttribute]
        public string letter
        {
            get => m_Letter;
            set
            {
                m_Letter = value;

                name = value;

                if (m_LetterButton != null)
                {
                    m_LetterButton.text = value;
                }
            }
        }

        Button m_LetterButton;
        VisualElement m_PlaceholderCard;

        bool m_IsSelected;
        bool m_IsBeingDragged;
        int m_StartIndex;
        Vector2 m_Offset;

        public UITKLetterCard()
        {
            // Load the UXML template.
            var template = Resources.Load<VisualTreeAsset>("UI Toolkit/UXML/letter-card");
            template.CloneTree(this);

            AddToClassList("letter-card");

            m_LetterButton = this.Q<Button>();

            SetSelected(false);

            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }

        void OnAttachToPanel(AttachToPanelEvent evt)
        {
            // Register pointer events for drag functionality.
            // Use TrickleDown to capture events before the button handles them.
            RegisterCallback<PointerDownEvent>(OnPointerDown, TrickleDown.TrickleDown);
            RegisterCallback<PointerMoveEvent>(OnPointerMove, TrickleDown.TrickleDown);
            RegisterCallback<PointerUpEvent>(OnPointerUp, TrickleDown.TrickleDown);

            // Handle cases where the pointer is released outside the card's bounds.
            RegisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);

            AssistiveSupport.screenReaderStatusChanged += OnScreenReaderStatusChanged;
        }

        void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            UnregisterCallback<PointerDownEvent>(OnPointerDown, TrickleDown.TrickleDown);
            UnregisterCallback<PointerMoveEvent>(OnPointerMove, TrickleDown.TrickleDown);
            UnregisterCallback<PointerUpEvent>(OnPointerUp, TrickleDown.TrickleDown);
            UnregisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);

            AssistiveSupport.screenReaderStatusChanged -= OnScreenReaderStatusChanged;
        }

        void OnPointerDown(PointerDownEvent evt)
        {
            m_IsBeingDragged = true;
            m_StartIndex = parent.IndexOf(this);

            // Calculate the offset between the pointer position and the top-left corner of the card.
            m_Offset = evt.localPosition;

            // Capture the pointer to receive events even when outside the element.
            this.CapturePointer(evt.pointerId);

            CreatePlaceholder();

            SetSelected(true);

            // Move the card to the end so that it will be rendered on top of the other cards while dragging.
            BringToFront();

            style.position = Position.Absolute;
            MoveToPosition(evt.position);

            style.height = resolvedStyle.height;
            style.rotate = new Rotate(Angle.Degrees(-15));

            evt.StopPropagation();
        }

        void OnPointerMove(PointerMoveEvent evt)
        {
            if (!m_IsBeingDragged)
            {
                return;
            }

            MoveToPosition(evt.position);

            MovePlaceholder();

            evt.StopPropagation();
        }

        void OnPointerUp(PointerUpEvent evt)
        {
            if (!m_IsBeingDragged)
            {
                return;
            }

            EndDrag();

            this.ReleasePointer(evt.pointerId);

            evt.StopPropagation();
        }

        void OnPointerCaptureOut(PointerCaptureOutEvent evt)
        {
            if (m_IsBeingDragged)
            {
                EndDrag();
            }
        }

        void EndDrag()
        {
            m_IsBeingDragged = false;

            var index = parent.IndexOf(m_PlaceholderCard);

            RemovePlaceholder();

            // Move and reset the card.
            PlaceSelfAt(index);

            style.position = Position.Relative;
            style.left = StyleKeyword.Null;
            style.top = StyleKeyword.Null;
            style.height = new StyleLength(new Length(100, LengthUnit.Percent));
            style.rotate = new Rotate(Angle.Degrees(0));

            SetSelected(false);

            Gameplay.instance.ReorderLetter(m_StartIndex, index);
        }

        /// <summary>
        /// Resets the card when the screen reader status changes.
        /// </summary>
        void OnScreenReaderStatusChanged(bool _)
        {
            SetSelected(false);
        }

        void SetSelected(bool selected)
        {
            if (m_IsSelected == selected)
            {
                return;
            }

            m_IsSelected = selected;

            if (selected)
            {
                m_LetterButton?.AddToClassList(k_SelectedClass);
            }
            else
            {
                m_LetterButton?.RemoveFromClassList(k_SelectedClass);
            }

            if (AssistiveSupport.isScreenReaderEnabled && !m_IsSelected)
            {
                Gameplay.instance.CheckWordComplete();
            }
        }

        void CreatePlaceholder()
        {
            m_PlaceholderCard = new VisualElement
            {
                name = "letter-card-placeholder",
                style =
                {
                    // Copy the size of this card.
                    width = resolvedStyle.width,
                    height = resolvedStyle.height,
                    marginLeft = resolvedStyle.marginLeft,
                    marginRight = resolvedStyle.marginRight,
                    flexShrink = 0
                }
            };

            // Insert the placeholder at the index of this card.
            parent.Insert(m_StartIndex, m_PlaceholderCard);
        }

        void MovePlaceholder()
        {
            if (m_PlaceholderCard == null || parent == null)
            {
                return;
            }

            var index = CalculatePlaceholderIndex();
            if (index >= 0)
            {
                parent.Insert(index, m_PlaceholderCard);
            }
        }

        int CalculatePlaceholderIndex()
        {
            if (parent == null)
            {
                return -1;
            }

            var currentIndex = parent.IndexOf(m_PlaceholderCard);
            var targetIndex = 0;

            // Calculate the target index based on the position of the center of this card relative to the other cards.
            for (var i = 0; i < parent.childCount; i++)
            {
                var otherCard = parent[i];
                if (otherCard == this || otherCard == m_PlaceholderCard)
                {
                    continue;
                }

                if (otherCard.worldBound.center.x < worldBound.center.x)
                {
                    targetIndex++;
                }
            }

            // Adjust for the dragged card being at the end.
            if (targetIndex > currentIndex)
            {
                targetIndex = Mathf.Min(targetIndex, parent.childCount - 1);
            }

            return targetIndex != currentIndex ? targetIndex : -1;
        }

        void RemovePlaceholder()
        {
            m_PlaceholderCard?.RemoveFromHierarchy();
            m_PlaceholderCard = null;
        }

        void MoveToPosition(Vector3 position)
        {
            var parentLocalPos = parent.WorldToLocal(position);
            style.left = parentLocalPos.x - m_Offset.x - resolvedStyle.marginLeft;
            style.top = parentLocalPos.y - m_Offset.y;
        }

        public bool MoveLeft(int numberOfPositions)
        {
            var index = parent.IndexOf(this);
            if (index <= 0)
            {
                return false;
            }

            MoveToIndex(index - numberOfPositions);
            return true;
        }

        public bool MoveRight(int numberOfPositions)
        {
            var index = parent.IndexOf(this);
            if (parent == null || index >= parent.childCount - 1)
            {
                return false;
            }

            MoveToIndex(index + numberOfPositions);
            return true;
        }

        void MoveToIndex(int index)
        {
            var oldIndex = parent.IndexOf(this);
            PlaceSelfAt(index);

            Gameplay.instance.ReorderLetter(oldIndex, index);
        }

        void PlaceSelfAt(int index)
        {
            if (parent == null)
            {
                return;
            }

            index = Mathf.Clamp(index, 0, parent.childCount - 1);
            parent.Insert(index, this);
        }
    }
}
