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

        // B4 (spec review 2026-09-22): the menu is two tiers. Continue and New Run are the primary
        // actions, everything else is a global route in a row along the bottom. The entry order in
        // MainMenuFlowData decides which is which: primary routes first.
        public int PrimaryEntryCount
        {
            get
            {
                var count = 0;

                for (var index = 0; index < VisibleEntries.Count; index++)
                {
                    var route = VisibleEntries[index] != null ? VisibleEntries[index].Route : MainMenuRoute.Quit;

                    // Owner 2026-09-22: Tutorial gets its own line again, beside Continue and New
                    // Run, instead of sitting in the row of global routes.
                    if (route != MainMenuRoute.Continue && route != MainMenuRoute.NewRun && route != MainMenuRoute.Tutorial)
                    {
                        break;
                    }

                    count++;
                }

                return count;
            }
        }

        public bool IsSecondaryEntry(int index)
        {
            return index >= PrimaryEntryCount;
        }

        // Up/Down moves inside the primary column and steps between the tiers; the row itself is
        // navigated with Left/Right. Each tier remembers where the focus was.
        public bool MoveSelectionVertical(int direction)
        {
            var primary = PrimaryEntryCount;
            var total = VisibleEntries.Count;

            if (total == 0 || primary == 0 || primary >= total)
            {
                return MoveSelection(direction);
            }

            var index = SelectedIndex;

            if (index < primary)
            {
                _lastPrimaryIndex = index;
                var next = index + direction;

                if (next >= 0 && next < primary)
                {
                    return SelectIndex(next);
                }

                return SelectIndex(Mathf.Clamp(_lastSecondaryIndex, primary, total - 1));
            }

            _lastSecondaryIndex = index;
            return SelectIndex(Mathf.Clamp(_lastPrimaryIndex, 0, primary - 1));
        }

        public bool MoveSelectionHorizontal(int direction)
        {
            var primary = PrimaryEntryCount;
            var total = VisibleEntries.Count;

            if (total == 0 || SelectedIndex < primary || primary >= total)
            {
                return false;
            }

            var count = total - primary;
            var offset = SelectedIndex - primary + direction;
            offset = (offset % count + count) % count;
            _lastSecondaryIndex = primary + offset;
            return SelectIndex(_lastSecondaryIndex);
        }

        private int _lastPrimaryIndex;
        private int _lastSecondaryIndex = int.MaxValue;

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