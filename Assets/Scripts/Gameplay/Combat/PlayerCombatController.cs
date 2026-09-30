using System;
using System.Collections.Generic;
using Corebound.Input;
using Corebound.Gameplay.Cards;
using Corebound.Gameplay.Economy;
using Corebound.Gameplay.Waves;
using Corebound.Gameplay.Weapons;
using Corebound.MetaProgression;
using Corebound.SaveSystem;
using Corebound.UI;
using UnityEngine;

namespace Corebound.Gameplay.Combat
{
    public sealed class PlayerCombatController : MonoBehaviour, ICombatHitReceiver, IWeaponGrantHandler
    {
        [SerializeField] private PlayerCombatData _combatData;
        [SerializeField] private UnityEngine.Object _inputReaderSource;
        [SerializeField] private CombatProjectile _projectilePrefab;
        [SerializeField] private Transform _projectileParent;
        [SerializeField] private CardService _cardService;
        [SerializeField] private UnityEngine.Object _saveManagerSource;
        [SerializeField] private StatusEffectData[] _statusDefinitions;

        private const string SecondWindRuleKey = "skill_second_wind";
        private const float ExecuteHealthThreshold = 0.3f;

        private readonly PlayerAimResolver _aimResolver = new PlayerAimResolver();
        private readonly WeaponPatternResolver _weaponPatternResolver = new WeaponPatternResolver();
        private readonly WeaponLoadoutRuntime _loadout = new WeaponLoadoutRuntime();
        private readonly List<WeaponBonusEffect> _loadoutEffectBuffer = new List<WeaponBonusEffect>();
        private readonly List<string> _saveIdBuffer = new List<string>(WeaponLoadoutRuntime.Capacity);
        private readonly List<int> _saveTierBuffer = new List<int>(WeaponLoadoutRuntime.Capacity);
        private readonly List<int> _savePriceBuffer = new List<int>(WeaponLoadoutRuntime.Capacity);

        private PlayerCombatRuntime _runtime;
        private ProjectilePool _projectilePool;
        private RunProgressionSnapshot _progression = RunProgressionSnapshot.Empty;
        private WeaponReadinessState _weaponReadinessState = WeaponReadinessState.Unavailable;
        private EconomyService _economyService;
        private WaveManager _waveManager;
        private bool _isLoadingLoadout;
        private bool _openingVolleyPending;
        private float _fireHeldSeconds;
        private float _fireReleasedSeconds;
        private int _weaponFiredFrame = -1;

        public event Action WeaponFired;

        private IInputReader InputReader => _inputReaderSource as IInputReader;

        private ISaveManager SaveManager => SaveManagerResolver.Resolve(_saveManagerSource);

        public float CurrentHealthNormalized => _runtime != null && _runtime.MaxHealth > Mathf.Epsilon ? _runtime.CurrentHealth / _runtime.MaxHealth : 1f;

        public bool IsAlive => _runtime != null && _runtime.IsAlive;

        private PlayerSupportEffects _supportEffects;

        public WeaponLoadoutRuntime Loadout => _loadout;

        public bool HasPendingWeapon => _loadout != null && _loadout.HasPendingWeapon;

        // The first equipped copy stands in wherever something still asks for "the" weapon (the
        // tutorial, save checkpoints, the HUD's name label).
        public WeaponData ActiveWeapon => ResolvePrimarySlot()?.Weapon;

        public string ActiveWeaponId => ActiveWeapon != null ? ActiveWeapon.WeaponId : string.Empty;

        public string ActiveWeaponName => ActiveWeapon != null ? ActiveWeapon.DisplayName : string.Empty;

        public WeaponModifierState WeaponModifierState => ResolvePrimarySlot()?.Modifiers ?? _emptyModifierState;

        private readonly WeaponModifierState _emptyModifierState = new WeaponModifierState();

        public WeaponReadinessState WeaponReadinessState => _weaponReadinessState;

        public float FireCooldownNormalized => ResolvePrimarySlot()?.CooldownNormalized ?? 0f;

        // For the reticle's magazine arc: how full the primary weapon's magazine is, and how far
        // through a reload it is (0 when not reloading).
        public float PrimaryAmmoNormalized => ResolvePrimarySlot()?.AmmoNormalized ?? 1f;

        public float PrimaryReloadProgress
        {
            get
            {
                var slot = ResolvePrimarySlot();
                return slot != null && slot.IsReloading ? 1f - slot.ReloadNormalized : 0f;
            }
        }

        public bool IsPrimaryReloading => ResolvePrimarySlot()?.IsReloading ?? false;

        public int ProjectileBudget => (_combatData != null ? _combatData.ProjectileBudget : 60)
            + (_loadout.IsArsenalActive && _combatData != null ? _combatData.ArsenalProjectileBudgetBonus : 0);

        public int LiveProjectileCount => CombatProjectile.ActivePlayerProjectileCount;

        public float MovementSpeedMultiplier => (_runtime != null ? _runtime.MovementSpeedMultiplier : 1f)
            * (_cardService != null ? _cardService.PlayerMovementSpeedMultiplier : 1f)
            * _progression.Skills[SkillEffectTarget.PlayerMoveSpeedMultiplier];

        private void Awake()
        {
            _progression = RunProgressionSnapshot.Capture(SaveManager, _combatData != null ? _combatData.MetaProgressionDefinitionData : null);
            var maxHealth = (_combatData != null ? _combatData.MaxHealth : 8f) * Mathf.Max(0.1f, _progression.Skills[SkillEffectTarget.PlayerMaxHealthMultiplier]);
            _runtime = new PlayerCombatRuntime(maxHealth);
            _projectilePool = new ProjectilePool(_projectileParent);
            RestoreRuntimeStateFromSave();
            LoadLoadout();
            _loadout.Changed += OnLoadoutChanged;

            if (_cardService != null)
            {
                _cardService.WeaponGrantHandler = this;
            }

            RefreshSlotModifierStates();
            RefreshWeaponReadinessState();

            if (GetComponent<Corebound.UI.Gameplay.WeaponLoadoutPanelPresenter>() == null)
            {
                gameObject.AddComponent<Corebound.UI.Gameplay.WeaponLoadoutPanelPresenter>().Bind(this);
            }

            // Attached the same way the loadout panel is, so the reticle exists wherever a player
            // does without needing a scene edit per scene.
            if (GetComponent<Corebound.UI.Gameplay.AimReticlePresenter>() == null)
            {
                gameObject.AddComponent<Corebound.UI.Gameplay.AimReticlePresenter>();
            }

            _supportEffects = gameObject.GetOrAdd<PlayerSupportEffects>();
            _supportEffects.Bind(this, _cardService);
        }

        private void OnEnable()
        {
            WaveEvents.WaveStarted += OnWaveStarted;
        }

        private void OnDisable()
        {
            WaveEvents.WaveStarted -= OnWaveStarted;
        }

        private void OnDestroy()
        {
            _loadout.Changed -= OnLoadoutChanged;

            if (_cardService != null && ReferenceEquals(_cardService.WeaponGrantHandler, this))
            {
                _cardService.WeaponGrantHandler = null;
            }
        }

        private void Update()
        {
            if (_runtime == null || _combatData == null)
            {
                return;
            }

            var deltaTime = Time.deltaTime;
            _runtime.Tick(deltaTime);
            TickTimedFireIntervalBuff(deltaTime);
            UpdateAimFacing();
            RefreshSlotModifierStates();

            if (ApplyStatusTick(deltaTime))
            {
                RefreshWeaponReadinessState();
                return;
            }

            var isFireHeld = InputReader != null && InputReader.IsPrimaryFireHeld && _runtime.IsAlive;

            if (isFireHeld)
            {
                _fireHeldSeconds += deltaTime;
                _fireReleasedSeconds = 0f;
            }
            else
            {
                // Beam Perfected's ramp resets on release (WeaponLoadout.md).
                _fireHeldSeconds = 0f;
                _fireReleasedSeconds += deltaTime;
            }

            for (var index = 0; index < _loadout.UnlockedSlotCount; index++)
            {
                TickSlot(_loadout.GetSlot(index), isFireHeld, deltaTime);
            }

            RefreshWeaponReadinessState();
        }

        // Cooldown, reload and firing for one copy. There is no selected weapon and no switch
        // input: every copy fires whenever the trigger is held and it is ready (WeaponLoadout.md).
        private void TickSlot(WeaponSlotRuntime slot, bool isFireHeld, float deltaTime)
        {
            if (slot == null || slot.IsEmpty)
            {
                return;
            }

            var weapon = slot.Weapon;
            var pattern = weapon.AttackPattern;
            slot.MagazineSize = ResolveMagazineSize(slot);

            if (slot.Ammo > slot.MagazineSize)
            {
                slot.Ammo = slot.MagazineSize;
            }

            slot.Cooldown = Mathf.Max(0f, slot.Cooldown - deltaTime);

            if (slot.IsReloading)
            {
                slot.ReloadTimer = Mathf.Max(0f, slot.ReloadTimer - deltaTime);

                if (!slot.IsReloading)
                {
                    slot.Ammo = slot.MagazineSize;
                }

                return;
            }

            // Releasing the trigger tops a partial magazine up after the reload delay, so a player
            // who fights in bursts is rarely caught empty - the reload punishes holding forever,
            // not stopping to aim.
            if (!isFireHeld && slot.HasMagazine && slot.Ammo < slot.MagazineSize && _fireReleasedSeconds >= _combatData.IdleReloadDelaySeconds)
            {
                BeginReload(slot, weapon);
                return;
            }

            if (!isFireHeld || slot.Cooldown > 0f || (slot.HasMagazine && slot.Ammo <= 0) || !_runtime.IsAlive)
            {
                return;
            }

            if (FireVolley(slot))
            {
                if (slot.HasMagazine)
                {
                    slot.Ammo--;

                    if (slot.Ammo <= 0)
                    {
                        BeginReload(slot, weapon);
                    }
                }

                var interval = ResolveFireInterval(slot);
                slot.Cooldown = interval;
                slot.LastFireInterval = interval;
            }
        }

        private void BeginReload(WeaponSlotRuntime slot, WeaponData weapon)
        {
            var reload = ResolveReloadSeconds(slot);
            slot.ReloadDuration = reload;
            slot.ReloadTimer = reload;
        }

        private bool FireVolley(WeaponSlotRuntime slot)
        {
            var weapon = slot.Weapon;

            if (_projectilePrefab == null || weapon.Projectile == null || weapon.DamageProfile == null)
            {
                return false;
            }

            var budget = ProjectileBudget;

            // Projectile budget (WeaponLoadout.md readability rule 3): the shot that would exceed
            // the cap is skipped; nobody else's projectile is despawned to make room.
            if (CombatProjectile.ActivePlayerProjectileCount >= budget)
            {
                return false;
            }

            var inputReader = InputReader;
            var origin = (Vector2)transform.position;
            var direction = _aimResolver.ResolveAimDirection(
                inputReader != null ? inputReader.Move : Vector2.zero,
                inputReader != null ? inputReader.Aim : Vector2.zero,
                origin,
                inputReader != null && inputReader.IsUsingGamepad,
                _combatData.CombatBalance,
                weapon.AimAssistProfile);
            var pattern = weapon.AttackPattern;
            var spawnDistance = pattern != null ? pattern.ProjectileSpawnDistance : 0.5f;
            var modifiers = slot.Modifiers;
            var upgrades = _progression.Upgrades;
            var skills = _progression.Skills;

            slot.VolleyCount++;

            // Rolled once per volley, not per projectile, so a Spread crit reads as a build paying
            // off rather than a pellet lottery.
            var criticalChance = Mathf.Clamp01(modifiers.CriticalChance
                + skills[SkillEffectTarget.CriticalChance]
                + upgrades.Get(weapon.WeaponId, UpgradableStat.CriticalChance));
            var isCritical = UnityEngine.Random.value < criticalChance
                || (modifiers.CriticalEveryNthShot > 0 && slot.VolleyCount % modifiers.CriticalEveryNthShot == 0)
                || _openingVolleyPending;
            _openingVolleyPending = false;

            var criticalMultiplier = modifiers.CriticalDamageMultiplier
                * skills[SkillEffectTarget.CriticalDamageMultiplier]
                * upgrades.GetIncreaseMultiplier(weapon.WeaponId, UpgradableStat.CriticalDamage);

            // base -> tier -> permanent upgrade -> skill tree -> set bonuses and cards (the latter
            // two live together in the slot's WeaponModifierState) - Upgrades.md's stated order.
            var damageMultiplier = WeaponTierRules.DamageMultiplier(slot.Tier)
                * upgrades.GetIncreaseMultiplier(weapon.WeaponId, UpgradableStat.Damage)
                * skills[SkillEffectTarget.WeaponDamageMultiplier]
                * modifiers.DamageMultiplier
                * (1f + modifiers.HeldDamageRampPerSecond * _fireHeldSeconds)
                * (isCritical ? criticalMultiplier : 1f);
            var projectileSpeedMultiplier = modifiers.ProjectileSpeedMultiplier * skills[SkillEffectTarget.ProjectileSpeedMultiplier];
            var spreadMultiplier = modifiers.SpreadAngleMultiplier * upgrades.GetReductionMultiplier(weapon.WeaponId, UpgradableStat.Accuracy);
            var shotIndex = 0;

            if (modifiers.FullRing)
            {
                CombatVfxSpawner.SpawnNovaRing(transform.position);
            }

            _weaponPatternResolver.ForEachShotDirection(direction, pattern, modifiers.AdditionalProjectiles, spreadMultiplier, modifiers.FullRing, shotDirection =>
            {
                if (CombatProjectile.ActivePlayerProjectileCount >= budget)
                {
                    return;
                }

                var spawnPosition = origin + shotDirection * spawnDistance;
                var damageContext = CreateDamageContext(weapon, modifiers, spawnPosition, shotDirection, damageMultiplier, isCritical);
                var projectile = _projectilePool.Get(_projectilePrefab);
                projectile.Launch(gameObject, weapon.Projectile, spawnPosition, shotDirection, damageContext, projectileSpeedMultiplier, modifiers.AdditionalPierce, modifiers.ExplodeOnExpire, modifiers.ExplodeOnImpact);

                WeaponEvents.RaiseProjectileSpawned(new ProjectileSpawnedEventArgs(gameObject, weapon, spawnPosition, shotDirection, damageContext.Amount, shotIndex, isCritical));
                shotIndex++;
            });

            if (shotIndex == 0)
            {
                return false;
            }

            // The tutorial listens for "the player fired" - once per frame, not once per copy.
            if (_weaponFiredFrame != Time.frameCount)
            {
                _weaponFiredFrame = Time.frameCount;
                WeaponFired?.Invoke();
            }

            WeaponEvents.RaiseWeaponFired(new WeaponFiredEventArgs(gameObject, weapon, origin, direction, shotIndex, isCritical));
            return true;
        }

        // A short, run-scoped fire-rate buff granted by an arena objective (D1 option 3, owner
        // 2026-09-25). Deliberately not a card, a skill or a weapon modifier: all three are
        // permanent for the run and are saved, while this must be gone when it runs out and must
        // not survive a checkpoint. Kept here rather than in a static so it dies with the player
        // object and cannot leak across runs or a domain reload.
        private float _timedFireIntervalMultiplier = 1f;
        private float _timedFireIntervalRemaining;

        public float TimedFireIntervalRemainingSeconds => _timedFireIntervalRemaining;

        // A stronger buff replaces a weaker one and refreshes the clock; a weaker one only extends
        // the clock. Stacking multiplicatively would let two caches in quick succession trivialise
        // a wave.
        public void ApplyTimedFireIntervalBuff(float fireIntervalMultiplier, float durationSeconds)
        {
            if (durationSeconds <= 0f || fireIntervalMultiplier >= 1f)
            {
                return;
            }

            _timedFireIntervalMultiplier = Mathf.Min(_timedFireIntervalMultiplier, Mathf.Max(0.1f, fireIntervalMultiplier));
            _timedFireIntervalRemaining = Mathf.Max(_timedFireIntervalRemaining, durationSeconds);
        }

        private void TickTimedFireIntervalBuff(float deltaTime)
        {
            if (_timedFireIntervalRemaining <= 0f)
            {
                return;
            }

            _timedFireIntervalRemaining -= deltaTime;

            if (_timedFireIntervalRemaining <= 0f)
            {
                _timedFireIntervalRemaining = 0f;
                _timedFireIntervalMultiplier = 1f;
            }
        }

        private float ResolveFireInterval(WeaponSlotRuntime slot)
        {
            var weapon = slot.Weapon;
            var baseInterval = weapon.AttackPattern != null ? weapon.AttackPattern.FireIntervalSeconds : 0.2f;
            return Mathf.Max(0.03f, baseInterval
                * WeaponTierRules.FireIntervalMultiplier(slot.Tier)
                * _progression.Upgrades.GetReductionMultiplier(weapon.WeaponId, UpgradableStat.FireSpeed)
                * _progression.Skills[SkillEffectTarget.WeaponFireIntervalMultiplier]
                * _timedFireIntervalMultiplier
                * slot.Modifiers.FireIntervalMultiplier);
        }

        private int ResolveMagazineSize(WeaponSlotRuntime slot)
        {
            var weapon = slot.Weapon;
            var baseSize = weapon.AttackPattern != null ? weapon.AttackPattern.MagazineSize : 0;

            if (baseSize <= 0)
            {
                return 0;
            }

            return Mathf.Max(1, baseSize
                + Mathf.RoundToInt(_progression.Upgrades.Get(weapon.WeaponId, UpgradableStat.Magazine))
                + Mathf.RoundToInt(_progression.Skills[SkillEffectTarget.MagazineSize])
                + slot.Modifiers.MagazineBonus);
        }

        private float ResolveReloadSeconds(WeaponSlotRuntime slot)
        {
            var weapon = slot.Weapon;
            var baseSeconds = weapon.AttackPattern != null ? weapon.AttackPattern.ReloadSeconds : 1f;
            return Mathf.Max(0.1f, baseSeconds
                * _progression.Upgrades.GetReductionMultiplier(weapon.WeaponId, UpgradableStat.ReloadSpeed)
                * _progression.Skills[SkillEffectTarget.ReloadTimeMultiplier]
                * slot.Modifiers.ReloadTimeMultiplier);
        }

        // Rebuilt every frame rather than on CardService.StateChanged: a pull model has no
        // subscribe-order problem when the Player spawns before CardService finishes loading its
        // save, and with a handful of modifiers per copy this neither allocates nor measurably costs.
        private void RefreshSlotModifierStates()
        {
            for (var index = 0; index < WeaponLoadoutRuntime.Capacity; index++)
            {
                var slot = _loadout.GetSlot(index);

                if (slot.IsEmpty)
                {
                    continue;
                }

                _loadout.CollectLoadoutEffects(slot.Weapon.WeaponId, _loadoutEffectBuffer);

                if (slot.Weapon.InnateEffects != null)
                {
                    _loadoutEffectBuffer.AddRange(slot.Weapon.InnateEffects);
                }
                var changed = slot.Modifiers.Rebuild(
                    slot.Weapon.WeaponId,
                    _cardService != null ? _cardService.WeaponDamageMultiplier : 1f,
                    _cardService != null ? _cardService.WeaponFireIntervalMultiplier : 1f,
                    _cardService != null ? _cardService.ProjectileSpeedMultiplier : 1f,
                    _cardService != null ? _cardService.WeaponModifiers : null,
                    _loadoutEffectBuffer);

                if (changed)
                {
                    WeaponEvents.RaiseWeaponModified(new WeaponModifiedEventArgs(slot.Weapon, slot.Modifiers));
                }
            }
        }

        private DamageContext CreateDamageContext(WeaponData weapon, WeaponModifierState modifiers, Vector2 hitPoint, Vector2 direction, float damageMultiplier, bool isCritical)
        {
            StatusEffectType? statusEffect = null;
            var profile = weapon.DamageProfile;

            if (profile != null && profile.ApplyStatusEffect && modifiers.RollStatusEffect())
            {
                statusEffect = profile.StatusEffectType;
            }
            else if (modifiers.SlowOnHit)
            {
                // Beam Synchronised: "a short slow on hit" is the Freeze status.
                statusEffect = StatusEffectType.Freeze;
            }

            // NEW_CARD_MECHANICS: rolled per projectile, which is the closest thing to "per hit"
            // that is known when the shot leaves the barrel. The receiver applies them on impact.
            var hitEffects = CombatHitEffects.None;

            if (modifiers.PoisonChance > 0f && UnityEngine.Random.value < modifiers.PoisonChance)
            {
                hitEffects |= CombatHitEffects.Poison;
            }

            if (modifiers.ShockChance > 0f && UnityEngine.Random.value < modifiers.ShockChance)
            {
                hitEffects |= CombatHitEffects.Shock;
            }

            if (modifiers.VulnerableOnHit)
            {
                hitEffects |= CombatHitEffects.Vulnerable;
            }

            return new DamageContext(
                DamageSourceType.Player,
                profile != null ? profile.DamageKind : DamageKind.Direct,
                (profile != null ? profile.BaseDamage : 1f) * damageMultiplier,
                hitPoint,
                direction,
                statusEffect,
                isCritical,
                weapon.WeaponId,
                hitEffects);
        }

        // Sentry Drone: one shot of the primary weapon's projectile at a share of its damage. No
        // crit, status or card on-hit roll - the drone is support fire, not a seventh weapon slot,
        // and it counts against the same projectile budget so it cannot push past the cap.
        public bool FireSupportProjectile(Vector2 origin, Vector2 direction, float damageFraction, string sourceId)
        {
            var slot = ResolvePrimarySlot();

            if (slot == null || _runtime == null || !_runtime.IsAlive || _projectilePrefab == null
                || CombatProjectile.ActivePlayerProjectileCount >= ProjectileBudget)
            {
                return false;
            }

            var weapon = slot.Weapon;
            var damage = (weapon.DamageProfile != null ? weapon.DamageProfile.BaseDamage : 1f)
                * WeaponTierRules.DamageMultiplier(slot.Tier)
                * _progression.Upgrades.GetIncreaseMultiplier(weapon.WeaponId, UpgradableStat.Damage)
                * _progression.Skills[SkillEffectTarget.WeaponDamageMultiplier]
                * slot.Modifiers.DamageMultiplier
                * Mathf.Max(0f, damageFraction);
            var context = new DamageContext(DamageSourceType.Player, DamageKind.Direct, damage, origin, direction, null, false, sourceId);
            var projectile = _projectilePool.Get(_projectilePrefab);
            projectile.Launch(gameObject, weapon.Projectile, origin, direction, context);
            return true;
        }

        // Leech Rounds: adds a flat amount of health without touching the invulnerability window.
        public bool TryHealAmount(float amount)
        {
            if (_runtime == null || amount <= 0f || !_runtime.IsAlive || CurrentHealthNormalized >= 0.999f)
            {
                return false;
            }

            return _runtime.Heal(amount) > 0f;
        }

        // Pause menu Player tab: every stat as base (+ what perks, skills, upgrades, tiers and set
        // bonuses add). Computed with the same resolvers the firing code uses, so it cannot drift
        // from what actually happens. One block per distinct weapon, from its first copy.
        public void CollectStatLines(List<StatLine> lines)
        {
            lines.Clear();

            if (_runtime == null || _combatData == null)
            {
                return;
            }

            lines.Add(StatLine.Header(LocalizationService.Resolve("Player", "Spieler")));
            lines.Add(StatLine.Value(LocalizationService.Resolve("Max health", "Max. Leben"), _combatData.MaxHealth, _runtime.MaxHealth, "0.#"));
            lines.Add(StatLine.Value(LocalizationService.Resolve("Move speed", "Tempo"), 100f, MovementSpeedMultiplier * 100f, "0", "%"));
            var incoming = (_cardService != null ? _cardService.IncomingDamageTakenMultiplier : 1f) * _progression.Skills[SkillEffectTarget.IncomingDamageMultiplier];
            lines.Add(StatLine.Value(LocalizationService.Resolve("Damage taken", "Erlittener Schaden"), 100f, incoming * 100f, "0", "%", true));
            lines.Add(StatLine.Plain(LocalizationService.Resolve("Projectiles in flight", "Projektile im Flug"), LiveProjectileCount + " / " + ProjectileBudget));

            var seen = new List<string>();

            for (var index = 0; index < WeaponLoadoutRuntime.Capacity; index++)
            {
                var slot = _loadout.GetSlot(index);

                if (slot.IsEmpty || seen.Contains(slot.Weapon.WeaponId))
                {
                    continue;
                }

                seen.Add(slot.Weapon.WeaponId);
                var weapon = slot.Weapon;
                var pattern = weapon.AttackPattern;
                var modifiers = slot.Modifiers;
                var upgrades = _progression.Upgrades;
                var skills = _progression.Skills;
                var baseDamage = weapon.DamageProfile != null ? weapon.DamageProfile.BaseDamage : 1f;
                var damage = baseDamage * WeaponTierRules.DamageMultiplier(slot.Tier)
                    * upgrades.GetIncreaseMultiplier(weapon.WeaponId, UpgradableStat.Damage)
                    * skills[SkillEffectTarget.WeaponDamageMultiplier]
                    * modifiers.DamageMultiplier;
                var baseInterval = pattern != null ? pattern.FireIntervalSeconds : 0.2f;
                var baseProjectiles = pattern != null ? pattern.ProjectileCount : 1;
                var critChance = Mathf.Clamp01(modifiers.CriticalChance + skills[SkillEffectTarget.CriticalChance] + upgrades.Get(weapon.WeaponId, UpgradableStat.CriticalChance));
                var critDamage = modifiers.CriticalDamageMultiplier * skills[SkillEffectTarget.CriticalDamageMultiplier] * upgrades.GetIncreaseMultiplier(weapon.WeaponId, UpgradableStat.CriticalDamage);

                lines.Add(StatLine.Header(weapon.DisplayName + "  x" + _loadout.CountCopies(weapon.WeaponId)));
                lines.Add(StatLine.Value(LocalizationService.Resolve("Damage per shot", "Schaden pro Schuss"), baseDamage, damage, "0.##"));
                lines.Add(StatLine.Value(LocalizationService.Resolve("Fire interval", "Feuerintervall"), baseInterval, ResolveFireInterval(slot), "0.00", "s", true));
                lines.Add(StatLine.Value(LocalizationService.Resolve("Projectiles", "Projektile"), baseProjectiles, modifiers.FullRing ? Mathf.Max(6, baseProjectiles + modifiers.AdditionalProjectiles) : baseProjectiles + modifiers.AdditionalProjectiles, "0"));
                lines.Add(StatLine.Value(LocalizationService.Resolve("Crit chance", "Kritische Chance"), 0f, critChance * 100f, "0.#", "%"));
                lines.Add(StatLine.Value(LocalizationService.Resolve("Crit damage", "Kritischer Schaden"), 2f, critDamage, "0.00", "x"));

                if (pattern != null && pattern.MagazineSize > 0)
                {
                    lines.Add(StatLine.Value(LocalizationService.Resolve("Magazine", "Magazin"), pattern.MagazineSize, ResolveMagazineSize(slot), "0"));
                    lines.Add(StatLine.Value(LocalizationService.Resolve("Reload", "Nachladen"), pattern.ReloadSeconds, ResolveReloadSeconds(slot), "0.00", "s", true));
                }
            }
        }

        // Skill tree Executioner (SkillTree.md): bonus damage to enemies below 30% health. Read
        // by CombatHitReceiver at the moment of the hit, where the target's health is known.
        public static float ResolveExecuteMultiplier(float targetHealthNormalized)
        {
            var bonus = RunProgressionSnapshot.Current.Skills[SkillEffectTarget.ExecuteDamageMultiplier];
            return bonus > 0f && targetHealthNormalized < ExecuteHealthThreshold ? 1f + bonus : 1f;
        }

        private void OnWaveStarted(WaveProgressEventArgs eventArgs)
        {
            // Opening Volley: the first volley of every wave is a guaranteed critical hit.
            _openingVolleyPending = _progression.Skills.HasRule(SkillEffectTarget.OpeningVolleyCritical);
        }

        // PlayerController.freezeRotation=true blocks physics-driven rotation (torque/collision)
        // but not this direct transform write. Without this, the player's aim was logically correct
        // but had zero visual feedback - the character sprite never turned, so aiming looked
        // completely unresponsive, especially on gamepad where the right stick produced no visible
        // change at all.
        private void UpdateAimFacing()
        {
            var inputReader = InputReader;
            var weapon = ActiveWeapon;

            if (inputReader == null || weapon == null)
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
                weapon.AimAssistProfile);

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

            // Reactive Plating: a ready shield takes the whole hit. Only a hit that would land -
            // not one inside the invulnerability window - may spend it.
            if (_runtime.IsAlive && !_runtime.IsInvulnerable && _supportEffects != null && _supportEffects.TryAbsorbHit())
            {
                _runtime.GrantInvulnerability(_combatData.CombatBalance != null ? _combatData.CombatBalance.PlayerInvulnerabilitySeconds : 0.35f);
                return;
            }

            if (!_runtime.TryReceiveHit(damageContext, _combatData.CombatBalance != null ? _combatData.CombatBalance.PlayerInvulnerabilitySeconds : 0.35f, out var remainingHealth))
            {
                return;
            }

            // Second Wind: once per stage, a lethal hit leaves the player standing instead.
            if (remainingHealth <= 0f && TryTriggerSecondWind())
            {
                remainingHealth = _runtime.CurrentHealth;
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

        private bool TryTriggerSecondWind()
        {
            var fraction = _progression.Skills[SkillEffectTarget.SecondWind];

            if (fraction <= 0f || !_progression.TryConsumeOncePerStage(SecondWindRuleKey))
            {
                return false;
            }

            _runtime.RestoreHealthNormalized(Mathf.Clamp01(fraction));
            NotificationPresenter.Show(LocalizationService.Resolve("Second Wind!", "Zweiter Atem!"));
            return true;
        }

        // Adds a fraction of maximum health rather than setting it, which is what a repair means -
        // RestoreHealthNormalized assigns outright and would *lower* a player who is healthier than
        // the fraction. Returns false when already full, so the caller can refuse to charge.
        public bool TryRestoreHealthFraction(float fraction)
        {
            if (_runtime == null || fraction <= 0f || !_runtime.IsAlive)
            {
                return false;
            }

            var current = CurrentHealthNormalized;

            if (current >= 0.999f)
            {
                return false;
            }

            _runtime.RestoreHealthNormalized(Mathf.Clamp01(current + fraction));
            return true;
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

            if (remainingHealth <= 0f && TryTriggerSecondWind())
            {
                remainingHealth = _runtime.CurrentHealth;
            }

            CombatEvents.RaiseDamageTaken(new DamageEventArgs(gameObject, gameObject, damageContext, remainingHealth));

            if (remainingHealth > 0f)
            {
                return true;
            }

            CombatEvents.RaiseCombatantDied(new DeathEventArgs(gameObject, damageContext));
            return true;
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
            WeaponEvents.RaiseWeaponStateChanged(new WeaponStateChangedEventArgs(ActiveWeapon, previousState, nextState));
        }

        private WeaponReadinessState ResolveWeaponReadinessState()
        {
            var primary = ResolvePrimarySlot();

            if (primary == null || _runtime == null || !_runtime.IsAlive)
            {
                return WeaponReadinessState.Unavailable;
            }

            return primary.Cooldown <= 0f && !primary.IsReloading ? WeaponReadinessState.Ready : WeaponReadinessState.Cooling;
        }

        private WeaponSlotRuntime ResolvePrimarySlot()
        {
            for (var index = 0; index < WeaponLoadoutRuntime.Capacity; index++)
            {
                var slot = _loadout.GetSlot(index);

                if (!slot.IsEmpty)
                {
                    return slot;
                }
            }

            return null;
        }

        // Relics.md's risk/reward relics (e.g. Reckless Circuit) trade a permanent increase here
        // for stronger offense elsewhere - read live so a mid-run relic grant applies immediately.
        private DamageContext ApplyIncomingDamageMultiplier(DamageContext damageContext)
        {
            var incomingMultiplier = (_cardService != null ? _cardService.IncomingDamageTakenMultiplier : 1f)
                * Mathf.Max(0.1f, _progression.Skills[SkillEffectTarget.IncomingDamageMultiplier]);

            if (Mathf.Approximately(incomingMultiplier, 1f))
            {
                return damageContext;
            }

            return damageContext.WithAmount(damageContext.Amount * incomingMultiplier);
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

        // ---- Loadout persistence ---------------------------------------------------------------

        private void LoadLoadout()
        {
            _isLoadingLoadout = true;
            var runSaveData = SaveManager != null ? SaveManager.LoadRunSaveData() : null;
            var catalog = _combatData != null ? _combatData.WeaponCatalog : null;
            var highestFilled = -1;

            if (runSaveData != null && catalog != null && runSaveData.EquippedWeaponIds != null)
            {
                for (var index = 0; index < runSaveData.EquippedWeaponIds.Count && index < WeaponLoadoutRuntime.Capacity; index++)
                {
                    var weapon = catalog.FindById(runSaveData.EquippedWeaponIds[index]);

                    if (weapon == null)
                    {
                        continue;
                    }

                    var tier = index < runSaveData.EquippedWeaponTiers.Count ? runSaveData.EquippedWeaponTiers[index] : 0;
                    var price = index < runSaveData.EquippedWeaponPrices.Count ? runSaveData.EquippedWeaponPrices[index] : 0;
                    _loadout.SetUnlockedSlotCount(WeaponLoadoutRuntime.Capacity);
                    _loadout.SetSlot(index, weapon, tier, price > 0 ? price : weapon.BasePurchaseCost);
                    highestFilled = index;
                }
            }

            // Never lock away a slot the save already filled (a slot unlock can only grow).
            _loadout.SetUnlockedSlotCount(Mathf.Max(_progression.WeaponSlotCount, highestFilled + 1));

            if (highestFilled < 0)
            {
                var fallback = ResolveWeaponFromMetaProgressionSave() ?? (_combatData != null ? _combatData.DefaultWeapon : null);

                if (fallback != null)
                {
                    _loadout.SetSlot(0, fallback, 0, fallback.BasePurchaseCost);
                }
            }

            _isLoadingLoadout = false;
        }

        private void OnLoadoutChanged()
        {
            if (_isLoadingLoadout || SaveManager == null)
            {
                return;
            }

            _saveIdBuffer.Clear();
            _saveTierBuffer.Clear();
            _savePriceBuffer.Clear();

            for (var index = 0; index < WeaponLoadoutRuntime.Capacity; index++)
            {
                var slot = _loadout.GetSlot(index);
                _saveIdBuffer.Add(slot.IsEmpty ? string.Empty : slot.Weapon.WeaponId);
                _saveTierBuffer.Add(slot.Tier);
                _savePriceBuffer.Add(slot.PurchasePrice);
            }

            var runSaveData = SaveManager.LoadRunSaveData() ?? new RunSaveData();
            runSaveData.SetEquippedWeapons(_saveIdBuffer, _saveTierBuffer, _savePriceBuffer);
            SaveManager.SaveRunSaveData(runSaveData);
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

            // B11: the player's own choice wins. Licences only widen the pool; before 2026-09-22
            // the most recently bought licence silently became the starting weapon.
            var catalog = _combatData.WeaponCatalog;
            var preferred = catalog != null ? catalog.FindById(metaSaveData.PreferredStartWeaponId) : null;

            if (preferred != null && StartWeaponOptions.IsAvailable(preferred, metaSaveData, _combatData.MetaProgressionDefinitionData))
            {
                return preferred;
            }

            return null;
        }

#if UNITY_EDITOR
        // Autopilot 'loadoutN': fill every slot so Set Bonuses and the budget can be tested
        // without earning the slot unlocks first. Editor-only; never reachable in a build.
        public void DebugConfigureLoadout(params string[] weaponIds)
        {
            if (_combatData == null || _combatData.WeaponCatalog == null || weaponIds == null)
            {
                return;
            }

            _loadout.SetUnlockedSlotCount(WeaponLoadoutRuntime.Capacity);

            for (var index = 0; index < WeaponLoadoutRuntime.Capacity; index++)
            {
                var weapon = index < weaponIds.Length ? _combatData.WeaponCatalog.FindById(weaponIds[index]) : null;
                _loadout.SetSlot(index, weapon, 0, weapon != null ? weapon.BasePurchaseCost : 0);
            }
        }
#endif

        // ---- Loadout management (WeaponLoadout.md: Intermission only) --------------------------

        public bool CanEditLoadout
        {
            get
            {
                if (_waveManager == null)
                {
                    _waveManager = FindFirstObjectByType<WaveManager>();
                }

                return _waveManager != null && _waveManager.State == WaveState.Intermission;
            }
        }

        private EconomyService Economy => _economyService != null ? _economyService : (_economyService = FindFirstObjectByType<EconomyService>());

        // IWeaponGrantHandler: weapon cards from the shop, the reward screen and the boss offer
        // all land here. A full loadout parks the weapon as pending instead of refusing it, so the
        // player chooses what to replace (or declines) in the loadout panel.
        public bool TryGrantWeapon(WeaponData weapon, int tier, int purchasePrice)
        {
            if (weapon == null || _loadout.HasPendingWeapon)
            {
                return false;
            }

            RecordWeaponDiscovered(weapon);

            var price = purchasePrice > 0 ? purchasePrice : weapon.BasePurchaseCost;

            if (_loadout.TryAdd(weapon, tier, price))
            {
                NotificationPresenter.Show(LocalizationService.Resolve("Equipped ", "Ausgerüstet: ") + weapon.DisplayName);
                return true;
            }

            _loadout.SetPending(weapon, tier, price, purchasePrice);
            NotificationPresenter.Show(LocalizationService.Resolve("Loadout full - choose a slot to replace", "Ausrüstung voll - wähle einen Platz zum Ersetzen"));
            return true;
        }

        // A weapon held once in a run may be chosen as a future run's starting weapon
        // (StartWeaponOptions). Licensed weapons are not discovered this way - their licence is
        // the gate - and a tutorial run discovers nothing, like every other meta reward.
        private void RecordWeaponDiscovered(WeaponData weapon)
        {
            var definition = _combatData != null ? _combatData.MetaProgressionDefinitionData : null;

            if (SaveManager == null || definition == null || weapon == null)
            {
                return;
            }

            var runSaveData = SaveManager.LoadRunSaveData();

            if (runSaveData != null && runSaveData.IsTutorialRun)
            {
                return;
            }

            var metaSaveData = SaveManager.LoadMetaProgressionSaveData(definition.CreateDefaultSaveData());

            if (metaSaveData == null || !metaSaveData.RecordWeaponDiscovered(weapon.WeaponId))
            {
                return;
            }

            SaveManager.SaveMetaProgressionSaveData(metaSaveData);
            Corebound.UI.NotificationPresenter.Show(
                Corebound.UI.LocalizationService.Resolve("New weapon discovered: ", "Neue Waffe entdeckt: ") + weapon.DisplayName
                + Corebound.UI.LocalizationService.Resolve(" - selectable at the start of future runs", " - in künftigen Runs als Startwaffe wählbar"));
        }

        public int ResolveSellValue(int slotIndex)
        {
            var slot = _loadout.GetSlot(slotIndex);
            return slot == null || slot.IsEmpty ? 0 : WeaponTierRules.SellValue(slot.PurchasePrice);
        }

        public int ResolveTierUpgradeCost(int slotIndex)
        {
            var slot = _loadout.GetSlot(slotIndex);
            return slot == null || slot.IsEmpty || slot.Tier >= WeaponTierRules.MaxTier || Economy == null
                ? 0
                : Economy.PeekCost(WeaponTierRules.UpgradeCost(slot.Weapon, slot.Tier));
        }

        public bool TrySellSlot(int slotIndex)
        {
            var slot = _loadout.GetSlot(slotIndex);

            // The last weapon cannot be sold: a run with nothing to fire is not a choice, it is a
            // dead run.
            if (!CanEditLoadout || slot == null || slot.IsEmpty || _loadout.FilledSlotCount <= 1 || Economy == null)
            {
                return false;
            }

            var value = WeaponTierRules.SellValue(slot.PurchasePrice);
            _loadout.SetSlot(slotIndex, null, 0, 0);
            Economy.Refund(value);
            return true;
        }

        public bool TryUpgradeSlot(int slotIndex)
        {
            var slot = _loadout.GetSlot(slotIndex);

            if (!CanEditLoadout || slot == null || slot.IsEmpty || slot.Tier >= WeaponTierRules.MaxTier || Economy == null)
            {
                return false;
            }

            var cost = Economy.ResolveCost(WeaponTierRules.UpgradeCost(slot.Weapon, slot.Tier));

            if (!Economy.TryPurchase(cost))
            {
                return false;
            }

            slot.SetTier(slot.Tier + 1);

            // The upgrade cost joins the copy's value, so selling a copy you invested in returns
            // half of everything it cost, not only its shelf price.
            _loadout.SetSlot(slotIndex, slot.Weapon, slot.Tier, slot.PurchasePrice + cost);
            return true;
        }

        public void SwapSlots(int first, int second)
        {
            if (CanEditLoadout)
            {
                _loadout.Swap(first, second);
            }
        }

        public bool TryPlacePendingWeapon(int slotIndex)
        {
            if (!_loadout.HasPendingWeapon || slotIndex < 0 || slotIndex >= _loadout.UnlockedSlotCount || Economy == null)
            {
                return false;
            }

            var replaced = _loadout.GetSlot(slotIndex);

            // Replacing sells the old copy for its usual value, so a swap is never a pure loss.
            if (!replaced.IsEmpty)
            {
                Economy.Refund(WeaponTierRules.SellValue(replaced.PurchasePrice));
            }

            _loadout.SetSlot(slotIndex, _loadout.PendingWeapon, _loadout.PendingTier, _loadout.PendingPrice);
            _loadout.ClearPending();
            return true;
        }

        // Declining is always allowed (WeaponLoadout.md rule). A declined purchase is refunded in
        // full, since nothing was received.
        public void DeclinePendingWeapon()
        {
            if (!_loadout.HasPendingWeapon)
            {
                return;
            }

            if (_loadout.PendingRefund > 0)
            {
                Economy?.Refund(_loadout.PendingRefund);
            }

            _loadout.ClearPending();
        }
    }
}
