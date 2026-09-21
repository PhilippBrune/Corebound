using System;

namespace Corebound.Gameplay.Factory
{
    public static class FactoryEvents
    {
        public static event Action<ProductionTriggeredEventArgs> ProductionTriggered;

        public static event Action<ProductionBlockedEventArgs> ProductionBlocked;

        public static event Action<ResourceGeneratedEventArgs> ResourceGenerated;

        // Section 5.5 requires the player be able to *recognise* that a system is working. A
        // chain climbing a tier is the one moment in the Factory's life that is worth announcing,
        // and it is what makes the amplifier's otherwise invisible payout legible.
        public static event Action<ChainTierChangedEventArgs> ChainTierChanged;

        public static void RaiseProductionTriggered(ProductionTriggeredEventArgs eventArgs)
        {
            ProductionTriggered?.Invoke(eventArgs);
        }

        public static void RaiseProductionBlocked(ProductionBlockedEventArgs eventArgs)
        {
            ProductionBlocked?.Invoke(eventArgs);
        }

        public static void RaiseChainTierChanged(ChainTierChangedEventArgs eventArgs)
        {
            ChainTierChanged?.Invoke(eventArgs);
        }

        public static void RaiseResourceGenerated(ResourceGeneratedEventArgs eventArgs)
        {
            ResourceGenerated?.Invoke(eventArgs);
        }
    }
}
