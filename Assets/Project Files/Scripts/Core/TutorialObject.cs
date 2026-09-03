using UnityEngine;

namespace TutorialFramework.Core
{
    [DisallowMultipleComponent]
    [SelectionBase]
    public class TutorialObject : MonoBehaviour
    {
        [Tooltip("Unique stable identifier used by ScriptableObjects to resolve this GameObject at runtime.")]
        [SerializeField] private string objectId;

        public string ObjectId => objectId;

        private void Awake()
        {
            if (!string.IsNullOrEmpty(objectId))
            {
                TutorialObjectRegistry.Register(this);
            }
        }

        private void OnEnable()
        {
            if (!string.IsNullOrEmpty(objectId))
            {
                TutorialObjectRegistry.Register(this);
            }
        }

        private void OnDestroy()
        {
            if (!string.IsNullOrEmpty(objectId))
            {
                TutorialObjectRegistry.Unregister(this);
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(objectId))
            {
                objectId = gameObject.name;
            }
        }
#endif
    }
}
