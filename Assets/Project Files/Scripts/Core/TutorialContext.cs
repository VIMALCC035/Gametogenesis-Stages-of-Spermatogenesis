using UnityEngine;
using TutorialFramework.RuntimeComponents;

namespace TutorialFramework.Core
{
    public enum EntryDirection
    {
        Forward,
        Backward,
        DirectTest
    }

    /// <summary>
    /// Service locator passed to TutorialActions during execution.
    /// Provides access to UI, Camera, State, and Highlighting without singleton coupling.
    /// </summary>
    public class TutorialContext
    {
        public MonoBehaviour CoroutineRunner { get; }
        public TutorialUIManager UI { get; }
        public TutorialCameraController Camera { get; }
        public HighlightManager Highlight { get; }
        public TutorialStateManager StateManager { get; }
        public EntryDirection Direction { get; }

        public TutorialContext(
            MonoBehaviour coroutineRunner,
            TutorialUIManager ui,
            TutorialCameraController camera,
            HighlightManager highlight,
            TutorialStateManager stateManager,
            EntryDirection direction)
        {
            CoroutineRunner = coroutineRunner;
            UI = ui;
            Camera = camera;
            Highlight = highlight;
            StateManager = stateManager;
            Direction = direction;
        }
    }
}
