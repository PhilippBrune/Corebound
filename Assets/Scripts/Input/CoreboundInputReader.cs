using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Corebound.Input
{
    public sealed class CoreboundInputReader : MonoBehaviour, IInputReader
    {
        private const string GameplayMapName = "Gameplay";
        private const string BuildMapName = "Build";
        private const string UiMapName = "UI";
        private const string SystemMapName = "System";

        private const string MoveActionName = "Move";
        private const string AimActionName = "Aim";
        private const string PrimaryFireActionName = "PrimaryFire";
        private const string InteractActionName = "Interact";
        private const string PauseActionName = "Pause";
        private const string LoadoutActionName = "Loadout";
        private const string NavigateActionName = "Navigate";
        private const string ConfirmActionName = "Confirm";
        private const string CancelActionName = "Cancel";
        private const string PreviousActionName = "PreviousSlot";
        private const string UiPreviousActionName = "Previous";
        private const string UiNextActionName = "Next";
        private const string NextActionName = "NextSlot";
        private const string PreviousTowerTypeActionName = "PreviousTowerType";
        private const string NextTowerTypeActionName = "NextTowerType";

        [SerializeField] private InputActionAsset _inputActions;
        [SerializeField] private InputContextData _initialContext;

        private InputActionMap _gameplayMap;
        private InputActionMap _buildMap;
        private InputActionMap _uiMap;
        private InputAction _uiPreviousAction;
        private InputAction _uiNextAction;
        private InputActionMap _systemMap;

        private InputAction _moveAction;
        private InputAction _aimAction;
        private InputAction _primaryFireAction;
        private InputAction _interactAction;
        private InputAction _buildNavigateAction;
        private InputAction _buildConfirmAction;
        private InputAction _buildCancelAction;
        private InputAction _previousSlotAction;
        private InputAction _nextSlotAction;
        private InputAction _previousTowerTypeAction;
        private InputAction _nextTowerTypeAction;
        private InputAction _uiNavigateAction;
        private InputAction _uiConfirmAction;
        private InputAction _uiCancelAction;
        private InputAction _systemPauseAction;
        private InputAction _systemLoadoutAction;

        private bool _callbacksHooked;
        private (InputAction action, Action<InputAction.CallbackContext> handler)[] _performedBindings;
        private (InputAction action, Action<InputAction.CallbackContext> handler)[] _canceledBindings;

        public Vector2 Move { get; private set; }

        // Stick and pointer are kept apart (playtest 2026-09-22, Xbox 360 pad: "not possible to aim
        // because the char turns always towards the mouse"). Aim used to be one Value action over
        // both the right stick and <Pointer>/position, and a Value action follows the control with
        // the largest actuation - a mouse position of (1200, 600) always outweighs a stick in
        // [-1, 1], so Aim kept reporting the mouse and flipped IsUsingGamepad back to false. The
        // action is PassThrough now, each control reports itself, and Aim returns the one that
        // matches the active device.
        public Vector2 Aim
        {
            get => IsUsingGamepad ? _stickAim : _pointerAim;
            private set
            {
                _stickAim = value;
                _pointerAim = value;
            }
        }

        private Vector2 _stickAim;
        private Vector2 _pointerAim;
        private bool _hasPointerSample;

        // A mouse that has not moved must not take aiming back from the stick; pointer position
        // events also arrive for sub-pixel jitter and window focus changes.
        private const float PointerTakeoverPixels = 4f;
        private const float StickTakeoverMagnitude = 0.25f;

        public bool IsPrimaryFireHeld { get; private set; }

        public bool IsInteractHeld { get; private set; }

        // A12: Settings can force the device. 0 = auto (the thresholds below decide), 1 = always
        // controller, 2 = always mouse and keyboard.
        public static int ForcedInputDeviceIndex { get; set; }

        public bool IsUsingGamepad => ForcedInputDeviceIndex == 1 || (ForcedInputDeviceIndex != 2 && _isUsingGamepad);

        private bool _isUsingGamepad;

        public event Action<Vector2> NavigatePerformed;

        public event Action ConfirmPressed;

        public event Action CancelPressed;

        public event Action InteractPressed;

        public event Action PausePressed;

        // WeaponLoadout.md: opens the Intermission loadout panel. Not on IInputReader: only the
        // loadout panel listens, and it binds to this concrete reader.
        public event Action LoadoutPressed;

        public event Action PreviousPressed;

        public event Action NextPressed;

        public event Action PreviousTowerTypePressed;

        public event Action NextTowerTypePressed;

#if UNITY_EDITOR
        // Editor-only injection surface for DebugAutopilot (Assets/Editor/DebugAutopilot.cs).
        // Synthetic OS input never reaches Unity's Input System, so the only way to drive a real
        // run from a script is to write the same state the InputAction callbacks below write.
        // Everything downstream - PlayerController, PlayerCombatController, TowerService,
        // ShopService, WaveManager - reads this one instance through IInputReader and is already
        // subscribed to these exact events, so the whole game runs its normal code paths.
        // Compiled out of player builds; nothing in Assets/Scripts calls any of it.
        public void DebugSetMove(Vector2 value)
        {
            Move = value;
        }

        public void DebugSetAim(Vector2 value)
        {
            Aim = value;
        }

        public void DebugSetPrimaryFireHeld(bool value)
        {
            IsPrimaryFireHeld = value;
        }

        // PlayerAimResolver treats Aim as a *screen position* in mouse mode and as a stick
        // direction in gamepad mode. A driven test has no real mouse, so it must announce itself
        // as a gamepad or every shot is aimed at whatever screen corner the fed vector maps to.
        // Controller-first is also the project's binding target, so this is the representative path.
        public void DebugSetUsingGamepad(bool value)
        {
            _isUsingGamepad = value;
        }

        public void DebugSetInteractHeld(bool value)
        {
            IsInteractHeld = value;
        }

        public void DebugRaiseConfirm()
        {
            ConfirmPressed?.Invoke();
        }

        public void DebugRaiseCancel()
        {
            CancelPressed?.Invoke();
        }

        public void DebugRaiseInteract()
        {
            InteractPressed?.Invoke();
        }

        public void DebugRaisePause()
        {
            PausePressed?.Invoke();
        }

        public void DebugRaiseLoadout()
        {
            LoadoutPressed?.Invoke();
        }

        public void DebugRaiseNextTowerType()
        {
            NextTowerTypePressed?.Invoke();
        }

        public void DebugRaiseNavigate(Vector2 direction)
        {
            NavigatePerformed?.Invoke(direction);
        }

        public void DebugRaiseNext()
        {
            NextPressed?.Invoke();
        }

        public void DebugRaisePrevious()
        {
            PreviousPressed?.Invoke();
        }

        private void OnValidate()
        {
            TryAssignDefaultInputActions();
        }
#endif

        private void OnEnable()
        {
            if (!TryCacheActions())
            {
                enabled = false;
                return;
            }

            HookCallbacks();
            ApplyContext(_initialContext);
        }

        private void OnDisable()
        {
            UnhookCallbacks();
            DisableAllMaps();
            ResetInputState();
        }

        public void ApplyContext(InputContextData context)
        {
            DisableAllMaps();

            if (context == null)
            {
                return;
            }

            SetMapEnabled(_gameplayMap, context.EnableGameplay);
            SetMapEnabled(_buildMap, context.EnableBuild);
            SetMapEnabled(_uiMap, context.EnableUi);
            SetMapEnabled(_systemMap, context.EnableSystem);
        }

        public void ResetInputState()
        {
            Move = Vector2.zero;
            Aim = Vector2.zero;
            IsPrimaryFireHeld = false;
            IsInteractHeld = false;
        }

        private bool TryCacheActions()
        {
            TryAssignDefaultInputActions();

            if (_inputActions == null)
            {
                Debug.LogError("CoreboundInputReader requires an InputActionAsset reference.", this);
                return false;
            }

            _gameplayMap = _inputActions.FindActionMap(GameplayMapName, true);
            _buildMap = _inputActions.FindActionMap(BuildMapName, true);
            _uiMap = _inputActions.FindActionMap(UiMapName, true);
            _systemMap = _inputActions.FindActionMap(SystemMapName, true);

            _moveAction = _gameplayMap.FindAction(MoveActionName, true);
            _aimAction = _gameplayMap.FindAction(AimActionName, true);
            _primaryFireAction = _gameplayMap.FindAction(PrimaryFireActionName, true);
            _interactAction = _gameplayMap.FindAction(InteractActionName, true);

            _buildNavigateAction = _buildMap.FindAction(NavigateActionName, true);
            _buildConfirmAction = _buildMap.FindAction(ConfirmActionName, true);
            _buildCancelAction = _buildMap.FindAction(CancelActionName, true);
            _previousSlotAction = _buildMap.FindAction(PreviousActionName, true);
            _nextSlotAction = _buildMap.FindAction(NextActionName, true);
            _previousTowerTypeAction = _buildMap.FindAction(PreviousTowerTypeActionName, true);
            _nextTowerTypeAction = _buildMap.FindAction(NextTowerTypeActionName, true);

            _uiNavigateAction = _uiMap.FindAction(NavigateActionName, true);
            _uiConfirmAction = _uiMap.FindAction(ConfirmActionName, true);
            _uiCancelAction = _uiMap.FindAction(CancelActionName, true);

            // Optional (false): Previous/Next were added to the UI map on 2026-09-23, so an older
            // actions asset still loads. Without them the menus never saw Q / Tab or the shoulder
            // buttons at all - tab switching and the profile panel's delete were dead keys, because
            // the only Previous/Next lived in the Build map, which menus do not enable.
            _uiPreviousAction = _uiMap.FindAction(UiPreviousActionName, false);
            _uiNextAction = _uiMap.FindAction(UiNextActionName, false);

            _systemPauseAction = _systemMap.FindAction(PauseActionName, true);

            // Optional (false): an actions asset from before the loadout existed still loads.
            _systemLoadoutAction = _systemMap.FindAction(LoadoutActionName, false);

            BuildCallbackBindings();
            return true;
        }

        // Every (action, handler) pair below gets both a "performed" and a "canceled" hook in
        // one place, so HookCallbacks/UnhookCallbacks can loop instead of hand-pairing 20+/-=
        // lines that would otherwise need to be kept in sync by hand.
        //
        // Gameplay.Pause and System.Submit are deliberately not cached/bound at all (2026-09-11):
        // InputContext_Gameplay/_Build enable the Gameplay, Build and System maps at the same
        // time, and Gameplay.Pause/System.Pause share the Escape key while Build.Confirm/
        // System.Submit share Enter. Wiring both sides of either pair fires the same event twice
        // per physical key press - for Pause specifically this made WaveStatePresenter toggle its
        // overlay open then immediately closed in the same frame, i.e. "ESC does nothing".
        // System.Pause/Build.Confirm alone already cover every context that needs them
        // (System.Pause also covers Build-only intermission moments Gameplay.Pause would miss).
        private void BuildCallbackBindings()
        {
            _performedBindings = new (InputAction action, Action<InputAction.CallbackContext> handler)[]
            {
                (_moveAction, OnMovePerformed),
                (_aimAction, OnAimPerformed),
                (_primaryFireAction, OnPrimaryFirePerformed),
                (_interactAction, OnInteractPerformed),
                (_buildNavigateAction, OnNavigatePerformed),
                (_buildConfirmAction, OnConfirmPerformed),
                (_buildCancelAction, OnCancelPerformed),
                (_previousSlotAction, OnPreviousPerformed),
                (_nextSlotAction, OnNextPerformed),
                (_previousTowerTypeAction, OnPreviousTowerTypePerformed),
                (_nextTowerTypeAction, OnNextTowerTypePerformed),
                (_uiNavigateAction, OnNavigatePerformed),
                (_uiConfirmAction, OnConfirmPerformed),
                (_uiCancelAction, OnCancelPerformed),
                (_uiPreviousAction, OnPreviousPerformed),
                (_uiNextAction, OnNextPerformed),
                (_systemPauseAction, OnPausePerformed),
                (_systemLoadoutAction, OnLoadoutPerformed),
            };

            _canceledBindings = new (InputAction action, Action<InputAction.CallbackContext> handler)[]
            {
                (_moveAction, OnMoveCanceled),
                (_aimAction, OnAimCanceled),
                (_primaryFireAction, OnPrimaryFireCanceled),
                (_interactAction, OnInteractCanceled),
            };
        }

        private void TryAssignDefaultInputActions()
        {
            _inputActions = DefaultInputActionsProvider.Resolve(_inputActions);
        }

        private void HookCallbacks()
        {
            if (_callbacksHooked)
            {
                return;
            }

            for (var index = 0; index < _performedBindings.Length; index++)
            {
                if (_performedBindings[index].action != null)
                {
                    _performedBindings[index].action.performed += _performedBindings[index].handler;
                }
            }

            for (var index = 0; index < _canceledBindings.Length; index++)
            {
                _canceledBindings[index].action.canceled += _canceledBindings[index].handler;
            }

            _callbacksHooked = true;
        }

        private void UnhookCallbacks()
        {
            if (!_callbacksHooked)
            {
                return;
            }

            for (var index = 0; index < _performedBindings.Length; index++)
            {
                if (_performedBindings[index].action != null)
                {
                    _performedBindings[index].action.performed -= _performedBindings[index].handler;
                }
            }

            for (var index = 0; index < _canceledBindings.Length; index++)
            {
                _canceledBindings[index].action.canceled -= _canceledBindings[index].handler;
            }

            _callbacksHooked = false;
        }

        private void DisableAllMaps()
        {
            SetMapEnabled(_gameplayMap, false);
            SetMapEnabled(_buildMap, false);
            SetMapEnabled(_uiMap, false);
            SetMapEnabled(_systemMap, false);
        }

        private static void SetMapEnabled(InputActionMap map, bool isEnabled)
        {
            if (map == null)
            {
                return;
            }

            if (isEnabled)
            {
                map.Enable();
                return;
            }

            map.Disable();
        }

        private void OnMovePerformed(InputAction.CallbackContext context)
        {
            UpdateInputDevice(context);
            Move = context.ReadValue<Vector2>();
        }

        private void OnMoveCanceled(InputAction.CallbackContext context)
        {
            Move = Vector2.zero;
        }

        private void OnAimPerformed(InputAction.CallbackContext context)
        {
            var value = context.ReadValue<Vector2>();

            if (context.control?.device is Gamepad)
            {
                _stickAim = value;

                if (value.sqrMagnitude >= StickTakeoverMagnitude * StickTakeoverMagnitude)
                {
                    _isUsingGamepad = true;
                }

                return;
            }

            var moved = !_hasPointerSample || (value - _pointerAim).sqrMagnitude >= PointerTakeoverPixels * PointerTakeoverPixels;
            _pointerAim = value;
            _hasPointerSample = true;

            if (moved)
            {
                _isUsingGamepad = false;
            }
        }

        private void OnAimCanceled(InputAction.CallbackContext context)
        {
            _stickAim = Vector2.zero;
        }

        private void OnPrimaryFirePerformed(InputAction.CallbackContext context)
        {
            UpdateInputDevice(context);
            IsPrimaryFireHeld = true;
        }

        private void OnPrimaryFireCanceled(InputAction.CallbackContext context)
        {
            IsPrimaryFireHeld = false;
        }

        // Build.Navigate/UI.Navigate now bind the Gamepad left stick directly (added so shop/reward
        // navigation actually works with the analog stick, not just the D-pad) alongside the
        // existing D-pad/arrow-key digital bindings. A digital button's .performed fires once per
        // press, but a Value-type stick control fires .performed on every frame its value changes at
        // all - which is nearly every frame while displaced, since real sticks report tiny value
        // jitter continuously. Every consumer of NavigatePerformed (Shop/CardReward/MainMenu offer
        // and row selection) does a flat +/-1 step per event with no debounce of its own, so without
        // this throttle, holding the stick in any direction would spam dozens of selection steps per
        // second instead of moving one slot at a time. The 180ms window still lets a held stick
        // auto-repeat at a normal menu pace instead of only firing once per press.
        private const float NavigateRepeatCooldownSeconds = 0.18f;
        private float _lastNavigateTime = -1f;

        private void OnNavigatePerformed(InputAction.CallbackContext context)
        {
            UpdateInputDevice(context);

            if (_lastNavigateTime >= 0f && Time.unscaledTime - _lastNavigateTime < NavigateRepeatCooldownSeconds)
            {
                return;
            }

            _lastNavigateTime = Time.unscaledTime;
            NavigatePerformed?.Invoke(context.ReadValue<Vector2>());
        }

        private void OnConfirmPerformed(InputAction.CallbackContext context)
        {
            UpdateInputDevice(context);
            ConfirmPressed?.Invoke();
        }

        private void OnCancelPerformed(InputAction.CallbackContext context)
        {
            UpdateInputDevice(context);
            CancelPressed?.Invoke();
        }

        private void OnInteractPerformed(InputAction.CallbackContext context)
        {
            UpdateInputDevice(context);
            IsInteractHeld = true;
            InteractPressed?.Invoke();
        }

        private void OnInteractCanceled(InputAction.CallbackContext context)
        {
            IsInteractHeld = false;
        }

        private void OnPausePerformed(InputAction.CallbackContext context)
        {
            UpdateInputDevice(context);
            PausePressed?.Invoke();
        }

        private void OnLoadoutPerformed(InputAction.CallbackContext context)
        {
            UpdateInputDevice(context);
            LoadoutPressed?.Invoke();
        }

        private void OnPreviousPerformed(InputAction.CallbackContext context)
        {
            UpdateInputDevice(context);
            PreviousPressed?.Invoke();
        }

        private void OnNextPerformed(InputAction.CallbackContext context)
        {
            UpdateInputDevice(context);
            NextPressed?.Invoke();
        }

        private void OnPreviousTowerTypePerformed(InputAction.CallbackContext context)
        {
            UpdateInputDevice(context);
            PreviousTowerTypePressed?.Invoke();
        }

        private void OnNextTowerTypePerformed(InputAction.CallbackContext context)
        {
            UpdateInputDevice(context);
            NextTowerTypePressed?.Invoke();
        }

        private void UpdateInputDevice(InputAction.CallbackContext context)
        {
            _isUsingGamepad = context.control?.device is Gamepad;
        }

        public string GetBindingDisplayString(string mapName, string actionName, string bindingGroup = "Keyboard&Mouse")
        {
            TryAssignDefaultInputActions();

            if (_inputActions == null)
            {
                return string.Empty;
            }

            var map = _inputActions.FindActionMap(mapName, false);
            var action = map != null ? map.FindAction(actionName, false) : null;

            if (action == null)
            {
                return string.Empty;
            }

            for (var index = 0; index < action.bindings.Count; index++)
            {
                var binding = action.bindings[index];

                if (binding.isComposite || binding.isPartOfComposite)
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(bindingGroup) && !string.Equals(binding.groups, bindingGroup, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                return action.GetBindingDisplayString(index, InputBinding.DisplayStringOptions.DontIncludeInteractions);
            }

            return action.GetBindingDisplayString();
        }
    }
}