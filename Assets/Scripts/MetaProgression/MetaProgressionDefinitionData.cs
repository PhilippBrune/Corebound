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

        // Owner decision 2026-09-22: Alloy comes from bosses and elites only, more for deeper
        // bosses. Skill Points no longer come from bosses at all (P3-2, 2026-09-29): every SP is paid
        // by a level or a quest, so the tree's 212 SP can be reached exactly and never exceeded. A
        // boss's first kill is a quest worth 1 SP; every kill pays an XP lump.
        //
        // Indexed by stage index - 1; stages beyond the array use its last entry.
        [SerializeField] private int[] _bossAlloyByStage = { 6, 8, 10, 12, 14 };
        [SerializeField] private int _bossFirstKillAlloy = 10;
        [SerializeField] private int _eliteKillAlloy = 1;

        [SerializeField] private UnlockDefinition[] _unlocks = Array.Empty<UnlockDefinition>();

        // Upgrades.md stat tracks and SkillTree.md's per-character trees. Hung off this asset
        // because it is the one progression asset both the main menu and the gameplay scene
        // already reference (PlayerCombatData, WaveManager, MetaProgressionService).
        [SerializeField] private UpgradeCatalogData _upgradeCatalog;
        [SerializeField] private SkillTreeDefinitionData[] _skillTrees = Array.Empty<SkillTreeDefinitionData>();

        // The items the Upgrades tab lists (Upgrades.md: "weapons then towers").
        [SerializeField] private WeaponCatalogData _upgradeWeapons;
        [SerializeField] private TowerCatalogData _upgradeTowers;

        // Every card the Records collection lists, collected or not (playtest 2026-09-29). Lives
        // here for the same reason as the catalogues above: it is the one asset the main menu
        // already holds, and the menu otherwise has no route to the gameplay card pool.
        [SerializeField] private Corebound.Gameplay.Cards.CardPoolData _cardCollection;

        // The stages, for the boss quests: one per stage boss, titled by stage and boss name.
        [SerializeField] private Corebound.Gameplay.Waves.StageProgressionData _stageProgression;

        // The enemy types and elite affixes the Alloy kill quests are about (A2, 2026-09-29).
        [SerializeField] private Corebound.Gameplay.Enemies.EnemyData[] _questEnemies = Array.Empty<Corebound.Gameplay.Enemies.EnemyData>();
        [SerializeField] private Corebound.Gameplay.Enemies.EnemyEliteModifierData[] _questEliteAffixes = Array.Empty<Corebound.Gameplay.Enemies.EnemyEliteModifierData>();

        public Corebound.Gameplay.Enemies.EnemyData[] QuestEnemies => _questEnemies;

        // STORY_2026-09-30. Hung off this asset because both the main menu (premise, Core Logs,
        // credits) and the run (the Core's lines, the ending) already reference it.
        [SerializeField] private Corebound.Story.StoryData _story;

        public Corebound.Story.StoryData Story => _story;

        // CHARACTER_CLASSES_2026-09-30: the playable classes, in the order the class select shows.
        [SerializeField] private CharacterData[] _characters = Array.Empty<CharacterData>();

        public CharacterData[] Characters => _characters;

        public CharacterData FindCharacter(string characterId)
        {
            for (var index = 0; _characters != null && index < _characters.Length; index++)
            {
                if (_characters[index] != null && _characters[index].CharacterId == characterId)
                {
                    return _characters[index];
                }
            }

            return _characters != null && _characters.Length > 0 ? _characters[0] : null;
        }

        public Corebound.Gameplay.Enemies.EnemyEliteModifierData[] QuestEliteAffixes => _questEliteAffixes;

        public string MetaCurrencyLabel => Corebound.UI.LocalizationService.Resolve(_metaCurrencyLabel, _metaCurrencyLabelDe);

        public Corebound.Gameplay.Waves.StageProgressionData StageProgression => _stageProgression;

        public int BossFirstKillAlloy => Mathf.Max(0, _bossFirstKillAlloy);

        public int EliteKillAlloy => Mathf.Max(0, _eliteKillAlloy);

        public UnlockDefinition[] Unlocks => _unlocks;

        public UpgradeCatalogData UpgradeCatalog => _upgradeCatalog;

        public SkillTreeDefinitionData[] SkillTrees => _skillTrees;

        public WeaponCatalogData UpgradeWeapons => _upgradeWeapons;

        public Corebound.Gameplay.Cards.CardPoolData CardCollection => _cardCollection;

        public TowerCatalogData UpgradeTowers => _upgradeTowers;

        // A weapon is locked only if some unlock grants it and that unlock is not bought.
        // One definition of "owned", shared with the New Run panel: a weapon is available when
        // nothing gates it, or when its licence has been bought. Licences added 2026-09-22 carry no
        // WeaponReward reference and are matched by id convention instead, which the old
        // reward-only lookup here missed - it reported Railgun, Nova and Launcher as unlocked and
        // offered their upgrade tracks before the weapon could appear in a run.
        public bool IsWeaponUnlocked(MetaProgressionSaveData saveData, WeaponData weapon)
        {
            return StartWeaponOptions.IsAvailable(weapon, saveData, this);
        }

        public UnlockDefinition FindUnlockFor(WeaponData weapon, TowerData tower)
        {
            if (_unlocks == null)
            {
                return null;
            }

            for (var index = 0; index < _unlocks.Length; index++)
            {
                var unlock = _unlocks[index];

                var weaponByConvention = weapon != null && unlock != null && unlock.UnlockId == "unlock_" + weapon.WeaponId;

                if (unlock != null && ((weapon != null && unlock.WeaponReward == weapon) || weaponByConvention || (tower != null && unlock.TowerReward == tower)))
                {
                    return unlock;
                }
            }

            return null;
        }

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