using UnityEngine;

namespace Corebound.Gameplay.Combat
{
    // Owner request 2026-09-21: "for debugging create a god mode where neither player nor core
    // loses life."
    //
    // The reason this exists is the vision's own open question: Section 10 targets a 60-70 minute
    // run, and no full five-stage run has ever been played end to end, so that figure is a guess.
    // Timing one requires surviving one, and surviving one requires either skill the harness does
    // not have or invulnerability. This is the cheaper half of that problem.
    //
    // Deliberately a damage veto rather than a health top-up: healing back to full after each hit
    // would still fire every damage event, every hit reaction, every Core-critical warning, and the
    // run would not be representative. Refusing the damage at the source keeps the run quiet.
    //
    // Off by default, toggled with F8, and it announces itself on screen the entire time it is on.
    // An invisible god mode is a way to accidentally balance the game around an invulnerable player.
    public static class DebugGodMode
    {
        public static bool IsEnabled { get; private set; }

        public static void Toggle()
        {
            IsEnabled = !IsEnabled;
            Debug.Log("[GodMode] " + (IsEnabled ? "ON - player and Core take no damage" : "OFF"));
        }

        public static void SetEnabled(bool enabled)
        {
            IsEnabled = enabled;
        }

        // Called from every damage entry point. Returns true when the hit should be discarded.
        public static bool ShouldIgnoreDamage(DamageSourceType sourceType)
        {
            // Only incoming damage is vetoed. The player's own damage must still land, or nothing
            // ever dies and the run cannot progress at all - which defeats the purpose.
            return IsEnabled && sourceType != DamageSourceType.Player;
        }
    }

    // Separate from the static above so the toggle key and the on-screen banner have somewhere to
    // live. Sits on CombatRuntime alongside the other debug aids.
    public sealed class DebugGodModeController : MonoBehaviour
    {
        private void Update()
        {
            var keyboard = UnityEngine.InputSystem.Keyboard.current;

            // Raw Keyboard polling rather than the InputReader, matching the F9/F10 debug
            // stage-select: a development aid must never consume or compete with a real gameplay
            // binding. Function keys are also the only keys that sit in the same physical place on
            // every keyboard layout, which matters on the QWERTZ keyboard this project is built on.
            if (keyboard != null && keyboard.f8Key.wasPressedThisFrame)
            {
                DebugGodMode.Toggle();
            }
        }

        private void OnGUI()
        {
            if (!DebugGodMode.IsEnabled || Corebound.DebugOverlay.Hidden)
            {
                return;
            }

            const float width = 260f;
            const float height = 24f;
            var rect = new Rect(8f, 8f, width, height);
            var previousColor = GUI.color;
            GUI.color = new Color(0.5f, 0f, 0f, 0.7f);
            GUI.Box(rect, GUIContent.none);
            GUI.color = previousColor;
            GUI.Label(new Rect(rect.x + 8f, rect.y + 3f, width - 16f, height), "GOD MODE  ·  F8 to disable");
        }
    }
}
