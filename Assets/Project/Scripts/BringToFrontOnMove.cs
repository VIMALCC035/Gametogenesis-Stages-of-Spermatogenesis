using UnityEngine;
using UnityEngine.EventSystems;

namespace  DeterminingMassofaBodyUsingMeterscale
{
    [RequireComponent(typeof(RectTransform))]
    public class BringToFrontOnMove : MonoBehaviour, IBeginDragHandler, IDragHandler
    {
        private RectTransform rectTransform;
        private Transform parentTransform;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            parentTransform = transform.parent;

            if (parentTransform == null)
            {
                Debug.LogWarning($"{nameof(BringToFrontOnMove)} requires a parent to function.", this);
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            BringToFront();
        }

        public void OnDrag(PointerEventData eventData)
        {
            BringToFront();
        }

        private void BringToFront()
        {
            if (parentTransform == null)
                return;

            if (transform.GetSiblingIndex() != parentTransform.childCount - 1)
            {
                transform.SetAsLastSibling();
            }
        }
    }
}