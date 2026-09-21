using System;
using Corebound.Gameplay.Economy;
using Corebound.Input;
using Corebound.Gameplay.Waves;
using UnityEngine;

namespace Corebound.Gameplay.Combat
{
    // Owner decision 2026-09-21: the Core stays stationary and cannot be stat-upgraded, but the
    // player may pay currency to repair it.
    //
    // This is the smallest thing that makes vision Section 5.3's promise - "Invest in offense or
    // strengthen the Core's defense?" - an actual choice. Until now there was no way to spend
    // anything on the Core at all, so that decision existed only in the document.
    //
    // Deliberately a repair and not an upgrade: repairing competes with towers and weapons for the
    // same Scrap without raising any ceiling, so it can never become a power creep sink. A player
    // who has taken no Core damage has nothing to buy here, which is the correct incentive.
    public sealed class CoreRepairService : MonoBehaviour
    {
        [SerializeField] private WaveManager _waveManager;
        [SerializeField] private EconomyService _economyService;
        [SerializeField] private CombatHitReceiver _coreHitReceiver;
        [SerializeField] private UnityEngine.Object _inputReaderSource;
        [SerializeField] private Transform _playerTransform;

        // Repair is triggered by standing near the Core and pressing Interact - the same input
        // that builds a tower when standing on a slot. Reusing one binding by proximity keeps
        // the control scheme from growing a key per verb, and it gives the Core a physical
        // place the player has to go to, which is better than a menu entry for something the
        // vision calls a defensive investment.
        [SerializeField] private float _interactRadius = 2.4f;

        // One purchase restores this fraction of the Core's maximum health. A fraction rather than
        // a flat amount so the repair stays meaningful as Core health is tuned.
        [SerializeField] [Range(0.05f, 1f)] private float _repairFraction = 0.25f;

        [SerializeField] private int _baseRepairCost = 14;

        // Each repair within the same run costs more than the last, so a player cannot simply
        // out-spend Core damage indefinitely and ignore defending it.
        [SerializeField] [Range(1f, 3f)] private float _repeatCostMultiplier = 1.5f;

        private int _repairsThisRun;

        private IInputReader InputReader => _inputReaderSource as IInputReader;

        public bool IsPlayerInRange => ResolvePlayerDistanceToCore() <= _interactRadius;

        public event Action StateChanged;

        public int RepairsThisRun => _repairsThisRun;

        public bool IsCoreDamaged => _coreHitReceiver != null && _coreHitReceiver.CurrentHealthNormalized < 0.999f;

        // Repairing is an Intermission action, like building and shopping. Allowing it mid-combat
        // would turn every Core hit into a pause-and-pay rather than a mistake with consequences.
        public bool CanRepair => IsCoreDamaged
            && _waveManager != null
            && _waveManager.State == WaveState.Intermission
            && IsPlayerInRange
            && CanAfford;

        public int CurrentRepairCost => Mathf.Max(
            1,
            Mathf.RoundToInt(_baseRepairCost * Mathf.Pow(_repeatCostMultiplier, _repairsThisRun)));

        private bool CanAfford => _economyService != null && _economyService.CanAfford(CurrentRepairCost);

        private void Awake()
        {
            if (_waveManager == null)
            {
                _waveManager = GetComponent<WaveManager>();
            }

            if (_economyService == null)
            {
                _economyService = GetComponent<EconomyService>();
            }

            ResolveCoreHitReceiver();
        }

        private void OnEnable()
        {
            if (InputReader != null)
            {
                InputReader.InteractPressed += OnInteractPressed;
            }
        }

        private void OnDisable()
        {
            if (InputReader != null)
            {
                InputReader.InteractPressed -= OnInteractPressed;
            }
        }

        private void OnInteractPressed()
        {
            TryRepair();
        }

        private float ResolvePlayerDistanceToCore()
        {
            if (_playerTransform == null)
            {
                var playerCombat = FindFirstObjectByType<PlayerCombatController>();
                _playerTransform = playerCombat != null ? playerCombat.transform : null;
            }

            ResolveCoreHitReceiver();

            if (_playerTransform == null || _coreHitReceiver == null)
            {
                return float.MaxValue;
            }

            return Vector2.Distance(_playerTransform.position, _coreHitReceiver.transform.position);
        }

        // The Core lives on its own anchor object, not on this component's GameObject, and it is
        // rebuilt on a stage change - so it is resolved by name the same way every other system
        // finds it, and re-resolved if the reference has gone stale.
        private void ResolveCoreHitReceiver()
        {
            if (_coreHitReceiver != null)
            {
                return;
            }

            var coreAnchor = GameObject.Find(CombatHitReceiver.CoreAnchorObjectName);
            _coreHitReceiver = coreAnchor != null ? coreAnchor.GetComponent<CombatHitReceiver>() : null;
        }

        public bool TryRepair()
        {
            ResolveCoreHitReceiver();

            if (!CanRepair)
            {
                return false;
            }

            // ResolveCost applies the PurchaseCostMultiplier and raises PriceModified once, which
            // is correct at the moment of an actual purchase. CurrentRepairCost is the base price;
            // TryPurchase expects the resolved one.
            var cost = _economyService.ResolveCost(CurrentRepairCost);

            if (!_economyService.TryPurchase(cost))
            {
                return false;
            }

            if (!_coreHitReceiver.TryRepairFraction(_repairFraction))
            {
                // Payment already went through, so roll it back. Same correctness guard
                // ShopService.RefundPurchase exists for, and for the same reason.
                _economyService.Refund(cost);
                return false;
            }

            _repairsThisRun++;
            StateChanged?.Invoke();
            return true;
        }

        // Called when a run starts, so the escalating cost does not carry across runs.
        public void ResetForNewRun()
        {
            _repairsThisRun = 0;
            StateChanged?.Invoke();
        }
    }
}
