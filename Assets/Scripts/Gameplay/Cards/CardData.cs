using Corebound.Gameplay.Weapons;
using Corebound.UI;
using UnityEngine;

namespace Corebound.Gameplay.Cards
{
    [CreateAssetMenu(menuName = "Corebound/Gameplay/Cards/Card Data", fileName = "CardData")]
    public sealed class CardData : ScriptableObject
    {
        [SerializeField] private string _cardId = "card_default";
        [SerializeField] private string _displayName = "Card";
        [SerializeField] private string _displayNameDe = string.Empty;
        [SerializeField] [TextArea(2, 3)] private string _description = string.Empty;
        [SerializeField] [TextArea(2, 3)] private string _descriptionDe = string.Empty;
        [SerializeField] private CardRarity _rarity = CardRarity.Common;
        [SerializeField] private string _requiredUnlockId = string.Empty;
        [SerializeField] private CardTagType[] _tags;
        [SerializeField] private CardEffectData[] _effects;

        // Weapons.md's modifier hooks. CardEffectData covers the three flat multipliers that
        // predate it; these assets are how a card reaches Crit, Crit damage, Multishot or Status
        // chance, and how a modifier can be scoped to one weapon id.
        [SerializeField] private WeaponModifierData[] _weaponModifiers;
        [SerializeField] private bool _stackable;

        // WeaponLoadout.md, Acquisition: weapons are offered the same ways cards are. A card with a
        // weapon here is not a passive - picking it equips the weapon (at this card's rarity as its
        // tier) instead of joining the selected cards.
        [SerializeField] private WeaponData _grantedWeapon;

        public string CardId => _cardId;

        public string DisplayName => LocalizationService.Resolve(_displayName, _displayNameDe);

        public string Description => LocalizationService.Resolve(_description, _descriptionDe);

        public CardRarity Rarity => _rarity;

        public WeaponData GrantedWeapon => _grantedWeapon;

        public bool IsWeaponCard => _grantedWeapon != null;

        public string RequiredUnlockId => _requiredUnlockId;

        // Authored per Docs/Specs/Cards.md's synergy/archetype system. Drawn as icons on the
        // card, and since 2026-09-30 read by CardService's offer weighting
        // (CardOfferData.TagAffinity), so a card's tags are part of how often it is offered.
        public CardTagType[] Tags => _tags;

        public CardEffectData[] Effects => _effects;

        public WeaponModifierData[] WeaponModifiers => _weaponModifiers;

        // Owner decision 2026-09-21: "all selected cards can stack". The per-card opt-in is kept
        // as data so a future card can deliberately be unique, but it no longer gates anything -
        // every card is stackable unless this is explicitly turned back into a real check. Leaving
        // the field in place rather than deleting it keeps the 30 authored card assets loading
        // unchanged and makes the decision reversible in one line.
        public bool Stackable => true;

        // The authored value, kept for tooling and for a future per-card exception.
        public bool AuthoredStackable => _stackable;
    }
}