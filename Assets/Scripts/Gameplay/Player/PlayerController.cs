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

        // 2026-10-07: all five player sprites - the Vanguard's base sprite and the four classes' -
        // go through one path, and only the picture depends on the file:
        // - each PNG is cropped to its visible figure and drawn with the figure's longer side at
        //   FigureWorldSize, centred on the transform;
        // - the transform's scale is a constant, so nothing parented to the player rescales;
        // - the collider's world radius is a constant too: the radius the game has been tuned with
        //   since the first player sprite (CollisionRadius x 2 at that sprite's fit scale).
        // Before this the whole canvas was fitted, so the figure's size and the hitbox both followed
        // the transparent padding in the PNG - the redelivered 911 px Vanguard made the hitbox 20%
        // larger, and its imported slice still described the old 1088 x 606 file.
        private const float TransformScale = 0.0836f;
        private const float FigureWorldSize = 0.5f;

        private float HitboxWorldRadius => (_playerData != null ? _playerData.CollisionRadius : 0.35f) * 2f * TransformScale;

        // K6-2 (2026-09-30): each class has its own top-down sprite (Assets 110-113, delivered
        // 2026-10-06). A class without one wears the Vanguard's sprite in its colour.
        public void ApplyCharacterLook(Corebound.MetaProgression.CharacterData character)
        {
            if (character == null || _spriteRenderer == null || _playerData == null)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(character.SpritePath) && ApplyPlayerSprite(character.SpritePath))
            {
                _spriteRenderer.color = Color.white;
                return;
            }

            _spriteRenderer.color = character.Tint;
        }

        private bool ApplyPlayerSprite(string path)
        {
            var sprite = DebugSpriteFactory.LoadProjectSpriteTrimmed(path, FigureWorldSize / TransformScale);

            if (sprite == null)
            {
                return false;
            }

            _spriteRenderer.sprite = sprite;
            transform.localScale = new Vector3(TransformScale, TransformScale, 1f);
            _circleCollider2D.radius = HitboxWorldRadius / TransformScale;
            return true;
        }

        private void ApplyVisualDefaults()
        {
            if (_playerData == null)
            {
                return;
            }

            if (ApplyPlayerSprite(PlayerSpritePath))
            {
                _spriteRenderer.color = Color.white;
                return;
            }

            // The file could not be read (no StreamingAssets mirror in a build): the imported sprite,
            // then a plain square, both at the same constant scale and hitbox.
            transform.localScale = new Vector3(TransformScale, TransformScale, 1f);
            _circleCollider2D.radius = HitboxWorldRadius / TransformScale;

            if (_playerData.Sprite != null)
            {
                _spriteRenderer.sprite = _playerData.Sprite;
                _spriteRenderer.color = Color.white;
            }
            else if (_spriteRenderer.sprite == null)
            {
                _spriteRenderer.sprite = DebugSpriteFactory.CreateSquare(32f);
                _spriteRenderer.color = _playerData.DebugColor;
            }
        }
    }
}