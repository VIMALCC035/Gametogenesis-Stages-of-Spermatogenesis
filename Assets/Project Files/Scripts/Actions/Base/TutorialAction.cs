using System;
using UnityEngine;
using TutorialFramework.Core;

namespace TutorialFramework.Actions
{
    /// <summary>
    /// Abstract base class for all polymorphic slide actions.
    /// Actions are pure data containers serialized via [SerializeReference] inside SlideDefinition.
    /// </summary>
    [Serializable]
    public abstract class TutorialAction
    {
        [Tooltip("If true, the slide controller or parent composite will wait for this action to finish before progressing.")]
        [SerializeField] private bool waitForCompletion = true;
        public bool WaitForCompletion => waitForCompletion;

        /// <summary>
        /// Starts executing the action. Must invoke onComplete when the action finishes (or immediately if waitForCompletion is false).
        /// </summary>
        public abstract void Execute(TutorialContext context, Action onComplete);

        /// <summary>
        /// Resets the action, killing any active tweens, listeners, or ongoing operations.
        /// </summary>
        public abstract void ResetAction(TutorialContext context);

        /// <summary>
        /// Fast-forwards the action directly to its completed state.
        /// Used when restoring state or jumping across slides.
        /// </summary>
        public virtual void FastForward(TutorialContext context)
        {
            ResetAction(context);
        }
    }
}
