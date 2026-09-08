using UnityEngine;

public class UIBillboard : MonoBehaviour
{
    [Header("Camera")]
    [SerializeField] private Camera targetCamera;

    [Header("Rotation")]
    [SerializeField] private bool lockX = false;
    [SerializeField] private bool lockY = false;
    [SerializeField] private bool lockZ = false;

    private void Awake()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }
    }

    private void LateUpdate()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;

            if (targetCamera == null)
                return;
        }

        Vector3 direction = targetCamera.transform.position - transform.position;

        if (direction.sqrMagnitude <= Mathf.Epsilon)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(direction);

        Vector3 rotation = targetRotation.eulerAngles;
        Vector3 currentRotation = transform.eulerAngles;

        if (lockX)
            rotation.x = currentRotation.x;

        if (lockY)
            rotation.y = currentRotation.y;

        if (lockZ)
            rotation.z = currentRotation.z;

        transform.rotation = Quaternion.Euler(rotation);
    }
}