using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Unity.Samples.LetterSpell
{
    public class LetterCard : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public Image backgroundImage;
        public Image selectedImage;

        /// <summary>
        /// Called when the card is dropped.
        /// </summary>
        public Action<int, int> dropped;

        RectTransform m_RectTransform;
        LayoutElement m_LayoutElement;
        BoxCollider2D m_BoxCollider2D;
        Vector3 m_Offset;

        GameObject m_PlaceholderCard;

        bool m_IsDragging;
        int m_StartIndex;

        void Start()
        {
            m_RectTransform = GetComponent<RectTransform>();
            m_LayoutElement = GetComponent<LayoutElement>();
            m_BoxCollider2D = GetComponent<BoxCollider2D>();

            backgroundImage.gameObject.SetActive(true);

            StartCoroutine(DelayedStart());
        }

        // Called when one frame has passed before the coroutine is started, to
        // ensure that the layout has been updated.
        IEnumerator DelayedStart()
        {
            yield return new WaitForEndOfFrame();
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

        public void OnBeginDrag(PointerEventData eventData)
        {
            m_IsDragging = true;
            m_StartIndex = transform.GetSiblingIndex();
            m_Offset = transform.position - (Vector3)eventData.position;
            m_LayoutElement.ignoreLayout = true;

            SetDraggingVisuals(true);

            // Instantiate a placeholder card and add it to the hierarchy.
            var parent = transform.parent;
            m_PlaceholderCard = Instantiate(gameObject, parent);
            m_PlaceholderCard.transform.SetSiblingIndex(transform.GetSiblingIndex());
            m_PlaceholderCard.GetComponent<LayoutElement>().ignoreLayout = false;

            foreach (Transform child in m_PlaceholderCard.transform)
            {
                Destroy(child.gameObject);
            }

            // Move the card last and rotate it.
            transform.SetSiblingIndex(parent.childCount - 1);
            transform.rotation = Quaternion.Euler(0, 0, 15);
        }

        public void OnDrag(PointerEventData eventData)
        {
            transform.position = (Vector3)eventData.position + m_Offset;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            m_IsDragging = false;
            m_LayoutElement.ignoreLayout = false;

            Destroy(m_PlaceholderCard);

            // Set the item dirty.
            LayoutRebuilder.MarkLayoutForRebuild(m_RectTransform);

            var index = GetIndex();

            // Move the card.
            transform.SetSiblingIndex(index);
            transform.rotation = Quaternion.identity;

            SetDraggingVisuals(false);

            dropped?.Invoke(m_StartIndex, index);
        }

        public bool MoveLeft(int numberOfPositions)
        {
            if (transform.GetSiblingIndex() == 0)
            {
                return false;
            }

            MoveToIndex(transform.GetSiblingIndex() - numberOfPositions);
            return true;
        }

        public bool MoveRight(int numberOfPositions)
        {
            if (transform.GetSiblingIndex() == transform.parent.childCount - 1)
            {
                return false;
            }

            MoveToIndex(transform.GetSiblingIndex() + numberOfPositions);
            return true;
        }

        void MoveToIndex(int index)
        {
            var oldIndex = transform.GetSiblingIndex();

            LayoutRebuilder.MarkLayoutForRebuild(m_RectTransform);
            transform.SetSiblingIndex(index);
            dropped?.Invoke(oldIndex, index);
        }

        public void OnTriggerStay2D(Collider2D other)
        {
            // Check if the other bounds contain the midpoint of Collider2D.
            if (other.bounds.Contains(m_BoxCollider2D.bounds.center) && m_IsDragging)
            {
                // Set the item dirty.
                LayoutRebuilder.MarkLayoutForRebuild(m_RectTransform);

                // Move the placeholder card.
                if (m_PlaceholderCard != null)
                {
                    m_PlaceholderCard.transform.SetSiblingIndex(GetIndex());
                }
            }
        }
        
        public void SetDraggingVisuals(bool isDragging)
        {
            backgroundImage.gameObject.SetActive(!isDragging);
            selectedImage.gameObject.SetActive(isDragging);
        }

        int GetIndex()
        {
            var layoutGroup = m_RectTransform.parent.GetComponent<HorizontalLayoutGroup>();
            var spacing = layoutGroup.spacing;
            var firstCard = layoutGroup.transform.GetChild(0);
            var firstCardLeft = ((RectTransform)firstCard.transform).anchoredPosition.x;
            var cardWidth = m_RectTransform.sizeDelta.x;
            var index = Mathf.RoundToInt((m_RectTransform.anchoredPosition.x - firstCardLeft + spacing / 2) / (cardWidth + spacing));

            return Mathf.Clamp(index, 0, transform.parent.childCount - 2);
        }
    }
}
