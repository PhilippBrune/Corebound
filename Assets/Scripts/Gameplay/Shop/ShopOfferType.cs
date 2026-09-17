namespace Corebound.Gameplay.Shop
{
    public enum ShopOfferType
    {
        Card = 0,

        // Declared per Shop.md's own Gameplay example ("Cards, Tower Upgrades oder andere
        // Build-Verbesserungen"). Resolved end-to-end: ShopService.OnConfirmPressed applies it via
        // TowerService.TryApplyUpgradeFromShop(), which targets whichever slot is currently
        // selected in the Build context rather than needing its own ShopOfferData target field.
        // Authored as ShopOffer_TowerUpgrade.asset, wired into ShopOfferTable_Stage1.
        TowerUpgrade = 1
    }
}