using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace TutorialFramework.RuntimeComponents
{
    [DisallowMultipleComponent]
    public class FormulaInputField : MonoBehaviour
    {
        [Header("Field Identity")]
        [Tooltip("Unique ID for this input box (e.g. 'c', 'd', 'l', 'l_prime', 'delta_f' or 'Field_1').")]
        [SerializeField] private string fieldId = "Field_1";

        [Header("UI Components")]
        [SerializeField] private TMP_InputField inputField;
        [SerializeField] private GameObject correctIcon;
        [SerializeField] private GameObject wrongIcon;

        public string FieldId => fieldId;
        public TMP_InputField InputField => EnsureInputField();
        public string CurrentValue => EnsureInputField() != null ? inputField.text : string.Empty;
        public bool IsLockedCorrect { get; private set; }

        private Action<string> onSubmitCallback;
        private Coroutine wrongFeedbackCoroutine;
        private bool isClearingForFeedback = false;

        private void Awake()
        {
            EnsureInputField();

            if (inputField != null)
            {
                inputField.characterLimit = 6;
                inputField.onValueChanged.AddListener(OnTypingChanged);
            }
        }

        private TMP_InputField EnsureInputField()
        {
            if (inputField == null)
            {
                inputField = GetComponent<TMP_InputField>();
                if (inputField == null) inputField = GetComponentInChildren<TMP_InputField>(true);
            }
            return inputField;
        }

        public void InitializeField()
        {
            IsLockedCorrect = false;
            EnsureInputField();

            if (inputField != null)
            {
                inputField.text = string.Empty;
                inputField.interactable = false;
            }

            SetFeedbackIcons(false, false);
        }

        public void ActivateForInput(Action<string> onSubmit)
        {
            onSubmitCallback = onSubmit;
            EnsureInputField();

            if (inputField != null)
            {
                inputField.interactable = true;
                inputField.ActivateInputField();
            }

            SetFeedbackIcons(false, false);
        }

        public void SetDisabled()
        {
            EnsureInputField();
            if (inputField != null)
            {
                inputField.interactable = false;
            }
        }

        public void SetDirectValue(string val)
        {
            EnsureInputField();
            if (inputField != null) inputField.text = val;
        }

        public void MarkCorrect()
        {
            IsLockedCorrect = true;
            if (wrongFeedbackCoroutine != null) StopCoroutine(wrongFeedbackCoroutine);

            EnsureInputField();
            if (inputField != null)
            {
                inputField.interactable = false;
            }

            SetFeedbackIcons(true, false);
        }

        public void MarkWrong()
        {
            if (wrongFeedbackCoroutine != null) StopCoroutine(wrongFeedbackCoroutine);
            wrongFeedbackCoroutine = StartCoroutine(ShowWrongFeedbackRoutine());
        }

        private IEnumerator ShowWrongFeedbackRoutine()
        {
            isClearingForFeedback = true;
            SetFeedbackIcons(false, true);

            EnsureInputField();
            if (inputField != null)
            {
                inputField.text = string.Empty;
            }

            isClearingForFeedback = false;
            yield return new WaitForSeconds(1.2f);

            SetFeedbackIcons(false, false);
            if (inputField != null && inputField.interactable)
            {
                inputField.ActivateInputField();
            }
        }

        private void OnTypingChanged(string text)
        {
            if (isClearingForFeedback) return;
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
