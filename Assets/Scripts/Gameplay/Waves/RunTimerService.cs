using UnityEngine;

namespace Corebound.Gameplay.Waves
{
    // Two clocks for one run (D1 option 3, owner 2026-09-28).
    //
    // ElapsedActiveSeconds is unpaused play: it accumulates Time.deltaTime, which is already 0 while
    // an overlay holds Time.timeScale at 0, so pause, tutorial beats and the reward screen stop it
    // without any special case. ElapsedRealSeconds is wall clock and must NOT be an accumulation of
    // deltaTime for exactly that reason - it uses unscaledDeltaTime instead. Getting that backwards
    // is the same mistake that froze the autopilot in front of a tutorial beat on 2026-09-25.
    //
    // The gap between the two is itself the interesting number: a large one means the run is being
    // spent in menus, which is what the vision's 60-70 minute target needs measured against.
    //
    // The Shop and the build browser run at timeScale 1, so they count as active play. That is
    // deliberate - build decisions are part of a run - and it does mean a slow shopper's run reads
    // as longer than a fast one's with identical combat.
    public sealed class RunTimerService : MonoBehaviour
    {
        private bool _isRunning;

        public float ElapsedActiveSeconds { get; private set; }

        public float ElapsedRealSeconds { get; private set; }

        public System.DateTime StartedUtc { get; private set; } = System.DateTime.UtcNow;

        public void BeginRun()
        {
            ElapsedActiveSeconds = 0f;
            ElapsedRealSeconds = 0f;
            StartedUtc = System.DateTime.UtcNow;
            _isRunning = true;
        }

        public void StopRun()
        {
            _isRunning = false;
        }

        // Restores both clocks when a run is resumed from a save, so a continued run's total is the
        // whole run rather than only the part played since the last load.
        public void Restore(float activeSeconds, float realSeconds, System.DateTime startedUtc)
        {
            ElapsedActiveSeconds = Mathf.Max(0f, activeSeconds);
            ElapsedRealSeconds = Mathf.Max(0f, realSeconds);
            StartedUtc = startedUtc;
            _isRunning = true;
        }

        private void Update()
        {
            if (!_isRunning)
            {
                return;
            }

            ElapsedActiveSeconds += Time.deltaTime;
            ElapsedRealSeconds += Time.unscaledDeltaTime;
        }

        public static string Format(float seconds)
        {
            var total = Mathf.Max(0, Mathf.FloorToInt(seconds));
            var hours = total / 3600;
            var minutes = total % 3600 / 60;
            var secs = total % 60;

            return hours > 0
                ? string.Format("{0}:{1:00}:{2:00}", hours, minutes, secs)
                : string.Format("{0}:{1:00}", minutes, secs);
        }
    }
}
