using Corebound.SaveSystem;

namespace Corebound.Gameplay.Waves
{
    // Extracted from WaveManager (God-object split, 2026-09-08): pure statistics/meta-currency
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

        // Meta Progression only runs as a MonoBehaviour service inside the MainMenu scene, so
        // gameplay writes the currency reward directly through the same save-domain accessor
        // TowerService/CardService already use for unlock checks, rather than requiring the
        // service itself to be duplicated into the gameplay scene.
        public static void GrantMetaCurrencyReward(ISaveManager saveManager, int amount)
        {
            if (amount <= 0 || saveManager == null)
            {
                return;
            }

            var metaSaveData = saveManager.LoadMetaProgressionSaveData(new MetaProgressionSaveData());
            metaSaveData.SetMetaCurrencyAmount(metaSaveData.MetaCurrencyAmount + amount);
            saveManager.SaveMetaProgressionSaveData(metaSaveData);
        }
    }
}
