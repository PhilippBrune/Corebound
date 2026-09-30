using Corebound.MetaProgression;
using Corebound.SaveSystem;

namespace Corebound.Story
{
    // Whether a Core Log is readable yet. Every condition is something the save already tracks, so
    // the Logs cost no new bookkeeping and cannot disagree with the Records and Quests screens.
    public static class CoreLogRules
    {
        public static bool IsUnlocked(CoreLogEntry entry, MetaProgressionSaveData meta, StatisticsSaveData statistics, MetaProgressionDefinitionData definition)
        {
            if (entry == null)
            {
                return false;
            }

            var value = entry.UnlockValue;
            var owner = MetaProgressionSaveData.DefaultCharacterId;

            switch (entry.Unlock)
            {
                case CoreLogUnlock.Always:
                    return true;
                case CoreLogUnlock.RunsStarted:
                    return statistics != null && statistics.TotalRunsStarted >= value;
                case CoreLogUnlock.BossDefeated:
                    return meta != null && meta.HasDefeatedBoss("boss_stage" + value);
                case CoreLogUnlock.Level:
                    return meta != null && (meta.Level >= value || meta.IsAtLevelCap);
                case CoreLogUnlock.TowerMastered:
                    return meta != null && AnyTowerMastered(meta, definition, owner);
                case CoreLogUnlock.FactoryTier:
                    return meta != null && (meta.GetQuestProgress(owner, QuestLedger.FactoryQuestId) >= value
                        || meta.IsQuestCompleted(owner, QuestLedger.FactoryQuestId));
                case CoreLogUnlock.EliteKills:
                    return meta != null && CountEliteKills(meta, definition, owner) >= value;
                case CoreLogUnlock.FullClear:
                    return meta != null && meta.HasCompletedFullRun;
                case CoreLogUnlock.ParagonLevel:
                    return meta != null && meta.ParagonLevel >= value;
                default:
                    return false;
            }
        }

        private static bool AnyTowerMastered(MetaProgressionSaveData meta, MetaProgressionDefinitionData definition, string owner)
        {
            var catalog = definition != null ? definition.UpgradeTowers : null;

            if (catalog == null || catalog.Towers == null)
            {
                return false;
            }

            foreach (var tower in catalog.Towers)
            {
                if (tower != null && meta.IsQuestCompleted(owner, QuestLedger.TowerMasteryQuestId(tower.TowerId)))
                {
                    return true;
                }
            }

            return false;
        }

        private static int CountEliteKills(MetaProgressionSaveData meta, MetaProgressionDefinitionData definition, string owner)
        {
            var affixes = definition != null ? definition.QuestEliteAffixes : null;
            var total = 0;

            if (affixes == null)
            {
                return 0;
            }

            foreach (var affix in affixes)
            {
                var questId = QuestLedger.EliteKillQuestId(QuestLedger.EliteAffixKey(affix));
                total += meta.IsQuestCompleted(owner, questId) ? QuestLedger.EliteKillTarget : meta.GetQuestProgress(owner, questId);
            }

            return total;
        }
    }
}
