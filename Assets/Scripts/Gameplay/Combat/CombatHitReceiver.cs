using System.Collections.Generic;
using Corebound.Gameplay.Cards;
using UnityEngine;

namespace Corebound.Gameplay.Combat
{
    public sealed class CombatHitReceiver : MonoBehaviour, ICombatHitReceiver
    {
        // Well-known name of the Core's hit-receiver GameObject, authored by WaveManager and
        // looked up by name from several unrelated systems (camera shake, HUD, save/persistence,
        // arena art). Centralized here so those lookups can't silently drift apart.
        public const string CoreAnchorObjectName = "CoreAnchor";

        // This receiver is shared by the Core and by every enemy, so god mode has to tell them
        // apart: vetoing damage for all of them would make enemies unkillable too.
        private bool IsCoreReceiver => gameObject.name == CoreAnchorObjectName;

        [SerializeField] private CombatTargetData _targetData;
        [SerializeField] private HitReactionData _hitReactionData;
        [SerializeField] private StatusEffectData[] _statusDefinitions;

        private readonly Dictionary<StatusEffectType, StatusEffectRuntime> _activeStatuses = new Dictionary<StatusEffectType, StatusEffectRuntime>();

        private HealthRuntime _healthRuntime;
        private SpriteRenderer _spriteRenderer;
        private BoxCollider2D _boxCollider2D;
        private float _flashTimer;
        private Color _baseColor;
        private float _runtimeMaxHealthMultiplier = 1f;
        private bool _hasAliveColorOverride;
        private Color _aliveColorOverride;
        private bool _rejectPlayerAndTowerDamage;

        // Optional and unset for enemy/boss receivers - only WaveManager wires this, onto the
        // Core's receiver, via SetIncomingDamageMultiplierSource(). Read live (not cached) so a
        // mid-run relic grant applies immediately, mirroring CardService.WeaponDamageMultiplier's
        // pull-model usage elsewhere.
        private CardService _incomingDamageMultiplierSource;

        // Set by CoreGuardController while the player actively channels the Core Guard mechanic -
        // separate from _incomingDamageMultiplierSource (a build-defining relic/card effect) since
        // this one flips on and off many times per run as a moment-to-moment player action rather
        // than a persistent run modifier. Defaults to 1 (no reduction) for every receiver that isn't
        // the Core.
        private float _guardDamageMultiplier = 1f;

        public bool IsAlive => _healthRuntime != null && _healthRuntime.IsAlive;

        public float CurrentHealthNormalized => _healthRuntime != null && _healthRuntime.MaxHealth > Mathf.Epsilon
            ? _healthRuntime.CurrentHealth / _healthRuntime.MaxHealth
            : 1f;

        public float MovementSpeedMultiplier
        {
            get
            {
                if (_activeStatuses.Count == 0)
                {
                    return 1f;
                }

                var multiplier = 1f;

                foreach (var pair in _activeStatuses)
                {
                    multiplier *= pair.Value.MoveSpeedMultiplier;
                }

                return multiplier;
            }
        }

        public void InitializeRuntime(
            CombatTargetData targetData,
            HitReactionData hitReactionData,
            StatusEffectData[] statusDefinitions,
            float maxHealthMultiplier = 1f,
            bool useAliveColorOverride = false,
            Color aliveColorOverride = default,
            bool rejectPlayerAndTowerDamage = false)
        {
            _targetData = targetData;
            _hitReactionData = hitReactionData;
            _statusDefinitions = statusDefinitions;
            _runtimeMaxHealthMultiplier = Mathf.Max(0.1f, maxHealthMultiplier);
            _hasAliveColorOverride = useAliveColorOverride;
            _aliveColorOverride = aliveColorOverride;
            _rejectPlayerAndTowerDamage = rejectPlayerAndTowerDamage;
            EnsureComponents();

            if (_boxCollider2D != null)
            {
                _boxCollider2D.enabled = true;
            }

            ResetState();
        }

        public void SetIncomingDamageMultiplierSource(CardService cardService)
        {
            _incomingDamageMultiplierSource = cardService;
        }

        public void SetGuardDamageMultiplier(float multiplier)
        {
            _guardDamageMultiplier = Mathf.Clamp01(multiplier);
        }

        // Restores a fraction of maximum health. Returns false when nothing could be restored -
        // a full Core, or a destroyed one - so a caller that charged for it can roll the payment
        // back rather than taking money for no effect.
        public bool TryRepairFraction(float fraction)
        {
            if (_healthRuntime == null || fraction <= 0f || !_healthRuntime.IsAlive)
            {
                return false;
            }

            var restored = _healthRuntime.Heal(_healthRuntime.MaxHealth * fraction);

            if (restored <= 0f)
            {
                return false;
            }

            return true;
        }

        public void RestoreHealthNormalized(float normalizedHealth)
        {
            EnsureComponents();

            if (_boxCollider2D != null)
            {
                _boxCollider2D.enabled = true;
            }

            ResetState();
            _healthRuntime.SetNormalized(normalizedHealth);

            if (_spriteRenderer != null)
            {
                _spriteRenderer.color = _healthRuntime.IsAlive ? _baseColor : (_targetData != null ? _targetData.DeadColor : Color.gray);
            }
        }

        private void Awake()
        {
            EnsureComponents();
            ResetState();
        }

        private void Update()
        {
            UpdateStatuses(Time.deltaTime);
            UpdateFlash(Time.deltaTime);
        }

        public void ReceiveHit(GameObject source, DamageContext damageContext)
        {
            if (_healthRuntime == null || !_healthRuntime.IsAlive)
            {
                return;
            }

            if (_rejectPlayerAndTowerDamage && (damageContext.SourceType == DamageSourceType.Player || damageContext.SourceType == DamageSourceType.Tower))
            {
                return;
            }

            // God mode vetoes incoming damage at the source rather than healing it back, so no
            // damage event, hit reaction or Core-critical warning fires during a timed run. This
            // receiver is used by the Core and by enemies alike; the check only covers non-Player
            // sources, so the player's own shots still kill things.
            if (IsCoreReceiver && DebugGodMode.ShouldIgnoreDamage(damageContext.SourceType))
            {
                return;
            }

            var incomingMultiplier = (_incomingDamageMultiplierSource != null ? _incomingDamageMultiplierSource.IncomingDamageTakenMultiplier : 1f) * _guardDamageMultiplier;

            if (!Mathf.Approximately(incomingMultiplier, 1f))
            {
                damageContext = new DamageContext(
                    damageContext.SourceType,
                    damageContext.DamageKind,
                    damageContext.Amount * incomingMultiplier,
                    damageContext.HitPoint,
                    damageContext.HitDirection,
                    damageContext.StatusEffect);
            }

            // Skill tree Executioner: the player's shots hit harder on a nearly dead enemy.
            if (!IsCoreReceiver && damageContext.SourceType == DamageSourceType.Player)
            {
                var executeMultiplier = PlayerCombatController.ResolveExecuteMultiplier(CurrentHealthNormalized);

                if (executeMultiplier > 1f)
                {
                    damageContext = new DamageContext(damageContext.SourceType, damageContext.DamageKind, damageContext.Amount * executeMultiplier, damageContext.HitPoint, damageContext.HitDirection, damageContext.StatusEffect);
                }
            }

            var remainingHealth = _healthRuntime.ApplyDamage(damageContext.Amount);

            // Skill tree Last Bastion: once per stage a lethal hit leaves the Core standing.
            if (remainingHealth <= 0f && IsCoreReceiver)
            {
                var fraction = Corebound.MetaProgression.RunProgressionSnapshot.Current.Skills[Corebound.MetaProgression.SkillEffectTarget.CoreContingency];

                if (fraction > 0f && Corebound.MetaProgression.RunProgressionSnapshot.Current.TryConsumeOncePerStage("skill_last_bastion"))
                {
                    _healthRuntime.Heal(_healthRuntime.MaxHealth * Mathf.Clamp01(fraction));
                    remainingHealth = _healthRuntime.CurrentHealth;
                    Corebound.UI.NotificationPresenter.Show(Corebound.UI.LocalizationService.Resolve("Last Bastion holds!", "Letzte Bastion hält!"));
                }
            }

            CombatEvents.RaiseDamageTaken(new DamageEventArgs(source, gameObject, damageContext, remainingHealth));

            ApplyHitFlash();

            if (damageContext.StatusEffect.HasValue)
            {
                TryApplyStatus(damageContext.StatusEffect.Value);
            }

            if (remainingHealth > 0f)
            {
                return;
            }

            HandleDeath(damageContext);
        }

        private void ResetState()
        {
            var maxHealth = _targetData != null ? _targetData.MaxHealth * _runtimeMaxHealthMultiplier : 10f;

            if (IsCoreReceiver)
            {
                maxHealth *= Mathf.Max(0.1f, Corebound.MetaProgression.RunProgressionSnapshot.Current.Skills[Corebound.MetaProgression.SkillEffectTarget.CoreMaxHealthMultiplier]);
            }
            _healthRuntime = new HealthRuntime(maxHealth);
            _activeStatuses.Clear();

            if (_spriteRenderer != null)
            {
                _baseColor = _hasAliveColorOverride ? _aliveColorOverride : (_targetData != null ? _targetData.AliveColor : Color.white);
                _spriteRenderer.color = _baseColor;
            }
        }

        private void HandleDeath(DamageContext damageContext)
        {
            if (_spriteRenderer != null)
            {
                _spriteRenderer.color = _targetData != null ? _targetData.DeadColor : Color.gray;
            }

            if (_boxCollider2D != null)
            {
                _boxCollider2D.enabled = false;
            }

            CombatEvents.RaiseCombatantDied(new DeathEventArgs(gameObject, damageContext));
        }

        private void TryApplyStatus(StatusEffectType effectType)
        {
            var statusData = StatusEffectData.Find(_statusDefinitions, effectType);

            if (statusData == null)
            {
                return;
            }

            _activeStatuses[effectType] = new StatusEffectRuntime(statusData);
            CombatEvents.RaiseStatusApplied(new StatusAppliedEventArgs(gameObject, effectType, statusData.DurationSeconds));
        }

        private void UpdateStatuses(float deltaTime)
        {
            if (_activeStatuses.Count == 0 || _healthRuntime == null || !_healthRuntime.IsAlive)
            {
                return;
            }

            List<StatusEffectType> expiredEffects = null;

            foreach (var pair in _activeStatuses)
            {
                var tickDamage = pair.Value.Advance(deltaTime);

                if (tickDamage > 0f)
                {
                    var context = new DamageContext(
                        DamageSourceType.Player,
                        pair.Key == StatusEffectType.Burn ? DamageKind.Burn : DamageKind.Freeze,
                        tickDamage,
                        transform.position,
                        Vector2.zero,
                        null);

                    var remainingHealth = _healthRuntime.ApplyDamage(tickDamage);
                    CombatEvents.RaiseDamageTaken(new DamageEventArgs(gameObject, gameObject, context, remainingHealth));

                    if (remainingHealth <= 0f)
                    {
                        HandleDeath(context);
                        break;
                    }
                }

                if (!pair.Value.IsExpired)
                {
                    continue;
                }

                if (expiredEffects == null)
                {
                    expiredEffects = new List<StatusEffectType>();
                }

                expiredEffects.Add(pair.Key);
            }

            if (expiredEffects == null)
            {
                return;
            }

            for (var index = 0; index < expiredEffects.Count; index++)
            {
                _activeStatuses.Remove(expiredEffects[index]);
            }
        }

        private void ApplyHitFlash()
        {
            if (_spriteRenderer == null)
            {
                return;
            }

            _flashTimer = _hitReactionData != null ? _hitReactionData.FlashDurationSeconds : 0.08f;
            var flashIntensity = _hitReactionData != null ? _hitReactionData.FlashIntensity : 0.7f;
            _spriteRenderer.color = Color.Lerp(_baseColor, Color.white, flashIntensity);
        }

        private void UpdateFlash(float deltaTime)
        {
            if (_flashTimer <= 0f || _spriteRenderer == null)
            {
                return;
            }

            _flashTimer -= deltaTime;

            if (_flashTimer <= 0f)
            {
                _spriteRenderer.color = _healthRuntime != null && _healthRuntime.IsAlive ? _baseColor : (_targetData != null ? _targetData.DeadColor : Color.gray);
            }
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
                _spriteRenderer.sprite = Corebound.Gameplay.Shared.DebugSpriteFactory.CreateSquare(32f);
            }

            _boxCollider2D = GetComponent<BoxCollider2D>();

            if (_boxCollider2D == null)
            {
                _boxCollider2D = gameObject.AddComponent<BoxCollider2D>();
            }

            var rigidbody2D = GetComponent<Rigidbody2D>();

            if (rigidbody2D == null)
            {
                rigidbody2D = gameObject.AddComponent<Rigidbody2D>();
            }

            rigidbody2D.bodyType = RigidbodyType2D.Kinematic;
            rigidbody2D.gravityScale = 0f;
            rigidbody2D.freezeRotation = true;

            if (_targetData != null)
            {
                _boxCollider2D.size = _targetData.Size;
            }
        }
    }
}