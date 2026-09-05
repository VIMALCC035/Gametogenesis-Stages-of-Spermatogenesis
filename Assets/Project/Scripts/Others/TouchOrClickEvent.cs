using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using System.Collections.Generic;

[RequireComponent(typeof(Collider))]
public class TouchOrClickEvent : MonoBehaviour
{
    // ========================= BASE EVENT =========================

    [Header("Base Touch Event")]
    public UnityEvent OnTouched;

    // ========================= CONDITIONAL EVENTS =========================

    [System.Serializable]
    public class ConditionalEvent
    {
        [Header("Page Condition")]
        [Tooltip("Event will trigger only when current page index matches this value.")]
        public int requiredPageIndex;

        [Header("Event")]
        public UnityEvent onInvoked;

        [Header("Trigger Settings")]
        public bool allowMultipleTriggers = true;

        [Header("Navigation")]
        [Tooltip("If enabled, navigation will unlock when this specific conditional event is triggered.")]
        public bool unlockNavigation = false;

        [HideInInspector]
        public bool hasTriggered;
    }

    [Header("Invoke When Page Index Matches")]
    public List<ConditionalEvent> conditionalEvents =
        new List<ConditionalEvent>();

    // ========================= SETTINGS =========================

    [Header("References")]
    [SerializeField] private Camera targetCamera;

    [Header("Behavior")]
    [SerializeField] private bool ignoreUI = true;

    // ========================= INTERNAL =========================

    private Collider cachedCollider;

    // ========================= LIFECYCLE =========================

    private void Awake()
    {
        cachedCollider = GetComponent<Collider>();

        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }
    }

    private void OnEnable()
    {
        ResetAllConditionalTriggers();
    }

    private void Update()
    {
        if (targetCamera == null)
            return;

        // -------- MOUSE --------

        if (Mouse.current != null &&
            Mouse.current.leftButton.wasPressedThisFrame)
        {
            ProcessPointer(
                Mouse.current.position.ReadValue());
        }

        // -------- TOUCH --------

        if (Touchscreen.current != null)
        {
            var touch = Touchscreen.current.primaryTouch;

            if (touch.press.wasPressedThisFrame)
            {
                ProcessPointer(
                    touch.position.ReadValue());
            }
        }
    }

    // ========================= INPUT PROCESSING =========================

    private void ProcessPointer(Vector2 screenPosition)
    {
        if (ignoreUI &&
            EventSystem.current != null &&
            EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        Ray ray =
            targetCamera.ScreenPointToRay(screenPosition);

        if (!Physics.Raycast(ray, out RaycastHit hit))
            return;

        if (hit.collider != cachedCollider)
            return;

        InvokeEvents();
    }

    // ========================= EVENT INVOCATION =========================

    private void InvokeEvents()
    {
        // Base touch event.
        OnTouched?.Invoke();

        int currentPage =
            PageNavigationController.CurrentIndex;

        foreach (var entry in conditionalEvents)
        {
            // PAGE INDEX CHECK
            if (entry.requiredPageIndex != currentPage)
                continue;

            // SINGLE TRIGGER CHECK
            if (!entry.allowMultipleTriggers &&
                entry.hasTriggered)
            {
                continue;
            }

            // Mark as triggered.
            entry.hasTriggered = true;

            // Invoke this specific event.
            entry.onInvoked?.Invoke();

            // Unlock navigation only for this specific event.
            if (entry.unlockNavigation)
            {
                PageNavigationController.RequestNavigationUnlock();
            }
        }
    }

    // ========================= PUBLIC API =========================

    public void ResetAllConditionalTriggers()
    {
        foreach (var entry in conditionalEvents)
        {
            entry.hasTriggered = false;
        }
    }
}