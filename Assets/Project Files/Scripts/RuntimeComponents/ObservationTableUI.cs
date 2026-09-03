using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

namespace TutorialFramework.RuntimeComponents
{
    public class ObservationTableUI : MonoBehaviour
    {
        [Header("Table Identification")]
        [Tooltip("Unique identifier used by ObservationTableAction (e.g. 'ObservationTable2').")]
        [SerializeField] private string tableId = "ObservationTable2";

        [Header("Containers & Toggle Controls")]
        [Tooltip("The Table Parent GameObject (TableParent) that shows/hides.")]
        [SerializeField] private GameObject tableParent;

        [Tooltip("The Calculator Keypad Parent GameObject (calendarParent / calculatorParent).")]
        [SerializeField] private GameObject calendarParent;

        [Tooltip("The button (ObservationTableTwoBtn) that stays visible.")]
        [SerializeField] private Button observationTableButton;
        [SerializeField] private TextMeshProUGUI buttonText;

        [Tooltip("The table's Check / Submit button.")]
        [SerializeField] private Button checkButton;
        [SerializeField] private TextMeshProUGUI feedbackText;

        [Header("Pop Animation Settings (DOTween)")]
        [SerializeField] private float popDuration = 0.3f;
        [SerializeField] private Ease popInEase = Ease.OutBack;
        [SerializeField] private Ease popOutEase = Ease.InBack;

        [Header("On-Screen Calculator Keypad")]
        [SerializeField] private TableCalculatorKeypad calculatorKeypad;

        [Header("Automatic Scroll Controller")]
        [SerializeField] private SimpleTableScrollbarController scrollController;

        [Header("All Registered Table Cells")]
        [SerializeField] private List<ObservationTableCell> tableCells = new List<ObservationTableCell>();

        // Persistent data across all slides in the session
        private static readonly Dictionary<string, string> persistentCellValues = new Dictionary<string, string>();
        private static readonly HashSet<string> persistentCorrectCells = new HashSet<string>();

        public string TableId => tableId;
        public bool IsTableVisible => tableParent != null && tableParent.activeSelf && tableParent.transform.localScale.x > 0.1f;

        private ObservationTableCell currentActiveCell;
        private float expectedAnswer;
        private int wrongAttemptsForCurrentCell = 0;
        private Action onStepCompleted;

        private void Awake()
        {
            EnsureKeypadReferences();

            if (observationTableButton != null)
            {
                observationTableButton.onClick.RemoveAllListeners();
                observationTableButton.onClick.AddListener(ToggleTableVisibility);
                observationTableButton.gameObject.SetActive(true);
                observationTableButton.interactable = true;
            }

            if (checkButton != null)
            {
                checkButton.onClick.RemoveAllListeners();
                checkButton.onClick.AddListener(OnCheckButtonClicked);
            }

            AutoRegisterCells();
            InitializePersistentDefaults();
            RefreshPersistentState();
        }

        private void EnsureKeypadReferences()
        {
            if (calculatorKeypad == null)
            {
#if UNITY_2023_1_OR_NEWER
                calculatorKeypad = FindFirstObjectByType<TableCalculatorKeypad>(FindObjectsInactive.Include);
#else
                calculatorKeypad = FindObjectOfType<TableCalculatorKeypad>(true);
#endif
            }

            if (calendarParent == null && calculatorKeypad != null)
            {
                calendarParent = calculatorKeypad.gameObject;
            }
        }

        private void AutoRegisterCells()
        {
            if (tableCells == null || tableCells.Count == 0)
            {
                tableCells = new List<ObservationTableCell>(GetComponentsInChildren<ObservationTableCell>(true));
            }

            foreach (var cell in tableCells)
            {
                if (cell != null && cell.InputField != null)
                {
                    cell.InputField.characterLimit = 6;
                }
            }
        }

        private void InitializePersistentDefaults()
        {
            foreach (var cell in tableCells)
            {
                if (cell == null) continue;

                if (cell.IsPreFilled && !persistentCellValues.ContainsKey(cell.CellId))
                {
                    persistentCellValues[cell.CellId] = cell.PreFilledValue;
                    persistentCorrectCells.Add(cell.CellId);
                }
            }
        }

        public void RefreshPersistentState()
        {
            foreach (var cell in tableCells)
            {
                if (cell == null) continue;

                string val = persistentCellValues.TryGetValue(cell.CellId, out var saved) ? saved : (cell.IsPreFilled ? cell.PreFilledValue : string.Empty);
                bool isCorrect = persistentCorrectCells.Contains(cell.CellId) || cell.IsPreFilled;

                cell.InitializeCell(val, isCorrect);
            }
        }

        public void ToggleTableVisibility()
        {
            if (tableParent == null) return;
            bool newState = !IsTableVisible;
            SetTableVisible(newState);
        }

        public void SetTableVisible(bool visible)
        {
            EnsureKeypadReferences();

            if (visible)
            {
                if (tableParent != null) PopIn(tableParent);
                if (calendarParent != null) PopIn(calendarParent);
            }
            else
            {
                if (tableParent != null) PopOut(tableParent);
                if (calendarParent != null) PopOut(calendarParent);
            }

            UpdateToggleButtonLabel(visible);

            if (observationTableButton != null)
            {
                observationTableButton.gameObject.SetActive(true);
                observationTableButton.interactable = true;
            }

            if (visible && currentActiveCell != null && scrollController != null)
            {
                scrollController.FocusOnCell(currentActiveCell.CellId, currentActiveCell.transform);
            }
        }

        private void PopIn(GameObject target)
        {
            if (target == null) return;

            target.transform.DOKill();
            target.SetActive(true);
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

        private void UpdateToggleButtonLabel(bool visible)
        {
            if (buttonText != null)
            {
                buttonText.text = visible ? "Hide Observation Table 2" : "Show Observation Table 2";
            }
        }

        public void ConfigureActiveStep(
            string targetCellId, 
            float expectedVal, 
            float tolerance, 
            bool autoOpenTable, 
            Action completeCallback)
        {
            onStepCompleted = completeCallback;
            expectedAnswer = expectedVal;
            wrongAttemptsForCurrentCell = 0;

            RefreshPersistentState();

            if (feedbackText != null) feedbackText.text = string.Empty;

            if (observationTableButton != null)
            {
                observationTableButton.gameObject.SetActive(true);
                observationTableButton.interactable = true;
            }

            foreach (var cell in tableCells)
            {
                if (cell != null) cell.SetDisabled(true);
            }

            currentActiveCell = tableCells.Find(c => c != null && c.CellId.Equals(targetCellId, StringComparison.OrdinalIgnoreCase));

            if (currentActiveCell != null)
            {
                if (autoOpenTable)
                {
                    SetTableVisible(true);
                }

                if (scrollController != null)
                {
                    scrollController.FocusOnCell(currentActiveCell.CellId, currentActiveCell.transform);
                }

                if (persistentCorrectCells.Contains(currentActiveCell.CellId))
                {
                    currentActiveCell.MarkCorrect(false);
                    onStepCompleted?.Invoke();
                    return;
                }

                EnsureKeypadReferences();

                if (calculatorKeypad != null)
                {
                    calculatorKeypad.BindToInput(
                        currentActiveCell.InputField, 
                        () => ExecuteUnifiedValidation(),
                        () => TriggerAutofill()
                    );
                }

                currentActiveCell.ActivateForInput();
            }
            else
            {
                Debug.LogWarning($"[ObservationTableUI] Target cell '{targetCellId}' not found in '{tableId}'!");
            }
        }

        private void OnCheckButtonClicked()
        {
            ExecuteUnifiedValidation();
        }

        private void ExecuteUnifiedValidation()
        {
            if (currentActiveCell == null) return;

            string textToValidate = currentActiveCell.CurrentValue;
            Debug.Log($"[ObservationTableUI] Validating input text: '{textToValidate}' against Expected: '{expectedAnswer}'");
            ValidateInput(textToValidate);
        }

        private void ValidateInput(string inputString)
        {
            if (string.IsNullOrWhiteSpace(inputString))
            {
                if (feedbackText != null) feedbackText.text = "Please enter a value.";
                return;
            }

            if (float.TryParse(inputString, out float enteredNumber))
            {
                bool isExactMatch = Mathf.Approximately(enteredNumber, expectedAnswer) || 
                                   Mathf.Abs(enteredNumber - expectedAnswer) < 0.001f;

                if (isExactMatch)
                {
                    if (feedbackText != null)
                    {
                        feedbackText.text = "<color=#00FF88>Correct! Value recorded in table.</color>";
                    }

                    if (currentActiveCell != null)
                    {
                        currentActiveCell.MarkCorrect(true);
                        persistentCellValues[currentActiveCell.CellId] = inputString;
                        persistentCorrectCells.Add(currentActiveCell.CellId);
                    }

                    if (calculatorKeypad != null)
                    {
                        calculatorKeypad.SetAutofillVisible(false);
                    }

                    var cb = onStepCompleted;
                    onStepCompleted = null;
                    cb?.Invoke();
                }
                else
                {
                    wrongAttemptsForCurrentCell++;

                    if (feedbackText != null)
                    {
                        feedbackText.text = $"<color=#FF4444>Incorrect value. Try again ({wrongAttemptsForCurrentCell}/3).</color>";
                    }

                    if (currentActiveCell != null)
                    {
                        currentActiveCell.MarkWrong();
                    }

                    if (wrongAttemptsForCurrentCell >= 3 && calculatorKeypad != null)
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
                if (currentActiveCell != null) currentActiveCell.MarkWrong();
            }
        }

        private void TriggerAutofill()
        {
            if (currentActiveCell == null) return;

            string formattedVal = expectedAnswer.ToString("F1");
            currentActiveCell.SetDirectValue(formattedVal);

            currentActiveCell.MarkCorrect(true);
            persistentCellValues[currentActiveCell.CellId] = formattedVal;
            persistentCorrectCells.Add(currentActiveCell.CellId);

            if (calculatorKeypad != null)
            {
                calculatorKeypad.SetAutofillVisible(false);
            }

            if (feedbackText != null)
            {
                feedbackText.text = "<color=#00FF88>Autofilled correct value!</color>";
            }

            var cb = onStepCompleted;
            onStepCompleted = null;
            cb?.Invoke();
        }

        public void Hide()
        {
            SetTableVisible(false);
            onStepCompleted = null;
        }
    }
}
