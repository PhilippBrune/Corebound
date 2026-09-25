using System.Collections;
using System.Text;
using Corebound.Input;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace Corebound.EditorTools
{
    // Proves - or disproves - that a virtual gamepad reaches the game's own InputActions. Run it
    // from Corebound > Virtual Gamepad > Self Test while in Play Mode; it presses one control at a
    // time and reports which semantic events the reader raised.
    //
    // The result matters beyond curiosity: if this passes, controller-only paths (aim, tab
    // switching, device detection) can be tested without a human holding a pad.
    [InitializeOnLoad]
    public static class VirtualGamepadSelfTest
    {
        // File trigger as well as a menu item: a menu item cannot be clicked from a script, and the
        // most interesting moment to run this is *during* Play, when the reader is live.
        private const string RequestFileName = "VirtualPadRequest.txt";
        private static double _nextPoll;

        static VirtualGamepadSelfTest()
        {
            EditorApplication.update += PollRequestFile;
        }

        private static void PollRequestFile()
        {
            if (EditorApplication.timeSinceStartup < _nextPoll)
            {
                return;
            }

            _nextPoll = EditorApplication.timeSinceStartup + 1.0;
            var path = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), RequestFileName);

            if (!System.IO.File.Exists(path))
            {
                return;
            }

            var contents = System.IO.File.ReadAllText(path).Trim();

            if (contents.Length == 0 || contents == "none")
            {
                return;
            }

            System.IO.File.WriteAllText(path, "none");

            if (contents.StartsWith("test"))
            {
                Run();
            }
        }

        [MenuItem("Corebound/Virtual Gamepad/Self Test")]
        public static void Run()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[VirtualPad] Self Test needs Play Mode - the reader only subscribes while playing.");
                return;
            }

            var reader = Object.FindFirstObjectByType<CoreboundInputReader>();

            if (reader == null)
            {
                Debug.LogWarning("[VirtualPad] no CoreboundInputReader in the loaded scenes.");
                return;
            }

            EditorCoroutineRunner.Start(RunRoutine(reader));
        }

        private static IEnumerator RunRoutine(CoreboundInputReader reader)
        {
            var report = new StringBuilder(512);
            var confirms = 0;
            var cancels = 0;
            var previous = 0;
            var next = 0;
            var interacts = 0;

            void OnConfirm() => confirms++;
            void OnCancel() => cancels++;
            void OnPrevious() => previous++;
            void OnNext() => next++;
            void OnInteract() => interacts++;

            reader.ConfirmPressed += OnConfirm;
            reader.CancelPressed += OnCancel;
            reader.PreviousPressed += OnPrevious;
            reader.NextPressed += OnNext;
            reader.InteractPressed += OnInteract;

            VirtualGamepadDriver.Add();
            yield return null;

            VirtualGamepadDriver.Tap(GamepadButton.South);
            yield return null;
            VirtualGamepadDriver.Tap(GamepadButton.East);
            yield return null;
            VirtualGamepadDriver.Tap(GamepadButton.LeftShoulder);
            yield return null;
            VirtualGamepadDriver.Tap(GamepadButton.RightShoulder);
            yield return null;

            // Stick deflection should both aim and flip the reader into gamepad mode.
            VirtualGamepadDriver.Hold(new Vector2(0f, 1f), new Vector2(1f, 0f), true);
            yield return null;
            yield return null;
            var aim = reader.Aim;
            var move = reader.Move;
            var usingGamepad = reader.IsUsingGamepad;
            VirtualGamepadDriver.Hold(Vector2.zero, Vector2.zero, false);
            yield return null;

            reader.ConfirmPressed -= OnConfirm;
            reader.CancelPressed -= OnCancel;
            reader.PreviousPressed -= OnPrevious;
            reader.NextPressed -= OnNext;
            reader.InteractPressed -= OnInteract;

            report.Append("[VirtualPad] self test: ");
            report.Append("Confirm=").Append(confirms);
            report.Append(" Cancel=").Append(cancels);
            report.Append(" Previous=").Append(previous);
            report.Append(" Next=").Append(next);
            report.Append(" Interact=").Append(interacts);
            report.Append(" | Move=").Append(move);
            report.Append(" Aim=").Append(aim);
            report.Append(" IsUsingGamepad=").Append(usingGamepad);

            var reachedActions = confirms + cancels + previous + next + interacts > 0 || move.sqrMagnitude > 0.1f;
            report.Append(reachedActions ? "  => PASS, virtual input reaches the real bindings" : "  => FAIL, nothing arrived");
            Debug.Log(report.ToString());
        }
    }

    // Minimal editor coroutine pump: the self test needs frames to pass between state pushes, and
    // EditorApplication.update is the only clock available from a menu item.
    public static class EditorCoroutineRunner
    {
        public static void Start(IEnumerator routine)
        {
            void Step()
            {
                if (routine.MoveNext())
                {
                    return;
                }

                EditorApplication.update -= Step;
            }

            EditorApplication.update += Step;
        }
    }
}
