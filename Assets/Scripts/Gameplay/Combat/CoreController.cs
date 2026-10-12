using System;
using UnityEngine;

namespace Corebound.Gameplay.Combat
{
    // One landed hit on the Core, as the protection layer needs it (CORE_PROTECTION_FEEL_2026-10-07).
    public readonly struct CoreHitInfo
    {
        public CoreHitInfo(Vector2 position, float amount, float healthNormalized, bool guarded)
        {
            Position = position;
            Amount = amount;
            HealthNormalized = healthNormalized;
            Guarded = guarded;
        }

        public Vector2 Position { get; }

        public float Amount { get; }

        // After the hit.
        public float HealthNormalized { get; }

        // The Core Guard was raised, so this hit was blocked down to its remainder.
        public bool Guarded { get; }
    }

    // Core.md's documented Technical Design: "Ein CoreController haelt die Unity-seitige
    // Verankerung in der Scene... Events wie OnCoreDamaged, OnCoreCritical und OnCoreDestroyed
    // informieren UI, Audio, Waves und Run-State-Logik." Before this, the Core was a bare
    // GameObject wearing a generic CombatHitReceiver built ad hoc by WaveManager, and every
    // consumer (camera shake, audio, HUD) filtered the shared CombatEvents stream by GameObject
    // identity itself. This sits on the same CoreAnchor GameObject, owns the Core's CoreData
    // asset, and republishes the three documented events so future consumers don't need to know
    // CombatEvents exists or which GameObject the Core happens to be.
    public sealed class CoreController : MonoBehaviour
    {
        public event Action<DamageEventArgs> OnCoreDamaged;

        public event Action OnCoreCritical;

        public event Action<DeathEventArgs> OnCoreDestroyed;

        // Static as well as per instance: the hit sound, the screen-edge pulse, the Core's voice
        // and the guard charge all listen, and none of them is handed the Core.
        public static event Action<CoreHitInfo> CoreHit;

        private CoreData _coreData;
        private CombatHitReceiver _hitReceiver;
        private bool _isBelowCriticalThreshold;

        // The Core of the running scene, or null between scenes.
        public static CoreController Active { get; private set; }

        // The protection layer's numbers, from the Core's asset when there is one.
        public static CoreData Tuning => Active != null && Active._coreData != null ? Active._coreData : CoreData.Fallback;

        public CoreData CoreData => _coreData;

        public CombatHitReceiver HitReceiver => _hitReceiver;

        public void Configure(CoreData coreData, CombatHitReceiver hitReceiver)
        {
            CombatEvents.DamageTaken -= HandleDamageTaken;
            CombatEvents.CombatantDied -= HandleCombatantDied;

            _coreData = coreData;
            _hitReceiver = hitReceiver;
            _isBelowCriticalThreshold = false;
            Active = this;

            CombatEvents.DamageTaken += HandleDamageTaken;
            CombatEvents.CombatantDied += HandleCombatantDied;
        }

        private void OnDestroy()
        {
            CombatEvents.DamageTaken -= HandleDamageTaken;
            CombatEvents.CombatantDied -= HandleCombatantDied;

            if (Active == this)
            {
                Active = null;
            }
        }

        private void HandleDamageTaken(DamageEventArgs eventArgs)
        {
            if (_hitReceiver == null || eventArgs.Target != _hitReceiver.gameObject)
            {
                return;
            }

            OnCoreDamaged?.Invoke(eventArgs);

            if (eventArgs.DamageContext.Amount > 0f)
            {
                CoreHit?.Invoke(new CoreHitInfo(transform.position, eventArgs.DamageContext.Amount, _hitReceiver.CurrentHealthNormalized, _hitReceiver.IsGuardActive));
            }

            var threshold = _coreData != null ? _coreData.CriticalHealthThreshold : 0.35f;
            var isBelowThresholdNow = _hitReceiver.CurrentHealthNormalized <= threshold;

            if (isBelowThresholdNow && !_isBelowCriticalThreshold)
            {
                OnCoreCritical?.Invoke();
            }

            _isBelowCriticalThreshold = isBelowThresholdNow;
        }

        private void HandleCombatantDied(DeathEventArgs eventArgs)
        {
            if (_hitReceiver == null || eventArgs.Victim != _hitReceiver.gameObject)
            {
                return;
            }

            OnCoreDestroyed?.Invoke(eventArgs);
        }
    }
}
