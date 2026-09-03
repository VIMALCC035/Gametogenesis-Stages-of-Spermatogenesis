#if UNITY_EDITOR
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using TutorialFramework.Data;

namespace TutorialFramework.Editor
{
    [CustomEditor(typeof(SlideDefinition))]
    public class SlideDefinitionEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var currentSlide = (SlideDefinition)target;
            string assetPath = AssetDatabase.GetAssetPath(currentSlide);
            string folderPath = string.IsNullOrEmpty(assetPath) ? "Assets" : Path.GetDirectoryName(assetPath);

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("SLIDE DEFINITION", EditorStyles.boldLabel);

            // Auto-sync display
            if (string.IsNullOrEmpty(currentSlide.slideId))
            {
                currentSlide.slideId = currentSlide.name;
                EditorUtility.SetDirty(currentSlide);
            }

            EditorGUILayout.HelpBox($"Slide ID: {currentSlide.slideId}\nFolder: {folderPath}", MessageType.Info);
            EditorGUILayout.Space(4);

            // Quick Insertion Toolbar
            EditorGUILayout.LabelField("Quick Slide Generation & Linking", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("➕ Insert Sub-Slide (e.g. _1)", GUILayout.Height(28)))
            {
                InsertSubSlide(currentSlide, folderPath);
            }

            if (GUILayout.Button("➕ Create & Link Next", GUILayout.Height(28)))
            {
                CreateAndLinkNext(currentSlide, folderPath);
            }

            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("🔗 Auto-Chain All Slides in This Folder", GUILayout.Height(24)))
            {
                if (EditorUtility.DisplayDialog("Auto-Chain Slides", 
                    $"This will automatically link all SlideDefinition assets in '{folderPath}' in alphabetical/numerical order. Continue?", "Yes", "Cancel"))
                {
                    AutoChainFolder(folderPath);
                }
            }

            EditorGUILayout.Space(8);
            DrawDefaultInspector();

            serializedObject.ApplyModifiedProperties();
        }

        private static void CreateAndLinkNext(SlideDefinition current, string folder)
        {
            string newName = GenerateNextSlideName(current.name);
            string newAssetPath = Path.Combine(folder, $"{newName}.asset").Replace("\\", "/");

            if (File.Exists(newAssetPath))
            {
                newAssetPath = AssetDatabase.GenerateUniqueAssetPath(newAssetPath);
                newName = Path.GetFileNameWithoutExtension(newAssetPath);
            }

            var newSlide = CreateInstance<SlideDefinition>();
            newSlide.slideId = newName;
            newSlide.previousSlide = current;
            newSlide.nextSlide = current.nextSlide;

            AssetDatabase.CreateAsset(newSlide, newAssetPath);

            // Update surrounding links
            if (current.nextSlide != null)
            {
                current.nextSlide.previousSlide = newSlide;
                EditorUtility.SetDirty(current.nextSlide);
            }

            current.nextSlide = newSlide;
            EditorUtility.SetDirty(current);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeObject = newSlide;
            EditorGUIUtility.PingObject(newSlide);
        }

        private static void InsertSubSlide(SlideDefinition current, string folder)
        {
            string newName = $"{current.name}_1";
            string newAssetPath = Path.Combine(folder, $"{newName}.asset").Replace("\\", "/");

            int counter = 1;
            while (File.Exists(newAssetPath))
            {
                counter++;
                newName = $"{current.name}_{counter}";
                newAssetPath = Path.Combine(folder, $"{newName}.asset").Replace("\\", "/");
            }

            var newSlide = CreateInstance<SlideDefinition>();
            newSlide.slideId = newName;
            newSlide.previousSlide = current;
            newSlide.nextSlide = current.nextSlide;

            AssetDatabase.CreateAsset(newSlide, newAssetPath);

            if (current.nextSlide != null)
            {
                current.nextSlide.previousSlide = newSlide;
                EditorUtility.SetDirty(current.nextSlide);
            }

            current.nextSlide = newSlide;
            EditorUtility.SetDirty(current);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeObject = newSlide;
            EditorGUIUtility.PingObject(newSlide);
        }

        private static void AutoChainFolder(string folder)
        {
            var guids = AssetDatabase.FindAssets("t:SlideDefinition", new[] { folder });
            var slides = guids
                .Select(g => AssetDatabase.LoadAssetAtPath<SlideDefinition>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(s => s != null)
                .OrderBy(s => s.name, new NaturalStringComparer())
                .ToList();

            if (slides.Count == 0) return;

            for (int i = 0; i < slides.Count; i++)
            {
                var s = slides[i];
                s.slideId = s.name;
                s.previousSlide = (i > 0) ? slides[i - 1] : null;
                s.nextSlide = (i < slides.Count - 1) ? slides[i + 1] : null;
                EditorUtility.SetDirty(s);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[SlideDefinitionEditor] Successfully auto-linked {slides.Count} slides in '{folder}'!");
        }

        private static string GenerateNextSlideName(string currentName)
        {
            var match = Regex.Match(currentName, @"^(.*?)(\d+)$");
            if (match.Success)
            {
                string prefix = match.Groups[1].Value;
                string numberStr = match.Groups[2].Value;
                int number = int.Parse(numberStr);
                return $"{prefix}{(number + 1).ToString().PadLeft(numberStr.Length, '0')}";
            }
            return $"{currentName}_Next";
        }
    }

    /// <summary>
    /// Sorts strings containing numbers in natural human order (e.g. Slide_001, Slide_006, Slide_006_1, Slide_007).
    /// </summary>
    public class NaturalStringComparer : System.Collections.Generic.IComparer<string>
    {
        public int Compare(string x, string y)
        {
            return EditorUtility.NaturalCompare(x, y);
        }
    }
}
#endif
