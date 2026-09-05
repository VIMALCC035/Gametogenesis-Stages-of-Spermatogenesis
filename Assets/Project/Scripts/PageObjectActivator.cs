using System;
using System.Collections.Generic;
using UnityEngine;

public class PageObjectActivator : MonoBehaviour
{
    [Serializable]
    public class PageObjectData
    {
        public GameObject gameObject;
        public int pageIndex;
    }

    [Header("Page Objects")]
    [SerializeField] private List<PageObjectData> pageObjects = new List<PageObjectData>();

    private void OnEnable()
    {
        PageNavigationController.OnPageChanged += OnPageChanged;
    }

    private void Start()
    {
        // Update objects for the current page when the scene starts
        UpdateObjects(PageNavigationController.CurrentIndex);
    }

    private void OnDisable()
    {
        PageNavigationController.OnPageChanged -= OnPageChanged;
    }

    private void OnPageChanged(int pageIndex)
    {
        UpdateObjects(pageIndex);
    }

    private void UpdateObjects(int currentPageIndex)
    {
        foreach (PageObjectData item in pageObjects)
        {
            if (item.gameObject == null)
                continue;

            // Visible from assigned page index and all upcoming pages
            // Hidden when going back before the assigned page
            bool shouldBeVisible = currentPageIndex >= item.pageIndex;

            item.gameObject.SetActive(shouldBeVisible);
        }
    }
}