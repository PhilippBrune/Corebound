using System;
using Corebound.SaveSystem;

namespace Corebound.MetaProgression
{
    public readonly struct MetaRewardGrant
    {
        public MetaRewardGrant(int skillPoints, int alloy, bool isFirstKill)
        {
            SkillPoints = skillPoints;
            Alloy = alloy;
            IsFirstKill = isFirstKill;
        }

        public int SkillPoints { get; }

        public int Alloy { get; }

        public bool IsFirstKill { get; }

        public bool IsEmpty => SkillPoints <= 0 && Alloy <= 0;
    }

    // The single place Skill Points and Alloy are paid (owner decision 2026-09-22: both come from
    // bosses and elites, SP from bosses only, on a curve that grows with stage depth).
    //
    // Written straight to the save at the moment of the kill rather than at run end, so a run that
    // beats the Stage 2 boss and then dies keeps what it earned - that is what makes going deeper
    // worth it even when the run fails. MetaProgressionService only exists in the MainMenu scene,
    // so this goes through the save manager exactly like the unlock checks do.
    public static class MetaRewardLedger
    {
        public static event Action<MetaRewardGrant> RewardGranted;

        // What the current run has earned so far, for the Run Failed screen (playtest 2026-09-22:
        // "add to Run Failed the earned resources"). Reset by WaveManager when a run's scene loads;
        // a resumed run therefore shows what it earned since the resume.
        public static int RunSkillPoints { get; private set; }

        public static int RunAlloy { get; private set; }

        public static void ResetRunTotals()
        {
            RunSkillPoints = 0;
            RunAlloy = 0;
        }

        private static void Record(MetaRewardGrant grant)
        {
            RunSkillPoints += grant.SkillPoints;
            RunAlloy += grant.Alloy;
            RewardGranted?.Invoke(grant);
        }

        // Owner 2026-09-22: "Tutorial should not give any Alloy or SP". The guided first run uses
        // the same systems as a real run, so the payouts have to be refused here rather than in
        // each caller.
        private static bool IsTutorialRun(ISaveManager saveManager)
        {
            var runSaveData = saveManager != null ? saveManager.LoadRunSaveData() : null;
            return runSaveData != null && runSaveData.IsTutorialRun;
        }

        public static MetaRewardGrant GrantBossDefeat(ISaveManager saveManager, MetaProgressionDefinitionData definition, int stageIndex, string bossId)
        {
            if (saveManager == null || definition == null || IsTutorialRun(saveManager))
            {
                return default;
            }

            var saveData = saveManager.LoadMetaProgressionSaveData(definition.CreateDefaultSaveData());
            var isFirstKill = saveData.RecordBossDefeated(bossId);
            var skillPoints = definition.ResolveBossSkillPoints(stageIndex) + (isFirstKill ? definition.BossFirstKillSkillPoints : 0);
            var alloy = definition.ResolveBossAlloy(stageIndex) + (isFirstKill ? definition.BossFirstKillAlloy : 0);
            saveData.GrantSkillPoints(skillPoints);
            saveData.GrantAlloy(alloy);
            saveManager.SaveMetaProgressionSaveData(saveData);

            var grant = new MetaRewardGrant(skillPoints, alloy, isFirstKill);
            Record(grant);
            return grant;
        }

        public static MetaRewardGrant GrantEliteKill(ISaveManager saveManager, MetaProgressionDefinitionData definition)
        {
            if (saveManager == null || definition == null || definition.EliteKillAlloy <= 0 || IsTutorialRun(saveManager))
            {
                return default;
            }

            var saveData = saveManager.LoadMetaProgressionSaveData(definition.CreateDefaultSaveData());
            saveData.GrantAlloy(definition.EliteKillAlloy);
            saveManager.SaveMetaProgressionSaveData(saveData);

            var grant = new MetaRewardGrant(0, definition.EliteKillAlloy, false);
            Record(grant);
            return grant;
        }

        // WeaponLoadout.md: weapon slot 2 unlocks permanently on the first stage cleared in any run.
        public static void RecordStageCleared(ISaveManager saveManager, MetaProgressionDefinitionData definition, bool completedFullRun)
        {
            if (saveManager == null)
            {
                return;
            }

            var saveData = saveManager.LoadMetaProgressionSaveData(definition != null ? definition.CreateDefaultSaveData() : new MetaProgressionSaveData());
            saveData.MarkStageCleared();

            if (completedFullRun)
            {
                saveData.MarkFullRunCompleted();
            }

            saveManager.SaveMetaProgressionSaveData(saveData);
        }
    }
}
