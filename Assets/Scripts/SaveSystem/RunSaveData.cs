using System.Collections.Generic;
using System;
using UnityEngine;

namespace Corebound.SaveSystem
{
    [Serializable]
    public sealed class RunSaveData
    {
        // v2 (2026-09-22): WeaponLoadout.md replaces the single EquippedWeaponId with six slots,
        // each carrying its weapon id, in-run tier and purchase price (the sell value's basis).
        public const int CurrentSaveFormatVersion = 2;
        public const int WeaponSlotCapacity = 6;

        [SerializeField] private int _saveFormatVersion = CurrentSaveFormatVersion;
        [SerializeField] private bool _hasActiveRun;
        [SerializeField] private string _sceneName = string.Empty;
        [SerializeField] private int _stageIndex;
        [SerializeField] private int _waveIndex;
        [SerializeField] private float _playerHealthNormalized = 1f;
        [SerializeField] private float _coreHealthNormalized = 1f;
        [SerializeField] private int _walletAmount;
        [SerializeField] private string _equippedWeaponId = string.Empty;
        [SerializeField] private bool _hasPendingReward;
        [SerializeField] private string _pendingRewardId = string.Empty;
        [SerializeField] private string _pendingRewardLabel = string.Empty;
        [SerializeField] private bool _isIntermissionOpen;
        [SerializeField] private List<string> _selectedCardIds = new List<string>();
        [SerializeField] private List<string> _selectedRelicIds = new List<string>();
        [SerializeField] private List<string> _evolvedRelicIds = new List<string>();
        [SerializeField] private int _rangedEnemiesKilledCount;
        [SerializeField] private int _wavesClearedCount;
        [SerializeField] private List<string> _builtTowerIds = new List<string>();
        [SerializeField] private List<int> _builtTowerUpgradeLevels = new List<int>();
        [SerializeField] private bool _isShopPending;
        [SerializeField] private string _pendingShopId = string.Empty;
        [SerializeField] private string _pendingShopLabel = string.Empty;
        [SerializeField] private bool _isBossEncounterActive;
        [SerializeField] private int _bossPhaseIndex;
        [SerializeField] private bool _isTutorialRun;
        [SerializeField] private long _lastUpdatedUtcTicks;

        // Run clocks, persisted so a run resumed across sessions reports its whole length
        // rather than only the current sitting. Appended, never inserted.
        [SerializeField] private float _elapsedActiveSeconds;
        [SerializeField] private float _elapsedRealSeconds;
        [SerializeField] private long _runStartedUtcTicks;

        [SerializeField] private List<string> _equippedWeaponIds = new List<string>();
        [SerializeField] private List<int> _equippedWeaponTiers = new List<int>();
        [SerializeField] private List<int> _equippedWeaponPrices = new List<int>();

        // Skill-tree starting bonuses (Starting Scrap, starting relics) are paid once per run and
        // stage-start Scrap once per stage; both are remembered here so a resumed run is not paid
        // a second time.
        [SerializeField] private bool _runStartBonusesApplied;
        [SerializeField] private int _stageStartBonusPaidStageIndex;

        public int SaveFormatVersion => _saveFormatVersion;

        public bool HasActiveRun => _hasActiveRun;

        public string SceneName => _sceneName;

        public int StageIndex => _stageIndex;

        public int WaveIndex => _waveIndex;

        public float PlayerHealthNormalized => _playerHealthNormalized;

        public float CoreHealthNormalized => _coreHealthNormalized;

        public int WalletAmount => _walletAmount;

        public string EquippedWeaponId => _equippedWeaponId;

        public bool HasPendingReward => _hasPendingReward;

        public string PendingRewardId => _pendingRewardId;

        public string PendingRewardLabel => _pendingRewardLabel;

        public bool IsIntermissionOpen => _isIntermissionOpen;

        public IReadOnlyList<string> SelectedCardIds => _selectedCardIds;

        public IReadOnlyList<string> SelectedRelicIds => _selectedRelicIds;

        public IReadOnlyList<string> EvolvedRelicIds => _evolvedRelicIds;

        public int RangedEnemiesKilledCount => _rangedEnemiesKilledCount;

        public int WavesClearedCount => _wavesClearedCount;

        public IReadOnlyList<string> BuiltTowerIds => _builtTowerIds;

        public IReadOnlyList<int> BuiltTowerUpgradeLevels => _builtTowerUpgradeLevels;

        public bool IsShopPending => _isShopPending;

        public string PendingShopId => _pendingShopId;

        public string PendingShopLabel => _pendingShopLabel;

        public bool IsBossEncounterActive => _isBossEncounterActive;

        public int BossPhaseIndex => _bossPhaseIndex;

        public bool IsTutorialRun => _isTutorialRun;

        public long LastUpdatedUtcTicks => _lastUpdatedUtcTicks;

        public float ElapsedActiveSeconds => _elapsedActiveSeconds;

        public float ElapsedRealSeconds => _elapsedRealSeconds;

        public long RunStartedUtcTicks => _runStartedUtcTicks;

        public void SetRunClocks(float activeSeconds, float realSeconds, long startedUtcTicks)
        {
            _elapsedActiveSeconds = Mathf.Max(0f, activeSeconds);
            _elapsedRealSeconds = Mathf.Max(0f, realSeconds);
            _runStartedUtcTicks = startedUtcTicks;
            Touch();
        }

        public IReadOnlyList<string> EquippedWeaponIds => _equippedWeaponIds;

        public IReadOnlyList<int> EquippedWeaponTiers => _equippedWeaponTiers;

        public IReadOnlyList<int> EquippedWeaponPrices => _equippedWeaponPrices;

        public bool RunStartBonusesApplied => _runStartBonusesApplied;

        public int StageStartBonusPaidStageIndex => _stageStartBonusPaidStageIndex;

        // v1 -> v2: the old single weapon becomes slot 0 (WeaponLoadout.md acceptance criterion).
        public bool MigrateToCurrentFormat()
        {
            _equippedWeaponIds ??= new List<string>();
            _equippedWeaponTiers ??= new List<int>();
            _equippedWeaponPrices ??= new List<int>();

            if (_saveFormatVersion >= CurrentSaveFormatVersion)
            {
                return false;
            }

            if (_equippedWeaponIds.Count == 0 && !string.IsNullOrWhiteSpace(_equippedWeaponId))
            {
                _equippedWeaponIds.Add(_equippedWeaponId);
                _equippedWeaponTiers.Add(0);
                _equippedWeaponPrices.Add(0);
            }

            // A v1 run already had its (nonexistent) start bonuses, so do not pay them now.
            _runStartBonusesApplied = _hasActiveRun;
            _stageStartBonusPaidStageIndex = _hasActiveRun ? _stageIndex : 0;
            _saveFormatVersion = CurrentSaveFormatVersion;
            return true;
        }

        public void SetEquippedWeapons(IReadOnlyList<string> weaponIds, IReadOnlyList<int> tiers, IReadOnlyList<int> prices)
        {
            _equippedWeaponIds.Clear();
            _equippedWeaponTiers.Clear();
            _equippedWeaponPrices.Clear();

            if (weaponIds != null)
            {
                for (var index = 0; index < weaponIds.Count && index < WeaponSlotCapacity; index++)
                {
                    _equippedWeaponIds.Add(weaponIds[index] ?? string.Empty);
                    _equippedWeaponTiers.Add(tiers != null && index < tiers.Count ? tiers[index] : 0);
                    _equippedWeaponPrices.Add(prices != null && index < prices.Count ? prices[index] : 0);
                }
            }

            // Slot 0 mirrors into the v1 field so anything still reading the single id sees the
            // primary weapon.
            _equippedWeaponId = _equippedWeaponIds.Count > 0 ? _equippedWeaponIds[0] : string.Empty;
            Touch();
        }

        public void MarkRunStartBonusesApplied()
        {
            _runStartBonusesApplied = true;
            Touch();
        }

        public void MarkStageStartBonusPaid(int stageIndex)
        {
            _stageStartBonusPaidStageIndex = stageIndex;
            Touch();
        }

        public static RunSaveData CreateBootstrap(RunSaveBootstrapData bootstrapData)
        {
            var saveData = new RunSaveData();

            if (bootstrapData == null || !bootstrapData.SeedContinueRun)
            {
                saveData.SetInactive();
                return saveData;
            }

            saveData._hasActiveRun = true;
            saveData._sceneName = bootstrapData.SceneName;
            saveData._stageIndex = bootstrapData.StageIndex;
            saveData._waveIndex = bootstrapData.WaveIndex;
            saveData._playerHealthNormalized = bootstrapData.PlayerHealthNormalized;
            saveData._coreHealthNormalized = bootstrapData.CoreHealthNormalized;
            saveData._walletAmount = bootstrapData.WalletAmount;
            saveData._equippedWeaponId = bootstrapData.EquippedWeaponId;
            saveData._equippedWeaponIds.Add(bootstrapData.EquippedWeaponId ?? string.Empty);
            saveData._equippedWeaponTiers.Add(0);
            saveData._equippedWeaponPrices.Add(0);
            saveData.Touch();
            return saveData;
        }

        public void UpdateCombatCheckpoint(string sceneName, int stageIndex, int waveIndex, float playerHealthNormalized, float coreHealthNormalized, int walletAmount, string equippedWeaponId)
        {
            _hasActiveRun = true;
            _sceneName = sceneName;
            _stageIndex = Mathf.Max(1, stageIndex);
            _waveIndex = Mathf.Max(1, waveIndex);
            _playerHealthNormalized = Mathf.Clamp01(playerHealthNormalized);
            _coreHealthNormalized = Mathf.Clamp01(coreHealthNormalized);
            _walletAmount = Mathf.Max(0, walletAmount);
            _equippedWeaponId = equippedWeaponId ?? string.Empty;
            Touch();
        }

        // Used by MainMenuRouteTargetController's debug stage-select (development/testing only -
        // see MainMenuRouteTargetController.ApplyDebugStageOverride) to start a fresh run directly
        // on a chosen stage instead of always Stage 1, so Stage 2+ content can be reached without
        // playing through everything before it every time.
        public void SetStageIndex(int stageIndex)
        {
            _stageIndex = Mathf.Max(1, stageIndex);
            Touch();
        }

        public void SetPendingReward(string rewardId, string rewardLabel)
        {
            _hasPendingReward = !string.IsNullOrWhiteSpace(rewardId);
            _pendingRewardId = rewardId ?? string.Empty;
            _pendingRewardLabel = rewardLabel ?? string.Empty;
            Touch();
        }

        public void ClearPendingReward()
        {
            _hasPendingReward = false;
            _pendingRewardId = string.Empty;
            _pendingRewardLabel = string.Empty;
            Touch();
        }

        public void SetIntermissionOpen(bool isIntermissionOpen, int stageIndex, int waveIndex)
        {
            _isIntermissionOpen = isIntermissionOpen;
            _stageIndex = Mathf.Max(1, stageIndex);
            _waveIndex = Mathf.Max(1, waveIndex);
            Touch();
        }

        public void SetTutorialRun(bool isTutorialRun)
        {
            _isTutorialRun = isTutorialRun;
            Touch();
        }

        // A new run's starting weapon: the whole loadout becomes that one weapon in slot 0.
        public void SetEquippedWeaponId(string weaponId)
        {
            _equippedWeaponId = weaponId ?? string.Empty;
            _equippedWeaponIds.Clear();
            _equippedWeaponTiers.Clear();
            _equippedWeaponPrices.Clear();

            if (!string.IsNullOrWhiteSpace(_equippedWeaponId))
            {
                _equippedWeaponIds.Add(_equippedWeaponId);
                _equippedWeaponTiers.Add(0);
                _equippedWeaponPrices.Add(0);
            }

            Touch();
        }

        public void SetWalletAmount(int walletAmount)
        {
            _walletAmount = Mathf.Max(0, walletAmount);
            Touch();
        }

        public void SetSelectedCardIds(IEnumerable<string> cardIds)
        {
            _selectedCardIds.Clear();

            if (cardIds != null)
            {
                _selectedCardIds.AddRange(cardIds);
            }

            Touch();
        }

        public void SetSelectedRelicIds(IEnumerable<string> relicIds)
        {
            _selectedRelicIds.Clear();

            if (relicIds != null)
            {
                _selectedRelicIds.AddRange(relicIds);
            }

            Touch();
        }

        public void SetEvolvedRelicIds(IEnumerable<string> relicIds)
        {
            _evolvedRelicIds.Clear();

            if (relicIds != null)
            {
                _evolvedRelicIds.AddRange(relicIds);
            }

            Touch();
        }

        public void SetRangedEnemiesKilledCount(int count)
        {
            _rangedEnemiesKilledCount = Mathf.Max(0, count);
            Touch();
        }

        public void SetWavesClearedCount(int count)
        {
            _wavesClearedCount = Mathf.Max(0, count);
            Touch();
        }

        public void SetBuiltTowerIds(IEnumerable<string> towerIds)
        {
            _builtTowerIds.Clear();

            if (towerIds != null)
            {
                _builtTowerIds.AddRange(towerIds);
            }

            Touch();
        }

        public void SetBuiltTowerUpgradeLevels(IEnumerable<int> towerUpgradeLevels)
        {
            _builtTowerUpgradeLevels.Clear();

            if (towerUpgradeLevels != null)
            {
                _builtTowerUpgradeLevels.AddRange(towerUpgradeLevels);
            }

            Touch();
        }

        public void SetShopPending(string shopId, string shopLabel)
        {
            _isShopPending = !string.IsNullOrWhiteSpace(shopId);
            _pendingShopId = shopId ?? string.Empty;
            _pendingShopLabel = shopLabel ?? string.Empty;
            Touch();
        }

        public void ClearShopPending()
        {
            _isShopPending = false;
            _pendingShopId = string.Empty;
            _pendingShopLabel = string.Empty;
            Touch();
        }

        public void SetBossEncounterState(bool isBossEncounterActive, int bossPhaseIndex)
        {
            _isBossEncounterActive = isBossEncounterActive;
            _bossPhaseIndex = Mathf.Max(0, bossPhaseIndex);
            Touch();
        }

        public void SetInactive()
        {
            _hasActiveRun = false;
            _sceneName = string.Empty;
            _stageIndex = 0;
            _waveIndex = 0;
            _playerHealthNormalized = 1f;
            _coreHealthNormalized = 1f;
            _walletAmount = 0;
            _equippedWeaponId = string.Empty;
            _hasPendingReward = false;
            _pendingRewardId = string.Empty;
            _pendingRewardLabel = string.Empty;
            _isIntermissionOpen = false;
            _selectedCardIds.Clear();
            _selectedRelicIds.Clear();
            _evolvedRelicIds.Clear();
            _rangedEnemiesKilledCount = 0;
            _wavesClearedCount = 0;
            _builtTowerIds.Clear();
            _builtTowerUpgradeLevels.Clear();
            _isShopPending = false;
            _pendingShopId = string.Empty;
            _pendingShopLabel = string.Empty;
            _isBossEncounterActive = false;
            _bossPhaseIndex = 0;
            _isTutorialRun = false;
            _equippedWeaponIds.Clear();
            _equippedWeaponTiers.Clear();
            _equippedWeaponPrices.Clear();
            _runStartBonusesApplied = false;
            _stageStartBonusPaidStageIndex = 0;
            Touch();
        }

        public void Touch()
        {
            _lastUpdatedUtcTicks = DateTime.UtcNow.Ticks;
        }
    }
}