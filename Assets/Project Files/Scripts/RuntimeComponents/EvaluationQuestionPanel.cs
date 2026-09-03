using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using TutorialFramework.Actions;

namespace TutorialFramework.RuntimeComponents
{
    /// <summary>
    /// Component placed on each pre-placed Scene Question Panel GameObject.
    /// Manages question prompt, updates option button text directly from ScriptableObject,
    /// provides instant option selection with tick/cross feedback, locks buttons upon correct answer,
    /// and toggles solution panel.
    /// </summary>
    [DisallowMultipleComponent]
    public class EvaluationQuestionPanel : MonoBehaviour
    {
        [Header("Panel Identity")]
        [Tooltip("Unique ID matching the panelID specified in the ScriptableObject (e.g. 'Question_01_Panel', 'Question_Lens_Panel').")]
        [SerializeField] private string panelID = "Question_01_Panel";

        [Header("Question Header")]
        [SerializeField] private TextMeshProUGUI questionPromptText;

        [Header("Option Buttons")]
        [SerializeField] private List<EvaluationOptionButton> optionButtons = new List<EvaluationOptionButton>();

        [Header("Solution Panel")]
        [Tooltip("Button that toggles/opens the solution panel.")]
        [SerializeField] private Button viewSolutionButton;
        [Tooltip("The Solution Dialog or Container GameObject.")]
        [SerializeField] private GameObject solutionPanel;
        [SerializeField] private TextMeshProUGUI solutionText;
        [SerializeField] private Image solutionImage;
        [SerializeField] private Button closeSolutionButton;

        public string PanelID => panelID;
        public bool IsVisible => gameObject.activeSelf;

        private QuestionSetup activeQuestion;
        private Action onCorrectCompleted;
        private string currentSelectedOptionID;
        private bool isAnsweredCorrectly = false;

        private void Awake()
        {
            AutoRegisterOptionButtons();

            if (viewSolutionButton != null)
            {
                viewSolutionButton.onClick.RemoveAllListeners();
                viewSolutionButton.onClick.AddListener(ToggleSolution);
            }

            if (closeSolutionButton != null)
            {
                closeSolutionButton.onClick.RemoveAllListeners();
                closeSolutionButton.onClick.AddListener(() => ShowSolution(false));
            }
        }

        private void AutoRegisterOptionButtons()
        {
            if (optionButtons == null || optionButtons.Count == 0)
            {
                optionButtons = new List<EvaluationOptionButton>(GetComponentsInChildren<EvaluationOptionButton>(true));
            }
        }

        public void BindQuestion(QuestionSetup question, Action onCorrect)
        {
            activeQuestion = question;
            onCorrectCompleted = onCorrect;
            currentSelectedOptionID = string.Empty;
            isAnsweredCorrectly = false;

            AutoRegisterOptionButtons();

            // 1. Set question text from ScriptableObject if provided
            if (questionPromptText != null && !string.IsNullOrEmpty(question.questionText))
            {
                questionPromptText.text = question.questionText;
            }

            // 2. Apply Option Text & Images from ScriptableObject
            if (question.options != null && question.options.Count > 0)
            {
                for (int i = 0; i < question.options.Count; i++)
                {
                    var optData = question.options[i];
                    if (optData == null) continue;

                    var optBtn = optionButtons.Find(b => b != null && b.OptionID.Equals(optData.optionID, StringComparison.OrdinalIgnoreCase));
                    if (optBtn == null && i < optionButtons.Count)
                    {
                        optBtn = optionButtons[i];
                    }

                    if (optBtn != null)
                    {
                        if (!string.IsNullOrEmpty(optData.optionText))
                        {
                            optBtn.SetText(optData.optionText);
                        }
                        if (optData.optionImage != null)
                        {
                            optBtn.SetImage(optData.optionImage);
                        }
                    }
                }
            }

            // 3. Set solution content from ScriptableObject
            if (solutionText != null && !string.IsNullOrEmpty(question.solutionText))
            {
                solutionText.text = question.solutionText;
            }
            if (solutionImage != null)
            {
                if (question.solutionImage != null)
                {
                    solutionImage.sprite = question.solutionImage;
                    solutionImage.gameObject.SetActive(true);
                }
                else
                {
                    solutionImage.gameObject.SetActive(false);
                }
            }

            ShowSolution(false);

            // 4. Initialize and unlock all option buttons
            foreach (var optBtn in optionButtons)
            {
                if (optBtn == null) continue;
                optBtn.Initialize(OnOptionSelected);
            }

            gameObject.SetActive(true);
        }

        private void OnOptionSelected(string optionID)
        {
            if (isAnsweredCorrectly || activeQuestion == null) return;

            currentSelectedOptionID = optionID;

            // Instant validation against correctAnswerID
            bool isCorrect = !string.IsNullOrEmpty(optionID) && 
                             !string.IsNullOrEmpty(activeQuestion.correctAnswerID) &&
                             optionID.Equals(activeQuestion.correctAnswerID, StringComparison.OrdinalIgnoreCase);

            // Show tick (✔) if correct or cross (✖) if wrong on the clicked option
            foreach (var optBtn in optionButtons)
            {
                if (optBtn == null) continue;

                if (optBtn.OptionID.Equals(optionID, StringComparison.OrdinalIgnoreCase))
                {
                    optBtn.SetSelected(true);
                    optBtn.ShowFeedback(isCorrect);
                }
                else
                {
                    optBtn.SetSelected(false);
                    optBtn.ResetVisuals();
                }
            }

            if (isCorrect)
            {
                isAnsweredCorrectly = true;
                Debug.Log($"[EvaluationQuestionPanel] Answer '{optionID}' is Correct! Locking all buttons.");

                // Lock all option buttons so student cannot change to another button
                SetButtonsInteractable(false);

                var cb = onCorrectCompleted;
                onCorrectCompleted = null;
                cb?.Invoke();
            }
            else
            {
                Debug.Log($"[EvaluationQuestionPanel] Answer '{optionID}' is Incorrect. Showing cross (✖).");
            }
        }

        private void SetButtonsInteractable(bool interactable)
        {
            foreach (var optBtn in optionButtons)
            {
                if (optBtn != null)
                {
                    optBtn.SetInteractable(interactable);
                }
            }
        }

        public void ToggleSolution()
        {
            if (solutionPanel != null)
            {
                ShowSolution(!solutionPanel.activeSelf);
            }
        }

        public void ShowSolution(bool visible)
        {
            if (solutionPanel != null)
            {
                solutionPanel.SetActive(visible);
                if (visible)
                {
                    solutionPanel.transform.DOKill();
                    solutionPanel.transform.localScale = Vector3.zero;
                    solutionPanel.transform.DOScale(Vector3.one, 0.25f).SetEase(Ease.OutBack).SetUpdate(true);
                }
            }
        }

        public void ResetPanel()
        {
            currentSelectedOptionID = string.Empty;
            isAnsweredCorrectly = false;
            ShowSolution(false);

            foreach (var optBtn in optionButtons)
            {
                if (optBtn != null)
                {
                    optBtn.SetInteractable(true);
                    optBtn.ResetVisuals();
                }
            }

            gameObject.SetActive(false);
        }
    }
}
