using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(Collider))]
public class TouchOrClickEventRotation : MonoBehaviour
{
    [System.Serializable]
    public class RotationElement
    {
        [Header("Collider Object")]
        public GameObject colliderObject;

        [Header("Index")]
        public int index;

        [Header("Target Z")]
        public float targetZ;

        [HideInInspector]
        public bool hasBeenTapped;
    }

    [Header("Input References")]
    [SerializeField] private Camera targetCamera;

    [Header("Common GameObject")]
    [SerializeField] private GameObject commonGameObject;

    [Header("Rotation Elements")]
    [SerializeField] private List<RotationElement> rotationElements =
        new List<RotationElement>();

    [Header("Lerp Duration")]
    [SerializeField] private float lerpDuration = 1.0f;

    [Header("Input Behavior")]
    [SerializeField] private bool ignoreUI = true;

    [Header("Rotation Events")]
    public UnityEvent OnRotationStarted;
    public UnityEvent OnRotationCompleted;

    private Coroutine rotationCoroutine;

    // ============================================================
    // LIFECYCLE
    // ============================================================

    private void Awake()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;
    }

    private void OnEnable()
    {
        PageNavigationController.OnPageChanged += HandlePageChanged;

        UpdateColliderStates();
    }

    private void Start()
    {
        UpdateColliderStates();
    }

    private void OnDisable()
    {
        PageNavigationController.OnPageChanged -= HandlePageChanged;
    }

    // ============================================================
    // PAGE CHANGE
    // ============================================================

    private void HandlePageChanged(int pageIndex)
    {
        ResetTapStates();
        UpdateColliderStates(pageIndex);
    }

    // ============================================================
    // TAP STATE
    // ============================================================

    private void ResetTapStates()
    {
        for (int i = 0; i < rotationElements.Count; i++)
        {
            RotationElement element = rotationElements[i];

            if (element == null)
                continue;

            element.hasBeenTapped = false;
        }
    }

    // ============================================================
    // COLLIDER MANAGEMENT
    // ============================================================

    private void UpdateColliderStates()
    {
        UpdateColliderStates(
            PageNavigationController.CurrentIndex
        );
    }

    private void UpdateColliderStates(int currentIndex)
    {
        for (int i = 0; i < rotationElements.Count; i++)
        {
            RotationElement element = rotationElements[i];

            if (element == null ||
                element.colliderObject == null)
            {
                continue;
            }

            Collider collider =
                element.colliderObject.GetComponent<Collider>();

            if (collider == null)
                continue;

            bool isCurrentIndex =
                element.index == currentIndex;

            collider.enabled =
                isCurrentIndex && !element.hasBeenTapped;
        }
    }

    // ============================================================
    // INPUT
    // ============================================================

    private void Update()
    {
        if (targetCamera == null)
            return;

        // --------------------------------------------------------
        // MOUSE
        // --------------------------------------------------------

        if (Mouse.current != null &&
            Mouse.current.leftButton.wasPressedThisFrame)
        {
            ProcessPointer(
                Mouse.current.position.ReadValue()
            );
        }

        // --------------------------------------------------------
        // TOUCH
        // --------------------------------------------------------

        if (Touchscreen.current != null)
        {
            var touch =
                Touchscreen.current.primaryTouch;

            if (touch.press.wasPressedThisFrame)
            {
                ProcessPointer(
                    touch.position.ReadValue()
                );
            }
        }
    }

    // ============================================================
    // POINTER PROCESSING
    // ============================================================

    private void ProcessPointer(Vector2 screenPosition)
    {
        if (ignoreUI &&
            EventSystem.current != null &&
            EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        Ray ray =
            targetCamera.ScreenPointToRay(screenPosition);

        if (!Physics.Raycast(ray, out RaycastHit hit))
            return;

        RotationElement element =
            FindElement(hit.collider);

        if (element == null)
            return;

        int currentIndex =
            PageNavigationController.CurrentIndex;

        // Safety check.
        if (element.index != currentIndex)
            return;

        // Only allow one successful tap.
        if (element.hasBeenTapped)
            return;

        // Mark immediately to prevent duplicate taps.
        element.hasBeenTapped = true;

        // Disable collider immediately.
        Collider collider =
            element.colliderObject.GetComponent<Collider>();

        if (collider != null)
            collider.enabled = false;

        // Rotate directly to the Z value assigned
        // to this element.
        RotateToTargetZ(element.targetZ);
    }

    // ============================================================
    // FIND ELEMENT
    // ============================================================

    private RotationElement FindElement(Collider hitCollider)
    {
        if (hitCollider == null)
            return null;

        for (int i = 0; i < rotationElements.Count; i++)
        {
            RotationElement element =
                rotationElements[i];

            if (element == null ||
                element.colliderObject == null)
            {
                continue;
            }

            Collider elementCollider =
                element.colliderObject.GetComponent<Collider>();

            if (elementCollider == null)
                continue;

            if (hitCollider == elementCollider)
                return element;
        }

        return null;
    }

    // ============================================================
    // ROTATION
    // ============================================================

    private void RotateToTargetZ(float targetZ)
    {
        if (commonGameObject == null)
            return;

        if (rotationCoroutine != null)
        {
            StopCoroutine(rotationCoroutine);
        }

        rotationCoroutine =
            StartCoroutine(
                LerpToTargetZ(targetZ)
            );
    }

    private IEnumerator LerpToTargetZ(float targetZ)
    {
        Transform targetTransform =
            commonGameObject.transform;

        Vector3 startEuler =
            targetTransform.eulerAngles;

        // Preserve X and Y.
        // Directly assign the target Z.
        Vector3 targetEuler =
            new Vector3(
                startEuler.x,
                startEuler.y,
                targetZ
            );

        Quaternion startRotation =
            targetTransform.rotation;

        Quaternion targetRotation =
            Quaternion.Euler(targetEuler);

        float elapsedTime = 0f;

        OnRotationStarted?.Invoke();

        if (lerpDuration <= 0f)
        {
            targetTransform.rotation =
                targetRotation;
        }
        else
        {
            while (elapsedTime < lerpDuration)
            {
                elapsedTime += Time.deltaTime;

                float t =
                    Mathf.Clamp01(
                        elapsedTime / lerpDuration
                    );

                targetTransform.rotation =
                    Quaternion.Lerp(
                        startRotation,
                        targetRotation,
                        t
                    );

                yield return null;
            }

            // Ensure exact final rotation.
            targetTransform.rotation =
                targetRotation;
        }

        OnRotationCompleted?.Invoke();

        rotationCoroutine = null;
    }

    // ============================================================
    // PUBLIC API
    // ============================================================

    public void RotateToIndex(int index)
    {
        for (int i = 0; i < rotationElements.Count; i++)
        {
            RotationElement element =
                rotationElements[i];

            if (element == null)
                continue;

            if (element.index != index)
                continue;

            if (element.hasBeenTapped)
                return;

            element.hasBeenTapped = true;

            if (element.colliderObject != null)
            {
                Collider collider =
                    element.colliderObject.GetComponent<Collider>();

                if (collider != null)
                    collider.enabled = false;
            }

            RotateToTargetZ(element.targetZ);

            return;
        }
    }

    public void RefreshColliders()
    {
        UpdateColliderStates();
    }

    public void ResetAllTapStates()
    {
        ResetTapStates();
        UpdateColliderStates();
    }

    public void StopRotation()
    {
        if (rotationCoroutine == null)
            return;

        StopCoroutine(rotationCoroutine);

        rotationCoroutine = null;
    }
}