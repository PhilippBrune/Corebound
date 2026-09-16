using Corebound.Gameplay.Enemies;
using UnityEngine;

namespace Corebound.Gameplay.Waves
{
    [CreateAssetMenu(menuName = "Corebound/Gameplay/Waves/Stage Enemy Pool Data", fileName = "StageEnemyPoolData")]
    public sealed class StageEnemyPoolData : ScriptableObject
    {
        [System.Serializable]
        public sealed class EnemyPoolEntry
        {
            [SerializeField] private EnemyData _enemyData;
            [SerializeField] private float _weight = 1f;
            [SerializeField] private bool _canSpawnAsElite = true;

            public EnemyData EnemyData => _enemyData;

            public float Weight => Mathf.Max(0f, _weight);

            public bool CanSpawnAsElite => _canSpawnAsElite;
        }

        [SerializeField] private EnemyPoolEntry[] _entries;

        public EnemyPoolEntry[] Entries => _entries;
    }
}