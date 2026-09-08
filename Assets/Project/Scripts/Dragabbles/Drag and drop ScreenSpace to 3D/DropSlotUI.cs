using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using TMPro;

namespace DeterminingMassofaBodyUsingMeterscale
{
    [RequireComponent(typeof(RectTransform))]
    public class DropSlotUI : MonoBehaviour, IDropHandler
    {
        [Header("Correct Draggable ID")]
        [SerializeField] private string correctItemID;

        [Header("Resize Settings")]
        [SerializeField] private bool resizeOnDrop = true;
        [SerializeField] private Vector2 targetSize = new Vector2(200f, 80f);
        [SerializeField] private float targetFontSize = 36f;

        [Header("Events")]
        [SerializeField] private UnityEvent onCorrectDrop;

        public bool IsOccupied { get; private set; }

        public void OnDrop(PointerEventData eventData)
        {
            if (IsOccupied || eventData.pointerDrag == null)
                return;

            DraggableScreenSpace draggable =
                eventData.pointerDrag.GetComponent<DraggableScreenSpace>();

            if (draggable == null)
                return;

            if (!string.Equals(draggable.ItemID, correctItemID))
                return;

            IsOccupied = true;

            RectTransform draggableRect =
                draggable.GetComponent<RectTransform>();

            // Snap the draggable to this drop slot.
            draggable.MarkDroppedCorrectly(transform as RectTransform);

            // Force Z = 0.
            Vector3 localPos = draggableRect.localPosition;
            localPos.z = 0f;
            draggableRect.localPosition = localPos;

            // Resize if enabled.
            if (resizeOnDrop)
            {
                draggableRect.sizeDelta = targetSize;

                TMP_Text tmp =
                    draggable.GetComponentInChildren<TMP_Text>();

                if (tmp != null)
                {
                    tmp.enableAutoSizing = false;
                    tmp.fontSize = targetFontSize;
                }
            }

            // Trigger event only after the correct item has snapped.
            onCorrectDrop?.Invoke();
        }

        public void ResetSlot()
        {
            IsOccupied = false;
        }
    }
}