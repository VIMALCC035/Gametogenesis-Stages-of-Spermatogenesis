using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace TutorialFramework.RuntimeComponents
{
    /// <summary>
    /// Lightweight, dedicated Navigation UI Manager containing only Next/Previous buttons and page counter.
    /// Free of any other interactive or calculation panels.
    /// </summary>
    [DisallowMultipleComponent]
    public class SlideNavigationUI : MonoBehaviour
    {
        [Header("Primary Navigation Buttons")]
        [Tooltip("The Next slide / step button.")]
        [SerializeField] private Button nextButton;

        [Tooltip("The Previous slide / step button.")]
        [SerializeField] private Button previousButton;
        [SerializeField] private TextMeshProUGUI pageNumberText;

        public Button NextButton => nextButton;
        public Button PreviousButton => previousButton;
        public TextMeshProUGUI PageNumberText => pageNumberText;

        public void Initialize(Action onNextClicked, Action onPrevClicked)
        {
            if (nextButton != null)
            {
                nextButton.onClick.RemoveAllListeners();
                nextButton.onClick.AddListener(() => onNextClicked?.Invoke());
            }

            if (previousButton != null)
            {
                previousButton.onClick.RemoveAllListeners();
                previousButton.onClick.AddListener(() => onPrevClicked?.Invoke());
            }
        }

        public void SetNextButtonState(bool enabled)
        {
            if (nextButton != null)
            {
                nextButton.interactable = enabled;
            }
        }

        public void SetPrevButtonState(bool enabled)
        {
            if (previousButton != null)
            {
                previousButton.interactable = enabled;
            }
        }

        public void SetPageNumber(int currentNumber, int totalCount)
        {
            if (pageNumberText != null)
            {
                pageNumberText.text = totalCount > 0 
                    ? $"{currentNumber}/{totalCount}" 
                    : $"{currentNumber}";
            }
        }
    }
}
