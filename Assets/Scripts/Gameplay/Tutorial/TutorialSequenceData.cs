using UnityEngine;

namespace Corebound.Gameplay.Tutorial
{
    [CreateAssetMenu(menuName = "Corebound/Gameplay/Tutorial/Tutorial Sequence Data", fileName = "TutorialSequenceData")]
    public sealed class TutorialSequenceData : ScriptableObject
    {
        [SerializeField] private TutorialStepData[] _steps;

        public TutorialStepData[] Steps => _steps;
    }
}
