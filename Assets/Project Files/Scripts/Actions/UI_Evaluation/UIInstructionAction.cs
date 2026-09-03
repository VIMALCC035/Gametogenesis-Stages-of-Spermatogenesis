using System;
using System.Collections.Generic;
using UnityEngine;
using TutorialFramework.Core;

namespace TutorialFramework.Actions
{
    [Serializable]
    public class UIInstructionAction : TutorialAction
    {
        [TextArea(2, 4)]
        public string instructionText;

        public List<string> panelsToEnable = new List<string>();
        public List<string> panelsToDisable = new List<string>();

        public override void Execute(TutorialContext context, Action onComplete)
        {
            if (context.UI != null)
            {
                if (!string.IsNullOrEmpty(instructionText))
                {
                    context.UI.SetInstruction(instructionText);
                }

                context.UI.ConfigurePanels(panelsToEnable, panelsToDisable);
            }

            onComplete?.Invoke();
        }

        public override void ResetAction(TutorialContext context)
        {
            // Handled by next slide UI configuration
        }
    }
}
