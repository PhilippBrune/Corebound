using System;
using System.Collections.Generic;
using Corebound.UI.MainMenu;
using Corebound.Input;
using Corebound.SaveSystem;
using UnityEngine;

namespace Corebound.Settings
{
    public sealed class SettingsRouteTargetController : MonoBehaviour, IMainMenuInteractiveRouteTarget
    {

        private readonly List<DisplayRow> _rows = new List<DisplayRow>();
        private readonly List<OptionLocation> _optionLocations = new List<OptionLocation>();

        [SerializeField] private SettingsManager _settingsManager;
        [SerializeField] private InputAssistData _inputAssistData;

        private float NavigationThreshold => _inputAssistData != null ? _inputAssistData.MenuNavigateThreshold : InputAssistData.DefaultMenuNavigateThreshold;

        private bool _isOpen;
        private int _selectedOptionIndex;
        private int _tabIndex;
        private SettingsSaveData _openingSnapshot;
        private SettingsSaveData _workingCopy;

        public event Action TargetChanged;

        public bool IsOpen => _isOpen;

        public MainMenuRoute Route => MainMenuRoute.Settings;

        public IReadOnlyList<DisplayRow> Rows => _rows;

        public string Title => "Settings";

        public string Summary => "Use left or right to adjust the selected option. Confirm applies all changes. Cancel discards this session.";

        public string SelectedDescription => TryGetSelectedOption(out var optionDefinition) ? optionDefinition.Description : string.Empty;

        public void Open()
        {
            if (_settingsManager == null)
            {
                return;
            }

            _isOpen = true;
            _openingSnapshot = _settingsManager.CreateWorkingCopy();
            _workingCopy = _openingSnapshot.Clone();
            _selectedOptionIndex = 0;
            RebuildRows();
            TargetChanged?.Invoke();
        }

        public void Close()
        {
            _isOpen = false;
            TargetChanged?.Invoke();
        }

        public bool TryHandleNavigation(Vector2 navigationInput)
        {
            if (!_isOpen)
            {
                return false;
            }

            if (Mathf.Abs(navigationInput.y) >= NavigationThreshold && Mathf.Abs(navigationInput.y) >= Mathf.Abs(navigationInput.x))
            {
                MoveSelection(navigationInput.y > 0f ? -1 : 1);
                return true;
            }

            if (Mathf.Abs(navigationInput.x) < NavigationThreshold)
            {
                return false;
            }

            if (!TryGetSelectedOption(out var optionDefinition))
            {
                return false;
            }

            _settingsManager.AdjustValue(_workingCopy, optionDefinition, navigationInput.x > 0f ? 1 : -1);
            _settingsManager.ApplyPreview(_workingCopy);
            RebuildRows();
            TargetChanged?.Invoke();
            return true;
        }

        public bool TryHandleConfirm()
        {
            if (!_isOpen || _workingCopy == null)
            {
                return false;
            }

            _settingsManager.Commit(_workingCopy);
            Close();
            return true;
        }

        public bool TryHandleCancel()
        {
            if (!_isOpen)
            {
                return false;
            }

            _settingsManager.ApplyPreview(_openingSnapshot);
            Close();
            return true;
        }

        private void MoveSelection(int direction)
        {
            if (_optionLocations.Count == 0)
            {
                return;
            }

            _selectedOptionIndex += direction;

            if (_selectedOptionIndex < 0)
            {
                _selectedOptionIndex = _optionLocations.Count - 1;
            }
            else if (_selectedOptionIndex >= _optionLocations.Count)
            {
                _selectedOptionIndex = 0;
            }

            // Without this, _rows keeps the IsSelected flags from the last rebuild - the highlight
            // only catches up once TryHandleNavigation's left/right branch calls RebuildRows() for
            // an unrelated reason (adjusting a value), so the visible highlight silently lags one
            // step behind the real _selectedOptionIndex.
            RebuildRows();
            TargetChanged?.Invoke();
        }

        private void RebuildRows()
        {
            _rows.Clear();
            _optionLocations.Clear();

            if (_settingsManager == null || _settingsManager.DefinitionData == null)
            {
                return;
            }

            var categories = _settingsManager.DefinitionData.Categories;

            if (categories == null || categories.Length == 0)
            {
                return;
            }

            _tabIndex = Mathf.Clamp(_tabIndex, 0, categories.Length - 1);
            _rows.Add(DisplayRow.CreateCategory(BuildTabStrip(categories)));

            var activeOptions = categories[_tabIndex].Options;

            if (activeOptions == null)
            {
                return;
            }

            for (var optionIndex = 0; optionIndex < activeOptions.Length; optionIndex++)
            {
                var flatIndex = _optionLocations.Count;
                _optionLocations.Add(new OptionLocation(_tabIndex, optionIndex));
                _rows.Add(DisplayRow.CreateOption(
                    activeOptions[optionIndex].Label,
                    _settingsManager.GetDisplayValue(_workingCopy, activeOptions[optionIndex]),
                    flatIndex == _selectedOptionIndex));
            }
        }

        private string BuildTabStrip(SettingsDefinitionData.CategoryDefinition[] categories)
        {
            var builder = new System.Text.StringBuilder(96);

            for (var index = 0; index < categories.Length; index++)
            {
                builder.Append(index > 0 ? "      " : string.Empty);
                builder.Append(index == _tabIndex ? "[" + categories[index].Label + "]" : categories[index].Label);
            }

            return builder.ToString();
        }

        // Q / Tab and LB / RB, the same switch as the pause menu and the Progression screen.
        public void SwitchTab(int direction)
        {
            var categories = _settingsManager != null && _settingsManager.DefinitionData != null
                ? _settingsManager.DefinitionData.Categories
                : null;

            if (!_isOpen || categories == null || categories.Length == 0)
            {
                return;
            }

            _tabIndex = (_tabIndex + direction + categories.Length) % categories.Length;
            _selectedOptionIndex = 0;
            RebuildRows();
            TargetChanged?.Invoke();
        }

        private bool TryGetSelectedOption(out SettingsDefinitionData.OptionDefinition optionDefinition)
        {
            optionDefinition = default;

            if (_settingsManager == null || _settingsManager.DefinitionData == null || _selectedOptionIndex < 0 || _selectedOptionIndex >= _optionLocations.Count)
            {
                return false;
            }

            var optionLocation = _optionLocations[_selectedOptionIndex];
            optionDefinition = _settingsManager.DefinitionData.Categories[optionLocation.CategoryIndex].Options[optionLocation.OptionIndex];
            return true;
        }

        private readonly struct OptionLocation
        {
            public OptionLocation(int categoryIndex, int optionIndex)
            {
                CategoryIndex = categoryIndex;
                OptionIndex = optionIndex;
            }

            public int CategoryIndex { get; }

            public int OptionIndex { get; }
        }

        public readonly struct DisplayRow
        {
            private DisplayRow(bool isCategory, string primaryText, string secondaryText, bool isSelected)
            {
                IsCategory = isCategory;
                PrimaryText = primaryText;
                SecondaryText = secondaryText;
                IsSelected = isSelected;
            }

            public bool IsCategory { get; }

            public string PrimaryText { get; }

            public string SecondaryText { get; }

            public bool IsSelected { get; }

            public static DisplayRow CreateCategory(string label)
            {
                return new DisplayRow(true, label, string.Empty, false);
            }

            public static DisplayRow CreateOption(string label, string value, bool isSelected)
            {
                return new DisplayRow(false, label, value, isSelected);
            }
        }
    }
}