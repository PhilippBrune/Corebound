using System;

namespace Corebound.Gameplay.Waves
{
    public static class WaveEvents
    {
        public static event Action<WaveProgressEventArgs> WaveStarted;

        public static event Action<WaveProgressEventArgs> WaveCompleted;

        public static event Action<WaveProgressEventArgs> RewardPending;

        public static event Action<WaveProgressEventArgs> IntermissionStarted;

        public static event Action<WaveProgressEventArgs> StageCompleted;

        public static void RaiseWaveStarted(WaveProgressEventArgs eventArgs)
        {
            WaveStarted?.Invoke(eventArgs);
        }

        public static void RaiseWaveCompleted(WaveProgressEventArgs eventArgs)
        {
            WaveCompleted?.Invoke(eventArgs);
        }

        public static void RaiseRewardPending(WaveProgressEventArgs eventArgs)
        {
            RewardPending?.Invoke(eventArgs);
        }

        public static void RaiseIntermissionStarted(WaveProgressEventArgs eventArgs)
        {
            IntermissionStarted?.Invoke(eventArgs);
        }

        public static void RaiseStageCompleted(WaveProgressEventArgs eventArgs)
        {
            StageCompleted?.Invoke(eventArgs);
        }
    }
}