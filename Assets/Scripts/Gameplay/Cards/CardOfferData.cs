using UnityEngine;

namespace Corebound.Gameplay.Cards
{
    [CreateAssetMenu(menuName = "Corebound/Gameplay/Cards/Card Offer Data", fileName = "CardOfferData")]
    public sealed class CardOfferData : ScriptableObject
    {
        [SerializeField] private int _optionCount = 3;
        [SerializeField] private float _commonWeight = 10f;
        [SerializeField] private float _rareWeight = 5f;
        [SerializeField] private float _epicWeight = 2f;
        [SerializeField] private float _legendaryWeight = 1f;
        [SerializeField] private int _legendaryMinimumSelectedCards = 3;

        // Open Topics 4, owner decision (a), 2026-09-30: the one knob for how much a card's tags
        // steer the offer. Positive favours cards whose tags the build already has (synergy),
        // negative favours tags it lacks (variety), 0 turns tags off and leaves rarity alone.
        // At +0.35 a card that fully matches the build is about twice as likely as one that
        // matches nothing; weapon cards are never tag-weighted.
        [SerializeField] [Range(-1f, 1f)] private float _tagAffinity = 0.35f;

        public int OptionCount => Mathf.Max(1, _optionCount);

        public float TagAffinity => Mathf.Clamp(_tagAffinity, -1f, 1f);

        public int LegendaryMinimumSelectedCards => Mathf.Max(0, _legendaryMinimumSelectedCards);

        public float ResolveWeight(CardRarity rarity)
        {
            switch (rarity)
            {
                case CardRarity.Rare:
                    return Mathf.Max(0f, _rareWeight);
                case CardRarity.Epic:
                    return Mathf.Max(0f, _epicWeight);
                case CardRarity.Legendary:
                    return Mathf.Max(0f, _legendaryWeight);
                default:
                    return Mathf.Max(0f, _commonWeight);
            }
        }
    }
}