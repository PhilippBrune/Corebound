using System;
using Corebound.Gameplay.Economy;
using Corebound.Gameplay.Waves;
using Corebound.Input;
using Corebound.UI;
using UnityEngine;

namespace Corebound.Gameplay.Combat
{
    // HP recovery (owner request 2026-09-28; design recommendation 1 in OPEN_TOPICS section 4.4).
    //
    // Until now nothing healed the player, so a chip of damage taken in Stage 1 was still there at
    // the Stage 5 boss and a long run decayed rather than being lost to any particular mistake.
    //
    // Deliberately the *same* verb and place as repairing the Core: stand near the Core during an
    // Intermission and press Interact. That reuses a binding, a radius and a prompt the player has
    // already learned, and it keeps Scrap genuinely contested - every point spent on staying alive
    // is a point not spent on a tower or a card, which is the decision this mechanic exists to
    // create.
    //
    // A separate component rather than a branch inside CoreRepairService, because the interact
    // router already arbitrates by priority and that is exactly the question here: when both the
    // Core and the player are hurt, the Core wins, since losing the Core ends the run outright.
    public sealed class PlayerRepairService : MonoBehaviour, IInteractHandler
    {
        [SerializeField] private WaveManager _waveManager;
        [SerializeField] private EconomyService _economyService;
        [SerializeField] private PlayerCombatController _playerCombatController;
        [SerializeField] private CoreRepairService _coreRepairService;
        [SerializeField] private UnityEngine.Object _inputReaderSource;
        [SerializeField] private Transform _playerTransform;

        [SerializeField] private float _interactRadius = 2.4f;

        // A fraction of maximum health, so the repair keeps its meaning as player health is tuned.
        [SerializeField] [Range(0.05f, 1f)] private float _repairFraction = 0.3f;

        // Cheaper than a Core repair at base, because the Core is the thing that ends the run and
        // should stay the more valuable purchase when both are damaged.
        [SerializeField] private int _baseRepairCost = 10;

        // Rises per use within a run, for the same reason the Core's does: a player must not be
        // able to simply out-spend incoming damage and ignore positioning.
        [SerializeField] [Range(1f, 3f)] private float _repeatCostMultiplier = 1.45f;

        private int _repairsThisRun;
        private Transform _coreTransform;

        private IInputReader InputReader => _inputReaderSource as IInputReader;

        public event Action StateChanged;

        public int RepairsThisRun => _repairsThisRun;

        public bool IsPlayerDamaged => _playerCombatController != null && _playerCombatController.CurrentHealthNormalized < 0.999f;

        public int CurrentRepairCost => Mathf.Max(
            1,
            Mathf.RoundToInt(_baseRepairCost * Mathf.Pow(_repeatCostMultiplier, _repairsThisRun)));

        public bool IsPlayerInRange => ResolvePlayerDistanceToCore() <= _interactRadius;

        // An Intermission action, like building, shopping and repairing the Core. Healing mid-combat
        // would turn every hit into a pause-and-pay rather than a mistake with consequences.
        public bool CanRepair => IsPlayerDamaged
            && _waveManager != null
            && _waveManager.State == WaveState.Intermission
            && IsPlayerInRange
            && _economyService != null
            && _economyService.CanAfford(CurrentRepairCost);

        public InteractPriority InteractPriority => InteractPriority.PlayerRepair;

        // Yields to the Core while the Core is also repairable. Both handlers sit in the same
        // router, so without this the player could heal themselves while the Core - the thing that
        // actually ends the run - stayed damaged and in range.
        public bool CanHandleInteract => CanRepair && (_coreRepairService == null || !_coreRepairService.CanRepair);

        public void HandleInteract()
        {
            TryRepair();
        }

        private void Awake()
        {
            if (_waveManager == null)
            {
                _waveManager = FindFirstObjectByType<WaveManager>();
            }

            if (_economyService == null)
            {
                _economyService = FindFirstObjectByType<EconomyService>();
            }

            if (_coreRepairService == null)
            {
                _coreRepairService = FindFirstObjectByType<CoreRepairService>();
            }

            if (_playerCombatController == null)
            {
                _playerCombatController = FindFirstObjectByType<PlayerCombatController>();
            }

            if (_playerTransform == null && _playerCombatController != null)
            {
                _playerTransform = _playerCombatController.transform;
            }

            if (_inputReaderSource == null && _waveManager != null)
            {
                _inputReaderSource = _waveManager.InputReaderSource;
            }
        }

        private void OnEnable()
        {
            if (InputReader != null)
            {
                InputReader.InteractPressed += OnInteractPressed;
                InteractRouter.Register(this);
            }
        }

        private void OnDisable()
        {
            if (InputReader != null)
            {
                InputReader.InteractPressed -= OnInteractPressed;
                InteractRouter.Unregister(this);
            }
        }

        private void OnInteractPressed()
        {
            InteractRouter.Dispatch();
        }

        public bool TryRepair()
        {
            if (!CanRepair)
            {
                return false;
            }

            var cost = _economyService.ResolveCost(CurrentRepairCost);

            if (!_economyService.TryPurchase(cost))
            {
                return false;
            }

            if (!_playerCombatController.TryRestoreHealthFraction(_repairFraction))
            {
                // Payment already went through, so roll it back - the same guard the Core repair
                // and the shop both carry, for the same reason.
                _economyService.Refund(cost);
                return false;
            }

            _repairsThisRun++;
            NotificationPresenter.Show(LocalizationService.Resolve(
                "Repaired - " + Mathf.RoundToInt(_repairFraction * 100f) + "% health",
                "Repariert - " + Mathf.RoundToInt(_repairFraction * 100f) + "% Leben"));
            StateChanged?.Invoke();
            return true;
        }

        // Called when a run starts, so the escalating cost does not carry across runs.
        public void ResetForNewRun()
        {
            _repairsThisRun = 0;
            StateChanged?.Invoke();
        }

        private float ResolvePlayerDistanceToCore()
        {
            if (_coreTransform == null)
            {
                var coreObject = GameObject.Find(CombatHitReceiver.CoreAnchorObjectName);
                _coreTransform = coreObject != null ? coreObject.transform : null;
            }

            if (_coreTransform == null || _playerTransform == null)
            {
                return float.MaxValue;
            }

            return Vector2.Distance(_playerTransform.position, _coreTransform.position);
        }
    }
}
