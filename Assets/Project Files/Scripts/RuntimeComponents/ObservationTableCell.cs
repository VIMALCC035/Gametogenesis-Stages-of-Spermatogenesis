using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace TutorialFramework.RuntimeComponents
{
    public class ObservationTableCell : MonoBehaviour
    {
        [Header("Cell Identity")]
        [Tooltip("Unique Cell ID (e.g., 'R1_Needle', 'R2_Observed_R', etc.).")]
        [SerializeField] private string cellId;

        [Header("Pre-Filled Default Data (e.g. For Row 1)")]
        [Tooltip("If true, this cell starts already filled with the value below.")]
        [SerializeField] private bool isPreFilled = false;

        [Tooltip("Default pre-filled reading (e.g. '10.0', '30.0', '65.8').")]
        [SerializeField] private string preFilledValue = "";

        [Header("UI Input & Display")]
        [SerializeField] private TMP_InputField inputField;
        [SerializeField] private TextMeshProUGUI valueDisplayText;

        [Header("Feedback Icons")]
        [SerializeField] private GameObject correctIcon;
        [SerializeField] private GameObject wrongIcon;

        public string CellId => cellId;
        public bool IsPreFilled => isPreFilled;
        public string PreFilledValue => preFilledValue;
        public TMP_InputField InputField => inputField;

        public string CurrentValue => inputField != null ? inputField.text : _savedValue;
        public bool IsLockedCorrect { get; private set; }

        private string _savedValue = string.Empty;
        private Coroutine wrongFeedbackCoroutine;
        private bool isClearingForFeedback = false;

        private void Awake()
        {
            if (inputField != null)
            {
                inputField.onValueChanged.AddListener(OnTypingChanged);
            }
        }

        public void InitializeCell(string savedValue, bool isCorrect)
        {
            if (!string.IsNullOrEmpty(savedValue) || isCorrect)
            {
                _savedValue = savedValue;
                IsLockedCorrect = isCorrect;
            }
            else if (isPreFilled)
            {
                _savedValue = preFilledValue;
                IsLockedCorrect = true;
            }
            else
            {
                _savedValue = string.Empty;
                IsLockedCorrect = false;
            }

            if (inputField != null)
            {
                inputField.text = _savedValue;
                inputField.interactable = !IsLockedCorrect;
            }

            if (valueDisplayText != null)
            {
                valueDisplayText.text = _savedValue;
            }

            // Finished/past cells do NOT show the tick
            SetFeedbackIcons(showCorrect: false, showWrong: false);
        }

        public void ActivateForInput()
        {
            if (inputField != null)
            {
                inputField.interactable = true;
                inputField.ActivateInputField();
            }

            SetFeedbackIcons(showCorrect: false, showWrong: false);
        }

        public void SetDisabled(bool preserveValue = true)
        {
            if (inputField != null)
            {
                inputField.interactable = false;
            }

            // Hide tick when disabled
            SetFeedbackIcons(showCorrect: false, showWrong: false);
        }

        public void SetDirectValue(string val)
        {
            _savedValue = val;
            if (inputField != null) inputField.text = val;
            if (valueDisplayText != null) valueDisplayText.text = val;
        }

        public void MarkCorrect(bool showCorrectIcon = true)
        {
            IsLockedCorrect = true;
            if (wrongFeedbackCoroutine != null) StopCoroutine(wrongFeedbackCoroutine);

            if (inputField != null)
            {
                _savedValue = inputField.text;
                inputField.interactable = false;
            }

            // Only show tick for the cell that just answered!
            SetFeedbackIcons(showCorrect: showCorrectIcon, showWrong: false);
        }

        public void MarkWrong()
        {
            if (wrongFeedbackCoroutine != null) StopCoroutine(wrongFeedbackCoroutine);
            wrongFeedbackCoroutine = StartCoroutine(ShowWrongFeedbackRoutine());
        }

        private IEnumerator ShowWrongFeedbackRoutine()
        {
            isClearingForFeedback = true;
            SetFeedbackIcons(showCorrect: false, showWrong: true);

            _savedValue = string.Empty;
            if (inputField != null)
            {
                inputField.text = string.Empty;
            }
            if (valueDisplayText != null)
            {
                valueDisplayText.text = string.Empty;
            }

            isClearingForFeedback = false;
            yield return new WaitForSeconds(1.2f);

            SetFeedbackIcons(showCorrect: false, showWrong: false);
            if (inputField != null && inputField.interactable)
            {
                inputField.ActivateInputField();
            }
        }

        private void OnTypingChanged(string text)
        {
            if (isClearingForFeedback) return;
            _savedValue = text;

            if (!string.IsNullOrEmpty(text) && wrongIcon != null && wrongIcon.activeSelf)
            {
                wrongIcon.SetActive(false);
            }
        }

        private void SetFeedbackIcons(bool showCorrect, bool showWrong)
        {
            if (correctIcon != null)
            {
                correctIcon.SetActive(showCorrect);
                if (showCorrect) correctIcon.transform.SetAsLastSibling();
            }

            if (wrongIcon != null)
            {
                wrongIcon.SetActive(showWrong);
                if (showWrong) wrongIcon.transform.SetAsLastSibling();
            }
        }
    }
}
