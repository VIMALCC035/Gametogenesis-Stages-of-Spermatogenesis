using System;
using UnityEngine;
using DG.Tweening;
using TutorialFramework.Core;

namespace TutorialFramework.Actions
{
    [Serializable]
    public class MoveObjectAction : TutorialAction
    {
        public string targetObjectId;
        public Vector3 targetPosition;
        public Vector3 targetRotation;
        public float duration = 1.0f;
        public Ease easeType = Ease.InOutQuad;
        public bool isLocal = true;

        private Tween activeTween;

        public override void Execute(TutorialContext context, Action onComplete)
        {
            var target = TutorialObjectRegistry.Get(targetObjectId);
            if (target == null)
            {
                Debug.LogWarning($"[MoveObjectAction] Target '{targetObjectId}' not found in registry.");
                onComplete?.Invoke();
                return;
            }

            if (duration <= 0f)
            {
                FastForward(context);
                onComplete?.Invoke();
                return;
            }

            Sequence seq = DOTween.Sequence();
            if (isLocal)
            {
                seq.Join(target.transform.DOLocalMove(targetPosition, duration).SetEase(easeType));
                seq.Join(target.transform.DOLocalRotate(targetRotation, duration).SetEase(easeType));
            }
            else
            {
                seq.Join(target.transform.DOMove(targetPosition, duration).SetEase(easeType));
                seq.Join(target.transform.DORotate(targetRotation, duration).SetEase(easeType));
            }

            activeTween = seq;
            seq.OnComplete(() =>
            {
                activeTween = null;
                onComplete?.Invoke();
            });
        }

        public override void ResetAction(TutorialContext context)
        {
            if (activeTween != null && activeTween.IsActive())
            {
                activeTween.Kill();
                activeTween = null;
            }
        }

        public override void FastForward(TutorialContext context)
        {
            ResetAction(context);
            var target = TutorialObjectRegistry.Get(targetObjectId);
            if (target != null)
            {
                if (isLocal)
                {
                    target.transform.localPosition = targetPosition;
                    target.transform.localEulerAngles = targetRotation;
                }
                else
                {
                    target.transform.position = targetPosition;
                    target.transform.eulerAngles = targetRotation;
                }
            }
        }
    }
}
