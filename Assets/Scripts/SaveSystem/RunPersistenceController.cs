using Corebound.Gameplay.Combat;
using Corebound.Gameplay.Resources;
using Corebound.Gameplay.Bosses;
using Corebound.Gameplay.Waves;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Corebound.SaveSystem
{
    public sealed class RunPersistenceController : MonoBehaviour
    {
        [SerializeField] private UnityEngine.Object _saveManagerSource;
        [SerializeField] private PlayerCombatController _playerCombatController;
        [SerializeField] private ResourceWalletService _resourceWalletService;
        [SerializeField] private RunProgressionCheckpointData _checkpointData;
        [SerializeField] private CombatHitReceiver _coreHitReceiver;

        private int _currentStageIndex = 1;
        private int _currentWaveIndex = 1;
        private float _currentCoreHealthNormalized = 1f;
        private bool _isBossEncounterActive;
        private int _currentBossPhaseIndex;

        private ISaveManager SaveManager => SaveManagerResolver.Resolve(_saveManagerSource);

        private CombatHitReceiver ResolvedCoreHitReceiver
        {
            get
            {
                if (_coreHitReceiver == null)
                {
                    var coreObject = GameObject.Find(CombatHitReceiver.CoreAnchorObjectName);
                    _coreHitReceiver = coreObject != null ? coreObject.GetComponent<CombatHitReceiver>() : null;
                }

                return _coreHitReceiver;
            }
        }

        public void SetCoreHitReceiver(CombatHitReceiver coreHitReceiver)
        {
            _coreHitReceiver = coreHitReceiver;
        }

        private void OnEnable()
        {
            CombatEvents.DamageTaken += OnDamageTaken;
            CombatEvents.CombatantDied += OnCombatantDied;

            if (!InitializeContextFromSave())
            {
                WriteCombatSnapshot();
            }
        }

        private void OnDisable()
        {
            CombatEvents.DamageTaken -= OnDamageTaken;
            CombatEvents.CombatantDied -= OnCombatantDied;
        }

        private void OnDamageTaken(DamageEventArgs eventArgs)
        {
            if (_playerCombatController != null && eventArgs.Target == _playerCombatController.gameObject)
            {
                WriteCombatSnapshot();
                return;
            }

            if (ResolvedCoreHitReceiver == null || eventArgs.Target != ResolvedCoreHitReceiver.gameObject)
            {
                return;
            }

            WriteCombatSnapshot();
        }

        private void OnCombatantDied(DeathEventArgs eventArgs)
        {
            if (SaveManager == null)
            {
                return;
            }

            if ((_playerCombatController != null && eventArgs.Victim == _playerCombatController.gameObject)
                || (ResolvedCoreHitReceiver != null && eventArgs.Victim == ResolvedCoreHitReceiver.gameObject))
            {
                SaveManager.ClearRunSaveData();
            }
        }

        public void ApplyWaveContext(int stageIndex, int waveIndex, bool isBossEncounterActive = false, int bossPhaseIndex = 0)
        {
            _currentStageIndex = Mathf.Max(1, stageIndex);
            _currentWaveIndex = Mathf.Max(1, waveIndex);
            SyncCoreHealthFromRuntime();
            _isBossEncounterActive = isBossEncounterActive;
            _currentBossPhaseIndex = Mathf.Max(0, bossPhaseIndex);
        }

        // Writes where the run currently *is*, without waiting for a wave to finish.
        //
        // Playtest 2026-09-28: "going to the main menu and continuing brought me back from Stage 2
        // to Stage 1 wave 6/6." The run was only ever checkpointed at wave completion, boss
        // completion and intermission, so advancing to a new stage wrote nothing - quit before
        // finishing a wave on the new stage and the newest checkpoint on disk was still the last
        // wave of the previous one. Which is exactly Stage 1, wave 6 of 6.
        public void WriteStageEntrySnapshot()
        {
            var runSaveData = SaveManager != null ? SaveManager.LoadRunSaveData() : null;

            if (runSaveData == null || _playerCombatController == null)
            {
                return;
            }

            SyncCoreHealthFromRuntime();
            runSaveData.UpdateCombatCheckpoint(
                SceneManager.GetActiveScene().name,
                _currentStageIndex,
                _currentWaveIndex,
                _playerCombatController.CurrentHealthNormalized,
                _currentCoreHealthNormalized,
                _resourceWalletService != null ? _resourceWalletService.CurrentAmount : 0,
                _playerCombatController.ActiveWeaponId);

            // A new stage starts in combat, never mid-intermission, and carries no pending reward -
            // leaving either set would resume the player into a screen the stage has not reached.
            runSaveData.SetIntermissionOpen(false, _currentStageIndex, _currentWaveIndex);
            runSaveData.SetPendingReward(string.Empty, string.Empty);
            runSaveData.SetBossEncounterState(false, 0);
            SaveManager.SaveRunSaveData(runSaveData);
        }

        public void WriteWaveCompletionSnapshot(int completedWaveIndex, WaveRewardData waveRewardData)
        {
            if (SaveManager == null || _playerCombatController == null)
            {
                return;
            }

            SyncCoreHealthFromRuntime();

            var runSaveData = SaveManager.LoadRunSaveData() ?? new RunSaveData();
            var nextWaveIndex = completedWaveIndex + 1;
            var nextWalletAmount = _resourceWalletService != null ? _resourceWalletService.CurrentAmount : runSaveData.WalletAmount;

            if (_resourceWalletService != null && waveRewardData != null)
            {
                _resourceWalletService.AddReward(waveRewardData.CompletionResourceBonus);
                nextWalletAmount = _resourceWalletService.CurrentAmount;
            }

            runSaveData.UpdateCombatCheckpoint(
                SceneManager.GetActiveScene().name,
                _currentStageIndex,
                nextWaveIndex,
                _playerCombatController.CurrentHealthNormalized,
                _currentCoreHealthNormalized,
                nextWalletAmount,
                _playerCombatController.ActiveWeaponId);

            if (waveRewardData != null)
            {
                runSaveData.SetPendingReward(waveRewardData.RewardId, waveRewardData.RewardLabel);
            }

            runSaveData.SetBossEncounterState(false, 0);
            runSaveData.SetIntermissionOpen(true, _currentStageIndex, nextWaveIndex);
            SaveManager.SaveRunSaveData(runSaveData);
        }

        public void WriteBossCompletionSnapshot(BossRewardData bossRewardData)
        {
            if (SaveManager == null || _playerCombatController == null)
            {
                return;
            }

            SyncCoreHealthFromRuntime();

            var runSaveData = SaveManager.LoadRunSaveData() ?? new RunSaveData();
            var walletAmount = _resourceWalletService != null ? _resourceWalletService.CurrentAmount : runSaveData.WalletAmount;

            if (_resourceWalletService != null && bossRewardData != null)
            {
                _resourceWalletService.AddReward(bossRewardData.CompletionResourceBonus);
                walletAmount = _resourceWalletService.CurrentAmount;
            }

            runSaveData.UpdateCombatCheckpoint(
                SceneManager.GetActiveScene().name,
                _currentStageIndex,
                _currentWaveIndex,
                _playerCombatController.CurrentHealthNormalized,
                _currentCoreHealthNormalized,
                walletAmount,
                _playerCombatController.ActiveWeaponId);

            if (bossRewardData != null)
            {
                runSaveData.SetPendingReward(bossRewardData.RewardId, bossRewardData.RewardLabel);
            }

            runSaveData.SetBossEncounterState(false, 0);
            runSaveData.SetIntermissionOpen(false, _currentStageIndex, _currentWaveIndex);
            SaveManager.SaveRunSaveData(runSaveData);
        }

        private void WriteCombatSnapshot()
        {
            if (SaveManager == null || _playerCombatController == null || _checkpointData == null)
            {
                return;
            }

            SyncCoreHealthFromRuntime();

            var runSaveData = SaveManager.LoadRunSaveData() ?? new RunSaveData();
            runSaveData.UpdateCombatCheckpoint(
                SceneManager.GetActiveScene().name,
                _currentStageIndex,
                _currentWaveIndex,
                _playerCombatController.CurrentHealthNormalized,
                _currentCoreHealthNormalized,
                _resourceWalletService != null ? _resourceWalletService.CurrentAmount : runSaveData.WalletAmount,
                _playerCombatController.ActiveWeaponId);
            runSaveData.SetBossEncounterState(_isBossEncounterActive, _currentBossPhaseIndex);
            runSaveData.SetIntermissionOpen(false, _currentStageIndex, _currentWaveIndex);
            SaveManager.SaveRunSaveData(runSaveData);
        }

        private bool InitializeContextFromSave()
        {
            var runSaveData = SaveManager != null ? SaveManager.LoadRunSaveData() : null;

            if (runSaveData != null && runSaveData.HasActiveRun)
            {
                _currentStageIndex = Mathf.Max(1, runSaveData.StageIndex);
                _currentWaveIndex = Mathf.Max(1, runSaveData.WaveIndex);
                _currentCoreHealthNormalized = Mathf.Clamp01(runSaveData.CoreHealthNormalized);
                _isBossEncounterActive = runSaveData.IsBossEncounterActive;
                _currentBossPhaseIndex = Mathf.Max(0, runSaveData.BossPhaseIndex);
                return true;
            }

            ApplyWaveContext(_checkpointData != null ? _checkpointData.StageIndex : 1, _checkpointData != null ? _checkpointData.WaveIndex : 1);
            _currentCoreHealthNormalized = _checkpointData != null ? _checkpointData.CoreHealthNormalized : _currentCoreHealthNormalized;
            _isBossEncounterActive = false;
            _currentBossPhaseIndex = 0;
            return false;
        }

        private void SyncCoreHealthFromRuntime()
        {
            if (ResolvedCoreHitReceiver != null)
            {
                _currentCoreHealthNormalized = ResolvedCoreHitReceiver.CurrentHealthNormalized;
            }
        }
    }
}