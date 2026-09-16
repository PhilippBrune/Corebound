using UnityEngine;

namespace Corebound.Gameplay.Shop
{
    [CreateAssetMenu(menuName = "Corebound/Gameplay/Shop/Shop Data", fileName = "ShopData")]
    public sealed class ShopData : ScriptableObject
    {
        [SerializeField] private string _shopId = "shop_stage_01";
        [SerializeField] private string _displayName = "Foundry Exchange";
        [SerializeField] private int[] _openAfterWaveNumbers = { 3 };
        [SerializeField] private float _stagePriceMultiplier = 1f;
        [SerializeField] private ShopOfferTableData _offerTableData;
        [SerializeField] private ShopPresentationData _presentationData;

        public string ShopId => _shopId;

        public string DisplayName => _displayName;

        public int[] OpenAfterWaveNumbers => _openAfterWaveNumbers;

        public float StagePriceMultiplier => Mathf.Max(0.1f, _stagePriceMultiplier);

        public ShopOfferTableData OfferTableData => _offerTableData;

        public ShopPresentationData PresentationData => _presentationData;

        public bool ShouldOpenAfterWave(int completedWaveNumber)
        {
            if (_openAfterWaveNumbers == null || _openAfterWaveNumbers.Length == 0)
            {
                return false;
            }

            for (var index = 0; index < _openAfterWaveNumbers.Length; index++)
            {
                if (_openAfterWaveNumbers[index] == completedWaveNumber)
                {
                    return true;
                }
            }

            return false;
        }
    }
}