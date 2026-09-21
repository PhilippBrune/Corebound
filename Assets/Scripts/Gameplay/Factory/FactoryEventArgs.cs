namespace Corebound.Gameplay.Factory
{
    public readonly struct ProductionTriggeredEventArgs
    {
        public ProductionTriggeredEventArgs(string chainId, string nodeId, ProductionTriggerType triggerType)
        {
            ChainId = chainId;
            NodeId = nodeId;
            TriggerType = triggerType;
        }

        public string ChainId { get; }

        public string NodeId { get; }

        public ProductionTriggerType TriggerType { get; }
    }

    public readonly struct ProductionBlockedEventArgs
    {
        public ProductionBlockedEventArgs(string chainId, string nodeId, string reason)
        {
            ChainId = chainId;
            NodeId = nodeId;
            Reason = reason;
        }

        public string ChainId { get; }

        public string NodeId { get; }

        // Short machine-readable cause ("no_producer_built", "no_wallet"), not player-facing text.
        public string Reason { get; }
    }

    public readonly struct ResourceGeneratedEventArgs
    {
        public ResourceGeneratedEventArgs(string chainId, string nodeId, string nodeDisplayName, int amount, ProductionTriggerType triggerType)
        {
            ChainId = chainId;
            NodeId = nodeId;
            NodeDisplayName = nodeDisplayName;
            Amount = amount;
            TriggerType = triggerType;
        }

        public string ChainId { get; }

        public string NodeId { get; }

        public string NodeDisplayName { get; }

        public int Amount { get; }

        public ProductionTriggerType TriggerType { get; }
    }

    public readonly struct ChainTierChangedEventArgs
    {
        public ChainTierChangedEventArgs(string chainId, string tierName, int previousTier, int currentTier, int activeNodeCount)
        {
            ChainId = chainId;
            TierName = tierName;
            PreviousTier = previousTier;
            CurrentTier = currentTier;
            ActiveNodeCount = activeNodeCount;
        }

        public string ChainId { get; }

        public string TierName { get; }

        public int PreviousTier { get; }

        public int CurrentTier { get; }

        public int ActiveNodeCount { get; }
    }
}
