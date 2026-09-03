using System.Collections.Generic;
using UnityEngine;

namespace TutorialFramework.RuntimeComponents
{
    public class HighlightManager : MonoBehaviour
    {
        private readonly Dictionary<Renderer, Material[]> originalMaterials = new Dictionary<Renderer, Material[]>();

        public void ApplyHighlight(GameObject target, Material highlightMaterial)
        {
            if (target == null || highlightMaterial == null) return;

            var renderers = target.GetComponentsInChildren<Renderer>(true);
            foreach (var r in renderers)
            {
                if (!originalMaterials.ContainsKey(r))
                {
                    originalMaterials[r] = r.sharedMaterials;
                }

                Material[] mats = new Material[r.sharedMaterials.Length];
                for (int i = 0; i < mats.Length; i++)
                {
                    mats[i] = highlightMaterial;
                }
                r.materials = mats;
            }
        }

        public void RemoveHighlight(GameObject target)
        {
            if (target == null) return;

            var renderers = target.GetComponentsInChildren<Renderer>(true);
            foreach (var r in renderers)
            {
                if (originalMaterials.TryGetValue(r, out var mats))
                {
                    r.sharedMaterials = mats;
                    originalMaterials.Remove(r);
                }
            }
        }

        public void ClearAll()
        {
            foreach (var kvp in originalMaterials)
            {
                if (kvp.Key != null)
                {
                    kvp.Key.sharedMaterials = kvp.Value;
                }
            }
            originalMaterials.Clear();
        }
    }
}
