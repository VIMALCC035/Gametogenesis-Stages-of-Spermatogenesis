using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using TutorialFramework.Data;
using TutorialFramework.Actions;

namespace TutorialFramework.Editor
{
    public class SlideSequenceDuplicator : EditorWindow
    {
        [Header("Source Slide Range")]
        [SerializeField] private int startSlideNumber = 46;
        [SerializeField] private int endSlideNumber = 63;

        [Header("New Destination Range")]
        [SerializeField] private int newStartSlideNumber = 64;

        [Header("Target & Asset Settings")]
        [SerializeField] private DefaultAsset slidesFolder;
        [SerializeField] private TutorialSequenceSO sequenceData;

        [Header("Automatic Name / ID Replacement")]
        [SerializeField] private string searchPattern = "R2";
        [SerializeField] private string replacePattern = "R3";

        [MenuItem("Tutorial Framework/Duplicate Slide Range...")]
        public static void ShowWindow()
        {
            var window = GetWindow<SlideSequenceDuplicator>("Slide Duplicator");
            window.minSize = new Vector2(460, 420);
            window.Show();
        }

        private void OnEnable()
        {
            if (slidesFolder == null)
            {
                var folderObj = AssetDatabase.LoadAssetAtPath<DefaultAsset>("Assets/Project/Scripts/ScriptableObjects/Slides");
                if (folderObj != null) slidesFolder = folderObj;
            }

            if (sequenceData == null)
            {
                var guids = AssetDatabase.FindAssets("t:TutorialSequenceSO");
                if (guids.Length > 0)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    sequenceData = AssetDatabase.LoadAssetAtPath<TutorialSequenceSO>(path);
                }
            }
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("1-Click Slide Sequence Duplicator", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Duplicates a range of slides (e.g. Slide_046 to Slide_063), automatically links previous/next chains, remaps cell IDs (R2 -> R3), and appends them to TutorialSequenceSO.", MessageType.Info);
            EditorGUILayout.Space(10);

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Source Range (Round 2)", EditorStyles.boldLabel);
            startSlideNumber = EditorGUILayout.IntField("From Slide #:", startSlideNumber);
            endSlideNumber = EditorGUILayout.IntField("To Slide #:", endSlideNumber);
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(5);

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Destination Range (Round 3)", EditorStyles.boldLabel);
            newStartSlideNumber = EditorGUILayout.IntField("New Start Slide #:", newStartSlideNumber);
            int count = Mathf.Max(0, endSlideNumber - startSlideNumber + 1);
            int newEndSlideNumber = newStartSlideNumber + count - 1;
            EditorGUILayout.LabelField($"Will create: Slide_{newStartSlideNumber:D3} through Slide_{newEndSlideNumber:D3} ({count} slides)");
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(5);

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Auto-Remap Observation Table IDs", EditorStyles.boldLabel);
            searchPattern = EditorGUILayout.TextField("Find Text:", searchPattern);
            replacePattern = EditorGUILayout.TextField("Replace With:", replacePattern);
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(5);

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Project References", EditorStyles.boldLabel);
            slidesFolder = (DefaultAsset)EditorGUILayout.ObjectField("Slides Folder:", slidesFolder, typeof(DefaultAsset), false);
            sequenceData = (TutorialSequenceSO)EditorGUILayout.ObjectField("Sequence SO:", sequenceData, typeof(TutorialSequenceSO), false);
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(15);

            GUI.backgroundColor = new Color(0.2f, 0.85f, 0.4f);
            if (GUILayout.Button($"🚀 Duplicate & Chain {count} Slides (Slide_{newStartSlideNumber:D3} - Slide_{newEndSlideNumber:D3})", GUILayout.Height(40)))
            {
                ExecuteDuplication();
            }
            GUI.backgroundColor = Color.white;
        }

        private void ExecuteDuplication()
        {
            string folderPath = slidesFolder != null 
                ? AssetDatabase.GetAssetPath(slidesFolder) 
                : "Assets/Project/Scripts/ScriptableObjects/Slides";

            if (!Directory.Exists(folderPath))
            {
                EditorUtility.DisplayDialog("Error", $"Folder '{folderPath}' not found!", "OK");
                return;
            }

            int count = endSlideNumber - startSlideNumber + 1;
            if (count <= 0)
            {
                EditorUtility.DisplayDialog("Error", "Invalid slide range!", "OK");
                return;
            }

            int calculatedEndNum = newStartSlideNumber + count - 1;
            List<SlideDefinition> newSlides = new List<SlideDefinition>();

            for (int i = 0; i < count; i++)
            {
                int srcNum = startSlideNumber + i;
                int dstNum = newStartSlideNumber + i;

                string srcPath = $"{folderPath}/Slide_{srcNum:D3}.asset";
                string dstPath = $"{folderPath}/Slide_{dstNum:D3}.asset";

                if (!File.Exists(srcPath))
                {
                    Debug.LogWarning($"[SlideDuplicator] Source slide '{srcPath}' does not exist! Skipping.");
                    continue;
                }

                SlideDefinition srcSlide = AssetDatabase.LoadAssetAtPath<SlideDefinition>(srcPath);
                if (srcSlide == null) continue;

                SlideDefinition dstSlide = Instantiate(srcSlide);
                dstSlide.slideId = $"Slide_{dstNum:D3}";
                dstSlide.name = $"Slide_{dstNum:D3}";

                RemapSlideActions(dstSlide, searchPattern, replacePattern);

                AssetDatabase.CreateAsset(dstSlide, dstPath);
                newSlides.Add(dstSlide);
            }

            // Link the chain: Previous -> Next
            for (int i = 0; i < newSlides.Count; i++)
            {
                SlideDefinition current = newSlides[i];

                if (i == 0)
                {
                    string prevOfFirstPath = $"{folderPath}/Slide_{startSlideNumber - 1:D3}.asset";
                    if (File.Exists(prevOfFirstPath))
                    {
                        var prevOfFirst = AssetDatabase.LoadAssetAtPath<SlideDefinition>(prevOfFirstPath);
                        current.previousSlide = prevOfFirst;
                    }
                    else
                    {
                        string endOfSrcPath = $"{folderPath}/Slide_{endSlideNumber:D3}.asset";
                        var endOfSrc = AssetDatabase.LoadAssetAtPath<SlideDefinition>(endOfSrcPath);
                        current.previousSlide = endOfSrc;
                        if (endOfSrc != null)
                        {
                            endOfSrc.nextSlide = current;
                            EditorUtility.SetDirty(endOfSrc);
                        }
                    }
                }
                else
                {
                    current.previousSlide = newSlides[i - 1];
                }

                if (i < newSlides.Count - 1)
                {
                    current.nextSlide = newSlides[i + 1];
                }
                else
                {
                    current.nextSlide = null;
                }

                EditorUtility.SetDirty(current);
            }

            // Connect previous block's end slide to the first new slide
            string bridgeSrcPath = $"{folderPath}/Slide_{endSlideNumber:D3}.asset";
            if (File.Exists(bridgeSrcPath) && newSlides.Count > 0)
            {
                var bridgeSrc = AssetDatabase.LoadAssetAtPath<SlideDefinition>(bridgeSrcPath);
                if (bridgeSrc != null)
                {
                    bridgeSrc.nextSlide = newSlides[0];
                    newSlides[0].previousSlide = bridgeSrc;
                    EditorUtility.SetDirty(bridgeSrc);
                    EditorUtility.SetDirty(newSlides[0]);
                }
            }

            // Append to TutorialSequenceSO
            if (sequenceData != null)
            {
                if (sequenceData.allSlides == null) sequenceData.allSlides = new List<SlideDefinition>();
                foreach (var s in newSlides)
                {
                    if (!sequenceData.allSlides.Contains(s))
                    {
                        sequenceData.allSlides.Add(s);
                    }
                }
                EditorUtility.SetDirty(sequenceData);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (newSlides.Count > 0)
            {
                Selection.activeObject = newSlides[0];
                EditorGUIUtility.PingObject(newSlides[0]);
            }

            EditorUtility.DisplayDialog("Success!", $"Successfully created {newSlides.Count} slides:\nSlide_{newStartSlideNumber:D3} through Slide_{calculatedEndNum:D3}\n\nAll next/previous links and table cell IDs (R2 -> R3) are configured!", "OK");
        }

        private void RemapSlideActions(SlideDefinition slide, string search, string replace)
        {
            if (slide == null || slide.actions == null || string.IsNullOrEmpty(search) || string.IsNullOrEmpty(replace)) return;

            foreach (var action in slide.actions)
            {
                if (action == null) continue;

                if (action is ObservationTableAction obs)
                {
                    if (!string.IsNullOrEmpty(obs.targetCellId))
                        obs.targetCellId = obs.targetCellId.Replace(search, replace);

                    if (obs.sequentialSteps != null)
                    {
                        for (int s = 0; s < obs.sequentialSteps.Count; s++)
                        {
                            var step = obs.sequentialSteps[s];
                            if (!string.IsNullOrEmpty(step.targetCellId))
                                step.targetCellId = step.targetCellId.Replace(search, replace);
                            obs.sequentialSteps[s] = step;
                        }
                    }
                }
                else if (action is DragDropAction drag)
                {
                    if (!string.IsNullOrEmpty(drag.validDropTargetId))
                        drag.validDropTargetId = drag.validDropTargetId.Replace(search, replace);
                }
                else if (action is TouchAction touch)
                {
                    if (!string.IsNullOrEmpty(touch.destinationObjectId))
                        touch.destinationObjectId = touch.destinationObjectId.Replace(search, replace);
                }
            }
        }
    }
}
