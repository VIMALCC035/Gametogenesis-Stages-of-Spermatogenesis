using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace TutorialFramework.RuntimeComponents
{
    public class EyeParallaxUI : MonoBehaviour
    {
        [Header("UI Controls")]
        [SerializeField] private Slider eyePositionSlider;
        [SerializeField] private GameObject frontViewPanel;
        [SerializeField] private Button checkParallexBtn;
        private Action<float> onEyeMovedCallback;
        private Action onObservationComplete;
        private bool hasMovedLeft;
        private bool hasMovedRight;
        private bool isCompleted;

        private void Awake()
        {
            if (eyePositionSlider != null)
            {
                eyePositionSlider.onValueChanged.AddListener(OnSliderChanged);
            }
        }

        private void Start()
        {
            checkParallexBtn.onClick.AddListener(() =>
            {
                frontViewPanel.SetActive(true);
                checkParallexBtn.gameObject.SetActive(false);
            });
        }
        public void Open(
            float initialSliderVal, 
            Action<float> onEyeMoved, 
            Action onComplete, 
            string subtitle = "Move eye position left and right to observe parallax.")
        {
            onEyeMovedCallback = onEyeMoved;
            onObservationComplete = onComplete;
            hasMovedLeft = false;
            hasMovedRight = false;
            isCompleted = false;
            if (eyePositionSlider != null)
            {
                eyePositionSlider.minValue = -1.0f; // Left
                eyePositionSlider.maxValue = 1.0f;  // Right
                eyePositionSlider.value = initialSliderVal;
            }

            onEyeMovedCallback?.Invoke(initialSliderVal);
        }

        public void Close()
        {
            onEyeMovedCallback = null;
            onObservationComplete = null;
        }

        private void OnSliderChanged(float val)
        {
            onEyeMovedCallback?.Invoke(val);

            if (val <= -0.7f) hasMovedLeft = true;
            if (val >= 0.7f) hasMovedRight = true;

            // When student has checked both left and right, mark observation complete
            if (!isCompleted && hasMovedLeft && hasMovedRight)
            {
                isCompleted = true;
                var cb = onObservationComplete;
                onObservationComplete = null;
                cb?.Invoke();
            }
        }
    }
}
