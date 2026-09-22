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
        // Upgrades.md: the meta currency is Alloy. The label field is kept (not renamed) so the
        // asset keeps deserializing; its value is now "Alloy".
        [SerializeField] private string _metaCurrencyLabel = "Alloy";
        [SerializeField] private string _metaCurrencyLabelDe = "Legierung";
        [SerializeField] private int _startingCurrencyAmount;
        [SerializeField] private bool _startingTutorialSeen;

        // Owner decision 2026-09-22: Skill Points and Alloy come from bosses and elites only. A run
        // that ends without beating a boss pays nothing; going deeper pays more. The old flat
        // run-completion/run-failure payouts are gone - they paid for simply finishing, which is
        // exactly what "only after defeating a boss" rules out.
        //
        // Indexed by stage index - 1; stages beyond the array use its last entry.
        [SerializeField] private int[] _bossSkillPointsByStage = { 1, 1, 2, 2, 3 };
        [SerializeField] private int _bossFirstKillSkillPoints = 2;
        [SerializeField] private int[] _bossAlloyByStage = { 6, 8, 10, 12, 14 };
        [SerializeField] private int _bossFirstKillAlloy = 10;
        [SerializeField] private int _eliteKillAlloy = 1;

        [SerializeField] private UnlockDefinition[] _unlocks = Array.Empty<UnlockDefinition>();

        // Upgrades.md stat tracks and SkillTree.md's per-character trees. Hung off this asset
        // because it is the one progression asset both the main menu and the gameplay scene
        // already reference (PlayerCombatData, WaveManager, MetaProgressionService).
        [SerializeField] private UpgradeCatalogData _upgradeCatalog;
        [SerializeField] private SkillTreeDefinitionData[] _skillTrees = Array.Empty<SkillTreeDefinitionData>();

        public string MetaCurrencyLabel => Corebound.UI.LocalizationService.Resolve(_metaCurrencyLabel, _metaCurrencyLabelDe);

        public int BossFirstKillSkillPoints => Mathf.Max(0, _bossFirstKillSkillPoints);

        public int BossFirstKillAlloy => Mathf.Max(0, _bossFirstKillAlloy);

        public int EliteKillAlloy => Mathf.Max(0, _eliteKillAlloy);

        public UnlockDefinition[] Unlocks => _unlocks;

        public UpgradeCatalogData UpgradeCatalog => _upgradeCatalog;

        public SkillTreeDefinitionData[] SkillTrees => _skillTrees;

        public SkillTreeDefinitionData FindSkillTree(string characterId)
        {
            if (_skillTrees == null || _skillTrees.Length == 0)
            {
                return null;
            }

            for (var index = 0; index < _skillTrees.Length; index++)
            {
                if (_skillTrees[index] != null && _skillTrees[index].CharacterId == characterId)
                {
                    return _skillTrees[index];
                }
            }

            return _skillTrees[0];
        }

        public int ResolveBossSkillPoints(int stageIndex)
        {
            return ResolveByStage(_bossSkillPointsByStage, stageIndex);
        }

        public int ResolveBossAlloy(int stageIndex)
        {
            return ResolveByStage(_bossAlloyByStage, stageIndex);
        }

        // Weapon slots 5 and 6 (WeaponLoadout.md) are Alloy unlocks; this counts how many the
        // save has bought.
        public int CountPurchasedWeaponSlots(MetaProgressionSaveData saveData)
        {
            if (saveData == null || _unlocks == null)
            {
                return 0;
            }

            var count = 0;

            for (var index = 0; index < _unlocks.Length; index++)
            {
                if (_unlocks[index] != null && _unlocks[index].WeaponSlotReward > 0 && saveData.HasPurchased(_unlocks[index].UnlockId))
                {
                    count += _unlocks[index].WeaponSlotReward;
                }
            }

            return count;
        }

        public MetaProgressionSaveData CreateDefaultSaveData()
        {
            var saveData = new MetaProgressionSaveData();
            saveData.SetStartingAlloy(_startingCurrencyAmount);
            saveData.SetHasSeenTutorial(_startingTutorialSeen);
            return saveData;
        }

        private static int ResolveByStage(int[] values, int stageIndex)
        {
            if (values == null || values.Length == 0)
            {
                return 0;
            }

            return Mathf.Max(0, values[Mathf.Clamp(stageIndex - 1, 0, values.Length - 1)]);
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

            // Number of extra weapon slots this unlock grants (WeaponLoadout.md slots 5 and 6).
            [SerializeField] private int _weaponSlotReward;

            public string UnlockId => _unlockId;

            public string Title => _title;

            public string Summary => _summary;

            public int Cost => _cost;

            public string TargetSystem => _targetSystem;

            public WeaponData WeaponReward => _weaponReward;

            public TowerData TowerReward => _towerReward;

            public CardData CardReward => _cardReward;

            public StartLoadoutData StartLoadoutReward => _startLoadoutReward;

            public int WeaponSlotReward => Mathf.Max(0, _weaponSlotReward);
        }
    }
}