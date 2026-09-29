using UnityEngine;
using UnityEngine.SceneManagement;

namespace Corebound.SaveSystem
{
    // Total playtime for the Records screen, accumulated continuously.
    //
    // Playtest 2026-09-29: "playtime in Records is shown as 0:00 - I would expect a real time
    // there." The first version only added a run's time when the run *finished* by winning or
    // dying, so an abandoned run, a run still in progress, and every minute spent in menus added
    // nothing - and every run finished before the feature existed had added nothing either. The
    // counter was correct and useless.
    //
    // Playtime now means what it means on a storefront: time the game was running with a profile
    // active, menus included. Unscaled, so the pause menu counts too. Flushed to the active
    // profile's statistics every FlushSeconds and on scene change, profile change and quit, so a
    // crash loses at most half a minute.
    public sealed class PlaytimeTracker : MonoBehaviour
    {
        private const float FlushSeconds = 30f;

        private static PlaytimeTracker _instance;
        private float _pendingSeconds;
        private float _sinceFlush;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (_instance != null)
            {
                return;
            }

            var host = new GameObject("PlaytimeTracker");
            DontDestroyOnLoad(host);
            host.hideFlags = HideFlags.HideInHierarchy;
            _instance = host.AddComponent<PlaytimeTracker>();
        }

        private void OnEnable()
        {
            SceneManager.activeSceneChanged += OnActiveSceneChanged;
            ProfileSelection.ActiveProfileChanged += Flush;
        }

        private void OnDisable()
        {
            SceneManager.activeSceneChanged -= OnActiveSceneChanged;
            ProfileSelection.ActiveProfileChanged -= Flush;
        }

        private void Update()
        {
            var delta = Time.unscaledDeltaTime;

            // A frame this long is a breakpoint, an alt-tab stall or a debugger pause, not play.
            if (delta <= 0f || delta > 5f)
            {
                return;
            }

            _pendingSeconds += delta;
            _sinceFlush += delta;

            if (_sinceFlush >= FlushSeconds)
            {
                Flush();
            }
        }

        private void OnApplicationQuit()
        {
            Flush();
        }

        private void OnActiveSceneChanged(Scene from, Scene to)
        {
            Flush();
        }

        // Written against whichever profile is active *now*. On a profile switch this runs from
        // the change event, after the switch, so a few seconds may be credited to the new profile
        // rather than the old one - an acceptable error for a figure measured in hours.
        private void Flush()
        {
            _sinceFlush = 0f;

            if (_pendingSeconds <= 0f)
            {
                return;
            }

            var saveManager = LocalProfileSaveManager.Instance;

            if (saveManager == null)
            {
                return;
            }

            var statistics = saveManager.LoadStatisticsSaveData(new StatisticsSaveData());

            if (statistics == null)
            {
                return;
            }

            statistics.AddPlaytimeSeconds(_pendingSeconds);
            saveManager.SaveStatisticsSaveData(statistics);
            _pendingSeconds = 0f;
        }
    }
}
