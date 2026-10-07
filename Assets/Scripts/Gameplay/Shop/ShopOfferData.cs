using Corebound.Gameplay.Cards;
using UnityEngine;

namespace Corebound.Gameplay.Shop
{
    [CreateAssetMenu(menuName = "Corebound/Gameplay/Shop/Shop Offer Data", fileName = "ShopOfferData")]
    public sealed class ShopOfferData : ScriptableObject
    {
        [SerializeField] private string _offerId = "shop_offer";
        [SerializeField] private ShopOfferType _offerType = ShopOfferType.Card;
        [SerializeField] private CardData _cardData;
        [SerializeField] private int _cost = 8;
        [SerializeField] private string _labelOverride = string.Empty;
        [SerializeField] [TextArea(2, 3)] private string _descriptionOverride = string.Empty;
        [SerializeField] private int _minStageIndex = 1;
        [SerializeField] private int _maxStageIndex = 99;

        // Shop.md asks for "Pools, Gewichtungen und Stage-spezifische Verfügbarkeiten".
        // MinStageIndex/MaxStageIndex were the availability half; this is the weighting half.
        // 1 is the neutral weight, so every existing asset keeps its current odds relative to
        // every other existing asset. 0 makes an offer unreachable without deleting it.
        [SerializeField] private float _selectionWeight = 1f;

        public string OfferId => _offerId;

        public ShopOfferType OfferType => _offerType;

        public CardData CardData => _cardData;

        public int Cost => Mathf.Max(0, _cost);

        public string LabelOverride => _labelOverride;

        public string DescriptionOverride => _descriptionOverride;

        public int MinStageIndex => Mathf.Max(1, _minStageIndex);

        public int MaxStageIndex => Mathf.Max(MinStageIndex, _maxStageIndex);

        public float SelectionWeight => Mathf.Max(0f, _selectionWeight);
    }
}