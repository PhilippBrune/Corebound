using System.Collections.Generic;

namespace Corebound.SaveSystem
{
    // Which card ids this profile has ever taken into a run. Shared by the Records card grid and the
    // "collect every card" quest, so the two can never disagree about what counts as collected.
    //
    // The union of three sources: the per-profile record kept since 2026-09-29, the cards of every
    // run in the history, and the run in progress. The last two cover everything collected before
    // the record existed; without them every existing profile would start the quest from zero.
    public static class CardCollectionRecord
    {
        public static void ResolveCollectedIds(ISaveManager saveManager, HashSet<string> into)
        {
            if (into == null)
            {
                return;
            }

            into.Clear();

            if (saveManager == null)
            {
                return;
            }

            var statistics = saveManager.LoadStatisticsSaveData(new StatisticsSaveData());
            AddAll(into, statistics != null ? statistics.CollectedCardIds : null);

            var history = saveManager.LoadRunHistorySaveData();

            if (history != null && history.Entries != null)
            {
                for (var index = 0; index < history.Entries.Count; index++)
                {
                    AddAll(into, history.Entries[index] != null ? history.Entries[index].CardIds : null);
                }
            }

            var runSaveData = saveManager.LoadRunSaveData();

            if (runSaveData != null && runSaveData.HasActiveRun)
            {
                AddAll(into, runSaveData.SelectedCardIds);
            }
        }

        private static void AddAll(HashSet<string> set, IReadOnlyList<string> ids)
        {
            if (ids == null)
            {
                return;
            }

            for (var index = 0; index < ids.Count; index++)
            {
                if (!string.IsNullOrEmpty(ids[index]))
                {
                    set.Add(ids[index]);
                }
            }
        }
    }
}
