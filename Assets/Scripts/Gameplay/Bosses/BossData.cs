using Corebound.Gameplay.Combat;
using UnityEngine;

namespace Corebound.Gameplay.Bosses
{
    [CreateAssetMenu(menuName = "Corebound/Gameplay/Bosses/Boss Data", fileName = "BossData")]
    public sealed class BossData : ScriptableObject
    {
        [SerializeField] private string _bossId = "boss_stage1";
        [SerializeField] private string _displayName = "Stage Boss";
        [SerializeField] private float _moveSpeed = 1.45f;
        [SerializeField] private Sprite _sprite;
        [SerializeField] private CombatTargetData _combatTargetData;
        [SerializeField] private HitReactionData _hitReactionData;
        [SerializeField] private StatusEffectData[] _statusDefinitions;
        [SerializeField] private BossPhaseData[] _phases;
        [SerializeField] private BossRewardData _bossRewardData;
        [SerializeField] private Color _debugColor = new Color(0.9f, 0.25f, 0.25f, 1f);

        public string BossId => _bossId;

        public string DisplayName => _displayName;

        public float MoveSpeed => _moveSpeed;

        public Sprite Sprite => _sprite;

        public CombatTargetData CombatTargetData => _combatTargetData;

        public HitReactionData HitReactionData => _hitReactionData;

        public StatusEffectData[] StatusDefinitions => _statusDefinitions;

        public BossPhaseData[] Phases => _phases;

        public BossRewardData BossRewardData => _bossRewardData;

        public Color DebugColor => _debugColor;
    }
}