using Corebound.Gameplay.Cards;
using UnityEngine;

namespace Corebound.Gameplay.Relics
{
    // Relics.md: "Relics nutzen dieselbe Modifier- und Event-Architektur wie Cards" - _effects
    // deliberately reuses CardEffectData (the same asset type Cards use) instead of a parallel
    // RelicEffectData class, so a Relic applies through the exact same CardModifierSnapshot pass
    // as a Card, just tracked in its own selected-list so Relic count doesn't affect Card-specific
    // UI/counting logic (e.g. CardOfferData.LegendaryMinimumSelectedCards).
    [CreateAssetMenu(menuName = "Corebound/Gameplay/Relics/Relic Data", fileName = "RelicData")]
    public sealed class RelicData : ScriptableObject
    {
        [SerializeField] private string _relicId = "relic_default";
        [SerializeField] private string _displayName = "Relic";
        [SerializeField] [TextArea(2, 3)] private string _description = string.Empty;
        [SerializeField] private CardEffectData[] _effects;
        [SerializeField] private string _requiredUnlockId = string.Empty;

        // Same hook CardData carries, so a relic reaches Crit/Multishot without a special case -
        // Weapons.md: "Card- und Tower-Effekte koennen Weapons ohne Sonderfaelle modifizieren."
        [SerializeField] private Corebound.Gameplay.Weapons.WeaponModifierData[] _weaponModifiers;

        // Evolving relics: _effects above is the starting effect set. Once CardService's tracked
        // counter for _evolutionCondition reaches _evolutionThreshold, the relic permanently
        // switches to _evolvedEffects instead (CardService.RebuildModifierSnapshot picks one or
        // the other per relic, keyed by RelicId in RunSaveData's EvolvedRelicIds). Leaving
        // _evolutionCondition at None (the default) makes a relic non-evolving, exactly like the
        // first 10 relics shipped before this field existed.
        [SerializeField] private RelicEvolutionCondition _evolutionCondition = RelicEvolutionCondition.None;
        [SerializeField] private int _evolutionThreshold;
        [SerializeField] private string _evolvedDisplayName = string.Empty;
        [SerializeField] [TextArea(2, 3)] private string _evolvedDescription = string.Empty;
        [SerializeField] private CardEffectData[] _evolvedEffects;

        public string RelicId => _relicId;

        public string DisplayName => _displayName;

        public string Description => _description;

        public CardEffectData[] Effects => _effects;

        public string RequiredUnlockId => _requiredUnlockId;

        public Corebound.Gameplay.Weapons.WeaponModifierData[] WeaponModifiers => _weaponModifiers;

        public RelicEvolutionCondition EvolutionCondition => _evolutionCondition;

        public int EvolutionThreshold => _evolutionThreshold;

        public string EvolvedDisplayName => !string.IsNullOrWhiteSpace(_evolvedDisplayName) ? _evolvedDisplayName : _displayName;

        public string EvolvedDescription => !string.IsNullOrWhiteSpace(_evolvedDescription) ? _evolvedDescription : _description;

        public CardEffectData[] EvolvedEffects => _evolvedEffects;

        public bool IsEvolving => _evolutionCondition != RelicEvolutionCondition.None && _evolvedEffects != null && _evolvedEffects.Length > 0;
    }
}
