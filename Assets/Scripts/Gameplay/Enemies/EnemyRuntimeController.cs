using Corebound.Gameplay.Combat;
using Corebound.Gameplay.Shared;
using UnityEngine;

namespace Corebound.Gameplay.Enemies
{
    public sealed class EnemyRuntimeController : MonoBehaviour
    {
        private const string ChaserSpritePath = "Assets/Art/Characters/Enemies/enemy_chaser_topdown.png";
        private const string RangedSpritePath = "Assets/Art/Characters/Enemies/enemy_ranged_pressure_topdown.png";
        private const string BruiserSpritePath = "Assets/Art/Characters/Enemies/enemy_bruiser_topdown.png";
        // Playtest request (2026-09-16): use the actual bolt sprite as the telegraph instead of the
        // separate ranged_pressure_telegraph.png art, so the "ghost" that flies toward the target
        // during the windup (see ShowTelegraph()) looks identical to the real shot that follows it,
        // rather than a differently-styled warning icon.
        private const string RangedTelegraphSpritePath = "Assets/Art/Combat/Projectiles/enemy_bolt_projectile.png";
        private const string EliteMarkerSpritePath = "Assets/Art/Combat/VFX/elite_enemy_marker.png";

        private static readonly Color BruiserChargeWarningColor = new Color(1f, 0.92f, 0.35f, 1f);

        private EnemyData _enemyData;
        private EnemyEliteModifierData _eliteModifierData;
        private Transform _playerTarget;
        private Transform _coreTarget;
        private CombatProjectile _projectilePrototype;
        private Transform _projectileParent;
        private CombatHitReceiver _hitReceiver;
        private Rigidbody2D _rigidbody2D;
        private SpriteRenderer _spriteRenderer;
        private SpriteRenderer _telegraphRenderer;
        private SpriteRenderer _eliteMarkerRenderer;
        private ProjectilePool _projectilePool;

        private bool _hasTakenDamage;
        private float _contactCooldownTimer;
        private float _attackCooldownTimer;
        private float _telegraphTimer;
        private float _chargeWindupTimer;
        private float _chargeTimer;
        private float _chargeCooldownTimer;
        private Vector2 _chargeDirection;
        private Transform _currentTarget;
        private int _avoidanceSteerSign = 1;

        public bool IsAlive => _hitReceiver != null && _hitReceiver.IsAlive;

        public int ResourceRewardAmount => _enemyData != null && _enemyData.RewardData != null
            ? Mathf.RoundToInt(_enemyData.RewardData.ResourceRewardAmount * (_eliteModifierData != null ? _eliteModifierData.RewardMultiplier : 1f))
            : 0;

        public bool IsRangedPressureRole => _enemyData != null && _enemyData.BehaviourData != null && _enemyData.BehaviourData.Role == EnemyRoleType.RangedPressure;

        public void Initialize(
            EnemyData enemyData,
            EnemyEliteModifierData eliteModifierData,
            Transform playerTarget,
            Transform coreTarget,
            CombatProjectile projectilePrototype,
            Transform projectileParent)
        {
            _enemyData = enemyData;
            _eliteModifierData = eliteModifierData;
            _playerTarget = playerTarget;
            _coreTarget = coreTarget;
            _projectilePrototype = projectilePrototype;
            _projectileParent = projectileParent;
            _hasTakenDamage = false;
            _contactCooldownTimer = 0f;
            _attackCooldownTimer = 0f;
            _telegraphTimer = 0f;
            _chargeTimer = 0f;
            _chargeCooldownTimer = 0f;
            _currentTarget = null;
            _avoidanceSteerSign = GetInstanceID() % 2 == 0 ? 1 : -1;

            EnsureComponents();
            HideTelegraph();
            ApplyEliteMarker();
            _projectilePool = _projectileParent != null ? new ProjectilePool(_projectileParent) : null;

            if (_hitReceiver != null && _enemyData != null)
            {
                _hitReceiver.InitializeRuntime(
                    _enemyData.CombatTargetData,
                    _enemyData.HitReactionData,
                    _enemyData.StatusDefinitions,
                    _eliteModifierData != null ? _eliteModifierData.HealthMultiplier : 1f,
                    true,
                    ResolveAliveColor());

                var hitbox = GetComponent<BoxCollider2D>();

                if (hitbox != null && _enemyData.CombatTargetData != null)
                {
                    hitbox.size = _enemyData.CombatTargetData.Size * 2f;
                }

                // CombatHitReceiver.InitializeRuntime() unconditionally sets the Rigidbody2D to
                // Kinematic (correct for the stationary Core and the directly-controlled Player),
                // undoing the Dynamic mode EnsureComponents() just set above. Kinematic bodies are
                // never physically stopped by collisions, so enemies were passing straight through
                // the arena's static boundary walls instead of being blocked by them - re-assert
                // Dynamic here, after InitializeRuntime has had its say.
                if (_rigidbody2D != null)
                {
                    _rigidbody2D.bodyType = RigidbodyType2D.Dynamic;
                }
            }

            if (_spriteRenderer != null)
            {
                _spriteRenderer.color = ResolveAliveColor();
            }

            gameObject.name = _eliteModifierData != null
                ? _eliteModifierData.DisplayName + " " + (_enemyData != null ? _enemyData.DisplayName : "Enemy")
                : (_enemyData != null ? _enemyData.DisplayName : "Enemy");

            EnemyEvents.RaiseEnemySpawned(new EnemyLifecycleEventArgs(gameObject, _enemyData, _eliteModifierData));
        }

        private void OnEnable()
        {
            CombatEvents.DamageTaken += OnDamageTaken;
            CombatEvents.CombatantDied += OnCombatantDied;
        }

        private void OnDisable()
        {
            CombatEvents.DamageTaken -= OnDamageTaken;
            CombatEvents.CombatantDied -= OnCombatantDied;
        }

        private void FixedUpdate()
        {
            if (_enemyData == null || _rigidbody2D == null || !IsAlive)
            {
                return;
            }

            _contactCooldownTimer = Mathf.Max(0f, _contactCooldownTimer - Time.fixedDeltaTime);
            _attackCooldownTimer = Mathf.Max(0f, _attackCooldownTimer - Time.fixedDeltaTime);
            _chargeCooldownTimer = Mathf.Max(0f, _chargeCooldownTimer - Time.fixedDeltaTime);

            var target = ResolveTarget();

            if (target != _currentTarget)
            {
                _currentTarget = target;

                if (target != null)
                {
                    EnemyEvents.RaiseEnemyTargetChanged(new EnemyTargetChangedEventArgs(gameObject, _enemyData, target, target == _playerTarget));
                }
            }

            if (target == null)
            {
                _rigidbody2D.linearVelocity = Vector2.zero;
                HideTelegraph();
                return;
            }

            var direction = ((Vector2)target.position - _rigidbody2D.position);
            var attackData = _enemyData.AttackData;
            var isRangedEnemy = attackData != null && attackData.AttackMode == EnemyAttackMode.ProjectileBurst;
            var moveSpeed = _enemyData.BehaviourData != null ? _enemyData.BehaviourData.MoveSpeed : 2f;

            if (_eliteModifierData != null)
            {
                moveSpeed *= _eliteModifierData.MoveSpeedMultiplier;
            }

            if (_hitReceiver != null)
            {
                moveSpeed *= _hitReceiver.MovementSpeedMultiplier;
            }

            if (UpdateBruiserPressure(direction, moveSpeed))
            {
                return;
            }

            if (UpdateRangedPressure(direction, attackData, moveSpeed))
            {
                return;
            }

            if (direction.sqrMagnitude <= 0.04f)
            {
                _rigidbody2D.linearVelocity = Vector2.zero;
                return;
            }

            if (isRangedEnemy && direction.magnitude > attackData.AttackRange)
            {
                var avoidingDirection = ApplyObstacleAvoidance(direction.normalized);
                _rigidbody2D.linearVelocity = avoidingDirection * moveSpeed;
                FaceMovementDirection(avoidingDirection);
                return;
            }

            var moveDirection = ApplyObstacleAvoidance(direction.normalized);
            _rigidbody2D.linearVelocity = moveDirection * moveSpeed;
            FaceMovementDirection(moveDirection);
        }

        // enemy_chaser_topdown.png (and the other top-down enemy art) is drawn nose-pointing-left
        // at its default rotation, not nose-pointing-right like Atan2's 0-degree convention -
        // without the 180-degree offset the sprite would visibly face away from the direction it
        // is actually moving in.
        private void FaceMovementDirection(Vector2 direction)
        {
            if (direction.sqrMagnitude <= 0.0001f || _rigidbody2D == null)
            {
                return;
            }

            var angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + 180f;
            _rigidbody2D.MoveRotation(angle);
        }

        // Enemy movement is direct-line-to-target with no real pathfinding. That was masked while
        // every enemy unconditionally chased the moving Player (ResolveTarget's old Chaser
        // shortcut) - a moving target rarely leaves an agent stuck on the same obstacle for long.
        // Now that enemies more often beeline the stationary Core (see ResolveTarget), a straight
        // path crossing one of StageArenaArtPresenter's solid props leaves them pushing into it
        // forever, reading as "enemy just stands there doing nothing". Obstacles/boundary walls are
        // plain BoxCollider2D with no Rigidbody2D, unlike the Player/Core/other enemies, so that
        // presence check reliably tells a static obstacle apart from a legitimate target to reach.
        private Vector2 ApplyObstacleAvoidance(Vector2 desiredDirection)
        {
            if (desiredDirection.sqrMagnitude <= Mathf.Epsilon || _rigidbody2D == null)
            {
                return desiredDirection;
            }

            const float probeDistance = 0.9f;
            var hit = Physics2D.Raycast(_rigidbody2D.position, desiredDirection, probeDistance);

            if (hit.collider == null || hit.rigidbody != null)
            {
                return desiredDirection;
            }

            var perpendicular = new Vector2(-desiredDirection.y, desiredDirection.x) * _avoidanceSteerSign;
            return (desiredDirection * 0.35f + perpendicular).normalized;
        }

        private void OnCollisionStay2D(Collision2D collision)
        {
            if (_contactCooldownTimer > 0f || _enemyData == null || !IsAlive)
            {
                return;
            }

            if (_enemyData.AttackData != null && _enemyData.AttackData.AttackMode != EnemyAttackMode.Contact)
            {
                return;
            }

            var playerCombatController = collision.rigidbody != null ? collision.rigidbody.GetComponent<PlayerCombatController>() : null;
            var coreHitReceiver = collision.rigidbody != null && _coreTarget != null && collision.rigidbody.transform == _coreTarget
                ? collision.rigidbody.GetComponent<CombatHitReceiver>()
                : null;

            if (playerCombatController == null && coreHitReceiver == null)
            {
                return;
            }

            var hitReceiver = playerCombatController as ICombatHitReceiver ?? coreHitReceiver;
            var hitTarget = playerCombatController != null ? playerCombatController.transform : coreHitReceiver.transform;

            var attackData = _enemyData.AttackData;
            var damageAmount = attackData != null ? attackData.ContactDamage : 1f;

            if (_eliteModifierData != null)
            {
                damageAmount *= _eliteModifierData.DamageMultiplier;
            }

            var damageContext = new DamageContext(
                DamageSourceType.Enemy,
                DamageKind.Direct,
                damageAmount,
                transform.position,
                ((Vector2)hitTarget.position - (Vector2)transform.position).normalized,
                attackData != null ? attackData.ResolveStatusEffect() : null);

            hitReceiver.ReceiveHit(gameObject, damageContext);
            _contactCooldownTimer = attackData != null ? attackData.ContactIntervalSeconds : 0.6f;
        }

        private bool ShouldUseRangedAttack(Vector2 targetDirection, EnemyAttackData attackData)
        {
            return attackData != null
                && attackData.AttackMode == EnemyAttackMode.ProjectileBurst
                && attackData.ProjectileData != null
                && _projectilePrototype != null
                && targetDirection.magnitude <= attackData.AttackRange;
        }

        private bool UpdateRangedPressure(Vector2 targetDirection, EnemyAttackData attackData, float moveSpeed)
        {
            if (attackData == null || attackData.AttackMode != EnemyAttackMode.ProjectileBurst)
            {
                return false;
            }

            if (_telegraphTimer > 0f)
            {
                _telegraphTimer = Mathf.Max(0f, _telegraphTimer - Time.fixedDeltaTime);
                _rigidbody2D.linearVelocity = ResolveRangedPressureVelocity(targetDirection, attackData);
                var windupProgress = attackData.WindupSeconds > Mathf.Epsilon ? 1f - (_telegraphTimer / attackData.WindupSeconds) : 1f;
                ShowTelegraph(targetDirection, attackData, windupProgress);

                if (_telegraphTimer <= 0f)
                {
                    HideTelegraph();
                    FireProjectileBurst(targetDirection.normalized, attackData);
                    _attackCooldownTimer = attackData.FireIntervalSeconds;
                }

                return true;
            }

            if (ShouldUseRangedAttack(targetDirection, attackData) && _attackCooldownTimer <= 0f)
            {
                _telegraphTimer = attackData.WindupSeconds;

                if (_telegraphTimer <= Mathf.Epsilon)
                {
                    FireProjectileBurst(targetDirection.normalized, attackData);
                    _attackCooldownTimer = attackData.FireIntervalSeconds;
                }
                else
                {
                    ShowTelegraph(targetDirection, attackData, 0f);
                }

                _rigidbody2D.linearVelocity = ResolveRangedPressureVelocity(targetDirection, attackData) * 0.35f;
                return true;
            }

            HideTelegraph();
            _rigidbody2D.linearVelocity = ResolveRangedPressureVelocity(targetDirection, attackData, true, moveSpeed);
            return attackData.AttackMode == EnemyAttackMode.ProjectileBurst;
        }

        private Vector2 ResolveRangedPressureVelocity(Vector2 targetDirection, EnemyAttackData attackData)
        {
            var moveSpeed = _enemyData != null && _enemyData.BehaviourData != null ? _enemyData.BehaviourData.MoveSpeed : 2f;

            if (_eliteModifierData != null)
            {
                moveSpeed *= _eliteModifierData.MoveSpeedMultiplier;
            }

            if (_hitReceiver != null)
            {
                moveSpeed *= _hitReceiver.MovementSpeedMultiplier;
            }

            return ResolveRangedPressureVelocity(targetDirection, attackData, false, moveSpeed);
        }

        private Vector2 ResolveRangedPressureVelocity(Vector2 targetDirection, EnemyAttackData attackData, bool allowApproach, float moveSpeed)
        {
            if (attackData == null)
            {
                return Vector2.zero;
            }

            var distance = targetDirection.magnitude;
            var preferredDistance = _enemyData != null && _enemyData.BehaviourData != null
                ? _enemyData.BehaviourData.PreferredCombatDistance
                : attackData.AttackRange * 0.72f;
            var direction = distance > Mathf.Epsilon ? targetDirection.normalized : Vector2.right;

            if (distance < preferredDistance * 0.75f)
            {
                return -direction * moveSpeed * 0.85f;
            }

            if (allowApproach && distance > preferredDistance)
            {
                return direction * moveSpeed * 0.55f;
            }

            var strafeDirection = new Vector2(-direction.y, direction.x);
            var strafeMultiplier = _enemyData != null && _enemyData.BehaviourData != null ? _enemyData.BehaviourData.StrafeSpeedMultiplier : 0.6f;
            return strafeDirection * moveSpeed * strafeMultiplier;
        }

        private bool UpdateBruiserPressure(Vector2 targetDirection, float moveSpeed)
        {
            if (_enemyData == null || _enemyData.BehaviourData == null || _enemyData.BehaviourData.Role != EnemyRoleType.Bruiser)
            {
                return false;
            }

            if (_chargeWindupTimer > 0f)
            {
                var windupSeconds = _enemyData.BehaviourData.ChargeWindupSeconds;
                _chargeWindupTimer = Mathf.Max(0f, _chargeWindupTimer - Time.fixedDeltaTime);
                ApplyChargeTelegraphColor(windupSeconds > Mathf.Epsilon ? 1f - (_chargeWindupTimer / windupSeconds) : 1f);
                _rigidbody2D.linearVelocity = Vector2.zero;

                if (_chargeWindupTimer <= 0f)
                {
                    _chargeTimer = _enemyData.BehaviourData.ChargeDurationSeconds;

                    if (_spriteRenderer != null)
                    {
                        _spriteRenderer.color = ResolveAliveColor();
                    }
                }

                return true;
            }

            if (_chargeTimer > 0f)
            {
                _chargeTimer = Mathf.Max(0f, _chargeTimer - Time.fixedDeltaTime);
                _rigidbody2D.linearVelocity = _chargeDirection * moveSpeed * _enemyData.BehaviourData.ChargeSpeedMultiplier;
                return true;
            }

            if (_chargeCooldownTimer > 0f)
            {
                return false;
            }

            if (targetDirection.magnitude > _enemyData.BehaviourData.PreferredCombatDistance)
            {
                return false;
            }

            _chargeDirection = targetDirection.sqrMagnitude > Mathf.Epsilon ? targetDirection.normalized : Vector2.right;
            _chargeWindupTimer = _enemyData.BehaviourData.ChargeWindupSeconds;
            _chargeCooldownTimer = _enemyData.BehaviourData.ChargeCooldownSeconds;

            if (_chargeWindupTimer <= Mathf.Epsilon)
            {
                _chargeTimer = _enemyData.BehaviourData.ChargeDurationSeconds;
            }

            return true;
        }

        // Bruiser charges telegraph by brightening the body toward the warning color as the
        // windup completes, giving the player a fair, readable window to dodge before the burst
        // of speed lands - matching the visual-warning language the Ranged Pressure telegraph
        // already establishes, without reusing its projectile-specific marker sprite.
        private void ApplyChargeTelegraphColor(float progress)
        {
            if (_spriteRenderer == null)
            {
                return;
            }

            _spriteRenderer.color = Color.Lerp(ResolveAliveColor(), BruiserChargeWarningColor, Mathf.Clamp01(progress));
        }

        private void FireProjectileBurst(Vector2 direction, EnemyAttackData attackData)
        {
            if (_projectilePool == null || attackData == null)
            {
                return;
            }

            var projectileCount = attackData.ProjectileCount + (_eliteModifierData != null ? _eliteModifierData.ProjectileBonusCount : 0);
            var spreadAngle = attackData.SpreadAngleDegrees;
            var startAngle = -spreadAngle * 0.5f;
            var angleStep = projectileCount > 1 ? spreadAngle / (projectileCount - 1) : 0f;
            var damageAmount = attackData.ContactDamage * (_eliteModifierData != null ? _eliteModifierData.DamageMultiplier : 1f);

            for (var index = 0; index < projectileCount; index++)
            {
                var angleOffset = startAngle + angleStep * index;
                var shotDirection = CombatMath.Rotate(direction, angleOffset);
                var projectile = _projectilePool.Get(_projectilePrototype);
                var damageContext = new DamageContext(DamageSourceType.Enemy, DamageKind.Direct, damageAmount, transform.position, shotDirection, attackData.ResolveStatusEffect());
                projectile.Launch(gameObject, attackData.ProjectileData, transform.position, shotDirection, damageContext);
            }
        }

        private Transform ResolveTarget()
        {
            if (_enemyData == null || _enemyData.BehaviourData == null)
            {
                return _coreTarget;
            }

            // Chaser used to unconditionally return _playerTarget here, ignoring PlayerAggroRange
            // and _hasTakenDamage entirely - since Chaser is the most common early-wave enemy, this
            // alone made the Core functionally unattackable (Project Vision's entire premise is
            // defending the Core against enemies). All three roles now go through the same
            // aggro-range/damage-taken check, so an enemy spawning far from the player beelines the
            // Core and only breaks off to chase the player once it wanders close or gets shot.
            switch (_enemyData.BehaviourData.Role)
            {
                case EnemyRoleType.Chaser:
                case EnemyRoleType.RangedPressure:
                case EnemyRoleType.Bruiser:
                    return ResolvePlayerOrCore(_hasTakenDamage);
                default:
                    return _coreTarget;
            }
        }

        private Transform ResolvePlayerOrCore(bool preferPlayer)
        {
            if (_playerTarget == null)
            {
                return _coreTarget;
            }

            var aggroRange = _enemyData != null && _enemyData.BehaviourData != null ? _enemyData.BehaviourData.PlayerAggroRange : 4f;
            var playerDistance = Vector2.Distance(transform.position, _playerTarget.position);

            if (preferPlayer || playerDistance <= aggroRange)
            {
                return _playerTarget;
            }

            return _coreTarget;
        }

        private void OnDamageTaken(DamageEventArgs eventArgs)
        {
            if (eventArgs.Target == gameObject)
            {
                _hasTakenDamage = true;
            }
        }

        private void OnCombatantDied(DeathEventArgs eventArgs)
        {
            if (eventArgs.Victim != gameObject || _rigidbody2D == null)
            {
                return;
            }

            EnemyEvents.RaiseEnemyKilled(new EnemyLifecycleEventArgs(gameObject, _enemyData, _eliteModifierData));
            _rigidbody2D.linearVelocity = Vector2.zero;
            HideTelegraph();
            gameObject.SetActive(false);
        }

        private void EnsureComponents()
        {
            _hitReceiver = GetComponent<CombatHitReceiver>();

            if (_hitReceiver == null)
            {
                _hitReceiver = gameObject.AddComponent<CombatHitReceiver>();
            }

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

            _spriteRenderer = GetComponent<SpriteRenderer>();

            if (_spriteRenderer == null)
            {
                _spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            }

            var sprite = ResolveSprite();

            if (sprite != null)
            {
                _spriteRenderer.sprite = sprite;
                var targetSize = _enemyData != null && _enemyData.CombatTargetData != null ? _enemyData.CombatTargetData.Size * 1.6f : Vector2.one;
                Corebound.Gameplay.Shared.DebugSpriteFactory.FitTransformToSprite(transform, sprite, targetSize);
            }
            else if (_spriteRenderer.sprite == null)
            {
                _spriteRenderer.sprite = Corebound.Gameplay.Shared.DebugSpriteFactory.CreateSquare(32f);
            }

            EnsureEliteMarkerRenderer();
        }

        private Sprite ResolveSprite()
        {
            if (_enemyData != null && _enemyData.Sprite != null)
            {
                return _enemyData.Sprite;
            }

            if (_enemyData == null)
            {
                return null;
            }

            switch (_enemyData.EnemyId)
            {
                case "enemy_chaser":
                    return Corebound.Gameplay.Shared.DebugSpriteFactory.LoadProjectSprite(ChaserSpritePath);
                case "enemy_ranged":
                    return Corebound.Gameplay.Shared.DebugSpriteFactory.LoadProjectSprite(RangedSpritePath);
                case "enemy_bruiser":
                    return Corebound.Gameplay.Shared.DebugSpriteFactory.LoadProjectSprite(BruiserSpritePath);
                default:
                    return null;
            }
        }

        private Color ResolveAliveColor()
        {
            var hasSprite = ResolveSprite() != null;

            if (_eliteModifierData == null)
            {
                return hasSprite ? Color.white : (_enemyData != null ? _enemyData.DebugColor : Color.red);
            }

            var baseColor = hasSprite ? Color.white : (_enemyData != null ? _enemyData.DebugColor : Color.red);

            return new Color(
                Mathf.Clamp01(baseColor.r * _eliteModifierData.ColorTint.r),
                Mathf.Clamp01(baseColor.g * _eliteModifierData.ColorTint.g),
                Mathf.Clamp01(baseColor.b * _eliteModifierData.ColorTint.b),
                baseColor.a);
        }

        // progress01 is 0 at the start of the windup and 1 the instant the real projectile fires.
        // Previously the telegraph just sat parked at a fixed spot near the enemy for the whole
        // windup, which read as a static "danger zone" rather than a warning about where an
        // incoming shot is actually headed (2026-09-15 playtest: "I would like to see that fly
        // towards the player or core"). It now travels the real distance to the target over the
        // windup so its arrival at the target visually lines up with the shot actually firing.
        private void ShowTelegraph(Vector2 targetDirection, EnemyAttackData attackData, float progress01)
        {
            EnsureTelegraphRenderer();

            if (_telegraphRenderer == null)
            {
                return;
            }

            var totalDistance = targetDirection.magnitude;
            var direction = totalDistance > Mathf.Epsilon ? targetDirection / totalDistance : Vector2.right;
            var traveledDistance = Mathf.Lerp(0f, totalDistance, Mathf.Clamp01(progress01));
            _telegraphRenderer.transform.localPosition = (Vector3)(direction * traveledDistance);
            _telegraphRenderer.transform.up = direction;
            _telegraphRenderer.transform.localScale = Vector3.one * Mathf.Max(0.6f, attackData != null ? attackData.AttackRange * 0.16f : 0.8f);
            _telegraphRenderer.color = new Color(1f, 0.52f, 0.22f, 0.86f);
            _telegraphRenderer.enabled = true;
        }

        private void ApplyEliteMarker()
        {
            EnsureEliteMarkerRenderer();

            if (_eliteMarkerRenderer == null)
            {
                return;
            }

            if (_eliteModifierData == null)
            {
                _eliteMarkerRenderer.enabled = false;
                return;
            }

            var targetSize = _enemyData != null && _enemyData.CombatTargetData != null
                ? _enemyData.CombatTargetData.Size
                : Vector2.one;

            _eliteMarkerRenderer.transform.localPosition = Vector3.zero;

            if (_eliteMarkerRenderer.sprite != null)
            {
                DebugSpriteFactory.FitTransformToSprite(_eliteMarkerRenderer.transform, _eliteMarkerRenderer.sprite, targetSize * 2.2f);
            }
            else
            {
                _eliteMarkerRenderer.transform.localScale = Vector3.one * Mathf.Max(1f, Mathf.Max(targetSize.x, targetSize.y) * 1.8f);
            }

            _eliteMarkerRenderer.color = Color.white;
            _eliteMarkerRenderer.enabled = true;
        }

        private void HideTelegraph()
        {
            if (_telegraphRenderer != null)
            {
                _telegraphRenderer.enabled = false;
            }
        }

        private void EnsureTelegraphRenderer()
        {
            if (_telegraphRenderer != null)
            {
                return;
            }

            var telegraphTransform = transform.Find("RangedTelegraph");

            if (telegraphTransform == null)
            {
                var telegraphObject = new GameObject("RangedTelegraph");
                telegraphObject.transform.SetParent(transform, false);
                telegraphTransform = telegraphObject.transform;
            }

            _telegraphRenderer = telegraphTransform.GetComponent<SpriteRenderer>();

            if (_telegraphRenderer == null)
            {
                _telegraphRenderer = telegraphTransform.gameObject.AddComponent<SpriteRenderer>();
            }

            if (_telegraphRenderer.sprite == null)
            {
                _telegraphRenderer.sprite = DebugSpriteFactory.LoadProjectSprite(RangedTelegraphSpritePath);
            }

            if (_telegraphRenderer.sprite == null)
            {
                _telegraphRenderer.sprite = DebugSpriteFactory.CreateSquare(32f);
            }

            _telegraphRenderer.sortingOrder = (_spriteRenderer != null ? _spriteRenderer.sortingOrder : 0) - 1;
            _telegraphRenderer.enabled = false;
        }

        private void EnsureEliteMarkerRenderer()
        {
            if (_eliteMarkerRenderer != null)
            {
                return;
            }

            var markerTransform = transform.Find("EliteMarker");

            if (markerTransform == null)
            {
                var markerObject = new GameObject("EliteMarker");
                markerObject.transform.SetParent(transform, false);
                markerTransform = markerObject.transform;
            }

            _eliteMarkerRenderer = markerTransform.GetComponent<SpriteRenderer>();

            if (_eliteMarkerRenderer == null)
            {
                _eliteMarkerRenderer = markerTransform.gameObject.AddComponent<SpriteRenderer>();
            }

            if (_eliteMarkerRenderer.sprite == null)
            {
                _eliteMarkerRenderer.sprite = DebugSpriteFactory.LoadProjectSprite(EliteMarkerSpritePath);
            }

            if (_eliteMarkerRenderer.sprite == null)
            {
                _eliteMarkerRenderer.sprite = DebugSpriteFactory.CreateSquare(32f);
                _eliteMarkerRenderer.color = _eliteModifierData != null ? _eliteModifierData.ColorTint : Color.white;
            }

            _eliteMarkerRenderer.sortingOrder = (_spriteRenderer != null ? _spriteRenderer.sortingOrder : 0) - 1;
            _eliteMarkerRenderer.enabled = false;
        }

    }
}