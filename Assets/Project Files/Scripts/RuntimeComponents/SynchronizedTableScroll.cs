using UnityEngine;
using UnityEngine.UI;

namespace TutorialFramework.RuntimeComponents
{
    [ExecuteAlways]
    public class SynchronizedTableScroll : MonoBehaviour
    {
        [Header("Scroll References")]
        [Tooltip("The main ScrollRect controlling the body rows (handles both horizontal & vertical scrolling).")]
        [SerializeField] private ScrollRect bodyScrollRect;

        [Tooltip("The RectTransform of the Header Content that must follow the horizontal scroll.")]
        [SerializeField] private RectTransform headerContent;

        [Tooltip("The RectTransform of the Body Content inside the ScrollRect Viewport.")]
        [SerializeField] private RectTransform bodyContent;

        private void OnEnable()
        {
            if (bodyScrollRect != null)
            {
                bodyScrollRect.onValueChanged.AddListener(OnBodyScrolled);
            }
            SyncHeaderPosition();
        }

        private void OnDisable()
        {
            if (bodyScrollRect != null)
            {
                bodyScrollRect.onValueChanged.RemoveListener(OnBodyScrolled);
            }
        }

        private void LateUpdate()
        {
            // Fallback sync for editor layout changes or programmatic scroll snaps
            SyncHeaderPosition();
        }

        private void OnBodyScrolled(Vector2 normalizedPos)
        {
            SyncHeaderPosition();
        }

        public void SyncHeaderPosition()
        {
            if (headerContent == null || bodyContent == null) return;

            // Mirror Body's horizontal anchored position to Header
            Vector2 headerPos = headerContent.anchoredPosition;
            if (!Mathf.Approximately(headerPos.x, bodyContent.anchoredPosition.x))
            {
                headerPos.x = bodyContent.anchoredPosition.x;
                headerContent.anchoredPosition = headerPos;
            }
        }
    }
}
