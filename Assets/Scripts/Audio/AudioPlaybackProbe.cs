#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Corebound.Audio
{
    // Playtest 2026-09-22: "after death there are still 2 sounds playing in the main menu. Add an
    // output log which polls every X seconds which audio file is currently playing."
    //
    // Agents cannot hear, so this is how an audio report becomes evidence. Every PollSeconds it
    // lists every AudioSource that is playing - clip, GameObject, scene, loop, volume - and logs
    // behind [AudioProbe] when the set changed, plus a heartbeat every HeartbeatSeconds while
    // anything plays. PlayOneShot sounds show as the source's own clip (often none), so a playing
    // source with "(one-shot)" is a PlayOneShot in progress.
    //
    // Editor and development builds only; a release build does not contain it.
    public sealed class AudioPlaybackProbe : MonoBehaviour
    {
        private const float PollSeconds = 2f;
        private const float HeartbeatSeconds = 10f;

        private readonly StringBuilder _builder = new StringBuilder(512);
        private string _lastReport = string.Empty;
        private float _pollTimer;
        private float _heartbeatTimer;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            var host = new GameObject("AudioPlaybackProbe");
            DontDestroyOnLoad(host);
            host.hideFlags = HideFlags.HideInHierarchy;
            host.AddComponent<AudioPlaybackProbe>();
            SceneManager.activeSceneChanged += (from, to) => Debug.Log("[AudioProbe] active scene -> " + to.name);
        }

        private void Update()
        {
            _pollTimer += Time.unscaledDeltaTime;
            _heartbeatTimer += Time.unscaledDeltaTime;

            if (_pollTimer < PollSeconds)
            {
                return;
            }

            _pollTimer = 0f;
            var report = BuildReport();

            if (report != _lastReport || (report.Length > 0 && _heartbeatTimer >= HeartbeatSeconds))
            {
                _heartbeatTimer = 0f;
                Debug.Log(report.Length > 0 ? "[AudioProbe] playing: " + report : "[AudioProbe] silent");
            }

            _lastReport = report;
        }

        private string BuildReport()
        {
            _builder.Clear();
            var sources = FindObjectsByType<AudioSource>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            var entries = new List<string>();

            for (var index = 0; index < sources.Length; index++)
            {
                var source = sources[index];

                if (source == null || !source.isPlaying)
                {
                    continue;
                }

                entries.Add((source.clip != null ? source.clip.name : "(one-shot)")
                    + " @" + source.gameObject.name
                    + " [" + source.gameObject.scene.name + "]"
                    + (source.loop ? " loop" : string.Empty)
                    + " vol=" + source.volume.ToString("0.00"));
            }

            // Sorted, so the same set in a different find order is not reported as a change.
            entries.Sort(System.StringComparer.Ordinal);

            for (var index = 0; index < entries.Count; index++)
            {
                _builder.Append(index > 0 ? " | " : string.Empty).Append(entries[index]);
            }

            return _builder.ToString();
        }
    }
}
#endif
