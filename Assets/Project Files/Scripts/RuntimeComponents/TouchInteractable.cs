using System;
using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace TutorialFramework.RuntimeComponents
{
    [DisallowMultipleComponent]
    public class TouchInteractable : MonoBehaviour, IPointerClickHandler
    {
        private Action onTouched;
        private bool isInteractable;
        private Camera mainCamera;

        public void EnableTouch(Action callback)
        {
            onTouched = callback;
            isInteractable = true;
            mainCamera = Camera.main;

            // Ensure collider exists
            var col3D = GetComponentInChildren<Collider>();
            var col2D = GetComponentInChildren<Collider2D>();

            if (col3D == null && col2D == null)
            {
                var rt = GetComponent<RectTransform>();
                if (rt != null)
                {
                    // UI Object -> Ensure graphic raycaster or box collider
                    var img = GetComponent<UnityEngine.UI.Graphic>();
                    if (img != null) img.raycastTarget = true;
                }
                else
                {
                    Debug.Log($"[TouchInteractable] Added BoxCollider to '{gameObject.name}' for click detection.");
                    gameObject.AddComponent<BoxCollider>();
                }
            }

            if (mainCamera != null && mainCamera.GetComponent<PhysicsRaycaster>() == null)
            {
                mainCamera.gameObject.AddComponent<PhysicsRaycaster>();
            }

            Debug.Log($"[TouchInteractable] Ready for click on '{gameObject.name}'");
        }

        public void DisableTouch()
        {
            isInteractable = false;
            onTouched = null;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!isInteractable) return;
            Debug.Log($"[TouchInteractable] OnPointerClick on '{gameObject.name}'");
            TriggerTouch();
        }

        private void OnMouseDown()
        {
            if (!isInteractable) return;
            Debug.Log($"[TouchInteractable] OnMouseDown on '{gameObject.name}'");
            TriggerTouch();
        }

        private void Update()
        {
            if (!isInteractable) return;
            if (mainCamera == null) mainCamera = Camera.main;
            if (mainCamera == null) return;

            bool clicked = false;
            Vector2 screenPos = Vector2.zero;

#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                clicked = true;
                screenPos = Mouse.current.position.ReadValue();
            }
            else if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            {
                clicked = true;
                screenPos = Touchscreen.current.primaryTouch.position.ReadValue();
            }
#else
            if (Input.GetMouseButtonDown(0))
            {
                clicked = true;
                screenPos = Input.mousePosition;
            }
#endif

            if (clicked)
            {
                Ray ray = mainCamera.ScreenPointToRay(screenPos);

                // 1. Check 3D Colliders (RaycastAll to ignore front obstructions)
                var hits3D = Physics.RaycastAll(ray, 100f);
                foreach (var hit in hits3D)
                {
                    if (hit.transform == transform || hit.transform.IsChildOf(transform))
                    {
                        Debug.Log($"[TouchInteractable] 3D Raycast Hit on '{gameObject.name}'");
                        TriggerTouch();
                        return;
                    }
                }

                // 2. Check 2D Colliders (Sprites)
                var hit2D = Physics2D.GetRayIntersection(ray);
                if (hit2D.collider != null)
                {
                    if (hit2D.transform == transform || hit2D.transform.IsChildOf(transform))
                    {
                        Debug.Log($"[TouchInteractable] 2D Raycast Hit on '{gameObject.name}'");
                        TriggerTouch();
                        return;
                    }
                }
            }
        }

        private void TriggerTouch()
        {
            if (!isInteractable) return;
            isInteractable = false;
            var cb = onTouched;
            onTouched = null;
            cb?.Invoke();
        }
    }
}
