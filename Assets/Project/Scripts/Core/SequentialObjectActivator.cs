using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class SequentialObjectActivator : MonoBehaviour
{
    [Header("Objects to Enable")]
    [SerializeField] private List<GameObject> objects = new List<GameObject>();

    [Header("Delay Between Objects")]
    [SerializeField] private float delay = 0.5f;

    [Header("Event After All Objects Are Enabled")]
    public UnityEvent OnSequenceComplete;

    private Coroutine activationCoroutine;

    /// <summary>
    /// Call this function to enable all objects one by one.
    /// </summary>
    public void EnableObjectsOneByOne()
    {
        if (activationCoroutine != null)
        {
            StopCoroutine(activationCoroutine);
        }

        activationCoroutine = StartCoroutine(EnableObjectsRoutine());
    }

    private IEnumerator EnableObjectsRoutine()
    {
        foreach (GameObject obj in objects)
        {
            if (obj == null)
                continue;

            // Enable current object
            obj.SetActive(true);

            // Wait before enabling the next object
            yield return new WaitForSeconds(delay);
        }

        activationCoroutine = null;

        // Trigger event after all objects are enabled
        OnSequenceComplete?.Invoke();
    }
}