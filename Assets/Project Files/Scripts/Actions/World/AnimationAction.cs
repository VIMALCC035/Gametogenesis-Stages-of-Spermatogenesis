using System;
using System.Collections;
using UnityEngine;
using TutorialFramework.Core;

namespace TutorialFramework.Actions
{
    public enum AnimationPlayMode
    {
        SetTrigger,
        PlayState,
        ResetToDefault
    }

    [Serializable]
    public class AnimationAction : TutorialAction
    {
        public string targetObjectId;
        public AnimationPlayMode mode = AnimationPlayMode.PlayState;
        public string stateOrTriggerName;
        public int layer = 0;

        public override void Execute(TutorialContext context, Action onComplete)
        {
            var target = TutorialObjectRegistry.Get(targetObjectId);
            if (target == null)
            {
                Debug.LogWarning($"[AnimationAction] No object found for '{targetObjectId}'");
                onComplete?.Invoke();
                return;
            }

            var animator = target.GetComponent<Animator>();
            if (animator == null)
            {
                Debug.LogWarning($"[AnimationAction] No Animator on '{targetObjectId}'");
                onComplete?.Invoke();
                return;
            }

            switch (mode)
            {
                case AnimationPlayMode.SetTrigger:
                    animator.SetTrigger(stateOrTriggerName);
                    break;
                case AnimationPlayMode.PlayState:
                    animator.Play(stateOrTriggerName, layer, 0f);
                    break;
                case AnimationPlayMode.ResetToDefault:
                    animator.Rebind();
                    animator.Update(0f);
                    onComplete?.Invoke();
                    return;
            }

            if (WaitForCompletion && context.CoroutineRunner != null)
            {
                context.CoroutineRunner.StartCoroutine(WaitForAnimation(animator, stateOrTriggerName, layer, onComplete));
            }
            else
            {
                onComplete?.Invoke();
            }
        }

        private IEnumerator WaitForAnimation(Animator animator, string stateName, int layerIndex, Action onComplete)
        {
            yield return null;
            if (animator == null)
            {
                onComplete?.Invoke();
                yield break;
            }

            var stateInfo = animator.GetCurrentAnimatorStateInfo(layerIndex);
            while (animator != null && stateInfo.IsName(stateName) && stateInfo.normalizedTime < 1.0f)
            {
                yield return null;
                if (animator == null) break;
                stateInfo = animator.GetCurrentAnimatorStateInfo(layerIndex);
            }
            onComplete?.Invoke();
        }

        public override void ResetAction(TutorialContext context)
        {
            var target = TutorialObjectRegistry.Get(targetObjectId);
            if (target != null)
            {
                var animator = target.GetComponent<Animator>();
                if (animator != null)
                {
                    animator.Rebind();
                    animator.Update(0f);
                }
            }
        }

        public override void FastForward(TutorialContext context)
        {
            var target = TutorialObjectRegistry.Get(targetObjectId);
            if (target != null && !string.IsNullOrEmpty(stateOrTriggerName))
            {
                var animator = target.GetComponent<Animator>();
                if (animator != null)
                {
                    animator.Play(stateOrTriggerName, layer, 1.0f);
                    animator.Update(0f);
                }
            }
        }
    }
}
