using Corebound.SaveSystem;

namespace Corebound.MetaProgression
{
    // K4-1: a class unlocks for free once the player has already tried the thing it specialises
    // in. Every condition is read from quests the ledger already tracks, account-wide, so any
    // class's run can unlock any other class.
    public static class CharacterUnlockRules
    {
#if UNITY_EDITOR
        // The autopilot's class variant plays a class the test profile has not unlocked yet.
        public static bool DebugUnlockAll;
#endif

        public static bool IsUnlocked(CharacterData character, MetaProgressionSaveData save, MetaProgressionDefinitionData definition)
        {
            if (character == null)
            {
                return false;
            }

#if UNITY_EDITOR
            if (DebugUnlockAll)
            {
                return true;
            }
#endif

            if (character.Unlock == CharacterUnlock.Start)
            {
                return true;
            }

            if (save == null)
            {
                return false;
            }

            var owner = QuestLedger.QuestOwnerId;

            switch (character.Unlock)
            {
                case CharacterUnlock.TowerMastered:
                    var towers = definition != null && definition.UpgradeTowers != null ? definition.UpgradeTowers.Towers : null;

                    if (towers != null)
                    {
                        foreach (var tower in towers)
                        {
                            if (tower != null && save.IsQuestCompleted(owner, QuestLedger.TowerMasteryQuestId(tower.TowerId)))
                            {
                                return true;
                            }
                        }
                    }

                    return false;
                case CharacterUnlock.FactoryTier:
                    return save.IsQuestCompleted(owner, QuestLedger.FactoryQuestId)
                        || save.GetQuestProgress(owner, QuestLedger.FactoryQuestId) >= character.UnlockValue;
                case CharacterUnlock.UntouchedCoreStage:
                    for (var stage = 1; stage <= 5; stage++)
                    {
                        if (save.IsQuestCompleted(owner, QuestLedger.UntouchedCoreQuestId(stage)))
                        {
                            return true;
                        }
                    }

                    return false;
                case CharacterUnlock.BossDefeated:
                    return save.HasDefeatedBoss("boss_stage" + character.UnlockValue);
                default:
                    return false;
            }
        }
    }
}
