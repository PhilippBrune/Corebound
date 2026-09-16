using System;

namespace Corebound.Gameplay.Combat
{
    public static class CombatEvents
    {
        public static event Action<DamageEventArgs> DamageTaken;

        public static event Action<DeathEventArgs> CombatantDied;

        public static event Action<StatusAppliedEventArgs> StatusApplied;

        public static void RaiseDamageTaken(DamageEventArgs eventArgs)
        {
            DamageTaken?.Invoke(eventArgs);
        }

        public static void RaiseCombatantDied(DeathEventArgs eventArgs)
        {
            CombatantDied?.Invoke(eventArgs);
        }

        public static void RaiseStatusApplied(StatusAppliedEventArgs eventArgs)
        {
            StatusApplied?.Invoke(eventArgs);
        }
    }
}