using UnityEngine;

namespace Corebound.Gameplay.Shop
{
    [CreateAssetMenu(menuName = "Corebound/Gameplay/Shop/Shop Offer Table Data", fileName = "ShopOfferTableData")]
    public sealed class ShopOfferTableData : ScriptableObject
    {
        [SerializeField] private ShopOfferData[] _offers;
        [SerializeField] private int _maxOfferCount = 3;

        public ShopOfferData[] Offers => _offers;

        public int MaxOfferCount => Mathf.Max(1, _maxOfferCount);
    }
}