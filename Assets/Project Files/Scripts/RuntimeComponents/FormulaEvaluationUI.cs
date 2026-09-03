using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

namespace TutorialFramework.RuntimeComponents
{
    [Serializable]
    public struct FormulaAnswerTarget
    {
        [Tooltip("Field ID matching FormulaInputField (e.g. 'c', 'd', 'l', 'l_prime', 'delta_f' or 'Field_1').")]
        public string fieldId;

        [Tooltip("Expected numeric value the student must enter.")]
        public float expectedValue;

        [Tooltip("Allowed decimal tolerance (default 0 for exact).")]
        public float allowedTolerance;

        [Tooltip("Instruction text shown during this step.")]
        public string instructionText;
    }

    [DisallowMultipleComponent]
    public class FormulaEvaluationUI : MonoBehaviour
    {
        [Header("Panel Identification")]
        [Tooltip("Unique ID used by FormulaEvaluationAction (e.g. 'FormulaPanel_ErrorF').")]
        [SerializeField] private string panelId = "FormulaPanel_ErrorF";

        [Header("Containers & Animation")]
        [SerializeField] private GameObject panelParent;
        [SerializeField] private float popDuration = 0.3f;
        [SerializeField] private Ease popInEase = Ease.OutBack;
        [SerializeField] private Ease popOutEase = Ease.InBack;

        [Header("Draggable Calculator Keypad")]
        [SerializeField] private TableCalculatorKeypad calculatorKeypad;

        [Header("Formula Input Fields (5 Fields)")]
        [SerializeField] private List<FormulaInputField> inputFields = new List<FormulaInputField>();

        [Header("Feedback & Submit (Optional)")]
        [SerializeField] private Button submitButton;
        [SerializeField] private TextMeshProUGUI feedbackText;

        public string PanelId => panelId;
        public bool IsVisible => panelParent != null && panelParent.activeSelf;

        private List<FormulaAnswerTarget> activeTargetList;
        private int currentTargetIndex = 0;
        private int wrongAttemptsForCurrentField = 0;
        private Action onAllCompleted;

        private void Awake()
        {
            if (panelParent == null) panelParent = gameObject;

            EnsureKeypad();

            if (submitButton != null)
            {
                submitButton.onClick.RemoveAllListeners();
                submitButton.onClick.AddListener(OnSubmitClicked);
            }

            if (inputFields == null || inputFields.Count == 0)
            {
                inputFields = new List<FormulaInputField>(GetComponentsInChildren<FormulaInputField>(true));
            }
        }

        private void EnsureKeypad()
        {
            if (calculatorKeypad == null)
            {
#if UNITY_2023_1_OR_NEWER
                calculatorKeypad = FindFirstObjectByType<TableCalculatorKeypad>(FindObjectsInactive.Include);
#else
                calculatorKeypad = FindObjectOfType<TableCalculatorKeypad>(true);
#endif
            }
        }

        public void StartEvaluation(List<FormulaAnswerTarget> targets, Action completionCallback)
        {
            EnsureKeypad();
            activeTargetList = targets;
            currentTargetIndex = 0;
            wrongAttemptsForCurrentField = 0;
            onAllCompleted = completionCallback;

            if (feedbackText != null) feedbackText.text = string.Empty;

            foreach (var field in inputFields)
            {
                if (field != null)
                {
                    field.InitializeField();
                }
            }

            PopIn(panelParent);

            if (calculatorKeypad != null)
            {
                calculatorKeypad.gameObject.SetActive(true);
            }

            ActivateCurrentStep();
        }

        private void ActivateCurrentStep()
        {
            if (activeTargetList == null || currentTargetIndex >= activeTargetList.Count)
            {
                Debug.Log("[FormulaEvaluationUI] All formula fields completed successfully!");
                if (feedbackText != null)
                {
                    feedbackText.text = "<color=#00FF88>All formula calculations verified!</color>";
                }

                if (calculatorKeypad != null)
                {
                    //calculatorKeypad.SetAutofillVisible(false);
                }

                var cb = onAllCompleted;
                onAllCompleted = null;
                cb?.Invoke();
                return;
            }

            wrongAttemptsForCurrentField = 0;
            var currentTarget = activeTargetList[currentTargetIndex];

            for (int i = 0; i < inputFields.Count; i++)
            {
                var f = inputFields[i];
                if (f != null && !f.IsLockedCorrect && i != currentTargetIndex)
                {
                    f.SetDisabled();
                }
            }

            var activeField = inputFields.Find(f => f != null && f.FieldId.Equals(currentTarget.fieldId, StringComparison.OrdinalIgnoreCase));
            if (activeField == null && currentTargetIndex < inputFields.Count)
            {
                activeField = inputFields[currentTargetIndex];
            }

            if (activeField != null)
            {
                EnsureKeypad();

                if (calculatorKeypad != null)
                {
                    calculatorKeypad.gameObject.SetActive(true);
                    calculatorKeypad.BindToInput(
                        activeField.InputField,
                        () => ExecuteValidation(activeField, currentTarget.expectedValue, currentTarget.allowedTolerance),
                        () => TriggerAutofill(activeField, currentTarget.expectedValue)
                    );
                }

                activeField.ActivateForInput((_) =>
                {
                    ExecuteValidation(activeField, currentTarget.expectedValue, currentTarget.allowedTolerance);
                });

                Debug.Log($"[FormulaEvaluationUI] Activated Step {currentTargetIndex + 1}/{activeTargetList.Count} on field '{activeField.FieldId}'");
            }
        }

        private void OnSubmitClicked()
        {
            if (activeTargetList == null || currentTargetIndex >= activeTargetList.Count) return;
            var target = activeTargetList[currentTargetIndex];
            var field = inputFields.Find(f => f != null && f.FieldId.Equals(target.fieldId, StringComparison.OrdinalIgnoreCase)) ?? inputFields[currentTargetIndex];

            if (field != null)
            {
                ExecuteValidation(field, target.expectedValue, target.allowedTolerance);
            }
        }

        private void ExecuteValidation(FormulaInputField field, float expectedVal, float tolerance)
        {
            if (field == null) return;
            string inputStr = field.CurrentValue;

            if (string.IsNullOrWhiteSpace(inputStr))
            {
                if (feedbackText != null) feedbackText.text = "Please enter a value.";
                return;
            }

            if (float.TryParse(inputStr, out float enteredNumber))
            {
                bool isExact = Mathf.Approximately(enteredNumber, expectedVal) || 
                              Mathf.Abs(enteredNumber - expectedVal) < 0.001f ||
                              (tolerance > 0 && Mathf.Abs(enteredNumber - expectedVal) <= tolerance);

                if (isExact)
                {
                    field.MarkCorrect();

                    if (feedbackText != null)
                    {
                        feedbackText.text = "<color=#00FF88>Correct value!</color>";
                    }

                    if (calculatorKeypad != null)
                    {
                        //calculatorKeypad.SetAutofillVisible(false);
                    }

                    currentTargetIndex++;
                    ActivateCurrentStep();
                }
                else
                {
                    wrongAttemptsForCurrentField++;
                    field.MarkWrong();

                    if (feedbackText != null)
                    {
                        feedbackText.text = $"<color=#FF4444>Incorrect value ({wrongAttemptsForCurrentField}/3). Try again.</color>";
                    }

                    if (wrongAttemptsForCurrentField >= 3 && calculatorKeypad != null)
                    {
                        calculatorKeypad.SetAutofillVisible(true);
                        if (feedbackText != null)
                        {
                            feedbackText.text = "<color=#FFCC00>Incorrect 3 times. Click 'Autofill' to proceed.</color>";
                        }
                    }
                }
            }
            else
            {
                if (feedbackText != null) feedbackText.text = "Please enter a valid number.";
                field.MarkWrong();
            }
        }

        private void TriggerAutofill(FormulaInputField field, float expectedVal)
        {
            if (field == null) return;

            string formatted = expectedVal.ToString("F3");
            field.SetDirectValue(formatted);
            field.MarkCorrect();

            if (calculatorKeypad != null)
            {
                //calculatorKeypad.SetAutofillVisible(false);
            }

            if (feedbackText != null)
            {
                feedbackText.text = "<color=#00FF88>Autofilled correct value!</color>";
            }

            currentTargetIndex++;
            ActivateCurrentStep();
        }

        public void Close()
        {
            if (panelParent != null) PopOut(panelParent);
            onAllCompleted = null;
        }

        private void PopIn(GameObject target)
        {
            if (target == null) return;
            target.SetActive(true);
            target.transform.DOKill();
            target.transform.localScale = Vector3.zero;
            target.transform.DOScale(Vector3.one, popDuration).SetEase(popInEase).SetUpdate(true);
        }

        private void PopOut(GameObject target)
        {
            if (target == null) return;
            target.transform.DOKill();
            target.transform.DOScale(Vector3.zero, popDuration * 0.8f).SetEase(popOutEase).SetUpdate(true).OnComplete(() =>
            {
                target.SetActive(false);
                target.transform.localScale = Vector3.one;
            });
        }
    }
}
