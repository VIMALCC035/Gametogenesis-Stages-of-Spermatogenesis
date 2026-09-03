using System;
using System.Collections.Generic;
using UnityEngine;
using TutorialFramework.Core;
using TutorialFramework.RuntimeComponents;

namespace TutorialFramework.Actions
{
    [Serializable]
    public class FormulaEvaluationAction : TutorialAction
    {
        [Header("Panel ID")]
        [Tooltip("Panel ID in TutorialUIManager (e.g. 'FormulaPanel_ErrorF').")]
        public string panelId = "FormulaPanel_ErrorF";

        [Header("Expected Values For The 5 Inputs (In Sequence)")]
        public List<FormulaAnswerTarget> targetAnswers = new List<FormulaAnswerTarget>();

        [Header("General Options")]
        public string instructionText = "Calculate the error in measurement of 'f' using the formula.";

        [Header("Objects To Toggle On All 5 Completed")]
        public List<string> objectsToEnableOnSuccess = new List<string>();
        public List<string> objectsToDisableOnSuccess = new List<string>();

        public override void Execute(TutorialContext context, Action onComplete)
        {
            if (!string.IsNullOrEmpty(instructionText) && context.UI != null)
            {
                context.UI.SetActionInstruction(instructionText, ActionInstructionType.Calculation);
            }

            var formulaUI = context.UI != null ? context.UI.GetFormulaPanel(panelId) : null;
            if (formulaUI == null)
            {
                Debug.LogWarning($"[FormulaEvaluationAction] Formula panel '{panelId}' not found in TutorialUIManager!");
                onComplete?.Invoke();
                return;
            }

            formulaUI.StartEvaluation(targetAnswers, () =>
            {
                Debug.Log($"[FormulaEvaluationAction] All 5 fields in '{panelId}' verified!");
                ToggleObjects(objectsToEnableOnSuccess, true, "Enabled On Success");
                ToggleObjects(objectsToDisableOnSuccess, false, "Disabled On Success");
                onComplete?.Invoke();
            });
        }

        private void ToggleObjects(List<string> objectIds, bool activeState, string logContext)
        {
            if (objectIds == null) return;
            foreach (var id in objectIds)
            {
                if (string.IsNullOrWhiteSpace(id)) continue;
                var obj = TutorialObjectRegistry.Get(id);
                if (obj != null)
                {
                    obj.gameObject.SetActive(activeState);
                }
            }
        }

        public override void ResetAction(TutorialContext context)
        {
            var formulaUI = context.UI != null ? context.UI.GetFormulaPanel(panelId) : null;
            if (formulaUI != null)
            {
                formulaUI.Close();
            }
        }
    }
}
