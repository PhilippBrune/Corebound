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

        // Asset 103. A 4x2 grid of 364x360 cells: row 0 is a flame impact over four frames, row 1
        // a frost impact. Delivered 2026-09-23 with the transparency checkerboard painted into the
        // pixels, repaired 2026-09-27, and wired here now that a burst can play a frame sequence.
        // Burn and Freeze previously shared the generic still, so a burning enemy and a frozen one
        // looked identical at the moment of impact - exactly what this sheet exists to fix.
        private const string ImpactFlameFrostSheetPath = "Assets/Art/Combat/VFX/vfx_impact_flame_frost.png";
        private const int ImpactSheetColumns = 4;
        private const int ImpactSheetRows = 2;

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
        private static Sprite[] _burnFrames;
        private static Sprite[] _freezeFrames;
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
            _burnFrames = LoadImpactRow(0);
            _freezeFrames = LoadImpactRow(1);
        }

        // Returns null rather than a part-filled array if any cell fails to load, so the caller
        // falls back to the existing single-sprite behaviour instead of flickering through holes.
        private static Sprite[] LoadImpactRow(int row)
        {
            return LoadSheetRow(ImpactFlameFrostSheetPath, row, ImpactSheetColumns, ImpactSheetRows);
        }

        private static Sprite[] LoadSheetRow(string sheetPath, int row, int columns, int rows)
        {
            var frames = new Sprite[columns];

            for (var column = 0; column < columns; column++)
            {
                frames[column] = Corebound.Gameplay.Shared.DebugSpriteFactory.LoadProjectAtlasCellSprite(
                    sheetPath, column, row, columns, rows);

                if (frames[column] == null)
                {
                    return null;
                }
            }

            return frames;
        }

        // Callable directly by TowerRuntimeController (a pure runtime-constructed object with no
        // scene reference to a live CombatVfxSpawner instance) so tower fire feedback doesn't need
        // its own duplicate sprite-loading/burst-spawning path.
        // Asset 105: Nova Pulse fires a ring in every direction; without a pulse at the player the
        // volley reads as scattered shards rather than one deliberate ring.
        private const string NovaRingSpritePath = "Assets/Art/Combat/VFX/vfx_nova_ring_pulse.png";
        private static Sprite _novaRingSprite;
        private static readonly Color NovaRingColor = new Color(0.85f, 0.55f, 1f, 0.85f);

        public static void SpawnNovaRing(Vector2 position)
        {
            if (_novaRingSprite == null)
            {
                _novaRingSprite = Corebound.Gameplay.Shared.DebugSpriteFactory.LoadProjectSprite(NovaRingSpritePath);
            }

            if (_novaRingSprite == null)
            {
                return;
            }

            SpawnBurst(position, _novaRingSprite, NovaRingColor, 3.4f, 0.28f, 44, 1f);
        }

        // Asset 104, wired 2026-09-27. A projectile with ExplodeOnImpact/ExplodeOnExpire dealt its
        // splash damage in complete silence visually - nothing on screen showed the radius, so the
        // player had no way to learn the Launcher's reach. The burst is sized to the caller's
        // radius so what is drawn is what actually got hit, rather than a decorative flash.
        //
        // Same 4x2 grid as the flame/frost sheet; the delivered art runs seven frames of a
        // mechanical mine detonating rather than the five-frame shockwave Asset 104 specified, so
        // all eight cells are played in order and the last (empty) one simply lands on the fade.
        private const string ImpactSplashSheetPath = "Assets/Art/Combat/VFX/vfx_impact_splash_burst.png";
        private static Sprite[] _splashFrames;
        private static bool _splashFramesLoaded;
        private static readonly Color SplashBurstColor = new Color(1f, 0.72f, 0.35f, 0.9f);

        public static void SpawnSplashBurst(Vector2 position, float radius)
        {
            if (!_splashFramesLoaded)
            {
                _splashFramesLoaded = true;
                _splashFrames = LoadSheetRow(ImpactSplashSheetPath, 0, ImpactSheetColumns, ImpactSheetRows);
            }

            SpawnBurst(position, null, SplashBurstColor, Mathf.Max(0.2f, radius * 2f), 0.34f, 46, 1f, Color.white, _splashFrames);
        }

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
                    SpawnBurst(position, _burnTickSprite, _burnTickColor, _statusEffectVfxSize, _statusEffectVfxLifetimeSeconds, 44, EffectsIntensityScale, Color.white, _burnFrames);
                    break;
                case StatusEffectType.Freeze:
                    SpawnBurst(position, _freezePulseSprite, _freezePulseColor, _statusEffectVfxSize, _statusEffectVfxLifetimeSeconds, 44, EffectsIntensityScale, Color.white, _freezeFrames);
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
            SpawnBurst(position, artSprite, fallbackColor, targetSize, lifetimeSeconds, sortingOrder, intensityScale, artTint, null);
        }

        private static void SpawnBurst(Vector2 position, Sprite artSprite, Color fallbackColor, float targetSize, float lifetimeSeconds, int sortingOrder, float intensityScale, Color artTint, Sprite[] frames)
        {
            // A frame sequence supersedes the single still; an unloadable sheet leaves frames null
            // and the still is used, so this can never render nothing.
            artSprite = frames != null && frames.Length > 0 ? frames[0] : artSprite;
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

            burstObject.AddComponent<CombatVfxBurstRuntime>().Initialize(targetSize * intensityScale, lifetimeSeconds, frames);
        }
    }
}
