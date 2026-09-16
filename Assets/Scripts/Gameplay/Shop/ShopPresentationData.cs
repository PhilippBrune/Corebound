using UnityEngine;

namespace Corebound.Gameplay.Shop
{
    [CreateAssetMenu(menuName = "Corebound/Gameplay/Shop/Shop Presentation Data", fileName = "ShopPresentationData")]
    public sealed class ShopPresentationData : ScriptableObject
    {
        [SerializeField] private string _title = "Foundry Exchange";
        [SerializeField] [TextArea(2, 3)] private string _summary = "Spend Scrap on one targeted upgrade before tower placement.";
        [SerializeField] private string _footerHint = "Confirm buys. Cancel skips the shop.";

        public string Title => _title;

        public string Summary => _summary;

        public string FooterHint => _footerHint;
    }
}