using Corebound.Audio;
using UnityEditor;

namespace Corebound.EditorTools
{
    // The probe writes a line every two seconds for the whole session, which buried the autopilot's
    // own output in Editor.log. It is off by default now; this is how it comes back when an audio
    // question needs it (owner 2026-09-27: "AudioPlaybackProbe is still spamming Editor.log ->
    // disable it now").
    public static class AudioPlaybackProbeMenu
    {
        private const string MenuPath = "Corebound/Audio/Playback Probe";

        [MenuItem(MenuPath)]
        private static void Toggle()
        {
            var enabled = !EditorPrefs.GetBool(AudioPlaybackProbe.EnabledPrefsKey, false);
            EditorPrefs.SetBool(AudioPlaybackProbe.EnabledPrefsKey, enabled);
            UnityEngine.Debug.Log("[AudioProbe] " + (enabled
                ? "enabled - takes effect on the next Play."
                : "disabled."));
        }

        [MenuItem(MenuPath, true)]
        private static bool ToggleValidate()
        {
            Menu.SetChecked(MenuPath, EditorPrefs.GetBool(AudioPlaybackProbe.EnabledPrefsKey, false));
            return true;
        }
    }
}
