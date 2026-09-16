using UnityEngine;

namespace Corebound.Gameplay.Enemies
{
    [CreateAssetMenu(menuName = "Corebound/Gameplay/Enemies/Enemy Elite Modifier Data", fileName = "EnemyEliteModifierData")]
    public sealed class EnemyEliteModifierData : ScriptableObject
    {
        [SerializeField] private string _displayName = "Elite";
        [SerializeField] private float _healthMultiplier = 1.8f;
        [SerializeField] private float _moveSpeedMultiplier = 1.15f;
        [SerializeField] private float _damageMultiplier = 1.4f;
        [SerializeField] private float _rewardMultiplier = 2f;
        [SerializeField] private int _projectileBonusCount = 1;
        [SerializeField] private Color _colorTint = new Color(1f, 0.8f, 0.35f, 1f);

        public string DisplayName => _displayName;

        public float HealthMultiplier => _healthMultiplier;

        public float MoveSpeedMultiplier => _moveSpeedMultiplier;

        public float DamageMultiplier => _damageMultiplier;

        public float RewardMultiplier => _rewardMultiplier;

        public int ProjectileBonusCount => Mathf.Max(0, _projectileBonusCount);

        public Color ColorTint => _colorTint;
    }
}