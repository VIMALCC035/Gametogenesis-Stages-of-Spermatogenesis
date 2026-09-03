using System;
using UnityEngine;
using DG.Tweening;

namespace TutorialFramework.RuntimeComponents
{
    public enum CameraTrackingMode
    {
        None,
        FollowPositionOnly, // Moves with object, preserves rotation
        FollowAndLookAt     // Moves and rotates towards object
    }

    public class TutorialCameraController : MonoBehaviour
    {
        [SerializeField] private Camera managedCamera;

        private Sequence activeSequence;
        private Transform trackingTarget;
        private CameraTrackingMode trackingMode = CameraTrackingMode.None;
        private Vector3 followOffset;
        private Vector3 targetCenterOffset;
        private float followSmoothSpeed = 5.0f;
        private bool autoOffset = true;

        // Axis & Rotation Constraints
        private bool freezeXRot = false;
        private bool freezeYRot = true;
        private bool freezeZRot = false;
        private bool freezeYPos = false;
        private Vector3 initialLockedEuler;

        private void Awake()
        {
            if (managedCamera == null)
            {
                managedCamera = Camera.main;
            }
        }

        public void TransitionTo(Vector3 pos, Vector3 rot, float fov, float duration, Ease ease, Action onComplete)
        {
            KillActiveTween();

            if (managedCamera == null)
            {
                onComplete?.Invoke();
                return;
            }

            if (duration <= 0f)
            {
                SetInstant(pos, rot, fov);
                onComplete?.Invoke();
                return;
            }

            activeSequence = DOTween.Sequence();
            activeSequence.Join(managedCamera.transform.DOMove(pos, duration).SetEase(ease));
            activeSequence.Join(managedCamera.transform.DORotate(rot, duration).SetEase(ease));
            if (fov > 0)
            {
                activeSequence.Join(managedCamera.DOFieldOfView(fov, duration).SetEase(ease));
            }
            activeSequence.OnComplete(() => onComplete?.Invoke());
        }

        public void StartTracking(
            Transform target, 
            CameraTrackingMode mode, 
            Vector3 centerOffset,
            Vector3 customOffset, 
            bool useAutoOffset, 
            float smoothSpeed,
            bool freezeYRotation,
            bool freezeXRotation,
            bool freezeZRotation,
            bool freezeYPosition,
            float targetFov)
        {
            trackingTarget = target;
            trackingMode = mode;
            targetCenterOffset = centerOffset;
            followSmoothSpeed = smoothSpeed > 0 ? smoothSpeed : 5.0f;
            autoOffset = useAutoOffset;
            freezeYRot = freezeYRotation;
            freezeXRot = freezeXRotation;
            freezeZRot = freezeZRotation;
            freezeYPos = freezeYPosition;

            if (managedCamera != null && target != null)
            {
                initialLockedEuler = managedCamera.transform.eulerAngles;

                Vector3 targetVisualCenter = target.position + targetCenterOffset;

                if (autoOffset)
                {
                    // Remember exact distance & relative offset from the visual center of the object
                    followOffset = managedCamera.transform.position - targetVisualCenter;
                }
                else
                {
                    followOffset = customOffset;
                }

                // If targetFov is set, smoothly zoom camera to desired FOV
                if (targetFov > 0 && managedCamera.fieldOfView != targetFov)
                {
                    managedCamera.DOFieldOfView(targetFov, 0.6f).SetEase(Ease.OutQuad);
                }
            }
        }

        public void StopTracking()
        {
            trackingTarget = null;
            trackingMode = CameraTrackingMode.None;
        }

        private void LateUpdate()
        {
            if (managedCamera == null || trackingTarget == null || trackingMode == CameraTrackingMode.None) return;

            Vector3 targetVisualCenter = trackingTarget.position + targetCenterOffset;

            switch (trackingMode)
            {
                case CameraTrackingMode.FollowPositionOnly:
                {
                    Vector3 targetPos = targetVisualCenter + followOffset;
                    if (freezeYPos)
                    {
                        targetPos.y = managedCamera.transform.position.y;
                    }

                    managedCamera.transform.position = Vector3.Lerp(managedCamera.transform.position, targetPos, Time.deltaTime * followSmoothSpeed);
                    ApplyRotationConstraints();
                    break;
                }

                case CameraTrackingMode.FollowAndLookAt:
                {
                    Vector3 targetPos = targetVisualCenter + followOffset;
                    if (freezeYPos)
                    {
                        targetPos.y = managedCamera.transform.position.y;
                    }

                    managedCamera.transform.position = Vector3.Lerp(managedCamera.transform.position, targetPos, Time.deltaTime * followSmoothSpeed);

                    Vector3 dir = targetVisualCenter - managedCamera.transform.position;
                    if (dir.sqrMagnitude > 0.001f)
                    {
                        Quaternion targetRot = Quaternion.LookRotation(dir);
                        Vector3 currentEuler = Quaternion.Slerp(managedCamera.transform.rotation, targetRot, Time.deltaTime * followSmoothSpeed).eulerAngles;

                        if (freezeXRot) currentEuler.x = initialLockedEuler.x;
                        if (freezeYRot) currentEuler.y = initialLockedEuler.y;
                        if (freezeZRot) currentEuler.z = initialLockedEuler.z;

                        managedCamera.transform.rotation = Quaternion.Euler(currentEuler);
                    }
                    break;
                }
            }
        }

        private void ApplyRotationConstraints()
        {
            Vector3 currentEuler = managedCamera.transform.eulerAngles;
            if (freezeXRot) currentEuler.x = initialLockedEuler.x;
            if (freezeYRot) currentEuler.y = initialLockedEuler.y;
            if (freezeZRot) currentEuler.z = initialLockedEuler.z;
            managedCamera.transform.eulerAngles = currentEuler;
        }

        public void SetInstant(Vector3 pos, Vector3 rot, float fov)
        {
            KillActiveTween();
            if (managedCamera == null) return;

            managedCamera.transform.position = pos;
            managedCamera.transform.eulerAngles = rot;
            initialLockedEuler = rot;
            if (fov > 0)
            {
                managedCamera.fieldOfView = fov;
            }
        }

        public void KillActiveTween()
        {
            if (activeSequence != null && activeSequence.IsActive())
            {
                activeSequence.Kill();
                activeSequence = null;
            }
        }
    }
}
