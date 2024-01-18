using UnityEngine;
using UnityEngine.Accessibility;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Unity.Samples.LetterSpell
{
    class LetterCardSwiper : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public UnityEvent swipedLeft = new();
        public UnityEvent swipedRight = new();
        public UnityEvent swipedUp = new();
        public UnityEvent swipedDown = new();

        public void OnBeginDrag(PointerEventData eventData)
        {
        }

        public void OnDrag(PointerEventData eventData)
        {
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            // Only allow swiping when the screen reader is on.
            if (!AssistiveSupport.isScreenReaderEnabled)
            {
                return;
            }

            var dragVector = (eventData.position - eventData.pressPosition).normalized;
        
            var positiveX = Mathf.Abs(dragVector.x);
            var positiveY = Mathf.Abs(dragVector.y);

            if (positiveX > positiveY)
            {
                if (dragVector.x > 0)
                {
                    swipedRight.Invoke();
                }
                else
                {
                    swipedLeft.Invoke();
                }
            }  
            else if (dragVector.y > 0)
            {
                swipedUp.Invoke();
            }
            else
            {
                swipedDown.Invoke();
            }
        }
    }
}
