using System.Collections.Generic;
using Corebound.Gameplay.Combat;
using Corebound.Gameplay.Factory;
using Corebound.Gameplay.Towers;
using Corebound.MetaProgression;
using Corebound.SaveSystem;
using UnityEngine;

namespace Corebound.Gameplay.Waves
{
    // The Alloy quests that are judged over a stretch of play rather than at a single kill
    // (LEVELLING_QUESTS_PARAGON Part 2, A2): a stage cleared without the Core taking damage, a stage
    // cleared with one weapon type, Factory chain tier 4, and a tower upgraded to its top level.
    // Kills are counted in MetaRewardLedger.RecordEnemyKill, inside the write each kill already makes.
    //
    // Added to the WaveManager's object at runtime. It only listens; every payout goes through
    // QuestLedger, and nothing here runs in a tutorial run.
    public sealed class GameplayQuestTracker : MonoBehaviour
    {
        private WaveManager _waveManager;
        private PlayerCombatController _player;
        private FactoryService _factoryService;
        private bool _coreDamagedThisStage;
        private bool _mixedWeaponsThisStage;
        private readonly HashSet<string> _distinct = new HashSet<string>();

        public void Bind(WaveManager waveManager)
        {
            _waveManager = waveManager;
        }

        private void OnEnable()
        {
            StageEvents.StageStarted += OnStageStarted;
            StageEvents.StageCompleted += OnStageCompleted;
            WaveEvents.WaveCompleted += OnWaveCompleted;
            CombatEvents.DamageTaken += OnDamageTaken;
            TowerRuntimeController.ReachedMaxLevel += OnTowerReachedMaxLevel;
        }

        private void OnDisable()
        {
            StageEvents.StageStarted -= OnStageStarted;
            StageEvents.StageCompleted -= OnStageCompleted;
            WaveEvents.WaveCompleted -= OnWaveCompleted;
            CombatEvents.DamageTaken -= OnDamageTaken;
            TowerRuntimeController.ReachedMaxLevel -= OnTowerReachedMaxLevel;

            if (_player != null && _player.Loadout != null)
            {
                _player.Loadout.Changed -= OnLoadoutChanged;
            }
        }

        private void OnStageStarted(StageEventArgs eventArgs)
        {
            _coreDamagedThisStage = false;
            ResolvePlayer();
            _mixedWeaponsThisStage = CountDistinctWeapons() > 1;
        }

        // A Core hit of any size spoils "without the Core taking damage".
        private void OnDamageTaken(DamageEventArgs eventArgs)
        {
            if (eventArgs.Target != null && eventArgs.Target.name == CombatHitReceiver.CoreAnchorObjectName && eventArgs.DamageContext.Amount > 0f)
            {
                _coreDamagedThisStage = true;
            }
        }

        // Carrying two weapon types at any moment of the stage spoils "one weapon type", so buying
        // a second type and selling it before the boss does not count.
        private void OnLoadoutChanged()
        {
            if (CountDistinctWeapons() > 1)
            {
                _mixedWeaponsThisStage = true;
            }
        }

        private void OnWaveCompleted(WaveProgressEventArgs eventArgs)
        {
            // Checked at every wave, not only at the end of a stage, so a run that reaches tier 4 and
            // then dies still earns it ("reach chain tier 4 in a run").
            if (_factoryService == null)
            {
                _factoryService = FindFirstObjectByType<FactoryService>();
            }

            if (_factoryService != null && _factoryService.HighestChainTier >= QuestLedger.FactoryTierTarget)
            {
                WithSave((save, definition) => QuestLedger.RecordFactoryTier(save, definition, _factoryService.HighestChainTier));
            }
        }

        private void OnStageCompleted(StageEventArgs eventArgs)
        {
            var oneWeapon = !_mixedWeaponsThisStage && CountDistinctWeapons() == 1;
            var coreUntouched = !_coreDamagedThisStage;
            WithSave((save, definition) => QuestLedger.RecordStageCleared(save, definition, eventArgs.StageIndex, coreUntouched, oneWeapon));
        }

        private void OnTowerReachedMaxLevel(TowerData tower)
        {
            if (tower != null)
            {
                WithSave((save, definition) => QuestLedger.RecordTowerMaxed(save, definition, tower.TowerId));
            }
        }

        private void WithSave(System.Action<MetaProgressionSaveData, MetaProgressionDefinitionData> action)
        {
            var saveManager = _waveManager != null ? _waveManager.SaveManagerSource : null;
            var definition = _waveManager != null ? _waveManager.MetaProgressionDefinition : null;

            if (saveManager == null || definition == null)
            {
                return;
            }

            var runSaveData = saveManager.LoadRunSaveData();

            if (runSaveData != null && runSaveData.IsTutorialRun)
            {
                return;
            }

            var save = saveManager.LoadMetaProgressionSaveData(definition.CreateDefaultSaveData());
            action(save, definition);
            saveManager.SaveMetaProgressionSaveData(save);
        }

        private void ResolvePlayer()
        {
            if (_player != null)
            {
                return;
            }

            _player = FindFirstObjectByType<PlayerCombatController>();

            if (_player != null && _player.Loadout != null)
            {
                _player.Loadout.Changed += OnLoadoutChanged;
            }
        }

        private int CountDistinctWeapons()
        {
            ResolvePlayer();
            _distinct.Clear();
            var loadout = _player != null ? _player.Loadout : null;

            for (var index = 0; loadout != null && index < Corebound.Gameplay.Weapons.WeaponLoadoutRuntime.Capacity; index++)
            {
                var slot = loadout.GetSlot(index);

                if (slot != null && !slot.IsEmpty)
                {
                    _distinct.Add(slot.Weapon.WeaponId);
                }
            }

            return _distinct.Count;
        }
    }
}
