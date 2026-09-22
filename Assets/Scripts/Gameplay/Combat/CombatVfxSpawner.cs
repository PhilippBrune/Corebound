using Corebound.Settings;
using UnityEngine;

namespace Corebound.Gameplay.Combat
{
    // Gives Combat a first, real visual feedback layer: a short expanding/fading burst on every
    // hit and a larger one on death. CombatEvents are static, so one instance anywhere in the
    // scene covers Player, Enemy, Boss, Tower and Core hits alike. The original
    // combat_vfx_sheet_essentials.png atlas documented 16 effect families but had an irregular
    // (non-grid) layout; it was replaced (2026-09-08) by individual per-effect images under
    // Assets/Art/Combat/VFX/ (see GRAPHICS_ASSET_PROMPTS.md Asset 24a-24g), which this spawner now
    // loads for hit/death. If either sprite fails to load, the original procedural colored-square
    // burst remains as a safe fallback so combat still has feedback.
    public sealed class CombatVfxSpawner : MonoBehaviour
    {
        private const string HitSparkSpritePath = "Assets/Art/Combat/VFX/vfx_hit_spark.png";
        private const string DeathBurstSpritePath = "Assets/Art/Combat/VFX/vfx_enemy_death_burst.png";
        private const string CoreFlareSpritePath = "Assets/Art/Combat/VFX/vfx_core_damage_flare.png";
        private const string BurnTickSpritePath = "Assets/Art/Combat/VFX/vfx_burn_status_tick.png";
        private const string FreezePulseSpritePath = "Assets/Art/Combat/VFX/vfx_freeze_status_pulse.png";
        private const string TowerActivationPulseSpritePath = "Assets/Art/Combat/VFX/vfx_tower_activation_pulse.png";

        // Never fully hides feedback at the low end - Settings.md frames Effects Intensity as an
        // accessibility control for reducing visual intensity, not eliminating combat feedback.
        private const float MinEffectsIntensityScale = 0.4f;

        [SerializeField] private SettingsManager _settingsManager;
        [SerializeField] private Color _hitSparkColor = new Color(1f, 0.95f, 0.6f, 0.85f);
        [SerializeField] private Color _deathBurstColor = new Color(1f, 0.5f, 0.22f, 0.95f);
        [SerializeField] private Color _coreFlareColor = new Color(1f, 0.3f, 0.2f, 0.95f);
        [SerializeField] private Color _burnTickColor = new Color(1f, 0.55f, 0.2f, 0.85f);
        [SerializeField] private Color _freezePulseColor = new Color(0.55f, 0.85f, 1f, 0.85f);
        [SerializeField] private float _hitSparkSize = 0.32f;
        [SerializeField] private float _deathBurstSize = 0.62f;
        [SerializeField] private float _coreFlareSize = 0.9f;
        [SerializeField] private float _statusEffectVfxSize = 0.4f;
        [SerializeField] private float _hitSparkLifetimeSeconds = 0.16f;
        [SerializeField] private float _deathBurstLifetimeSeconds = 0.3f;
        [SerializeField] private float _coreFlareLifetimeSeconds = 0.4f;
        [SerializeField] private float _statusEffectVfxLifetimeSeconds = 0.35f;

        private static Sprite _hitSparkSprite;
        private static Sprite _deathBurstSprite;
        private static Sprite _coreFlareSprite;
        private static Sprite _burnTickSprite;
        private static Sprite _freezePulseSprite;
        private static Sprite _towerActivationPulseSprite;
        private static bool _spritesLoaded;
        private static readonly Color TowerActivationPulseColor = new Color(0.55f, 0.75f, 1f, 0.8f);

        private float EffectsIntensityScale
        {
            get
            {
                var intensity = _settingsManager != null && _settingsManager.RuntimeSettings != null
                    ? _settingsManager.RuntimeSettings.EffectsIntensity
                    : 1f;

                return Mathf.Lerp(MinEffectsIntensityScale, 1f, Mathf.Clamp01(intensity));
            }
        }

        private void OnEnable()
        {
            EnsureSpritesLoaded();
            CombatEvents.DamageTaken += OnDamageTaken;
            CombatEvents.CombatantDied += OnCombatantDied;
            CombatEvents.StatusApplied += OnStatusApplied;
        }

        private static void EnsureSpritesLoaded()
        {
            if (_spritesLoaded)
            {
                return;
            }

            _spritesLoaded = true;
            _hitSparkSprite = Corebound.Gameplay.Shared.DebugSpriteFactory.LoadProjectSprite(HitSparkSpritePath);
            _deathBurstSprite = Corebound.Gameplay.Shared.DebugSpriteFactory.LoadProjectSprite(DeathBurstSpritePath);
            _coreFlareSprite = Corebound.Gameplay.Shared.DebugSpriteFactory.LoadProjectSprite(CoreFlareSpritePath);
            _burnTickSprite = Corebound.Gameplay.Shared.DebugSpriteFactory.LoadProjectSprite(BurnTickSpritePath);
            _freezePulseSprite = Corebound.Gameplay.Shared.DebugSpriteFactory.LoadProjectSprite(FreezePulseSpritePath);
            _towerActivationPulseSprite = Corebound.Gameplay.Shared.DebugSpriteFactory.LoadProjectSprite(TowerActivationPulseSpritePath);
        }

        // Callable directly by TowerRuntimeController (a pure runtime-constructed object with no
        // scene reference to a live CombatVfxSpawner instance) so tower fire feedback doesn't need
        // its own duplicate sprite-loading/burst-spawning path.
        public static void SpawnTowerActivationPulse(Vector2 position)
        {
            EnsureSpritesLoaded();
            SpawnBurst(position, _towerActivationPulseSprite, TowerActivationPulseColor, 0.5f, 0.25f, 44, 1f);
        }

        private void OnDisable()
        {
            CombatEvents.DamageTaken -= OnDamageTaken;
            CombatEvents.CombatantDied -= OnCombatantDied;
            CombatEvents.StatusApplied -= OnStatusApplied;
        }

        private void OnDamageTaken(DamageEventArgs eventArgs)
        {
            // Core damage takes priority over the generic hit-spark instead of stacking both -
            // VFX.md calls the Core-Damage-Flare the highest-priority signal in the whole set.
            if (eventArgs.Target != null && eventArgs.Target.name == CombatHitReceiver.CoreAnchorObjectName)
            {
                SpawnBurst(eventArgs.DamageContext.HitPoint, _coreFlareSprite, _coreFlareColor, _coreFlareSize, _coreFlareLifetimeSeconds, 47, EffectsIntensityScale);
                return;
            }

            // Playtest 2026-09-22: "crit hit feedback should be implemented". A crit gets a larger,
            // gold spark and a rising number, so a build that stacks crit can see it paying off -
            // the crit audio cue alone was the only signal, and it is de-duplicated.
            if (eventArgs.DamageContext.IsCritical && eventArgs.DamageContext.SourceType == DamageSourceType.Player)
            {
                SpawnBurst(eventArgs.DamageContext.HitPoint, _hitSparkSprite, CriticalSparkColor, _hitSparkSize * CriticalSparkScale, _hitSparkLifetimeSeconds * 1.4f, 46, EffectsIntensityScale, CriticalSparkColor);
                CriticalHitPopup.Spawn(eventArgs.DamageContext.HitPoint, eventArgs.DamageContext.Amount);
                return;
            }

            SpawnBurst(eventArgs.DamageContext.HitPoint, _hitSparkSprite, _hitSparkColor, _hitSparkSize, _hitSparkLifetimeSeconds, 45, EffectsIntensityScale);
        }

        private static readonly Color CriticalSparkColor = new Color(1f, 0.82f, 0.25f, 1f);
        private const float CriticalSparkScale = 1.8f;

        private void OnCombatantDied(DeathEventArgs eventArgs)
        {
            var position = eventArgs.Victim != null ? (Vector2)eventArgs.Victim.transform.position : eventArgs.DamageContext.HitPoint;
            SpawnBurst(position, _deathBurstSprite, _deathBurstColor, _deathBurstSize, _deathBurstLifetimeSeconds, 46, EffectsIntensityScale);
        }

        private void OnStatusApplied(StatusAppliedEventArgs eventArgs)
        {
            if (eventArgs.Target == null)
            {
                return;
            }

            var position = (Vector2)eventArgs.Target.transform.position;

            switch (eventArgs.EffectType)
            {
                case StatusEffectType.Burn:
                    SpawnBurst(position, _burnTickSprite, _burnTickColor, _statusEffectVfxSize, _statusEffectVfxLifetimeSeconds, 44, EffectsIntensityScale);
                    break;
                case StatusEffectType.Freeze:
                    SpawnBurst(position, _freezePulseSprite, _freezePulseColor, _statusEffectVfxSize, _statusEffectVfxLifetimeSeconds, 44, EffectsIntensityScale);
                    break;
            }
        }

        private static void SpawnBurst(Vector2 position, Sprite artSprite, Color fallbackColor, float targetSize, float lifetimeSeconds, int sortingOrder, float intensityScale)
        {
            SpawnBurst(position, artSprite, fallbackColor, targetSize, lifetimeSeconds, sortingOrder, intensityScale, Color.white);
        }

        // artTint multiplies the art sprite (white = unchanged); the crit spark is the same art in gold.
        private static void SpawnBurst(Vector2 position, Sprite artSprite, Color fallbackColor, float targetSize, float lifetimeSeconds, int sortingOrder, float intensityScale, Color artTint)
        {
            var burstObject = new GameObject("CombatVfxBurst");
            burstObject.transform.position = position;

            var spriteRenderer = burstObject.AddComponent<SpriteRenderer>();

            if (artSprite != null)
            {
                spriteRenderer.sprite = artSprite;
                spriteRenderer.color = new Color(artTint.r, artTint.g, artTint.b, intensityScale);
            }
            else
            {
                spriteRenderer.sprite = Corebound.Gameplay.Shared.DebugSpriteFactory.CreateSquare(32f);
                spriteRenderer.color = new Color(fallbackColor.r, fallbackColor.g, fallbackColor.b, fallbackColor.a * intensityScale);
            }

            spriteRenderer.sortingOrder = sortingOrder;

            burstObject.AddComponent<CombatVfxBurstRuntime>().Initialize(targetSize * intensityScale, lifetimeSeconds);
        }
    }
}
