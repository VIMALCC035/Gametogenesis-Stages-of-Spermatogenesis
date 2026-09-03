using System;
using System.Collections.Generic;
using UnityEngine;
using TutorialFramework.Data;
using TutorialFramework.Actions;
using TutorialFramework.RuntimeComponents;

namespace TutorialFramework.Core
{
    public enum TutorialMode
    {
        Production,
        Testing
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(TutorialStateManager))]
    public class TutorialController : MonoBehaviour
    {
        [Header("Mode & Entry")]
        [SerializeField] private TutorialMode mode = TutorialMode.Production;
        [SerializeField] private TutorialSequenceSO sequenceData;
        [SerializeField] private SlideDefinition startSlide;
        [SerializeField] private SlideDefinition directTestSlide;

        [Header("Runtime Service Providers")]
        [SerializeField] private TutorialUIManager uiManager;
        [SerializeField] private SlideNavigationUI navigationUI;
        [SerializeField] private TutorialCameraController cameraController;
        [SerializeField] private HighlightManager highlightManager;

        public SlideDefinition CurrentSlide { get; private set; }
        public bool IsSlideCompleted { get; private set; }
        public TutorialAction CurrentExecutingAction { get; private set; }

        private TutorialStateManager stateManager;
        private TutorialContext context;
        private int totalSlideCount = 0;
        private int currentSlideIndex = 1;

        private readonly HashSet<SlideDefinition> completedSlides = new HashSet<SlideDefinition>();

        private void Awake()
        {
            TutorialObjectRegistry.InitializeAll();

            if (navigationUI == null)
            {
#if UNITY_2023_1_OR_NEWER
                navigationUI = FindFirstObjectByType<SlideNavigationUI>(FindObjectsInactive.Include);
#else
                navigationUI = FindObjectOfType<SlideNavigationUI>(true);
#endif
            }

            stateManager = GetComponent<TutorialStateManager>();
#if UNITY_2023_1_OR_NEWER
            var allSceneObjects = FindObjectsByType<TutorialObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
            var allSceneObjects = FindObjectsOfType<TutorialObject>(true);
#endif
            stateManager.CaptureInitialBaseline(allSceneObjects);

            CalculateTotalSlides();
        }

        private void Start()
        {
            if (navigationUI != null)
            {
                navigationUI.Initialize(OnNextRequested, OnPreviousRequested);
            }

            if (uiManager != null)
            {
                uiManager.Initialize(OnNextRequested, OnPreviousRequested);
            }

            if (mode == TutorialMode.Testing && directTestSlide != null)
            {
                LoadDirectTestSlide(directTestSlide);
            }
            else if (startSlide != null)
            {
                EnterSlide(startSlide, EntryDirection.Forward);
            }
            else if (sequenceData != null && sequenceData.rootSlide != null)
            {
                EnterSlide(sequenceData.rootSlide, EntryDirection.Forward);
            }
            else
            {
                Debug.LogWarning("[TutorialController] No StartSlide or Sequence assigned!");
            }
        }

        private void CalculateTotalSlides()
        {
            if (sequenceData != null && sequenceData.allSlides != null && sequenceData.allSlides.Count > 0)
            {
                totalSlideCount = sequenceData.allSlides.Count;
                return;
            }

            var current = startSlide ?? (sequenceData != null ? sequenceData.rootSlide : null);
            int count = 0;
            var visited = new HashSet<SlideDefinition>();

            while (current != null && !visited.Contains(current))
            {
                visited.Add(current);
                count++;
                current = current.nextSlide;
            }

            totalSlideCount = count;
        }

        public void EnterSlide(SlideDefinition slide, EntryDirection direction)
        {
            if (slide == null) return;

            // 1. Teardown active actions on departing slide
            if (CurrentSlide != null && CurrentSlide.actions != null)
            {
                foreach (var action in CurrentSlide.actions)
                {
                    action?.ResetAction(context);
                }
            }

            // 2. Set new current slide
            CurrentSlide = slide;
            CurrentExecutingAction = null;
            context = new TutorialContext(this, uiManager, cameraController, highlightManager, stateManager, direction);

            // 3. Update Slide Index Count
            UpdateCurrentSlideIndex(slide, direction);

            // 4. Check if slide was already completed or moving backward
            bool wasAlreadyCompleted = completedSlides.Contains(slide) || direction == EntryDirection.Backward;
            IsSlideCompleted = wasAlreadyCompleted;

            // 5. Restore global panel states & visibility
            if (direction == EntryDirection.Backward)
            {
                if (!stateManager.RestoreSlideSnapshot(slide))
                {
                    stateManager.ApplySlideStateOverrides(slide);
                }
            }
            else
            {
                stateManager.ApplySlideStateOverrides(slide);
            }

            // 6. Configure UI (On backward/completed state, DO NOT re-enable initial prompt panels)
            bool isNextEnabled = slide.nextSlide != null && (IsSlideCompleted || slide.nextButtonRule == NextButtonRule.AlwaysEnabled);
            bool isPrevEnabled = slide.previousSlide != null;

            if (navigationUI != null)
            {
                navigationUI.SetPageNumber(currentSlideIndex, totalSlideCount);
                navigationUI.SetPrevButtonState(isPrevEnabled);
                navigationUI.SetNextButtonState(isNextEnabled);
            }

            if (uiManager != null)
            {
                uiManager.SetDefaultInstruction(slide.defaultInstruction);
                uiManager.SetPageNumber(currentSlideIndex, totalSlideCount);

                if (!wasAlreadyCompleted && direction != EntryDirection.Backward)
                {
                    uiManager.ConfigurePanels(slide.panelsToEnable, slide.panelsToDisable);
                }
                else
                {
                    uiManager.ConfigurePanels(null, slide.panelsToDisable);
                    uiManager.ConfigurePanels(null, slide.panelsToEnable);
                }

                uiManager.SetPrevButtonState(isPrevEnabled);
                uiManager.SetNextButtonState(isNextEnabled);
            }

            // 7. Execute Slide Actions
            if (wasAlreadyCompleted)
            {
                ExecuteCompletedSlideActions(slide);
            }
            else
            {
                ExecuteSlideActionsSequentially(slide);
            }
        }

        private void ExecuteCompletedSlideActions(SlideDefinition slide)
        {
            if (slide.actions == null) return;

            TutorialAction lastActionWithInstruction = null;

            foreach (var action in slide.actions)
            {
                if (action == null) continue;

                // Animate camera so the viewpoint is right for this slide
                if (action is CameraAction)
                {
                    action.Execute(context, null);
                }
                else
                {
                    action.FastForward(context);
                }

                if (HasActionInstruction(action))
                {
                    lastActionWithInstruction = action;
                }
            }

            // Display the LAST action's instruction & icon on the UI
            if (lastActionWithInstruction != null && uiManager != null)
            {
                ApplyActionInstruction(lastActionWithInstruction);
            }

            bool canGoNext = slide.nextSlide != null;

            if (navigationUI != null)
            {
                navigationUI.SetNextButtonState(canGoNext);
            }

            if (uiManager != null)
            {
                uiManager.SetNextButtonState(canGoNext);
            }
        }

        private bool HasActionInstruction(TutorialAction action)
        {
            if (action is TouchAction t && !string.IsNullOrEmpty(t.instructionText)) return true;
            if (action is DragDropAction d && !string.IsNullOrEmpty(d.instructionText)) return true;
            if (action is ObservationTableAction o && (!string.IsNullOrEmpty(o.defaultInstruction) || (o.sequentialSteps != null && o.sequentialSteps.Count > 0))) return true;
            if (action is FormulaEvaluationAction f && !string.IsNullOrEmpty(f.instructionText)) return true;
            if (action is EvaluationSetupAction setup && !string.IsNullOrEmpty(setup.instructionText)) return true;
            if (action is SliderAdjustmentAction s && !string.IsNullOrEmpty(s.instructionText)) return true;
            if (action is EvaluationAction e && !string.IsNullOrEmpty(e.instructionText)) return true;
            return false;
        }

        private void ApplyActionInstruction(TutorialAction action)
        {
            if (action is TouchAction touch)
            {
                uiManager.SetActionInstruction(touch.instructionText, ActionInstructionType.Touch);
            }
            else if (action is DragDropAction drag)
            {
                uiManager.SetActionInstruction(drag.instructionText, ActionInstructionType.Drag);
            }
            else if (action is ObservationTableAction obs)
            {
                string text = obs.defaultInstruction;
                if (obs.sequentialSteps != null && obs.sequentialSteps.Count > 0)
                {
                    text = obs.sequentialSteps[obs.sequentialSteps.Count - 1].instructionText;
                }
                uiManager.SetActionInstruction(text, ActionInstructionType.ObservationTable);
            }
            else if (action is FormulaEvaluationAction formula)
            {
                uiManager.SetActionInstruction(formula.instructionText, ActionInstructionType.Calculation);
            }
            else if (action is EvaluationSetupAction evalSetup)
            {
                uiManager.SetActionInstruction(evalSetup.instructionText, ActionInstructionType.GenericAction);
            }
            else if (action is SliderAdjustmentAction slider)
            {
                uiManager.SetActionInstruction(slider.instructionText, ActionInstructionType.Slider);
            }
            else if (action is EvaluationAction eval)
            {
                uiManager.SetActionInstruction(eval.instructionText, ActionInstructionType.GenericAction);
            }
        }

        private void UpdateCurrentSlideIndex(SlideDefinition slide, EntryDirection direction)
        {
            if (sequenceData != null && sequenceData.allSlides != null && sequenceData.allSlides.Count > 0)
            {
                int idx = sequenceData.allSlides.IndexOf(slide);
                if (idx >= 0)
                {
                    currentSlideIndex = idx + 1;
                    return;
                }
            }

            int depth = 1;
            var ptr = slide.previousSlide;
            var visited = new HashSet<SlideDefinition> { slide };

            while (ptr != null && !visited.Contains(ptr))
            {
                visited.Add(ptr);
                depth++;
                ptr = ptr.previousSlide;
            }

            currentSlideIndex = depth;
        }

        private void ExecuteSlideActionsSequentially(SlideDefinition slide)
        {
            if (slide.actions == null || slide.actions.Count == 0)
            {
                MarkSlideComplete();
                return;
            }

            ExecuteActionAtIndex(0, slide);
        }

        private void ExecuteActionAtIndex(int index, SlideDefinition slide)
        {
            if (index >= slide.actions.Count)
            {
                MarkSlideComplete();
                return;
            }

            var action = slide.actions[index];
            if (action == null)
            {
                ExecuteActionAtIndex(index + 1, slide);
                return;
            }

            CurrentExecutingAction = action;

            if (action.WaitForCompletion)
            {
                action.Execute(context, () =>
                {
                    ExecuteActionAtIndex(index + 1, slide);
                });
            }
            else
            {
                action.Execute(context, null);
                ExecuteActionAtIndex(index + 1, slide);
            }
        }

        private void MarkSlideComplete()
        {
            IsSlideCompleted = true;

            if (CurrentSlide != null)
            {
                completedSlides.Add(CurrentSlide);
                stateManager.CaptureSlideSnapshot(CurrentSlide);
            }

            bool canGoNext = CurrentSlide != null && CurrentSlide.nextSlide != null;

            if (navigationUI != null)
            {
                navigationUI.SetNextButtonState(canGoNext);
            }

            if (uiManager != null)
            {
                uiManager.SetNextButtonState(canGoNext);
            }
        }

        public void OnNextRequested()
        {
            if (!IsSlideCompleted && CurrentSlide.nextButtonRule == NextButtonRule.RequireSlideCompletion)
            {
                return;
            }

            if (CurrentSlide != null)
            {
                completedSlides.Add(CurrentSlide);
                stateManager.CaptureSlideSnapshot(CurrentSlide);
            }

            if (CurrentSlide != null && CurrentSlide.nextSlide != null)
            {
                EnterSlide(CurrentSlide.nextSlide, EntryDirection.Forward);
            }
        }

        public void OnPreviousRequested()
        {
            if (CurrentSlide != null)
            {
                stateManager.CaptureSlideSnapshot(CurrentSlide);
            }

            if (CurrentSlide != null && CurrentSlide.previousSlide != null)
            {
                EnterSlide(CurrentSlide.previousSlide, EntryDirection.Backward);
            }
        }

        public void LoadDirectTestSlide(SlideDefinition testSlide)
        {
            if (testSlide == null) return;
            stateManager.ResetToBaseline();
            EnterSlide(testSlide, EntryDirection.DirectTest);
        }
    }
}
