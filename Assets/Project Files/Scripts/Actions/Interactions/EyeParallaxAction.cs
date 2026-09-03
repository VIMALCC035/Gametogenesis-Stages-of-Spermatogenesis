using System;
using System.Collections.Generic;
using UnityEngine;
using TutorialFramework.Core;

namespace TutorialFramework.Actions
{
    [Serializable]
    public class EyeParallaxAction : TutorialAction
    {
        [Header("Needle Sprites (Inside Render Texture or UI)")]
        [Tooltip("Object ID of the Top Inverted Needle Sprite.")]
        public string topNeedleObjectId;

        [Tooltip("Object ID of the Bottom Real Needle Sprite.")]
        public string bottomNeedleObjectId;

        [Header("Explicit Center Position Transforms")]
        [Tooltip("If true, uses the explicit center positions below. If false, reads current position.")]
        public bool useExplicitCenterPositions = true;

        [Tooltip("Exact Center Local Position for the Top Needle (at Eye Position = 0).")]
        public Vector3 topNeedleCenterPosition = new Vector3(0, 0.05f, 0);

        [Tooltip("Exact Center Local Position for the Bottom Needle (at Eye Position = 0).")]
        public Vector3 bottomNeedleCenterPosition = new Vector3(0, -0.05f, 0);

        [Tooltip("Optional: Anchor Object ID in scene to copy Top Needle center position from.")]
        public string topNeedleCenterAnchorId;

        [Tooltip("Optional: Anchor Object ID in scene to copy Bottom Needle center position from.")]
        public string bottomNeedleCenterAnchorId;

        [Header("Parallax State")]
        [Tooltip("If TRUE (Slide 6): Moving eye slider left makes needles separate, right stays aligned. If FALSE (Slide 8): Both needles stay aligned in BOTH directions.")]
        public bool hasParallax = true;

        [Tooltip("Initial eye slider position (-1 = Left, 0 = Center, +1 = Right).")]
        [Range(-1f, 1f)]
        public float initialEyePosition = 0.0f; // Starts at Center (0)

        [Tooltip("Base horizontal shift amount as eye moves.")]
        public float maxShiftAmount = 0.035f;

        [Tooltip("How much the top needle separates from the bottom needle when moving Left during parallax.")]
        public float leftSeparationMultiplier = 1.8f;

        [Header("UI Instructions & Subtitle")]
        public string subtitleText = "There may exist some parallax.";
        public string instructionText = "Move the eye position slider left and right to observe parallax.";

        [Header("Objects To Toggle On Success")]
        public List<string> objectsToEnableOnSuccess = new List<string>();
        public List<string> objectsToDisableOnSuccess = new List<string>();

        private Vector3 topBasePos;
        private Vector3 bottomBasePos;
        private bool isRectTop;
        private bool isRectBottom;

        public override void Execute(TutorialContext context, Action onComplete)
        {
            if (context.UI == null || context.UI.EyeParallax == null)
            {
                Debug.LogWarning("[EyeParallaxAction] EyeParallaxUI is not assigned in TutorialUIManager!");
                onComplete?.Invoke();
                return;
            }

            if (!string.IsNullOrEmpty(instructionText))
            {
                context.UI.SetInstruction(instructionText);
            }

            var topNeedle = TutorialObjectRegistry.Get(topNeedleObjectId);
            var bottomNeedle = TutorialObjectRegistry.Get(bottomNeedleObjectId);

            if (topNeedle == null || bottomNeedle == null)
            {
                Debug.LogError($"[EyeParallaxAction] Missing needle objects: '{topNeedleObjectId}' or '{bottomNeedleObjectId}'!");
                onComplete?.Invoke();
                return;
            }

            var topRt = topNeedle.GetComponent<RectTransform>();
            isRectTop = topRt != null;

            var bottomRt = bottomNeedle.GetComponent<RectTransform>();
            isRectBottom = bottomRt != null;

            // Resolve explicit Center Positions
            ResolveCenterPositions(topNeedle, bottomNeedle);

            context.UI.EyeParallax.Open(
                initialEyePosition,
                (eyeVal) =>
                {
                    ApplyEyeShift(topNeedle, bottomNeedle, eyeVal);
                },
                () =>
                {
                    Debug.Log("[EyeParallaxAction] Parallax check observation complete!");

                    ToggleObjects(objectsToEnableOnSuccess, true, "Enabled On Success");
                    ToggleObjects(objectsToDisableOnSuccess, false, "Disabled On Success");

                    onComplete?.Invoke();
                },
                subtitleText
            );
        }

        private void ResolveCenterPositions(TutorialObject topNeedle, TutorialObject bottomNeedle)
        {
            if (useExplicitCenterPositions)
            {
                topBasePos = topNeedleCenterPosition;
                bottomBasePos = bottomNeedleCenterPosition;

                if (!string.IsNullOrEmpty(topNeedleCenterAnchorId))
                {
                    var anchor = TutorialObjectRegistry.Get(topNeedleCenterAnchorId);
                    if (anchor != null) topBasePos = anchor.transform.localPosition;
                }

                if (!string.IsNullOrEmpty(bottomNeedleCenterAnchorId))
                {
                    var anchor = TutorialObjectRegistry.Get(bottomNeedleCenterAnchorId);
                    if (anchor != null) bottomBasePos = anchor.transform.localPosition;
                }
            }
            else
            {
                topBasePos = isRectTop ? (Vector3)topNeedle.GetComponent<RectTransform>().anchoredPosition : topNeedle.transform.localPosition;
                bottomBasePos = isRectBottom ? (Vector3)bottomNeedle.GetComponent<RectTransform>().anchoredPosition : bottomNeedle.transform.localPosition;
            }
        }

        private void ApplyEyeShift(TutorialObject topNeedle, TutorialObject bottomNeedle, float eyeValue)
        {
            float baseShift = eyeValue * maxShiftAmount;
            float topShift = baseShift;
            float bottomShift = baseShift;

            if (hasParallax)
            {
                if (eyeValue < 0f) // Moving LEFT -> Needles separate!
                {
                    float separation = Mathf.Abs(eyeValue) * leftSeparationMultiplier * maxShiftAmount;
                    topShift = baseShift - separation;
                }
                else // Center or RIGHT -> Stay aligned!
                {
                    topShift = baseShift;
                    bottomShift = baseShift;
                }
            }
            else
            {
                // NO PARALLAX: Both needles move together in both directions
                topShift = baseShift;
                bottomShift = baseShift;
            }

            // Apply to Top Needle
            Vector3 newTopPos = topBasePos;
            newTopPos.x = topBasePos.x + topShift;
            if (isRectTop)
            {
                var rt = topNeedle.GetComponent<RectTransform>();
                if (rt != null) rt.anchoredPosition = newTopPos;
            }
            else
            {
                topNeedle.transform.localPosition = newTopPos;
            }

            // Apply to Bottom Needle
            Vector3 newBottomPos = bottomBasePos;
            newBottomPos.x = bottomBasePos.x + bottomShift;
            if (isRectBottom)
            {
                var rt = bottomNeedle.GetComponent<RectTransform>();
                if (rt != null) rt.anchoredPosition = newBottomPos;
            }
            else
            {
                bottomNeedle.transform.localPosition = newBottomPos;
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
                }
            }
        }

        public override void ResetAction(TutorialContext context)
        {
            if (context.UI != null && context.UI.EyeParallax != null)
            {
                context.UI.EyeParallax.Close();
            }
        }
    }
}
