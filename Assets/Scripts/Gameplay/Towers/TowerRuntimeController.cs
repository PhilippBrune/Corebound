using Corebound.Gameplay.Cards;
using Corebound.Gameplay.Bosses;
using Corebound.Gameplay.Combat;
using Corebound.Gameplay.Enemies;
using Corebound.Gameplay.Shared;
using UnityEngine;

namespace Corebound.Gameplay.Towers
{
    public sealed class TowerRuntimeController : MonoBehaviour
    {
        private const string BallistaSpritePath = "Assets/Art/Towers/tower_ballista_topdown.png.png";
        private const string SupportSpritePath = "Assets/Art/Towers/tower_support_topdown.png";
        private const string MortarSpritePath = "Assets/Art/Towers/tower_mortar_topdown.png";
        private const string SentinelSpritePath = "Assets/Art/Towers/tower_sentinel_topdown.png";

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

        public string DisplayName => _towerData != null ? _towerData.DisplayName : "Tower";

        public bool CanUpgrade => _towerData != null && _towerData.UpgradeLevels != null && _currentUpgradeLevel < _towerData.UpgradeLevels.Length;

        public int NextUpgradeCost => CanUpgrade ? _towerData.UpgradeLevels[_currentUpgradeLevel].UpgradeCost : 0;

        public string UpgradeLabel => CanUpgrade ? _towerData.UpgradeLevels[_currentUpgradeLevel].Label : "Maxed";

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
            var damageAmount = _towerData.DamageProfile != null ? _towerData.DamageProfile.BaseDamage * _towerService.TowerDamageMultiplier * ResolveDamageMultiplier() : 1f;
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

        private float ResolveRange()
        {
            var range = _towerData != null ? _towerData.Range : 6f;

            if (_towerData == null || _towerData.UpgradeLevels == null)
            {
                return range;
            }

            for (var index = 0; index < _currentUpgradeLevel; index++)
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
            var fireInterval = _towerData != null ? _towerData.FireIntervalSeconds : 0.85f;

            if (_towerData == null || _towerData.UpgradeLevels == null)
            {
                return fireInterval;
            }

            for (var index = 0; index < _currentUpgradeLevel; index++)
            {
                var levelData = _towerData.UpgradeLevels[index];

                if (levelData != null)
                {
                    fireInterval *= levelData.FireIntervalMultiplier;
                }
            }

            return fireInterval;
        }

        private float ResolveDamageMultiplier()
        {
            var damageMultiplier = 1f;

            if (_towerData == null || _towerData.UpgradeLevels == null)
            {
                return damageMultiplier;
            }

            for (var index = 0; index < _currentUpgradeLevel; index++)
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
            if (_towerData != null && _towerData.Sprite != null)
            {
                return _towerData.Sprite;
            }

            if (_towerData == null)
            {
                return null;
            }

            switch (_towerData.TowerId)
            {
                case "tower_ballista":
                    return DebugSpriteFactory.LoadProjectSprite(BallistaSpritePath);
                case "tower_support":
                    return DebugSpriteFactory.LoadProjectSprite(SupportSpritePath);
                case "tower_mortar":
                    // Tower_Mortar.asset's serialized _sprite reference (fileID 21300000) is stale -
                    // the source texture is imported in Multiple sprite mode with a different
                    // generated internal fileID, so the asset reference resolves to null at runtime.
                    // Loading the art directly from disk sidesteps that broken reference, matching the
                    // fallback already used for tower_ballista/tower_support above. The explicit pixel
                    // rect matches the crop the Sprite Editor already defines for this texture (its
                    // 910x910 canvas has a lot of transparent padding around the turret art itself,
                    // which would otherwise make FitTransformToSprite scale the tower down to a speck).
                    return DebugSpriteFactory.LoadProjectSpriteRect(MortarSpritePath, new Rect(166f, 150f, 578f, 624f));
                case "tower_sentinel":
                    // Tower_Sentinel.asset has no _sprite assigned at all (was never wired since no
                    // art existed). Using the same by-path load as Mortar from the start, rather than
                    // wiring the Inspector reference directly, sidesteps the exact stale-fileID trap
                    // that broke Mortar's reference after a Sprite Editor re-slice - crop rect measured
                    // from this texture's own 911x911 canvas (a lot of transparent padding around the
                    // slim turret-on-a-pole art).
                    return DebugSpriteFactory.LoadProjectSpriteRect(SentinelSpritePath, new Rect(304f, 138f, 302f, 659f));
                default:
                    return null;
            }
        }
    }
}