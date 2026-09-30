using System;
using System.Collections.Generic;
using Corebound.Gameplay.Cards;
using Corebound.Gameplay.Weapons;
using Corebound.SaveSystem;
using Corebound.UI;

namespace Corebound.MetaProgression
{
    // Serialized nowhere (quest ids are strings), so the order only decides how the Quests tab
    // groups them.
    public enum QuestGroup
    {
        // Skill Point quests (LEVELLING_QUESTS_PARAGON Part 1).
        WeaponKills,
        WeaponArsenal,
        WeaponMastery,
        Collection,
        Bosses,

        // Alloy quests (Part 2, owner 2026-09-29: A2 as proposed).
        TowerMastery,
        TowerKills,
        EnemyKills,
        EliteKills,
        Factory,
        RarityCollection,
        StageUntouchedCore,
        StageOneWeapon
    }

    public sealed class QuestDefinition
    {
        public QuestDefinition(string questId, QuestGroup group, int target, int rewardSkillPoints, int rewardAlloy,
            WeaponData weapon, int stageIndex, string subjectName, CardRarity rarity = CardRarity.Common)
        {
            QuestId = questId;
            Group = group;
            Target = target;
            RewardSkillPoints = rewardSkillPoints;
            RewardAlloy = rewardAlloy;
            Weapon = weapon;
            StageIndex = stageIndex;
            SubjectName = subjectName;
            Rarity = rarity;
        }

        public string QuestId { get; }

        public QuestGroup Group { get; }

        public int Target { get; }

        public int RewardSkillPoints { get; }

        public int RewardAlloy { get; }

        public bool PaysAlloy => RewardAlloy > 0;

        public WeaponData Weapon { get; }

        public int StageIndex { get; }

        // Boss, tower, enemy type or elite affix name, depending on the group.
        public string SubjectName { get; }

        public CardRarity Rarity { get; }

        public string RewardLabel => PaysAlloy
            ? "+" + RewardAlloy + LocalizationService.Resolve(" Alloy", " Legierung")
            : "+" + RewardSkillPoints + " SP";

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
                    case QuestGroup.Bosses:
                        return LocalizationService.Resolve("Defeat the Stage " + StageIndex + " boss", "Besiege den Boss von Stage " + StageIndex)
                            + (string.IsNullOrEmpty(SubjectName) ? string.Empty : " (" + SubjectName + ")");
                    case QuestGroup.TowerMastery:
                        return LocalizationService.Resolve("Upgrade a " + SubjectName + " to its highest level in one run", "Verbessere einen " + SubjectName + " in einem Run auf die höchste Stufe");
                    case QuestGroup.TowerKills:
                        return LocalizationService.Resolve("Defeat 300 enemies with the " + SubjectName, "Besiege 300 Gegner mit dem " + SubjectName);
                    case QuestGroup.EnemyKills:
                        return LocalizationService.Resolve("Defeat 1,000 " + SubjectName, "Besiege 1.000 " + SubjectName);
                    case QuestGroup.EliteKills:
                        return LocalizationService.Resolve("Defeat 50 " + SubjectName + "s", "Besiege 50 " + SubjectName);
                    case QuestGroup.Factory:
                        return LocalizationService.Resolve("Reach Factory chain tier 4 in a run", "Erreiche in einem Run Fabrik-Kettenstufe 4");
                    case QuestGroup.RarityCollection:
                        return LocalizationService.Resolve("Collect every " + RarityName(Rarity) + " card", "Sammle jede " + RarityName(Rarity) + " Karte");
                    case QuestGroup.StageUntouchedCore:
                        return LocalizationService.Resolve("Clear Stage " + StageIndex + " without the Core taking damage", "Schließe Stage " + StageIndex + " ab, ohne dass der Kern Schaden nimmt");
                    default:
                        return LocalizationService.Resolve("Clear Stage " + StageIndex + " using only one weapon type", "Schließe Stage " + StageIndex + " mit nur einer Waffenart ab");
                }
            }
        }

        public static string RarityName(CardRarity rarity)
        {
            switch (rarity)
            {
                case CardRarity.Rare:
                    return LocalizationService.Resolve("Rare", "seltene");
                case CardRarity.Epic:
                    return LocalizationService.Resolve("Epic", "epische");
                case CardRarity.Legendary:
                    return LocalizationService.Resolve("Legendary", "legendäre");
                default:
                    return LocalizationService.Resolve("Common", "gewöhnliche");
            }
        }
    }

    // The quests (LEVELLING_QUESTS_PARAGON_2026-09-29).
    //
    // Skill Point quests, 33 for 51 SP - with the 161 SP levels pay, exactly the tree's 212:
    //   9 x  defeat 1,000 enemies with a weapon                       1 SP each    9
    //   9 x  clear the game carrying six of one weapon                1 SP each    9
    //   9 x  master a weapon (every Alloy upgrade at maximum)         3 SP each   27
    //   1 x  collect every card                                       1 SP         1
    //   5 x  defeat each stage boss for the first time               1 SP each    5
    //
    // Alloy quests, 31 for 4,000 Alloy (A2), one-time (A3-1), shown on the same tab (A4-1) and
    // still paying Alloy past the level cap (A5-1):
    //   4 x  upgrade a tower to its highest level in one run        150           600
    //   4 x  defeat 300 enemies with a tower                        100           400
    //   5 x  defeat 1,000 of an enemy type                          150           750
    //   3 x  defeat 50 elites of an affix                           100           300
    //   1 x  reach Factory chain tier 4 in a run                    200           200
    //   4 x  collect every card of a rarity                         100-300       750
    //   5 x  clear a stage without Core damage                      100           500
    //   5 x  clear a stage with one weapon type                     100           500
    //
    // Every completion goes through Complete(), the one place a quest pays - the same chokepoint
    // shape as MetaRewardLedger, so no caller can pay twice or forget the toast. The catalogue is
    // built from data, not authored, so the ids are stable and nothing needs hand-maintaining.
    public static class QuestLedger
    {
        public const int WeaponKillTarget = 1000;
        public const int WeaponKillReward = 1;
        public const int ArsenalReward = 1;
        public const int MasteryReward = 3;
        public const int CollectionReward = 1;
        public const int BossReward = 1;
        public const int ArsenalWeaponCount = 6;

        public const int TowerMasteryAlloy = 150;
        public const int TowerKillTarget = 300;
        public const int TowerKillAlloy = 100;
        public const int EnemyKillTarget = 1000;
        public const int EnemyKillAlloy = 150;
        public const int EliteKillTarget = 50;
        public const int EliteKillAlloy = 100;
        public const int FactoryTierTarget = 4;
        public const int FactoryAlloy = 200;
        public const int StageQuestAlloy = 100;
        private static readonly int[] RarityCollectionAlloy = { 100, 150, 200, 300 };

        // K3-2 (2026-09-30): the account's level is shared between classes, and so are its quests -
        // a quest done as the Engineer is done. Progress stays keyed by a character id in the save
        // (P6-1 stored it per character), always this one, so nothing already earned moves.
        public const string QuestOwnerId = SaveSystem.MetaProgressionSaveData.DefaultCharacterId;

        public const string CollectionQuestId = "cards_all";
        public const string FactoryQuestId = "factory_tier4";

        public static event Action<QuestDefinition> QuestCompleted;

        private static MetaProgressionDefinitionData _cachedFor;
        private static readonly List<QuestDefinition> Catalog = new List<QuestDefinition>();

        public static string KillQuestId(string weaponId) => "kills_" + weaponId;

        public static string ArsenalQuestId(string weaponId) => "arsenal_" + weaponId;

        public static string MasteryQuestId(string weaponId) => "mastery_" + weaponId;

        public static string BossQuestId(int stageIndex) => "boss_stage_" + stageIndex;

        public static string TowerMasteryQuestId(string towerId) => "tower_max_" + towerId;

        public static string TowerKillQuestId(string towerId) => "tower_kills_" + towerId;

        public static string EnemyKillQuestId(string enemyId) => "enemy_kills_" + enemyId;

        public static string EliteKillQuestId(string affixKey) => "elite_kills_" + affixKey;

        public static string RarityQuestId(CardRarity rarity) => "cards_" + rarity.ToString().ToLowerInvariant();

        public static string UntouchedCoreQuestId(int stageIndex) => "stage_core_untouched_" + stageIndex;

        public static string OneWeaponQuestId(int stageIndex) => "stage_one_weapon_" + stageIndex;

        // Elite affixes have no id field; the asset name is stable and unique.
        public static string EliteAffixKey(UnityEngine.Object affix) => affix != null ? affix.name : string.Empty;

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

            Catalog.Add(new QuestDefinition(CollectionQuestId, QuestGroup.Collection, Math.Max(1, CountPoolCards(definition, null)), CollectionReward, 0, null, 0, string.Empty));

            var stages = definition.StageProgression != null ? definition.StageProgression.Stages : null;

            for (var index = 0; stages != null && index < stages.Length; index++)
            {
                var stage = stages[index];
                var bossName = stage != null && stage.BossData != null ? stage.BossData.DisplayName : string.Empty;
                Catalog.Add(new QuestDefinition(BossQuestId(index + 1), QuestGroup.Bosses, 1, BossReward, 0, null, index + 1, bossName));
            }

            // ---- Alloy quests ----
            var towers = definition.UpgradeTowers != null ? definition.UpgradeTowers.Towers : null;

            for (var index = 0; towers != null && index < towers.Length; index++)
            {
                if (towers[index] != null)
                {
                    Catalog.Add(new QuestDefinition(TowerMasteryQuestId(towers[index].TowerId), QuestGroup.TowerMastery, 1, 0, TowerMasteryAlloy, null, 0, towers[index].DisplayName));
                }
            }

            for (var index = 0; towers != null && index < towers.Length; index++)
            {
                if (towers[index] != null)
                {
                    Catalog.Add(new QuestDefinition(TowerKillQuestId(towers[index].TowerId), QuestGroup.TowerKills, TowerKillTarget, 0, TowerKillAlloy, null, 0, towers[index].DisplayName));
                }
            }

            var enemies = definition.QuestEnemies;

            for (var index = 0; enemies != null && index < enemies.Length; index++)
            {
                if (enemies[index] != null)
                {
                    Catalog.Add(new QuestDefinition(EnemyKillQuestId(enemies[index].EnemyId), QuestGroup.EnemyKills, EnemyKillTarget, 0, EnemyKillAlloy, null, 0, enemies[index].DisplayName));
                }
            }

            var affixes = definition.QuestEliteAffixes;

            for (var index = 0; affixes != null && index < affixes.Length; index++)
            {
                if (affixes[index] != null)
                {
                    Catalog.Add(new QuestDefinition(EliteKillQuestId(EliteAffixKey(affixes[index])), QuestGroup.EliteKills, EliteKillTarget, 0, EliteKillAlloy, null, 0, affixes[index].DisplayName));
                }
            }

            Catalog.Add(new QuestDefinition(FactoryQuestId, QuestGroup.Factory, FactoryTierTarget, 0, FactoryAlloy, null, 0, string.Empty));

            for (var rarity = 0; rarity < RarityCollectionAlloy.Length; rarity++)
            {
                var cardRarity = (CardRarity)rarity;
                Catalog.Add(new QuestDefinition(RarityQuestId(cardRarity), QuestGroup.RarityCollection, Math.Max(1, CountPoolCards(definition, cardRarity)), 0, RarityCollectionAlloy[rarity], null, 0, string.Empty, cardRarity));
            }

            for (var index = 0; stages != null && index < stages.Length; index++)
            {
                Catalog.Add(new QuestDefinition(UntouchedCoreQuestId(index + 1), QuestGroup.StageUntouchedCore, 1, 0, StageQuestAlloy, null, index + 1, string.Empty));
            }

            for (var index = 0; stages != null && index < stages.Length; index++)
            {
                Catalog.Add(new QuestDefinition(OneWeaponQuestId(index + 1), QuestGroup.StageOneWeapon, 1, 0, StageQuestAlloy, null, index + 1, string.Empty));
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

        // Progress for the Quests tab. Mastery and the card collections are read live from what
        // they measure; the others are counters kept in the save.
        public static int ResolveProgress(MetaProgressionSaveData save, MetaProgressionDefinitionData definition, QuestDefinition quest, ICollection<string> collectedCardIds)
        {
            if (save == null || quest == null)
            {
                return 0;
            }

            if (save.IsQuestCompleted(QuestOwnerId, quest.QuestId))
            {
                return quest.Group == QuestGroup.WeaponMastery ? ResolveMasteryTarget(definition, quest) : quest.Target;
            }

            switch (quest.Group)
            {
                case QuestGroup.WeaponMastery:
                    UpgradeRules.ResolveWeaponMastery(save, definition != null ? definition.UpgradeCatalog : null, quest.Weapon, out var bought, out _);
                    return bought;
                case QuestGroup.Collection:
                    return Math.Min(CountCollected(definition, collectedCardIds, null), quest.Target);
                case QuestGroup.RarityCollection:
                    return Math.Min(CountCollected(definition, collectedCardIds, quest.Rarity), quest.Target);
                default:
                    return Math.Min(save.GetQuestProgress(QuestOwnerId, quest.QuestId), quest.Target);
            }
        }

        public static int ResolveMasteryTarget(MetaProgressionDefinitionData definition, QuestDefinition quest)
        {
            UpgradeRules.ResolveWeaponMastery(new MetaProgressionSaveData(), definition != null ? definition.UpgradeCatalog : null, quest.Weapon, out _, out var total);
            return Math.Max(1, total);
        }

        // ---- Events from play ---------------------------------------------------------------------

        // One enemy kill: the weapon or tower that landed it, and what died. Folded into the one save
        // write the caller already makes for the kill's XP.
        public static void RecordKill(MetaProgressionSaveData save, MetaProgressionDefinitionData definition,
            string weaponId, string towerId, string enemyId, string eliteAffixKey)
        {
            if (save == null)
            {
                return;
            }

            if (!string.IsNullOrEmpty(weaponId))
            {
                Advance(save, definition, KillQuestId(weaponId), 1);
            }

            if (!string.IsNullOrEmpty(towerId))
            {
                Advance(save, definition, TowerKillQuestId(towerId), 1);
            }

            if (!string.IsNullOrEmpty(enemyId))
            {
                Advance(save, definition, EnemyKillQuestId(enemyId), 1);
            }

            if (!string.IsNullOrEmpty(eliteAffixKey))
            {
                Advance(save, definition, EliteKillQuestId(eliteAffixKey), 1);
            }
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

        public static bool RecordTowerMaxed(MetaProgressionSaveData save, MetaProgressionDefinitionData definition, string towerId)
        {
            return save != null && !string.IsNullOrEmpty(towerId) && Complete(save, definition, TowerMasteryQuestId(towerId));
        }

        public static bool RecordFactoryTier(MetaProgressionSaveData save, MetaProgressionDefinitionData definition, int tier)
        {
            if (save == null || save.IsQuestCompleted(QuestOwnerId, FactoryQuestId))
            {
                return false;
            }

            var best = Math.Max(save.GetQuestProgress(QuestOwnerId, FactoryQuestId), tier);
            save.SetQuestProgress(QuestOwnerId, FactoryQuestId, best, false);
            return best >= FactoryTierTarget && Complete(save, definition, FactoryQuestId);
        }

        public static void RecordStageCleared(MetaProgressionSaveData save, MetaProgressionDefinitionData definition, int stageIndex, bool coreUntouched, bool oneWeaponType)
        {
            if (save == null)
            {
                return;
            }

            if (coreUntouched)
            {
                Complete(save, definition, UntouchedCoreQuestId(stageIndex));
            }

            if (oneWeaponType)
            {
                Complete(save, definition, OneWeaponQuestId(stageIndex));
            }
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

                if (quest.Group != QuestGroup.WeaponMastery || save.IsQuestCompleted(QuestOwnerId, quest.QuestId))
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

        // The whole collection (SP) and each rarity (Alloy), from the same "ever collected" record
        // the Records card grid shows.
        public static bool EvaluateCardCollection(MetaProgressionSaveData save, MetaProgressionDefinitionData definition, ISaveManager saveManager)
        {
            if (save == null || definition == null || definition.CardCollection == null)
            {
                return false;
            }

            var collected = new HashSet<string>();
            CardCollectionRecord.ResolveCollectedIds(saveManager, collected);
            var changed = false;
            var catalog = BuildCatalog(definition);

            for (var index = 0; index < catalog.Count; index++)
            {
                var quest = catalog[index];

                if ((quest.Group != QuestGroup.Collection && quest.Group != QuestGroup.RarityCollection)
                    || save.IsQuestCompleted(QuestOwnerId, quest.QuestId))
                {
                    continue;
                }

                var have = CountCollected(definition, collected, quest.Group == QuestGroup.RarityCollection ? quest.Rarity : (CardRarity?)null);
                save.SetQuestProgress(QuestOwnerId, quest.QuestId, have, false);

                if (have >= quest.Target)
                {
                    changed |= Complete(save, definition, quest.QuestId);
                }
            }

            return changed;
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
                    save.SetQuestProgress(QuestOwnerId, BossQuestId(index + 1), 1, true);
                }
            }

            save.MarkQuestsMigrated();
            return true;
        }

        // ---- Payout -----------------------------------------------------------------------------

        private static void Advance(MetaProgressionSaveData save, MetaProgressionDefinitionData definition, string questId, int amount)
        {
            var characterId = QuestOwnerId;

            if (save.IsQuestCompleted(characterId, questId))
            {
                return;
            }

            var quest = Find(definition, questId);

            if (quest == null)
            {
                return;
            }

            var progress = save.GetQuestProgress(characterId, questId) + amount;
            save.SetQuestProgress(characterId, questId, progress, false);

            if (progress >= quest.Target)
            {
                Complete(save, definition, questId);
            }
        }

        private static bool Complete(MetaProgressionSaveData save, MetaProgressionDefinitionData definition, string questId)
        {
            var characterId = QuestOwnerId;
            var quest = Find(definition, questId);

            if (quest == null || save.IsQuestCompleted(characterId, questId))
            {
                return false;
            }

            save.SetQuestProgress(characterId, questId, quest.Target, true);

            if (quest.RewardSkillPoints > 0)
            {
                save.GrantSkillPoints(quest.RewardSkillPoints);
                MetaRewardLedger.NoteQuestSkillPoints(quest.RewardSkillPoints);
            }

            if (quest.RewardAlloy > 0)
            {
                save.GrantAlloy(quest.RewardAlloy);
                MetaRewardLedger.NoteQuestAlloy(quest.RewardAlloy);
            }

            // A toast, never an overlay: a quest can finish mid-combat, where nothing may touch
            // Time.timeScale (P8-1).
            NotificationPresenter.Show(LocalizationService.Resolve(
                "Quest complete: " + quest.Title + "  " + quest.RewardLabel,
                "Quest abgeschlossen: " + quest.Title + "  " + quest.RewardLabel));
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
                    Catalog.Add(new QuestDefinition(idFor(weapons[index].WeaponId), group, target, reward, 0, weapons[index], 0, string.Empty));
                }
            }
        }

        private static int CountPoolCards(MetaProgressionDefinitionData definition, CardRarity? rarity)
        {
            var cards = definition.CardCollection != null ? definition.CardCollection.Cards : null;
            var count = 0;

            for (var index = 0; cards != null && index < cards.Length; index++)
            {
                if (cards[index] != null && !cards[index].IsClassCard && (!rarity.HasValue || cards[index].Rarity == rarity.Value))
                {
                    count++;
                }
            }

            return count;
        }

        private static int CountCollected(MetaProgressionDefinitionData definition, ICollection<string> collectedCardIds, CardRarity? rarity)
        {
            var cards = definition != null && definition.CardCollection != null ? definition.CardCollection.Cards : null;
            var count = 0;

            for (var index = 0; cards != null && collectedCardIds != null && index < cards.Length; index++)
            {
                if (cards[index] != null && !cards[index].IsClassCard && (!rarity.HasValue || cards[index].Rarity == rarity.Value) && collectedCardIds.Contains(cards[index].CardId))
                {
                    count++;
                }
            }

            return count;
        }
    }
}
