using System;

namespace Corebound.Gameplay.Relics
{
    // Relics.md's Technical Design names OnRelicOffered/OnRelicSelected/OnRelicApplied as the
    // intended event surface; RelicGranted covers OnRelicApplied's actual current trigger (relics
    // are auto-granted, not offered/selected from options - see CardService.TryGrantRandomRelic).
    // Offered/Selected stay unraised until a real multi-option choice flow exists.
    public static class RelicEvents
    {
        public static event Action<RelicGrantedEventArgs> RelicGranted;

        public static void RaiseRelicGranted(RelicGrantedEventArgs eventArgs)
        {
            RelicGranted?.Invoke(eventArgs);
        }
    }
}
