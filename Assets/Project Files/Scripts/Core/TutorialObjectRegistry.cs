using System.Collections.Generic;
using UnityEngine;

namespace TutorialFramework.Core
{
    /// <summary>
    /// Fast O(1) in-memory registry mapping unique string IDs to runtime TutorialObject instances.
    /// Supports both active and initially-inactive scene objects.
    /// </summary>
    public static class TutorialObjectRegistry
    {
        private static readonly Dictionary<string, TutorialObject> Registry = new Dictionary<string, TutorialObject>();
        private static bool isInitialized = false;

        public static void InitializeAll()
        {
            Registry.Clear();

            // Find all TutorialObjects including INACTIVE ones
#if UNITY_2023_1_OR_NEWER
            var allObjects = Object.FindObjectsByType<TutorialObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
            var allObjects = Object.FindObjectsOfType<TutorialObject>(true);
#endif
            foreach (var obj in allObjects)
            {
                Register(obj);
            }
            isInitialized = true;
        }

        public static void Register(TutorialObject obj)
        {
            if (obj == null || string.IsNullOrWhiteSpace(obj.ObjectId)) return;

            string key = obj.ObjectId.Trim();
            if (Registry.TryGetValue(key, out var existing))
            {
                if (existing != obj && existing != null)
                {
                    Debug.LogError($"[TutorialObjectRegistry] Duplicate Object ID detected: '{key}' on '{obj.gameObject.name}' and '{existing.gameObject.name}'! IDs must be globally unique.", obj);
                }
                return;
            }
            Registry[key] = obj;
            if (obj.ObjectId != key && !Registry.ContainsKey(obj.ObjectId))
            {
                Registry[obj.ObjectId] = obj;
            }
        }

        public static void Unregister(TutorialObject obj)
        {
            if (obj == null || string.IsNullOrWhiteSpace(obj.ObjectId)) return;

            string key = obj.ObjectId.Trim();
            if (Registry.TryGetValue(key, out var existing) && existing == obj)
            {
                Registry.Remove(key);
            }
            if (Registry.TryGetValue(obj.ObjectId, out var rawExisting) && rawExisting == obj)
            {
                Registry.Remove(obj.ObjectId);
            }
        }

        public static TutorialObject Get(string objectId)
        {
            if (string.IsNullOrWhiteSpace(objectId)) return null;

            if (!isInitialized)
            {
                InitializeAll();
            }

            string trimmed = objectId.Trim();
            if (Registry.TryGetValue(trimmed, out var obj) && obj != null)
            {
                return obj;
            }

            if (Registry.TryGetValue(objectId, out var rawObj) && rawObj != null)
            {
                return rawObj;
            }

            // Fallback: Check if the object exists in scene but was loaded or added dynamically
            InitializeAll();
            if (Registry.TryGetValue(trimmed, out var fallbackObj) && fallbackObj != null)
            {
                return fallbackObj;
            }

            // Fallback: Check by GameObject name (handles case where name and objectId differ by whitespace)
            foreach (var registered in Registry.Values)
            {
                if (registered != null && (registered.gameObject.name.Trim().Equals(trimmed, System.StringComparison.OrdinalIgnoreCase) || registered.ObjectId.Trim().Equals(trimmed, System.StringComparison.OrdinalIgnoreCase)))
                {
                    return registered;
                }
            }

            Debug.LogWarning($"[TutorialObjectRegistry] Object with ID '{objectId}' was not found in active or inactive scene objects!");
            return null;
        }

        public static T GetComponent<T>(string objectId) where T : Component
        {
            var obj = Get(objectId);
            if (obj == null) return null;

            var component = obj.GetComponent<T>();
            if (component == null)
            {
                Debug.LogWarning($"[TutorialObjectRegistry] Object '{objectId}' found, but missing required component '{typeof(T).Name}'!");
            }
            return component;
        }

        public static bool TryGetGameObject(string objectId, out GameObject go)
        {
            var obj = Get(objectId);
            if (obj != null)
            {
                go = obj.gameObject;
                return true;
            }
            go = null;
            return false;
        }

        public static void Clear()
        {
            Registry.Clear();
            isInitialized = false;
        }
    }
}
