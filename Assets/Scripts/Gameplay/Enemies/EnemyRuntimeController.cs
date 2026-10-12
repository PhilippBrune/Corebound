using Corebound.Gameplay.Combat;
using Corebound.Gameplay.Shared;
using Corebound.Gameplay.Waves;
using UnityEngine;

namespace Corebound.Gameplay.Enemies
{
    public sealed class EnemyRuntimeController : MonoBehaviour
    {
        private const string ChaserSpritePath = "Assets/Art/Characters/Enemies/enemy_chaser_topdown.png";
        private const string RangedSpritePath = "Assets/Art/Characters/Enemies/enemy_ranged_pressure_topdown.png";
        private const string BruiserSpritePath = "Assets/Art/Characters/Enemies/enemy_bruiser_topdown.png";

        // Unused-assets review (2026-09-16/17): these per-stage variants existed on disk but were
        // never referenced by any code path - every stage rendered Stage 1's enemy art. Resolved
        // against the active stage's StageThemeData.ThemeId (same "WaveManager first, live lookup"
        // pattern already used by StageArenaArtPresenter.ResolveStageThemeData()), cached once in
        // Initialize() rather than re-resolved per call - ResolveSprite() is on the hot path via
        // ResolveAliveColor(), which runs every frame during a Bruiser's charge windup.
        private const string ChaserStage2SpritePath = "Assets/Art/Characters/Enemies/enemy_chaser_stage2_topdown.png";
        private const string ChaserStage3SpritePath = "Assets/Art/Characters/Enemies/enemy_chaser_stage3_topdown.png";
        private const string ChaserStage4SpritePath = "Assets/Art/Characters/Enemies/enemy_chaser_stage4_topdown.png";
        private const string ChaserStage5SpritePath = "Assets/Art/Characters/Enemies/enemy_chaser_stage5_topdown.png";
        private const string RangedStage2SpritePath = "Assets/Art/Characters/Enemies/enemy_ranged_pressure_stage2_topdown.png";
        private const string RangedStage3SpritePath = "Assets/Art/Characters/Enemies/enemy_ranged_pressure_stage3_topdown.png";
        private const string RangedStage4SpritePath = "Assets/Art/Characters/Enemies/enemy_ranged_pressure_stage4_topdown.png";
        private const string RangedStage5SpritePath = "Assets/Art/Characters/Enemies/enemy_ranged_pressure_stage5_topdown.png";
        private const string BruiserStage2SpritePath = "Assets/Art/Characters/Enemies/enemy_bruiser_stage2_topdown.png";
        private const string BruiserStage3SpritePath = "Assets/Art/Characters/Enemies/enemy_bruiser_stage3_topdown.png";
        private const string BruiserStage4SpritePath = "Assets/Art/Characters/Enemies/enemy_bruiser_stage4_topdown.png";
        private const string BruiserStage5SpritePath = "Assets/Art/Characters/Enemies/enemy_bruiser_stage5_topdown.png";

        // Assets 75-84: the Zone Denier and Relay Warden art was delivered on 2026-09-21 but never
        // referenced or mirrored, so both roles still rendered a tinted chaser/ranged sheet and
        // 10_OPEN_POINTS called them missing. Same per-stage pattern as the three older roles.
        private const string AreaControlSpritePath = "Assets/Art/Characters/Enemies/enemy_zone_denier_topdown.png";
        private const string AreaControlStage2SpritePath = "Assets/Art/Characters/Enemies/enemy_zone_denier_stage2_topdown.png";
        private const string AreaControlStage3SpritePath = "Assets/Art/Characters/Enemies/enemy_zone_denier_stage3_topdown.png";
        private const string AreaControlStage4SpritePath = "Assets/Art/Characters/Enemies/enemy_zone_denier_stage4_topdown.png";
        private const string AreaControlStage5SpritePath = "Assets/Art/Characters/Enemies/enemy_zone_denier_stage5_topdown.png";
        private const string SupportSpritePath = "Assets/Art/Characters/Enemies/enemy_relay_warden_topdown.png";
        private const string SupportStage2SpritePath = "Assets/Art/Characters/Enemies/enemy_relay_warden_stage2_topdown.png";
        private const string SupportStage3SpritePath = "Assets/Art/Characters/Enemies/enemy_relay_warden_stage3_topdown.png";
        private const string SupportStage4SpritePath = "Assets/Art/Characters/Enemies/enemy_relay_warden_stage4_topdown.png";
        private const string SupportStage5SpritePath = "Assets/Art/Characters/Enemies/enemy_relay_warden_stage5_topdown.png";
        // Playtest request (2026-09-16): use the actual bolt sprite as the telegraph instead of the
        // separate ranged_pressure_telegraph.png art, so the "ghost" that flies toward the target
        // during the windup (see ShowTelegraph()) looks identical to the real shot that follows it,
        // rather than a differently-styled warning icon.
        private const string RangedTelegraphSpritePath = "Assets/Art/Combat/Projectiles/enemy_bolt_projectile.png";
        private const string EliteMarkerSpritePath = "Assets/Art/Combat/VFX/elite_enemy_marker.png";

        private static readonly Color BruiserChargeWarningColor = new Color(1f, 0.92f, 0.35f, 1f);

        private EnemyData _enemyData;
        private EnemyEliteModifierData _eliteModifierData;

        // Elite kills pay Alloy (owner decision 2026-09-22), so WaveManager needs to know.
        public bool IsElite => _eliteModifierData != null;

        // What died, for the enemy-type and elite-affix quests.
        public EnemyData Data => _enemyData;

        public EnemyEliteModifierData EliteModifier => _eliteModifierData;
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
        private string _activeStageThemeId;

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

        // Read by the Core-protection layer (CORE_PROTECTION_FEEL_2026-10-07): which enemies are
        // going for the Core (threat lines, A1), since when, and how recently (intercepts, C9 - an
        // enemy the player shoots turns on the player, so "heading for the Core" has to mean
        // "was, a moment ago" by the time it dies).
        public bool IsHeadingForCore => IsAlive && _coreTarget != null && _currentTarget == _coreTarget;

        public Transform CoreTarget => _coreTarget;

        public float CoreApproachStartedAt { get; private set; } = -100f;

        public float LastHeadingForCoreAt { get; private set; } = -100f;

        public int ResourceRewardAmount => _enemyData != null && _enemyData.RewardData != null
            ? Mathf.RoundToInt(_enemyData.RewardData.ResourceRewardAmount * (_eliteModifierData != null ? _eliteModifierData.RewardMultiplier : 1f))
            : 0;

        public bool IsRangedPressureRole => _enemyData != null && _enemyData.BehaviourData != null && _enemyData.BehaviourData.Role == EnemyRoleType.RangedPressure;

        private EnemyRoleType Role => _enemyData != null && _enemyData.BehaviourData != null ? _enemyData.BehaviourData.Role : EnemyRoleType.Chaser;

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
            _activeStageThemeId = FindFirstObjectByType<WaveManager>() is WaveManager waveManager
                && waveManager.StageDataAsset != null
                && waveManager.StageDataAsset.ThemeData != null
                    ? waveManager.StageDataAsset.ThemeData.ThemeId
                    : null;
            _hasTakenDamage = false;
            _contactCooldownTimer = 0f;

            // Enemies come from a pool, so the stuck watchdog has to start clean or a reused body
            // inherits the previous life's progress sample and can trip immediately.
            _lastProgressPosition = transform.position;
            _stuckSampleTimer = 0f;
            _stuckSeconds = 0f;
            _stuckBurstTimer = 0f;
            _attackCooldownTimer = 0f;
            _telegraphTimer = 0f;
            _chargeTimer = 0f;
            _chargeCooldownTimer = 0f;
            _currentTarget = null;
            CoreApproachStartedAt = -100f;
            LastHeadingForCoreAt = -100f;
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

            RefreshSupportAuraRegistration();
            EnemyEvents.RaiseEnemySpawned(new EnemyLifecycleEventArgs(gameObject, _enemyData, _eliteModifierData));
        }

        private void OnEnable()
        {
            CombatEvents.DamageTaken += OnDamageTaken;
            CombatEvents.CombatantDied += OnCombatantDied;
            RefreshSupportAuraRegistration();
        }

        private void OnDisable()
        {
            CombatEvents.DamageTaken -= OnDamageTaken;
            CombatEvents.CombatantDied -= OnCombatantDied;

            // Enemies are pooled, so OnDisable is the return-to-pool moment. Unregistering here
            // rather than only on death is what stops a pooled Support enemy from buffing the next
            // wave from inside the pool.
            EnemySupportAuraRegistry.Unregister(transform);
        }

        // A Support enemy only emits while it is alive and enabled; Initialize is what tells it
        // which behaviour asset it is running this time, since the pool reuses instances across
        // enemy types.
        private void RefreshSupportAuraRegistration()
        {
            if (_enemyData == null || _enemyData.BehaviourData == null
                || _enemyData.BehaviourData.Role != EnemyRoleType.Support
                || _enemyData.BehaviourData.SupportAuraRadius <= 0f)
            {
                EnemySupportAuraRegistry.Unregister(transform);
                return;
            }

            EnemySupportAuraRegistry.Register(
                transform,
                _enemyData.BehaviourData.SupportAuraRadius,
                _enemyData.BehaviourData.SupportSpeedMultiplier,
                _enemyData.BehaviourData.SupportDamageMultiplier);
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

                if (target != null && target == _coreTarget)
                {
                    CoreApproachStartedAt = Time.time;
                }

                if (target != null)
                {
                    EnemyEvents.RaiseEnemyTargetChanged(new EnemyTargetChangedEventArgs(gameObject, _enemyData, target, target == _playerTarget));
                }
            }

            if (target != null && target == _coreTarget)
            {
                LastHeadingForCoreAt = Time.time;
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

            moveSpeed *= EnemySupportAuraRegistry.ResolveSpeedMultiplier(_rigidbody2D.position, transform);

            if (UpdateSupportBehaviour(direction, moveSpeed))
            {
                return;
            }

            if (UpdateAreaControl(direction, attackData, moveSpeed))
            {
                return;
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
            moveDirection = ApplyStuckRecovery(moveDirection, direction, moveSpeed);
            _rigidbody2D.linearVelocity = moveDirection * moveSpeed;
            FaceMovementDirection(moveDirection);
        }

        // Playtest 2026-09-28: "enemies can be stuck - create a way to prevent this."
        //
        // ApplyObstacleAvoidance steers around a single blocker, but it cannot resolve a wedge: two
        // props forming a V, a corner between a blocker and the boundary, or a knot of enemies all
        // pushing the same way. The enemy then sits with a non-zero desired velocity and no actual
        // movement, and because RemainingAliveEnemies never drops the whole wave waits on it - the
        // same shape as the 2026-09-20 bug where spawns landed outside the walls and hung the run
        // forever.
        //
        // Two escalating responses, because the cheap one is usually enough and the expensive one
        // must never be needed twice:
        //   1. After StuckSeconds of no real progress, push perpendicular to the target for a short
        //      burst, alternating side each time, which walks the enemy out of a V.
        //   2. After StuckTeleportSeconds it is genuinely trapped - nudge it bodily toward the
        //      target. Ugly, rare, and strictly better than a run that cannot end.
        private const float StuckSampleSeconds = 0.5f;
        private const float StuckProgressThreshold = 0.12f;
        private const float StuckSeconds = 1.5f;
        private const float StuckBurstSeconds = 0.55f;
        private const float StuckTeleportSeconds = 6f;

        private Vector2 _lastProgressPosition;
        private float _stuckSampleTimer;
        private float _stuckSeconds;
        private float _stuckBurstTimer;
        private float _stuckBurstSign = 1f;

        private Vector2 ApplyStuckRecovery(Vector2 desiredDirection, Vector2 toTarget, float moveSpeed)
        {
            if (moveSpeed <= 0.01f)
            {
                return desiredDirection;
            }

            _stuckSampleTimer += Time.fixedDeltaTime;

            if (_stuckSampleTimer >= StuckSampleSeconds)
            {
                // Measured against how far it *should* have travelled, so a slowed or aura-damped
                // enemy is not mistaken for a stuck one.
                var expected = moveSpeed * _stuckSampleTimer;
                var travelled = (_rigidbody2D.position - _lastProgressPosition).magnitude;
                _stuckSeconds = travelled < expected * StuckProgressThreshold
                    ? _stuckSeconds + _stuckSampleTimer
                    : 0f;

                _lastProgressPosition = _rigidbody2D.position;
                _stuckSampleTimer = 0f;

                if (_stuckSeconds >= StuckTeleportSeconds)
                {
                    _stuckSeconds = 0f;
                    _stuckBurstTimer = 0f;
                    var step = toTarget.sqrMagnitude > 0.0001f ? toTarget.normalized : Vector2.up;
                    _rigidbody2D.position = _rigidbody2D.position + step * 1.25f;
                    _lastProgressPosition = _rigidbody2D.position;
                    return desiredDirection;
                }

                if (_stuckSeconds >= StuckSeconds && _stuckBurstTimer <= 0f)
                {
                    _stuckBurstTimer = StuckBurstSeconds;
                    _stuckBurstSign = -_stuckBurstSign;
                }
            }

            if (_stuckBurstTimer <= 0f)
            {
                return desiredDirection;
            }

            _stuckBurstTimer -= Time.fixedDeltaTime;
            var straight = toTarget.sqrMagnitude > 0.0001f ? toTarget.normalized : desiredDirection;
            var sideways = new Vector2(-straight.y, straight.x) * _stuckBurstSign;
            return (sideways * 0.85f + straight * 0.35f).normalized;
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

            damageAmount *= EnemySupportAuraRegistry.ResolveDamageMultiplier(transform.position, transform);

            // HitPoint uses the target's position, not this enemy's own - see CombatProjectile's
            // identical fix (2026-09-17 playtest) for the ranged case; contact damage has the same
            // bug (hit-spark/core-flare rendering on the attacker instead of the victim).
            var damageContext = new DamageContext(
                DamageSourceType.Enemy,
                DamageKind.Direct,
                damageAmount,
                hitTarget.position,
                ((Vector2)hitTarget.position - (Vector2)transform.position).normalized,
                attackData != null ? attackData.ResolveStatusEffect() : null);

            hitReceiver.ReceiveHit(gameObject, damageContext);
            _contactCooldownTimer = attackData != null ? attackData.ContactIntervalSeconds : 0.6f;
        }

        // Flaechenkontrolle. Advances on its target until it reaches ZoneHoldDistance, then stops
        // dead and fires full rings of projectiles on its normal fire interval. It never closes and
        // never retreats, so the threat it creates is positional: the ground around it becomes
        // expensive to stand on, and the player has to decide whether to spend time removing it or
        // route around it. Deliberately built out of the existing ProjectileBurst attack data - the
        // only new thing is where it chooses to stand.
        private bool UpdateAreaControl(Vector2 targetDirection, EnemyAttackData attackData, float moveSpeed)
        {
            if (Role != EnemyRoleType.AreaControl)
            {
                return false;
            }

            var holdDistance = _enemyData.BehaviourData.ZoneHoldDistance;
            var distance = targetDirection.magnitude;

            if (distance > holdDistance)
            {
                HideTelegraph();
                var approachDirection = ApplyObstacleAvoidance(targetDirection.normalized);
                _rigidbody2D.linearVelocity = approachDirection * moveSpeed;
                FaceMovementDirection(approachDirection);
                return true;
            }

            _rigidbody2D.linearVelocity = Vector2.zero;

            if (attackData == null || attackData.AttackMode != EnemyAttackMode.ProjectileBurst || attackData.ProjectileData == null || _projectilePrototype == null)
            {
                return true;
            }

            if (_telegraphTimer > 0f)
            {
                _telegraphTimer = Mathf.Max(0f, _telegraphTimer - Time.fixedDeltaTime);
                var windupProgress = attackData.WindupSeconds > Mathf.Epsilon ? 1f - (_telegraphTimer / attackData.WindupSeconds) : 1f;
                ShowTelegraph(targetDirection, attackData, windupProgress);

                if (_telegraphTimer <= 0f)
                {
                    HideTelegraph();
                    FireProjectileBurst(targetDirection.sqrMagnitude > 0.0001f ? targetDirection.normalized : Vector2.right, attackData);
                    _attackCooldownTimer = attackData.FireIntervalSeconds;
                }

                return true;
            }

            if (_attackCooldownTimer > 0f)
            {
                HideTelegraph();
                return true;
            }

            _telegraphTimer = attackData.WindupSeconds;

            if (_telegraphTimer <= Mathf.Epsilon)
            {
                FireProjectileBurst(targetDirection.sqrMagnitude > 0.0001f ? targetDirection.normalized : Vector2.right, attackData);
                _attackCooldownTimer = attackData.FireIntervalSeconds;
            }
            else
            {
                ShowTelegraph(targetDirection, attackData, 0f);
            }

            return true;
        }

        // Support fuer andere Enemies. Never attacks, so it contributes nothing on its own and
        // everything through what it stands next to: it steers toward the enemy group it is
        // reinforcing and keeps its distance from the Player, which makes killing it a real
        // positioning problem rather than a free extra target.
        private bool UpdateSupportBehaviour(Vector2 targetDirection, float moveSpeed)
        {
            if (Role != EnemyRoleType.Support)
            {
                return false;
            }

            HideTelegraph();

            var anchor = ResolveSupportAnchor();
            var desiredDirection = anchor != null
                ? (Vector2)anchor.position - _rigidbody2D.position
                : targetDirection;

            // Backs away when the Player closes inside the aggro range. A Support that walks into
            // melee is a Support that dies for free, which would make the role pointless.
            if (_playerTarget != null)
            {
                var playerOffset = (Vector2)_playerTarget.position - _rigidbody2D.position;
                var retreatRange = _enemyData.BehaviourData.PlayerAggroRange;

                if (playerOffset.sqrMagnitude <= retreatRange * retreatRange && playerOffset.sqrMagnitude > 0.0001f)
                {
                    desiredDirection = -playerOffset;
                }
            }

            if (desiredDirection.sqrMagnitude <= 0.04f)
            {
                _rigidbody2D.linearVelocity = Vector2.zero;
                return true;
            }

            var moveDirection = ApplyObstacleAvoidance(desiredDirection.normalized);
            _rigidbody2D.linearVelocity = moveDirection * moveSpeed;
            FaceMovementDirection(moveDirection);
            return true;
        }

        // Prefers another Support if one exists (two of them stacking auras is the intended
        // escalation), otherwise falls back to the Core so it still advances with the wave instead
        // of idling at the spawn gate.
        private Transform ResolveSupportAnchor()
        {
            var nearestEmitter = EnemySupportAuraRegistry.FindNearestEmitter(_rigidbody2D.position, transform);
            return nearestEmitter != null ? nearestEmitter : _coreTarget;
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

            // A 360-degree spread has to divide by the projectile count, not count - 1: dividing by
            // count - 1 places the first and last projectile on the same bearing, so an AreaControl
            // ring visibly fires one doubled-up shot and leaves a gap opposite it.
            var isFullCircle = spreadAngle >= 359f;
            var angleStep = isFullCircle
                ? spreadAngle / projectileCount
                : (projectileCount > 1 ? spreadAngle / (projectileCount - 1) : 0f);

            var damageAmount = attackData.ContactDamage
                * (_eliteModifierData != null ? _eliteModifierData.DamageMultiplier : 1f)
                * EnemySupportAuraRegistry.ResolveDamageMultiplier(transform.position, transform);

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

                // AreaControl and Support both ignore the Player as a target entirely, including
                // after taking damage - that indifference is the point of both roles. Support does
                // still react to the Player by backing away, which UpdateSupportBehaviour handles.
                case EnemyRoleType.AreaControl:
                case EnemyRoleType.Support:
                    return _coreTarget;
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

            EnemySupportAuraRegistry.Unregister(transform);
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
            if (_enemyData == null)
            {
                return null;
            }

            // Resolved by (EnemyId, active stage theme) before ever trusting _enemyData.Sprite -
            // Enemy_Chaser/Ranged/Bruiser.asset's own _sprite reference always points at Stage 1's
            // art (correctly, just stage-agnostic), so returning it first the way this used to would
            // permanently shadow ResolveStageSpritePath() below and every stage would keep rendering
            // Stage 1's enemies. Same "resolve by known id first" precedent as
            // BossRuntimeController.ResolveBossIdSpritePath().
            var stageSpritePath = ResolveStageSpritePath(_enemyData.EnemyId, _activeStageThemeId);

            if (stageSpritePath != null)
            {
                return Corebound.Gameplay.Shared.DebugSpriteFactory.LoadProjectSprite(stageSpritePath);
            }

            return _enemyData.Sprite;
        }

        private static string ResolveStageSpritePath(string enemyId, string stageThemeId)
        {
            switch (enemyId)
            {
                case "enemy_chaser":
                    switch (stageThemeId)
                    {
                        case "theme_stage_02": return ChaserStage2SpritePath;
                        case "theme_stage_03": return ChaserStage3SpritePath;
                        case "theme_stage_04": return ChaserStage4SpritePath;
                        case "theme_stage_05": return ChaserStage5SpritePath;
                        default: return ChaserSpritePath;
                    }
                case "enemy_ranged":
                    switch (stageThemeId)
                    {
                        case "theme_stage_02": return RangedStage2SpritePath;
                        case "theme_stage_03": return RangedStage3SpritePath;
                        case "theme_stage_04": return RangedStage4SpritePath;
                        case "theme_stage_05": return RangedStage5SpritePath;
                        default: return RangedSpritePath;
                    }
                case "enemy_bruiser":
                    switch (stageThemeId)
                    {
                        case "theme_stage_02": return BruiserStage2SpritePath;
                        case "theme_stage_03": return BruiserStage3SpritePath;
                        case "theme_stage_04": return BruiserStage4SpritePath;
                        case "theme_stage_05": return BruiserStage5SpritePath;
                        default: return BruiserSpritePath;
                    }
                case "enemy_area_control":
                    switch (stageThemeId)
                    {
                        case "theme_stage_02": return AreaControlStage2SpritePath;
                        case "theme_stage_03": return AreaControlStage3SpritePath;
                        case "theme_stage_04": return AreaControlStage4SpritePath;
                        case "theme_stage_05": return AreaControlStage5SpritePath;
                        default: return AreaControlSpritePath;
                    }
                case "enemy_support":
                    switch (stageThemeId)
                    {
                        case "theme_stage_02": return SupportStage2SpritePath;
                        case "theme_stage_03": return SupportStage3SpritePath;
                        case "theme_stage_04": return SupportStage4SpritePath;
                        case "theme_stage_05": return SupportStage5SpritePath;
                        default: return SupportSpritePath;
                    }
                default:
                    return null;
            }
        }

        private Color ResolveAliveColor()
        {
            var hasSprite = ResolveSprite() != null;
            var spriteTint = _enemyData != null ? _enemyData.SpriteTint : Color.white;
            var baseColor = hasSprite ? spriteTint : (_enemyData != null ? _enemyData.DebugColor : Color.red);

            if (_eliteModifierData == null)
            {
                return baseColor;
            }

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
            // Playtest feedback (2026-09-16): using the real bolt sprite at near-full opacity made
            // the telegraph read as a second, redundant shot ("did it already fire at me?") rather
            // than a warning of the shot to come - the dedicated telegraph art this used to use is
            // gone, so distinguish it by tint/opacity instead: bright warning-yellow (matching
            // BruiserChargeWarningColor's established "this is a warning, not a resolved hit"
            // language) and noticeably translucent, growing more solid as the windup completes.
            var telegraphAlpha = Mathf.Lerp(0.35f, 0.7f, Mathf.Clamp01(progress01));
            _telegraphRenderer.color = new Color(1f, 0.92f, 0.35f, telegraphAlpha);
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
