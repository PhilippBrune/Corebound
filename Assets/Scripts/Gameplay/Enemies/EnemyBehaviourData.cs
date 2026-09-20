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

        // AreaControl: how close to its target it advances before anchoring. Larger than
        // PreferredCombatDistance on purpose - it is meant to hold a ring of ground, not to
        // close in the way the ranged role does.
        [SerializeField] private float _zoneHoldDistance = 4.5f;

        // Support: 0 radius means no aura, so every existing behaviour asset is unaffected.
        [SerializeField] private float _supportAuraRadius;
        [SerializeField] private float _supportSpeedMultiplier = 1.25f;
        [SerializeField] private float _supportDamageMultiplier = 1.3f;

        public EnemyRoleType Role => _role;

        public float MoveSpeed => _moveSpeed;

        public float PlayerAggroRange => _playerAggroRange;

        public float PreferredCombatDistance => Mathf.Max(0.25f, _preferredCombatDistance);

        public float StrafeSpeedMultiplier => Mathf.Max(0.1f, _strafeSpeedMultiplier);

        public float ChargeSpeedMultiplier => Mathf.Max(0.1f, _chargeSpeedMultiplier);

        public float ChargeWindupSeconds => Mathf.Max(0f, _chargeWindupSeconds);

        public float ChargeDurationSeconds => Mathf.Max(0.05f, _chargeDurationSeconds);

        public float ChargeCooldownSeconds => Mathf.Max(0.1f, _chargeCooldownSeconds);

        public float ZoneHoldDistance => Mathf.Max(0.5f, _zoneHoldDistance);

        public float SupportAuraRadius => Mathf.Max(0f, _supportAuraRadius);

        public float SupportSpeedMultiplier => Mathf.Max(0.1f, _supportSpeedMultiplier);

        public float SupportDamageMultiplier => Mathf.Max(0.1f, _supportDamageMultiplier);
    }
}