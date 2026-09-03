using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace TutorialFramework.RuntimeComponents
{
    public class CalculatorUI : MonoBehaviour
    {
        [SerializeField] private TMP_InputField inputDisplay;
        [SerializeField] private Button submitButton;
        [SerializeField] private GameObject container;

        private float expectedAnswer;
        private float tolerance;
        private Action onCorrect;

        private void Awake()
        {
            if (submitButton != null)
            {
                submitButton.onClick.AddListener(OnSubmitClicked);
            }
        }

        public void Open(float answer, float tol, Action successCallback)
        {
            expectedAnswer = answer;
            tolerance = tol;
            onCorrect = successCallback;

            if (inputDisplay != null) inputDisplay.text = string.Empty;
            if (container != null) container.SetActive(true);
            else gameObject.SetActive(true);
        }

        public void Close()
        {
            if (container != null) container.SetActive(false);
            else gameObject.SetActive(false);
            onCorrect = null;
        }

        public void OnSubmitClicked()
        {
            if (inputDisplay == null) return;

            if (float.TryParse(inputDisplay.text, out float enteredVal))
            {
                if (Mathf.Abs(enteredVal - expectedAnswer) <= tolerance)
                {
                    var cb = onCorrect;
                    onCorrect = null;
                    cb?.Invoke();
                }
                else
                {
                    Debug.Log($"[CalculatorUI] Incorrect value entered: {enteredVal}. Expected: {expectedAnswer} (+/- {tolerance})");
                }
            }
        }
    }
}
