using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TutorialFramework.Core;
using TutorialFramework.RuntimeComponents;

namespace TutorialFramework.Actions
{
    [Serializable]
    public struct SecondaryMoveTarget
    {
        [Tooltip("Object ID in TutorialObjectRegistry to move.")]
        public string targetObjectId;

        [Tooltip("Target local position to move towards (e.g. 0,0,0 for center).")]
        public Vector3 targetLocalPosition;

        [Tooltip("Optional destination object ID to match position.")]
        public string destinationObjectId;

        public float duration;
        public Ease easeType;
    }

    [Serializable]
    public class TouchAction : TutorialAction
    {
        [Header("Click / Touch Trigger Target")]
        [Tooltip("Object ID of the Button, Screw, or Interactable clicked.")]
        public string targetObjectId;

        [Header("Primary Movement (Optional: moves the clicked object itself)")]
        public string destinationObjectId;
        public Vector3 targetPosition = Vector3.zero;
        public Vector3 moveOffset = Vector3.zero;
        public float moveDuration = 0.5f;
        public Ease easeType = Ease.OutQuad;
        public bool isLocalMovement = false;
        public bool matchDestinationRotation = false;

        [Header("Secondary Objects To Move (e.g. Centering Needles on Click)")]
        [Tooltip("List of other objects (like Top & Bottom needles) to animate to center when this button is clicked.")]
        public List<SecondaryMoveTarget> secondaryObjectsToMove = new List<SecondaryMoveTarget>();

        [Header("Object Toggles on Touch (Immediately when clicked)")]
        public List<string> objectsToEnableOnTouch = new List<string>();
        public List<string> objectsToDisableOnTouch = new List<string>();

        [Header("Object Toggles on Complete (After movement finishes)")]
        public List<string> objectsToEnableOnComplete = new List<string>();
        public List<string> objectsToDisableOnComplete = new List<string>();

        [Header("UI Instructions")]
        public string instructionText;

        public override void Execute(TutorialContext context, Action onComplete)
        {
            if (!string.IsNullOrEmpty(instructionText) && context.UI != null)
            {
                context.UI.SetActionInstruction(instructionText, ActionInstructionType.Touch);
            }

            var target = TutorialObjectRegistry.Get(targetObjectId);
            if (target == null)
            {
                Debug.LogWarning($"[TouchAction] Target object '{targetObjectId}' not found in registry!");
                onComplete?.Invoke();
                return;
            }

            target.transform.DOKill();
            var lingeringDrag = target.GetComponent<DraggableObject>();
            if (lingeringDrag != null)
            {
                lingeringDrag.Teardown();
            }

            Debug.Log($"[TouchAction] Slide started -> Waiting for click on '{targetObjectId}' (GameObject: '{target.name}')");

            var uiBtn = target.GetComponent<Button>();
            if (uiBtn != null)
            {
                uiBtn.onClick.AddListener(() =>
                {
                    uiBtn.onClick.RemoveAllListeners();
                    PerformAction(target, onComplete);
                });
                return;
            }

            var interactable = target.GetComponent<TouchInteractable>();
            if (interactable == null)
            {
                interactable = target.gameObject.AddComponent<TouchInteractable>();
            }

            interactable.EnableTouch(() =>
            {
                PerformAction(target, onComplete);
            });
        }

        private void PerformAction(TutorialObject target, Action onComplete)
        {
            Debug.Log($"[TouchAction] Object '{targetObjectId}' CLICKED! Performing movements.");

            ToggleObjects(objectsToEnableOnTouch, true, "Enabled On Touch");
            ToggleObjects(objectsToDisableOnTouch, false, "Disabled On Touch");

            Sequence mainSeq = DOTween.Sequence();

            bool hasPrimaryMove = false;
            Vector3 endPos = target.transform.position;
            Vector3 endRot = target.transform.eulerAngles;

            if (!string.IsNullOrEmpty(destinationObjectId))
            {
                var destObj = TutorialObjectRegistry.Get(destinationObjectId);
                if (destObj != null)
                {
                    endPos = isLocalMovement ? destObj.transform.localPosition : destObj.transform.position;
                    endRot = isLocalMovement ? destObj.transform.localEulerAngles : destObj.transform.eulerAngles;
                    hasPrimaryMove = true;
                }
            }
            else if (targetPosition != Vector3.zero)
            {
                endPos = targetPosition;
                hasPrimaryMove = true;
            }
            else if (moveOffset != Vector3.zero)
            {
                endPos = isLocalMovement 
                    ? target.transform.localPosition + moveOffset 
                    : target.transform.position + moveOffset;
                hasPrimaryMove = true;
            }

            if (hasPrimaryMove && moveDuration > 0)
            {
                if (isLocalMovement)
                {
                    mainSeq.Join(target.transform.DOLocalMove(endPos, moveDuration).SetEase(easeType));
                    if (matchDestinationRotation)
                        mainSeq.Join(target.transform.DOLocalRotate(endRot, moveDuration).SetEase(easeType));
                }
                else
                {
                    mainSeq.Join(target.transform.DOMove(endPos, moveDuration).SetEase(easeType));
                    if (matchDestinationRotation)
                        mainSeq.Join(target.transform.DORotate(endRot, moveDuration).SetEase(easeType));
                }
            }

            if (secondaryObjectsToMove != null && secondaryObjectsToMove.Count > 0)
            {
                foreach (var sec in secondaryObjectsToMove)
                {
                    var secObj = TutorialObjectRegistry.Get(sec.targetObjectId);
                    if (secObj == null)
                    {
                        Debug.LogWarning($"[TouchAction] Secondary target object '{sec.targetObjectId}' not found in registry!");
                        continue;
                    }

                    Vector3 targetPos = sec.targetLocalPosition;
                    if (!string.IsNullOrEmpty(sec.destinationObjectId))
                    {
                        var dest = TutorialObjectRegistry.Get(sec.destinationObjectId);
                        if (dest != null) targetPos = dest.transform.localPosition;
                    }

                    float dur = sec.duration > 0 ? sec.duration : 0.8f;
                    Ease ease = sec.easeType != Ease.Unset ? sec.easeType : Ease.OutQuad;

                    var rt = secObj.GetComponent<RectTransform>();
                    if (rt != null)
                    {
                        mainSeq.Join(rt.DOAnchorPos(targetPos, dur).SetEase(ease));
                    }
                    else
                    {
                        mainSeq.Join(secObj.transform.DOLocalMove(targetPos, dur).SetEase(ease));
                    }
                }
            }

            if (mainSeq.Duration() > 0)
            {
                mainSeq.OnComplete(() =>
                {
                    ToggleObjects(objectsToEnableOnTouch, false, "Hide Temporary Touch Hints");
                    ToggleObjects(objectsToEnableOnComplete, true, "Enabled On Complete");
                    ToggleObjects(objectsToDisableOnComplete, false, "Disabled On Complete");
                    onComplete?.Invoke();
                });
            }
            else
            {
                ToggleObjects(objectsToEnableOnTouch, false, "Hide Temporary Touch Hints");
                ToggleObjects(objectsToEnableOnComplete, true, "Enabled On Complete");
                ToggleObjects(objectsToDisableOnComplete, false, "Disabled On Complete");
                onComplete?.Invoke();
            }
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
            var target = TutorialObjectRegistry.Get(targetObjectId);
            if (target != null)
            {
                var interactable = target.GetComponent<TouchInteractable>();
                if (interactable != null) interactable.DisableTouch();
            }

            // Clean up temporary touch hints
            ToggleObjects(objectsToEnableOnTouch, false, "Cleanup Temporary Touch Hints");
        }

        public override void FastForward(TutorialContext context)
        {
            ResetAction(context);

            // Maintain only completed objects
            ToggleObjects(objectsToEnableOnComplete, true, "Completed FastForward Enable");
            ToggleObjects(objectsToDisableOnComplete, false, "Completed FastForward Disable");
        }
    }
}
