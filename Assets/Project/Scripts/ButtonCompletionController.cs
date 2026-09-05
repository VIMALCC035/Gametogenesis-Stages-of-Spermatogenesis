using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class ButtonCompletionController : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private List<Button> buttons = new List<Button>();

    [Header("Completion Event")]
    [SerializeField] private UnityEvent onAllButtonsClicked;

    private readonly HashSet<Button> clickedButtons = new HashSet<Button>();
    private bool allButtonsCompleted;

    private void Awake()
    {
        RegisterButtonListeners();
    }

    private void OnDestroy()
    {
        UnregisterButtonListeners();
    }

    private void RegisterButtonListeners()
    {
        foreach (Button button in buttons)
        {
            if (button == null)
                continue;

            button.onClick.AddListener(() => OnButtonClicked(button));
        }
    }

    private void UnregisterButtonListeners()
    {
        foreach (Button button in buttons)
        {
            if (button == null)
                continue;

            button.onClick.RemoveAllListeners();
        }
    }

    private void OnButtonClicked(Button clickedButton)
    {
        if (allButtonsCompleted || clickedButton == null)
            return;

        // Prevent the same button from being counted multiple times.
        if (!clickedButtons.Add(clickedButton))
            return;

        // Check whether every assigned button has been clicked.
        if (clickedButtons.Count >= GetValidButtonCount())
        {
            allButtonsCompleted = true;
            onAllButtonsClicked?.Invoke();
        }
    }

    private int GetValidButtonCount()
    {
        int count = 0;

        foreach (Button button in buttons)
        {
            if (button != null)
                count++;
        }

        return count;
    }

    /// <summary>
    /// Resets the completion state so the buttons can be completed again.
    /// </summary>
    public void ResetCompletion()
    {
        clickedButtons.Clear();
        allButtonsCompleted = false;
    }

    /// <summary>
    /// Returns true when all assigned buttons have been clicked.
    /// </summary>
    public bool AreAllButtonsClicked()
    {
        return allButtonsCompleted;
    }
}