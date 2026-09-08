using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class AllObjectsEnabledEvent : MonoBehaviour
{
    [Header("Objects To Monitor")]
    [Tooltip("The event will trigger when all objects in this list are active.")]
    [SerializeField] private List<GameObject> objectsToMonitor = new List<GameObject>();

    [Header("Event")]
    [SerializeField] private UnityEvent onAllObjectsEnabled;

    private bool eventTriggered;

    private void Update()
    {
        CheckObjects();
    }

    /// <summary>
    /// Checks whether all monitored objects are currently active.
    /// </summary>
    private void CheckObjects()
    {
        if (objectsToMonitor.Count == 0)
            return;

        bool allObjectsEnabled = true;

        for (int i = 0; i < objectsToMonitor.Count; i++)
        {
            GameObject targetObject = objectsToMonitor[i];

            if (targetObject == null || !targetObject.activeSelf)
            {
                allObjectsEnabled = false;
                break;
            }
        }

        if (allObjectsEnabled)
        {
            if (!eventTriggered)
            {
                eventTriggered = true;
                onAllObjectsEnabled?.Invoke();
            }
        }
        else
        {
            eventTriggered = false;
        }
    }

    /// <summary>
    /// Manually checks the objects immediately.
    /// Useful when enabling/disabling objects through another script.
    /// </summary>
    public void CheckNow()
    {
        CheckObjects();
    }

    /// <summary>
    /// Resets the event state, allowing the event to trigger again.
    /// </summary>
    public void ResetEvent()
    {
        eventTriggered = false;
        CheckObjects();
    }
}