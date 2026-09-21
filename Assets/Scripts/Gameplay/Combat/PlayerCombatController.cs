using System;
using Corebound.Input;
using Corebound.Gameplay.Cards;
using Corebound.Gameplay.Weapons;
using Corebound.SaveSystem;
using UnityEngine;

namespace Corebound.Gameplay.Combat
{
    public sealed class PlayerCombatController : MonoBehaviour, ICombatHitReceiver
    {
        [SerializeField] private PlayerCombatData _combatData;
        [SerializeField] private UnityEngine.Object _inputReaderSource;
        [SerializeField] private CombatProjectile _projectilePrefab;
        [SerializeField] private Transform _projectileParent;
        [SerializeField] private CardService _cardService;
        [SerializeField] private UnityEngine.Object _saveManagerSource;
        [SerializeField] private StatusEffectData[] _statusDefinitions;

        private readonly PlayerAimResolver _aimResolver = new PlayerAimResolver();
        private readonly WeaponPatternResolver _weaponPatternResolver = new WeaponPatternResolver();
        private readonly WeaponModifierState _weaponModifierState = new WeaponModifierState();

        private PlayerCombatRuntime _runtime;
        private ProjectilePool _projectilePool;
        private WeaponData _activeWeapon;
        private WeaponReadinessState _weaponReadinessState = WeaponReadinessState.Unavailable;
        private int _shotIndexInVolley;
        private bool _isCurrentVolleyCritical;

        public event Action WeaponFired;

        private IInputReader InputReader => _inputReaderSource as IInputReader;

        private ISaveManager SaveManager => SaveManagerResolver.Resolve(_saveManagerSource);

        public float CurrentHealthNormalized => _runtime != null && _runtime.MaxHealth > Mathf.Epsilon ? _runtime.CurrentHealth / _runtime.MaxHealth : 1f;

        public string ActiveWeaponId => _activeWeapon != null ? _activeWeapon.WeaponId : string.Empty;

        public string ActiveWeaponName => _activeWeapon != null ? _activeWeapon.DisplayName : string.Empty;

        public WeaponData ActiveWeapon => _activeWeapon;

        public WeaponModifierState WeaponModifierState => _weaponModifierState;

        public WeaponReadinessState WeaponReadinessState => _weaponReadinessState;

        // 0 when the weapon can fire this instant, 1 immediately after a shot.
        public float FireCooldownNormalized => _runtime != null ? _runtime.FireCooldownNormalized : 0f;

        public float MovementSpeedMultiplier => (_runtime != null ? _runtime.MovementSpeedMultiplier : 1f) * (_cardService != null ? _cardService.PlayerMovementSpeedMultiplier : 1f);

        private void Awake()
        {
            _runtime = new PlayerCombatRuntime(_combatData != null ? _combatData.MaxHealth : 8f);
            _projectilePool = new ProjectilePool(_projectileParent);
            RestoreRuntimeStateFromSave();
            RefreshActiveWeapon();
            RefreshWeaponModifierState();
            RefreshWeaponReadinessState();
        }

        private void Update()
        {
            if (_runtime == null || _combatData == null)
            {
                return;
            }

            _runtime.Tick(Time.deltaTime);
            UpdateAimFacing();
            RefreshWeaponModifierState();
            RefreshWeaponReadinessState();

            if (ApplyStatusTick(Time.deltaTime))
            {
                return;
            }

            if (InputReader == null || !InputReader.IsPrimaryFireHeld || !_runtime.CanFire())
            {
                return;
            }

            FireProjectile();
        }

        // PlayerController.freezeRotation=true blocks physics-driven rotation (torque/collision)
        // but not this direct transform write. Without this, the player's aim was logically correct
        // (FireProjectile already resolves and uses the right direction) but had zero visual
        // feedback - the character sprite never turned, so aiming looked completely unresponsive,
        // especially on gamepad where the right stick produced no visible change at all.
        private void UpdateAimFacing()
        {
            var inputReader = InputReader;

            if (inputReader == null || _activeWeapon == null)
            {
                return;
            }

            var origin = (Vector2)transform.position;
            var direction = _aimResolver.ResolveAimDirection(
                inputReader.Move,
                inputReader.Aim,
                origin,
                inputReader.IsUsingGamepad,
                _combatData.CombatBalance,
                _activeWeapon.AimAssistProfile);

            if (direction.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            // player_base_topdown.png is drawn facing down (toward the camera) at its default
            // rotation, not facing right like Atan2's 0-degree convention - a +90 degree offset
            // maps "facing down" to 0 rotation and "facing right" to 90, matching every other
            // direction around the circle.
            var angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + 90f;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        public void ReceiveHit(GameObject source, DamageContext damageContext)
        {
            if (_runtime == null || _combatData == null)
            {
                return;
            }

            if (DebugGodMode.ShouldIgnoreDamage(damageContext.SourceType))
            {
                return;
            }

            damageContext = ApplyIncomingDamageMultiplier(damageContext);

            if (!_runtime.TryReceiveHit(damageContext, _combatData.CombatBalance != null ? _combatData.CombatBalance.PlayerInvulnerabilitySeconds : 0.35f, out var remainingHealth))
            {
                return;
            }

            CombatEvents.RaiseDamageTaken(new DamageEventArgs(source, gameObject, damageContext, remainingHealth));

            if (damageContext.StatusEffect.HasValue)
            {
                var statusData = StatusEffectData.Find(_statusDefinitions, damageContext.StatusEffect.Value);

                if (statusData != null)
                {
                    _runtime.ApplyStatus(damageContext.StatusEffect.Value, statusData);
                    CombatEvents.RaiseStatusApplied(new StatusAppliedEventArgs(gameObject, damageContext.StatusEffect.Value, statusData.DurationSeconds));
                }
            }

            if (remainingHealth > 0f)
            {
                return;
            }

            CombatEvents.RaiseCombatantDied(new DeathEventArgs(gameObject, damageContext));
        }

        private bool ApplyStatusTick(float deltaTime)
        {
            var tickDamage = _runtime.AdvanceStatuses(deltaTime);

            if (tickDamage <= 0f)
            {
                return false;
            }

            var remainingHealth = _runtime.ApplyStatusTickDamage(tickDamage);
            var damageContext = new DamageContext(
                DamageSourceType.Enemy,
                DamageKind.Burn,
                tickDamage,
                transform.position,
                Vector2.zero,
                null);

            CombatEvents.RaiseDamageTaken(new DamageEventArgs(gameObject, gameObject, damageContext, remainingHealth));

            if (remainingHealth > 0f)
            {
                return true;
            }

            CombatEvents.RaiseCombatantDied(new DeathEventArgs(gameObject, damageContext));
            return true;
        }

        private void FireProjectile()
        {
            if (_projectilePrefab == null || _activeWeapon == null || _activeWeapon.Projectile == null || _activeWeapon.DamageProfile == null)
            {
                return;
            }

            var inputReader = InputReader;
            var moveInput = inputReader != null ? inputReader.Move : Vector2.zero;
            var aimInput = inputReader != null ? inputReader.Aim : Vector2.zero;
            var isUsingGamepad = inputReader != null && inputReader.IsUsingGamepad;
            var origin = (Vector2)transform.position;
            var direction = _aimResolver.ResolveAimDirection(moveInput, aimInput, origin, isUsingGamepad, _combatData.CombatBalance, _activeWeapon.AimAssistProfile);
            var attackPattern = _activeWeapon.AttackPattern;
            var spawnDistance = attackPattern != null ? attackPattern.ProjectileSpawnDistance : 0.5f;

            RefreshWeaponModifierState();

            // Rolled once per trigger pull, not per projectile: a Spread weapon that crit on two of
            // five pellets would read as a damage-number lottery rather than as a build paying off.
            _isCurrentVolleyCritical = _weaponModifierState.RollCritical();

            var damageMultiplier = _weaponModifierState.ResolveDamageMultiplier(_isCurrentVolleyCritical);
            var projectileSpeedMultiplier = _weaponModifierState.ProjectileSpeedMultiplier;
            _shotIndexInVolley = 0;

            _weaponPatternResolver.ForEachShotDirection(direction, attackPattern, _weaponModifierState.AdditionalProjectiles, shotDirection =>
            {
                var spawnPosition = origin + shotDirection * spawnDistance;
                var damageContext = CreateDamageContext(spawnPosition, shotDirection, damageMultiplier);
                var projectile = _projectilePool.Get(_projectilePrefab);
                projectile.Launch(gameObject, _activeWeapon.Projectile, spawnPosition, shotDirection, damageContext, projectileSpeedMultiplier);

                WeaponEvents.RaiseProjectileSpawned(new ProjectileSpawnedEventArgs(
                    gameObject,
                    _activeWeapon,
                    spawnPosition,
                    shotDirection,
                    damageContext.Amount,
                    _shotIndexInVolley,
                    _isCurrentVolleyCritical));

                _shotIndexInVolley++;
            });

            var fireInterval = attackPattern != null ? attackPattern.FireIntervalSeconds : 0.2f;
            _runtime.ConsumeFireCooldown(fireInterval * _weaponModifierState.FireIntervalMultiplier);
            WeaponFired?.Invoke();
            WeaponEvents.RaiseWeaponFired(new WeaponFiredEventArgs(
                gameObject,
                _activeWeapon,
                origin,
                direction,
                _shotIndexInVolley,
                _isCurrentVolleyCritical));
            RefreshWeaponReadinessState();
        }

        // Rebuilt every frame rather than on CardService.StateChanged: a pull model has no
        // subscribe-order problem when the Player spawns before CardService finishes loading its
        // save, and with a handful of modifiers this neither allocates nor measurably costs.
        private void RefreshWeaponModifierState()
        {
            var changed = _weaponModifierState.Rebuild(
                ActiveWeaponId,
                _cardService != null ? _cardService.WeaponDamageMultiplier : 1f,
                _cardService != null ? _cardService.WeaponFireIntervalMultiplier : 1f,
                _cardService != null ? _cardService.ProjectileSpeedMultiplier : 1f,
                _cardService != null ? _cardService.WeaponModifiers : null);

            if (changed)
            {
                WeaponEvents.RaiseWeaponModified(new WeaponModifiedEventArgs(_activeWeapon, _weaponModifierState));
            }
        }

        private void RefreshWeaponReadinessState()
        {
            var nextState = ResolveWeaponReadinessState();

            if (nextState == _weaponReadinessState)
            {
                return;
            }

            var previousState = _weaponReadinessState;
            _weaponReadinessState = nextState;
            WeaponEvents.RaiseWeaponStateChanged(new WeaponStateChangedEventArgs(_activeWeapon, previousState, nextState));
        }

        private WeaponReadinessState ResolveWeaponReadinessState()
        {
            if (_activeWeapon == null || _runtime == null || !_runtime.IsAlive)
            {
                return WeaponReadinessState.Unavailable;
            }

            return _runtime.CanFire() ? WeaponReadinessState.Ready : WeaponReadinessState.Cooling;
        }

        // Relics.md's risk/reward relics (e.g. Reckless Circuit) trade a permanent increase here
        // for stronger offense elsewhere - read live so a mid-run relic grant applies immediately.
        private DamageContext ApplyIncomingDamageMultiplier(DamageContext damageContext)
        {
            var incomingMultiplier = _cardService != null ? _cardService.IncomingDamageTakenMultiplier : 1f;

            if (Mathf.Approximately(incomingMultiplier, 1f))
            {
                return damageContext;
            }

            return new DamageContext(
                damageContext.SourceType,
                damageContext.DamageKind,
                damageContext.Amount * incomingMultiplier,
                damageContext.HitPoint,
                damageContext.HitDirection,
                damageContext.StatusEffect);
        }

        private DamageContext CreateDamageContext(Vector2 hitPoint, Vector2 direction, float damageMultiplier)
        {
            StatusEffectType? statusEffect = null;

            if (_activeWeapon != null && _activeWeapon.DamageProfile != null && _activeWeapon.DamageProfile.ApplyStatusEffect && _weaponModifierState.RollStatusEffect())
            {
                statusEffect = _activeWeapon.DamageProfile.StatusEffectType;
            }

            return new DamageContext(
                DamageSourceType.Player,
                _activeWeapon != null && _activeWeapon.DamageProfile != null ? _activeWeapon.DamageProfile.DamageKind : DamageKind.Direct,
                (_activeWeapon != null && _activeWeapon.DamageProfile != null ? _activeWeapon.DamageProfile.BaseDamage : 1f) * damageMultiplier,
                hitPoint,
                direction,
                statusEffect);
        }

        private void RefreshActiveWeapon()
        {
            _activeWeapon = ResolveWeaponFromRunSave();

            if (_activeWeapon != null)
            {
                return;
            }

            _activeWeapon = ResolveWeaponFromMetaProgressionSave();

            if (_activeWeapon == null && _combatData != null)
            {
                _activeWeapon = _combatData.DefaultWeapon;
            }
        }

        private void RestoreRuntimeStateFromSave()
        {
            if (_runtime == null || SaveManager == null)
            {
                return;
            }

            var runSaveData = SaveManager.LoadRunSaveData();

            if (runSaveData == null || !runSaveData.HasActiveRun)
            {
                return;
            }

            _runtime.RestoreHealthNormalized(runSaveData.PlayerHealthNormalized);
        }

        private WeaponData ResolveWeaponFromRunSave()
        {
            if (_combatData == null || _combatData.WeaponCatalog == null || SaveManager == null)
            {
                return null;
            }

            var runSaveData = SaveManager.LoadRunSaveData();

            if (runSaveData == null)
            {
                return null;
            }

            return _combatData.WeaponCatalog.FindById(runSaveData.EquippedWeaponId);
        }

        private WeaponData ResolveWeaponFromMetaProgressionSave()
        {
            if (_combatData == null || _combatData.MetaProgressionDefinitionData == null || SaveManager == null)
            {
                return null;
            }

            var metaSaveData = SaveManager.LoadMetaProgressionSaveData(_combatData.MetaProgressionDefinitionData.CreateDefaultSaveData());
            var unlocks = _combatData.MetaProgressionDefinitionData.Unlocks;

            if (metaSaveData == null || unlocks == null)
            {
                return null;
            }

            for (var index = unlocks.Length - 1; index >= 0; index--)
            {
                var unlockDefinition = unlocks[index];

                if (!metaSaveData.HasPurchased(unlockDefinition.UnlockId) || unlockDefinition.WeaponReward == null)
                {
                    continue;
                }

                return unlockDefinition.WeaponReward;
            }

            return null;
        }
    }
}