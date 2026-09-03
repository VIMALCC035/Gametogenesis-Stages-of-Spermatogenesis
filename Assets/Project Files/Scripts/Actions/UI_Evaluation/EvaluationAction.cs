using System;
using UnityEngine;
using TutorialFramework.Core;
using TutorialFramework.RuntimeComponents;

namespace TutorialFramework.Actions
{
    public enum EvaluationType
    {
        Numeric,
        Boolean,
        StringMatch
    }

    [Serializable]
    public class EvaluationAction : TutorialAction
    {
        public EvaluationType evaluationType = EvaluationType.Numeric;
        public float expectedNumericAnswer;
        public float numericTolerance = 0.01f;
        public string expectedStringAnswer;
        public bool expectedBooleanAnswer;

        [TextArea(1, 3)]
        public string instructionText;

        public override void Execute(TutorialContext context, Action onComplete)
        {
            if (!string.IsNullOrEmpty(instructionText) && context.UI != null)
            {
                context.UI.SetActionInstruction(instructionText, ActionInstructionType.GenericAction);
            }

            // In standard workflow, this completes when evaluation criteria are satisfied
            onComplete?.Invoke();
        }

        public override void ResetAction(TutorialContext context)
        {
        }
    }
}
