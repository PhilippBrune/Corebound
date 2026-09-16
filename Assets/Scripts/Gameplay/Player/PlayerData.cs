using UnityEngine;

namespace Corebound.Gameplay.Player
{
    [CreateAssetMenu(menuName = "Corebound/Gameplay/Player/Player Data", fileName = "PlayerData")]
    public sealed class PlayerData : ScriptableObject
    {
        [SerializeField] private float _moveSpeed = 5f;
        [SerializeField] private float _collisionRadius = 0.35f;
        [SerializeField] private Sprite _sprite;
        [SerializeField] private Color _debugColor = new Color(0.25f, 0.85f, 1f, 1f);

        public float MoveSpeed => _moveSpeed;

        public float CollisionRadius => _collisionRadius;

        public Sprite Sprite => _sprite;

        public Color DebugColor => _debugColor;
    }
}