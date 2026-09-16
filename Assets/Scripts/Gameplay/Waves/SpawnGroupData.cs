using Corebound.Gameplay.Enemies;
using UnityEngine;

namespace Corebound.Gameplay.Waves
{
    [CreateAssetMenu(menuName = "Corebound/Gameplay/Waves/Spawn Group Data", fileName = "SpawnGroupData")]
    public sealed class SpawnGroupData : ScriptableObject
    {
        [SerializeField] private EnemyData _enemyData;
        [SerializeField] private EnemyEliteModifierData _eliteModifierData;
        [SerializeField] private int _count = 1;
        [SerializeField] private float _initialDelaySeconds;
        [SerializeField] private float _spawnIntervalSeconds = 0.35f;

        public EnemyData EnemyData => _enemyData;

        public EnemyEliteModifierData EliteModifierData => _eliteModifierData;

        public int Count => Mathf.Max(1, _count);

        public float InitialDelaySeconds => _initialDelaySeconds;

        public float SpawnIntervalSeconds => _spawnIntervalSeconds;
    }
}