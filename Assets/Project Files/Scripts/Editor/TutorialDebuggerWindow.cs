#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using TutorialFramework.Core;
using TutorialFramework.Data;

namespace TutorialFramework.Editor
{
    public class TutorialDebuggerWindow : EditorWindow
    {
        private SlideDefinition jumpTargetSlide;
        private Vector2 scrollPos;

        [MenuItem("Tools/Tutorial Framework/Tutorial Debugger")]
        public static void Open()
        {
            GetWindow<TutorialDebuggerWindow>("Tutorial Debugger");
        }

        private void OnGUI()
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            var controller = FindFirstObjectByType<TutorialController>();

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Tutorial Runtime Debugger", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Enter Play Mode in the Editor to use the interactive Live Debugger.", MessageType.Info);
                EditorGUILayout.EndScrollView();
                return;
            }

            if (controller == null)
            {
                EditorGUILayout.HelpBox("No TutorialController found in the active scene.", MessageType.Warning);
                EditorGUILayout.EndScrollView();
                return;
            }

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            string activeId = controller.CurrentSlide != null ? controller.CurrentSlide.slideId : "None";
            EditorGUILayout.LabelField($"Active Slide ID: {activeId}", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Slide Complete: {controller.IsSlideCompleted}");

            string currentActionName = controller.CurrentExecutingAction != null
                ? controller.CurrentExecutingAction.GetType().Name
                : "None (Idle / Completed)";
            EditorGUILayout.LabelField($"Current Action: {currentActionName}");
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Direct Slide Jump (Testing Mode)", EditorStyles.boldLabel);
            jumpTargetSlide = (SlideDefinition)EditorGUILayout.ObjectField("Target Slide SO", jumpTargetSlide, typeof(SlideDefinition), false);

            if (GUILayout.Button("Load Slide Immediately", GUILayout.Height(32)))
            {
                if (jumpTargetSlide != null)
                {
                    controller.LoadDirectTestSlide(jumpTargetSlide);
                }
                else
                {
                    Debug.LogWarning("[TutorialDebugger] Please assign a target SlideDefinition asset to jump to.");
                }
            }

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Slide Step Navigation", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("◀ Previous Slide", GUILayout.Height(28)))
            {
                controller.OnPreviousRequested();
            }
            if (GUILayout.Button("Next Slide ▶", GUILayout.Height(28)))
            {
                controller.OnNextRequested();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndScrollView();

            Repaint();
        }
    }
}
#endif