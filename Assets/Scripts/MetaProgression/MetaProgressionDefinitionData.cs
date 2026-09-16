using Corebound.Gameplay.Cards;
using Corebound.Gameplay.Towers;
using System;
using Corebound.Gameplay.Weapons;
using Corebound.Settings;
using Corebound.SaveSystem;
using UnityEngine;

namespace Corebound.MetaProgression
{
    [CreateAssetMenu(menuName = "Corebound/Meta Progression/Meta Progression Definition Data", fileName = "MetaProgressionDefinitionData")]
    public sealed class MetaProgressionDefinitionData : ScriptableObject
    {
        [SerializeField] private string _metaCurrencyLabel = "Core Shards";
        [SerializeField] private int _startingCurrencyAmount = 8;
        [SerializeField] private bool _startingTutorialSeen;
        [SerializeField] private int _runCompletionCurrencyReward = 5;
        [SerializeField] private int _runFailureCurrencyReward = 1;
        [SerializeField] private UnlockDefinition[] _unlocks = Array.Empty<UnlockDefinition>();

        public string MetaCurrencyLabel => _metaCurrencyLabel;

        public int RunCompletionCurrencyReward => Mathf.Max(0, _runCompletionCurrencyReward);

        public int RunFailureCurrencyReward => Mathf.Max(0, _runFailureCurrencyReward);

        public UnlockDefinition[] Unlocks => _unlocks;

        public MetaProgressionSaveData CreateDefaultSaveData()
        {
            var saveData = new MetaProgressionSaveData();
            saveData.SetMetaCurrencyAmount(_startingCurrencyAmount);
            saveData.SetHasSeenTutorial(_startingTutorialSeen);
            return saveData;
        }

        [Serializable]
        public sealed class UnlockDefinition
        {
            [SerializeField] private string _unlockId;
            [SerializeField] private string _title;
            [SerializeField] private string _summary;
            [SerializeField] private int _cost = 3;
            [SerializeField] private string _targetSystem = "Cards";
            [SerializeField] private WeaponData _weaponReward;
            [SerializeField] private TowerData _towerReward;
            [SerializeField] private CardData _cardReward;
            [SerializeField] private StartLoadoutData _startLoadoutReward;

            public string UnlockId => _unlockId;

            public string Title => _title;

            public string Summary => _summary;

            public int Cost => _cost;

            public string TargetSystem => _targetSystem;

            public WeaponData WeaponReward => _weaponReward;

            public TowerData TowerReward => _towerReward;

            public CardData CardReward => _cardReward;

            public StartLoadoutData StartLoadoutReward => _startLoadoutReward;
        }
    }
}