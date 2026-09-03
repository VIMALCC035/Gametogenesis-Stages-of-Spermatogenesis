using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using TutorialFramework.Core;

namespace TutorialFramework.RuntimeComponents
{
    public enum ActionInstructionType
    {
        GenericAction,
        Touch,
        Drag,
        ObservationTable,
        Calculation,
        Slider,
        Measurement
    }

    public class TutorialUIManager : MonoBehaviour
    {
        [Header("Primary Navigation")]
        [SerializeField] private Button nextButton;
        [SerializeField] private Button previousButton;
        [SerializeField] private TextMeshProUGUI pageNumberText;

        [Header("Single Unified Instruction Panel")]
        [Tooltip("The main Instruction Panel GameObject.")]
        [SerializeField] private GameObject instructionPanel;

        [Tooltip("CanvasGroup for smooth fade-in / fade-out animations (Auto-added if missing).")]
        [SerializeField] private CanvasGroup instructionCanvasGroup;

        [Tooltip("Duration of the fade transition in seconds.")]
        [SerializeField] private float fadeDuration = 0.18f;

        [Tooltip("The Background Image of the panel that swaps between Default BG and Action BG.")]
        [SerializeField] private Image instructionBackgroundImage;

        [Tooltip("The Text component for displaying instructions.")]
        [SerializeField] private TextMeshProUGUI instructionText;

        [Tooltip("The Action Icon Image (automatically disabled when not needed).")]
        [SerializeField] private Image actionIconImage;

        [Header("Background Sprites")]
        [SerializeField] private Sprite defaultBackgroundSprite;
        [SerializeField] private Sprite actionBackgroundSprite;

        [Header("Action Icons")]
        [SerializeField] private Sprite defaultActionIcon;
        [SerializeField] private Sprite touchIcon;
        [SerializeField] private Sprite dragIcon;
        [SerializeField] private Sprite observationTableIcon;
        [SerializeField] private Sprite calculationIcon;
        [SerializeField] private Sprite sliderIcon;
        [SerializeField] private Sprite measurementIcon;

        [Header("Integrated Interactive Panels")]
        [SerializeField] private EyeParallaxUI eyeParallaxUI;
        [SerializeField] private SliderAdjustmentUI sliderAdjustmentUI;
        [SerializeField] private CalculatorUI calculatorUI;
        [SerializeField] private List<ObservationTableUI> observationTables = new List<ObservationTableUI>();
        [SerializeField] private List<FormulaEvaluationUI> formulaPanels = new List<FormulaEvaluationUI>();
        [SerializeField] private List<GameObject> registeredPanels = new List<GameObject>();

        public EyeParallaxUI EyeParallax => eyeParallaxUI;
        public SliderAdjustmentUI SliderAdjustment => sliderAdjustmentUI;
        public CalculatorUI Calculator => calculatorUI;

        private Tween currentFadeTween;

        private void Awake()
        {
            EnsureCanvasGroup();
        }

        private void EnsureCanvasGroup()
        {
            if (instructionPanel != null && instructionCanvasGroup == null)
            {
                instructionCanvasGroup = instructionPanel.GetComponent<CanvasGroup>();
                if (instructionCanvasGroup == null)
                {
                    instructionCanvasGroup = instructionPanel.AddComponent<CanvasGroup>();
                }
            }
        }

        public void Initialize(Action onNextClicked, Action onPrevClicked)
        {
            EnsureCanvasGroup();

            Debug.Log("[TutorialUIManager] Initializing navigation buttons and instruction panel.");
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

        /// <summary>
        /// Displays default slide instruction. If text is null/empty, hides the panel.
        /// </summary>
        public void SetDefaultInstruction(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                HideInstructionPanel();
                return;
            }

            TransitionInstructionContent(() =>
            {
                if (instructionText != null)
                {
                    instructionText.text = text;
                }

                if (instructionBackgroundImage != null && defaultBackgroundSprite != null)
                {
                    instructionBackgroundImage.sprite = defaultBackgroundSprite;
                }

                if (actionIconImage != null)
                {
                    actionIconImage.gameObject.SetActive(false);
                }
            });
        }

        /// <summary>
        /// Displays action instruction. If text is null/empty, hides the panel.
        /// </summary>
        public void SetActionInstruction(string text, ActionInstructionType actionType = ActionInstructionType.GenericAction, Sprite customIcon = null)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                HideInstructionPanel();
                return;
            }

            TransitionInstructionContent(() =>
            {
                if (instructionText != null)
                {
                    instructionText.text = text;
                }

                if (instructionBackgroundImage != null && actionBackgroundSprite != null)
                {
                    instructionBackgroundImage.sprite = actionBackgroundSprite;
                }

                if (actionIconImage != null)
                {
                    Sprite chosenSprite = customIcon != null ? customIcon : GetSpriteForActionType(actionType);
                    if (chosenSprite != null)
                    {
                        actionIconImage.sprite = chosenSprite;
                        actionIconImage.gameObject.SetActive(true);
                    }
                    else
                    {
                        actionIconImage.gameObject.SetActive(false);
                    }
                }
            });
        }

        public void HideInstructionPanel()
        {
            EnsureCanvasGroup();
            if (instructionPanel == null) return;

            currentFadeTween?.Kill();

            if (instructionCanvasGroup != null && instructionPanel.activeSelf)
            {
                currentFadeTween = instructionCanvasGroup.DOFade(0f, fadeDuration).SetUpdate(true).OnComplete(() =>
                {
                    instructionPanel.SetActive(false);
                    if (instructionText != null) instructionText.text = string.Empty;
                });
            }
            else
            {
                instructionPanel.SetActive(false);
                if (instructionText != null) instructionText.text = string.Empty;
            }
        }

        private void TransitionInstructionContent(Action applyContent)
        {
            EnsureCanvasGroup();

            if (instructionPanel != null && !instructionPanel.activeSelf)
            {
                instructionPanel.SetActive(true);
            }

            if (instructionCanvasGroup == null)
            {
                applyContent?.Invoke();
                return;
            }

            currentFadeTween?.Kill();

            if (instructionCanvasGroup.alpha > 0.05f)
            {
                currentFadeTween = instructionCanvasGroup.DOFade(0f, fadeDuration).SetUpdate(true).OnComplete(() =>
                {
                    applyContent?.Invoke();
                    currentFadeTween = instructionCanvasGroup.DOFade(1f, fadeDuration).SetUpdate(true);
                });
            }
            else
            {
                instructionCanvasGroup.alpha = 0f;
                applyContent?.Invoke();
                currentFadeTween = instructionCanvasGroup.DOFade(1f, fadeDuration).SetUpdate(true);
            }
        }

        private Sprite GetSpriteForActionType(ActionInstructionType actionType)
        {
            switch (actionType)
            {
                case ActionInstructionType.Touch:
                    return touchIcon != null ? touchIcon : defaultActionIcon;
                case ActionInstructionType.Drag:
                    return dragIcon != null ? dragIcon : defaultActionIcon;
                case ActionInstructionType.ObservationTable:
                    return observationTableIcon != null ? observationTableIcon : defaultActionIcon;
                case ActionInstructionType.Calculation:
                    return calculationIcon != null ? calculationIcon : defaultActionIcon;
                case ActionInstructionType.Slider:
                    return sliderIcon != null ? sliderIcon : defaultActionIcon;
                case ActionInstructionType.Measurement:
                    return measurementIcon != null ? measurementIcon : defaultActionIcon;
                default:
                    return defaultActionIcon;
            }
        }

        public void SetInstruction(string text)
        {
            SetDefaultInstruction(text);
        }

        public void SetPageNumber(int currentNumber, int totalCount)
        {
            if (pageNumberText != null)
            {
                pageNumberText.text = totalCount > 0 ? $"{currentNumber}/{totalCount}" : $"{currentNumber}";
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

        public void ConfigurePanels(List<string> enablePanels, List<string> disablePanels)
        {
            if (enablePanels != null)
            {
                foreach (var panelName in enablePanels)
                {
                    if (string.IsNullOrWhiteSpace(panelName)) continue;
                    SetPanelState(panelName, true);
                }
            }

            if (disablePanels != null)
            {
                foreach (var panelName in disablePanels)
                {
                    if (string.IsNullOrWhiteSpace(panelName)) continue;
                    SetPanelState(panelName, false);
                }
            }
        }

        private void SetPanelState(string panelNameOrId, bool activeState)
        {
            bool found = false;

            var tutObj = TutorialObjectRegistry.Get(panelNameOrId);
            if (tutObj != null)
            {
                tutObj.gameObject.SetActive(activeState);
                found = true;
            }

            if (registeredPanels != null)
            {
                foreach (var p in registeredPanels)
                {
                    if (p != null && p.name.Equals(panelNameOrId, StringComparison.OrdinalIgnoreCase))
                    {
                        p.SetActive(activeState);
                        found = true;
                    }
                }
            }

            if (!found)
            {
                var allTransforms = transform.root.GetComponentsInChildren<Transform>(true);
                foreach (var t in allTransforms)
                {
                    if (t.gameObject.name.Equals(panelNameOrId, StringComparison.OrdinalIgnoreCase))
                    {
                        t.gameObject.SetActive(activeState);
                        found = true;
                    }
                }
            }
        }

        public ObservationTableUI GetObservationTable(string id)
        {
            var found = observationTables.Find(t => t != null && t.TableId.Equals(id, StringComparison.OrdinalIgnoreCase));
            if (found == null)
            {
#if UNITY_2023_1_OR_NEWER
                var allTables = FindObjectsByType<ObservationTableUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
                var allTables = FindObjectsOfType<ObservationTableUI>(true);
#endif
                foreach (var t in allTables)
                {
                    if (t.TableId.Equals(id, StringComparison.OrdinalIgnoreCase) || t.gameObject.name.Equals(id, StringComparison.OrdinalIgnoreCase))
                    {
                        if (!observationTables.Contains(t)) observationTables.Add(t);
                        return t;
                    }
                }
            }
            return found;
        }

        public FormulaEvaluationUI GetFormulaPanel(string id)
        {
            var found = formulaPanels.Find(p => p != null && p.PanelId.Equals(id, StringComparison.OrdinalIgnoreCase));
            if (found == null)
            {
#if UNITY_2023_1_OR_NEWER
                var allPanels = FindObjectsByType<FormulaEvaluationUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
                var allPanels = FindObjectsOfType<FormulaEvaluationUI>(true);
#endif
                foreach (var p in allPanels)
                {
                    if (p.PanelId.Equals(id, StringComparison.OrdinalIgnoreCase) || p.gameObject.name.Equals(id, StringComparison.OrdinalIgnoreCase))
                    {
                        if (!formulaPanels.Contains(p)) formulaPanels.Add(p);
                        Debug.Log($"[TutorialUIManager] Dynamically resolved FormulaEvaluationUI '{id}' (GameObject: '{p.gameObject.name}')");
                        return p;
                    }
                }
            }
            return found;
        }
    }
}
