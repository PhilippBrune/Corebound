using System;
using Corebound.Gameplay.Cards;
using Corebound.UI;
using UnityEngine;

namespace Corebound.MetaProgression
{
    // SkillTree.md. Serialized by index in every skill tree asset, so it is append-only, exactly
    // like CardEffectTarget and EnemyRoleType.
    public enum SkillEffectTarget
    {
        // Numeric (Multiply unless noted).
        WeaponDamageMultiplier = 0,
        WeaponFireIntervalMultiplier = 1,
        CriticalChance = 2,                 // Add, 0..1
        CriticalDamageMultiplier = 3,
        PlayerMaxHealthMultiplier = 4,
        PlayerMoveSpeedMultiplier = 5,
        TowerDamageMultiplier = 6,
        TowerRangeMultiplier = 7,
        TowerFireIntervalMultiplier = 8,
        CoreMaxHealthMultiplier = 9,
        ScrapGainMultiplier = 10,
        StartingScrap = 11,                 // Add
        WeaponSlots = 12,                   // Add
        PurchaseCostMultiplier = 13,
        CoreRepairCostMultiplier = 14,
        MagazineSize = 15,                  // Add
        ReloadTimeMultiplier = 16,
        ProjectileSpeedMultiplier = 17,
        IncomingDamageMultiplier = 18,
        StageStartScrap = 19,               // Add

        // Rules (Add; any value > 0 turns the rule on, the value is its strength where it has one).
        OpeningVolleyCritical = 20,         // first volley of every wave is a guaranteed crit
        SecondWind = 21,                    // once per stage a lethal hit leaves the player at value x max health
        CoreContingency = 22,               // once per stage a lethal hit leaves the Core at value x max health
        FreeFirstTowerPerStage = 23,        // the first tower built each stage costs (1 - value) of its price (name kept: serialized)
        FreeFirstRepairPerStage = 24,       // the first Core repair each stage costs no Scrap
        EliteScrapMultiplier = 25,          // elites drop (1 + value) x Scrap
        StartingRelic = 26,                 // every run starts with value random relics
        BossTowerRangeMultiplier = 27,      // towers reach (1 + value) x further during a boss encounter
        ExecuteDamageMultiplier = 28,       // (1 + value) x damage to enemies below 30% health

        // Appended 2026-09-22 (spec review C8). Both exist so that the Industry and Command
        // branches can end in rule changes rather than in another percentage.
        PickupMagnetRange = 29,             // Add: extra auto-pickup range in units; a large value is "everything"
        RewardRerolls = 30                  // Add: rerolls available per reward screen
    }

    [Serializable]
    public sealed class SkillEffectData
    {
        [SerializeField] private SkillEffectTarget _target;
        [SerializeField] private CardEffectOperation _operation = CardEffectOperation.Add;
        [SerializeField] private float _value;

        public SkillEffectTarget Target => _target;

        public CardEffectOperation Operation => _operation;

        public float Value => _value;
    }

    [Serializable]
    public sealed class SkillSubNodeData
    {
        [SerializeField] private string _nodeId = "sub_node";
        [SerializeField] private string _displayName = "Sub Node";
        [SerializeField] private string _displayNameDe = string.Empty;
        [SerializeField] private string _description = string.Empty;
        [SerializeField] private string _descriptionDe = string.Empty;
        [SerializeField] private int _costPerRank = 1;
        [SerializeField] private int _maxRank = 1;
        [SerializeField] private SkillEffectData[] _effects = Array.Empty<SkillEffectData>();

        public string NodeId => _nodeId;

        public string DisplayName => LocalizationService.Resolve(_displayName, _displayNameDe);

        public string Description => LocalizationService.Resolve(_description, _descriptionDe);

        public int CostPerRank => Mathf.Max(1, _costPerRank);

        public int MaxRank => Mathf.Max(1, _maxRank);

        public SkillEffectData[] Effects => _effects;
    }

    [Serializable]
    public sealed class SkillKeyNodeData
    {
        [SerializeField] private string _nodeId = "key_node";
        [SerializeField] private string _displayName = "Key Node";
        [SerializeField] private string _displayNameDe = string.Empty;
        [SerializeField] private string _description = string.Empty;
        [SerializeField] private string _descriptionDe = string.Empty;
        [SerializeField] private int _cost = 3;
        [SerializeField] private string _prerequisiteNodeId = string.Empty;
        [SerializeField] private int _branchSpendRequirement;
        [SerializeField] private SkillEffectData[] _effects = Array.Empty<SkillEffectData>();
        [SerializeField] private SkillSubNodeData[] _subNodes = Array.Empty<SkillSubNodeData>();
        [SerializeField] private string _iconSpritePath = string.Empty;

        // N1 (owner 2026-09-28): node positions are authored, not computed. A skill tree's shape
        // is content - where two chains converge, or a keystone sits deliberately far out, is a
        // design statement and should not be a side effect of a layout algorithm. Measured in
        // graph units from the centre portrait; sub-nodes are placed on an arc around their key
        // node automatically, so only key nodes carry one.
        //
        // Left at zero the radial presenter falls back to an even fan for that branch, so an
        // unauthored tree still draws rather than piling every node on the portrait.
        [SerializeField] private Vector2 _graphPosition;

        public string NodeId => _nodeId;

        public string DisplayName => LocalizationService.Resolve(_displayName, _displayNameDe);

        public string Description => LocalizationService.Resolve(_description, _descriptionDe);

        public int Cost => Mathf.Max(1, _cost);

        public string PrerequisiteNodeId => _prerequisiteNodeId;

        public int BranchSpendRequirement => Mathf.Max(0, _branchSpendRequirement);

        public SkillEffectData[] Effects => _effects;

        public SkillSubNodeData[] SubNodes => _subNodes;

        public string IconSpritePath => _iconSpritePath;

        public Vector2 GraphPosition => _graphPosition;
    }

    [Serializable]
    public sealed class SkillBranchData
    {
        [SerializeField] private string _branchId = "combat";
        [SerializeField] private string _displayName = "Combat";
        [SerializeField] private string _displayNameDe = "Kampf";
        [SerializeField] private Color _accentColor = Color.white;
        [SerializeField] private SkillKeyNodeData[] _keyNodes = Array.Empty<SkillKeyNodeData>();

        public string BranchId => _branchId;

        public string DisplayName => LocalizationService.Resolve(_displayName, _displayNameDe);

        public Color AccentColor => _accentColor;

        public SkillKeyNodeData[] KeyNodes => _keyNodes;
    }

    // Ascension is numbers only (SkillTree.md hard rule), so a bonus is a single numeric effect.
    [Serializable]
    public sealed class AscensionBonusData
    {
        [SerializeField] private string _bonusId = "asc_weapon_damage";
        [SerializeField] private string _displayName = "+2% weapon damage";
        [SerializeField] private string _displayNameDe = "+2 % Waffenschaden";
        [SerializeField] private SkillEffectData _effect = new SkillEffectData();

        public string BonusId => _bonusId;

        public string DisplayName => LocalizationService.Resolve(_displayName, _displayNameDe);

        public SkillEffectData Effect => _effect;
    }

    // One asset per playable character (owner decision 2026-09-21: one tree per character).
    // SkillTree.md lists branch / key node / sub node as separate ScriptableObjects; they are
    // [Serializable] classes inside this asset instead, because eighty separate node assets would
    // be unreviewable and nothing else ever references a node.
    [CreateAssetMenu(menuName = "Corebound/Meta Progression/Skill Tree Definition Data", fileName = "SkillTreeDefinitionData")]
    public sealed class SkillTreeDefinitionData : ScriptableObject
    {
        [SerializeField] private string _characterId = SaveSystem.MetaProgressionSaveData.DefaultCharacterId;
        [SerializeField] private string _characterName = "Vanguard";
        [SerializeField] private string _characterNameDe = "Vorhut";

        // Bumped whenever the tree's content changes; a save on an older version gets one free
        // respec automatically (SkillTree.md, Respec).
        [SerializeField] private int _treeVersion = 1;
        [SerializeField] private SkillBranchData[] _branches = Array.Empty<SkillBranchData>();
        [SerializeField] private AscensionBonusData[] _ascensionPool = Array.Empty<AscensionBonusData>();
        [SerializeField] private int _ascensionBaseCost = 5;
        [SerializeField] private int _ascensionOfferCount = 3;

        // Price of every respec after the free one per tree version. Not specified by the owner;
        // 30 Alloy is about one boss-heavy run, which makes a second respec a real decision.
        [SerializeField] private int _paidRespecAlloyCost = 30;

        public string CharacterId => _characterId;

        public string CharacterName => LocalizationService.Resolve(_characterName, _characterNameDe);

        public int TreeVersion => _treeVersion;

        public SkillBranchData[] Branches => _branches;

        public AscensionBonusData[] AscensionPool => _ascensionPool;

        public int AscensionOfferCount => Mathf.Max(1, _ascensionOfferCount);

        public int PaidRespecAlloyCost => Mathf.Max(0, _paidRespecAlloyCost);

        // SkillTree.md: rank n costs 5 + floor(n / 10), so the curve stretches rather than walls.
        public int AscensionCostForRank(int currentRank)
        {
            return Mathf.Max(1, _ascensionBaseCost) + Mathf.Max(0, currentRank) / 10;
        }

        public AscensionBonusData FindAscensionBonus(string bonusId)
        {
            if (_ascensionPool == null)
            {
                return null;
            }

            for (var index = 0; index < _ascensionPool.Length; index++)
            {
                if (_ascensionPool[index] != null && _ascensionPool[index].BonusId == bonusId)
                {
                    return _ascensionPool[index];
                }
            }

            return null;
        }
    }
}
