using Corebound.SaveSystem;

namespace Corebound.Gameplay.Waves
{
    // Extracted from WaveManager (God-object split, 2026-09-08): pure statistics
    // persistence with no dependency on WaveManager's spawn/state-machine fields, so it can be
    // called with just the save manager it needs at the call site.
    internal static class WaveStatisticsRecorder
    {
        public static void RecordEnemyKill(ISaveManager saveManager)
        {
            if (saveManager == null)
            {
                return;
            }

            var statistics = saveManager.LoadStatisticsSaveData(new StatisticsSaveData());
            statistics.RecordEnemyKill();
            saveManager.SaveStatisticsSaveData(statistics);
        }

        public static void RecordRunOutcome(ISaveManager saveManager, int stageIndex, bool completed)
        {
            if (saveManager == null)
            {
                return;
            }

            var statistics = saveManager.LoadStatisticsSaveData(new StatisticsSaveData());

            if (completed)
            {
                statistics.RecordRunCompleted(stageIndex);
            }
            else
            {
                statistics.RecordRunFailed(stageIndex);
            }

            saveManager.SaveStatisticsSaveData(statistics);
        }
    }
}
