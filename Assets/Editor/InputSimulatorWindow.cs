using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace Corebound.EditorTools
{
    // Standing limitation this whole project (see .claude/skills/unity-editor-control): the new
    // Input System reads Raw Input directly from the OS/driver during Play Mode, so OS-level
    // synthetic input (SendInput/keybd_event/mouse_event, what the Win32 automation this session
    // otherwise relies on uses) never reaches Keyboard.current/Mouse.current - WASD movement,
    // firing, MainMenu button clicks, none of it. That blocked live-verifying several fixes this
    // session (shop-after-wave-3, the MainMenu -> New Run click path, general combat).
    //
    // KNOWN LIMITATION (2026-09-17), not yet solved - read before spending more time here:
    // InputSystem.QueueStateEvent() genuinely does update the low-level device buffer -
    // Keyboard.current.enterKey.isPressed correctly flips true/false right after
    // QueueStateEvent()+InputSystem.Update(), confirmed repeatedly, including continuously
    // re-sending every tick for the whole hold (see Tick()) and forcing
    // InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView so Game View
    // OS focus can't be the excuse. Despite that, a normal MonoBehaviour's own Update() during the
    // SAME Play session polling that exact same property back never once observed it as pressed,
    // and the bound InputAction (CoreboundInputReader's Confirm action) never fires .performed and
    // never reads a non-zero ReadValue<float>() either - verified with direct diagnostic logging
    // inside CoreboundInputReader itself (since reverted - don't leave diagnostic code in
    // gameplay scripts). Ruled out: stale/corrupted InputActionMap state (persists across a full
    // Editor process restart), wrong action map enabled (ApplyContext logs confirmed ui=True),
    // wrong InputActionAsset reference (GUIDs cross-checked against the scene), event coalescing
    // (continuous re-send didn't help). Root cause not found - something specific to how a
    // Player-loop MonoBehaviour's Keyboard.current view reconciles with Editor-queued state
    // during Play-in-Editor, deeper than this session had budget to keep chasing blind.
    //
    // What this tool IS still good for: confirming device-level state injection works at all,
    // and as a starting point if picked back up - the mouse path (ClickMouse) has NOT been
    // separately verified against a bound InputAction the way the keyboard path was, so it may or
    // may not have the same issue.
    //
    // InputSystem.QueueStateEvent() is Unity's own supported mechanism for simulated input (the
    // same one InputTestFixture uses for automated tests), which is why this looked like the
    // right tool for the job despite not fully panning out here.
    //
    // Held keys (movement) can't block inside OnGUI, so PressKey's optional hold duration is
    // driven by a small queue ticked from EditorApplication.update instead.
    public sealed class InputSimulatorWindow : EditorWindow
    {
        private struct ScheduledRelease
        {
            public double ReleaseTime;
            public Key Key;
        }

        private static readonly HashSet<Key> HeldKeys = new HashSet<Key>();
        private static readonly List<ScheduledRelease> PendingReleases = new List<ScheduledRelease>();

        private float _holdSeconds = 0.35f;
        private Vector2 _mousePosition = new Vector2(960f, 540f);

        [MenuItem("Corebound/Input Simulator")]
        public static void Open()
        {
            EnsureGameViewFocusIgnored();
            GetWindow<InputSimulatorWindow>("Input Simulator");
        }

        // By default the Input System only routes input to the game while the Game view itself
        // has OS focus (InputSettings.EditorInputBehaviorInPlayMode.
        // PointersAndKeyboardsRespectGameViewFocus), and clicking buttons on this separate
        // floating EditorWindow takes OS focus away from the Game view - a reasonable suspect for
        // why bound actions weren't firing. Forcing AllDeviceInputAlwaysGoesToGameView removes
        // that variable. It did NOT turn out to be the (sole) explanation - see the class comment
        // above for the full, still-unresolved picture - but there's no reason to leave this
        // unset, so it stays.
        private static void EnsureGameViewFocusIgnored()
        {
            if (InputSystem.settings.editorInputBehaviorInPlayMode != InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView)
            {
                InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            }
        }

        private void OnEnable()
        {
            EnsureGameViewFocusIgnored();
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
        }

        private void OnDisable()
        {
            EditorApplication.update -= Tick;
        }

        private static string _lastAction = "(none yet)";
        private static int _clickCount;

        private void OnGUI()
        {
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Enter Play Mode first - simulated input only reaches a running game.", MessageType.Info);
            }

            if (Keyboard.current == null)
            {
                EditorGUILayout.HelpBox("No Keyboard device detected yet (Keyboard.current is null). Enter Play Mode once so the Input System creates its virtual devices.", MessageType.Warning);
            }

            EditorGUILayout.HelpBox("Clicks seen: " + _clickCount + " | Last: " + _lastAction, MessageType.None);

            EditorGUILayout.Space();
            _holdSeconds = EditorGUILayout.Slider("Movement hold (s)", _holdSeconds, 0.05f, 2f);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Movement / Navigate (WASD)", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("W", GUILayout.Width(40))) PressKeyHeld(Key.W, _holdSeconds);
                GUILayout.FlexibleSpace();
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("A", GUILayout.Width(40))) PressKeyHeld(Key.A, _holdSeconds);
                if (GUILayout.Button("S", GUILayout.Width(40))) PressKeyHeld(Key.S, _holdSeconds);
                if (GUILayout.Button("D", GUILayout.Width(40))) PressKeyHeld(Key.D, _holdSeconds);
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Navigate (arrows)", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("↑", GUILayout.Width(40))) PressKeyTap(Key.UpArrow);
                GUILayout.FlexibleSpace();
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("←", GUILayout.Width(40))) PressKeyTap(Key.LeftArrow);
                if (GUILayout.Button("↓", GUILayout.Width(40))) PressKeyTap(Key.DownArrow);
                if (GUILayout.Button("→", GUILayout.Width(40))) PressKeyTap(Key.RightArrow);
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Enter (Confirm)")) PressKeyTap(Key.Enter);
                if (GUILayout.Button("Escape (Cancel/Pause)")) PressKeyTap(Key.Escape);
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("E (Interact/NextSlot)")) PressKeyTap(Key.E);
                if (GUILayout.Button("Q (PreviousSlot)")) PressKeyTap(Key.Q);
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Z (PrevTowerType)")) PressKeyTap(Key.Z);
                if (GUILayout.Button("X (NextTowerType)")) PressKeyTap(Key.X);
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Mouse (PrimaryFire / Aim)", EditorStyles.boldLabel);
            _mousePosition = EditorGUILayout.Vector2Field("Screen position (px)", _mousePosition);
            if (GUILayout.Button("Move + Left Click"))
            {
                ClickMouse(_mousePosition);
            }

            EditorGUILayout.Space();
            if (GUILayout.Button("Release ALL held keys (panic button)"))
            {
                ReleaseAll();
            }
        }

        private static void PressKeyTap(Key key)
        {
            PressKeyHeld(key, 0.15f);
        }

        private static void PressKeyHeld(Key key, float holdSeconds)
        {
            _clickCount++;
            _lastAction = key + " @ " + System.DateTime.Now.ToString("HH:mm:ss.fff");

            if (Keyboard.current == null)
            {
                Debug.LogWarning("[InputSimulator] No Keyboard.current - enter Play Mode first.");
                return;
            }

            HeldKeys.Add(key);
            SendKeyboardState();

            PendingReleases.Add(new ScheduledRelease
            {
                ReleaseTime = EditorApplication.timeSinceStartup + holdSeconds,
                Key = key
            });
        }

        private static void Tick()
        {
            // A real held key reports "down" on every hardware poll, not just once, so this
            // re-sends the current HeldKeys state every tick for as long as any key is held
            // (instead of only at press and release) - the working theory being that a one-shot
            // QueueStateEvent might get overwritten by Unity's own next real (unpressed) poll
            // before a game script observes it. This did NOT fix the class-comment issue above
            // (confirmed via diagnostic - a bound InputAction still never saw it), so the theory
            // was wrong or incomplete, but continuous re-send is still strictly more correct than
            // one-shot for a "hold" and worth keeping regardless.
            if (HeldKeys.Count > 0)
            {
                SendKeyboardState();
            }

            if (PendingReleases.Count == 0)
            {
                return;
            }

            var now = EditorApplication.timeSinceStartup;
            var changed = false;

            for (var index = PendingReleases.Count - 1; index >= 0; index--)
            {
                if (now < PendingReleases[index].ReleaseTime)
                {
                    continue;
                }

                HeldKeys.Remove(PendingReleases[index].Key);
                PendingReleases.RemoveAt(index);
                changed = true;
            }

            if (changed)
            {
                SendKeyboardState();
            }
        }

        private static void SendKeyboardState()
        {
            if (Keyboard.current == null)
            {
                Debug.LogWarning("[InputSimulator] Keyboard.current is null.");
                return;
            }

            var state = new KeyboardState();

            foreach (var key in HeldKeys)
            {
                state.Set(key, true);
            }

            InputSystem.QueueStateEvent(Keyboard.current, state);
            InputSystem.Update();
        }

        private static void ReleaseAll()
        {
            HeldKeys.Clear();
            PendingReleases.Clear();
            SendKeyboardState();
        }

        private static void ClickMouse(Vector2 screenPosition)
        {
            if (Mouse.current == null)
            {
                Debug.LogWarning("[InputSimulator] No Mouse.current - enter Play Mode first.");
                return;
            }

            InputSystem.QueueStateEvent(Mouse.current, new MouseState { position = screenPosition });
            InputSystem.Update();

            InputSystem.QueueStateEvent(Mouse.current, new MouseState { position = screenPosition, buttons = 1 << (int)MouseButton.Left });
            InputSystem.Update();

            InputSystem.QueueStateEvent(Mouse.current, new MouseState { position = screenPosition, buttons = 0 });
            InputSystem.Update();
        }
    }
}
