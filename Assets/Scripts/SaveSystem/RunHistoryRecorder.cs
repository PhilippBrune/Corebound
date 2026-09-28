using System.Collections.Generic;
using Corebound.MetaProgression;
using UnityEngine;

namespace Corebound.SaveSystem
{
    // The one place a finished run is written to history, mirroring MetaRewardLedger: a single
    // chokepoint means no caller can forget, and the tutorial exclusion lives in exactly one place.
    //
    // D3 (owner 2026-09-28): victories and deaths only. A run abandoned to the main menu leaves no
    // trace, which is consistent with treating the history as a record of finished attempts - and
    // is the reason there is no Abandoned outcome in the enum at all.
    public static class RunHistoryRecorder
    {
        public static void RecordRunEnded(
            ISaveManager saveManager,
            MetaProgressionDefinitionData definition,
            RunOutcome outcome,
            int stageReached,
            int waveReached,
            string endedByLabel,
            float elapsedActiveSeconds,
            float elapsedRealSeconds,
            System.DateTime startedUtc,
            int enemiesKilled,
            int scrapEarned,
            IReadOnlyList<string> cardIds,
            IReadOnlyList<string> relicIds)
        {
            if (saveManager == null)
            {
                return;
            }

            var runSaveData = saveManager.LoadRunSaveData();

            // A guided run is not a performance - the same reason it earns no Alloy or SP.
            if (runSaveData != null && runSaveData.IsTutorialRun)
            {
                return;
            }

            var metaSaveData = definition != null
                ? saveManager.LoadMetaProgressionSaveData(definition.CreateDefaultSaveData())
                : null;

            var entry = RunHistoryEntry.Create(
                startedUtc,
                outcome,
                elapsedActiveSeconds,
                elapsedRealSeconds,
                stageReached,
                waveReached,
                endedByLabel,
                ProfileSelection.ActiveIndex,
                metaSaveData != null ? metaSaveData.AscensionRank : 0);

            entry.SetBuild(
                runSaveData != null ? runSaveData.EquippedWeaponIds : null,
                runSaveData != null ? runSaveData.EquippedWeaponTiers : null,
                cardIds,
                relicIds,
                runSaveData != null ? runSaveData.BuiltTowerIds : null,
                runSaveData != null ? runSaveData.BuiltTowerUpgradeLevels : null);

            // MetaRewardLedger's per-run totals are the authority on what this run actually paid
            // out; the save's lifetime figures would report the profile's whole history instead.
            entry.SetEarnings(scrapEarned, MetaRewardLedger.RunAlloy, MetaRewardLedger.RunSkillPoints, enemiesKilled);

            var history = saveManager.LoadRunHistorySaveData() ?? new RunHistorySaveData();
            history.Add(entry);
            history.SortNewestFirst();
            saveManager.SaveRunHistorySaveData(history);

            // The dead field finally means something: StatisticsSaveData has carried
            // _totalPlaytimeSeconds and AddPlaytimeSeconds since it was written, and nothing ever
            // called them, so it has been reading 0 for the life of the project.
            var statistics = saveManager.LoadStatisticsSaveData(new StatisticsSaveData());

            if (statistics != null)
            {
                statistics.AddPlaytimeSeconds(Mathf.Max(0f, elapsedRealSeconds));
                saveManager.SaveStatisticsSaveData(statistics);
            }
        }
    }
}
