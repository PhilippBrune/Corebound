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

        // A weapon with no spread has nothing for Accuracy to narrow, so that track is not offered
        // for it. One rule, used by the Upgrades tab and the weapon-mastery quest alike: a quest that
        // counted a track the tab never shows could not be finished.
        public static bool IsTrackApplicable(UpgradeTrackData track, Corebound.Gameplay.Weapons.WeaponData weapon)
        {
            if (track == null || weapon == null || track.AppliesTo != UpgradeTargetKind.Weapon)
            {
                return false;
            }

            if (track.Stat == UpgradableStat.Accuracy)
            {
                return weapon.AttackPattern != null && weapon.AttackPattern.SpreadAngleDegrees > 0f;
            }

            return true;
        }

        // Ranks bought and ranks available across every track that applies to this weapon, for the
        // mastery quest's progress bar and completion check.
        public static void ResolveWeaponMastery(MetaProgressionSaveData save, UpgradeCatalogData catalog, Corebound.Gameplay.Weapons.WeaponData weapon, out int bought, out int total)
        {
            bought = 0;
            total = 0;

            if (save == null || catalog == null || catalog.Tracks == null || weapon == null)
            {
                return;
            }

            for (var index = 0; index < catalog.Tracks.Length; index++)
            {
                var track = catalog.Tracks[index];

                if (!IsTrackApplicable(track, weapon))
                {
                    continue;
                }

                total += track.MaxRank;
                bought += UnityEngine.Mathf.Min(track.MaxRank, save.GetUpgradeRank(weapon.WeaponId, (int)track.Stat));
            }
        }
    }
}
