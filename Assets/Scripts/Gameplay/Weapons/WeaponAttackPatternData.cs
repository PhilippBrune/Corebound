using UnityEngine;

namespace Corebound.Gameplay.Weapons
{
    [CreateAssetMenu(menuName = "Corebound/Gameplay/Weapons/Weapon Attack Pattern Data", fileName = "WeaponAttackPatternData")]
    public sealed class WeaponAttackPatternData : ScriptableObject
    {
        [SerializeField] private float _fireIntervalSeconds = 0.2f;
        [SerializeField] private int _projectileCount = 1;
        [SerializeField] private float _spreadAngleDegrees;
        [SerializeField] private float _projectileSpawnDistance = 0.5f;

        public float FireIntervalSeconds => _fireIntervalSeconds;

        public int ProjectileCount => Mathf.Max(1, _projectileCount);

        public float SpreadAngleDegrees => _spreadAngleDegrees;

        public float ProjectileSpawnDistance => _projectileSpawnDistance;
    }
}