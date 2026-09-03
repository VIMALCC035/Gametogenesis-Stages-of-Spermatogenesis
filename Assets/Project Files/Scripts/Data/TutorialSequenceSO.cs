using System.Collections.Generic;
using UnityEngine;

namespace TutorialFramework.Data
{
    [CreateAssetMenu(fileName = "TutorialSequence_Main", menuName = "Tutorial Framework/Tutorial Sequence")]
    public class TutorialSequenceSO : ScriptableObject
    {
        [Tooltip("The initial starting slide for production execution.")]
        public SlideDefinition rootSlide;

        [Tooltip("All registered slides in this project sequence (for quick debugging, validation, and jumping).")]
        public List<SlideDefinition> allSlides = new List<SlideDefinition>();
    }
}
