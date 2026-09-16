using System;

namespace Corebound.Gameplay.Bosses
{
    public static class BossEvents
    {
        public static event Action<BossEventArgs> OnBossSpawned;

        public static event Action<BossEventArgs> OnBossPhaseChanged;

        public static event Action<BossEventArgs> OnBossDefeated;

        public static void RaiseBossSpawned(BossEventArgs eventArgs)
        {
            OnBossSpawned?.Invoke(eventArgs);
        }

        public static void RaiseBossPhaseChanged(BossEventArgs eventArgs)
        {
            OnBossPhaseChanged?.Invoke(eventArgs);
        }

        public static void RaiseBossDefeated(BossEventArgs eventArgs)
        {
            OnBossDefeated?.Invoke(eventArgs);
        }
    }
}
