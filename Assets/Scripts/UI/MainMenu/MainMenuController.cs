using System;
using System.Collections.Generic;
using UnityEngine;

namespace Corebound.UI.MainMenu
{
    public sealed class MainMenuController : MonoBehaviour
    {
        [SerializeField] private MainMenuFlowData _flowData;
        [SerializeField] private UnityEngine.Object _stateProviderSource;
        [SerializeField] private MainMenuState _fallbackState = new MainMenuState(false, false, false);

        private readonly MainMenuRuntimeModel _model = new MainMenuRuntimeModel();

        public event Action StateChanged;

        public event Action<MainMenuRoute> RouteConfirmed;

        public bool IsIntroVisible => _model.IsIntroVisible;

        public IReadOnlyList<MenuEntryData> VisibleEntries => _model.VisibleEntries;

        public int SelectedIndex => _model.SelectedIndex;

        public MenuEntryData SelectedEntry => _model.SelectedEntry;

        private IMainMenuStateProvider StateProvider => _stateProviderSource as IMainMenuStateProvider;

        private void Awake()
        {
            if (_flowData == null)
            {
                enabled = false;
                return;
            }

            _model.Initialize(_flowData, ResolveInitialState());
            StateChanged?.Invoke();
        }

        public void ApplyState(MainMenuState state)
        {
            _model.ApplyState(state);
            StateChanged?.Invoke();
        }

        public bool AcknowledgeIntro()
        {
            var changed = _model.TryDismissIntro();

            if (changed)
            {
                StateChanged?.Invoke();
            }

            return changed;
        }

        public bool MoveSelection(int direction)
        {
            var changed = _model.TryMoveSelection(direction);

            if (changed)
            {
                StateChanged?.Invoke();
            }

            return changed;
        }

        public bool SelectIndex(int index)
        {
            var changed = _model.TrySelectIndex(index);

            if (changed)
            {
                StateChanged?.Invoke();
            }

            return changed;
        }

        public bool ConfirmSelection()
        {
            if (_model.IsIntroVisible)
            {
                return AcknowledgeIntro();
            }

            if (!_model.TryConfirmSelection(out var route))
            {
                return false;
            }

            RouteConfirmed?.Invoke(route);
            return true;
        }

        private MainMenuState ResolveInitialState()
        {
            if (StateProvider != null)
            {
                return StateProvider.GetCurrentState();
            }

            return _fallbackState;
        }
    }
}