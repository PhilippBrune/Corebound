using UnityEngine;

namespace Corebound.Gameplay.Combat
{
    [CreateAssetMenu(menuName = "Corebound/Gameplay/Combat/Combat Target", fileName = "CombatTarget")]
    public sealed class CombatTargetData : ScriptableObject
    {
        [SerializeField] private float _maxHealth = 12f;
        [SerializeField] private Color _aliveColor = new Color(1f, 0.35f, 0.35f, 1f);
        [SerializeField] private Color _deadColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        [SerializeField] private Vector2 _size = new Vector2(0.8f, 0.8f);

        public float MaxHealth => _maxHealth;

        public Color AliveColor => _aliveColor;

        public Color DeadColor => _deadColor;

        public Vector2 Size => _size;
    }
}