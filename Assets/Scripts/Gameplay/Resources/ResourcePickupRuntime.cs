using Corebound.Gameplay.Shared;
using UnityEngine;

namespace Corebound.Gameplay.Resources
{
    // A real world pickup for the primary Run Resource, closing the Resources.md gap where
    // enemy kills previously credited the wallet instantly with no physical drop. Sits where it
    // spawned until the player is within AutoPickupRange, then flies in and gets collected; a
    // wave-end vacuum (ForceCollect) pulls in whatever the player did not walk over, matching the
    // resolved "short auto-pickup range in combat + full vacuum at wave end" decision.
    public sealed class ResourcePickupRuntime : MonoBehaviour
    {
        private const string PickupSpritePath = "Assets/Art/Resources/resource_primary_icon.png";
        private const float CollectDistance = 0.3f;
        private const float VacuumSpeedMultiplier = 2.6f;

        private ResourceWalletService _resourceWalletService;
        private Transform _playerTarget;
        private SpriteRenderer _spriteRenderer;
        private int _value;
        private float _autoPickupRange;
        private float _moveSpeed;
        private bool _isVacuuming;

        public void Initialize(int value, Vector2 position, Transform playerTarget, ResourceWalletService resourceWalletService)
        {
            EnsureComponents();

            _value = value;
            _playerTarget = playerTarget;
            _resourceWalletService = resourceWalletService;
            _autoPickupRange = resourceWalletService != null ? resourceWalletService.AutoPickupRange : 1.8f;
            _moveSpeed = resourceWalletService != null ? resourceWalletService.PickupMoveSpeed : 4.5f;
            _isVacuuming = false;

            transform.position = position;
            gameObject.SetActive(true);
        }

        public void ForceCollect()
        {
            if (gameObject.activeSelf)
            {
                _isVacuuming = true;
            }
        }

        private void Update()
        {
            if (_playerTarget == null)
            {
                return;
            }

            var toPlayer = (Vector2)_playerTarget.position - (Vector2)transform.position;
            var distance = toPlayer.magnitude;

            if (!_isVacuuming && distance > _autoPickupRange)
            {
                return;
            }

            if (distance <= CollectDistance)
            {
                Collect();
                return;
            }

            var direction = toPlayer / distance;
            var speed = _isVacuuming ? _moveSpeed * VacuumSpeedMultiplier : _moveSpeed;
            transform.position += (Vector3)(direction * speed * Time.deltaTime);
        }

        private void Collect()
        {
            if (_resourceWalletService != null)
            {
                _resourceWalletService.AddReward(_value);
            }

            gameObject.SetActive(false);
        }

        private void EnsureComponents()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();

            if (_spriteRenderer == null)
            {
                _spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            }

            if (_spriteRenderer.sprite == null)
            {
                var sprite = DebugSpriteFactory.LoadProjectSprite(PickupSpritePath);

                if (sprite != null)
                {
                    _spriteRenderer.sprite = sprite;
                    _spriteRenderer.color = Color.white;
                    DebugSpriteFactory.FitTransformToSprite(transform, sprite, Vector2.one * 0.4f);
                }
                else
                {
                    _spriteRenderer.sprite = DebugSpriteFactory.CreateSquare(32f);
                    _spriteRenderer.color = new Color(1f, 0.83f, 0.34f, 1f);
                    transform.localScale = Vector3.one * 0.3f;
                }
            }

            _spriteRenderer.sortingOrder = -6;
        }
    }
}
