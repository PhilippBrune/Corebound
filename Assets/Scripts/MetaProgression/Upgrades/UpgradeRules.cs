using Corebound.SaveSystem;

namespace Corebound.MetaProgression
{
    public enum UpgradePurchaseResult
    {
        Purchased,
        MaxRank,
        Locked,
        InsufficientAlloy,
        Invalid
    }

    // Upgrades.md purchase rules. Permanent (owner decision 2026-09-22: no respec for upgrades),
    // so there is deliberately no refund path here at all.
    public static class UpgradeRules
    {
        public static UpgradePurchaseResult Evaluate(MetaProgressionSaveData save, UpgradeTrackData track, string itemId, bool isItemUnlocked)
        {
            if (save == null || track == null || string.IsNullOrWhiteSpace(itemId))
            {
                return UpgradePurchaseResult.Invalid;
            }

            // "Upgrading a locked weapon is not possible. Unlock first."
            if (!isItemUnlocked)
            {
                return UpgradePurchaseResult.Locked;
            }

            var rank = save.GetUpgradeRank(itemId, (int)track.Stat);

            if (rank >= track.MaxRank)
            {
                return UpgradePurchaseResult.MaxRank;
            }

            return save.AlloyBalance >= track.CostForRank(rank + 1) ? UpgradePurchaseResult.Purchased : UpgradePurchaseResult.InsufficientAlloy;
        }

        public static UpgradePurchaseResult TryPurchase(MetaProgressionSaveData save, UpgradeTrackData track, string itemId, bool isItemUnlocked)
        {
            var result = Evaluate(save, track, itemId, isItemUnlocked);

            if (result != UpgradePurchaseResult.Purchased)
            {
                return result;
            }

            var rank = save.GetUpgradeRank(itemId, (int)track.Stat);

            if (!save.TrySpendAlloy(track.CostForRank(rank + 1)))
            {
                return UpgradePurchaseResult.InsufficientAlloy;
            }

            save.SetUpgradeRank(itemId, (int)track.Stat, rank + 1);
            return UpgradePurchaseResult.Purchased;
        }
    }
}
