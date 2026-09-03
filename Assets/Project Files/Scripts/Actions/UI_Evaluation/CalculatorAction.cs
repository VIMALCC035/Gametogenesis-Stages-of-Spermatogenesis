using System;
using UnityEngine;
using TutorialFramework.Core;

namespace TutorialFramework.Actions
{
    [Serializable]
    public class CalculatorAction : TutorialAction
    {
        public float expectedAnswer = 25.0f;
        public float allowedTolerance = 0.1f;

        [TextArea(1, 3)]
        public string instructionText = "Calculate the result and enter the value.";

        public override void Execute(TutorialContext context, Action onComplete)
        {
            if (context.UI == null || context.UI.Calculator == null)
            {
                onComplete?.Invoke();
                return;
            }

            if (!string.IsNullOrEmpty(instructionText))
            {
                context.UI.SetInstruction(instructionText);
            }

            var calc = context.UI.Calculator;
            calc.Open(expectedAnswer, allowedTolerance, () =>
            {
                calc.Close();
                onComplete?.Invoke();
            });
        }

        public override void ResetAction(TutorialContext context)
        {
            if (context.UI != null && context.UI.Calculator != null)
            {
                context.UI.Calculator.Close();
            }
        }
    }
}
