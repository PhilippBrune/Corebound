using System;
using System.Collections.Generic;
using UnityEngine;
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

    // The single place XP and Alloy are paid during a run. Alloy comes from bosses and elites
    // (owner decision 2026-09-22). Skill Points come only from levels and quests since 2026-09-29
    // (P3-2): bosses pay an XP lump instead, and a boss's first kill completes its quest.
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
        //
        // Playtest 2026-09-29: "I died in stage one at the boss and got no EXP." The waves had paid
        // XP, but the screen only counted Skill Points and Alloy, found both zero and said "Earned
        // this run: nothing". XP, levels and the SP that levels and quests pay are counted here too.
        public static int RunSkillPoints { get; private set; }

        public static int RunAlloy { get; private set; }

        public static int RunExperience { get; private set; }

        public static int RunLevelsGained { get; private set; }

        public static void ResetRunTotals()
        {
            RunSkillPoints = 0;
            RunAlloy = 0;
            RunExperience = 0;
            RunLevelsGained = 0;
        }

        // What a quest paid while a run was going on (QuestLedger calls these on completion).
        public static void NoteQuestSkillPoints(int skillPoints)
        {
            RunSkillPoints += Mathf.Max(0, skillPoints);
        }

        public static void NoteQuestAlloy(int alloy)
        {
            RunAlloy += Mathf.Max(0, alloy);
        }

        private static void Record(MetaRewardGrant grant)
        {
            RunSkillPoints += grant.SkillPoints;
            RunAlloy += grant.Alloy;
            RewardGranted?.Invoke(grant);
        }

        // Playtest 2026-09-27: "after the very first time earning Alloy or SP the game should show
        // the player how to spend them." Both grant paths funnel through here, so the check lives
        // here rather than being repeated in each caller - and it fires at the moment of the kill,
        // where the player can connect the number to what they just did, instead of on some later
        // screen where it reads as a stray tip.
        //
        // Fires once per profile: the flag is saved with the same write that banks the currency, so
        // a run that earns Alloy and then dies still counts as having seen it.
        private static void TryShowMetaCurrencyHint(ISaveManager saveManager, MetaProgressionSaveData saveData, MetaRewardGrant grant)
        {
            if (saveData == null || saveData.HasSeenMetaCurrencyHint || grant.IsEmpty)
            {
                return;
            }

            saveData.MarkMetaCurrencyHintSeen();

            var what = grant.SkillPoints > 0 && grant.Alloy > 0
                ? UI.LocalizationService.Resolve("Skill Points and Alloy", "Skillpunkte und Legierung")
                : (grant.SkillPoints > 0
                    ? UI.LocalizationService.Resolve("Skill Points", "Skillpunkte")
                    : UI.LocalizationService.Resolve("Alloy", "Legierung"));

            UI.NotificationPresenter.Show(UI.LocalizationService.Resolve(
                "First " + what + " earned - spend them in Progression, from the Main Menu. They are kept when a run ends.",
                "Erste " + what + " verdient - ausgeben im Fortschritt-Menü im Hauptmenü. Sie bleiben erhalten, wenn ein Run endet."));
        }

        // Owner 2026-09-22: "Tutorial should not give any Alloy or SP". The guided first run uses
        // the same systems as a real run, so the payouts have to be refused here rather than in
        // each caller.
        private static bool IsTutorialRun(ISaveManager saveManager)
        {
            var runSaveData = saveManager != null ? saveManager.LoadRunSaveData() : null;
            return runSaveData != null && runSaveData.IsTutorialRun;
        }

        // L1 (owner 2026-09-28): weighted kills, wave clears and stage clears, not kills alone.
        // Kills alone reward grinding a wave already beaten and punish a build that lets towers do
        // the work; weighting the clears makes XP track progress, which is what a level is supposed
        // to represent, while kills keep the bar moving so it never looks stuck.
        //
        // Retuned 2026-09-29 ("I want to get EXP per wave, and the boss gives extra"). A wave paid
        // a flat 18, so a Stage 1 run that died at the boss earned ~100 XP against a 400 XP first
        // level - nothing visible. A wave now pays 25 per stage of depth, and the boss is the lump:
        // 250 per stage, which replaces the old separate stage-clear payment (a stage is only ever
        // cleared by killing its boss). A Stage 1 death at the boss now crosses the first level.
        public const int ExperiencePerKill = 2;
        public const int ExperiencePerWavePerStage = 25;
        public const int BossExperiencePerStage = 250;

        public static int ResolveWaveExperience(int stageIndex)
        {
            return ExperiencePerWavePerStage * Mathf.Max(1, stageIndex);
        }

        // Fired once per level crossed, with the Skill Points that level paid (2, or 5 on a tenth
        // level), so the HUD can toast without the save layer knowing about UI.
        public static event Action<int, int> LevelGained;

        // Fired once per Paragon level crossed past the cap; each pays one Ascension Point.
        public static event Action<int> ParagonLevelGained;

        public static void GrantExperience(ISaveManager saveManager, MetaProgressionDefinitionData definition, int amount)
        {
            if (saveManager == null || definition == null || amount <= 0 || IsTutorialRun(saveManager))
            {
                return;
            }

            var saveData = saveManager.LoadMetaProgressionSaveData(definition.CreateDefaultSaveData());

            if (saveData == null)
            {
                return;
            }

            var result = saveData.GrantExperience(amount);
            saveManager.SaveMetaProgressionSaveData(saveData);
            AnnounceExperience(saveData, result, amount);
        }

        // One enemy kill: its XP, and the kill quests it advances - the weapon or tower that landed
        // the blow, the enemy type and the elite affix. One load and one write for all of it; this
        // runs on every kill of every wave.
        public static void RecordEnemyKill(ISaveManager saveManager, MetaProgressionDefinitionData definition,
            string weaponId, string towerId = null, string enemyId = null, string eliteAffixKey = null)
        {
            if (saveManager == null || definition == null || IsTutorialRun(saveManager))
            {
                return;
            }

            var saveData = saveManager.LoadMetaProgressionSaveData(definition.CreateDefaultSaveData());

            if (saveData == null)
            {
                return;
            }

            var result = saveData.GrantExperience(ExperiencePerKill);
            QuestLedger.RecordKill(saveData, definition, weaponId, towerId, enemyId, eliteAffixKey);
            saveManager.SaveMetaProgressionSaveData(saveData);
            AnnounceExperience(saveData, result, ExperiencePerKill);
        }

        private static void AnnounceExperience(MetaProgressionSaveData saveData, MetaProgressionSaveData.ExperienceResult result, int amount)
        {
            RunExperience += Mathf.Max(0, amount);
            RunLevelsGained += result.LevelsGained;
            RunSkillPoints += result.SkillPointsGranted;

            for (var index = 0; index < result.LevelsGained; index++)
            {
                var level = saveData.Level - result.LevelsGained + index + 1;
                LevelGained?.Invoke(level, MetaProgressionSaveData.ResolveSkillPointsForLevel(level));
            }

            for (var index = 0; index < result.ParagonLevelsGained; index++)
            {
                ParagonLevelGained?.Invoke(saveData.ParagonLevel - result.ParagonLevelsGained + index + 1);
            }
        }

        public static MetaRewardGrant GrantBossDefeat(ISaveManager saveManager, MetaProgressionDefinitionData definition, int stageIndex, string bossId)
        {
            if (saveManager == null || definition == null || IsTutorialRun(saveManager))
            {
                return default;
            }

            var saveData = saveManager.LoadMetaProgressionSaveData(definition.CreateDefaultSaveData());
            var isFirstKill = saveData.RecordBossDefeated(bossId);
            var alloy = definition.ResolveBossAlloy(stageIndex) + (isFirstKill ? definition.BossFirstKillAlloy : 0);
            saveData.GrantAlloy(alloy);

            // P3-2: no SP from the boss itself. The first kill completes "Defeat the Stage N boss",
            // which pays its SP, counts it for the run and toasts on its own; every kill pays the
            // XP lump. The grant therefore carries Alloy only, so nothing is counted twice.
            if (isFirstKill)
            {
                QuestLedger.RecordBossFirstKill(saveData, definition, stageIndex);
            }

            var bossExperience = BossExperiencePerStage * Mathf.Max(1, stageIndex);
            var experience = saveData.GrantExperience(bossExperience);
            var grant = new MetaRewardGrant(0, alloy, isFirstKill);
            TryShowMetaCurrencyHint(saveManager, saveData, grant);
            saveManager.SaveMetaProgressionSaveData(saveData);
            AnnounceExperience(saveData, experience, bossExperience);
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

            var grant = new MetaRewardGrant(0, definition.EliteKillAlloy, false);
            TryShowMetaCurrencyHint(saveManager, saveData, grant);
            saveManager.SaveMetaProgressionSaveData(saveData);
            Record(grant);
            return grant;
        }

        // WeaponLoadout.md: weapon slot 2 unlocks permanently on the first stage cleared in any run.
        // slotWeaponIds is the loadout at the moment of clearing, for the "clear the game carrying
        // six of one weapon" quests; only read when the run is complete.
        public static void RecordStageCleared(ISaveManager saveManager, MetaProgressionDefinitionData definition, bool completedFullRun, IReadOnlyList<string> slotWeaponIds = null)
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

                if (!IsTutorialRun(saveManager))
                {
                    QuestLedger.RecordGameCleared(saveData, definition, slotWeaponIds);
                }
            }

            saveManager.SaveMetaProgressionSaveData(saveData);
        }
    }
}
