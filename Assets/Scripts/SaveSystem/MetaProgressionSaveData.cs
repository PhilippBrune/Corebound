using System;
using System.Collections.Generic;
using UnityEngine;

namespace Corebound.SaveSystem
{
    [Serializable]
    public sealed class MetaProgressionSaveData
    {
        [SerializeField] private int _saveFormatVersion = 1;
        [SerializeField] private int _metaCurrencyAmount;
        [SerializeField] private bool _hasSeenTutorial;
        [SerializeField] private List<string> _purchasedUnlockIds = new List<string>();

        public int SaveFormatVersion => _saveFormatVersion;

        public int MetaCurrencyAmount => _metaCurrencyAmount;

        public bool HasSeenTutorial => _hasSeenTutorial;

        public IReadOnlyList<string> PurchasedUnlockIds => _purchasedUnlockIds;

        public MetaProgressionSaveData Clone()
        {
            return new MetaProgressionSaveData
            {
                _saveFormatVersion = _saveFormatVersion,
                _metaCurrencyAmount = _metaCurrencyAmount,
                _hasSeenTutorial = _hasSeenTutorial,
                _purchasedUnlockIds = new List<string>(_purchasedUnlockIds)
            };
        }

        public void SetMetaCurrencyAmount(int value)
        {
            _metaCurrencyAmount = Mathf.Max(0, value);
        }

        public void SetHasSeenTutorial(bool value)
        {
            _hasSeenTutorial = value;
        }

        public bool HasPurchased(string unlockId)
        {
            return !string.IsNullOrWhiteSpace(unlockId) && _purchasedUnlockIds.Contains(unlockId);
        }

        public bool TryPurchase(string unlockId, int cost)
        {
            if (string.IsNullOrWhiteSpace(unlockId) || HasPurchased(unlockId) || _metaCurrencyAmount < cost)
            {
                return false;
            }

            _metaCurrencyAmount -= cost;
            _purchasedUnlockIds.Add(unlockId);
            return true;
        }
    }
}