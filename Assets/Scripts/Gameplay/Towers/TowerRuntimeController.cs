using Corebound.Gameplay.Cards;
using Corebound.Gameplay.Bosses;
using Corebound.Gameplay.Combat;
using Corebound.Gameplay.Enemies;
using Corebound.Gameplay.Shared;
using Corebound.UI;
using UnityEngine;

namespace Corebound.Gameplay.Towers
{
    public sealed class TowerRuntimeController : MonoBehaviour
    {
        private TowerData _towerData;
        private TowerService _towerService;
        private CombatProjectile _projectilePrototype;
        private Transform _projectileParent;
        private SpriteRenderer _spriteRenderer;
        private int _currentUpgradeLevel;

        private float _cooldownTimer;

        // Reused across FindTarget calls so each fire-cooldown tick doesn't allocate a fresh
        // Collider2D[] via Physics2D.OverlapCircleAll.
        private readonly Collider2D[] _targetHitBuffer = new Collider2D[32];

        public int CurrentUpgradeLevel => _currentUpgradeLevel;

        public TowerData Data => _towerData;

        // Stats at a given upgrade level with every current modifier applied (cards, skills,
        // upgrades). The pause menu's Towers tab and the upgrade overlay compare level N with N + 1.
        public float DamageAtLevel(int level)
        {
            return _towerData != null && _towerData.DamageProfile != null
                ? _towerData.DamageProfile.BaseDamage * _towerService.TowerDamageMultiplier * ResolveDamageMultiplier(level) * ResolveProgressionDamageMultiplier()
                : 0f;
        }

        public float FireIntervalAtLevel(int level)
        {
            return ResolveFireIntervalSeconds(level);
        }

        public float RangeAtLevel(int level)
        {
            return ResolveRange(level);
        }

        public float DpsAtLevel(int level)
        {
            var interval = FireIntervalAtLevel(level);
            return interval > Mathf.Epsilon ? DamageAtLevel(level) / interval : 0f;
        }

        public string DisplayName => _towerData != null ? _towerData.DisplayName : LocalizationService.Resolve("Tower", "Turm");

        public bool CanUpgrade => _towerData != null && _towerData.UpgradeLevels != null && _currentUpgradeLevel < _towerData.UpgradeLevels.Length;

        public int NextUpgradeCost => CanUpgrade ? _towerData.UpgradeLevels[_currentUpgradeLevel].UpgradeCost : 0;

        public string UpgradeLabel => CanUpgrade ? _towerData.UpgradeLevels[_currentUpgradeLevel].Label : LocalizationService.Resolve("Maxed", "Maximiert");

        public void Initialize(TowerData towerData, TowerService towerService, CombatProjectile projectilePrototype, Transform projectileParent, int upgradeLevel = 0)
        {
            _towerData = towerData;
            _towerService = towerService;
            _projectilePrototype = projectilePrototype;
            _projectileParent = projectileParent;
            _currentUpgradeLevel = 0;
            EnsureComponents();

            for (var index = 0; index < upgradeLevel; index++)
            {
                if (!CanUpgrade)
                {
                    break;
                }

                ApplyUpgrade();
            }
        }

        private void Update()
        {
            if (_towerData == null || _projectilePrototype == null || _towerService == null)
            {
                return;
            }

            _cooldownTimer = Mathf.Max(0f, _cooldownTimer - Time.deltaTime);

            if (_cooldownTimer > 0f)
            {
                return;
            }

            var target = FindTarget();

            if (target == null)
            {
                return;
            }

            FireAt(target);
            _cooldownTimer = ResolveFireIntervalSeconds();
        }

        private CombatHitReceiver FindTarget()
        {
            var hitCount = Physics2D.OverlapCircleNonAlloc(transform.position, ResolveRange(), _targetHitBuffer);
            var bestDistance = float.MaxValue;
            CombatHitReceiver bestTarget = null;

            for (var index = 0; index < hitCount; index++)
            {
                var hit = _targetHitBuffer[index];
                var hitReceiver = hit != null ? hit.attachedRigidbody != null ? hit.attachedRigidbody.GetComponent<CombatHitReceiver>() : null : null;
                var enemyTarget = hit != null && hit.attachedRigidbody != null ? hit.attachedRigidbody.GetComponent<EnemyRuntimeController>() : null;
                var bossTarget = hit != null && hit.attachedRigidbody != null ? hit.attachedRigidbody.GetComponent<BossRuntimeController>() : null;

                if (hitReceiver == null || !hitReceiver.IsAlive || (enemyTarget == null && bossTarget == null))
                {
                    continue;
                }

                var distance = Vector2.Distance(transform.position, hitReceiver.transform.position);

                if (distance >= bestDistance)
                {
                    continue;
                }

                bestDistance = distance;
                bestTarget = hitReceiver;
            }

            return bestTarget;
        }

        private void FireAt(CombatHitReceiver target)
        {
            Corebound.Gameplay.Combat.CombatVfxSpawner.SpawnTowerActivationPulse(transform.position);

            var projectile = _towerService.GetProjectileInstance(_projectilePrototype, _projectileParent);
            var direction = ((Vector2)target.transform.position - (Vector2)transform.position).normalized;
            FaceFireDirection(direction);
            var damageAmount = _towerData.DamageProfile != null
                ? _towerData.DamageProfile.BaseDamage * _towerService.TowerDamageMultiplier * ResolveDamageMultiplier(_currentUpgradeLevel) * ResolveProgressionDamageMultiplier()
                : 1f;
            var statusEffect = _towerData.DamageProfile != null && _towerData.DamageProfile.ApplyStatusEffect ? _towerData.DamageProfile.StatusEffectType : (StatusEffectType?)null;
            var damageContext = new DamageContext(
                DamageSourceType.Tower,
                _towerData.DamageProfile != null ? _towerData.DamageProfile.DamageKind : DamageKind.Direct,
                damageAmount,
                transform.position,
                direction,
                statusEffect);

            projectile.Launch(gameObject, _towerData.Projectile, transform.position, direction, damageContext);
        }

        // tower_ballista_topdown.png / tower_mortar_topdown.png both draw their barrel pointing
        // right by default (0 degrees in atan2 convention), confirmed by viewing the art directly -
        // so no rotation offset is needed here, unlike the enemy sprites elsewhere in this project
        // which needed a 180 degree correction for facing left by default.
        private void FaceFireDirection(Vector2 direction)
        {
            if (direction.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            var angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        public bool TryUpgrade()
        {
            if (!CanUpgrade)
            {
                return false;
            }

            ApplyUpgrade();
            return true;
        }

        private void ApplyUpgrade()
        {
            var levelData = _towerData != null && _towerData.UpgradeLevels != null && _currentUpgradeLevel < _towerData.UpgradeLevels.Length
                ? _towerData.UpgradeLevels[_currentUpgradeLevel]
                : null;

            if (levelData == null)
            {
                return;
            }

            _currentUpgradeLevel += 1;

            if (_spriteRenderer != null)
            {
                _spriteRenderer.color = Color.Lerp(_towerData != null ? _towerData.DebugColor : Color.white, levelData.Tint, 0.5f);
            }
        }

        // Permanent progression, frozen at run start (Upgrades.md tower tracks, per TowerId; skill
        // tree Construction branch, for every tower).
        private float ResolveProgressionDamageMultiplier()
        {
            var progression = Corebound.MetaProgression.RunProgressionSnapshot.Current;
            var towerId = _towerData != null ? _towerData.TowerId : string.Empty;
            return progression.Upgrades.GetIncreaseMultiplier(towerId, Corebound.MetaProgression.UpgradableStat.Damage)
                * progression.Skills[Corebound.MetaProgression.SkillEffectTarget.TowerDamageMultiplier];
        }

        private float ResolveRange()
        {
            return ResolveRange(_currentUpgradeLevel);
        }

        private float ResolveRange(int level)
        {
            var progression = Corebound.MetaProgression.RunProgressionSnapshot.Current;
            var towerId = _towerData != null ? _towerData.TowerId : string.Empty;

            // Overwatch: towers reach further while a boss is on the field.
            var bossRange = Corebound.Gameplay.Bosses.BossEncounterState.IsActive
                ? 1f + progression.Skills[Corebound.MetaProgression.SkillEffectTarget.BossTowerRangeMultiplier]
                : 1f;
            return ResolveBaseRange(level)
                * progression.Upgrades.GetIncreaseMultiplier(towerId, Corebound.MetaProgression.UpgradableStat.Range)
                * progression.Skills[Corebound.MetaProgression.SkillEffectTarget.TowerRangeMultiplier]
                * bossRange;
        }

        private float ResolveBaseRange(int level)
        {
            var range = _towerData != null ? _towerData.Range : 6f;

            if (_towerData == null || _towerData.UpgradeLevels == null)
            {
                return range;
            }

            for (var index = 0; index < level && index < _towerData.UpgradeLevels.Length; index++)
            {
                var levelData = _towerData.UpgradeLevels[index];

                if (levelData != null)
                {
                    range *= levelData.RangeMultiplier;
                }
            }

            return range;
        }

        private float ResolveFireIntervalSeconds()
        {
            return ResolveFireIntervalSeconds(_currentUpgradeLevel);
        }

        private float ResolveFireIntervalSeconds(int level)
        {
            var progression = Corebound.MetaProgression.RunProgressionSnapshot.Current;
            var towerId = _towerData != null ? _towerData.TowerId : string.Empty;
            return Mathf.Max(0.05f, ResolveBaseFireIntervalSeconds(level)
                * progression.Upgrades.GetReductionMultiplier(towerId, Corebound.MetaProgression.UpgradableStat.FireSpeed)
                * progression.Skills[Corebound.MetaProgression.SkillEffectTarget.TowerFireIntervalMultiplier]);
        }

        private float ResolveBaseFireIntervalSeconds(int level)
        {
            var fireInterval = _towerData != null ? _towerData.FireIntervalSeconds : 0.85f;

            if (_towerData == null || _towerData.UpgradeLevels == null)
            {
                return fireInterval;
            }

            for (var index = 0; index < level && index < _towerData.UpgradeLevels.Length; index++)
            {
                var levelData = _towerData.UpgradeLevels[index];

                if (levelData != null)
                {
                    fireInterval *= levelData.FireIntervalMultiplier;
                }
            }

            return fireInterval;
        }

        private float ResolveDamageMultiplier(int level)
        {
            var damageMultiplier = 1f;

            if (_towerData == null || _towerData.UpgradeLevels == null)
            {
                return damageMultiplier;
            }

            for (var index = 0; index < level && index < _towerData.UpgradeLevels.Length; index++)
            {
                var levelData = _towerData.UpgradeLevels[index];

                if (levelData != null)
                {
                    damageMultiplier *= levelData.DamageMultiplier;
                }
            }

            return damageMultiplier;
        }

        private void EnsureComponents()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();

            if (_spriteRenderer == null)
            {
                _spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            }

            var sprite = ResolveSprite();

            if (sprite != null)
            {
                _spriteRenderer.sprite = sprite;
                DebugSpriteFactory.FitTransformToSprite(transform, sprite, new Vector2(0.9f, 0.9f));
            }
            else if (_spriteRenderer.sprite == null)
            {
                _spriteRenderer.sprite = DebugSpriteFactory.CreateSquare(32f);
            }

            // Checking sprite (what actually got resolved, including the by-path fallback above)
            // rather than _towerData.Sprite (the raw, sometimes-stale asset reference) matters here:
            // tower_mortar's serialized _sprite reference is broken, so it only renders correctly via
            // ResolveSprite()'s fallback - checking _towerData.Sprite directly would still treat it as
            // spriteless and stomp the real artwork with the debug tint/scale below.
            _spriteRenderer.color = sprite != null ? Color.white : (_towerData != null ? _towerData.DebugColor : Color.white);

            if (sprite == null)
            {
                transform.localScale = new Vector3(0.75f, 0.75f, 1f);
            }
        }

        private Sprite ResolveSprite()
        {
            return ResolveSprite(_towerData);
        }

        // Static so the build browser shows exactly the art the built tower will use.
        public static Sprite ResolveSprite(TowerData towerData)
        {
            if (towerData != null && towerData.Sprite != null)
            {
                return towerData.Sprite;
            }

            if (towerData == null)
            {
                return null;
            }

            // Art by path, from the tower's own data (2026-09-22). A tower without a path keeps
            // the coloured-square fallback in EnsureComponents().
            if (string.IsNullOrWhiteSpace(towerData.SpritePath))
            {
                return null;
            }

            var rect = towerData.SpriteRect;

            return rect.width > 0f && rect.height > 0f
                ? DebugSpriteFactory.LoadProjectSpriteRect(towerData.SpritePath, rect)
                : DebugSpriteFactory.LoadProjectSprite(towerData.SpritePath);
        }
    }
}