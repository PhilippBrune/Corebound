using UnityEngine;

namespace Corebound.Gameplay.Enemies
{
    [CreateAssetMenu(menuName = "Corebound/Gameplay/Enemies/Enemy Behaviour Data", fileName = "EnemyBehaviourData")]
    public sealed class EnemyBehaviourData : ScriptableObject
    {
        [SerializeField] private EnemyRoleType _role = EnemyRoleType.Chaser;
        [SerializeField] private float _moveSpeed = 2f;
        [SerializeField] private float _playerAggroRange = 4f;
        [SerializeField] private float _preferredCombatDistance = 2.8f;
        [SerializeField] private float _strafeSpeedMultiplier = 0.6f;
        [SerializeField] private float _chargeSpeedMultiplier = 2.2f;
        [SerializeField] private float _chargeWindupSeconds = 0.4f;
        [SerializeField] private float _chargeDurationSeconds = 0.55f;
        [SerializeField] private float _chargeCooldownSeconds = 2.4f;

        public EnemyRoleType Role => _role;

        public float MoveSpeed => _moveSpeed;

        public float PlayerAggroRange => _playerAggroRange;

        public float PreferredCombatDistance => Mathf.Max(0.25f, _preferredCombatDistance);

        public float StrafeSpeedMultiplier => Mathf.Max(0.1f, _strafeSpeedMultiplier);

        public float ChargeSpeedMultiplier => Mathf.Max(0.1f, _chargeSpeedMultiplier);

        public float ChargeWindupSeconds => Mathf.Max(0f, _chargeWindupSeconds);

        public float ChargeDurationSeconds => Mathf.Max(0.05f, _chargeDurationSeconds);

        public float ChargeCooldownSeconds => Mathf.Max(0.1f, _chargeCooldownSeconds);
    }
}