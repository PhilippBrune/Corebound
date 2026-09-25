using System;
using System.Collections.Generic;
using Corebound.Gameplay.Resources;
using Corebound.Gameplay.Towers;
using Corebound.Gameplay.Waves;
using Corebound.Input;
using Corebound.SaveSystem;
using Corebound.UI;
using Corebound.UI.MainMenu;
using UnityEngine;

namespace Corebound.Gameplay.Cards
{
    public sealed class CardRewardController : MonoBehaviour
    {

        [SerializeField] private CardService _cardService;
        [SerializeField] private ResourceWalletService _resourceWalletService;
        [SerializeField] private TowerService _towerService;
        [SerializeField] private UnityEngine.Object _inputReaderSource;
        [SerializeField] private UnityEngine.Object _saveManagerSource;
        [SerializeField] private InputContextData _rewardInputContext;
        [SerializeField] private InputContextData _gameplayInputContext;
        [SerializeField] private InputAssistData _inputAssistData;

        private float NavigateThreshold => _inputAssistData != null ? _inputAssistData.MenuNavigateThreshold : InputAssistData.DefaultMenuNavigateThreshold;

        private bool _isOpen;
        private int _selectedIndex;
        private string _pendingRewardLabel = string.Empty;

        public event Action StateChanged;

        public bool IsOpen => _isOpen;

        public int SelectedIndex => _selectedIndex;

        public string PendingRewardLabel => _pendingRewardLabel;

        public InputContextData RewardInputContext => _rewardInputContext;

        public InputContextData GameplayInputContext => _gameplayInputContext;

        // Reward_Screen.md: "Live run currency display (Scrap / Alloy counter)". Alloy is permanent,
        // so what matters on this screen is what the current run has earned of it so far.
        public string ResourceSummary => _resourceWalletService != null
            ? _resourceWalletService.DisplayLabel + ": " + _resourceWalletService.CurrentAmount
                + LocalizationService.Resolve("  ·  Alloy +", "  ·  Legierung +") + Corebound.MetaProgression.MetaRewardLedger.RunAlloy
            : string.Empty;

        public string TowerSummary => _towerService != null ? LocalizationService.Resolve("Active Towers: ", "Aktive Tuerme: ") + _towerService.BuiltTowerCount : string.Empty;

        public string SelectedDescription => _cardService != null && _cardService.CurrentOffer.Count > 0 && _selectedIndex >= 0 && _selectedIndex < _cardService.CurrentOffer.Count
            ? CardEffectDescriber.Describe(_cardService.CurrentOffer[_selectedIndex])
            : string.Empty;

        public float OpenedAtUnscaledTime { get; private set; }

        // Mouse support for the reward screen (the gameplay scene has no EventSystem, so the
        // presenter hit-tests the slots itself and drives selection through these).
        public void SelectIndex(int index)
        {
            if (!_isOpen || index == _selectedIndex || index < 0 || index >= GetCurrentOffer().Count)
            {
                return;
            }

            _selectedIndex = index;
            StateChanged?.Invoke();
        }

        public void ConfirmSelection()
        {
            OnConfirmPressed();
        }

        private IInputReader InputReader => _inputReaderSource as IInputReader;

        private ISaveManager SaveManager => SaveManagerResolver.Resolve(_saveManagerSource);

        private void OnEnable()
        {
            if (InputReader != null)
            {
                InputReader.NavigatePerformed += OnNavigatePerformed;
                InputReader.ConfirmPressed += OnConfirmPressed;
                InputReader.CancelPressed += OnCancelPressed;
                InputReader.PreviousPressed += OnRerollPressed;
            }

            WaveEvents.RewardPending += OnRewardPending;
            TryOpenFromRunSave();
        }

        private void OnDisable()
        {
            if (InputReader != null)
            {
                InputReader.NavigatePerformed -= OnNavigatePerformed;
                InputReader.ConfirmPressed -= OnConfirmPressed;
                InputReader.CancelPressed -= OnCancelPressed;
                InputReader.PreviousPressed -= OnRerollPressed;
            }

            WaveEvents.RewardPending -= OnRewardPending;
        }

        public IReadOnlyList<CardData> GetCurrentOffer()
        {
            return _cardService != null ? _cardService.CurrentOffer : Array.Empty<CardData>();
        }

        private void TryOpenFromRunSave()
        {
            if (SaveManager == null || _cardService == null)
            {
                return;
            }

            var runSaveData = SaveManager.LoadRunSaveData();

            if (runSaveData == null || !runSaveData.HasPendingReward)
            {
                return;
            }

            _pendingRewardLabel = string.IsNullOrWhiteSpace(runSaveData.PendingRewardLabel) ? LocalizationService.Resolve("Card Reward", "Kartenbelohnung") : runSaveData.PendingRewardLabel;
            _isOpen = true;
            OpenedAtUnscaledTime = Time.unscaledTime;
            _selectedIndex = 0;
            _cardService.BuildRewardOffer();

            if (InputReader != null && _rewardInputContext != null)
            {
                InputReader.ApplyContext(_rewardInputContext);
                InputReader.ResetInputState();
            }

            StateChanged?.Invoke();
        }

        private void CloseReward()
        {
            _isOpen = false;
            _pendingRewardLabel = string.Empty;

            if (InputReader != null && _gameplayInputContext != null)
            {
                InputReader.ApplyContext(_gameplayInputContext);
                InputReader.ResetInputState();
            }

            StateChanged?.Invoke();
        }

        private void OnNavigatePerformed(Vector2 navigationInput)
        {
            if (!_isOpen || Mathf.Abs(navigationInput.x) < NavigateThreshold || Mathf.Abs(navigationInput.x) < Mathf.Abs(navigationInput.y))
            {
                return;
            }

            var offer = GetCurrentOffer();

            if (offer.Count == 0)
            {
                return;
            }

            _selectedIndex += navigationInput.x > 0f ? 1 : -1;

            if (_selectedIndex < 0)
            {
                _selectedIndex = offer.Count - 1;
            }
            else if (_selectedIndex >= offer.Count)
            {
                _selectedIndex = 0;
            }

            StateChanged?.Invoke();
        }

        // Skill Tree "Wider Draft": redraw the three cards, as often as the tree grants.
        private void OnRerollPressed()
        {
            if (!_isOpen || _cardService == null || !_cardService.TryRerollOffer())
            {
                return;
            }

            _selectedIndex = 0;
            StateChanged?.Invoke();
        }

        public CardRarity ResolveOfferedRarity(CardData card)
        {
            return _cardService != null ? _cardService.ResolveOfferedRarity(card) : (card != null ? card.Rarity : CardRarity.Common);
        }

        public int RemainingRerolls => _cardService != null ? _cardService.RemainingRerolls : 0;

        private void OnConfirmPressed()
        {
            if (!_isOpen || _cardService == null || SaveManager == null)
            {
                return;
            }

            var offer = GetCurrentOffer();

            if (_selectedIndex < 0 || _selectedIndex >= offer.Count)
            {
                return;
            }

            if (!_cardService.TryApplyCard(offer[_selectedIndex]))
            {
                return;
            }

            var runSaveData = SaveManager.LoadRunSaveData() ?? new RunSaveData();
            runSaveData.ClearPendingReward();
            runSaveData.SetIntermissionOpen(false, runSaveData.StageIndex, runSaveData.WaveIndex);
            SaveManager.SaveRunSaveData(runSaveData);
            CloseReward();
        }

        private void OnCancelPressed()
        {
            if (!_isOpen)
            {
                return;
            }

            StateChanged?.Invoke();
        }

        private void OnRewardPending(WaveProgressEventArgs eventArgs)
        {
            if (_isOpen)
            {
                return;
            }

            TryOpenFromRunSave();
        }
    }
}