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

        // Owner decision 2026-09-22 (Upgrades.md option a): a real reload loop. A volley consumes
        // one round; an empty magazine reloads for _reloadSeconds, and releasing the trigger tops
        // the magazine up after the same delay. 0 rounds means "no magazine" (never reloads).
        [SerializeField] private int _magazineSize = 12;
        [SerializeField] private float _reloadSeconds = 1.2f;

        public float FireIntervalSeconds => _fireIntervalSeconds;

        public int ProjectileCount => Mathf.Max(1, _projectileCount);

        public float SpreadAngleDegrees => _spreadAngleDegrees;

        public float ProjectileSpawnDistance => _projectileSpawnDistance;

        public int MagazineSize => Mathf.Max(0, _magazineSize);

        public float ReloadSeconds => Mathf.Max(0.05f, _reloadSeconds);
    }
}