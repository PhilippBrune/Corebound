using UnityEngine;

namespace Corebound.Gameplay.Waves
{
    // God-object refactor step 1 (OPEN_TOPICS 2.3): the pause/overlay seam, taken at its invariant
    // rather than at its UI.
    //
    // The reason that seam was chosen first is that it carries exactly one rule: **only one thing in
    // this project may ever hold Time.timeScale at 0, and it must always give it back.** That rule
    // was previously expressed as two bare assignments inside a 1600-line presenter, a hundred lines
    // apart, with twenty-five call sites in between able to route past them. Every timeScale bug
    // this project has had came from that shape - including the one on 2026-09-25 that froze the
    // autopilot in front of a tutorial beat, and the run-timer trap that followed it.
    //
    // Moving the UI out of WaveStatePresenter is the larger remaining job. Moving the invariant out
    // is the part that makes the rest safe to do incrementally: once ownership lives here, a later
    // extraction that forgets to restore timeScale fails loudly instead of silently freezing a run.
    public static class GameplayTimeScaleGuard
    {
        private static object _holder;

        public static bool IsHeld => _holder != null;

        // Who currently holds the freeze, for diagnostics. Named rather than boolean because "the
        // game is frozen and nothing on screen explains why" is the failure this is guarding.
        public static string HolderName => _holder != null ? _holder.ToString() : "none";

        // Returns false when someone else already holds it. Callers are expected to treat that as
        // "do not open" rather than forcing their way in - which is exactly what
        // WaveStatePresenter.TryShowTutorialBeat already does, and why a tutorial beat correctly
        // waits behind the pause menu.
        public static bool TryAcquire(object holder)
        {
            if (holder == null || (_holder != null && !ReferenceEquals(_holder, holder)))
            {
                return false;
            }

            _holder = holder;
            Time.timeScale = 0f;
            return true;
        }

        public static void Release(object holder)
        {
            if (_holder == null || !ReferenceEquals(_holder, holder))
            {
                return;
            }

            _holder = null;
            Time.timeScale = 1f;
        }

        // A scene change or a domain reload can destroy the holder without it releasing, which
        // would strand the next run at timeScale 0 with no way back. Called on run start.
        public static void ForceRelease()
        {
            _holder = null;
            Time.timeScale = 1f;
        }
    }
}
