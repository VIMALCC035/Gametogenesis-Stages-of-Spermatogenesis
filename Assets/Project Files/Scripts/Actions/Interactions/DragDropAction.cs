using System;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using TutorialFramework.Core;
using TutorialFramework.RuntimeComponents;

namespace TutorialFramework.Actions
{
    [Serializable]
    public class DragDropAction : TutorialAction
    {
        [Header("Draggable Object & Target Measurement")]
        [Tooltip("Object ID of the 3D stand on the bench to drag.")]
        public string draggableObjectId;

        [Tooltip("Object ID of the correct target position / point.")]
        public string validDropTargetId;

        [Tooltip("Allowed measurement tolerance range.")]
        public float allowedDropRadius = 0.1f;

        public bool snapToTarget = true;
        public float snapDuration = 0.2f;

        [Header("Coupled Render Texture Needle Movement")]
        [Tooltip("Optional: ID of the Needle Sprite inside your Render Texture to move synchronously with this 3D stand!")]
        public string coupledNeedleObjectId;

        [Tooltip("Multiplier for needle sprite movement speed (default 1.0).")]
        public float coupledMovementMultiplier = 1.0f;

        [Header("Gentle Sliding & Measurement Behaviour")]
        [Tooltip("If FALSE (Recommended), the object stays where the user leaves it so they can fine-tune point values.")]
        public bool resetToStartOnInvalid = false;

        [Range(0.05f, 1f)]
        public float dragSensitivity = 0.85f;

        [Header("Movement Axis & Bounds Clamping")]
        public DragAxisConstraint dragAxis = DragAxisConstraint.X_Only;
        public bool useClamp = true;
        public DragClampMode clampMode = DragClampMode.Absolute_World;
        public float clampMin = -0.6f;
        public float clampMax = -0.276f;

        [Header("Objects To Toggle On Drag Click / Start")]
        [Tooltip("Object IDs to ACTIVATE immediately when the user clicks/touches the draggable object.")]
        public List<string> objectsToEnableOnDragStart = new List<string>();

        [Tooltip("Object IDs to DEACTIVATE immediately when the user clicks/touches the draggable object.")]
        public List<string> objectsToDisableOnDragStart = new List<string>();

        [Header("Objects To Toggle On Correct Drop")]
        [Tooltip("Object IDs to ACTIVATE when dropped correctly at the answer position.")]
        public List<string> objectsToEnableOnSuccess = new List<string>();

        [Tooltip("Object IDs to DEACTIVATE when dropped correctly (e.g. hiding target slot highlight).")]
        public List<string> objectsToDisableOnSuccess = new List<string>();

        [Header("UI Instructions")]
        public string instructionText;

        public override void Execute(TutorialContext context, Action onComplete)
        {
            if (!string.IsNullOrEmpty(instructionText) && context.UI != null)
            {
                context.UI.SetActionInstruction(instructionText, ActionInstructionType.Drag);
            }

            var draggableObj = TutorialObjectRegistry.Get(draggableObjectId);
            var dropTarget = TutorialObjectRegistry.GetComponent<DropTarget>(validDropTargetId);

            if (draggableObj == null || dropTarget == null)
            {
                Debug.LogError($"[DragDropAction] Missing Draggable ('{draggableObjectId}') or DropTarget ('{validDropTargetId}') in registry!");
                onComplete?.Invoke();
                return;
            }

            draggableObj.transform.DOKill();

            var lingeringTouch = draggableObj.GetComponent<TouchInteractable>();
            if (lingeringTouch != null)
            {
                lingeringTouch.DisableTouch();
            }

            Transform coupledTransform = null;
            if (!string.IsNullOrEmpty(coupledNeedleObjectId))
            {
                var coupledObj = TutorialObjectRegistry.Get(coupledNeedleObjectId);
                if (coupledObj != null)
                {
                    coupledTransform = coupledObj.transform;
                }
            }

            var draggable = draggableObj.GetComponent<DraggableObject>();
            if (draggable == null)
            {
                draggable = draggableObj.gameObject.AddComponent<DraggableObject>();
            }

            draggable.Setup(
                dropTarget, 
                allowedDropRadius, 
                snapToTarget, 
                snapDuration, 
                dragAxis, 
                useClamp, 
                clampMode,
                clampMin, 
                clampMax, 
                resetToStartOnInvalid,
                dragSensitivity,
                coupledTransform,
                coupledMovementMultiplier,
                () =>
                {
                    Debug.Log($"[DragDropAction] Drag started on '{draggableObjectId}'!");
                    ToggleObjects(objectsToEnableOnDragStart, true, "Enabled On Drag Start");
                    ToggleObjects(objectsToDisableOnDragStart, false, "Disabled On Drag Start");
                },
                () =>
                {
                    Debug.Log($"[DragDropAction] Object '{draggableObjectId}' reached correct target '{validDropTargetId}'!");

                    // Hide temporary drag hints
                    ToggleObjects(objectsToEnableOnDragStart, false, "Hide Temporary Drag Hints");
                    ToggleObjects(objectsToEnableOnSuccess, true, "Enabled On Success");
                    ToggleObjects(objectsToDisableOnSuccess, false, "Disabled On Success");

                    onComplete?.Invoke();
                }
            );
        }

        private void ToggleObjects(List<string> objectIds, bool activeState, string logContext)
        {
            if (objectIds == null) return;
            foreach (var id in objectIds)
            {
                if (string.IsNullOrWhiteSpace(id)) continue;
                var obj = TutorialObjectRegistry.Get(id);
                if (obj != null)
                {
                    obj.gameObject.SetActive(activeState);
                }
            }
        }

        public override void ResetAction(TutorialContext context)
        {
            var draggable = TutorialObjectRegistry.GetComponent<DraggableObject>(draggableObjectId);
            if (draggable != null)
            {
                draggable.Teardown();
            }

            // Clean up temporary drag start hints
            ToggleObjects(objectsToEnableOnDragStart, false, "Cleanup Drag Start Hints");
        }

        public override void FastForward(TutorialContext context)
        {
            ResetAction(context);

            // Maintain final success state only
            ToggleObjects(objectsToEnableOnSuccess, true, "FastForward Success Enable");
            ToggleObjects(objectsToDisableOnSuccess, false, "FastForward Success Disable");
        }
    }
}
