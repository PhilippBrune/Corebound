using System.Collections.Generic;

namespace Corebound.UI.MainMenu
{
    public sealed class MainMenuRuntimeModel
    {
        private readonly List<MenuEntryData> _visibleEntries = new List<MenuEntryData>();

        private MainMenuFlowData _flowData;
        private MainMenuState _state;
        private int _selectedIndex = -1;
        private bool _isIntroVisible;

        public bool IsIntroVisible => _isIntroVisible;

        public IReadOnlyList<MenuEntryData> VisibleEntries => _visibleEntries;

        public int SelectedIndex => _selectedIndex;

        public MenuEntryData SelectedEntry => HasSelection() ? _visibleEntries[_selectedIndex] : null;

        public void Initialize(MainMenuFlowData flowData, MainMenuState state)
        {
            _flowData = flowData;
            _state = state;
            _isIntroVisible = flowData != null && flowData.ShowPressStartIntro;

            RebuildVisibleEntries();
        }

        public void ApplyState(MainMenuState state)
        {
            _state = state;
            RebuildVisibleEntries();
        }

        public bool TryDismissIntro()
        {
            if (!_isIntroVisible)
            {
                return false;
            }

            _isIntroVisible = false;
            return true;
        }

        public bool TryMoveSelection(int direction)
        {
            if (_visibleEntries.Count == 0 || direction == 0)
            {
                return false;
            }

            if (_selectedIndex < 0)
            {
                _selectedIndex = 0;
                return true;
            }

            var nextIndex = _selectedIndex + (direction > 0 ? 1 : -1);

            if (nextIndex < 0)
            {
                nextIndex = _visibleEntries.Count - 1;
            }
            else if (nextIndex >= _visibleEntries.Count)
            {
                nextIndex = 0;
            }

            if (nextIndex == _selectedIndex)
            {
                return false;
            }

            _selectedIndex = nextIndex;
            return true;
        }

        public bool TrySelectIndex(int index)
        {
            if (index < 0 || index >= _visibleEntries.Count || index == _selectedIndex)
            {
                return false;
            }

            _selectedIndex = index;
            return true;
        }

        public bool TryConfirmSelection(out MainMenuRoute route)
        {
            route = MainMenuRoute.NewRun;

            if (_isIntroVisible || !HasSelection())
            {
                return false;
            }

            route = ResolveRoute(_visibleEntries[_selectedIndex].Route);
            return true;
        }

        private bool HasSelection()
        {
            return _selectedIndex >= 0 && _selectedIndex < _visibleEntries.Count;
        }

        private void RebuildVisibleEntries()
        {
            _visibleEntries.Clear();

            if (_flowData == null || _flowData.Entries == null)
            {
                _selectedIndex = -1;
                return;
            }

            foreach (var entry in _flowData.Entries)
            {
                if (entry == null || !IsVisible(entry))
                {
                    continue;
                }

                _visibleEntries.Add(entry);
            }

            if (_visibleEntries.Count == 0)
            {
                _selectedIndex = -1;
                return;
            }

            if (_selectedIndex < 0 || _selectedIndex >= _visibleEntries.Count)
            {
                _selectedIndex = 0;
            }
        }

        private bool IsVisible(MenuEntryData entry)
        {
            switch (entry.Availability)
            {
                case MainMenuEntryAvailability.Always:
                    return true;
                case MainMenuEntryAvailability.RequiresContinueRun:
                    return _state.HasContinueRun;
                case MainMenuEntryAvailability.RequiresTutorialSeen:
                    return _state.HasSeenTutorial;
                case MainMenuEntryAvailability.RequiresMetaProgression:
                    return _state.IsMetaProgressionUnlocked;
                default:
                    return false;
            }
        }

        // Until 2026-09-23 a New Run on a profile that had not seen the tutorial was silently
        // turned into a tutorial run. On a fresh profile that made New Run unreachable, which is
        // what blocked testing the starting-weapon choice and everything behind it. The decision is
        // the player's now: the New Run panel offers the guided run as its first row.
        private MainMenuRoute ResolveRoute(MainMenuRoute route)
        {
            return route;
        }
    }
}