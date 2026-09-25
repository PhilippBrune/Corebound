using System;
using Corebound.Input;
using Corebound.Gameplay.Combat;
using Corebound.Gameplay.Shared;
using UnityEngine;

namespace Corebound.Gameplay.Player
{
    public sealed class PlayerController : MonoBehaviour
    {
        private const string PlayerSpritePath = "Assets/Art/Characters/Player/player_base_topdown.png";

        [SerializeField] private PlayerData _playerData;
        [SerializeField] private InputContextData _inputContext;
        [SerializeField] private UnityEngine.Object _inputReaderSource;

        private readonly PlayerMovementMotor _movementMotor = new PlayerMovementMotor();

        private Rigidbody2D _rigidbody2D;
        private CircleCollider2D _circleCollider2D;
        private SpriteRenderer _spriteRenderer;
        private PlayerCombatController _combatController;
        private bool _hasRaisedMoved;

        public event Action Moved;

        // What the player actually moves at right now, cards and skills included. Read by
        // ArenaObjectiveService to compute how long an arena objective must stay reachable
        // (D4 option 2) rather than hardcoding a duration per stage.
        public float CurrentMoveSpeed => (_playerData != null ? _playerData.MoveSpeed : 4f)
            * (_combatController != null ? _combatController.MovementSpeedMultiplier : 1f);

        private IInputReader InputReader => _inputReaderSource as IInputReader;

        private void Awake()
        {
            EnsureComponents();
            ApplyVisualDefaults();
            _combatController = GetComponent<PlayerCombatController>();
        }

        private void OnEnable()
        {
            if (InputReader != null)
            {
                InputReader.ApplyContext(_inputContext);
            }
        }

        private void FixedUpdate()
        {
            if (_playerData == null || _rigidbody2D == null)
            {
                return;
            }

            var moveInput = InputReader != null ? InputReader.Move : Vector2.zero;
            var speedMultiplier = _combatController != null ? _combatController.MovementSpeedMultiplier : 1f;
            var velocity = _movementMotor.ComputeVelocity(moveInput, _playerData.MoveSpeed * speedMultiplier);

            _rigidbody2D.linearVelocity = velocity;

            if (!_hasRaisedMoved && velocity.sqrMagnitude > Mathf.Epsilon)
            {
                _hasRaisedMoved = true;
                Moved?.Invoke();
            }
        }

        private void EnsureComponents()
        {
            _rigidbody2D = GetComponent<Rigidbody2D>();

            if (_rigidbody2D == null)
            {
                _rigidbody2D = gameObject.AddComponent<Rigidbody2D>();
            }

            _rigidbody2D.bodyType = RigidbodyType2D.Dynamic;
            _rigidbody2D.gravityScale = 0f;
            _rigidbody2D.freezeRotation = true;
            _rigidbody2D.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            _rigidbody2D.interpolation = RigidbodyInterpolation2D.Interpolate;

            _circleCollider2D = GetComponent<CircleCollider2D>();

            if (_circleCollider2D == null)
            {
                _circleCollider2D = gameObject.AddComponent<CircleCollider2D>();
            }

            _spriteRenderer = GetComponent<SpriteRenderer>();

            if (_spriteRenderer == null)
            {
                _spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            }
        }

        private void ApplyVisualDefaults()
        {
            if (_playerData == null)
            {
                return;
            }

            _circleCollider2D.radius = _playerData.CollisionRadius * 2f;

            var sprite = _playerData.Sprite != null ? _playerData.Sprite : DebugSpriteFactory.LoadProjectSprite(PlayerSpritePath);

            if (sprite != null)
            {
                _spriteRenderer.sprite = sprite;
                _spriteRenderer.color = Color.white;
                DebugSpriteFactory.FitTransformToSprite(transform, sprite, Vector2.one * (_playerData.CollisionRadius * 2.6f));
            }
            else if (_spriteRenderer.sprite == null)
            {
                _spriteRenderer.sprite = DebugSpriteFactory.CreateSquare(32f);
                _spriteRenderer.color = _playerData.DebugColor;
            }
        }
    }
}