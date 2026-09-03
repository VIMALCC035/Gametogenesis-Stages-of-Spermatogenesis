using System;
using UnityEngine;

namespace TutorialFramework.RuntimeComponents
{
    [RequireComponent(typeof(LineRenderer), typeof(Collider))]
    public class PencilMeasurementSurface : MonoBehaviour
    {
        private LineRenderer lineRenderer;
        private float expectedLen;
        private float tolerance;
        private Action onComplete;
        private bool isDrawingActive;
        private Vector3 startPoint;
        private Camera mainCamera;

        private void Awake()
        {
            lineRenderer = GetComponent<LineRenderer>();
            lineRenderer.positionCount = 0;
            mainCamera = Camera.main;
        }

        public void BeginDrawing(float expectedLength, float tol, Action completeCallback)
        {
            expectedLen = expectedLength;
            tolerance = tol;
            onComplete = completeCallback;
            isDrawingActive = true;
            ClearDrawing();
        }

        public void ClearDrawing()
        {
            if (lineRenderer != null)
            {
                lineRenderer.positionCount = 0;
            }
            isDrawingActive = false;
        }

        private void OnMouseDown()
        {
            if (!isDrawingActive) return;
            startPoint = GetWorldIntersection();
            lineRenderer.positionCount = 2;
            lineRenderer.SetPosition(0, startPoint);
            lineRenderer.SetPosition(1, startPoint);
        }

        private void OnMouseDrag()
        {
            if (!isDrawingActive || lineRenderer.positionCount < 2) return;
            lineRenderer.SetPosition(1, GetWorldIntersection());
        }

        private void OnMouseUp()
        {
            if (!isDrawingActive || lineRenderer.positionCount < 2) return;
            Vector3 endPoint = GetWorldIntersection();
            float drawnLength = Vector3.Distance(startPoint, endPoint);

            if (Mathf.Abs(drawnLength - expectedLen) <= tolerance)
            {
                isDrawingActive = false;
                var cb = onComplete;
                onComplete = null;
                cb?.Invoke();
            }
            else
            {
                ClearDrawing();
                isDrawingActive = true; // allow redraw
            }
        }

        private Vector3 GetWorldIntersection()
        {
            if (mainCamera == null) mainCamera = Camera.main;
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                return hit.point;
            }
            return transform.position;
        }
    }
}
