using System;
using System.Collections.Generic;
using Corebound.Gameplay.Cards;
using Corebound.Gameplay.Combat;
using Corebound.Gameplay.Economy;
using Corebound.Gameplay.Resources;
using Corebound.Gameplay.Shop;
using Corebound.Gameplay.Shared;
using Corebound.Input;
using Corebound.MetaProgression;
using Corebound.SaveSystem;
using Corebound.UI;
using UnityEngine;
using Corebound.Gameplay.Waves;

namespace Corebound.Gameplay.Towers
{
    public sealed class TowerService : MonoBehaviour
    {
        private const string GameplayMapName = "Gameplay";
        private const string InteractActionName = "Interact";
        private const string BuildMapName = "Build";
        private const string PreviousTowerTypeActionName = "PreviousTowerType";
        private const string NextTowerTypeActionName = "NextTowerType";

        [SerializeField] private TowerCatalogData _towerCatalog;
        [SerializeField] private UnityEngine.Object _saveManagerSource;
        [SerializeField] private MetaProgressionService _metaProgressionService;
        [SerializeField] private CardService _cardService;
        [SerializeField] private ResourceWalletService _resourceWalletService;
        [SerializeField] private EconomyService _economyService;
        [SerializeField] private UnityEngine.Object _inputReaderSource;
        [SerializeField] private Transform _playerTarget;
        [SerializeField] private CombatProjectile _projectilePrototype;
        [SerializeField] private Transform _projectileParent;
        [SerializeField] private Texture2D _slotMarkerTexture;
        [SerializeField] private float _slotInteractDistance = 1.4f;
        [SerializeField] private Vector2[] _slotPositions =
        {
            new Vector2(-3.6f, -1.6f),
            new Vector2(-1.2f, -2.25f),
            new Vector2(1.2f, -2.25f),
            new Vector2(3.6f, -1.6f)
        };
        [SerializeField] private string[] _defaultBuiltTowerIds = Array.Empty<string>();

        private readonly List<string> _builtTowerIds = new List<string>();
        private readonly List<int> _builtTowerUpgradeLevels = new List<int>();
        private readonly List<TowerRuntimeController> _runtimeTowers = new List<TowerRuntimeController>();
        private readonly List<TowerSlotRuntime> _runtimeSlots = new List<TowerSlotRuntime>();

        private CardRewardController _cardRewardController;
        private ProjectilePool _runtimeProjectilePool;
        private readonly List<TowerData> _unlockedTowerBuffer = new List<TowerData>();
        private Sprite _slotMarkerSprite;
        private int _selectedSlotIndex;
        private Transform _playerTransform;
        private bool _manualSlotSelectionThisIntermission;
        private int _selectedTowerTypeIndex;
        private ShopService _shopService;
        private WaveManager _waveManager;

        public event Action TowersChanged;

        public event Action IntermissionBuildCommitted;

        public int BuiltTowerCount => _builtTowerIds.Count;

        public int CountBuiltTowersById(string towerId)
        {
            if (string.IsNullOrWhiteSpace(towerId))
            {
                return 0;
            }

            var count = 0;

            for (var index = 0; index < _builtTowerIds.Count; index++)
            {
                if (_builtTowerIds[index] == towerId)
                {
                    count++;
                }
            }

            return count;
        }

        public float TowerDamageMultiplier => _cardService != null ? _cardService.TowerDamageMultiplier : 1f;

        public string SelectedSlotSummary => ResolveSelectedSlotSummary();

        public bool HasAvailableIntermissionAction => ResolveHasAvailableIntermissionAction();

        public string BuildInteractKeyLabel => ResolveBuildInteractKeyLabel();

        private ISaveManager SaveManager => SaveManagerResolver.Resolve(_saveManagerSource);

        private IInputReader InputReader => _inputReaderSource as IInputReader;

        private ProjectilePool RuntimeProjectilePool => _runtimeProjectilePool ?? (_runtimeProjectilePool = new ProjectilePool(_projectileParent));

        private void Awake()
        {
            _cardRewardController = GetComponent<CardRewardController>();
            _shopService = GetComponent<ShopService>();
            _waveManager = GetComponent<WaveManager>();
            _slotMarkerSprite = CreateSlotMarkerSprite();
            LoadBuiltTowerIds();
            CreateSlotRuntimes();
            EnsureBuiltTowerSeed();
            SpawnRuntimeTowers();
            UpdateSlotVisuals();
        }

        private void OnEnable()
        {
            if (InputReader != null)
            {
                InputReader.PreviousPressed += OnPreviousPressed;
                InputReader.NextPressed += OnNextPressed;
                InputReader.InteractPressed += OnInteractPressed;
                InputReader.PreviousTowerTypePressed += OnPreviousTowerTypePressed;
                InputReader.NextTowerTypePressed += OnNextTowerTypePressed;
            }

            if (_waveManager != null)
            {
                _waveManager.StateChanged += OnWaveStateChanged;
            }
        }

        private void OnDisable()
        {
            if (InputReader != null)
            {
                InputReader.PreviousPressed -= OnPreviousPressed;
                InputReader.NextPressed -= OnNextPressed;
                InputReader.InteractPressed -= OnInteractPressed;
                InputReader.PreviousTowerTypePressed -= OnPreviousTowerTypePressed;
                InputReader.NextTowerTypePressed -= OnNextTowerTypePressed;
            }

            if (_waveManager != null)
            {
                _waveManager.StateChanged -= OnWaveStateChanged;
            }
        }

        // isBuildActive (and therefore whether the slot marker sprites are even enabled) is only
        // recomputed from _waveManager.State on tower/slot selection actions - without this,
        // slots stayed disabled from Combat state straight through the Intermission/build phase
        // until the player happened to press Previous/Next, making every slot invisible even
        // though "Stand at a glowing Tower Slot" was telling them to look for one.
        private void OnWaveStateChanged()
        {
            // A manual Prev/Next override lasts only for the intermission it was made in.
            // Carrying it across waves would silently disable proximity selection for the rest
            // of the run after a single button press.
            if (_waveManager != null && _waveManager.State != WaveState.Intermission)
            {
                _manualSlotSelectionThisIntermission = false;
            }

            UpdateSlotVisuals();
        }

        private void OnDestroy()
        {
            if (_slotMarkerSprite == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(_slotMarkerSprite);
            }
            else
            {
                DestroyImmediate(_slotMarkerSprite);
            }
        }

        public CombatProjectile GetProjectileInstance(CombatProjectile projectilePrototype, Transform projectileParent)
        {
            return RuntimeProjectilePool.Get(projectilePrototype);
        }

        public bool IsTowerUnlocked(TowerData towerData)
        {
            return towerData != null && MetaProgressionService.IsUnlockSatisfied(_metaProgressionService, SaveManager, towerData.RequiredUnlockId);
        }

        private void LoadBuiltTowerIds()
        {
            _builtTowerIds.Clear();
            _builtTowerUpgradeLevels.Clear();

            if (_towerCatalog == null || SaveManager == null)
            {
                return;
            }

            var runSaveData = SaveManager.LoadRunSaveData();

            if (runSaveData == null || runSaveData.BuiltTowerIds == null)
            {
                return;
            }

            for (var index = 0; index < runSaveData.BuiltTowerIds.Count; index++)
            {
                _builtTowerIds.Add(runSaveData.BuiltTowerIds[index]);
                _builtTowerUpgradeLevels.Add(runSaveData.BuiltTowerUpgradeLevels != null && index < runSaveData.BuiltTowerUpgradeLevels.Count ? Mathf.Max(0, runSaveData.BuiltTowerUpgradeLevels[index]) : 0);
            }
        }

        private void EnsureBuiltTowerSeed()
        {
            if (_builtTowerIds.Count > 0 || _defaultBuiltTowerIds == null)
            {
                return;
            }

            for (var index = 0; index < _defaultBuiltTowerIds.Length; index++)
            {
                var tower = _towerCatalog != null ? _towerCatalog.FindById(_defaultBuiltTowerIds[index]) : null;

                if (tower == null || !IsTowerUnlocked(tower))
                {
                    continue;
                }

                _builtTowerIds.Add(tower.TowerId);
                _builtTowerUpgradeLevels.Add(0);
            }

            SaveBuiltTowerState();
        }

        private void SaveBuiltTowerState()
        {
            if (SaveManager == null)
            {
                return;
            }

            var runSaveData = SaveManager.LoadRunSaveData() ?? new RunSaveData();
            runSaveData.SetBuiltTowerIds(_builtTowerIds);
            runSaveData.SetBuiltTowerUpgradeLevels(_builtTowerUpgradeLevels);
            SaveManager.SaveRunSaveData(runSaveData);
        }

        private void SpawnRuntimeTowers()
        {
            for (var index = 0; index < _builtTowerIds.Count; index++)
            {
                if (index >= _runtimeSlots.Count)
                {
                    break;
                }

                var towerData = _towerCatalog != null ? _towerCatalog.FindById(_builtTowerIds[index]) : null;

                if (towerData == null)
                {
                    continue;
                }

                var upgradeLevel = index < _builtTowerUpgradeLevels.Count ? _builtTowerUpgradeLevels[index] : 0;
                SpawnTowerAtSlot(index, towerData, false, upgradeLevel);
            }

            TowersChanged?.Invoke();
        }

        // tower_slot_marker.png is ~10.9x6.1 world units at its native pixels-per-unit, but slots
        // sit as close as 2.4 units apart - without scaling to fit, all four markers overlap into
        // one indistinguishable blob instead of four separate glowing slots.
        private static readonly Vector2 SlotMarkerTargetWorldSize = new Vector2(1.8f, 1.8f);

        // Dynamic Arena Slice (Docs/Slices/15_DYNAMIC_ARENA_SLICE.md) flagged this as a known,
        // deliberately deferred gap: CreateSlotRuntimes() only ran once from Awake(), so a player who
        // reached Stage 2 in the same run still saw Stage 1's slot layout ("Towerslots... still
        // static placement" - playtest feedback, 2026-09-17). Mirrors
        // StageArenaArtPresenter.RebuildForNewStage(), which WaveManager.AdvanceToNextStage() already
        // calls right after swapping _stageData - called from the same place, right after.
        // _builtTowerIds/_builtTowerUpgradeLevels (the actual owned-tower data, index-matched to slot
        // index) are untouched here, so towers already built carry over onto the new stage's slots at
        // the same index; if the new stage has fewer slots than towers owned, SpawnRuntimeTowers()'s
        // existing `index >= _runtimeSlots.Count` guard already skips the excess safely.
        public void RebuildSlotsForNewStage()
        {
            for (var index = 0; index < _runtimeTowers.Count; index++)
            {
                if (_runtimeTowers[index] != null)
                {
                    Destroy(_runtimeTowers[index].gameObject);
                }
            }

            _runtimeTowers.Clear();

            for (var index = 0; index < _runtimeSlots.Count; index++)
            {
                Destroy(_runtimeSlots[index].Transform.gameObject);
            }

            CreateSlotRuntimes();
            SpawnRuntimeTowers();
            UpdateSlotVisuals();
        }

        private void CreateSlotRuntimes()
        {
            _runtimeSlots.Clear();

            var slotPositions = ResolveActiveSlotPositions();

            if (slotPositions == null)
            {
                return;
            }

            for (var index = 0; index < slotPositions.Length; index++)
            {
                var slotObject = new GameObject("TowerSlot_" + index);
                slotObject.transform.SetParent(transform, false);
                slotObject.transform.position = new Vector3(slotPositions[index].x, slotPositions[index].y, 0f);

                var spriteRenderer = slotObject.AddComponent<SpriteRenderer>();
                var sprite = _slotMarkerSprite != null ? _slotMarkerSprite : DebugSpriteFactory.CreateSquare(48f);
                spriteRenderer.sprite = sprite;
                spriteRenderer.sortingOrder = -5;

                var baseScale = ResolveSlotMarkerBaseScale(sprite);
                slotObject.transform.localScale = new Vector3(baseScale, baseScale, 1f);

                _runtimeSlots.Add(new TowerSlotRuntime(slotObject.transform, spriteRenderer, baseScale));
            }
        }

        // Dynamic Arena Slice (Docs/Slices/15_DYNAMIC_ARENA_SLICE.md): prefers the active stage's
        // own tower-slot layout when it has authored one, falling back to this component's own
        // serialized _slotPositions (today's Stage 1 layout, hand-placed in the scene) otherwise -
        // a stage with no authored layout builds identically to before this change.
        // Resolution order, most specific first:
        //   1. StageArenaLayoutData.BuildableZones - the Dynamic Arena Slice's shape-constrained
        //      placement: a stage declares regions and how many slots each holds, and the slot
        //      count is free to differ per stage (a bigger arena can carry more towers).
        //   2. StageData.TowerSlotPositions - the earlier flat per-stage point list, still honoured
        //      so stages authored against it keep working.
        //   3. This component's own serialized array - Stage 1's original layout.
        private Vector2[] ResolveActiveSlotPositions()
        {
            var stageData = _waveManager != null ? _waveManager.StageDataAsset : null;
            var layoutData = stageData != null && stageData.ThemeData != null ? stageData.ThemeData.LayoutData : null;

            if (layoutData != null && layoutData.HasBuildableZones)
            {
                var zoneSlotPositions = layoutData.ResolveTowerSlotPositions();

                if (zoneSlotPositions != null && zoneSlotPositions.Length > 0)
                {
                    return zoneSlotPositions;
                }
            }

            var stageSlotPositions = stageData != null ? stageData.TowerSlotPositions : null;

            return stageSlotPositions != null && stageSlotPositions.Length > 0 ? stageSlotPositions : _slotPositions;
        }

        private static float ResolveSlotMarkerBaseScale(Sprite sprite)
        {
            if (sprite == null)
            {
                return 1f;
            }

            var bounds = sprite.bounds.size;

            if (bounds.x <= Mathf.Epsilon || bounds.y <= Mathf.Epsilon)
            {
                return 1f;
            }

            return Mathf.Min(SlotMarkerTargetWorldSize.x / bounds.x, SlotMarkerTargetWorldSize.y / bounds.y);
        }

        // Playtest 2026-09-21: "make the selection based on the distance to the slot. The
        // closest slot should be selected." Cycling by index meant the highlighted slot had no
        // relationship to where the player was standing, so building meant pressing Next until
        // the right one happened to light up. Selecting by proximity makes the player's own
        // position the control, which is both faster and controller-friendly.
        //
        // Prev/Next are kept as a manual override for two slots at a similar distance; the
        // automatic pick simply stops applying once the player has cycled by hand this
        // intermission.
        private void UpdateNearestSlotSelection()
        {
            if (_manualSlotSelectionThisIntermission || _runtimeSlots.Count == 0)
            {
                return;
            }

            var player = ResolvePlayerTransform();

            if (player == null)
            {
                return;
            }

            var origin = (Vector2)player.position;
            var nearestIndex = -1;
            var nearestDistanceSquared = float.MaxValue;

            for (var index = 0; index < _runtimeSlots.Count; index++)
            {
                var slot = _runtimeSlots[index];

                if (slot.Transform == null)
                {
                    continue;
                }

                var distanceSquared = ((Vector2)slot.Transform.position - origin).sqrMagnitude;

                if (distanceSquared >= nearestDistanceSquared)
                {
                    continue;
                }

                nearestDistanceSquared = distanceSquared;
                nearestIndex = index;
            }

            if (nearestIndex < 0 || nearestIndex == _selectedSlotIndex)
            {
                return;
            }

            _selectedSlotIndex = nearestIndex;
            UpdateSlotVisuals();
            TowersChanged?.Invoke();
        }

        private Transform ResolvePlayerTransform()
        {
            if (_playerTransform == null)
            {
                var playerCombat = FindFirstObjectByType<Corebound.Gameplay.Combat.PlayerCombatController>();
                _playerTransform = playerCombat != null ? playerCombat.transform : null;
            }

            return _playerTransform;
        }

        private void Update()
        {
            UpdateNearestSlotSelection();
        }

        private void OnPreviousPressed()
        {
            if (!CanHandleBuildInput())
            {
                return;
            }

            _manualSlotSelectionThisIntermission = true;
            MoveSelection(-1);
        }

        private void OnNextPressed()
        {
            if (!CanHandleBuildInput())
            {
                return;
            }

            _manualSlotSelectionThisIntermission = true;
            MoveSelection(1);
        }

        private void OnInteractPressed()
        {
            if (!CanHandleBuildInput())
            {
                return;
            }

            TryBuildSelectedTower();
        }

        private void OnPreviousTowerTypePressed()
        {
            if (!CanHandleBuildInput())
            {
                return;
            }

            CycleTowerType(-1);
        }

        private void OnNextTowerTypePressed()
        {
            if (!CanHandleBuildInput())
            {
                return;
            }

            CycleTowerType(1);
        }

        // Only meaningful while standing at an empty slot - cycling the tower type while a slot
        // already has an occupant would silently do nothing visible, since ResolveBuildTowerData()
        // is only consulted for empty-slot builds, not upgrades.
        private void CycleTowerType(int direction)
        {
            if (direction == 0 || _selectedSlotIndex < 0 || _selectedSlotIndex >= _runtimeSlots.Count || _runtimeSlots[_selectedSlotIndex].HasTower)
            {
                return;
            }

            ResolveUnlockedTowers(_unlockedTowerBuffer);

            if (_unlockedTowerBuffer.Count == 0)
            {
                return;
            }

            _selectedTowerTypeIndex = (_selectedTowerTypeIndex + direction + _unlockedTowerBuffer.Count) % _unlockedTowerBuffer.Count;
            UpdateSlotVisuals();
        }

        private void ResolveUnlockedTowers(List<TowerData> buffer)
        {
            buffer.Clear();

            if (_towerCatalog == null || _towerCatalog.Towers == null)
            {
                return;
            }

            for (var index = 0; index < _towerCatalog.Towers.Length; index++)
            {
                var towerData = _towerCatalog.Towers[index];

                if (towerData != null && IsTowerUnlocked(towerData))
                {
                    buffer.Add(towerData);
                }
            }
        }

        private bool CanHandleBuildInput()
        {
            return _runtimeSlots.Count > 0
                && Time.timeScale > 0f
                && _waveManager != null
                && IsBuildableWaveState(_waveManager.State)
                && (_shopService == null || !_shopService.IsOpen)
                && (_cardRewardController == null || !_cardRewardController.IsOpen);
        }

        private static bool IsBuildableWaveState(WaveState state)
        {
            // Towers may be built or upgraded during active combat as well as intermission,
            // as long as the player is at a valid slot and no blocking UI is open.
            return state == WaveState.Combat || state == WaveState.BossCombat || state == WaveState.Intermission;
        }

        private void MoveSelection(int direction)
        {
            if (_runtimeSlots.Count == 0 || direction == 0)
            {
                return;
            }

            _selectedSlotIndex = (_selectedSlotIndex + direction + _runtimeSlots.Count) % _runtimeSlots.Count;
            _selectedTowerTypeIndex = 0;
            UpdateSlotVisuals();
        }

        private void TryBuildSelectedTower()
        {
            if (_selectedSlotIndex < 0 || _selectedSlotIndex >= _runtimeSlots.Count)
            {
                return;
            }

            var slot = _runtimeSlots[_selectedSlotIndex];

            if (slot.HasTower || !IsPlayerNearSlot(slot.Position))
            {
                TryUpgradeSelectedTower(slot);
                return;
            }

            var towerData = ResolveBuildTowerData();

            if (towerData == null || !TryCommitPurchase(ResolveTowerCost(towerData.BuildCost)))
            {
                return;
            }

            SpawnTowerAtSlot(_selectedSlotIndex, towerData, true, 0);
            IntermissionBuildCommitted?.Invoke();
        }

        private void TryUpgradeSelectedTower(TowerSlotRuntime slot)
        {
            if (slot == null || !slot.HasTower || !IsPlayerNearSlot(slot.Position))
            {
                return;
            }

            var occupant = slot.Occupant;

            if (occupant == null || !occupant.CanUpgrade)
            {
                return;
            }

            var upgradeCost = ResolveTowerCost(occupant.NextUpgradeCost);

            if (!TryCommitPurchase(upgradeCost))
            {
                return;
            }

            if (!occupant.TryUpgrade())
            {
                RefundPurchase(upgradeCost);
                return;
            }

            ApplyUpgradedState(occupant);
        }

        // Shop.md calls out Tower Upgrades as a sellable offer type alongside Cards. Unlike the
        // Build-mode path above, a Shop purchase is a menu action with payment already committed
        // by ShopService (via ShopOfferData.Cost, independent of the tower's own dynamic upgrade
        // cost) - it deliberately does not require the player to be standing at the slot, since
        // the Shop is browsed as a modal separate from physical positioning.
        public bool CanUpgradeSelectedTowerForShop()
        {
            var occupant = ResolveSelectedSlotOccupant();
            return occupant != null && occupant.CanUpgrade;
        }

        public string SelectedTowerUpgradeTitle()
        {
            var occupant = ResolveSelectedSlotOccupant();
            return occupant != null ? occupant.DisplayName + LocalizationService.Resolve(" Upgrade", "-Upgrade") : LocalizationService.Resolve("Tower Upgrade", "Turm-Upgrade");
        }

        public string SelectedTowerUpgradeDescription()
        {
            var occupant = ResolveSelectedSlotOccupant();
            return occupant != null ? LocalizationService.Resolve("Upgrades ", "Verbessert ") + occupant.DisplayName + LocalizationService.Resolve(" to: ", " auf: ") + occupant.UpgradeLabel : string.Empty;
        }

        public bool TryApplyUpgradeFromShop()
        {
            var occupant = ResolveSelectedSlotOccupant();

            if (occupant == null || !occupant.CanUpgrade || !occupant.TryUpgrade())
            {
                return false;
            }

            ApplyUpgradedState(occupant);
            return true;
        }

        private TowerRuntimeController ResolveSelectedSlotOccupant()
        {
            return _selectedSlotIndex >= 0 && _selectedSlotIndex < _runtimeSlots.Count ? _runtimeSlots[_selectedSlotIndex].Occupant : null;
        }

        private void ApplyUpgradedState(TowerRuntimeController occupant)
        {
            if (_selectedSlotIndex >= 0 && _selectedSlotIndex < _builtTowerUpgradeLevels.Count)
            {
                _builtTowerUpgradeLevels[_selectedSlotIndex] = occupant.CurrentUpgradeLevel;
                SaveBuiltTowerState();
            }

            UpdateSlotVisuals();
            TowersChanged?.Invoke();
            IntermissionBuildCommitted?.Invoke();
        }

        private bool IsPlayerNearSlot(Vector3 slotPosition)
        {
            if (_playerTarget == null)
            {
                return true;
            }

            return Vector2.Distance(_playerTarget.position, slotPosition) <= _slotInteractDistance;
        }

        // Mirrors ShopService.ResolveOfferCost's role: every display, affordability-check and
        // purchase-commit site must read the SAME resolved number, or a discount card could show
        // one price and charge another. Resolve once here, then pass that same value on to
        // TryCommitPurchase - never resolve twice, or a discount would silently apply twice.
        private int ResolveTowerCost(int baseCost)
        {
            return _economyService != null ? _economyService.ResolveCost(baseCost) : baseCost;
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

        private TowerData ResolveBuildTowerData()
        {
            ResolveUnlockedTowers(_unlockedTowerBuffer);

            if (_unlockedTowerBuffer.Count == 0)
            {
                return null;
            }

            var index = Mathf.Clamp(_selectedTowerTypeIndex, 0, _unlockedTowerBuffer.Count - 1);
            return _unlockedTowerBuffer[index];
        }

        private void SpawnTowerAtSlot(int slotIndex, TowerData towerData, bool persist, int upgradeLevel)
        {
            if (towerData == null || slotIndex < 0 || slotIndex >= _runtimeSlots.Count)
            {
                return;
            }

            var slot = _runtimeSlots[slotIndex];
            var towerObject = new GameObject(towerData.DisplayName);
            towerObject.transform.position = slot.Position;
            var towerController = towerObject.AddComponent<TowerRuntimeController>();
            towerController.Initialize(towerData, this, _projectilePrototype, _projectileParent, upgradeLevel);
            _runtimeTowers.Add(towerController);
            slot.AssignTower(towerController);

            if (persist)
            {
                _builtTowerIds.Add(towerData.TowerId);
                _builtTowerUpgradeLevels.Add(upgradeLevel);
                SaveBuiltTowerState();
            }

            UpdateSlotVisuals();
            TowersChanged?.Invoke();
        }

        private void UpdateSlotVisuals()
        {
            var isBuildActive = _waveManager != null
                && IsBuildableWaveState(_waveManager.State)
                && (_shopService == null || !_shopService.IsOpen)
                && (_cardRewardController == null || !_cardRewardController.IsOpen);

            for (var index = 0; index < _runtimeSlots.Count; index++)
            {
                var slot = _runtimeSlots[index];
                var isSelected = index == _selectedSlotIndex;
                var canBuild = !slot.HasTower && IsPlayerNearSlot(slot.Position) && CanAffordSelectedTower();
                slot.ApplyVisual(isBuildActive, isSelected, canBuild);
            }
        }

        private bool CanAffordSelectedTower()
        {
            var towerData = ResolveBuildTowerData();
            return towerData != null && _resourceWalletService != null && _resourceWalletService.CurrentAmount >= ResolveTowerCost(towerData.BuildCost);
        }

        private bool ResolveHasAvailableIntermissionAction()
        {
            var towerData = ResolveBuildTowerData();

            if (towerData != null && _resourceWalletService != null && _resourceWalletService.CurrentAmount >= ResolveTowerCost(towerData.BuildCost))
            {
                for (var index = 0; index < _runtimeSlots.Count; index++)
                {
                    if (!_runtimeSlots[index].HasTower)
                    {
                        return true;
                    }
                }
            }

            if (_resourceWalletService == null)
            {
                return false;
            }

            for (var index = 0; index < _runtimeSlots.Count; index++)
            {
                var occupant = _runtimeSlots[index].Occupant;

                if (occupant != null && occupant.CanUpgrade && _resourceWalletService.CurrentAmount >= ResolveTowerCost(occupant.NextUpgradeCost))
                {
                    return true;
                }
            }

            return false;
        }

        private string ResolveSelectedSlotSummary()
        {
            if (_runtimeSlots.Count == 0)
            {
                return LocalizationService.Resolve("No Tower Slots", "Keine Turm-Slots");
            }

            var slot = _runtimeSlots[Mathf.Clamp(_selectedSlotIndex, 0, _runtimeSlots.Count - 1)];

            if (slot.HasTower)
            {
                var occupant = slot.Occupant;
                return occupant != null && occupant.CanUpgrade
                    ? LocalizationService.Resolve("Tower Placement | Slot ", "Turmplatzierung | Slot ") + (_selectedSlotIndex + 1) + ": " + occupant.name + " | " + occupant.UpgradeLabel + LocalizationService.Resolve(" | Cost ", " | Kosten ") + ResolveTowerCost(occupant.NextUpgradeCost) + LocalizationService.Resolve(" | Press ", " | Drücke ") + ResolveBuildInteractKeyLabel() + LocalizationService.Resolve(" to upgrade", " zum Verbessern")
                    : "Slot " + (_selectedSlotIndex + 1) + ": " + (occupant != null ? occupant.name : LocalizationService.Resolve("Tower", "Turm")) + LocalizationService.Resolve(" | Max level", " | Maximalstufe");
            }

            var towerData = ResolveBuildTowerData();

            if (towerData == null)
            {
                return LocalizationService.Resolve("No Buildable Tower", "Kein bebaubarer Turm");
            }

            var rangeState = IsPlayerNearSlot(slot.Position)
                ? LocalizationService.Resolve("Press ", "Drücke ") + ResolveBuildInteractKeyLabel() + LocalizationService.Resolve(" to build", " zum Bauen")
                : LocalizationService.Resolve("Move closer", "Näher herangehen");
            var summary = LocalizationService.Resolve("Tower Placement | Slot ", "Turmplatzierung | Slot ") + (_selectedSlotIndex + 1) + ": " + towerData.DisplayName + LocalizationService.Resolve(" | Cost ", " | Kosten ") + ResolveTowerCost(towerData.BuildCost) + " | " + rangeState;

            // ResolveBuildTowerData() only ever returns the tower at _selectedTowerTypeIndex - without
            // this hint, cycling tower types (bound on the Build map, not the always-visible Interact
            // action) has zero on-screen discoverability, so players with multiple unlocked towers
            // have no way to learn how to pick a different one to build.
            if (_unlockedTowerBuffer.Count > 1)
            {
                summary += " | " + ResolvePreviousTowerTypeKeyLabel() + "/" + ResolveNextTowerTypeKeyLabel() + LocalizationService.Resolve(" to change tower", " zum Turmwechsel");
            }

            return summary;
        }

        private string ResolveBuildInteractKeyLabel()
        {
            return ResolveActionKeyLabel(GameplayMapName, InteractActionName, "E", "Y");
        }

        private string ResolvePreviousTowerTypeKeyLabel()
        {
            return ResolveActionKeyLabel(BuildMapName, PreviousTowerTypeActionName, "Z", "LT");
        }

        private string ResolveNextTowerTypeKeyLabel()
        {
            return ResolveActionKeyLabel(BuildMapName, NextTowerTypeActionName, "X", "RT");
        }

        // Project Vision commits to "Controller First" - a hint hardcoded to the Keyboard&Mouse
        // binding group (as every one of these read before this fix) shows a controller player
        // "Press E" for an action they trigger with a gamepad button, which is unreadable to them.
        // Reading InputReader.IsUsingGamepad and switching binding groups keeps the on-screen hint
        // matching whichever device the player actually last used.
        private string ResolveActionKeyLabel(string mapName, string actionName, string keyboardFallback, string gamepadFallback)
        {
            if (_inputReaderSource is CoreboundInputReader concreteReader)
            {
                var bindingGroup = concreteReader.IsUsingGamepad ? "Gamepad" : "Keyboard&Mouse";
                var display = concreteReader.GetBindingDisplayString(mapName, actionName, bindingGroup);

                if (!string.IsNullOrWhiteSpace(display))
                {
                    return display.ToUpperInvariant();
                }

                return concreteReader.IsUsingGamepad ? gamepadFallback : keyboardFallback;
            }

            return keyboardFallback;
        }

        private Sprite CreateSlotMarkerSprite()
        {
            if (_slotMarkerTexture == null)
            {
                return null;
            }

            var sprite = Sprite.Create(_slotMarkerTexture, new Rect(0f, 0f, _slotMarkerTexture.width, _slotMarkerTexture.height), new Vector2(0.5f, 0.5f), 100f);
            sprite.hideFlags = HideFlags.DontSaveInEditor;
            return sprite;
        }

        private sealed class TowerSlotRuntime
        {
            private readonly SpriteRenderer _spriteRenderer;
            private readonly float _baseScale;

            public TowerSlotRuntime(Transform transform, SpriteRenderer spriteRenderer, float baseScale)
            {
                Transform = transform;
                _spriteRenderer = spriteRenderer;
                _baseScale = baseScale;
            }

            public Transform Transform { get; }

            public Vector3 Position => Transform.position;

            public TowerRuntimeController Occupant { get; private set; }

            public bool HasTower => Occupant != null;

            public void AssignTower(TowerRuntimeController towerRuntimeController)
            {
                Occupant = towerRuntimeController;
            }

            public void ApplyVisual(bool isBuildActive, bool isSelected, bool canBuild)
            {
                if (_spriteRenderer == null)
                {
                    return;
                }

                _spriteRenderer.enabled = isBuildActive;

                if (!isBuildActive)
                {
                    return;
                }

                if (HasTower)
                {
                    _spriteRenderer.color = isSelected ? new Color(0.95f, 0.82f, 0.42f, 0.82f) : new Color(0.34f, 0.42f, 0.52f, 0.34f);
                    Transform.localScale = new Vector3(0.85f * _baseScale, 0.85f * _baseScale, 1f);
                    return;
                }

                if (isSelected)
                {
                    _spriteRenderer.color = canBuild
                        ? new Color(0.95f, 0.98f, 1f, 0.92f)
                        : new Color(0.95f, 0.68f, 0.52f, 0.85f);
                    Transform.localScale = new Vector3(1.08f * _baseScale, 1.08f * _baseScale, 1f);
                    return;
                }

                _spriteRenderer.color = new Color(0.78f, 0.9f, 1f, 0.66f);
                Transform.localScale = new Vector3(_baseScale, _baseScale, 1f);
            }
        }
    }
}