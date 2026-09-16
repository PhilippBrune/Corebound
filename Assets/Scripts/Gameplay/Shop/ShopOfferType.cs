namespace Corebound.Gameplay.Shop
{
    public enum ShopOfferType
    {
        Card = 0,

        // Declared per Shop.md's own Gameplay example ("Cards, Tower Upgrades oder andere
        // Build-Verbesserungen"); ShopService.OnConfirmPressed does not yet resolve this case
        // (there is no ShopOfferData field to name which tower/slot an upgrade offer targets),
        // so offers of this type are not sellable through the Shop yet - the enum member exists
        // so that follow-up work can extend the switch instead of first needing a data-model
        // change.
        TowerUpgrade = 1
    }
}