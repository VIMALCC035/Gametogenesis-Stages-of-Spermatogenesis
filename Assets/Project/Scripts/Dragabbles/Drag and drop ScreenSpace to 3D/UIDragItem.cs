using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

[RequireComponent(typeof(CanvasGroup))]
[RequireComponent(typeof(RectTransform))]
public class UIDragItem : MonoBehaviour,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler
{
    [Header("Identification")]
    [SerializeField] private string itemID;

    [Header("Drag Settings")]
    [SerializeField] private float dragScale = 1.2f;
    [SerializeField] private float scaleLerpSpeed = 12f;
    [SerializeField] private float returnSpeed = 12f;

    [Header("World Space Drop")]
    [SerializeField] private WorldSpaceDropTarget dropTarget;

    [Header("Events")]
    [SerializeField] private UnityEvent onCorrectDrop;
    [SerializeField] private UnityEvent onIncorrectDrop;

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Canvas sourceCanvas;

    private Vector2 originalPosition;
    private Vector3 originalScale;
    private Vector2 pointerOffset;

    private Coroutine moveRoutine;
    private Coroutine scaleRoutine;

    public string ItemID => itemID;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        sourceCanvas = GetComponentInParent<Canvas>();

        if (sourceCanvas == null)
        {
            Debug.LogError(
                $"{nameof(UIDragItem)}: No parent Canvas found.",
                this);

            enabled = false;
            return;
        }

        originalScale = rectTransform.localScale;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        originalPosition = rectTransform.anchoredPosition;

        canvasGroup.blocksRaycasts = false;

        Camera sourceCamera = GetEventCamera(sourceCanvas);

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                sourceCanvas.transform as RectTransform,
                eventData.position,
                sourceCamera,
                out Vector2 localPoint))
        {
            pointerOffset =
                rectTransform.anchoredPosition - localPoint;
        }

        StartScale(originalScale * dragScale);
    }

    public void OnDrag(PointerEventData eventData)
    {
        Camera sourceCamera = GetEventCamera(sourceCanvas);

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                sourceCanvas.transform as RectTransform,
                eventData.position,
                sourceCamera,
                out Vector2 localPoint))
        {
            rectTransform.anchoredPosition =
                localPoint + pointerOffset;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.blocksRaycasts = true;

        bool droppedCorrectly = false;

        if (dropTarget != null)
        {
            droppedCorrectly =
                dropTarget.TryDrop(this, eventData);
        }

        if (droppedCorrectly)
        {
            StartScale(originalScale);
            onCorrectDrop?.Invoke();
            return;
        }

        StartReturn();
        StartScale(originalScale);

        onIncorrectDrop?.Invoke();
    }

    private Camera GetEventCamera(Canvas canvas)
    {
        if (canvas == null)
            return null;

        if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            return null;

        return canvas.worldCamera != null
            ? canvas.worldCamera
            : Camera.main;
    }

    private void StartReturn()
    {
        if (moveRoutine != null)
            StopCoroutine(moveRoutine);

        moveRoutine = StartCoroutine(ReturnRoutine());
    }

    private IEnumerator ReturnRoutine()
    {
        while (Vector2.Distance(
                   rectTransform.anchoredPosition,
                   originalPosition) > 0.01f)
        {
            rectTransform.anchoredPosition =
                Vector2.Lerp(
                    rectTransform.anchoredPosition,
                    originalPosition,
                    Time.deltaTime * returnSpeed);

            yield return null;
        }

        rectTransform.anchoredPosition = originalPosition;
        moveRoutine = null;
    }

    private void StartScale(Vector3 target)
    {
        if (scaleRoutine != null)
            StopCoroutine(scaleRoutine);

        scaleRoutine = StartCoroutine(
            ScaleRoutine(target));
    }

    private IEnumerator ScaleRoutine(Vector3 target)
    {
        while (Vector3.Distance(
                   rectTransform.localScale,
                   target) > 0.001f)
        {
            rectTransform.localScale =
                Vector3.Lerp(
                    rectTransform.localScale,
                    target,
                    Time.deltaTime * scaleLerpSpeed);

            yield return null;
        }

        rectTransform.localScale = target;
        scaleRoutine = null;
    }
}