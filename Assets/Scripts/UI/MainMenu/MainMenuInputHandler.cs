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
            InputReader.ConfirmPressed -= OnConfirmPressed;
            InputReader.CancelPressed -= OnCancelPressed;
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

            if (Mathf.Abs(navigationInput.y) < NavigateThreshold || Mathf.Abs(navigationInput.y) < Mathf.Abs(navigationInput.x))
            {
                return;
            }

            if (_mainMenuController.MoveSelection(navigationInput.y > 0f ? -1 : 1))
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