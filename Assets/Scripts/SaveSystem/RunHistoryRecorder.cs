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
        // What the run was carrying when it ended, read from the live objects by the caller.
        //
        // Playtest 2026-09-29: "Records should contain which weapons were used." The entries had
        // a weapon list all along, and it was empty for every lost run: RunPersistenceController
        // clears the run save the moment the player or Core dies, and that handler runs before
        // WaveManager writes the history, so the build was read from an already-wiped save. The
        // same wipe also dropped the tutorial flag, which would have let a tutorial death into the
        // history. The caller now captures all of it itself.
        public readonly struct RunBuild
        {
            public RunBuild(bool isTutorialRun, IReadOnlyList<string> weaponIds, IReadOnlyList<int> weaponTiers,
                IReadOnlyList<string> towerIds, IReadOnlyList<int> towerLevels)
            {
                IsTutorialRun = isTutorialRun;
                WeaponIds = weaponIds;
                WeaponTiers = weaponTiers;
                TowerIds = towerIds;
                TowerLevels = towerLevels;
            }

            public bool IsTutorialRun { get; }

            public IReadOnlyList<string> WeaponIds { get; }

            public IReadOnlyList<int> WeaponTiers { get; }

            public IReadOnlyList<string> TowerIds { get; }

            public IReadOnlyList<int> TowerLevels { get; }
        }

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
            IReadOnlyList<string> relicIds,
            RunBuild build)
        {
            if (saveManager == null)
            {
                return;
            }

            // A guided run is not a performance - the same reason it earns no Alloy or SP.
            if (build.IsTutorialRun)
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

            entry.SetBuild(build.WeaponIds, build.WeaponTiers, cardIds, relicIds, build.TowerIds, build.TowerLevels);

            // MetaRewardLedger's per-run totals are the authority on what this run actually paid
            // out; the save's lifetime figures would report the profile's whole history instead.
            entry.SetEarnings(scrapEarned, MetaRewardLedger.RunAlloy, MetaRewardLedger.RunSkillPoints, enemiesKilled);

            var history = saveManager.LoadRunHistorySaveData() ?? new RunHistorySaveData();
            history.Add(entry);
            history.SortNewestFirst();
            saveManager.SaveRunHistorySaveData(history);

            // Playtime is no longer added here: PlaytimeTracker counts it continuously, because a
            // figure that only grew when a run was won or lost read 0:00 for anyone who quit to
            // the menu (playtest 2026-09-29).
        }
    }
}
