using Corebound.Audio;
using Corebound.Gameplay.Combat;
using Corebound.Gameplay.Enemies;
using Corebound.Gameplay.Resources;
using Corebound.UI;
using UnityEngine;

namespace Corebound.Gameplay.Waves
{
    // What a cleared wave paid for keeping the Core whole (D10), read by the wave banner.
    public readonly struct CoreWaveClear
    {
        public CoreWaveClear(bool untouched, int streak, int scrap)
        {
            Untouched = untouched;
            Streak = streak;
            Scrap = scrap;
        }

        public bool Untouched { get; }

        // Untouched waves in a row, this one included.
        public int Streak { get; }

        public int Scrap { get; }
    }

    // The economy half of CORE_PROTECTION_FEEL_2026-10-07 (owner decisions P2-1, P3-1, P4-1):
    // - B4: a Core-hit sound, at most one per HitCueMinInterval so a swarm reads as an alarm;
    // - C9: an enemy killed near the Core while it was going for it pays Scrap ("Intercepted +1");
    // - D10: a wave cleared without a Core hit pays Scrap, more from the third in a row.
    // The streak lives for the session: a resumed run starts counting again.
    //
    // Added to the WaveManager's object at runtime like GameplayQuestTracker, so no scene edit is
    // needed. The numbers are CoreData's.
    public sealed class CoreDefenseDirector : MonoBehaviour
    {
        private static readonly Color InterceptColor = new Color(0.55f, 0.88f, 1f, 1f);

        private ResourceWalletService _wallet;
        private bool _coreHitThisWave;
        private int _untouchedStreak;
        private float _nextHitCueAt;

#if UNITY_EDITOR
        // Autopilot "core" probe.
        public static int DebugCoreHits;
        public static int DebugHitCues;
        public static int DebugIntercepts;
        public static int DebugInterceptScrap;
        public static int DebugUntouchedWaves;
        public static int DebugUntouchedScrap;
#endif

        public CoreWaveClear LastWaveClear { get; private set; }

        public int UntouchedStreak => _untouchedStreak;

        public void Bind(ResourceWalletService wallet)
        {
            _wallet = wallet;
        }

        private void OnEnable()
        {
            CoreController.CoreHit += OnCoreHit;
            WaveEvents.WaveStarted += OnWaveStarted;
            CombatEvents.CombatantDied += OnCombatantDied;
        }

        private void OnDisable()
        {
            CoreController.CoreHit -= OnCoreHit;
            WaveEvents.WaveStarted -= OnWaveStarted;
            CombatEvents.CombatantDied -= OnCombatantDied;
        }

        private void OnWaveStarted(WaveProgressEventArgs eventArgs)
        {
            _coreHitThisWave = false;
            LastWaveClear = default;
        }

        private void OnCoreHit(CoreHitInfo hit)
        {
            _coreHitThisWave = true;
#if UNITY_EDITOR
            DebugCoreHits++;
#endif
            var tuning = CoreController.Tuning;

            // Unscaled: a hit that lands as a pause opens must not hold the next cue back for ever.
            if (Time.unscaledTime < _nextHitCueAt || tuning.HitCue == null)
            {
                return;
            }

            _nextHitCueAt = Time.unscaledTime + tuning.HitCueMinInterval;
            AudioManager.PlayCueStatic(tuning.HitCue);
#if UNITY_EDITOR
            DebugHitCues++;
#endif
        }

        // Called by WaveManager immediately before WaveCompleted is raised, so every listener of
        // that event - the wave banner first - already sees this wave's result.
        public CoreWaveClear ResolveWaveClear()
        {
            if (_coreHitThisWave)
            {
                _untouchedStreak = 0;
                LastWaveClear = new CoreWaveClear(false, 0, 0);
                return LastWaveClear;
            }

            var tuning = CoreController.Tuning;
            _untouchedStreak++;
            var scrap = _untouchedStreak >= tuning.UntouchedStreakLength ? tuning.UntouchedStreakScrap : tuning.UntouchedWaveScrap;

            if (_wallet != null && scrap > 0)
            {
                _wallet.AddFlat(scrap);
            }

            LastWaveClear = new CoreWaveClear(true, _untouchedStreak, scrap);
#if UNITY_EDITOR
            DebugUntouchedWaves++;
            DebugUntouchedScrap += scrap;
#endif

            if (scrap > 0)
            {
                NotificationPresenter.Show(LocalizationService.Resolve("Core untouched", "Kern unversehrt")
                    + (_untouchedStreak >= 2 ? " ×" + _untouchedStreak : string.Empty)
                    + "  ·  +" + scrap + LocalizationService.Resolve(" Scrap", " Schrott"));
            }

            return LastWaveClear;
        }

        // A kill by the player or a tower (status ticks count as the player's), close to the Core,
        // of an enemy that was going for it a moment ago - shooting an enemy turns it on the
        // player, so "at the moment of death" would almost never be true.
        private void OnCombatantDied(DeathEventArgs eventArgs)
        {
            if (eventArgs.Victim == null)
            {
                return;
            }

            var source = eventArgs.DamageContext.SourceType;

            if (source != DamageSourceType.Player && source != DamageSourceType.Tower)
            {
                return;
            }

            var enemy = eventArgs.Victim.GetComponent<EnemyRuntimeController>();
            var core = CoreController.Active;

            if (enemy == null || core == null)
            {
                return;
            }

            var tuning = CoreController.Tuning;
            var position = (Vector2)eventArgs.Victim.transform.position;
            var radius = tuning.InterceptRadius;

            if (Time.time - enemy.LastHeadingForCoreAt > tuning.InterceptRecentSeconds
                || (position - (Vector2)core.transform.position).sqrMagnitude > radius * radius)
            {
                return;
            }

            var scrap = tuning.InterceptScrap;

            if (_wallet != null && scrap > 0)
            {
                _wallet.AddFlat(scrap);
            }

#if UNITY_EDITOR
            DebugIntercepts++;
            DebugInterceptScrap += scrap;
#endif
            CriticalHitPopup.SpawnLabel(position + Vector2.up * 0.35f,
                LocalizationService.Resolve("Intercepted", "Abgefangen") + (scrap > 0 ? " +" + scrap : string.Empty),
                InterceptColor, 0.85f, 0.04f);
        }
    }
}
