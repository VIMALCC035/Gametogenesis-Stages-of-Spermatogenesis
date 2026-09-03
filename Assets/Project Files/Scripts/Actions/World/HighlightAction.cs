using System;
using UnityEngine;
using TutorialFramework.Core;

namespace TutorialFramework.Actions
{
    [Serializable]
    public class HighlightAction : TutorialAction
    {
        public string targetObjectId;
        public Material highlightMaterial;
        public bool enableHighlight = true;

        public override void Execute(TutorialContext context, Action onComplete)
        {
            var target = TutorialObjectRegistry.Get(targetObjectId);
            if (target != null && context.Highlight != null)
            {
                if (enableHighlight)
                {
                    context.Highlight.ApplyHighlight(target.gameObject, highlightMaterial);
                }
                else
                {
                    context.Highlight.RemoveHighlight(target.gameObject);
                }
            }
            onComplete?.Invoke();
        }

        public override void ResetAction(TutorialContext context)
        {
            var target = TutorialObjectRegistry.Get(targetObjectId);
            if (target != null && context.Highlight != null)
            {
                context.Highlight.RemoveHighlight(target.gameObject);
            }
        }
    }
}
