using System;
using System.Collections.Generic;
using UnityEngine;
using TutorialFramework.Core;

namespace TutorialFramework.Actions
{
    /// <summary>
    /// Standalone action to enable and disable GameObjects/Panels by ID cleanly without position changes.
    /// </summary>
    [Serializable]
    public class ToggleObjectAction : TutorialAction
    {
        [Header("Objects To Activate / Enable")]
        public List<string> objectsToEnable = new List<string>();

        [Header("Objects To Deactivate / Disable")]
        public List<string> objectsToDisable = new List<string>();

        [Header("Optional Instruction Update")]
        [TextArea(1, 3)]
        public string instructionText;

        public override void Execute(TutorialContext context, Action onComplete)
        {
            if (!string.IsNullOrEmpty(instructionText) && context.UI != null)
            {
                context.UI.SetInstruction(instructionText);
            }

            ToggleList(objectsToEnable, true);
            ToggleList(objectsToDisable, false);

            onComplete?.Invoke();
        }

        private void ToggleList(List<string> ids, bool activeState)
        {
            if (ids == null) return;
            foreach (var id in ids)
            {
                if (string.IsNullOrWhiteSpace(id)) continue;
                var obj = TutorialObjectRegistry.Get(id);
                if (obj != null)
                {
                    obj.gameObject.SetActive(activeState);
                    Debug.Log($"[ToggleObjectAction] Set '{id}' activeSelf = {activeState}");
                }
            }
        }

        public override void ResetAction(TutorialContext context)
        {
        }

        public override void FastForward(TutorialContext context)
        {
            Execute(context, null);
        }
    }
}
