using System.Collections.Generic;
using UnityEngine;
using TutorialFramework.Actions;

namespace TutorialFramework.Data
{
    public enum NextButtonRule
    {
        RequireSlideCompletion,
        AlwaysEnabled
    }

    [CreateAssetMenu(fileName = "Slide_001", menuName = "Tutorial Framework/Slide Definition")]
    public class SlideDefinition : ScriptableObject
    {
        [Header("Slide Identity")]
        [Tooltip("Stable identifier. Automatically syncs with the asset filename if left blank.")]
        public string slideId;

        [Header("Navigation Graph")]
        [Tooltip("Previous Slide definition in the sequence.")]
        public SlideDefinition previousSlide;
        [Tooltip("Next Slide definition in the sequence.")]
        public SlideDefinition nextSlide;

        [Header("UI Panels Configuration")]
        [Tooltip("Panel GameObjects to activate when this slide opens.")]
        public List<string> panelsToEnable = new List<string>();
        [Tooltip("Panel GameObjects to deactivate when this slide opens.")]
        public List<string> panelsToDisable = new List<string>();

        [Header("Instruction")]
        [TextArea(2, 5)]
        public string defaultInstruction;

        [Header("Navigation Rules")]
        public NextButtonRule nextButtonRule = NextButtonRule.RequireSlideCompletion;

        [Header("State Setup (Baseline / Overrides)")]
        [Tooltip("Direct object transforms/active states applied upon entering this slide.")]
        public List<ObjectTransformState> baselineStateOverrides = new List<ObjectTransformState>();

        [Header("Polymorphic Actions")]
        [SerializeReference]
        [SubclassSelector]
        public List<TutorialAction> actions = new List<TutorialAction>();

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Automatically sync slideId to asset filename if empty or newly created
            if (string.IsNullOrWhiteSpace(slideId))
            {
                slideId = name;
            }
        }
#endif
    }
}
