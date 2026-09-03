using System;
using System.Collections.Generic;
using UnityEngine;
using TutorialFramework.Core;
using TutorialFramework.RuntimeComponents;

namespace TutorialFramework.Actions
{
    [Serializable]
    public class QuestionOptionData
    {
        [Tooltip("Stable unique Option ID matching the button in the scene (e.g. 'F1', 'Option_A', 'opt_10cm').")]
        public string optionID = "Option_1";

        [Tooltip("The text displayed on this option button.")]
        public string optionText;

        [Tooltip("Optional image/icon displayed on this option button.")]
        public Sprite optionImage;

        public QuestionOptionData() { }

        public QuestionOptionData(string id, string text, Sprite img = null)
        {
            optionID = id;
            optionText = text;
            optionImage = img;
        }
    }

    [Serializable]
    public class QuestionSetup
    {
        [Tooltip("Panel ID matching the pre-placed EvaluationQuestionPanel in the scene (e.g. 'Question_01_Panel').")]
        public string panelID = "Question_01_Panel";

        [TextArea(1, 3)]
        [Tooltip("Optional question prompt text (reflects in scene question text).")]
        public string questionText;

        [Header("Option Buttons Text & Images")]
        [Tooltip("Configure the text and images for each option button in this question.")]
        public List<QuestionOptionData> options = new List<QuestionOptionData>();

        [Header("Correct Answer")]
        [Tooltip("The correct Option ID (e.g. 'F1', 'Option_A', 'opt_convex').")]
        public string correctAnswerID = "Option_1";

        [Header("Solution (Optional)")]
        [TextArea(2, 4)]
        [Tooltip("Explanation text displayed when viewing the solution.")]
        public string solutionText;

        [Tooltip("Optional diagram / illustration sprite for the solution.")]
        public Sprite solutionImage;
    }

    /// <summary>
    /// Clean, direct evaluation action that binds to pre-placed Scene Question Panels (EvaluationQuestionPanel).
    /// Updates question text & option button text directly from the ScriptableObject.
    /// Validates choices instantly on button click with tick (✔) / cross (✖) feedback and solution view.
    /// </summary>
    [Serializable]
    public class EvaluationSetupAction : TutorialAction
    {
        [Header("Evaluation Title & Instruction")]
        [TextArea(1, 3)]
        [Tooltip("Header text or instruction displayed on the slide.")]
        public string instructionText = "Choose the correct answer to continue:";

        [Header("Questions In This Slide")]
        [Tooltip("List of question panel configurations for this slide.")]
        public List<QuestionSetup> questions = new List<QuestionSetup>();

        [Header("Object Toggles on Success")]
        public List<string> objectsToEnableOnSuccess = new List<string>();
        public List<string> objectsToDisableOnSuccess = new List<string>();

        private Action evaluationCompleteCallback;
        private readonly List<EvaluationQuestionPanel> activeScenePanels = new List<EvaluationQuestionPanel>();

        public override void Execute(TutorialContext context, Action onComplete)
        {
            evaluationCompleteCallback = onComplete;
            activeScenePanels.Clear();

            if (!string.IsNullOrEmpty(instructionText) && context.UI != null)
            {
                context.UI.SetInstruction(instructionText);
            }

            if (questions == null || questions.Count == 0)
            {
                onComplete?.Invoke();
                return;
            }

            int remainingToSolve = questions.Count;
            bool anyPanelFound = false;

            foreach (var q in questions)
            {
                if (q == null) continue;

                var scenePanel = FindSceneQuestionPanel(q.panelID);
                if (scenePanel != null)
                {
                    anyPanelFound = true;
                    activeScenePanels.Add(scenePanel);

                    scenePanel.BindQuestion(q, () =>
                    {
                        remainingToSolve--;
                        if (remainingToSolve <= 0)
                        {
                            OnAllQuestionsSolved();
                        }
                    });
                }
                else
                {
                    Debug.LogWarning($"[EvaluationSetupAction] Pre-placed scene panel '{q.panelID}' not found!");
                }
            }

            if (!anyPanelFound)
            {
                Debug.LogWarning("[EvaluationSetupAction] No matching EvaluationQuestionPanel found in scene!");
                onComplete?.Invoke();
            }
        }

        private void OnAllQuestionsSolved()
        {
            Debug.Log("[EvaluationSetupAction] All questions completed successfully!");
            ToggleObjects(objectsToEnableOnSuccess, true);
            ToggleObjects(objectsToDisableOnSuccess, false);

            var cb = evaluationCompleteCallback;
            evaluationCompleteCallback = null;
            cb?.Invoke();
        }

        private EvaluationQuestionPanel FindSceneQuestionPanel(string panelId)
        {
            if (string.IsNullOrEmpty(panelId)) return null;

#if UNITY_2023_1_OR_NEWER
            var allPanels = UnityEngine.Object.FindObjectsByType<EvaluationQuestionPanel>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
            var allPanels = UnityEngine.Object.FindObjectsOfType<EvaluationQuestionPanel>(true);
#endif
            foreach (var p in allPanels)
            {
                if (p.PanelID.Equals(panelId, StringComparison.OrdinalIgnoreCase) || 
                    p.gameObject.name.Equals(panelId, StringComparison.OrdinalIgnoreCase))
                {
                    return p;
                }
            }

            // Fallback: check TutorialObjectRegistry
            var tutObj = TutorialObjectRegistry.Get(panelId);
            if (tutObj != null)
            {
                var panelComp = tutObj.GetComponent<EvaluationQuestionPanel>();
                if (panelComp != null) return panelComp;
            }

            return null;
        }

        private void ToggleObjects(List<string> objectIds, bool activeState)
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
            // Reset pre-placed scene panels
            foreach (var panel in activeScenePanels)
            {
                if (panel != null) panel.ResetPanel();
            }
            activeScenePanels.Clear();

            evaluationCompleteCallback = null;
        }

        public override void FastForward(TutorialContext context)
        {
            ResetAction(context);
            ToggleObjects(objectsToEnableOnSuccess, true);
            ToggleObjects(objectsToDisableOnSuccess, false);
        }
    }
}
