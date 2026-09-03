using System;
using System.Collections.Generic;
using UnityEngine;
using TutorialFramework.Core;
using TutorialFramework.RuntimeComponents;

namespace TutorialFramework.Actions
{
    public enum AxisDirection
    {
        X_Axis, // Left / Right (Lateral)
        Y_Axis, // Up / Down (Height)
        Z_Axis  // Forward / Backward
    }

    [Serializable]
    public class SliderAdjustmentAction : TutorialAction
    {
        [Header("Target Object To Move")]
        [Tooltip("Object ID in TutorialObjectRegistry to move (3D GameObject or 2D UI Image).")]
        public string controlledObjectId;

        [Tooltip("Movement axis controlled by the slider.")]
        public AxisDirection movementAxis = AxisDirection.Y_Axis;

        [Header("Slider Range & Values")]
        public float minSliderValue = -0.1f;
        public float maxSliderValue = 0.1f;
        public float initialValue = -0.08f;

        [Header("Target Answer & Tolerance")]
        [Tooltip("The correct target value where needle tips / alignment is achieved.")]
        public float targetAnswerValue = 0.0f;

        [Tooltip("Allowed tolerance range (e.g., 0.005 for high precision).")]
        public float allowedTolerance = 0.01f;

        [Header("UI Instructions & Label")]
        [Tooltip("Title shown on the slider UI (e.g., 'Adjust Needle Height' or 'Lateral Shift').")]
        public string sliderTitle = "Adjust Needle Position";

        [Tooltip("Instruction text displayed on the main UI panel.")]
        public string instructionText = "Use the slider to align the needle tip with its inverted image.";

        [Header("Objects To Toggle On Success")]
        [Tooltip("Object IDs to ACTIVATE when correct alignment is reached.")]
        public List<string> objectsToEnableOnSuccess = new List<string>();

        [Tooltip("Object IDs to DEACTIVATE when correct alignment is reached.")]
        public List<string> objectsToDisableOnSuccess = new List<string>();

        private Vector3 initialBasePos;
        private bool isRectTransform;

        public override void Execute(TutorialContext context, Action onComplete)
        {
            if (context.UI == null || context.UI.SliderAdjustment == null)
            {
                Debug.LogWarning("[SliderAdjustmentAction] SliderAdjustmentUI is not assigned in TutorialUIManager!");
                onComplete?.Invoke();
                return;
            }

            if (!string.IsNullOrEmpty(instructionText))
            {
                context.UI.SetActionInstruction(instructionText, ActionInstructionType.Slider);
            }

            var target = TutorialObjectRegistry.Get(controlledObjectId);
            if (target == null)
            {
                Debug.LogError($"[SliderAdjustmentAction] Controlled object '{controlledObjectId}' not found in registry!");
                onComplete?.Invoke();
                return;
            }

            var rectTransform = target.GetComponent<RectTransform>();
            isRectTransform = rectTransform != null;
            initialBasePos = isRectTransform ? (Vector3)rectTransform.anchoredPosition : target.transform.localPosition;

            context.UI.SliderAdjustment.Open(
                sliderTitle,
                initialValue,
                minSliderValue,
                maxSliderValue,
                targetAnswerValue,
                allowedTolerance,
                (val) =>
                {
                    ApplyMovement(target, val);
                },
                () =>
                {
                    Debug.Log($"[SliderAdjustmentAction] Target alignment reached for '{controlledObjectId}'!");

                    ToggleObjects(objectsToEnableOnSuccess, true, "Enabled On Success");
                    ToggleObjects(objectsToDisableOnSuccess, false, "Disabled On Success");

                    onComplete?.Invoke();
                }
            );
        }

        private void ApplyMovement(TutorialObject target, float sliderVal)
        {
            if (target == null) return;

            Vector3 newPos = initialBasePos;

            switch (movementAxis)
            {
                case AxisDirection.X_Axis:
                    newPos.x = initialBasePos.x + sliderVal;
                    break;
                case AxisDirection.Y_Axis:
                    newPos.y = initialBasePos.y + sliderVal;
                    break;
                case AxisDirection.Z_Axis:
                    newPos.z = initialBasePos.z + sliderVal;
                    break;
            }

            if (isRectTransform)
            {
                var rt = target.GetComponent<RectTransform>();
                if (rt != null) rt.anchoredPosition = newPos;
            }
            else
            {
                target.transform.localPosition = newPos;
            }
        }

        private void ToggleObjects(List<string> objectIds, bool activeState, string logContext)
        {
            if (objectIds == null) return;
            foreach (var id in objectIds)
            {
                if (string.IsNullOrWhiteSpace(id)) continue;
                var obj = TutorialObjectRegistry.Get(id);
                if (obj != null)
                {
                    obj.gameObject.SetActive(activeState);
                    Debug.Log($"[SliderAdjustmentAction] {logContext} -> GameObject '{id}' (activeSelf={activeState})");
                }
            }
        }

        public override void ResetAction(TutorialContext context)
        {
            if (context.UI != null && context.UI.SliderAdjustment != null)
            {
                context.UI.SliderAdjustment.Close();
            }
        }
    }
}
