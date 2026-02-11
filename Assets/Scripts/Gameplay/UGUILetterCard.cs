using System;
using TMPro;
using Unity.Samples.ScreenReader;
using UnityEngine;
using UnityEngine.Accessibility;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Unity.Samples.LetterSpell
{
    public class UGUILetterCard : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        const string k_LetterCardDefaultHint = "Submit to select and start moving.";
        const string k_LetterCardSelectedHint = "Navigate left or right to move. Submit to unselect.";

        public Image backgroundImage;
        public Image selectedImage;

        string m_Letter = "A";

        /// <summary>
        /// The letter displayed on this card.
        /// </summary>
        public string letter
        {
            get => m_Letter;
            set
            {
                m_Letter = value;

                name = value;
                GetComponentInChildren<TextMeshProUGUI>().text = value;
                GetComponent<AccessibleElement>().label = value;
            }
        }

        RectTransform m_RectTransform;
        LayoutElement m_LayoutElement;
        BoxCollider2D m_BoxCollider2D;
        AccessibleElement m_AccessibleElement;

        GameObject m_PlaceholderCard;

        bool m_IsSelected;
        bool m_IsBeingDragged;
        int m_StartIndex;
        Vector3 m_Offset;

        void Start()
        {
            m_RectTransform = GetComponent<RectTransform>();
            m_LayoutElement = GetComponent<LayoutElement>();
            m_BoxCollider2D = GetComponent<BoxCollider2D>();

            m_AccessibleElement = GetComponent<AccessibleElement>();
            m_AccessibleElement.selected += OnSelected;

            AssistiveSupport.screenReaderStatusChanged += OnScreenReaderStatusChanged;

            SetSelected(false);
        }

        void Update()
        {
            if (m_BoxCollider2D.size != m_RectTransform.rect.size || m_BoxCollider2D.offset != m_RectTransform.rect.center)
            {
                var rect = m_RectTransform.rect;
                m_BoxCollider2D.size = rect.size;
                m_BoxCollider2D.offset = rect.center;
            }
        }

        void OnDestroy()
        {
            m_AccessibleElement.selected -= OnSelected;

            AssistiveSupport.screenReaderStatusChanged -= OnScreenReaderStatusChanged;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            m_IsBeingDragged = true;
            m_StartIndex = transform.GetSiblingIndex();

            // Calculate the offset between the pointer position and the center of the card so that we can maintain that
            // offset while dragging.
            m_Offset = transform.position - (Vector3)eventData.position;

            // Ignore the layout while dragging so that the other cards will stay in place, and we can move this card
            // freely.
            m_LayoutElement.ignoreLayout = true;

            CreatePlaceholder();

            SetSelected(true);

            // Move the card to the end so that it will be rendered on top of the other cards while dragging.
            transform.SetSiblingIndex(transform.parent.childCount - 1);

            transform.rotation = Quaternion.Euler(0, 0, 15);
        }

        public void OnDrag(PointerEventData eventData)
        {
            transform.position = (Vector3)eventData.position + m_Offset;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            m_IsBeingDragged = false;
            m_LayoutElement.ignoreLayout = false;

            var index = m_PlaceholderCard.transform.GetSiblingIndex();

            Destroy(m_PlaceholderCard);

            // Move and reset the card.
            LayoutRebuilder.MarkLayoutForRebuild(m_RectTransform);
            transform.SetSiblingIndex(index);
            transform.rotation = Quaternion.identity;

            SetSelected(false);

            Gameplay.instance.ReorderLetter(m_StartIndex, index);
        }

        /// <summary>
        /// Called when the card is being dragged and overlaps with another card. Checks if the midpoint of this card is
        /// within the other card's bounds, and if so, moves the placeholder card to the appropriate index.
        /// </summary>
        /// <param name="other">The other card that is overlapping with this card while dragging.</param>
        void OnTriggerStay2D(Collider2D other)
        {
            // Check if the midpoint of this card is within the other card's bounds.
            if (other.bounds.Contains(m_BoxCollider2D.bounds.center) && m_IsBeingDragged)
            {
                // Move the placeholder card.
                LayoutRebuilder.MarkLayoutForRebuild(m_RectTransform);
                m_PlaceholderCard?.transform.SetSiblingIndex(CalculatePlaceholderIndex());
            }
        }

        bool OnSelected()
        {
            SetSelected(!m_IsSelected);
            return true;
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

            backgroundImage.gameObject.SetActive(!selected);
            selectedImage.gameObject.SetActive(selected);

            m_AccessibleElement.hint = selected ? k_LetterCardSelectedHint : k_LetterCardDefaultHint;

            if (AssistiveSupport.isScreenReaderEnabled && !m_IsSelected)
            {
                Gameplay.instance.CheckWordComplete();
            }
        }

        void CreatePlaceholder()
        {
            // Create a new placeholder card and insert it at the index of this card.
            m_PlaceholderCard = Instantiate(gameObject, transform.parent);
            m_PlaceholderCard.transform.SetSiblingIndex(m_StartIndex);
            m_PlaceholderCard.GetComponent<LayoutElement>().ignoreLayout = false;

            // Remove visual components from the placeholder card.
            // We only need the RectTransform and LayoutElement to make it take up space in the layout.
            foreach (Transform child in m_PlaceholderCard.transform)
            {
                Destroy(child.gameObject);
            }
        }

        /// <summary>
        /// Gets the index of this card based on its position relative to the other cards. This is done by checking the
        /// position of the midpoint of this card against the positions of the other cards, and calculating the index
        /// based on that.
        /// </summary>
        /// <returns></returns>
        int CalculatePlaceholderIndex()
        {
            var layoutGroup = m_RectTransform.parent.GetComponent<HorizontalLayoutGroup>();
            var spacing = layoutGroup.spacing;
            var firstCard = layoutGroup.transform.GetChild(0);
            var firstCardLeft = ((RectTransform)firstCard.transform).anchoredPosition.x;
            var cardWidth = m_RectTransform.sizeDelta.x;
            var index = Mathf.RoundToInt((m_RectTransform.anchoredPosition.x - firstCardLeft + spacing / 2) / (cardWidth + spacing));

            return Mathf.Clamp(index, 0, transform.parent.childCount - 2);
        }

        public bool MoveLeft(int numberOfPositions)
        {
            var index = transform.GetSiblingIndex();
            if (index <= 0)
            {
                return false;
            }

            MoveToIndex(index - numberOfPositions);
            return true;
        }

        public bool MoveRight(int numberOfPositions)
        {
            var index = transform.GetSiblingIndex();
            if (index >= transform.parent.childCount - 1)
            {
                return false;
            }

            MoveToIndex(index + numberOfPositions);
            return true;
        }

        void MoveToIndex(int index)
        {
            var oldIndex = transform.GetSiblingIndex();

            LayoutRebuilder.MarkLayoutForRebuild(m_RectTransform);
            transform.SetSiblingIndex(index);

            Gameplay.instance.ReorderLetter(oldIndex, index);
        }
    }
}
