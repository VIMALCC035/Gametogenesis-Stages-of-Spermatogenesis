using System;
using System.Collections.Generic;
using UnityEngine;
using TutorialFramework.Core;
using TutorialFramework.RuntimeComponents;

namespace TutorialFramework.Actions
{
    [Serializable]
    public struct TableCellStep
    {
        [Tooltip("Cell ID to activate (e.g. 'R2_Needle', 'R2_Lens', 'R2_Mirror').")]
        public string targetCellId;

        [Tooltip("Expected numeric value the student must enter.")]
        public float expectedValue;

        [Tooltip("Allowed decimal tolerance (default 0).")]
        public float allowedTolerance;

        [Tooltip("Instruction text displayed on UI during this step.")]
        public string instructionText;
    }

    [Serializable]
    public class ObservationTableAction : TutorialAction
    {
        [Header("Table ID")]
        [Tooltip("Table ID in TutorialUIManager (e.g. 'ObservationTable2').")]
        public string tableId = "ObservationTable2";

        [Header("Single Cell Target (Legacy / Single Step)")]
        [Tooltip("Target Cell ID if only one cell is required.")]
        public string targetCellId = "R2_Mirror";
        public float expectedValue = 65.8f;
        public float allowedTolerance = 0.2f;

        [Header("Multiple Sequential Cell Steps (Optional)")]
        [Tooltip("If this list is populated, executes these cell entries one by one in sequence!")]
        public List<TableCellStep> sequentialSteps = new List<TableCellStep>();

        [Header("General Options")]
        [Tooltip("If true, automatically opens the table when this action begins.")]
        public bool autoOpenTable = true;

        [TextArea(1, 3)]
        public string defaultInstruction = "Enter the required observation values into the table.";

        [Header("Objects To Toggle On Final Completion")]
        public List<string> objectsToEnableOnSuccess = new List<string>();
        public List<string> objectsToDisableOnSuccess = new List<string>();

        public override void Execute(TutorialContext context, Action onComplete)
        {
            if (context.UI == null)
            {
                onComplete?.Invoke();
                return;
            }

            var table = context.UI.GetObservationTable(tableId);
            if (table == null)
            {
                Debug.LogWarning($"[ObservationTableAction] Table with ID '{tableId}' not found in TutorialUIManager.");
                onComplete?.Invoke();
                return;
            }

            if (sequentialSteps != null && sequentialSteps.Count > 0)
            {
                ExecuteStep(0, context, table, onComplete);
            }
            else
            {
                if (!string.IsNullOrEmpty(defaultInstruction))
                {
                    context.UI.SetActionInstruction(defaultInstruction, ActionInstructionType.ObservationTable);
                }

                table.ConfigureActiveStep(
                    targetCellId, 
                    expectedValue, 
                    allowedTolerance, 
                    autoOpenTable, 
                    () =>
                    {
                        Debug.Log($"[ObservationTableAction] Cell '{targetCellId}' completed!");
                        ToggleObjects(objectsToEnableOnSuccess, true, "Enabled On Success");
                        ToggleObjects(objectsToDisableOnSuccess, false, "Disabled On Success");
                        onComplete?.Invoke();
                    }
                );
            }
        }

        private void ExecuteStep(int stepIndex, TutorialContext context, ObservationTableUI table, Action onComplete)
        {
            if (stepIndex >= sequentialSteps.Count)
            {
                Debug.Log("[ObservationTableAction] All sequential cell steps completed!");
                ToggleObjects(objectsToEnableOnSuccess, true, "Enabled On Success");
                ToggleObjects(objectsToDisableOnSuccess, false, "Disabled On Success");
                onComplete?.Invoke();
                return;
            }

            var step = sequentialSteps[stepIndex];
            string instr = !string.IsNullOrEmpty(step.instructionText) ? step.instructionText : defaultInstruction;
            if (context.UI != null && !string.IsNullOrEmpty(instr))
            {
                context.UI.SetActionInstruction(instr, ActionInstructionType.ObservationTable);
            }

            table.ConfigureActiveStep(
                step.targetCellId,
                step.expectedValue,
                step.allowedTolerance,
                autoOpenTable,
                () =>
                {
                    Debug.Log($"[ObservationTableAction] Step {stepIndex + 1}/{sequentialSteps.Count} ('{step.targetCellId}') completed!");
                    ExecuteStep(stepIndex + 1, context, table, onComplete);
                }
            );
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
            if (context?.UI != null)
            {
                var table = context.UI.GetObservationTable(tableId);
                if (table != null)
                {
                    table.Hide();
                }
            }
        }

        public override void FastForward(TutorialContext context)
        {
            if (context?.UI != null)
            {
                var table = context.UI.GetObservationTable(tableId);
                if (table != null)
                {
                    table.RefreshPersistentState();
                    if (autoOpenTable)
                    {
                        table.SetTableVisible(true);
                    }
                }
            }
        }
    }
}
