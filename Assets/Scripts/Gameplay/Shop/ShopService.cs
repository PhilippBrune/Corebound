using System;
using System.Collections.Generic;
using Corebound.Gameplay.Cards;
using Corebound.Gameplay.Economy;
using Corebound.Gameplay.Resources;
using Corebound.Input;
using Corebound.SaveSystem;
using Corebound.UI;
using Corebound.UI.MainMenu;
using UnityEngine;

namespace Corebound.Gameplay.Shop
{
    public sealed class ShopService : MonoBehaviour
    {

        [SerializeField] private ShopData _fallbackShopData;
        [SerializeField] private CardService _cardService;
        [SerializeField] private Corebound.Gameplay.Towers.TowerService _towerService;
        [SerializeField] private ResourceWalletService _resourceWalletService;
        [SerializeField] private EconomyService _economyService;
        [SerializeField] private CardRewardController _cardRewardController;
        [SerializeField] private UnityEngine.Object _inputReaderSource;
        [SerializeField] private UnityEngine.Object _saveManagerSource;
        [SerializeField] private MainMenuSkinData _skinData;
        [SerializeField] private Corebound.Input.InputAssistData _inputAssistData;

        private float NavigateThreshold => _inputAssistData != null ? _inputAssistData.MenuNavigateThreshold : InputAssistData.DefaultMenuNavigateThreshold;

        private readonly List<ShopOfferData> _currentOffers = new List<ShopOfferData>();
        private readonly List<ShopOfferData> _offerCandidates = new List<ShopOfferData>();
        private int _openedFrame = -1;

        private ShopData _activeShopData;
        private int _activeStageIndex = 1;
        private int _selectedIndex;

        public event Action StateChanged;

        public event Action ShopResolved;

        public bool IsOpen { get; private set; }

        public int SelectedIndex => _selectedIndex;

        // Mouse support, as on the reward screen: the presenter hit-tests the slots (no EventSystem
        // in the gameplay scene) and drives selection and purchase through these.
        public void SelectIndex(int index)
        {
            if (!IsOpen || index == _selectedIndex || index < 0 || index >= _currentOffers.Count)
            {
                return;
            }

            _selectedIndex = index;
            StateChanged?.Invoke();
        }

        public float OpenedAtUnscaledTime { get; private set; }

        public string Title => _activeShopData != null && _activeShopData.PresentationData != null && !string.IsNullOrWhiteSpace(_activeShopData.PresentationData.Title)
            ? _activeShopData.PresentationData.Title
            : (_activeShopData != null ? _activeShopData.DisplayName : LocalizationService.Resolve("Foundry Exchange", "Schmiedebörse"));

        public string Summary => _activeShopData != null && _activeShopData.PresentationData != null
            ? _activeShopData.PresentationData.Summary
            : LocalizationService.Resolve("Spend Scrap on one targeted upgrade before tower placement.", "Gib Schrott für ein gezieltes Upgrade aus, bevor du Towers platzierst.");

        public string FooterHint => _activeShopData != null && _activeShopData.PresentationData != null
            ? _activeShopData.PresentationData.FooterHint
            : LocalizationService.Resolve("Confirm buys. Cancel skips the shop.", "Bestätigen kauft. Abbrechen überspringt den Shop.");

        public string ResourceSummary => _resourceWalletService != null ? _resourceWalletService.DisplayLabel + ": " + _resourceWalletService.CurrentAmount : string.Empty;

        public string SelectedDescription
        {
            get
            {
                if (_selectedIndex < 0 || _selectedIndex >= _currentOffers.Count)
                {
                    return string.Empty;
                }

                // Same rule as the reward screen (Reward_Screen.md): the card body carries the
                // prose, this line carries the numbers.
                var offer = _currentOffers[_selectedIndex];
                var effects = offer != null && offer.CardData != null
                    ? Corebound.Gameplay.Cards.CardEffectDescriber.Describe(offer.CardData)
                    : string.Empty;

                return (string.IsNullOrWhiteSpace(effects) ? ResolveOfferDescription(offer) : effects)
                    + LocalizationService.Resolve("   Cost ", "   Kosten ") + ResolveOfferCost(offer);
            }
        }

        public MainMenuSkinData SkinData => _skinData;

        public IReadOnlyList<ShopOfferData> CurrentOffers => _currentOffers;

        private IInputReader InputReader => _inputReaderSource as IInputReader;

        private ISaveManager SaveManager => SaveManagerResolver.Resolve(_saveManagerSource);

        private void Awake()
        {
            if (_cardService == null)
            {
                _cardService = GetComponent<CardService>();
            }

            if (_resourceWalletService == null)
            {
                _resourceWalletService = GetComponent<ResourceWalletService>();
            }

            if (_economyService == null)
            {
                _economyService = GetComponent<EconomyService>();
            }

            if (_cardRewardController == null)
            {
                _cardRewardController = GetComponent<CardRewardController>();
            }

            if (_towerService == null)
            {
                _towerService = GetComponent<Corebound.Gameplay.Towers.TowerService>();
            }

            var presenter = GetComponent<ShopPresenter>();

            if (presenter == null)
            {
                presenter = gameObject.AddComponent<ShopPresenter>();
            }
        }

        private void OnEnable()
        {
            if (InputReader != null)
            {
                InputReader.NavigatePerformed += OnNavigatePerformed;
                InputReader.ConfirmPressed += OnConfirmPressed;
                InputReader.CancelPressed += OnCancelPressed;
            }
        }

        private void OnDisable()
        {
            if (InputReader != null)
            {
                InputReader.NavigatePerformed -= OnNavigatePerformed;
                InputReader.ConfirmPressed -= OnConfirmPressed;
                InputReader.CancelPressed -= OnCancelPressed;
            }
        }

        public bool TryOpenForIntermission(Gameplay.Waves.StageData stageData, int completedWaveNumber)
        {
            var runSaveData = SaveManager != null ? SaveManager.LoadRunSaveData() : null;
            var shopData = stageData != null && stageData.ShopData != null ? stageData.ShopData : _fallbackShopData;

            if (shopData == null)
            {
                return false;
            }

            var shouldRestore = runSaveData != null && runSaveData.IsShopPending && runSaveData.PendingShopId == shopData.ShopId;

            if (!shouldRestore && !shopData.ShouldOpenAfterWave(completedWaveNumber))
            {
                return false;
            }

            _activeStageIndex = stageData != null ? stageData.StageIndex : 1;
            BuildOffers(shopData, _activeStageIndex);

            if (_currentOffers.Count == 0)
            {
                if (runSaveData != null && runSaveData.IsShopPending)
                {
                    runSaveData.ClearShopPending();
                    SaveManager.SaveRunSaveData(runSaveData);
                }

                return false;
            }

            _activeShopData = shopData;
            _selectedIndex = Mathf.Clamp(_selectedIndex, 0, _currentOffers.Count - 1);
            IsOpen = true;
            OpenedAtUnscaledTime = Time.unscaledTime;
            _openedFrame = Time.frameCount;

            if (SaveManager != null)
            {
                runSaveData = runSaveData ?? new RunSaveData();
                runSaveData.SetShopPending(shopData.ShopId, Title);
                SaveManager.SaveRunSaveData(runSaveData);
            }

            if (InputReader != null && _cardRewardController != null && _cardRewardController.RewardInputContext != null)
            {
                InputReader.ApplyContext(_cardRewardController.RewardInputContext);
                InputReader.ResetInputState();
            }

            ShopEvents.RaiseShopOpened(CreateEventArgs(_selectedIndex, false));
            StateChanged?.Invoke();
            return true;
        }

        // Shop.md: "Pools, Gewichtungen und Stage-spezifische Verfuegbarkeiten". Offers used to
        // be taken in array order until the cap, so the first three available entries were the
        // only three a player ever saw. Now every available offer competes by weight, drawn
        // without replacement - the same approach CardService already uses for rarity.
        private void BuildOffers(ShopData shopData, int stageIndex)
        {
            _currentOffers.Clear();
            _offerCandidates.Clear();

            var offerTable = shopData != null ? shopData.OfferTableData : null;
            var offers = offerTable != null ? offerTable.Offers : null;

            if (offers == null || _cardService == null)
            {
                return;
            }

            for (var index = 0; index < offers.Length; index++)
            {
                if (IsOfferAvailable(offers[index], stageIndex))
                {
                    _offerCandidates.Add(offers[index]);
                }
            }

            while (_currentOffers.Count < offerTable.MaxOfferCount && _offerCandidates.Count > 0)
            {
                var drawnIndex = DrawWeightedCandidateIndex();

                if (drawnIndex < 0)
                {
                    break;
                }

                _currentOffers.Add(_offerCandidates[drawnIndex]);
                _offerCandidates.RemoveAt(drawnIndex);
            }
        }

        // Returns -1 when every remaining candidate is weighted 0, which is the author saying
        // "never offer these" - filling the slot anyway would defeat the weight.
        private int DrawWeightedCandidateIndex()
        {
            var totalWeight = 0f;

            for (var index = 0; index < _offerCandidates.Count; index++)
            {
                totalWeight += _offerCandidates[index].SelectionWeight;
            }

            if (totalWeight <= Mathf.Epsilon)
            {
                return -1;
            }

            var roll = UnityEngine.Random.value * totalWeight;

            for (var index = 0; index < _offerCandidates.Count; index++)
            {
                roll -= _offerCandidates[index].SelectionWeight;

                if (roll <= 0f)
                {
                    return index;
                }
            }

            return _offerCandidates.Count - 1;
        }

        private bool IsOfferAvailable(ShopOfferData offer, int stageIndex)
        {
            if (offer == null || stageIndex < offer.MinStageIndex || stageIndex > offer.MaxStageIndex)
            {
                return false;
            }

            switch (offer.OfferType)
            {
                case ShopOfferType.Card:
                    return offer.CardData != null && _cardService != null && _cardService.CanOfferCard(offer.CardData);
                case ShopOfferType.TowerUpgrade:
                    return _towerService != null && _towerService.CanUpgradeSelectedTowerForShop();
                default:
                    return false;
            }
        }

        private void OnNavigatePerformed(Vector2 navigationInput)
        {
            if (!IsOpen || Mathf.Abs(navigationInput.x) < NavigateThreshold || Mathf.Abs(navigationInput.x) < Mathf.Abs(navigationInput.y) || _currentOffers.Count == 0)
            {
                return;
            }

            _selectedIndex += navigationInput.x > 0f ? 1 : -1;

            if (_selectedIndex < 0)
            {
                _selectedIndex = _currentOffers.Count - 1;
            }
            else if (_selectedIndex >= _currentOffers.Count)
            {
                _selectedIndex = 0;
            }

            ShopEvents.RaiseShopOfferSelected(CreateEventArgs(_selectedIndex, false));
            StateChanged?.Invoke();
        }

        // Mouse click on a slot (the presenter hit-tests, there is no EventSystem in this scene).
        public void TryPurchaseSelectedOffer()
        {
            OnConfirmPressed();
        }

        private void OnConfirmPressed()
        {
            if (!IsOpen || _selectedIndex < 0 || _selectedIndex >= _currentOffers.Count)
            {
                return;
            }

            // The shop opens *during* a Confirm press, not between two of them: confirming the card
            // reward closes the reward screen, which synchronously raises StateChanged ->
            // WaveManager.OnRewardStateChanged -> StartIntermission -> TryOpenForIntermission. All
            // of that happens inside the ConfirmPressed invocation list, and this handler is also
            // subscribed to ConfirmPressed - so the very press that opened the shop arrived here a
            // moment later and resolved it immediately. The shop was opening and closing within a
            // single frame, every time, which is why three playtests in a row reported "shop still
            // not shown" while the offers were being built correctly.
            //
            // Same shape as WaveManager._intermissionGateResolvedFrame, which exists for the same
            // reason on the same event.
            if (Time.frameCount == _openedFrame)
            {
                return;
            }

            var offer = _currentOffers[_selectedIndex];

            if (offer == null)
            {
                return;
            }

            var cost = ResolveOfferCost(offer);

            ShopEvents.RaisePurchaseRequested(CreateEventArgs(_selectedIndex, false));

            if (!CanPurchase(offer) || !TryCommitPurchase(cost))
            {
                ShopEvents.RaisePurchaseDeclined(CreateEventArgs(_selectedIndex, false));
                return;
            }

            var purchased = false;

            switch (offer.OfferType)
            {
                case ShopOfferType.Card:
                    purchased = _cardService != null && _cardService.TryApplyCard(offer.CardData, cost);
                    break;
                case ShopOfferType.TowerUpgrade:
                    purchased = _towerService != null && _towerService.TryApplyUpgradeFromShop();
                    break;
            }

            if (!purchased)
            {
                RefundPurchase(cost);
                return;
            }

            ShopEvents.RaisePurchaseSucceeded(CreateEventArgs(_selectedIndex, true));

            // Playtest 2026-09-21: "currently i can only buy one item in the shop, make it
            // unlimited as long as the player has the money". Closing on the first purchase
            // made every shop visit a single forced choice; the player could not spend a good
            // Scrap balance even when three offers were all affordable.
            //
            // The bought offer is removed rather than left in place, so a card cannot be bought
            // twice from one visit, and the shop closes on its own once nothing is left.
            _currentOffers.RemoveAt(_selectedIndex);

            if (_currentOffers.Count == 0)
            {
                CloseShop(true);
                return;
            }

            _selectedIndex = Mathf.Clamp(_selectedIndex, 0, _currentOffers.Count - 1);
            StateChanged?.Invoke();
        }

        private bool TryCommitPurchase(int cost)
        {
            if (_economyService != null)
            {
                return _economyService.TryPurchase(cost);
            }

            return _resourceWalletService != null && _resourceWalletService.TrySpend(cost);
        }

        private void RefundPurchase(int amount)
        {
            if (_economyService != null)
            {
                _economyService.Refund(amount);
                return;
            }

            _resourceWalletService?.AddReward(amount);
        }

        private void OnCancelPressed()
        {
            // Same single-frame guard as OnConfirmPressed: whichever press opened the shop must
            // not also dismiss it.
            if (!IsOpen || Time.frameCount == _openedFrame)
            {
                return;
            }

            CloseShop(false);
        }

        // Shop.md requires the UI show "Kosten, Kaufbarkeit, Seltenheit". Affordability used to
        // be evaluated only when Confirm was pressed, so an unaffordable offer looked exactly
        // like an affordable one. ShopPresenter reads this per offer while drawing.
        public bool CanAffordOffer(ShopOfferData offer)
        {
            return offer != null
                && _resourceWalletService != null
                && _resourceWalletService.CurrentAmount >= ResolveOfferCost(offer);
        }

        private bool CanPurchase(ShopOfferData offer)
        {
            return offer != null
                && _resourceWalletService != null
                && _resourceWalletService.CurrentAmount >= ResolveOfferCost(offer)
                && IsOfferAvailable(offer, _activeStageIndex);
        }

        private void CloseShop(bool purchased)
        {
            if (!IsOpen)
            {
                return;
            }

            IsOpen = false;

            if (SaveManager != null)
            {
                var runSaveData = SaveManager.LoadRunSaveData() ?? new RunSaveData();
                runSaveData.ClearShopPending();
                SaveManager.SaveRunSaveData(runSaveData);
            }

            if (InputReader != null && _cardRewardController != null && _cardRewardController.GameplayInputContext != null)
            {
                InputReader.ApplyContext(_cardRewardController.GameplayInputContext);
                InputReader.ResetInputState();
            }

            ShopEvents.RaiseShopClosed(CreateEventArgs(_selectedIndex, purchased));
            ShopResolved?.Invoke();
            StateChanged?.Invoke();
        }

        public int ResolveOfferCost(ShopOfferData offer)
        {
            if (offer == null)
            {
                return 0;
            }

            var multiplier = _activeShopData != null ? _activeShopData.StagePriceMultiplier : 1f;
            var stageAdjustedCost = Mathf.Max(0, Mathf.RoundToInt(offer.Cost * multiplier));
            return _economyService != null ? _economyService.ResolveCost(stageAdjustedCost) : stageAdjustedCost;
        }

        public string ResolveOfferTitle(ShopOfferData offer)
        {
            if (offer == null)
            {
                return string.Empty;
            }

            if (!string.IsNullOrWhiteSpace(offer.LabelOverride))
            {
                return offer.LabelOverride;
            }

            switch (offer.OfferType)
            {
                case ShopOfferType.Card:
                    return offer.CardData != null ? offer.CardData.DisplayName : "Card";
                case ShopOfferType.TowerUpgrade:
                    return _towerService != null ? _towerService.SelectedTowerUpgradeTitle() : "Tower Upgrade";
                default:
                    return offer.OfferId;
            }
        }

        public string ResolveOfferDescription(ShopOfferData offer)
        {
            if (offer == null)
            {
                return string.Empty;
            }

            if (!string.IsNullOrWhiteSpace(offer.DescriptionOverride))
            {
                return offer.DescriptionOverride;
            }

            switch (offer.OfferType)
            {
                case ShopOfferType.Card:
                    return offer.CardData != null ? offer.CardData.Description : string.Empty;
                case ShopOfferType.TowerUpgrade:
                    return _towerService != null ? _towerService.SelectedTowerUpgradeDescription() : string.Empty;
                default:
                    return string.Empty;
            }
        }

        public CardRarity ResolveOfferRarity(ShopOfferData offer)
        {
            if (offer != null && offer.OfferType == ShopOfferType.Card && offer.CardData != null)
            {
                return offer.CardData.Rarity;
            }

            return CardRarity.Common;
        }

        private ShopEventArgs CreateEventArgs(int offerIndex, bool purchased)
        {
            var offer = offerIndex >= 0 && offerIndex < _currentOffers.Count ? _currentOffers[offerIndex] : null;
            return new ShopEventArgs(
                _activeShopData != null ? _activeShopData.ShopId : string.Empty,
                offer != null ? offer.OfferId : string.Empty,
                ResolveOfferTitle(offer),
                offerIndex,
                ResolveOfferCost(offer),
                purchased);
        }
    }
}