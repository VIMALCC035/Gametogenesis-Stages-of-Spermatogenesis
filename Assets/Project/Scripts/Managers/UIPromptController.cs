using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class UIPromptController : MonoBehaviour
{
    [Header("Pages")]
    [SerializeField] private PageData[] pages;

    [Header("Dialog UI")]
    [SerializeField] private GameObject dialogPanel;
    [SerializeField] private Image dialogImage;
    [SerializeField] private TextMeshProUGUI dialogText;

    private int currentPageIndex = -1;

    private void OnEnable()
    {
        PageNavigationController.OnPageChanged += HandlePageChanged;
    }

    private void OnDisable()
    {
        PageNavigationController.OnPageChanged -= HandlePageChanged;
    }

    private void Start()
    {
        HandlePageChanged(PageNavigationController.CurrentIndex);
    }

    private void HandlePageChanged(int index)
    {
        if (pages == null || pages.Length == 0)
            return;

        if (index < 0 || index >= pages.Length)
            return;

        currentPageIndex = index;
        ShowPage(index);
    }

    private void ShowPage(int index)
    {
        PageData page = pages[index];

        if (dialogPanel != null)
            dialogPanel.SetActive(false);

        ResetAllPanels();

        if (!page.showDialogBox && !page.showAlternatePanels)
            return;

        // =========================================================
        // DIALOG BOX
        // =========================================================

        if (page.showDialogBox)
        {
            if (dialogPanel != null)
                dialogPanel.SetActive(true);

            if (dialogText != null)
                dialogText.text = page.pageText;

            if (dialogImage != null)
            {
                // Each page now has its own dialog sprite.
                dialogImage.sprite = page.dialogSprite;

                // Hide the Image component if no sprite is assigned.
                dialogImage.enabled = page.dialogSprite != null;
            }
        }

        ApplyPanelVisibility(index);
    }

    private void ResetAllPanels()
    {
        if (pages == null)
            return;

        foreach (PageData page in pages)
        {
            if (page == null || page.alternatePanels == null)
                continue;

            foreach (AlternatePanelData panelData in page.alternatePanels)
            {
                if (panelData != null && panelData.panel != null)
                    panelData.panel.SetActive(false);
            }
        }
    }

    private void ApplyPanelVisibility(int currentIndex)
    {
        if (pages == null)
            return;

        for (int i = 0; i <= currentIndex; i++)
        {
            PageData page = pages[i];

            if (page == null ||
                !page.showAlternatePanels ||
                page.alternatePanels == null)
            {
                continue;
            }

            foreach (AlternatePanelData panelData in page.alternatePanels)
            {
                if (panelData == null || panelData.panel == null)
                    continue;

                // =====================================================
                // FIRST IGNORE
                // =====================================================
                // If enabled, ignore this panel ONLY on its first visit.
                // From the second visit onward, normal logic applies.

                if (i == currentIndex &&
                    panelData.firstIgnore &&
                    !panelData.hasVisitedOnce)
                {
                    panelData.hasVisitedOnce = true;
                    continue;
                }

                // =====================================================
                // ENABLE ONCE
                // =====================================================
                // If already enabled once, do not enable it again.

                if (panelData.enableOnce &&
                    panelData.hasBeenEnabledOnce)
                {
                    continue;
                }

                // =====================================================
                // CURRENT PAGE
                // =====================================================

                if (i == currentIndex)
                {
                    panelData.panel.SetActive(true);

                    if (panelData.enableOnce)
                        panelData.hasBeenEnabledOnce = true;
                }

                // =====================================================
                // UPCOMING PAGES
                // =====================================================

                else if (panelData.stayInUpcomingPages)
                {
                    // Only allow staying panels if not restricted
                    // by enableOnce.

                    if (!panelData.enableOnce ||
                        !panelData.hasBeenEnabledOnce)
                    {
                        panelData.panel.SetActive(true);
                    }
                }
            }
        }
    }
}

[System.Serializable]
public class PageData
{
    [Header("Page Name / Page No")]
    public string pageName;

    [TextArea]
    public string pageText;

    [Header("Dialog Settings")]
    public bool showDialogBox;

    [Tooltip("Dialog image sprite used specifically for this page.")]
    public Sprite dialogSprite;

    [Header("Alternate Panel Settings")]
    public bool showAlternatePanels;

    [Header("Alternate Panels For This Page")]
    public List<AlternatePanelData> alternatePanels;
}

[System.Serializable]
public class AlternatePanelData
{
    public GameObject panel;

    [Tooltip("If enabled, this panel will remain active in upcoming pages.")]
    public bool stayInUpcomingPages;

    [Header("First Visit")]
    [Tooltip(
        "If enabled, this panel will be ignored on the first visit only. " +
        "It will appear from the second visit onward."
    )]
    public bool firstIgnore;

    [Header("Enable Once Feature")]
    [Tooltip(
        "If enabled, panel will activate only once and never again on revisit."
    )]
    public bool enableOnce;

    [HideInInspector]
    public bool hasBeenEnabledOnce;

    [HideInInspector]
    public bool hasVisitedOnce;
}

#if UNITY_EDITOR

[CustomEditor(typeof(UIPromptController))]
[CanEditMultipleObjects]
public class UIPromptControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GUILayout.Space(10);

        if (GUILayout.Button("Name Pages"))
        {
            foreach (Object targetObject in targets)
            {
                UIPromptController controller =
                    (UIPromptController)targetObject;

                NamePages(controller);
            }
        }
    }

    private void NamePages(UIPromptController controller)
    {
        SerializedObject so =
            new SerializedObject(controller);

        SerializedProperty pagesProp =
            so.FindProperty("pages");

        if (pagesProp == null || pagesProp.arraySize == 0)
        {
            Debug.LogWarning("No pages found to rename.");
            return;
        }

        for (int i = 0; i < pagesProp.arraySize; i++)
        {
            SerializedProperty page =
                pagesProp.GetArrayElementAtIndex(i);

            SerializedProperty nameProp =
                page.FindPropertyRelative("pageName");

            if (nameProp != null)
            {
                nameProp.stringValue =
                    $"Page {i + 1}";
            }
        }

        so.ApplyModifiedProperties();

        EditorUtility.SetDirty(controller);
    }
}

#endif