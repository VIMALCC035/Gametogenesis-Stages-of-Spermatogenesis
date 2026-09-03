#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using TutorialFramework.Actions;

namespace TutorialFramework.Editor
{
    [CustomPropertyDrawer(typeof(SubclassSelectorAttribute))]
    public class SubclassSelectorDrawer : PropertyDrawer
    {
        [Serializable]
        private struct PlacementJSON
        {
            public Vector3 position;
            public Quaternion rotation;
            public Vector3 scale;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.ManagedReference)
            {
                EditorGUI.PropertyField(position, property, label, true);
                return;
            }

            Type baseType = GetFieldType();
            var derivedTypes = TypeCache.GetTypesDerivedFrom(baseType)
                .Where(t => !t.IsAbstract && !t.IsGenericType)
                .OrderBy(t => t.Name)
                .ToList();

            string currentTypeName = property.managedReferenceValue != null
                ? property.managedReferenceValue.GetType().Name
                : "<None (Null)>";

            Rect dropdownRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);

            if (EditorGUI.DropdownButton(dropdownRect, new GUIContent($"Type: {currentTypeName}"), FocusType.Keyboard))
            {
                GenericMenu menu = new GenericMenu();
                menu.AddItem(new GUIContent("None (Null)"), property.managedReferenceValue == null, () =>
                {
                    property.managedReferenceValue = null;
                    property.serializedObject.ApplyModifiedProperties();
                });

                foreach (var type in derivedTypes)
                {
                    string typeName = type.Name;
                    bool isSelected = property.managedReferenceValue != null && property.managedReferenceValue.GetType() == type;

                    menu.AddItem(new GUIContent(typeName), isSelected, () =>
                    {
                        property.managedReferenceValue = Activator.CreateInstance(type);
                        property.serializedObject.ApplyModifiedProperties();
                    });
                }
                menu.ShowAsContext();
            }

            position.y += EditorGUIUtility.singleLineHeight + 2;

            // Extra buttons for CameraAction & MoveObjectAction
            if (property.managedReferenceValue is CameraAction)
            {
                float btnWidth = (position.width - 8) / 3f;
                Rect sceneBtnRect = new Rect(position.x, position.y, btnWidth, EditorGUIUtility.singleLineHeight);
                Rect mainBtnRect = new Rect(position.x + btnWidth + 4, position.y, btnWidth, EditorGUIUtility.singleLineHeight);
                Rect pasteBtnRect = new Rect(position.x + (btnWidth * 2) + 8, position.y, btnWidth, EditorGUIUtility.singleLineHeight);

                if (GUI.Button(sceneBtnRect, "👁 Scene View"))
                {
                    CaptureFromSceneView(property);
                }

                if (GUI.Button(mainBtnRect, "🎥 Main Cam"))
                {
                    CaptureFromMainCamera(property);
                }

                if (GUI.Button(pasteBtnRect, "📋 Paste Pos"))
                {
                    PasteFromClipboard(property);
                }

                position.y += EditorGUIUtility.singleLineHeight + 4;
            }
            else if (property.managedReferenceValue is MoveObjectAction)
            {
                Rect pasteBtnRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
                if (GUI.Button(pasteBtnRect, "📋 Paste Copied Transform from Clipboard"))
                {
                    PasteFromClipboard(property);
                }
                position.y += EditorGUIUtility.singleLineHeight + 4;
            }

            EditorGUI.PropertyField(position, property, label, true);
        }

        private static void PasteFromClipboard(SerializedProperty property)
        {
            string clipboard = EditorGUIUtility.systemCopyBuffer;
            if (string.IsNullOrEmpty(clipboard))
            {
                Debug.LogWarning("[SubclassSelectorDrawer] Clipboard is empty.");
                return;
            }

            try
            {
                int jsonIdx = clipboard.IndexOf('{');
                if (jsonIdx >= 0)
                {
                    string json = clipboard.Substring(jsonIdx);
                    PlacementJSON data = JsonUtility.FromJson<PlacementJSON>(json);
                    
                    Vector3 euler = data.rotation.eulerAngles;
                    ApplyCameraValues(property, data.position, euler, 60f, "Clipboard Transform");
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SubclassSelectorDrawer] Could not parse clipboard data: {e.Message}");
            }
        }

        private static void CaptureFromSceneView(SerializedProperty property)
        {
            SceneView sv = SceneView.lastActiveSceneView ?? (SceneView.sceneViews.Count > 0 ? (SceneView)SceneView.sceneViews[0] : null);
            if (sv != null && sv.camera != null)
            {
                Vector3 pos = sv.camera.transform.position;
                Vector3 rot = sv.camera.transform.eulerAngles;
                float fov = sv.camera.fieldOfView;

                ApplyCameraValues(property, pos, rot, fov, "Scene View Camera");
            }
            else
            {
                Debug.LogWarning("[CameraAction] No active SceneView window found.");
            }
        }

        private static void CaptureFromMainCamera(SerializedProperty property)
        {
#if UNITY_2023_1_OR_NEWER
            Camera cam = Camera.main ?? UnityEngine.Object.FindFirstObjectByType<Camera>();
#else
            Camera cam = Camera.main ?? UnityEngine.Object.FindObjectOfType<Camera>();
#endif
            if (cam != null)
            {
                Vector3 pos = cam.transform.position;
                Vector3 rot = cam.transform.eulerAngles;
                float fov = cam.fieldOfView;

                ApplyCameraValues(property, pos, rot, fov, $"Main Camera ({cam.gameObject.name})");
            }
            else
            {
                Debug.LogWarning("[CameraAction] No Camera found in current scene.");
            }
        }

        private static void ApplyCameraValues(SerializedProperty property, Vector3 pos, Vector3 rot, float fov, string sourceName)
        {
            var posProp = property.FindPropertyRelative("targetPosition");
            var rotProp = property.FindPropertyRelative("targetRotation");
            var fovProp = property.FindPropertyRelative("fieldOfView");

            if (posProp != null) posProp.vector3Value = pos;
            if (rotProp != null) rotProp.vector3Value = rot;
            if (fovProp != null && fov > 0) fovProp.floatValue = fov;

            property.serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(property.serializedObject.targetObject);

            Debug.Log($"[CameraAction] Applied from {sourceName} -> Pos: {pos}, Rot: {rot}, FOV: {fov}");
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float extra = 0f;
            if (property.managedReferenceValue is CameraAction || property.managedReferenceValue is MoveObjectAction)
            {
                extra += EditorGUIUtility.singleLineHeight + 4;
            }
            return EditorGUI.GetPropertyHeight(property, true) + EditorGUIUtility.singleLineHeight + 4 + extra;
        }

        private Type GetFieldType()
        {
            if (fieldInfo.FieldType.IsGenericType && (fieldInfo.FieldType.GetGenericTypeDefinition() == typeof(System.Collections.Generic.List<>) || fieldInfo.FieldType.GetGenericTypeDefinition() == typeof(System.Collections.Generic.IEnumerable<>)))
            {
                return fieldInfo.FieldType.GetGenericArguments()[0];
            }

            if (fieldInfo.FieldType.IsArray)
            {
                return fieldInfo.FieldType.GetElementType();
            }

            return fieldInfo.FieldType;
        }
    }
}
#endif
