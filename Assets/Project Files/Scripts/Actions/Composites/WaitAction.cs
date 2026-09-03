using System;
using System.Collections;
using UnityEngine;
using TutorialFramework.Core;

namespace TutorialFramework.Actions
{
    [Serializable]
    public class WaitAction : TutorialAction
    {
        [SerializeField] private float durationSeconds = 1.0f;
        private Coroutine activeCoroutine;

        public override void Execute(TutorialContext context, Action onComplete)
        {
            if (durationSeconds <= 0f)
            {
                onComplete?.Invoke();
                return;
            }

            activeCoroutine = context.CoroutineRunner.StartCoroutine(WaitRoutine(durationSeconds, onComplete));
        }

        private IEnumerator WaitRoutine(float duration, Action onComplete)
        {
            yield return new WaitForSeconds(duration);
            activeCoroutine = null;
            onComplete?.Invoke();
        }

        public override void ResetAction(TutorialContext context)
        {
            if (activeCoroutine != null)
            {
                context.CoroutineRunner.StopCoroutine(activeCoroutine);
                activeCoroutine = null;
            }
        }
    }
}
