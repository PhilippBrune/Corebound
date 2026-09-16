using UnityEngine;

namespace Corebound.Gameplay.Waves
{
    [CreateAssetMenu(menuName = "Corebound/Gameplay/Waves/Stage Progression Data", fileName = "StageProgressionData")]
    public sealed class StageProgressionData : ScriptableObject
    {
        [SerializeField] private StageData[] _stages;
        [SerializeField] private bool _allowBranching;

        public StageData[] Stages => _stages;

        public bool AllowBranching => _allowBranching;

        public StageData GetNextStage(StageData currentStage)
        {
            if (currentStage == null || _stages == null || _stages.Length == 0)
            {
                return null;
            }

            for (var index = 0; index < _stages.Length; index++)
            {
                if (_stages[index] != currentStage)
                {
                    continue;
                }

                var nextIndex = index + 1;
                return nextIndex >= 0 && nextIndex < _stages.Length ? _stages[nextIndex] : null;
            }

            return null;
        }
    }
}