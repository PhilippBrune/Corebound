using System.Collections.Generic;
using UnityEngine;

namespace Corebound.Input
{
    // Player.md, interaction priority: blocking UI -> tower slot -> Core repair -> Core guard ->
    // reward/shop -> pickups. Three systems subscribed to InteractPressed independently, so one
    // press near the Core during an Intermission could buy a repair *and* start a guard hold, and
    // a tower slot within the Core's repair radius made it build as well. Spec review 2026-09-22,
    // finding A1.
    //
    // Each handler still subscribes to the input event; it just asks the router to dispatch instead
    // of acting directly. The router collects every registered handler once per frame and gives the
    // press to exactly one - the highest priority that can take it - so the outcome does not depend
    // on subscription order.
    public enum InteractPriority
    {
        CoreGuard = 10,

        // HP recovery (2026-09-28) shares the Core's position and verb, so it needs a rank of its
        // own: below repairing the Core, because losing the Core ends the run and a player standing
        // at a damaged Core with damaged health should mend the Core first.
        PlayerRepair = 15,
        CoreRepair = 20,
        TowerSlot = 30
    }

    public interface IInteractHandler
    {
        InteractPriority InteractPriority { get; }

        // May this handler take the press right now? Checked in priority order.
        bool CanHandleInteract { get; }

        void HandleInteract();
    }

    public static class InteractRouter
    {
        private static readonly List<IInteractHandler> Handlers = new List<IInteractHandler>();
        private static int _dispatchedFrame = -1;

        // True while the hold that is currently down was already spent on something else - the
        // Core guard reads this so that a press that built a tower does not also raise the shield.
        public static bool HoldConsumed { get; private set; }

        public static void Register(IInteractHandler handler)
        {
            if (handler != null && !Handlers.Contains(handler))
            {
                Handlers.Add(handler);
            }
        }

        public static void Unregister(IInteractHandler handler)
        {
            Handlers.Remove(handler);
        }

        // Called by every handler from its InteractPressed subscription. The first call in a frame
        // decides; later calls in the same frame do nothing.
        public static void Dispatch()
        {
            if (_dispatchedFrame == Time.frameCount)
            {
                return;
            }

            _dispatchedFrame = Time.frameCount;
            IInteractHandler winner = null;

            for (var index = 0; index < Handlers.Count; index++)
            {
                var candidate = Handlers[index];

                if (candidate == null || !candidate.CanHandleInteract)
                {
                    continue;
                }

                if (winner == null || candidate.InteractPriority > winner.InteractPriority)
                {
                    winner = candidate;
                }
            }

            if (winner == null)
            {
                return;
            }

            HoldConsumed = winner.InteractPriority > InteractPriority.CoreGuard;
            winner.HandleInteract();
        }

        // Called when Interact is released, so the next hold starts clean.
        public static void ReleaseHold()
        {
            HoldConsumed = false;
        }

        // Play mode can start with stale statics in the Editor (domain reload off, or a handler
        // destroyed with the scene while still registered).
        public static void Clear()
        {
            Handlers.Clear();
            _dispatchedFrame = -1;
            HoldConsumed = false;
        }
    }
}
