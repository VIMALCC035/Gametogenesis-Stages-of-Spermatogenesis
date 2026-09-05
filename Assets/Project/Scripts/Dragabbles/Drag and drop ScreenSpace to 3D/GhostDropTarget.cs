using UnityEngine;
using UnityEngine.Events;
using System;
using System.Collections.Generic;

namespace DeterminingMassofaBodyUsingMeterscale
{
    [RequireComponent(typeof(Collider))]
    public class GhostDropTarget : MonoBehaviour
    {
        [Header("Identification")]
        [SerializeField] private string correctItemID;

        [Header("GameObject To Control")]
        [SerializeField] private GameObject targetObject;

        [Header("Highlight Material")]
        [SerializeField] private Material highlightMaterial;

        [Header("Navigation")]
        [SerializeField] private bool unlockNavigationOnCorrectDrop = true;

        [Header("Snap Completed Event")]
        [SerializeField] private UnityEvent onSnapCompleted;

        public event Action OnCorrectDropped;

        private readonly Dictionary<Renderer, Material[]> originalMaterials = new();

        private bool completed;

        private void Awake()
        {
            CacheOriginalMaterials();
        }

        private void Start()
        {
            completed = false;
            ApplyHighlightMaterial();
        }

        private void CacheOriginalMaterials()
        {
            originalMaterials.Clear();

            if (targetObject == null)
            {
                Debug.LogWarning(
                    $"{nameof(GhostDropTarget)} on {gameObject.name}: Target Object is not assigned.",
                    this);

                return;
            }

            Renderer[] renderers =
                targetObject.GetComponentsInChildren<Renderer>(true);

            foreach (Renderer renderer in renderers)
            {
                if (renderer == null)
                    continue;

                if (renderer is MeshRenderer ||
                    renderer is SkinnedMeshRenderer)
                {
                    originalMaterials[renderer] =
                        renderer.sharedMaterials;
                }
            }
        }

        private void ApplyHighlightMaterial()
        {
            if (highlightMaterial == null)
            {
                Debug.LogWarning(
                    $"{nameof(GhostDropTarget)} on {gameObject.name}: Highlight Material is not assigned.",
                    this);

                return;
            }

            foreach (KeyValuePair<Renderer, Material[]> pair in originalMaterials)
            {
                Renderer renderer = pair.Key;

                if (renderer == null)
                    continue;

                Material[] materials = renderer.sharedMaterials;

                for (int i = 0; i < materials.Length; i++)
                {
                    materials[i] = highlightMaterial;
                }

                renderer.sharedMaterials = materials;
            }
        }

        private void RestoreOriginalMaterials()
        {
            foreach (KeyValuePair<Renderer, Material[]> pair in originalMaterials)
            {
                Renderer renderer = pair.Key;

                if (renderer == null)
                    continue;

                renderer.sharedMaterials = pair.Value;
            }
        }

        public bool TryDrop(UIDragItem item)
        {
            if (completed || item == null)
                return false;

            // Check whether the dropped item is correct.
            if (item.ItemID != correctItemID)
                return false;

            completed = true;

            // Restore original materials.
            RestoreOriginalMaterials();

            // Disable the successfully dropped item.
            item.gameObject.SetActive(false);

            // Unlock page navigation only if enabled.
            if (unlockNavigationOnCorrectDrop)
            {
                PageNavigationController.RequestNavigationUnlock();
            }

            // Trigger C# event.
            OnCorrectDropped?.Invoke();

            // Trigger Inspector UnityEvent.
            onSnapCompleted?.Invoke();

            return true;
        }
    }
}