using System;
using Corebound.Gameplay.Combat;
using Corebound.Gameplay.Enemies;
using Corebound.Gameplay.Shared;
using Corebound.Gameplay.Waves;
using UnityEngine;

namespace Corebound.Gameplay.Bosses
{
    public sealed class BossRuntimeController : MonoBehaviour
    {
        private const string BossStage1SpritePath = "Assets/Art/Characters/Bosses/boss_stage1_topdown.png";
        private const string BossStage2SpritePath = "Assets/Art/Characters/Bosses/boss_stage2_topdown.png";
        private const string BossStage3SpritePath = "Assets/Art/Characters/Bosses/boss_stage3_topdown.png";
        private const string BossStage4SpritePath = "Assets/Art/Characters/Bosses/boss_stage4_topdown.png";
        private const string BossStage5SpritePath = "Assets/Art/Characters/Bosses/boss_stage5_topdown.png";
        private static readonly Color BossAttackWarningColor = new Color(1f, 0.3f, 0.28f, 1f);

        private const float HelperAngleJitterDegrees = 14f;
        private const float HelperMinRadius = 1.4f;
        private const float HelperMaxRadius = 2.6f;

        private float _phaseSpawnAngleOffsetDegrees;
        private float _spiralOffsetDegrees;

        // Mirrors WaveManager.WaveStartSpawnDelaySeconds - gives the player the same beat to
        // reposition/prepare before the boss engages that a normal wave's spawn delay gives
        // before its first enemy appears. Applied unconditionally in Initialize() (fresh start
        // and save-restore alike), matching how StartCurrentWave() re-arms its own delay on
        // every call regardless of path.
        private const float BossEngageDelaySeconds = 2f;

        private BossData _bossData;
        private Transform _playerTarget;
        private Transform _coreTarget;
        private CombatProjectile _projectilePrototype;
        private Transform _projectileParent;
        private EnemyPool _enemyPool;

        private CombatHitReceiver _hitReceiver;
        private Rigidbody2D _rigidbody2D;
        private SpriteRenderer _spriteRenderer;

        // Shape-specific wind-up art (Assets 96-98, 2026-09-21). Before these existed the only tell
        // was the boss sprite tinting toward BossAttackWarningColor, which says "an attack is
        // coming" but not which one - and since 2026-09-21 each boss fires a different shape.
        // A sibling, not a child: the boss transform is scale-fitted to its sprite, so a child
        // would inherit that scale and every telegraph would be sized per boss by accident.
        private const string RingTelegraphPath = "Assets/Art/Combat/VFX/vfx_boss_telegraph_ring.png";
        private const string LanceTelegraphPath = "Assets/Art/Combat/VFX/vfx_boss_telegraph_lance.png";
        private const string VolleyTelegraphPath = "Assets/Art/Combat/VFX/vfx_boss_telegraph_volley.png";
        private const float RingTelegraphDiameter = 6.5f;
        private const float LanceTelegraphLength = 9f;
        private const float VolleyTelegraphDepth = 5.5f;
        private SpriteRenderer _shapeTelegraphRenderer;
        private Sprite _ringTelegraphSprite;
        private Sprite _lanceTelegraphSprite;
        private Sprite _volleyTelegraphSprite;
        private ProjectilePool _projectilePool;
        private bool[] _helperPhaseTriggered;
        private float _attackCooldownTimer;
        private float _attackWindupTimer;
        private float _targetSwapTimer;
        private int _currentPhaseIndex;
        private bool _preferPlayerTarget = true;
        private float _engageDelayTimer;

        public event Action<int> PhaseChanged;

        // Relayed by WaveManager, which is the only place that holds the stage/wave context
        // BossEventArgs needs - same shape as PhaseChanged above.
        public event Action<int> HeavyAttackStarted;

        public bool IsAlive => _hitReceiver != null && _hitReceiver.IsAlive;

        public int CurrentPhaseIndex => _currentPhaseIndex;

        public string BossName => _bossData != null ? _bossData.DisplayName : string.Empty;

        public string CurrentPhaseLabel => ResolvePhase() != null ? ResolvePhase().PhaseLabel : string.Empty;

        public float CurrentHealthNormalized => _hitReceiver != null ? _hitReceiver.CurrentHealthNormalized : 1f;

        public BossRewardData BossRewardData => _bossData != null ? _bossData.BossRewardData : null;

        public void Initialize(BossData bossData, Transform playerTarget, Transform coreTarget, CombatProjectile projectilePrototype, Transform projectileParent, EnemyPool enemyPool, int restoredPhaseIndex)
        {
            _bossData = bossData;
            _playerTarget = playerTarget;
            _coreTarget = coreTarget;
            _projectilePrototype = projectilePrototype;
            _projectileParent = projectileParent;
            _enemyPool = enemyPool;
            _attackCooldownTimer = 0f;
            _targetSwapTimer = 1.8f;
            _preferPlayerTarget = true;
            _engageDelayTimer = BossEngageDelaySeconds;

            EnsureComponents();

            if (_hitReceiver != null && _bossData != null)
            {
                _hitReceiver.InitializeRuntime(_bossData.CombatTargetData, _bossData.HitReactionData, _bossData.StatusDefinitions, 1f, true, _bossData.Sprite != null ? Color.white : _bossData.DebugColor);
            }

            _projectilePool = _projectileParent != null ? new ProjectilePool(_projectileParent) : null;
            _helperPhaseTriggered = _bossData != null && _bossData.Phases != null ? new bool[_bossData.Phases.Length] : Array.Empty<bool>();
            _currentPhaseIndex = Mathf.Clamp(restoredPhaseIndex, 0, Mathf.Max(0, (_bossData != null && _bossData.Phases != null ? _bossData.Phases.Length : 1) - 1));
            TriggerPhase(_currentPhaseIndex);
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
            HideShapeTelegraph();
        }

        private void OnDestroy()
        {
            if (_shapeTelegraphRenderer != null)
            {
                Destroy(_shapeTelegraphRenderer.gameObject);
            }
        }

        private void FixedUpdate()
        {
            if (_bossData == null || _rigidbody2D == null || !IsAlive)
            {
                return;
            }

            if (_engageDelayTimer > 0f)
            {
                _engageDelayTimer = Mathf.Max(0f, _engageDelayTimer - Time.fixedDeltaTime);
                _rigidbody2D.linearVelocity = Vector2.zero;
                return;
            }

            _attackCooldownTimer = Mathf.Max(0f, _attackCooldownTimer - Time.fixedDeltaTime);
            _targetSwapTimer = Mathf.Max(0f, _targetSwapTimer - Time.fixedDeltaTime);

            if (_targetSwapTimer <= 0f)
            {
                _preferPlayerTarget = !_preferPlayerTarget;
                _targetSwapTimer = 1.8f;
            }

            var phase = ResolvePhase();
            var attackPattern = phase != null ? phase.AttackPatternData : null;
            var target = ResolvePrimaryTarget(attackPattern);
            var secondaryTarget = ResolveSecondaryTarget(target);

            if (target == null)
            {
                _rigidbody2D.linearVelocity = Vector2.zero;
                return;
            }

            var direction = (Vector2)target.position - _rigidbody2D.position;
            var distance = direction.magnitude;
            var preferredDistance = attackPattern != null ? attackPattern.PreferredDistance : 3.5f;

            if (distance > preferredDistance * 0.55f)
            {
                _rigidbody2D.linearVelocity = direction.normalized * (_bossData.MoveSpeed * 1.15f);
            }
            else
            {
                var strafeDirection = new Vector2(-direction.y, direction.x).normalized;
                _rigidbody2D.linearVelocity = strafeDirection * (_bossData.MoveSpeed * 0.35f);
            }

            if (attackPattern != null && (distance <= attackPattern.AttackRange || _attackWindupTimer > 0f))
            {
                TryFireProjectiles(target, secondaryTarget, attackPattern);
            }
        }

        private void OnDamageTaken(DamageEventArgs eventArgs)
        {
            if (eventArgs.Target != gameObject || _bossData == null || _bossData.Phases == null)
            {
                return;
            }

            var nextPhaseIndex = _currentPhaseIndex + 1;

            if (nextPhaseIndex >= _bossData.Phases.Length)
            {
                return;
            }

            var nextPhase = _bossData.Phases[nextPhaseIndex];

            if (nextPhase != null && CurrentHealthNormalized <= nextPhase.TriggerHealthNormalized)
            {
                TriggerPhase(nextPhaseIndex);
            }
        }

        private void OnCombatantDied(DeathEventArgs eventArgs)
        {
            if (eventArgs.Victim != gameObject || _rigidbody2D == null)
            {
                return;
            }

            _rigidbody2D.linearVelocity = Vector2.zero;
            gameObject.SetActive(false);
        }

        private void TriggerPhase(int phaseIndex)
        {
            _currentPhaseIndex = Mathf.Clamp(phaseIndex, 0, Mathf.Max(0, (_bossData != null && _bossData.Phases != null ? _bossData.Phases.Length : 1) - 1));

            if (_helperPhaseTriggered.Length > _currentPhaseIndex && !_helperPhaseTriggered[_currentPhaseIndex])
            {
                SpawnPhaseHelpers();
                _helperPhaseTriggered[_currentPhaseIndex] = true;
            }

            PhaseChanged?.Invoke(_currentPhaseIndex);
        }

        private void SpawnPhaseHelpers()
        {
            _phaseSpawnAngleOffsetDegrees = UnityEngine.Random.Range(0f, 360f);

            if (_enemyPool == null)
            {
                return;
            }

            var phase = ResolvePhase();
            var helperGroups = phase != null ? phase.HelperSpawnGroups : null;

            if (helperGroups == null)
            {
                return;
            }

            for (var groupIndex = 0; groupIndex < helperGroups.Length; groupIndex++)
            {
                var helperGroup = helperGroups[groupIndex];

                if (helperGroup == null || helperGroup.EnemyData == null)
                {
                    continue;
                }

                for (var spawnIndex = 0; spawnIndex < helperGroup.Count; spawnIndex++)
                {
                    var enemy = _enemyPool.Get();
                    enemy.transform.position = transform.position + (Vector3)ResolveHelperOffset(groupIndex, spawnIndex);
                    enemy.Initialize(helperGroup.EnemyData, helperGroup.EliteModifierData, _playerTarget, _coreTarget, _projectilePrototype, _projectileParent);
                }
            }
        }

        private void TryFireProjectiles(Transform primaryTarget, Transform secondaryTarget, BossAttackPatternData attackPattern)
        {
            if (_projectilePrototype == null || _projectilePool == null || attackPattern == null || attackPattern.ProjectileData == null)
            {
                return;
            }

            // Windup re-aims at the current target position every tick (like the Ranged Pressure
            // enemy telegraph) rather than locking a direction up front, so the burst still reads
            // as reactive while giving the player a fair, visible window to dodge before it fires.
            if (_attackWindupTimer > 0f)
            {
                var windupSeconds = attackPattern.WindupSeconds;
                _attackWindupTimer = Mathf.Max(0f, _attackWindupTimer - Time.fixedDeltaTime);
                var windupProgress = windupSeconds > Mathf.Epsilon ? 1f - (_attackWindupTimer / windupSeconds) : 1f;
                ApplyAttackTelegraphColor(windupProgress);
                UpdateShapeTelegraph(attackPattern, primaryTarget, windupProgress);

                if (_attackWindupTimer > 0f)
                {
                    return;
                }

                FireLockedBurst(primaryTarget, secondaryTarget, attackPattern);
                ResetTelegraphColor();
                HideShapeTelegraph();
                return;
            }

            if (_attackCooldownTimer > 0f)
            {
                return;
            }

            if (attackPattern.WindupSeconds <= Mathf.Epsilon)
            {
                FireLockedBurst(primaryTarget, secondaryTarget, attackPattern);
                return;
            }

            _attackWindupTimer = attackPattern.WindupSeconds;
            ApplyAttackTelegraphColor(0f);
            HeavyAttackStarted?.Invoke(_currentPhaseIndex);
        }

        private void FireLockedBurst(Transform primaryTarget, Transform secondaryTarget, BossAttackPatternData attackPattern)
        {
            var didFire = false;

            if (primaryTarget != null)
            {
                var direction = ((Vector2)primaryTarget.position - _rigidbody2D.position).normalized;
                FireProjectileBurst(direction, attackPattern.ProjectileCount, attackPattern.SpreadAngleDegrees, attackPattern);
                didFire = true;
            }

            if (secondaryTarget != null)
            {
                var secondaryDirection = ((Vector2)secondaryTarget.position - _rigidbody2D.position);

                if (secondaryDirection.sqrMagnitude > Mathf.Epsilon)
                {
                    FireProjectileBurst(secondaryDirection.normalized, 1, 0f, attackPattern);
                    didFire = true;
                }
            }

            if (didFire)
            {
                _attackCooldownTimer = attackPattern.FireIntervalSeconds;
            }
        }

        private void ApplyAttackTelegraphColor(float progress)
        {
            if (_spriteRenderer == null)
            {
                return;
            }

            _spriteRenderer.color = Color.Lerp(ResolveIdleColor(), BossAttackWarningColor, Mathf.Clamp01(progress));
        }

        private void UpdateShapeTelegraph(BossAttackPatternData attackPattern, Transform primaryTarget, float progress)
        {
            var shape = attackPattern != null ? attackPattern.AttackShape : BossAttackShape.Fan;
            var sprite = ResolveShapeTelegraphSprite(shape);

            if (sprite == null)
            {
                HideShapeTelegraph();
                return;
            }

            var renderer = EnsureShapeTelegraphRenderer();
            renderer.sprite = sprite;
            renderer.gameObject.SetActive(true);

            var origin = (Vector2)transform.position;
            var direction = primaryTarget != null ? (Vector2)primaryTarget.position - origin : Vector2.down;
            direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.down;
            var angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            var spriteSize = sprite.bounds.size;
            var telegraphTransform = renderer.transform;

            // Fades in and, for the ring, grows over the wind-up, so the moment of firing is
            // readable from the art alone.
            var alpha = Mathf.Lerp(0.25f, 0.85f, Mathf.Clamp01(progress));
            renderer.color = new Color(1f, 1f, 1f, alpha);

            switch (shape)
            {
                case BossAttackShape.Lance:
                {
                    // The lance art runs corner to corner at 45 degrees, base bottom-left.
                    var diagonal = Mathf.Sqrt(spriteSize.x * spriteSize.x + spriteSize.y * spriteSize.y);
                    var scale = diagonal > Mathf.Epsilon ? LanceTelegraphLength / diagonal : 1f;
                    telegraphTransform.position = origin + direction * (LanceTelegraphLength * 0.5f);
                    telegraphTransform.rotation = Quaternion.Euler(0f, 0f, angle - 45f);
                    telegraphTransform.localScale = new Vector3(scale, scale, 1f);
                    break;
                }
                case BossAttackShape.Volley:
                {
                    // Lanes run along the art's x axis, which is the firing direction.
                    var scale = spriteSize.x > Mathf.Epsilon ? VolleyTelegraphDepth / spriteSize.x : 1f;
                    telegraphTransform.position = origin + direction * (VolleyTelegraphDepth * 0.5f);
                    telegraphTransform.rotation = Quaternion.Euler(0f, 0f, angle);
                    telegraphTransform.localScale = new Vector3(scale, scale, 1f);
                    break;
                }
                default:
                {
                    var diameter = RingTelegraphDiameter * Mathf.Lerp(0.55f, 1f, Mathf.Clamp01(progress));
                    var scale = spriteSize.x > Mathf.Epsilon ? diameter / spriteSize.x : 1f;
                    telegraphTransform.position = origin;
                    telegraphTransform.rotation = Quaternion.Euler(0f, 0f, shape == BossAttackShape.Spiral ? _spiralOffsetDegrees : 0f);
                    telegraphTransform.localScale = new Vector3(scale, scale, 1f);
                    break;
                }
            }
        }

        // Fan has no dedicated art: its cone is already legible from the boss facing the player,
        // and a wrong-shaped telegraph is worse than none.
        private Sprite ResolveShapeTelegraphSprite(BossAttackShape shape)
        {
            switch (shape)
            {
                case BossAttackShape.Ring:
                case BossAttackShape.Spiral:
                    return _ringTelegraphSprite != null ? _ringTelegraphSprite : (_ringTelegraphSprite = DebugSpriteFactory.LoadProjectSprite(RingTelegraphPath));
                case BossAttackShape.Lance:
                    return _lanceTelegraphSprite != null ? _lanceTelegraphSprite : (_lanceTelegraphSprite = DebugSpriteFactory.LoadProjectSprite(LanceTelegraphPath));
                case BossAttackShape.Volley:
                    return _volleyTelegraphSprite != null ? _volleyTelegraphSprite : (_volleyTelegraphSprite = DebugSpriteFactory.LoadProjectSprite(VolleyTelegraphPath));
                default:
                    return null;
            }
        }

        private SpriteRenderer EnsureShapeTelegraphRenderer()
        {
            if (_shapeTelegraphRenderer != null)
            {
                return _shapeTelegraphRenderer;
            }

            var telegraphObject = new GameObject("BossShapeTelegraph");
            telegraphObject.transform.SetParent(transform.parent, false);
            _shapeTelegraphRenderer = telegraphObject.AddComponent<SpriteRenderer>();

            // Under the boss, over the arena floor and props (-10..-6), and never under the
            // player's own projectiles (WeaponLoadout.md: telegraphs are never hidden by friendly fire).
            _shapeTelegraphRenderer.sortingOrder = (_spriteRenderer != null ? _spriteRenderer.sortingOrder : 0) - 1;
            return _shapeTelegraphRenderer;
        }

        private void HideShapeTelegraph()
        {
            if (_shapeTelegraphRenderer != null)
            {
                _shapeTelegraphRenderer.gameObject.SetActive(false);
            }
        }

        private void ResetTelegraphColor()
        {
            if (_spriteRenderer != null)
            {
                _spriteRenderer.color = ResolveIdleColor();
            }
        }

        private Color ResolveIdleColor()
        {
            return _bossData != null && _bossData.Sprite != null ? Color.white : (_bossData != null ? _bossData.DebugColor : Color.white);
        }

        private void FireProjectileBurst(Vector2 direction, int projectileCount, float spreadAngleDegrees, BossAttackPatternData attackPattern)
        {
            if (_projectilePrototype == null || _projectilePool == null || attackPattern == null || attackPattern.ProjectileData == null)
            {
                return;
            }

            projectileCount = Mathf.Max(1, projectileCount);

            // A full ring divides by count, not count - 1: dividing by count - 1 puts the first
            // and last shot on the same bearing, leaving a doubled shot and a gap opposite it.
            // Same correction the AreaControl enemy needed.
            var shape = attackPattern.AttackShape;
            var isRing = shape == BossAttackShape.Ring;
            var effectiveSpread = isRing ? 360f : spreadAngleDegrees;
            var startAngle = isRing ? 0f : -effectiveSpread * 0.5f;
            var angleStep = isRing
                ? effectiveSpread / projectileCount
                : (projectileCount > 1 ? effectiveSpread / (projectileCount - 1) : 0f);

            if (shape == BossAttackShape.Spiral)
            {
                // Accumulated across volleys, so consecutive bursts sweep rather than repeat.
                _spiralOffsetDegrees += attackPattern.SpiralStepDegrees;
                startAngle += _spiralOffsetDegrees;
            }

            // Lance puts every shot on one bearing; the spread collapses to zero and the shots
            // are separated in space along the firing line instead.
            if (shape == BossAttackShape.Lance)
            {
                startAngle = 0f;
                angleStep = 0f;
            }

            var perpendicular = new Vector2(-direction.y, direction.x);

            for (var index = 0; index < projectileCount; index++)
            {
                var shotDirection = CombatMath.Rotate(direction, startAngle + angleStep * index);
                var projectile = _projectilePool.Get(_projectilePrototype);
                var damageContext = new DamageContext(DamageSourceType.Enemy, DamageKind.Direct, attackPattern.DamageAmount, transform.position, shotDirection, null);
                // Volley fans out in space rather than in angle: parallel shots abreast, which
                // reads as a wall to dodge around instead of a cone to step out of. Lance
                // staggers along the firing line so the shots arrive as a stream.
                var spawnPosition = (Vector2)transform.position;

                if (shape == BossAttackShape.Volley)
                {
                    var offset = (index - (projectileCount - 1) * 0.5f) * attackPattern.VolleySpacing;
                    spawnPosition += perpendicular * offset;
                }
                else if (shape == BossAttackShape.Lance)
                {
                    spawnPosition += direction * (index * attackPattern.VolleySpacing);
                }

                projectile.Launch(gameObject, attackPattern.ProjectileData, spawnPosition, shotDirection, damageContext);
            }
        }

        private Transform ResolvePrimaryTarget(BossAttackPatternData attackPattern)
        {
            if (_playerTarget == null)
            {
                return _coreTarget;
            }

            if (_coreTarget == null)
            {
                return _playerTarget;
            }

            var playerDistance = Vector2.Distance(transform.position, _playerTarget.position);
            var coreDistance = Vector2.Distance(transform.position, _coreTarget.position);

            if (Mathf.Abs(playerDistance - coreDistance) > 1.25f)
            {
                return playerDistance < coreDistance ? _playerTarget : _coreTarget;
            }

            if (attackPattern != null && attackPattern.TrackPlayer)
            {
                return _preferPlayerTarget ? _playerTarget : _coreTarget;
            }

            return _preferPlayerTarget ? _coreTarget : _playerTarget;
        }

        private Transform ResolveSecondaryTarget(Transform primaryTarget)
        {
            if (primaryTarget == null)
            {
                return null;
            }

            if (primaryTarget == _playerTarget)
            {
                return _coreTarget;
            }

            if (primaryTarget == _coreTarget)
            {
                return _playerTarget;
            }

            return _coreTarget != null ? _coreTarget : _playerTarget;
        }

        private BossPhaseData ResolvePhase()
        {
            if (_bossData == null || _bossData.Phases == null || _bossData.Phases.Length == 0)
            {
                return null;
            }

            return _bossData.Phases[Mathf.Clamp(_currentPhaseIndex, 0, _bossData.Phases.Length - 1)];
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

            // Several Boss_StageN.asset's serialized _sprite guids point at the wrong (often
            // boss_stage1_topdown.png's own) sprite - same "sprite reference silently broke" class
            // of bug as Mortar/Sentinel towers elsewhere in this project - so BossId is resolved
            // against its known art path first and loads that directly instead of trusting the
            // asset reference. Falls through to the asset's own _sprite for any future boss that
            // isn't in this table and does have a correctly wired reference.
            var sprite = ResolveBossIdSpritePath(_bossData != null ? _bossData.BossId : null) is string bossSpritePath
                ? Corebound.Gameplay.Shared.DebugSpriteFactory.LoadProjectSprite(bossSpritePath)
                : (_bossData != null ? _bossData.Sprite : null);

            if (sprite != null)
            {
                _spriteRenderer.sprite = sprite;
                var targetSize = _bossData != null && _bossData.CombatTargetData != null ? _bossData.CombatTargetData.Size * 1.8f : new Vector2(2.4f, 2.4f);
                Corebound.Gameplay.Shared.DebugSpriteFactory.FitTransformToSprite(transform, sprite, targetSize);
            }
            else if (_spriteRenderer.sprite == null)
            {
                _spriteRenderer.sprite = Corebound.Gameplay.Shared.DebugSpriteFactory.CreateSquare(32f);
            }
        }

        private static string ResolveBossIdSpritePath(string bossId)
        {
            switch (bossId)
            {
                case "boss_stage1":
                    return BossStage1SpritePath;
                case "boss_stage2":
                    return BossStage2SpritePath;
                case "boss_stage3":
                    return BossStage3SpritePath;
                case "boss_stage4":
                    return BossStage4SpritePath;
                case "boss_stage5":
                    return BossStage5SpritePath;
                default:
                    return null;
            }
        }

        // Phase helpers used to land on a fixed lattice around the boss - group 0 always at
        // 0 degrees, each extra helper exactly 30 degrees further round, always 1.6 units out -
        // so every phase change produced the same picture and the same safe gap. Same treatment
        // as the regular wave spawn fan: keep the deterministic arc as the skeleton so helpers
        // still arrive spread out rather than clumped, and jitter the angle and radius so the
        // arrangement is never twice the same.
        //
        // _phaseSpawnAngleOffsetDegrees is rolled once per phase, not per helper, so a single
        // phase's helpers stay evenly spaced relative to each other while the whole ring sits
        // somewhere new.
        private Vector2 ResolveHelperOffset(int groupIndex, int spawnIndex)
        {
            var jitterDegrees = UnityEngine.Random.Range(-HelperAngleJitterDegrees, HelperAngleJitterDegrees);
            var angle = (groupIndex * 90f + spawnIndex * 30f + _phaseSpawnAngleOffsetDegrees + jitterDegrees) * Mathf.Deg2Rad;
            var radius = UnityEngine.Random.Range(HelperMinRadius, HelperMaxRadius);
            return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
        }

    }
}