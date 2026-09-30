using System;
using Corebound.Gameplay.Cards;
using Corebound.UI;
using UnityEngine;

namespace Corebound.MetaProgression
{
    // Docs/Specs/DIY/CHARACTER_CLASSES_2026-09-30.md. Serialized by index: append only.
    public enum CharacterSignature
    {
        Vanguard = 0,   // Set Bonus tiers need one copy fewer; damage per active tier
        Engineer = 1,   // towers near the Engineer fire faster; Interact at a tower overcharges it
        Salvager = 2,   // richer kills, a magnet, and Scrap hoarded past a threshold becomes damage
        Custodian = 3,  // tethered to the Core: it takes less, shots pierce; breaking it sends a shockwave
        Catalyst = 4    // statuses last longer and an enemy dying with two or more spreads them all
    }

    // How a class becomes playable (K4-1: condition-based and free). Append only.
    public enum CharacterUnlock
    {
        Start = 0,
        TowerMastered = 1,      // any tower upgraded to its highest level in one run
        FactoryTier = 2,        // value: Factory chain tier reached in one run
        UntouchedCoreStage = 3, // any stage cleared without the Core taking damage
        BossDefeated = 4        // value: stage whose boss has been defeated
    }

    // One playable class. Owner decisions 2026-09-30: all five at once (K1-3), strong signatures
    // (K2-1), account level shared and only the tree per class (K3-2), free unlocks by condition
    // (K4-1), a per-class card leaning (K5-1), a sprite per class (K6-2).
    [CreateAssetMenu(menuName = "Corebound/Meta Progression/Character Data", fileName = "Character")]
    public sealed class CharacterData : ScriptableObject
    {
        [SerializeField] private string _characterId = SaveSystem.MetaProgressionSaveData.DefaultCharacterId;
        [SerializeField] private string _displayName = "Vanguard";
        [SerializeField] private string _displayNameDe = "Vorhut";
        [SerializeField] private string _role = string.Empty;
        [SerializeField] private string _roleDe = string.Empty;
        [SerializeField] private string _summary = string.Empty;
        [SerializeField] private string _summaryDe = string.Empty;
        [SerializeField] private CharacterSignature _signature;

        [Header("Unlock")]
        [SerializeField] private CharacterUnlock _unlock;
        [SerializeField] private int _unlockValue;
        [SerializeField] private string _unlockHint = string.Empty;
        [SerializeField] private string _unlockHintDe = string.Empty;

        [Header("Kit and stats")]
        [SerializeField] private float _maxHealthMultiplier = 1f;
        [SerializeField] private float _moveSpeedMultiplier = 1f;
        [SerializeField] private float _weaponDamageMultiplier = 1f;
        [SerializeField] private int _startingScrapBonus;
        [SerializeField] private string _startingTowerId = string.Empty;

        [Header("Card leaning (K5-1)")]
        [SerializeField] private CardTagType[] _favouredTags = Array.Empty<CardTagType>();
        [SerializeField] private float _favouredTagBonus = 0.6f;
        [SerializeField] private float _buildTagAffinity = 0.35f;

        [Header("Look (K6-2: own sprite; a tint until it exists)")]
        [SerializeField] private string _spritePath = string.Empty;
        [SerializeField] private Color _tint = Color.white;

        [SerializeField] private SkillTreeDefinitionData _skillTree;

        public string CharacterId => _characterId;

        public string DisplayName => LocalizationService.Resolve(_displayName, _displayNameDe);

        public string Role => LocalizationService.Resolve(_role, _roleDe);

        public string Summary => LocalizationService.Resolve(_summary, _summaryDe);

        public CharacterSignature Signature => _signature;

        public CharacterUnlock Unlock => _unlock;

        public int UnlockValue => _unlockValue;

        public string UnlockHint => LocalizationService.Resolve(_unlockHint, _unlockHintDe);

        public float MaxHealthMultiplier => Mathf.Max(0.1f, _maxHealthMultiplier);

        public float MoveSpeedMultiplier => Mathf.Max(0.1f, _moveSpeedMultiplier);

        public float WeaponDamageMultiplier => Mathf.Max(0.1f, _weaponDamageMultiplier);

        public int StartingScrapBonus => Mathf.Max(0, _startingScrapBonus);

        public string StartingTowerId => _startingTowerId;

        public CardTagType[] FavouredTags => _favouredTags;

        public float FavouredTagBonus => Mathf.Max(0f, _favouredTagBonus);

        public float BuildTagAffinity => Mathf.Clamp(_buildTagAffinity, -1f, 1f);

        public string SpritePath => _spritePath;

        public Color Tint => _tint;

        public SkillTreeDefinitionData SkillTree => _skillTree;

        public bool Favours(CardTagType tag)
        {
            if (_favouredTags == null)
            {
                return false;
            }

            for (var index = 0; index < _favouredTags.Length; index++)
            {
                if (_favouredTags[index] == tag)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
