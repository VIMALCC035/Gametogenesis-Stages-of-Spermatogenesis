using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TutorialFramework.RuntimeComponents
{
    public enum RowPositionMode
    {
        [Tooltip("Calculates shift relative to Row 1 baseline (e.g. -52 -> 0, -176 -> +124px).")]
        ShiftFromRow1Baseline,

        [Tooltip("Directly sets bodyRect.anchoredPosition.y to the row value.")]
        ExactAnchoredPosY
    }

    [Serializable]
    public struct RowPositionSetting
    {
        [Tooltip("Identifier matching cell prefix (e.g. 'R1', 'R2', 'R3', 'R4', 'R5').")]
        public string rowId;

        [Tooltip("Row Y position (e.g. -52, -176, -301, -426, -550).")]
        public float rowY;
    }

    [ExecuteAlways]
    public class SimpleTableScrollbarController : MonoBehaviour
    {
        [Header("Hierarchy Targets (RectTransforms)")]
        [Tooltip("The Header RectTransform.")]
        [SerializeField] private RectTransform headerRect;

        [Tooltip("The Body / Rows Container RectTransform.")]
        [SerializeField] private RectTransform bodyRect;

        [Header("Scrollbars")]
        [SerializeField] private Scrollbar horizontalScrollbar;
        [SerializeField] private Scrollbar verticalScrollbar;

        [Header("Scroll Direction Inversion")]
        [SerializeField] private bool invertHorizontal = false;

        [Header("Horizontal Distance To Scroll")]
        [Tooltip("Total horizontal pixel distance to scroll left (264px calibrated).")]
        [SerializeField] private float totalHorizontalShift = 264f;

        [Header("Row Position Calculation Mode")]
        [SerializeField] private RowPositionMode positionMode = RowPositionMode.ShiftFromRow1Baseline;

        [Header("Row Y Positions")]
        [SerializeField] private List<RowPositionSetting> rowVerticalPositions = new List<RowPositionSetting>
        {
            new RowPositionSetting { rowId = "R1", rowY = -52f },
            new RowPositionSetting { rowId = "R2", rowY = -176f },
            new RowPositionSetting { rowId = "R3", rowY = -301f },
            new RowPositionSetting { rowId = "R4", rowY = -426f },
            new RowPositionSetting { rowId = "R5", rowY = -550f }
        };

        [Header("Baseline Starting Anchored Positions")]
        [SerializeField] private bool autoCaptureBaselineOnAwake = true;
        [SerializeField] private Vector2 headerBaseAnchoredPos;
        [SerializeField] private Vector2 bodyBaseAnchoredPos;

        private float currentHorizontalT = 0f;
        private float currentAppliedTargetRowY = -52f;

        private void Awake()
        {
            CaptureBaselines();
            if (rowVerticalPositions.Count > 0)
            {
                currentAppliedTargetRowY = rowVerticalPositions[0].rowY;
            }
        }

        private void OnEnable()
        {
            CaptureBaselines();

            if (horizontalScrollbar != null)
            {
                horizontalScrollbar.onValueChanged.RemoveAllListeners();
                horizontalScrollbar.onValueChanged.AddListener(OnHorizontalChanged);
            }

            if (verticalScrollbar != null)
            {
                verticalScrollbar.onValueChanged.RemoveAllListeners();
                verticalScrollbar.onValueChanged.AddListener(OnVerticalChanged);
            }

            UpdatePositions();
        }

        private void OnDisable()
        {
            if (horizontalScrollbar != null)
            {
                horizontalScrollbar.onValueChanged.RemoveListener(OnHorizontalChanged);
            }

            if (verticalScrollbar != null)
            {
                verticalScrollbar.onValueChanged.RemoveListener(OnVerticalChanged);
            }
        }

        private void CaptureBaselines()
        {
            if (!autoCaptureBaselineOnAwake) return;

            if (headerRect != null && headerBaseAnchoredPos == Vector2.zero)
            {
                headerBaseAnchoredPos = headerRect.anchoredPosition;
            }

            if (bodyRect != null && bodyBaseAnchoredPos == Vector2.zero)
            {
                bodyBaseAnchoredPos = bodyRect.anchoredPosition;
            }
        }

        public void UpdatePositions()
        {
            ApplyCombinedPositions(currentHorizontalT, currentAppliedTargetRowY);
        }

        private void OnHorizontalChanged(float t)
        {
            currentHorizontalT = t;
            ApplyCombinedPositions(currentHorizontalT, currentAppliedTargetRowY);
        }

        private void OnVerticalChanged(float t)
        {
            if (rowVerticalPositions == null || rowVerticalPositions.Count < 2) return;

            float r1 = rowVerticalPositions[0].rowY;
            float rLast = rowVerticalPositions[rowVerticalPositions.Count - 1].rowY;

            currentAppliedTargetRowY = Mathf.Lerp(r1, rLast, t);
            ApplyCombinedPositions(currentHorizontalT, currentAppliedTargetRowY);
        }

        private void ApplyCombinedPositions(float horizT, float targetRowY)
        {
            float effectiveHorizT = invertHorizontal ? (1f - horizT) : horizT;
            float shiftX = -effectiveHorizT * totalHorizontalShift;

            if (headerRect != null)
            {
                Vector2 hPos = headerRect.anchoredPosition;
                hPos.x = headerBaseAnchoredPos.x + shiftX;
                headerRect.anchoredPosition = hPos;
            }

            if (bodyRect != null)
            {
                float calculatedBodyY;

                if (positionMode == RowPositionMode.ExactAnchoredPosY)
                {
                    calculatedBodyY = targetRowY;
                }
                else
                {
                    float row1Y = rowVerticalPositions.Count > 0 ? rowVerticalPositions[0].rowY : -52f;
                    float shiftUp = Mathf.Abs(row1Y - targetRowY);
                    calculatedBodyY = bodyBaseAnchoredPos.y + shiftUp;
                }

                Vector2 bPos = bodyRect.anchoredPosition;
                bPos.x = bodyBaseAnchoredPos.x + shiftX;
                bPos.y = calculatedBodyY;
                bodyRect.anchoredPosition = bPos;
            }
        }

        public void FocusOnCell(string cellId, Transform targetCellTransform = null)
        {
            if (string.IsNullOrEmpty(cellId) && targetCellTransform != null)
            {
                var cellComp = targetCellTransform.GetComponent<ObservationTableCell>();
                if (cellComp != null) cellId = cellComp.CellId;
            }

            // 1. Horizontal Column Target (0 = Left Columns, 1 = Right Columns)
            float targetHoriz = 0f;
            if (!string.IsNullOrEmpty(cellId))
            {
                string lower = cellId.ToLower();
                if (lower.Contains("observed") || lower.Contains("corrected") || lower.Contains("focal") || lower.Contains("delta") || lower.Contains("r'") || lower.Contains("col4") || lower.Contains("col5"))
                {
                    targetHoriz = 1f;
                }
                else
                {
                    targetHoriz = 0f;
                }
            }

            // 2. Exact Vertical Row Lookup directly from rowY
            float targetRowY = -52f;

            if (!string.IsNullOrEmpty(cellId) && rowVerticalPositions != null && rowVerticalPositions.Count > 0)
            {
                string lower = cellId.ToLower();
                bool found = false;

                foreach (var setting in rowVerticalPositions)
                {
                    if (string.IsNullOrEmpty(setting.rowId)) continue;

                    if (lower.StartsWith(setting.rowId.ToLower()) || lower.Contains(setting.rowId.ToLower()))
                    {
                        targetRowY = setting.rowY;
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    targetRowY = rowVerticalPositions[0].rowY;
                }
            }

            currentHorizontalT = targetHoriz;
            currentAppliedTargetRowY = targetRowY;

            if (horizontalScrollbar != null) horizontalScrollbar.value = targetHoriz;

            if (verticalScrollbar != null && rowVerticalPositions.Count >= 2)
            {
                float r1 = rowVerticalPositions[0].rowY;
                float rLast = rowVerticalPositions[rowVerticalPositions.Count - 1].rowY;
                verticalScrollbar.value = Mathf.InverseLerp(r1, rLast, targetRowY);
            }

            ApplyCombinedPositions(currentHorizontalT, currentAppliedTargetRowY);

            Debug.Log($"[SimpleTableScrollbarController] Focused on '{cellId}' -> Target Row Y: {targetRowY} (Body Y: {bodyRect?.anchoredPosition.y})");
        }

        public void FocusOnCell(Transform targetCell)
        {
            if (targetCell == null) return;
            var cellComp = targetCell.GetComponent<ObservationTableCell>();
            string id = cellComp != null ? cellComp.CellId : targetCell.name;
            FocusOnCell(id, targetCell);
        }

        public void ResetToTopLeft()
        {
            currentHorizontalT = 0f;
            if (rowVerticalPositions.Count > 0)
            {
                currentAppliedTargetRowY = rowVerticalPositions[0].rowY;
            }
            if (horizontalScrollbar != null) horizontalScrollbar.value = 0f;
            if (verticalScrollbar != null) verticalScrollbar.value = 0f;
            ApplyCombinedPositions(0f, currentAppliedTargetRowY);
        }
    }
}
