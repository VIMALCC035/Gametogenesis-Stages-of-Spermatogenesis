using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;

[RequireComponent(typeof(RectTransform))]
public class WorldSpaceDropTarget : MonoBehaviour
{
    [Header("Identification")]
    [SerializeField] private string acceptedItemID;

    [Header("World Space Canvas")]
    [SerializeField] private Canvas worldSpaceCanvas;

    [Header("Drop Settings")]
    [SerializeField] private bool requirePointerInsideTarget = true;

    [Header("Events")]
    [SerializeField] private UnityEvent onCorrectDrop;
    [SerializeField] private UnityEvent onIncorrectDrop;

    private RectTransform rectTransform;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();

        if (worldSpaceCanvas == null)
        {
            worldSpaceCanvas =
                GetComponentInParent<Canvas>();
        }

        if (worldSpaceCanvas == null)
        {
            Debug.LogError(
                $"{nameof(WorldSpaceDropTarget)}: No World Space Canvas found.",
                this);
        }
    }

    public bool TryDrop(
        UIDragItem item,
        PointerEventData eventData)
    {
        if (item == null)
            return false;

        if (worldSpaceCanvas == null)
            return false;

        if (!IsCorrectItem(item))
        {
            onIncorrectDrop?.Invoke();
            return false;
        }

        if (requirePointerInsideTarget &&
            !IsPointerInsideTarget(eventData.position))
        {
            onIncorrectDrop?.Invoke();
            return false;
        }

        onCorrectDrop?.Invoke();
        return true;
    }

    private bool IsCorrectItem(UIDragItem item)
    {
        if (string.IsNullOrEmpty(acceptedItemID))
            return true;

        return item.ItemID == acceptedItemID;
    }

    private bool IsPointerInsideTarget(Vector2 screenPosition)
    {
        Camera targetCamera =
            worldSpaceCanvas.worldCamera != null
                ? worldSpaceCanvas.worldCamera
                : Camera.main;

        return RectTransformUtility.RectangleContainsScreenPoint(
            rectTransform,
            screenPosition,
            targetCamera);
    }
}