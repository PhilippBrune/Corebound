using System;
using Corebound.Gameplay.Cards;
using Corebound.SaveSystem;
using UnityEngine;

namespace Corebound.Gameplay.Resources
{
    public sealed class ResourceWalletService : MonoBehaviour
    {
        [SerializeField] private ResourceData _resourceData;
        [SerializeField] private UnityEngine.Object _saveManagerSource;
        [SerializeField] private CardService _cardService;

        private int _currentAmount;
        private bool _isDirty;

        public event Action WalletChanged;

        public int CurrentAmount => _currentAmount;

        public string DisplayLabel => _resourceData != null ? _resourceData.DisplayName : "Resource";

        public float AutoPickupRange => _resourceData != null ? _resourceData.AutoPickupRange : 1.8f;

        public float PickupMoveSpeed => _resourceData != null ? _resourceData.PickupMoveSpeed : 4.5f;

        private ISaveManager SaveManager => SaveManagerResolver.Resolve(_saveManagerSource);

        private void Awake()
        {
            LoadFromSave();
        }

        // Wallet changes (resource pickups especially) can fire many times per frame; writing
        // the save file synchronously on every single one caused a disk write per pickup. Instead
        // we mark dirty and flush once per frame, plus on disable/quit so nothing is lost.
        private void LateUpdate()
        {
            FlushIfDirty();
        }

        private void OnDisable()
        {
            FlushIfDirty();
        }

        private void OnApplicationQuit()
        {
            FlushIfDirty();
        }

        public int AddReward(int baseAmount)
        {
            var multiplier = _cardService != null ? _cardService.ResourceGainMultiplier : 1f;
            var gainedAmount = Mathf.Max(1, Mathf.RoundToInt(baseAmount * multiplier));
            SetCurrentAmount(_currentAmount + gainedAmount);
            return gainedAmount;
        }

        public bool TrySpend(int amount)
        {
            if (amount <= 0 || _currentAmount < amount)
            {
                return false;
            }

            SetCurrentAmount(_currentAmount - amount);
            return true;
        }

        public void LoadFromSave()
        {
            if (SaveManager == null)
            {
                return;
            }

            var runSaveData = SaveManager.LoadRunSaveData();
            _currentAmount = runSaveData != null ? runSaveData.WalletAmount : 0;
            WalletChanged?.Invoke();
        }

        private void SetCurrentAmount(int nextAmount)
        {
            _currentAmount = Mathf.Max(0, nextAmount);
            _isDirty = true;
            WalletChanged?.Invoke();
        }

        private void FlushIfDirty()
        {
            if (!_isDirty || SaveManager == null)
            {
                return;
            }

            var runSaveData = SaveManager.LoadRunSaveData() ?? new RunSaveData();
            runSaveData.SetWalletAmount(_currentAmount);
            SaveManager.SaveRunSaveData(runSaveData);
            _isDirty = false;
        }
    }
}