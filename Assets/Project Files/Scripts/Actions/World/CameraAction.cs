using System;
using UnityEngine;
using DG.Tweening;
using TutorialFramework.Core;
using TutorialFramework.RuntimeComponents;

namespace TutorialFramework.Actions
{
    [Serializable]
    public class CameraAction : TutorialAction
    {
        [Header("Dynamic Object Follow")]
        [Tooltip("Tracking mode: FollowPositionOnly (moves with object without altering rotation) or None (static).")]
        public CameraTrackingMode trackingMode = CameraTrackingMode.FollowPositionOnly;

        [Tooltip("Object ID in TutorialObjectRegistry to follow.")]
        public string targetObjectId;

        [Tooltip("Offset to reach the visual center of the object (e.g. Y + 0.25 to focus on needle tip / lens center instead of bottom pivot).")]
        public Vector3 targetCenterOffset = new Vector3(0, 0.25f, 0);

        [Tooltip("If true, automatically calculates relative distance/offset from the object's visual center.")]
        public bool autoCalculateOffset = true;

        [Tooltip("Optional custom offset if autoCalculateOffset is unchecked.")]
        public Vector3 customFollowOffset = Vector3.zero;

        [Tooltip("Smoothing speed for follow movement (e.g., 5.0).")]
        public float trackingSmoothSpeed = 5.0f;

        [Header("Zoom Factor / Field Of View")]
        [Tooltip("Camera Field Of View (e.g., 60 = normal overview, 35 = zoomed in on needle, 20 = close-up).")]
        [Range(10f, 90f)]
        public float fieldOfView = 50f;

        [Header("Rotation & Axis Constraints")]
        [Tooltip("Freezes the Y rotation (yaw) of the camera to its initial value.")]
        public bool freezeYRotation = true;

        [Tooltip("Freezes the X rotation (pitch) of the camera to its initial value.")]
        public bool freezeXRotation = false;

        [Tooltip("Freezes the Z rotation (roll) of the camera to its initial value.")]
        public bool freezeZRotation = false;

        [Tooltip("Freezes the vertical Y position so the camera stays level.")]
        public bool freezeYPosition = false;

        [Header("Initial Camera Placement")]
        [Tooltip("Optional: Camera Anchor ID in scene to match initial placement.")]
        public string cameraAnchorId;

        public Vector3 targetPosition;
        public Vector3 targetRotation;

        [Header("Transition Settings")]
        public float duration = 1.2f;
        public Ease easeType = Ease.InOutCubic;

        public override void Execute(TutorialContext context, Action onComplete)
        {
            if (context.Camera == null)
            {
                onComplete?.Invoke();
                return;
            }

            ResolveInitialPlacement(out Vector3 destPos, out Vector3 destRot, out float destFov);

            context.Camera.TransitionTo(destPos, destRot, destFov, duration, easeType, () =>
            {
                SetupTracking(context);
                onComplete?.Invoke();
            });

            if (!WaitForCompletion)
            {
                SetupTracking(context);
                onComplete?.Invoke();
            }
        }

        private void SetupTracking(TutorialContext context)
        {
            if (trackingMode == CameraTrackingMode.None || string.IsNullOrEmpty(targetObjectId))
            {
                context.Camera.StopTracking();
                return;
            }

            var target = TutorialObjectRegistry.Get(targetObjectId);
            if (target != null)
            {
                context.Camera.StartTracking(
                    target.transform, 
                    trackingMode, 
                    targetCenterOffset,
                    customFollowOffset, 
                    autoCalculateOffset, 
                    trackingSmoothSpeed,
                    freezeYRotation,
                    freezeXRotation,
                    freezeZRotation,
                    freezeYPosition,
                    fieldOfView
                );
            }
        }

        public override void ResetAction(TutorialContext context)
        {
            if (context.Camera != null)
            {
                context.Camera.KillActiveTween();
                context.Camera.StopTracking();
            }
        }

        public override void FastForward(TutorialContext context)
        {
            if (context.Camera != null)
            {
                ResolveInitialPlacement(out Vector3 destPos, out Vector3 destRot, out float destFov);
                context.Camera.SetInstant(destPos, destRot, destFov);
                SetupTracking(context);
            }
        }

        private void ResolveInitialPlacement(out Vector3 pos, out Vector3 rot, out float fov)
        {
            pos = targetPosition;
            rot = targetRotation;
            fov = fieldOfView > 0 ? fieldOfView : 50f;

            if (!string.IsNullOrEmpty(cameraAnchorId))
            {
                var anchor = TutorialObjectRegistry.Get(cameraAnchorId);
                if (anchor != null)
                {
                    pos = anchor.transform.position;
                    rot = anchor.transform.eulerAngles;

                    var camComp = anchor.GetComponent<Camera>();
                    if (camComp != null)
                    {
                        fov = camComp.fieldOfView;
                    }
                }
            }
        }
    }
}
