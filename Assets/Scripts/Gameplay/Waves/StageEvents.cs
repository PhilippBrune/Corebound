using System;

namespace Corebound.Gameplay.Waves
{
    public static class StageEvents
    {
        public static event Action<StageEventArgs> StageStarted;

        public static event Action<StageEventArgs> StageCompleted;

        public static event Action<StageEventArgs> StageTransitioned;

        public static void RaiseStageStarted(StageEventArgs eventArgs)
        {
            StageStarted?.Invoke(eventArgs);
        }

        public static void RaiseStageCompleted(StageEventArgs eventArgs)
        {
            StageCompleted?.Invoke(eventArgs);
        }

        public static void RaiseStageTransitioned(StageEventArgs eventArgs)
        {
            StageTransitioned?.Invoke(eventArgs);
        }
    }
}