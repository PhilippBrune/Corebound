using Corebound.Gameplay.Waves;
using UnityEngine;

namespace Corebound.Gameplay.Bosses
{
    [CreateAssetMenu(menuName = "Corebound/Gameplay/Bosses/Boss Phase Data", fileName = "BossPhaseData")]
    public sealed class BossPhaseData : ScriptableObject
    {
        [SerializeField] private string _phaseLabel = "Phase";
        [SerializeField] [Range(0f, 1f)] private float _triggerHealthNormalized = 1f;
        [SerializeField] private BossAttackPatternData _attackPatternData;
        [SerializeField] private SpawnGroupData[] _helperSpawnGroups;

        public string PhaseLabel => _phaseLabel;

        public float TriggerHealthNormalized => _triggerHealthNormalized;

        public BossAttackPatternData AttackPatternData => _attackPatternData;

        public SpawnGroupData[] HelperSpawnGroups => _helperSpawnGroups;
    }
}