using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;



namespace DeterminingMassofaBodyUsingMeterscale
{
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(CanvasGroup))]
    public class DraggableScreenSpace : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Header("Must match DropSlotUI correctItemID")]
        [SerializeField] private string itemID;

        [Header("Return Settings")]
        [SerializeField] private float returnDuration = 0.25f;

        public string ItemID => itemID;

        private RectTransform rectTransform;
        private CanvasGroup canvasGroup;
        private Canvas canvas;

        private Vector2 startAnchoredPosition;
        private Vector2 pointerOffset;

        private Coroutine returnCoroutine;
        private bool droppedCorrectly;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            canvasGroup = GetComponent<CanvasGroup>();
            canvas = GetComponentInParent<Canvas>();

            if (canvas == null)
            {
                Debug.LogError("DraggableScreenSpace must be inside a Canvas.");
                enabled = false;
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            droppedCorrectly = false;
            startAnchoredPosition = rectTransform.anchoredPosition;

            canvasGroup.blocksRaycasts = false;

            if (returnCoroutine != null)
                StopCoroutine(returnCoroutine);

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rectTransform.parent as RectTransform,
                eventData.position,
                canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
                out Vector2 localPoint);

            pointerOffset = localPoint - rectTransform.anchoredPosition;
        }

        public void OnDrag(PointerEventData eventData)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rectTransform.parent as RectTransform,
                eventData.position,
                canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
                out Vector2 localPoint);

            rectTransform.anchoredPosition = localPoint - pointerOffset;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            canvasGroup.blocksRaycasts = true;

            if (!droppedCorrectly)
                returnCoroutine = StartCoroutine(SmoothReturn());
        }

        public void MarkDroppedCorrectly(RectTransform targetSlot)
        {
            droppedCorrectly = true;

            transform.SetParent(targetSlot, false);

            rectTransform.anchoredPosition = Vector2.zero;

            Vector3 localPos = rectTransform.localPosition;
            localPos.z = 0f;                 // ✅ Force Z = 0
            rectTransform.localPosition = localPos;

            rectTransform.localScale = Vector3.one;

            canvasGroup.blocksRaycasts = false;
            enabled = false;
        }

        private IEnumerator SmoothReturn()
        {
            Vector2 start = rectTransform.anchoredPosition;
            float time = 0f;

            while (time < returnDuration)
            {
                time += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, time / returnDuration);

                rectTransform.anchoredPosition =
                    Vector2.Lerp(start, startAnchoredPosition, t);

                yield return null;
            }

            rectTransform.anchoredPosition = startAnchoredPosition;
        }
    }
}