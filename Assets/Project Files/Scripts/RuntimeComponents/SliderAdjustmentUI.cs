using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace TutorialFramework.RuntimeComponents
{
    public class SliderAdjustmentUI : MonoBehaviour
    {
        [Header("UI Controls")]
        [SerializeField] private Slider adjustmentSlider;
        [SerializeField] private GameObject container;

        private Action<float> onValueChangedCallback;
        private Action onCorrectCallback;
        private float targetVal;
        private float allowedTolerance;
        private bool isCompleted;

        private void Awake()
        {
            if (adjustmentSlider != null)
            {
                adjustmentSlider.onValueChanged.AddListener(OnSliderMoved);
            }
        }

        public void Open(
            string title, 
            float initialValue, 
            float minVal, 
            float maxVal, 
            float targetValue, 
            float tolerance, 
            Action<float> onValueChanged, 
            Action onCorrect)
        {
            targetVal = targetValue;
            allowedTolerance = tolerance;
            onValueChangedCallback = onValueChanged;
            onCorrectCallback = onCorrect;
            isCompleted = false;
            if (adjustmentSlider != null)
            {
                adjustmentSlider.minValue = minVal;
                adjustmentSlider.maxValue = maxVal;
                adjustmentSlider.value = initialValue;
            }

            if (container != null) container.SetActive(true);
            else gameObject.SetActive(true);

            onValueChangedCallback?.Invoke(initialValue);
        }

        public void Close()
        {
            if (container != null) container.SetActive(false);
            else gameObject.SetActive(false);

            onValueChangedCallback = null;
            onCorrectCallback = null;
        }

        private void OnSliderMoved(float value)
        {
            onValueChangedCallback?.Invoke(value);

            if (!isCompleted && Mathf.Abs(value - targetVal) <= allowedTolerance)
            {
                isCompleted = true;
                var cb = onCorrectCallback;
                onCorrectCallback = null;
                cb?.Invoke();
            }
        }
    }
}
