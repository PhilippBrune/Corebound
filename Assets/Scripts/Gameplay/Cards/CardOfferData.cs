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

        public int OptionCount => Mathf.Max(1, _optionCount);

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