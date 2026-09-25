using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace Corebound.EditorTools
{
    // Controller testing without a controller.
    //
    // CLAUDE.md records that synthetic OS input (SendInput/keybd_event) never reaches Unity's Input
    // System, which is true: those drive the Editor's own IMGUI, not the device layer. A *virtual
    // device* is the other way round - `InputSystem.AddDevice<Gamepad>()` creates a real device as
    // far as the Input System is concerned, and `QueueStateEvent` feeds it real state. Actions,
    // bindings, control schemes and device-detection all run exactly as they do for plugged-in
    // hardware, which is the part the autopilot's direct-to-reader surface cannot exercise.
    //
    // This is what makes it worth having: the autopilot proves the *game* works, the virtual pad
    // proves the *bindings* work - that Aim is on the right stick, that the shoulder buttons switch
    // tabs, that the reader flips to gamepad mode on stick deflection and back on mouse movement.
    public static class VirtualGamepadDriver
    {
        private static Gamepad _pad;

        public static bool IsActive => _pad != null && _pad.added;

        public static Gamepad Pad => _pad;

        [MenuItem("Corebound/Virtual Gamepad/Add")]
        public static Gamepad Add()
        {
            if (IsActive)
            {
                return _pad;
            }

            _pad = InputSystem.AddDevice<Gamepad>("CoreboundVirtualPad");
            Debug.Log("[VirtualPad] added: " + _pad.displayName + " (deviceId " + _pad.deviceId + ")");
            return _pad;
        }

        [MenuItem("Corebound/Virtual Gamepad/Remove")]
        public static void Remove()
        {
            if (_pad == null)
            {
                return;
            }

            InputSystem.RemoveDevice(_pad);
            _pad = null;
            Debug.Log("[VirtualPad] removed");
        }

        // One state push. Every field is optional, so a caller can move a stick without touching
        // the buttons it is already holding.
        public static void Send(Vector2 leftStick = default, Vector2 rightStick = default, GamepadButton[] buttons = null, float rightTrigger = 0f)
        {
            if (!IsActive)
            {
                Add();
            }

            var state = new GamepadState
            {
                leftStick = leftStick,
                rightStick = rightStick,
                rightTrigger = rightTrigger,
            };

            if (buttons != null)
            {
                for (var index = 0; index < buttons.Length; index++)
                {
                    state = state.WithButton(buttons[index]);
                }
            }

            InputSystem.QueueStateEvent(_pad, state);
        }

        // A button needs a frame down and a frame up, or "performed" never fires.
        public static void Tap(GamepadButton button)
        {
            Send(buttons: new[] { button });
            InputSystem.Update();
            Send();
            InputSystem.Update();
        }

        public static void Hold(Vector2 leftStick, Vector2 rightStick, bool fire)
        {
            Send(leftStick, rightStick, null, fire ? 1f : 0f);
        }
    }
}
