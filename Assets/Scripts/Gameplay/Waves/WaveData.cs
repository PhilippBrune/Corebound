using UnityEngine;

namespace Corebound.Gameplay.Waves
{
    [CreateAssetMenu(menuName = "Corebound/Gameplay/Waves/Wave Data", fileName = "WaveData")]
    public sealed class WaveData : ScriptableObject
    {
        [SerializeField] private string _waveId = "wave_01";
        [SerializeField] private string _displayLabel = "Wave 1";
        [SerializeField] private SpawnGroupData[] _spawnGroups;
        [SerializeField] private WaveModifierData _modifierData;
        [SerializeField] private WaveRewardData _waveRewardData;

        public string WaveId => _waveId;

        public string DisplayLabel => _displayLabel;

        public SpawnGroupData[] SpawnGroups => _spawnGroups;

        public WaveModifierData ModifierData => _modifierData;

        public WaveRewardData WaveRewardData => _waveRewardData;
    }
}