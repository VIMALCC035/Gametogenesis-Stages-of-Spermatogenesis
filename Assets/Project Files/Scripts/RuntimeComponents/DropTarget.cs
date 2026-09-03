using UnityEngine;

namespace TutorialFramework.RuntimeComponents
{
    public class DropTarget : MonoBehaviour
    {
        [SerializeField] private float gizmoRadius = 0.3f;
        [SerializeField] private Color gizmoColor = Color.cyan;

        public Vector3 TargetPosition => transform.position;

        private void OnDrawGizmos()
        {
            Gizmos.color = gizmoColor;
            Gizmos.DrawWireSphere(transform.position, gizmoRadius);
        }
    }
}
