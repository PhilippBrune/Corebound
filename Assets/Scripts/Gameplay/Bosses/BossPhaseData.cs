using Corebound.Gameplay.Waves;
using UnityEngine;

namespace Corebound.Gameplay.Bosses
{
    [CreateAssetMenu(menuName = "Corebound/Gameplay/Bosses/Boss Phase Data", fileName = "BossPhaseData")]
    public sealed class BossPhaseData : ScriptableObject
    {
        // Shown on the boss bar, so it is a name, not an id (Stages 2-5 read "ReactorSentinel P1"
        // until 2026-10-07).
        [SerializeField] private string _phaseLabel = "Phase";
        [SerializeField] private string _phaseLabelDe = "Phase";
        [SerializeField] [Range(0f, 1f)] private float _triggerHealthNormalized = 1f;
        [SerializeField] private BossAttackPatternData _attackPatternData;
        [SerializeField] private SpawnGroupData[] _helperSpawnGroups;

        public string PhaseLabel => Corebound.UI.LocalizationService.Resolve(_phaseLabel, _phaseLabelDe);

        public float TriggerHealthNormalized => _triggerHealthNormalized;

        public BossAttackPatternData AttackPatternData => _attackPatternData;

        public SpawnGroupData[] HelperSpawnGroups => _helperSpawnGroups;
    }
}