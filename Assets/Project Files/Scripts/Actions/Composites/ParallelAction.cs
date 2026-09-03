using System;
using System.Collections.Generic;
using UnityEngine;
using TutorialFramework.Core;

namespace TutorialFramework.Actions
{
    /// <summary>
    /// Executes all child actions concurrently.
    /// Optionally waits until all blocking child actions complete before signaling completion.
    /// </summary>
    [Serializable]
    public class ParallelAction : TutorialAction
    {
        [SerializeReference]
        [SubclassSelector]
        public List<TutorialAction> subActions = new List<TutorialAction>();

        public override void Execute(TutorialContext context, Action onComplete)
        {
            if (subActions == null || subActions.Count == 0)
            {
                onComplete?.Invoke();
                return;
            }

            int remaining = 0;
            var waitingActions = new List<TutorialAction>();

            foreach (var action in subActions)
            {
                if (action != null && action.WaitForCompletion)
                {
                    remaining++;
                    waitingActions.Add(action);
                }
            }

            if (remaining == 0)
            {
                foreach (var action in subActions)
                {
                    action?.Execute(context, null);
                }
                onComplete?.Invoke();
                return;
            }

            void OnSubActionComplete()
            {
                remaining--;
                if (remaining <= 0)
                {
                    onComplete?.Invoke();
                }
            }

            foreach (var action in subActions)
            {
                if (action == null) continue;

                if (waitingActions.Contains(action))
                {
                    action.Execute(context, OnSubActionComplete);
                }
                else
                {
                    action.Execute(context, null);
                }
            }
        }

        public override void ResetAction(TutorialContext context)
        {
            if (subActions == null) return;
            foreach (var action in subActions)
            {
                action?.ResetAction(context);
            }
        }

        public override void FastForward(TutorialContext context)
        {
            if (subActions == null) return;
            foreach (var action in subActions)
            {
                action?.FastForward(context);
            }
        }
    }
}
