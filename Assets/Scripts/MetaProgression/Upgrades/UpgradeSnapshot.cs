using System.Collections.Generic;
using Corebound.SaveSystem;

namespace Corebound.MetaProgression
{
    // Upgrades.md Runtime: "a plain class with no Unity dependencies, so it is testable", resolved
    // once at run start and read by PlayerCombatController and the tower code - never the save.
    //
    // Every value is the track's per-rank value times the purchased rank, already clamped to the
    // track's max rank, so "no stat may exceed its ceiling from upgrades alone" holds by
    // construction rather than by a check somewhere downstream.
    public sealed class UpgradeSnapshot
    {
        public static readonly UpgradeSnapshot Empty = new UpgradeSnapshot();

        private readonly Dictionary<string, float> _values = new Dictionary<string, float>();

        public static UpgradeSnapshot Build(MetaProgressionSaveData saveData, UpgradeCatalogData catalog)
        {
            var snapshot = new UpgradeSnapshot();

            if (saveData == null || catalog == null || saveData.UpgradeRanks == null)
            {
                return snapshot;
            }

            var ranks = saveData.UpgradeRanks;

            for (var index = 0; index < ranks.Count; index++)
            {
                var entry = ranks[index];

                if (string.IsNullOrWhiteSpace(entry.ItemId) || entry.Rank <= 0)
                {
                    continue;
                }

                // Weapon and tower ids never collide (weapon_* / tower_*), so the kind is implied by
                // the item; resolving both kinds keeps the save format free of a kind field.
                var stat = (UpgradableStat)entry.Stat;
                var track = catalog.FindTrack(UpgradeTargetKind.Weapon, stat);

                if (track == null || !entry.ItemId.StartsWith("weapon", System.StringComparison.Ordinal))
                {
                    track = catalog.FindTrack(UpgradeTargetKind.Tower, stat);
                }

                if (track == null)
                {
                    continue;
                }

                snapshot._values[Key(entry.ItemId, stat)] = track.TotalValueAtRank(entry.Rank);
            }

            return snapshot;
        }

        public float Get(string itemId, UpgradableStat stat)
        {
            return itemId != null && _values.TryGetValue(Key(itemId, stat), out var value) ? value : 0f;
        }

        // 1 + value: Damage (+6%/rank), Range (+4%/rank) and Critical Damage (+8%/rank).
        public float GetIncreaseMultiplier(string itemId, UpgradableStat stat)
        {
            return 1f + Get(itemId, stat);
        }

        // 1 + value with a negative value: Fire Speed (-4%/rank on the interval), Reload Speed,
        // Accuracy (spread) and Build Discount. Floored so no data mistake can reach zero.
        public float GetReductionMultiplier(string itemId, UpgradableStat stat)
        {
            var multiplier = 1f + Get(itemId, stat);
            return multiplier < 0.2f ? 0.2f : multiplier;
        }

        private static string Key(string itemId, UpgradableStat stat)
        {
            return itemId + "#" + (int)stat;
        }
    }
}
