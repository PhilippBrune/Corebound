using System;

namespace Corebound.Gameplay.Bosses
{
    public sealed class BossEventArgs : EventArgs
    {
        public BossEventArgs(int stageIndex, int waveIndex, string bossId, string bossName, int phaseIndex, string phaseLabel, float healthNormalized)
        {
            StageIndex = stageIndex;
            WaveIndex = waveIndex;
            BossId = bossId ?? string.Empty;
            BossName = bossName ?? string.Empty;
            PhaseIndex = phaseIndex;
            PhaseLabel = phaseLabel ?? string.Empty;
            HealthNormalized = healthNormalized;
        }

        public int StageIndex { get; }

        public int WaveIndex { get; }

        public string BossId { get; }

        public string BossName { get; }

        public int PhaseIndex { get; }

        public string PhaseLabel { get; }

        public float HealthNormalized { get; }
    }
}
