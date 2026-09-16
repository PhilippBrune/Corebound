using UnityEngine;
using Corebound.Gameplay.Combat;

namespace Corebound.Gameplay.Enemies
{
    [CreateAssetMenu(menuName = "Corebound/Gameplay/Enemies/Enemy Attack Data", fileName = "EnemyAttackData")]
    public sealed class EnemyAttackData : ScriptableObject
    {
        [SerializeField] private EnemyAttackMode _attackMode = EnemyAttackMode.Contact;
        [SerializeField] private float _contactDamage = 1f;
        [SerializeField] private float _contactIntervalSeconds = 0.6f;
        [SerializeField] private float _attackRange = 4f;
        [SerializeField] private float _windupSeconds = 0.35f;
        [SerializeField] private float _fireIntervalSeconds = 1.2f;
        [SerializeField] private int _projectileCount = 1;
        [SerializeField] private float _spreadAngleDegrees = 16f;
        [SerializeField] private ProjectileData _projectileData;
        [SerializeField] private bool _appliesStatusEffect;
        [SerializeField] private StatusEffectType _statusEffectType = StatusEffectType.Burn;

        public EnemyAttackMode AttackMode => _attackMode;

        public float ContactDamage => _contactDamage;

        public float ContactIntervalSeconds => _contactIntervalSeconds;

        public float AttackRange => _attackRange;

        public float WindupSeconds => Mathf.Max(0f, _windupSeconds);

        public float FireIntervalSeconds => _fireIntervalSeconds;

        public int ProjectileCount => Mathf.Max(1, _projectileCount);

        public float SpreadAngleDegrees => _spreadAngleDegrees;

        public ProjectileData ProjectileData => _projectileData;

        public StatusEffectType? ResolveStatusEffect()
        {
            return _appliesStatusEffect ? _statusEffectType : (StatusEffectType?)null;
        }
    }
}