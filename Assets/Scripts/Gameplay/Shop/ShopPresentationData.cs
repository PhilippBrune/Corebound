using Corebound.UI;
using UnityEngine;

namespace Corebound.Gameplay.Shop
{
    [CreateAssetMenu(menuName = "Corebound/Gameplay/Shop/Shop Presentation Data", fileName = "ShopPresentationData")]
    public sealed class ShopPresentationData : ScriptableObject
    {
        [SerializeField] private string _title = "Foundry Exchange";
        [SerializeField] private string _titleDe = "Schmiedebörse";
        [SerializeField] [TextArea(2, 3)] private string _summary = "Spend Scrap on one targeted upgrade before tower placement.";
        [SerializeField] [TextArea(2, 3)] private string _summaryDe = "Gib Schrott für ein gezieltes Upgrade aus, bevor du Towers platzierst.";
        [SerializeField] private string _footerHint = "Confirm buys. Cancel skips the shop.";
        [SerializeField] private string _footerHintDe = "Bestätigen kauft. Abbrechen überspringt den Shop.";

        public string Title => LocalizationService.Resolve(_title, _titleDe);

        public string Summary => LocalizationService.Resolve(_summary, _summaryDe);

        public string FooterHint => LocalizationService.Resolve(_footerHint, _footerHintDe);
    }
}