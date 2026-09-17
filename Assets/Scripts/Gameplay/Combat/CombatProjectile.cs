using System.Collections.Generic;
using Corebound.Gameplay.Shared;
using UnityEngine;

namespace Corebound.Gameplay.Combat
{
    public sealed class CombatProjectile : MonoBehaviour
    {
        private readonly HashSet<GameObject> _hitTargets = new HashSet<GameObject>();

        private float _remainingLifetime;
        private float _speed;
        private Vector2 _direction;
        private DamageContext _damageContext;
        private GameObject _owner;
        private SpriteRenderer _spriteRenderer;
        private CircleCollider2D _circleCollider2D;
        private Rigidbody2D _rigidbody2D;
        private int _remainingPierceCount;

        private void Awake()
        {
            EnsureComponents();
            gameObject.SetActive(false);
        }

        private void FixedUpdate()
        {
            if (!gameObject.activeSelf || _rigidbody2D == null)
            {
                return;
            }

            _rigidbody2D.MovePosition(_rigidbody2D.position + _direction * (_speed * Time.fixedDeltaTime));
            _remainingLifetime -= Time.fixedDeltaTime;

            if (_remainingLifetime <= 0f)
            {
                Release();
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!gameObject.activeSelf)
            {
                return;
            }

            var hitTargetObject = other.attachedRigidbody != null ? other.attachedRigidbody.gameObject : other.gameObject;
            var hitbox = (other.attachedRigidbody != null ? other.attachedRigidbody.GetComponent<ICombatHitReceiver>() : null)
                ?? other.GetComponent<ICombatHitReceiver>()
                ?? other.GetComponentInParent<ICombatHitReceiver>();

            if (hitTargetObject == _owner)
            {
                return;
            }

            if (hitbox == null)
            {
                if (!other.isTrigger)
                {
                    Release();
                }

                return;
            }

            if (!_hitTargets.Add(hitTargetObject))
            {
                // Already hit this target during this flight (its trigger can linger across
                // more than one physics step) - do not apply the same hit twice.
                return;
            }

            // Playtest feedback (2026-09-17): "the hit effect is shown on the enemy when it hits
            // the player or core - should be on player or core depending what was hit". Root
            // cause: _damageContext.HitPoint is baked in at Launch() time from the shooter's own
            // position (see EnemyRuntimeController.FireProjectileBurst/PlayerCombatController's
            // fire methods) and never updated as the projectile travels, so CombatVfxSpawner's
            // hit-spark/core-flare - which reads HitPoint, not the victim's current position -
            // rendered at the shooter's position instead of the actual impact point. Rebuilding
            // with the projectile's real position at the moment of collision fixes this for every
            // projectile (enemy and player alike) without needing to touch the VFX spawner itself.
            var impactContext = new DamageContext(
                _damageContext.SourceType,
                _damageContext.DamageKind,
                _damageContext.Amount,
                transform.position,
                _damageContext.HitDirection,
                _damageContext.StatusEffect);

            hitbox.ReceiveHit(_owner, impactContext);

            if (_remainingPierceCount > 0)
            {
                _remainingPierceCount -= 1;
                return;
            }

            Release();
        }

        public void Launch(GameObject owner, ProjectileData projectileData, Vector2 origin, Vector2 direction, DamageContext damageContext, float speedMultiplier = 1f)
        {
            EnsureComponents();
            _owner = owner;
            _direction = direction.sqrMagnitude < Mathf.Epsilon ? Vector2.right : direction.normalized;
            _speed = (projectileData != null ? projectileData.Speed : 10f) * Mathf.Max(0.05f, speedMultiplier);
            _remainingLifetime = projectileData != null ? projectileData.LifetimeSeconds : 1f;
            _remainingPierceCount = projectileData != null ? projectileData.PierceCount : 0;
            _damageContext = damageContext;
            _hitTargets.Clear();

            transform.position = origin;
            transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg);

            if (projectileData != null)
            {
                var sprite = projectileData.ResolveSprite();

                _spriteRenderer.sprite = sprite != null ? sprite : DebugSpriteFactory.CreateSquare(32f);
                _circleCollider2D.radius = projectileData.Radius;
                _spriteRenderer.color = projectileData.DebugColor;

                if (sprite != null)
                {
                    DebugSpriteFactory.FitTransformToSprite(transform, sprite, Vector2.one * (projectileData.Radius * 2f));
                }
                else
                {
                    transform.localScale = Vector3.one * (projectileData.Radius * 2f);
                }
            }
            else
            {
                _spriteRenderer.sprite = DebugSpriteFactory.CreateSquare(32f);
                _spriteRenderer.color = Color.white;
                transform.localScale = Vector3.one * 0.28f;
            }

            gameObject.SetActive(true);
        }

        private void Release()
        {
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
                _spriteRenderer.sprite = DebugSpriteFactory.CreateSquare(32f);
            }

            _circleCollider2D = GetComponent<CircleCollider2D>();

            if (_circleCollider2D == null)
            {
                _circleCollider2D = gameObject.AddComponent<CircleCollider2D>();
            }

            _circleCollider2D.isTrigger = true;

            var rigidbody2D = GetComponent<Rigidbody2D>();

            if (rigidbody2D == null)
            {
                rigidbody2D = gameObject.AddComponent<Rigidbody2D>();
            }

            rigidbody2D.bodyType = RigidbodyType2D.Kinematic;
            rigidbody2D.simulated = true;
            rigidbody2D.gravityScale = 0f;
            rigidbody2D.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            _rigidbody2D = rigidbody2D;
        }
    }
}