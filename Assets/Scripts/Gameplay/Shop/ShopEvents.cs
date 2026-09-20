using System;

namespace Corebound.Gameplay.Shop
{
    public static class ShopEvents
    {
        public static event Action<ShopEventArgs> ShopOpened;

        public static event Action<ShopEventArgs> ShopOfferSelected;

        public static event Action<ShopEventArgs> PurchaseRequested;

        public static event Action<ShopEventArgs> PurchaseSucceeded;

        // Not in Shop.md's event list, but its Audio section requires an insufficient-funds
        // sound, and no existing event fires on a refused purchase - PurchaseRequested is only
        // raised once the payment has already gone through.
        public static event Action<ShopEventArgs> PurchaseDeclined;

        public static event Action<ShopEventArgs> ShopClosed;

        public static void RaiseShopOpened(ShopEventArgs eventArgs)
        {
            ShopOpened?.Invoke(eventArgs);
        }

        public static void RaiseShopOfferSelected(ShopEventArgs eventArgs)
        {
            ShopOfferSelected?.Invoke(eventArgs);
        }

        public static void RaisePurchaseRequested(ShopEventArgs eventArgs)
        {
            PurchaseRequested?.Invoke(eventArgs);
        }

        public static void RaisePurchaseSucceeded(ShopEventArgs eventArgs)
        {
            PurchaseSucceeded?.Invoke(eventArgs);
        }

        public static void RaisePurchaseDeclined(ShopEventArgs eventArgs)
        {
            PurchaseDeclined?.Invoke(eventArgs);
        }

        public static void RaiseShopClosed(ShopEventArgs eventArgs)
        {
            ShopClosed?.Invoke(eventArgs);
        }
    }
}