using System;

namespace Corebound.Gameplay.Factory
{
    public static class FactoryEvents
    {
        public static event Action<ProductionTriggeredEventArgs> ProductionTriggered;

        public static event Action<ProductionBlockedEventArgs> ProductionBlocked;

        public static event Action<ResourceGeneratedEventArgs> ResourceGenerated;

        public static void RaiseProductionTriggered(ProductionTriggeredEventArgs eventArgs)
        {
            ProductionTriggered?.Invoke(eventArgs);
        }

        public static void RaiseProductionBlocked(ProductionBlockedEventArgs eventArgs)
        {
            ProductionBlocked?.Invoke(eventArgs);
        }

        public static void RaiseResourceGenerated(ResourceGeneratedEventArgs eventArgs)
        {
            ResourceGenerated?.Invoke(eventArgs);
        }
    }
}
