using System;
using UnityEngine;

namespace TutorialFramework.Data
{
    [Serializable]
    public struct ObjectTransformState
    {
        public string targetObjectId;
        public bool active;
        public Vector3 localPosition;
        public Vector3 localEulerAngles;
        public Vector3 localScale;
        public bool isRectTransform;
        public Vector2 anchoredPosition;

        public static ObjectTransformState Capture(Core.TutorialObject obj)
        {
            var t = obj.transform;
            var rt = obj.GetComponent<RectTransform>();
            return new ObjectTransformState
            {
                targetObjectId = obj.ObjectId,
                active = obj.gameObject.activeSelf,
                localPosition = t.localPosition,
                localEulerAngles = t.localEulerAngles,
                localScale = t.localScale,
                isRectTransform = rt != null,
                anchoredPosition = rt != null ? rt.anchoredPosition : Vector2.zero
            };
        }
    }

    [Serializable]
    public class SlideStateSnapshot
    {
        public string slideId;
        public ObjectTransformState[] objectStates;
    }
}
