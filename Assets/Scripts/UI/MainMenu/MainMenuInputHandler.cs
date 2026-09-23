using System;
using Corebound.Input;
using UnityEngine;

namespace Corebound.UI.MainMenu
{
    public sealed class MainMenuInputHandler : MonoBehaviour
    {

        [SerializeField] private MainMenuController _mainMenuController;
        [SerializeField] private UnityEngine.Object _inputReaderSource;
        [SerializeField] private UnityEngine.Object _routeTargetControllerSource;
        [SerializeField] private InputContextData _inputContext;
        [SerializeField] private InputAssistData _inputAssistData;

        private float NavigateThreshold => _inputAssistData != null ? _inputAssistData.MenuNavigateThreshold : InputAssistData.DefaultMenuNavigateThreshold;

        private IInputReader InputReader => _inputReaderSource as IInputReader;

        private IMainMenuRouteTargetController RouteTargetController => _routeTargetControllerSource as IMainMenuRouteTargetController;

    public event Action NavigateSubmitted;

    public event Action ConfirmSubmitted;

    public event Action CancelSubmitted;

        private void OnEnable()
        {
            if (_mainMenuController == null || InputReader == null)
            {
                enabled = false;
                return;
            }

            InputReader.ApplyContext(_inputContext);
            InputReader.ResetInputState();
            InputReader.NavigatePerformed += OnNavigatePerformed;
            InputReader.PreviousPressed += OnPreviousPressed;
            InputReader.NextPressed += OnNextPressed;
            InputReader.ConfirmPressed += OnConfirmPressed;
            InputReader.CancelPressed += OnCancelPressed;
        }

        private void OnDisable()
        {
            if (InputReader == null)
            {
                return;
            }

            InputReader.NavigatePerformed -= OnNavigatePerformed;
            InputReader.PreviousPressed -= OnPreviousPressed;
            InputReader.NextPressed -= OnNextPressed;
            InputReader.ConfirmPressed -= OnConfirmPressed;
            InputReader.CancelPressed -= OnCancelPressed;
        }

        // Q / LB: the profile panel's secondary action (delete). Every other panel ignores it.
        private void OnPreviousPressed()
        {
            if (RouteTargetController != null && RouteTargetController.IsRouteTargetOpen)
            {
                RouteTargetController.TryHandleTabSwitch(-1);
            }
        }

        private void OnNextPressed()
        {
            if (RouteTargetController != null && RouteTargetController.IsRouteTargetOpen)
            {
                RouteTargetController.TryHandleTabSwitch(1);
            }
        }

        private void OnNavigatePerformed(Vector2 navigationInput)
        {
            if (RouteTargetController != null && RouteTargetController.IsRouteTargetOpen)
            {
                RouteTargetController.TryHandleNavigation(navigationInput);
                return;
            }

            if (_mainMenuController.IsIntroVisible)
            {
                return;
            }

            // B4: Left/Right walks the secondary row, Up/Down the primary column and the step
            // between the two tiers.
            if (Mathf.Abs(navigationInput.x) >= NavigateThreshold && Mathf.Abs(navigationInput.x) > Mathf.Abs(navigationInput.y))
            {
                if (_mainMenuController.MoveSelectionHorizontal(navigationInput.x > 0f ? 1 : -1))
                {
                    NavigateSubmitted?.Invoke();
                }

                return;
            }

            if (Mathf.Abs(navigationInput.y) < NavigateThreshold)
            {
                return;
            }

            if (_mainMenuController.MoveSelectionVertical(navigationInput.y > 0f ? -1 : 1))
            {
                NavigateSubmitted?.Invoke();
            }
        }

        // Mouse-driven counterpart to keyboard/gamepad Navigate: a pointer hovering a top-level
        // entry selects it the same way a dpad/arrow press would. Scoped to the top-level list
        // only (RouteTargetController.IsRouteTargetOpen guards sub-panel rows out for now, same
        // as OnNavigatePerformed above) - sub-panel row hover is a deliberate follow-up, not part
        // of this pass.
        public void RequestSelectIndex(int index)
        {
            if (RouteTargetController != null && RouteTargetController.IsRouteTargetOpen)
            {
                return;
            }

            if (_mainMenuController.IsIntroVisible)
            {
                return;
            }

            if (_mainMenuController.SelectIndex(index))
            {
                NavigateSubmitted?.Invoke();
            }
        }

        // Playtest 2026-09-22: the New Run confirmation "can only be closed via Enter - make it Enter
        // or left mouse click". Route info panels (New Run, Quit, Continue...) have no clickable
        // element, so a click anywhere acts as Confirm and a right click as Cancel. Only for those
        // panels: an interactive target like the Progression screen would turn a stray click into a
        // purchase. Ignored in the frame the panel opened, or the click that opened it would also
        // confirm it.
        private int _panelOpenedFrame = -1;
        private bool _wasPanelOpen;

        private void Update()
        {
            var controller = RouteTargetController;
            var panelOpen = controller != null && controller.IsRouteTargetOpen && controller.ActivePanelData != null;

            if (panelOpen && !_wasPanelOpen)
            {
                _panelOpenedFrame = Time.frameCount;
            }

            _wasPanelOpen = panelOpen;
            var mouse = UnityEngine.InputSystem.Mouse.current;

            if (mouse == null)
            {
                return;
            }

            // "Press Start" is the first thing anyone sees, and a mouse player's first instinct is
            // to click it rather than hunt for a key (playtest 2026-09-23).
            if (_mainMenuController.IsIntroVisible)
            {
                if (mouse.leftButton.wasPressedThisFrame && _mainMenuController.AcknowledgeIntro())
                {
                    ConfirmSubmitted?.Invoke();
                }

                return;
            }

            if (!panelOpen || Time.frameCount <= _panelOpenedFrame + 1)
            {
                return;
            }

            if (mouse.leftButton.wasPressedThisFrame)
            {
                OnConfirmPressed();
            }
            else if (mouse.rightButton.wasPressedThisFrame)
            {
                OnCancelPressed();
            }
        }

        // Mouse-driven counterpart to keyboard/gamepad Confirm - reuses the exact same routing
        // (route-target-open vs. top-level list) so a click behaves identically to pressing
        // Enter/gamepad South on the same entry.
        public void RequestConfirm()
        {
            OnConfirmPressed();
        }

        private void OnConfirmPressed()
        {
            if (RouteTargetController != null && RouteTargetController.IsRouteTargetOpen)
            {
                if (RouteTargetController.TryHandleConfirm())
                {
                    ConfirmSubmitted?.Invoke();
                }

                return;
            }

            if (_mainMenuController.ConfirmSelection())
            {
                ConfirmSubmitted?.Invoke();
            }
        }

        private void OnCancelPressed()
        {
            if (RouteTargetController != null && RouteTargetController.IsRouteTargetOpen)
            {
                if (RouteTargetController.TryHandleCancel())
                {
                    CancelSubmitted?.Invoke();
                }

                return;
            }

            if (_mainMenuController.IsIntroVisible)
            {
                if (_mainMenuController.AcknowledgeIntro())
                {
                    CancelSubmitted?.Invoke();
                }
            }
        }
    }
}