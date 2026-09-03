using UnityEngine;
using TutorialFramework.Core;
using TutorialFramework.Data;

namespace TutorialFramework.Bootstrap
{
    /// <summary>
    /// Utility for establishing prerequisite scene state and testing any slide directly without walking through earlier slides.
    /// </summary>
    public static class TutorialBootstrap
    {
        public static void InitializeSlideForTesting(SlideDefinition targetSlide, TutorialController controller)
        {
            if (targetSlide == null || controller == null) return;

            var stateManager = controller.GetComponent<TutorialStateManager>();
            if (stateManager != null)
            {
                stateManager.ResetToBaseline();
            }

            controller.LoadDirectTestSlide(targetSlide);
        }
    }
}
