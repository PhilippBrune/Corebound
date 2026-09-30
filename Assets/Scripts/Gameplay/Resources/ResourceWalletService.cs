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

        // Skill Tree "Magnetise" (Industry key node): adds to the auto-pickup range, far enough
        // that Scrap comes to the player instead of the player walking the arena for it.
        public float AutoPickupRange => (_resourceData != null ? _resourceData.AutoPickupRange : 1.8f)
            + Corebound.MetaProgression.RunProgressionSnapshot.Current.Skills[Corebound.MetaProgression.SkillEffectTarget.PickupMagnetRange]
            + Corebound.Gameplay.Combat.CharacterEffects.MagnetRangeBonus;

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

        // Refunds, sell value and skill-tree starting Scrap are not income and must not be scaled
        // by Scrap-gain modifiers (Refund used to go through AddReward and was inflated by them).
        public void AddFlat(int amount)
        {
            if (amount > 0)
            {
                SetCurrentAmount(_currentAmount + amount);
            }
        }

        public int AddReward(int baseAmount)
        {
            var multiplier = (_cardService != null ? _cardService.ResourceGainMultiplier : 1f)
                * Mathf.Max(0f, Corebound.MetaProgression.RunProgressionSnapshot.Current.Skills[Corebound.MetaProgression.SkillEffectTarget.ScrapGainMultiplier]);
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