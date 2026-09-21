using Corebound.Gameplay.Combat;
using UnityEngine;

namespace Corebound.Gameplay.Bosses
{
    [CreateAssetMenu(menuName = "Corebound/Gameplay/Bosses/Boss Attack Pattern Data", fileName = "BossAttackPatternData")]
    public sealed class BossAttackPatternData : ScriptableObject
    {
        [SerializeField] private float _attackRange = 6f;
        [SerializeField] private float _preferredDistance = 4.5f;
        [SerializeField] private float _windupSeconds = 0.4f;
        [SerializeField] private float _fireIntervalSeconds = 0.9f;
        [SerializeField] private float _damageAmount = 2f;
        [SerializeField] private int _projectileCount = 3;
        [SerializeField] private float _spreadAngleDegrees = 24f;
        // Fan is the shape every existing pattern used, so leaving this at its default keeps
        // both authored assets behaving exactly as before.
        [SerializeField] private BossAttackShape _attackShape = BossAttackShape.Fan;

        // Degrees the whole pattern rotates between consecutive volleys. Only Spiral reads it.
        [SerializeField] private float _spiralStepDegrees = 23f;

        // World-space gap between parallel shots. Only Volley reads it.
        [SerializeField] private float _volleySpacing = 0.85f;

        [SerializeField] private bool _trackPlayer = true;
        [SerializeField] private ProjectileData _projectileData;

        public float AttackRange => _attackRange;

        public float PreferredDistance => _preferredDistance;

        public float WindupSeconds => Mathf.Max(0f, _windupSeconds);

        public float FireIntervalSeconds => _fireIntervalSeconds;

        public float DamageAmount => _damageAmount;

        public int ProjectileCount => Mathf.Max(1, _projectileCount);

        public float SpreadAngleDegrees => _spreadAngleDegrees;

        public BossAttackShape AttackShape => _attackShape;

        public float SpiralStepDegrees => _spiralStepDegrees;

        public float VolleySpacing => Mathf.Max(0.1f, _volleySpacing);

        public bool TrackPlayer => _trackPlayer;

        public ProjectileData ProjectileData => _projectileData;
    }
}