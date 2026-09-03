using System.Collections.Generic;
using UnityEngine;
using TutorialFramework.Data;

namespace TutorialFramework.Core
{
    public class TutorialStateManager : MonoBehaviour
    {
        [Header("State Settings")]
        [Tooltip("If false (Recommended), only active/inactive visibility is restored, leaving 3D transforms untouched.")]
        [SerializeField] private bool restoreTransforms = false;

        private readonly Dictionary<string, ObjectTransformState> initialBaseline = new Dictionary<string, ObjectTransformState>();
        private readonly Dictionary<string, SlideStateSnapshot> slideSnapshots = new Dictionary<string, SlideStateSnapshot>();

        public void CaptureInitialBaseline(IEnumerable<TutorialObject> allObjects)
        {
            initialBaseline.Clear();
            foreach (var obj in allObjects)
            {
                if (obj == null) continue;
                initialBaseline[obj.ObjectId] = ObjectTransformState.Capture(obj);
            }
        }

        public void ResetToBaseline()
        {
            foreach (var kvp in initialBaseline)
            {
                ApplyState(kvp.Value);
            }
        }

        public void CaptureSlideSnapshot(SlideDefinition slide)
        {
            if (slide == null || string.IsNullOrEmpty(slide.slideId)) return;

#if UNITY_2023_1_OR_NEWER
            var activeObjects = Object.FindObjectsByType<TutorialObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
            var activeObjects = Object.FindObjectsOfType<TutorialObject>(true);
#endif
            var states = new ObjectTransformState[activeObjects.Length];

            for (int i = 0; i < activeObjects.Length; i++)
            {
                states[i] = ObjectTransformState.Capture(activeObjects[i]);
            }

            slideSnapshots[slide.slideId] = new SlideStateSnapshot
            {   
                slideId = slide.slideId,
                objectStates = states
            };

            Debug.Log($"[TutorialStateManager] Captured visibility state for Slide: '{slide.slideId}' ({states.Length} objects)");
        }

        public bool RestoreSlideSnapshot(SlideDefinition slide)
        {
            if (slide == null || !slideSnapshots.TryGetValue(slide.slideId, out var snapshot))
            {
                return false;
            }

            foreach (var state in snapshot.objectStates)
            {
                ApplyState(state);
            }

            Debug.Log($"[TutorialStateManager] Restored visibility state for Slide: '{slide.slideId}'");
            return true;
        }

        public void ApplySlideStateOverrides(SlideDefinition slide)
        {       
            if (slide == null || slide.baselineStateOverrides == null) return;

            foreach (var state in slide.baselineStateOverrides)
            {
                ApplyState(state);
            }
        }

        public void ApplyState(ObjectTransformState state)
        {
            var target = TutorialObjectRegistry.Get(state.targetObjectId);
            if (target == null) return;

            // Only maintain GameObject active state (visible / hidden)
            target.gameObject.SetActive(state.active);

            // Only restore UI 2D positions if explicitly enabled, never mess up 3D scene objects
            if (restoreTransforms)
            {
                if (state.isRectTransform && target.TryGetComponent<RectTransform>(out var rt))
                {
                    rt.anchoredPosition = state.anchoredPosition;
                }
                else
                {
                    target.transform.localPosition = state.localPosition;
                    target.transform.localEulerAngles = state.localEulerAngles;
                    target.transform.localScale = state.localScale == Vector3.zero ? Vector3.one : state.localScale;
                }
            }
        }
    }
}
