using Corebound.Gameplay.Combat;
using UnityEngine;

namespace Corebound.Gameplay.Enemies
{
    [CreateAssetMenu(menuName = "Corebound/Gameplay/Enemies/Enemy Data", fileName = "EnemyData")]
    public sealed class EnemyData : ScriptableObject
    {
        [SerializeField] private string _enemyId = "enemy_default";
        [SerializeField] private string _displayName = "Enemy";
        [SerializeField] private Sprite _sprite;
        [SerializeField] private EnemyBehaviourData _behaviourData;
        [SerializeField] private EnemyAttackData _attackData;
        [SerializeField] private EnemyRewardData _rewardData;
        [SerializeField] private CombatTargetData _combatTargetData;
        [SerializeField] private HitReactionData _hitReactionData;
        [SerializeField] private StatusEffectData[] _statusDefinitions;
        [SerializeField] private Color _debugColor = new Color(1f, 0.4f, 0.4f, 1f);

        public string EnemyId => _enemyId;

        public string DisplayName => _displayName;

        public Sprite Sprite => _sprite;

        public EnemyBehaviourData BehaviourData => _behaviourData;

        public EnemyAttackData AttackData => _attackData;

        public EnemyRewardData RewardData => _rewardData;

        public CombatTargetData CombatTargetData => _combatTargetData;

        public HitReactionData HitReactionData => _hitReactionData;

        public StatusEffectData[] StatusDefinitions => _statusDefinitions;

        public Color DebugColor => _debugColor;
    }
}