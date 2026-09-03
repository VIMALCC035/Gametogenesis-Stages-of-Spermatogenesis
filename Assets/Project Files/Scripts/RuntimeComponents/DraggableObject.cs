using System;
using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace TutorialFramework.RuntimeComponents
{
    public enum DragAxisConstraint
    {
        Free,
        X_Only, // Constrain movement along bench X axis
        Z_Only, // Constrain movement along bench Z axis
        Y_Only
    }

    public enum DragClampMode
    {
        Absolute_World,   // Exact world coordinates (e.g. -0.6 to -0.276)
        Absolute_Local,   // Exact local coordinates relative to parent
        Relative_To_Start // Offset from start position (e.g. -0.3 to +0.3)
    }

    [DisallowMultipleComponent]
    public class DraggableObject : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        private DropTarget target;
        private float snapRadius;
        private bool snapToTarget;
        private float snapDuration;
        private DragAxisConstraint axisConstraint = DragAxisConstraint.Free;
        private bool useClamp;
        private DragClampMode clampMode = DragClampMode.Absolute_World;
        private float minClamp;
        private float maxClamp;
        private bool resetToStartOnInvalid;
        private float dragSensitivity;
        private Action onDragStart;
        private Action onComplete;

        // Coupled Object (e.g. Needle Sprite in Render Texture)
        private Transform coupledTransform;
        private Vector3 coupledInitialLocalPos;
        private float coupledMultiplier = 1.0f;

        private Vector3 startWorldPos;
        private Vector3 startLocalPos;
        private Camera mainCamera;
        private bool isDragging;
        private Vector3 dragPlaneOffset;
        private Plane dragPlane;
        private Tween activeTween;
        private bool hasMovedDuringDrag;

        public void Setup(
            DropTarget validTarget, 
            float radius, 
            bool snap, 
            float duration, 
            DragAxisConstraint axis, 
            bool enableClamp,
            DragClampMode mode,
            float clampMin,
            float clampMax,
            bool resetOnInvalid,
            float sensitivity,
            Transform coupledObject,
            float multiplier,
            Action startCallback,
            Action completeCallback)
        {
            target = validTarget;
            snapRadius = radius;
            snapToTarget = snap;
            snapDuration = duration;
            axisConstraint = axis;
            useClamp = enableClamp;
            clampMode = mode;
            minClamp = Mathf.Min(clampMin, clampMax);
            maxClamp = Mathf.Max(clampMin, clampMax);
            resetToStartOnInvalid = resetOnInvalid;
            dragSensitivity = sensitivity > 0 ? sensitivity : 0.85f;
            coupledTransform = coupledObject;
            coupledMultiplier = multiplier != 0 ? multiplier : 1.0f;
            onDragStart = startCallback;
            onComplete = completeCallback;
            startWorldPos = transform.position;
            startLocalPos = transform.localPosition;
            mainCamera = Camera.main;
            enabled = true;
            hasMovedDuringDrag = false;

            Debug.Log($"[DraggableObject] Initialized on '{gameObject.name}' -> StartWorldPos: {startWorldPos}, Clamp: [{minClamp}, {maxClamp}] (Mode: {clampMode})");

            if (coupledTransform != null)
            {
                coupledInitialLocalPos = coupledTransform.localPosition;
            }

            var col = GetComponentInChildren<Collider>();
            if (col == null && GetComponentInChildren<Collider2D>() == null)
            {
                gameObject.AddComponent<BoxCollider>();
            }

            if (mainCamera != null && mainCamera.GetComponent<PhysicsRaycaster>() == null)
            {
                mainCamera.gameObject.AddComponent<PhysicsRaycaster>();
            }
        }

        public void Teardown()
        {
            enabled = false;
            isDragging = false;
            onDragStart = null;
            onComplete = null;
            if (activeTween != null && activeTween.IsActive())
            {
                activeTween.Kill();
                activeTween = null;
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!enabled) return;
            BeginDrag(eventData.position);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!isDragging) return;
            UpdateDrag(eventData.position);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!isDragging) return;
            EndDrag();
        }

        private void Update()
        {
            if (!enabled || mainCamera == null) return;

            Vector2 screenPos = Vector2.zero;
            bool isDown = false;
            bool isHeld = false;
            bool isUp = false;

#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null)
            {
                screenPos = Mouse.current.position.ReadValue();
                isDown = Mouse.current.leftButton.wasPressedThisFrame;
                isHeld = Mouse.current.leftButton.isPressed;
                isUp = Mouse.current.leftButton.wasReleasedThisFrame;
            }
            else if (Touchscreen.current != null)
            {
                screenPos = Touchscreen.current.primaryTouch.position.ReadValue();
                isDown = Touchscreen.current.primaryTouch.press.wasPressedThisFrame;
                isHeld = Touchscreen.current.primaryTouch.press.isPressed;
                isUp = Touchscreen.current.primaryTouch.press.wasReleasedThisFrame;
            }
#else
            screenPos = Input.mousePosition;
            isDown = Input.GetMouseButtonDown(0);
            isHeld = Input.GetMouseButton(0);
            isUp = Input.GetMouseButtonUp(0);
#endif

            if (isDown && !isDragging)
            {
                Ray ray = mainCamera.ScreenPointToRay(screenPos);
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    if (hit.transform == transform || hit.transform.IsChildOf(transform))
                    {
                        BeginDrag(screenPos);
                    }
                }
            }
            else if (isHeld && isDragging)
            {
                UpdateDrag(screenPos);
            }
            else if (isUp && isDragging)
            {
                EndDrag();
            }
        }

        private void BeginDrag(Vector2 screenPosition)
        {
            if (mainCamera == null) mainCamera = Camera.main;
            if (mainCamera == null) return;

            if (activeTween != null && activeTween.IsActive())
            {
                activeTween.Kill();
            }

            Vector3 planeNormal = (axisConstraint == DragAxisConstraint.X_Only || axisConstraint == DragAxisConstraint.Z_Only) 
                ? Vector3.up 
                : -mainCamera.transform.forward;

            dragPlane = new Plane(planeNormal, transform.position);

            Ray ray = mainCamera.ScreenPointToRay(screenPosition);
            if (dragPlane.Raycast(ray, out float enter))
            {
                Vector3 hitPoint = ray.GetPoint(enter);
                dragPlaneOffset = transform.position - hitPoint;
            }
            else
            {
                dragPlaneOffset = Vector3.zero;
            }

            isDragging = true;
            hasMovedDuringDrag = false;

            var startCb = onDragStart;
            onDragStart = null;
            startCb?.Invoke();
        }

        private void UpdateDrag(Vector2 screenPosition)
        {
            if (mainCamera == null) return;

            Ray ray = mainCamera.ScreenPointToRay(screenPosition);
            if (dragPlane.Raycast(ray, out float enter))
            {
                Vector3 rawHit = ray.GetPoint(enter) + dragPlaneOffset;
                Vector3 currentPos = transform.position;

                Vector3 targetWorldPos = Vector3.Lerp(currentPos, rawHit, dragSensitivity);

                Vector3 finalPos = currentPos;
                switch (axisConstraint)
                {
                    case DragAxisConstraint.Free:
                        finalPos = targetWorldPos;
                        break;
                    case DragAxisConstraint.X_Only:
                        finalPos.x = ApplyClamp(targetWorldPos.x, startWorldPos.x, startLocalPos.x);
                        break;
                    case DragAxisConstraint.Z_Only:
                        finalPos.z = ApplyClamp(targetWorldPos.z, startWorldPos.z, startLocalPos.z);
                        break;
                    case DragAxisConstraint.Y_Only:
                        finalPos.y = ApplyClamp(targetWorldPos.y, startWorldPos.y, startLocalPos.y);
                        break;
                }

                if (Vector3.Distance(transform.position, finalPos) > 0.0001f)
                {
                    hasMovedDuringDrag = true;
                }

                transform.position = finalPos;

                if (coupledTransform != null)
                {
                    Vector3 delta = finalPos - startWorldPos;
                    coupledTransform.localPosition = coupledInitialLocalPos + (delta * coupledMultiplier);
                }
            }
        }

        private float ApplyClamp(float rawVal, float baseStartWorld, float baseStartLocal)
        {
            if (!useClamp) return rawVal;

            float min = minClamp;
            float max = maxClamp;

            switch (clampMode)
            {
                case DragClampMode.Absolute_World:
                    min = minClamp;
                    max = maxClamp;
                    // Encompass start position so clicking never causes a jump!
                    min = Mathf.Min(min, baseStartWorld);
                    max = Mathf.Max(max, baseStartWorld);
                    break;
                case DragClampMode.Absolute_Local:
                    float worldOffset = baseStartWorld - baseStartLocal;
                    min = minClamp + worldOffset;
                    max = maxClamp + worldOffset;
                    min = Mathf.Min(min, baseStartWorld);
                    max = Mathf.Max(max, baseStartWorld);
                    break;
                case DragClampMode.Relative_To_Start:
                    min = baseStartWorld + minClamp;
                    max = baseStartWorld + maxClamp;
                    break;
            }

            return Mathf.Clamp(rawVal, Mathf.Min(min, max), Mathf.Max(min, max));
        }

        private void EndDrag()
        {
            isDragging = false;

            if (target == null)
            {
                return;
            }

            // Only complete if user actually moved the needle
            if (!hasMovedDuringDrag)
            {
                return;
            }

            float distance = Vector3.Distance(transform.position, target.TargetPosition);
            Debug.Log($"[DraggableObject] EndDrag on '{gameObject.name}' -> Final Pos: {transform.position}, Target: {target.TargetPosition}, Distance: {distance}, SnapRadius: {snapRadius}");

            if (distance <= snapRadius)
            {
                enabled = false;
                if (snapToTarget)
                {
                    activeTween = transform.DOMove(target.TargetPosition, snapDuration).OnComplete(() =>
                    {
                        var cb = onComplete;
                        onComplete = null;
                        cb?.Invoke();
                    });
                }
                else
                {
                    var cb = onComplete;
                    onComplete = null;
                    cb?.Invoke();
                }
            }
            else if (resetToStartOnInvalid)
            {
                activeTween = transform.DOMove(startWorldPos, 0.35f).SetEase(Ease.OutQuad);
                if (coupledTransform != null)
                {
                    coupledTransform.DOLocalMove(coupledInitialLocalPos, 0.35f).SetEase(Ease.OutQuad);
                }
            }
        }
    }
}
