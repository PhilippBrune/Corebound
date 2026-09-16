using System;
using System.Collections.Generic;
using Corebound.Input;
using Corebound.UI.MainMenu;
using UnityEngine;

namespace Corebound.MetaProgression
{
    public sealed class MetaProgressionRouteTargetController : MonoBehaviour, IMainMenuInteractiveRouteTarget
    {

        private readonly List<DisplayRow> _rows = new List<DisplayRow>();

        [SerializeField] private MetaProgressionService _metaProgressionService;
        [SerializeField] private InputAssistData _inputAssistData;

        private float NavigationThreshold => _inputAssistData != null ? _inputAssistData.MenuNavigateThreshold : InputAssistData.DefaultMenuNavigateThreshold;

        private bool _isOpen;
        private int _selectedUnlockIndex;

        public event Action TargetChanged;

        public bool IsOpen => _isOpen;

        public MainMenuRoute Route => MainMenuRoute.MetaProgression;

        public IReadOnlyList<DisplayRow> Rows => _rows;

        public string Title => "Meta Progression";

        public string CurrencyLabel => _metaProgressionService != null && _metaProgressionService.DefinitionData != null
            ? _metaProgressionService.DefinitionData.MetaCurrencyLabel + ": " + _metaProgressionService.CurrencyAmount
            : string.Empty;

        public string Summary => TryGetSelectedUnlock(out var unlockDefinition)
            ? unlockDefinition.Summary
            : "Review persistent unlocks and expand future Runs.";

        private void OnEnable()
        {
            if (_metaProgressionService != null)
            {
                _metaProgressionService.ProgressionChanged += OnProgressionChanged;
            }
        }

        private void OnDisable()
        {
            if (_metaProgressionService != null)
            {
                _metaProgressionService.ProgressionChanged -= OnProgressionChanged;
            }
        }

        public void Open()
        {
            _isOpen = true;
            _selectedUnlockIndex = 0;
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
            if (!_isOpen || _metaProgressionService == null || _metaProgressionService.DefinitionData == null)
            {
                return false;
            }

            if (Mathf.Abs(navigationInput.y) < NavigationThreshold || Mathf.Abs(navigationInput.y) < Mathf.Abs(navigationInput.x))
            {
                return false;
            }

            var unlocks = _metaProgressionService.DefinitionData.Unlocks;

            if (unlocks == null || unlocks.Length == 0)
            {
                return false;
            }

            _selectedUnlockIndex += navigationInput.y > 0f ? -1 : 1;

            if (_selectedUnlockIndex < 0)
            {
                _selectedUnlockIndex = unlocks.Length - 1;
            }
            else if (_selectedUnlockIndex >= unlocks.Length)
            {
                _selectedUnlockIndex = 0;
            }

            RebuildRows();
            TargetChanged?.Invoke();
            return true;
        }

        public bool TryHandleConfirm()
        {
            if (!_isOpen || _metaProgressionService == null)
            {
                return false;
            }

            var result = _metaProgressionService.TryPurchase(_selectedUnlockIndex);
            RebuildRows();
            TargetChanged?.Invoke();
            return result;
        }

        public bool TryHandleCancel()
        {
            if (!_isOpen)
            {
                return false;
            }

            Close();
            return true;
        }

        private void OnProgressionChanged()
        {
            RebuildRows();
            TargetChanged?.Invoke();
        }

        private void RebuildRows()
        {
            _rows.Clear();

            if (_metaProgressionService == null || _metaProgressionService.DefinitionData == null || _metaProgressionService.DefinitionData.Unlocks == null)
            {
                return;
            }

            var unlocks = _metaProgressionService.DefinitionData.Unlocks;

            for (var index = 0; index < unlocks.Length; index++)
            {
                var isUnlocked = _metaProgressionService.IsUnlocked(unlocks[index].UnlockId);
                var statusLabel = isUnlocked ? "Unlocked" : unlocks[index].Cost + " cost";
                _rows.Add(new DisplayRow(unlocks[index].Title + " | " + unlocks[index].TargetSystem, statusLabel, index == _selectedUnlockIndex, isUnlocked));
            }
        }

        private bool TryGetSelectedUnlock(out MetaProgressionDefinitionData.UnlockDefinition unlockDefinition)
        {
            unlockDefinition = default;

            if (_metaProgressionService == null || _metaProgressionService.DefinitionData == null || _metaProgressionService.DefinitionData.Unlocks == null)
            {
                return false;
            }

            var unlocks = _metaProgressionService.DefinitionData.Unlocks;

            if (_selectedUnlockIndex < 0 || _selectedUnlockIndex >= unlocks.Length)
            {
                return false;
            }

            unlockDefinition = unlocks[_selectedUnlockIndex];
            return true;
        }

        public readonly struct DisplayRow
        {
            public DisplayRow(string label, string statusLabel, bool isSelected, bool isUnlocked)
            {
                Label = label;
                StatusLabel = statusLabel;
                IsSelected = isSelected;
                IsUnlocked = isUnlocked;
            }

            public string Label { get; }

            public string StatusLabel { get; }

            public bool IsSelected { get; }

            public bool IsUnlocked { get; }
        }
    }
}