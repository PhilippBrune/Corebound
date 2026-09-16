using System;

namespace Corebound.Gameplay.Shop
{
    public static class ShopEvents
    {
        public static event Action<ShopEventArgs> ShopOpened;

        public static event Action<ShopEventArgs> ShopOfferSelected;

        public static event Action<ShopEventArgs> PurchaseRequested;

        public static event Action<ShopEventArgs> PurchaseSucceeded;

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

        public static void RaiseShopClosed(ShopEventArgs eventArgs)
        {
            ShopClosed?.Invoke(eventArgs);
        }
    }
}