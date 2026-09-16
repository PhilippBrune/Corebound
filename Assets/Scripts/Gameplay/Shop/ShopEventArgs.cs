namespace Corebound.Gameplay.Shop
{
    public readonly struct ShopEventArgs
    {
        public ShopEventArgs(string shopId, string offerId, string label, int offerIndex, int cost, bool purchased)
        {
            ShopId = shopId;
            OfferId = offerId;
            Label = label;
            OfferIndex = offerIndex;
            Cost = cost;
            Purchased = purchased;
        }

        public string ShopId { get; }

        public string OfferId { get; }

        public string Label { get; }

        public int OfferIndex { get; }

        public int Cost { get; }

        public bool Purchased { get; }
    }
}