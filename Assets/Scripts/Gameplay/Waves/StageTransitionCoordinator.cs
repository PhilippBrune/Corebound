using System;
using Corebound.Gameplay.Resources;
using Corebound.Gameplay.Shared;
using Corebound.Gameplay.Towers;
using Corebound.MetaProgression;
using Corebound.SaveSystem;
using Corebound.UI;
using UnityEngine;

namespace Corebound.Gameplay.Waves
{
    // Open Topics 3.3, step 2 of 4 (2026-10-01): the world side of entering a stage, cut out of
    // WaveManager. WaveManager keeps the state machine - which stage and wave the run is on, its
    // timers and gates - and asks this to make the rest of the game agree with it.
    //
    // Two paths enter a stage. Resuming a saved run rebuilds instantly, because the scene was built
    // for its default stage before the save was read; advancing mid-run crossfades around a Core
    // that holds its place (Open Topics 3.2, option 3) and then checkpoints the save. Before this
    // both were written out inside WaveManager with small differences between them.
    //
    // Services are passed as lookups, not references: WaveManager resolves some of them in
    // OnEnable, after this object already exists.
    public sealed class StageTransitionCoordinator
    {
        private readonly Func<ISaveManager> _saveManager;
        private readonly Func<ResourceWalletService> _wallet;
        private readonly Func<TowerService> _towers;
        private readonly Func<RunPersistenceController> _persistence;

        public StageTransitionCoordinator(
            Func<ISaveManager> saveManager,
            Func<ResourceWalletService> wallet,
            Func<TowerService> towers,
            Func<RunPersistenceController> persistence)
        {
            _saveManager = saveManager;
            _wallet = wallet;
            _towers = towers;
            _persistence = persistence;
        }

        public static StageData ResolveNextStage(StageData current)
        {
            return current != null ? current.NextStageData : null;
        }

        // The arena and the tower slots, which both lay themselves out from the current stage's
        // theme and layout. Tower slots follow the arena: a stage's slots come from its layout's
        // buildable zones, so Stage 1's slots on Stage 3's floor would be wrong in both directions.
        public void RebuildWorld(bool crossfade)
        {
            UnityEngine.Object.FindFirstObjectByType<StageArenaArtPresenter>()?.RebuildForNewStage(crossfade);
            _towers()?.RebuildSlotsForNewStage();
        }

        // The "once per stage" skill rules (Second Wind, Last Bastion, Forward Foundations, Field
        // Repair) re-arm on the stage they are told about.
        public void BeginStage(int stageIndex)
        {
            RunProgressionSnapshot.Current.SetStage(stageIndex);
        }

        // Dividend / Supply Drop: Scrap at the start of every stage, paid once per stage and
        // remembered in the run save, so a resumed stage is not paid twice. Paid from Start on a
        // fresh scene, because by then the wallet has loaded the save and cannot overwrite it.
        public void PayStageStartBonus(int stageIndex)
        {
            var saveManager = _saveManager();
            var wallet = _wallet();

            if (saveManager == null || wallet == null)
            {
                return;
            }

            var runSaveData = saveManager.LoadRunSaveData();

            if (runSaveData == null || runSaveData.IsTutorialRun || runSaveData.StageStartBonusPaidStageIndex >= stageIndex)
            {
                return;
            }

            var amount = Mathf.RoundToInt(RunProgressionSnapshot.Current.Skills[SkillEffectTarget.StageStartScrap]);

            if (amount > 0)
            {
                wallet.AddFlat(amount);
                NotificationPresenter.Show("+" + amount + LocalizationService.Resolve(" Scrap (stage start)", " Schrott (Stagebeginn)"));
            }

            runSaveData = saveManager.LoadRunSaveData() ?? runSaveData;
            runSaveData.MarkStageStartBonusPaid(stageIndex);
            saveManager.SaveRunSaveData(runSaveData);
        }

        // After the first wave of the new stage has started, so the persistence controller already
        // holds the new stage and wave. Without it the save still described the previous stage's
        // last wave until a wave finished, and quitting in between resumed the player a whole stage
        // back (playtest 2026-09-28).
        public void CheckpointStageEntry()
        {
            _persistence()?.WriteStageEntrySnapshot();
        }
    }
}
