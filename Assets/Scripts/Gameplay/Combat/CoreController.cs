using System;
using UnityEngine;

namespace Corebound.Gameplay.Combat
{
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

        private CoreData _coreData;
        private CombatHitReceiver _hitReceiver;
        private bool _isBelowCriticalThreshold;

        public CoreData CoreData => _coreData;

        public CombatHitReceiver HitReceiver => _hitReceiver;

        public void Configure(CoreData coreData, CombatHitReceiver hitReceiver)
        {
            CombatEvents.DamageTaken -= HandleDamageTaken;
            CombatEvents.CombatantDied -= HandleCombatantDied;

            _coreData = coreData;
            _hitReceiver = hitReceiver;
            _isBelowCriticalThreshold = false;

            CombatEvents.DamageTaken += HandleDamageTaken;
            CombatEvents.CombatantDied += HandleCombatantDied;
        }

        private void OnDestroy()
        {
            CombatEvents.DamageTaken -= HandleDamageTaken;
            CombatEvents.CombatantDied -= HandleCombatantDied;
        }

        private void HandleDamageTaken(DamageEventArgs eventArgs)
        {
            if (_hitReceiver == null || eventArgs.Target != _hitReceiver.gameObject)
            {
                return;
            }

            OnCoreDamaged?.Invoke(eventArgs);

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
