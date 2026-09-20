using System;

namespace Corebound.Gameplay.Bosses
{
    public static class BossEvents
    {
        public static event Action<BossEventArgs> OnBossSpawned;

        public static event Action<BossEventArgs> OnBossPhaseChanged;

        // Slice 12 lists "Boss-Intro, Phasenwechsel, Grossangriff und Boss-Sieg" as the four
        // Audio event hooks. Three existed; this is the Grossangriff one. Raised at the start
        // of an attack windup, not when the shot lands, so the cue arrives with the telegraph
        // and is a warning rather than a report.
        public static event Action<BossEventArgs> OnBossHeavyAttack;

        public static event Action<BossEventArgs> OnBossDefeated;

        public static void RaiseBossSpawned(BossEventArgs eventArgs)
        {
            OnBossSpawned?.Invoke(eventArgs);
        }

        public static void RaiseBossPhaseChanged(BossEventArgs eventArgs)
        {
            OnBossPhaseChanged?.Invoke(eventArgs);
        }

        public static void RaiseBossHeavyAttack(BossEventArgs eventArgs)
        {
            OnBossHeavyAttack?.Invoke(eventArgs);
        }

        public static void RaiseBossDefeated(BossEventArgs eventArgs)
        {
            OnBossDefeated?.Invoke(eventArgs);
        }
    }
}
