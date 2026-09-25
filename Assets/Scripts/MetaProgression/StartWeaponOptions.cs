using System.Collections.Generic;
using Corebound.Gameplay.Weapons;
using Corebound.SaveSystem;

namespace Corebound.MetaProgression
{
    // B11 (spec review 2026-09-22): which weapons a new run may start with. A weapon is available
    // when nothing gates it, or when its licence has been bought. Shared by the New Run panel and
    // by PlayerCombatController, so the list the player picks from is exactly the list the run
    // honours.
    public static class StartWeaponOptions
    {
        // The weapon every profile starts with before anything is unlocked.
        public const string DefaultWeaponId = "weapon_rapid";

        public static bool IsAvailable(WeaponData weapon, MetaProgressionSaveData saveData, MetaProgressionDefinitionData definitionData)
        {
            if (weapon == null)
            {
                return false;
            }

            if (weapon.WeaponId == DefaultWeaponId)
            {
                return true;
            }

            var unlockId = ResolveUnlockId(weapon, definitionData);

            // Licensed weapon: the licence is the whole gate, buying it makes it a start option.
            if (!string.IsNullOrWhiteSpace(unlockId))
            {
                return saveData != null && saveData.HasPurchased(unlockId);
            }

            // Unlicensed weapon: it has to have been held in a run at least once (owner 2026-09-25,
            // "a new profile should have only one default weapon; when first picking up a weapon it
            // can be chosen"). Without this a fresh profile could start with any of the five
            // unlicensed weapons, which gave the first run nothing to discover.
            return saveData != null && saveData.HasDiscoveredWeapon(weapon.WeaponId);
        }

        public static void Collect(WeaponCatalogData catalog, MetaProgressionSaveData saveData, MetaProgressionDefinitionData definitionData, List<WeaponData> result)
        {
            result.Clear();

            if (catalog == null || catalog.Weapons == null)
            {
                return;
            }

            for (var index = 0; index < catalog.Weapons.Length; index++)
            {
                var weapon = catalog.Weapons[index];

                if (IsAvailable(weapon, saveData, definitionData))
                {
                    result.Add(weapon);
                }
            }
        }

        // A licence is the unlock whose reward is this weapon; the weapons added 2026-09-22 carry
        // no reward reference, so they are matched by the id convention instead
        // ("weapon_railgun" -> "unlock_weapon_railgun").
        private static string ResolveUnlockId(WeaponData weapon, MetaProgressionDefinitionData definitionData)
        {
            if (definitionData == null || definitionData.Unlocks == null)
            {
                return string.Empty;
            }

            var byConvention = "unlock_" + weapon.WeaponId;

            for (var index = 0; index < definitionData.Unlocks.Length; index++)
            {
                var unlock = definitionData.Unlocks[index];

                if (unlock == null)
                {
                    continue;
                }

                if (unlock.WeaponReward == weapon || unlock.UnlockId == byConvention)
                {
                    return unlock.UnlockId;
                }
            }

            return string.Empty;
        }
    }
}
