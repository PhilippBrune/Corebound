using System;
using System.Collections.Generic;
using Corebound.Gameplay.Weapons;
using Corebound.SaveSystem;
using Corebound.UI;

namespace Corebound.MetaProgression
{
    public enum QuestGroup
    {
        WeaponKills,
        WeaponArsenal,
        WeaponMastery,
        Collection,
        Bosses
    }

    public sealed class QuestDefinition
    {
        public QuestDefinition(string questId, QuestGroup group, int target, int rewardSkillPoints, WeaponData weapon, int stageIndex, string bossName)
        {
            QuestId = questId;
            Group = group;
            Target = target;
            RewardSkillPoints = rewardSkillPoints;
            Weapon = weapon;
            StageIndex = stageIndex;
            BossName = bossName;
        }

        public string QuestId { get; }

        public QuestGroup Group { get; }

        public int Target { get; }

        public int RewardSkillPoints { get; }

        public WeaponData Weapon { get; }

        public int StageIndex { get; }

        public string BossName { get; }

        public string Title
        {
            get
            {
                var weaponName = Weapon != null ? Weapon.DisplayName : string.Empty;

                switch (Group)
                {
                    case QuestGroup.WeaponKills:
                        return LocalizationService.Resolve("Defeat 1,000 enemies with " + weaponName, "Besiege 1.000 Gegner mit " + weaponName);
                    case QuestGroup.WeaponArsenal:
                        return LocalizationService.Resolve("Clear the game carrying six " + weaponName, "Schließe das Spiel mit sechs " + weaponName + " ab");
                    case QuestGroup.WeaponMastery:
                        return LocalizationService.Resolve("Master " + weaponName + ": every Alloy upgrade at its maximum", "Meistere " + weaponName + ": jede Legierungs-Verbesserung auf Maximum");
                    case QuestGroup.Collection:
                        return LocalizationService.Resolve("Collect every card", "Sammle jede Karte");
                    default:
                        return LocalizationService.Resolve("Defeat the Stage " + StageIndex + " boss", "Besiege den Boss von Stage " + StageIndex)
                            + (string.IsNullOrEmpty(BossName) ? string.Empty : " (" + BossName + ")");
                }
            }
        }
    }

    // The Skill Point quests (owner 2026-09-29, amending P4 of LEVELLING_QUESTS_PARAGON_2026-09-29):
    //
    //   9 x  defeat 1,000 enemies with a weapon                       1 SP each    9
    //   9 x  clear the game carrying six of one weapon                1 SP each    9
    //   9 x  master a weapon (every Alloy upgrade at maximum)         3 SP each   27
    //   1 x  collect every card                                       1 SP         1
    //   5 x  defeat each stage boss for the first time               1 SP each    5
    //   33 quests                                                                 51 SP
    //
    // With the 161 SP levels pay, that is exactly the tree's 212. Every completion goes through
    // Complete(), the one place a quest pays - the same chokepoint shape as MetaRewardLedger, so no
    // caller can pay twice or forget the toast.
    //
    // The catalogue is built from data, not authored: one set per weapon in the Upgrades catalogue
    // and one boss quest per stage, so the ids are stable and nothing needs hand-maintaining.
    public static class QuestLedger
    {
        public const int WeaponKillTarget = 1000;
        public const int WeaponKillReward = 1;
        public const int ArsenalReward = 1;
        public const int MasteryReward = 3;
        public const int CollectionReward = 1;
        public const int BossReward = 1;
        public const int ArsenalWeaponCount = 6;

        public const string CollectionQuestId = "cards_all";

        public static event Action<QuestDefinition> QuestCompleted;

        private static MetaProgressionDefinitionData _cachedFor;
        private static readonly List<QuestDefinition> Catalog = new List<QuestDefinition>();

        public static string KillQuestId(string weaponId) => "kills_" + weaponId;

        public static string ArsenalQuestId(string weaponId) => "arsenal_" + weaponId;

        public static string MasteryQuestId(string weaponId) => "mastery_" + weaponId;

        public static string BossQuestId(int stageIndex) => "boss_stage_" + stageIndex;

        public static IReadOnlyList<QuestDefinition> BuildCatalog(MetaProgressionDefinitionData definition)
        {
            if (definition == _cachedFor && Catalog.Count > 0)
            {
                return Catalog;
            }

            Catalog.Clear();
            _cachedFor = definition;

            if (definition == null)
            {
                return Catalog;
            }

            var weapons = definition.UpgradeWeapons != null ? definition.UpgradeWeapons.Weapons : null;

            // Grouped by kind rather than by weapon, which is how the Quests tab lists them.
            AddWeaponQuests(weapons, QuestGroup.WeaponKills, WeaponKillTarget, WeaponKillReward, KillQuestId);
            AddWeaponQuests(weapons, QuestGroup.WeaponArsenal, 1, ArsenalReward, ArsenalQuestId);
            AddWeaponQuests(weapons, QuestGroup.WeaponMastery, 1, MasteryReward, MasteryQuestId);

            var poolCount = CountPoolCards(definition);
            Catalog.Add(new QuestDefinition(CollectionQuestId, QuestGroup.Collection, Math.Max(1, poolCount), CollectionReward, null, 0, string.Empty));

            var stages = definition.StageProgression != null ? definition.StageProgression.Stages : null;

            for (var index = 0; stages != null && index < stages.Length; index++)
            {
                var stage = stages[index];
                var bossName = stage != null && stage.BossData != null ? stage.BossData.DisplayName : string.Empty;
                Catalog.Add(new QuestDefinition(BossQuestId(index + 1), QuestGroup.Bosses, 1, BossReward, null, index + 1, bossName));
            }

            return Catalog;
        }

        public static int TotalRewardSkillPoints(MetaProgressionDefinitionData definition)
        {
            var total = 0;
            var catalog = BuildCatalog(definition);

            for (var index = 0; index < catalog.Count; index++)
            {
                total += catalog[index].RewardSkillPoints;
            }

            return total;
        }

        // Progress for the Quests tab. Mastery and the card collection are read live from what
        // they measure; the others are counters kept in the save.
        public static int ResolveProgress(MetaProgressionSaveData save, MetaProgressionDefinitionData definition, QuestDefinition quest, int collectedCards)
        {
            if (save == null || quest == null)
            {
                return 0;
            }

            if (save.IsQuestCompleted(save.SelectedCharacterId, quest.QuestId))
            {
                return quest.Group == QuestGroup.WeaponMastery ? ResolveMasteryTarget(definition, quest) : quest.Target;
            }

            switch (quest.Group)
            {
                case QuestGroup.WeaponMastery:
                    UpgradeRules.ResolveWeaponMastery(save, definition != null ? definition.UpgradeCatalog : null, quest.Weapon, out var bought, out _);
                    return bought;
                case QuestGroup.Collection:
                    return Math.Min(collectedCards, quest.Target);
                default:
                    return Math.Min(save.GetQuestProgress(save.SelectedCharacterId, quest.QuestId), quest.Target);
            }
        }

        public static int ResolveMasteryTarget(MetaProgressionDefinitionData definition, QuestDefinition quest)
        {
            UpgradeRules.ResolveWeaponMastery(new MetaProgressionSaveData(), definition != null ? definition.UpgradeCatalog : null, quest.Weapon, out _, out var total);
            return Math.Max(1, total);
        }

        // ---- Events from play ---------------------------------------------------------------------

        // One kill by a player weapon. Returns true when the save changed (always, until the quest is
        // done) so the caller can fold the write into the one it already makes for XP.
        public static bool RecordWeaponKill(MetaProgressionSaveData save, MetaProgressionDefinitionData definition, string weaponId)
        {
            if (save == null || string.IsNullOrEmpty(weaponId))
            {
                return false;
            }

            var questId = KillQuestId(weaponId);
            var characterId = save.SelectedCharacterId;

            if (save.IsQuestCompleted(characterId, questId) || Find(definition, questId) == null)
            {
                return false;
            }

            var progress = save.GetQuestProgress(characterId, questId) + 1;
            save.SetQuestProgress(characterId, questId, progress, false);

            if (progress >= WeaponKillTarget)
            {
                Complete(save, definition, questId);
            }

            return true;
        }

        // The final stage was cleared. "Carrying six" means all six slots hold that weapon at the
        // moment the game is won - the Set Bonus loadout the quest is named after.
        public static bool RecordGameCleared(MetaProgressionSaveData save, MetaProgressionDefinitionData definition, IReadOnlyList<string> slotWeaponIds)
        {
            if (save == null || slotWeaponIds == null || slotWeaponIds.Count < ArsenalWeaponCount)
            {
                return false;
            }

            var first = slotWeaponIds[0];

            if (string.IsNullOrEmpty(first))
            {
                return false;
            }

            var sameCount = 0;

            for (var index = 0; index < slotWeaponIds.Count; index++)
            {
                if (slotWeaponIds[index] == first)
                {
                    sameCount++;
                }
            }

            return sameCount >= ArsenalWeaponCount && Complete(save, definition, ArsenalQuestId(first));
        }

        public static bool RecordBossFirstKill(MetaProgressionSaveData save, MetaProgressionDefinitionData definition, int stageIndex)
        {
            return save != null && Complete(save, definition, BossQuestId(stageIndex));
        }

        // Checked after every upgrade purchase and whenever the meta save loads in the menu, so a
        // profile that had already maxed a weapon before the quest existed is paid on first sight.
        public static bool EvaluateMastery(MetaProgressionSaveData save, MetaProgressionDefinitionData definition)
        {
            if (save == null || definition == null)
            {
                return false;
            }

            var changed = false;
            var catalog = BuildCatalog(definition);

            for (var index = 0; index < catalog.Count; index++)
            {
                var quest = catalog[index];

                if (quest.Group != QuestGroup.WeaponMastery || save.IsQuestCompleted(save.SelectedCharacterId, quest.QuestId))
                {
                    continue;
                }

                UpgradeRules.ResolveWeaponMastery(save, definition.UpgradeCatalog, quest.Weapon, out var bought, out var total);

                if (total > 0 && bought >= total)
                {
                    changed |= Complete(save, definition, quest.QuestId);
                }
            }

            return changed;
        }

        public static bool EvaluateCardCollection(MetaProgressionSaveData save, MetaProgressionDefinitionData definition, ISaveManager saveManager)
        {
            if (save == null || definition == null || definition.CardCollection == null
                || save.IsQuestCompleted(save.SelectedCharacterId, CollectionQuestId))
            {
                return false;
            }

            var collected = new HashSet<string>();
            CardCollectionRecord.ResolveCollectedIds(saveManager, collected);
            var cards = definition.CardCollection.Cards;
            var have = 0;
            var total = 0;

            for (var index = 0; cards != null && index < cards.Length; index++)
            {
                if (cards[index] == null)
                {
                    continue;
                }

                total++;

                if (collected.Contains(cards[index].CardId))
                {
                    have++;
                }
            }

            save.SetQuestProgress(save.SelectedCharacterId, CollectionQuestId, have, false);
            return total > 0 && have >= total ? Complete(save, definition, CollectionQuestId) : false;
        }

        // Once per profile: bosses killed before the quests existed already paid their first-kill
        // SP under the old rules, so their quests are marked done without paying again (concept,
        // "Existing profiles").
        public static bool MigrateLegacy(MetaProgressionSaveData save, MetaProgressionDefinitionData definition)
        {
            if (save == null || definition == null || save.QuestsMigrated)
            {
                return false;
            }

            var stages = definition.StageProgression != null ? definition.StageProgression.Stages : null;

            for (var index = 0; stages != null && index < stages.Length; index++)
            {
                var bossId = stages[index] != null && stages[index].BossData != null ? stages[index].BossData.BossId : null;

                if (!string.IsNullOrEmpty(bossId) && save.HasDefeatedBoss(bossId))
                {
                    save.SetQuestProgress(save.SelectedCharacterId, BossQuestId(index + 1), 1, true);
                }
            }

            save.MarkQuestsMigrated();
            return true;
        }

        // ---- Payout -----------------------------------------------------------------------------

        private static bool Complete(MetaProgressionSaveData save, MetaProgressionDefinitionData definition, string questId)
        {
            var characterId = save.SelectedCharacterId;
            var quest = Find(definition, questId);

            if (quest == null || save.IsQuestCompleted(characterId, questId))
            {
                return false;
            }

            save.SetQuestProgress(characterId, questId, quest.Target, true);
            save.GrantSkillPoints(quest.RewardSkillPoints);

            // A toast, never an overlay: a quest can finish mid-combat, where nothing may touch
            // Time.timeScale (P8-1).
            NotificationPresenter.Show(LocalizationService.Resolve(
                "Quest complete: " + quest.Title + "  +" + quest.RewardSkillPoints + " SP",
                "Quest abgeschlossen: " + quest.Title + "  +" + quest.RewardSkillPoints + " SP"));
            QuestCompleted?.Invoke(quest);
            return true;
        }

        private static QuestDefinition Find(MetaProgressionDefinitionData definition, string questId)
        {
            var catalog = BuildCatalog(definition);

            for (var index = 0; index < catalog.Count; index++)
            {
                if (catalog[index].QuestId == questId)
                {
                    return catalog[index];
                }
            }

            return null;
        }

        private static void AddWeaponQuests(WeaponData[] weapons, QuestGroup group, int target, int reward, Func<string, string> idFor)
        {
            for (var index = 0; weapons != null && index < weapons.Length; index++)
            {
                if (weapons[index] != null)
                {
                    Catalog.Add(new QuestDefinition(idFor(weapons[index].WeaponId), group, target, reward, weapons[index], 0, string.Empty));
                }
            }
        }

        private static int CountPoolCards(MetaProgressionDefinitionData definition)
        {
            var cards = definition.CardCollection != null ? definition.CardCollection.Cards : null;
            var count = 0;

            for (var index = 0; cards != null && index < cards.Length; index++)
            {
                if (cards[index] != null)
                {
                    count++;
                }
            }

            return count;
        }
    }
}
