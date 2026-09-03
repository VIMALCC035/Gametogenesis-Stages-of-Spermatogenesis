using System;
using System.Collections.Generic;
using UnityEngine;
using TutorialFramework.Core;

namespace TutorialFramework.Actions
{
    /// <summary>
    /// Executes child actions in sequential order, awaiting completion of each step before proceeding to the next.
    /// </summary>
    [Serializable]
    public class SequenceAction : TutorialAction
    {
        [SerializeReference]
        [SubclassSelector]
        public List<TutorialAction> sequence = new List<TutorialAction>();

        public override void Execute(TutorialContext context, Action onComplete)
        {
            if (sequence == null || sequence.Count == 0)
            {
                onComplete?.Invoke();
                return;
            }

            ExecuteStep(0, context, onComplete);
        }

        private void ExecuteStep(int index, TutorialContext context, Action onComplete)
        {
            if (index >= sequence.Count)
            {
                onComplete?.Invoke();
                return;
            }

            var currentAction = sequence[index];
            if (currentAction == null)
            {
                ExecuteStep(index + 1, context, onComplete);
                return;
            }

            if (currentAction.WaitForCompletion)
            {
                currentAction.Execute(context, () => ExecuteStep(index + 1, context, onComplete));
            }
            else
            {
                currentAction.Execute(context, null);
                ExecuteStep(index + 1, context, onComplete);
            }
        }

        public override void ResetAction(TutorialContext context)
        {
            if (sequence == null) return;
            foreach (var action in sequence)
            {
                action?.ResetAction(context);
            }
        }

        public override void FastForward(TutorialContext context)
        {
            if (sequence == null) return;
            foreach (var action in sequence)
            {
                action?.FastForward(context);
            }
        }
    }
}
