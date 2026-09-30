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
    public sealed class TowerService : MonoBehaviour, IInteractHandler
    {
        private const string GameplayMapName = "Gameplay";
        private const string InteractActionName = "Interact";
        private const string BuildMapName = "Build";
        private const string PreviousTowerTypeActionName = "PreviousTowerType";
        private const string NextTowerTypeActionName = "NextTowerType";
        private const string FreeTowerRuleKey = "skill_forward_foundations";

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

        public IReadOnlyList<string> BuiltTowerIds => _builtTowerIds;

        public IReadOnlyList<int> BuiltTowerUpgradeLevels => _builtTowerUpgradeLevels;

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

        // Playtest 2026-09-21: "i dont like the current selection how to chose to build a tower.
        // I miss e.g. information what the towers are able of and what are there pro/cons."
        //
        // The old line named the tower and its cost and nothing else, so choosing between four
        // towers meant either remembering their stats or building one to find out. This builds
        // the whole comparison the decision needs: what it does per second, how far it reaches,
        // what it is good against and what it is bad against, plus where the player sits in the
        // roster so they know there are others to cycle to.
        //
        // Deliberately assembled here rather than in the presenter: the presenter should render
        // text, not decide what a tower is good at.
        public string DescribeBuildCandidate(TowerData towerData, int rosterIndex, int rosterCount)
        {
            if (towerData == null)
            {
                return string.Empty;
            }

            var builder = new System.Text.StringBuilder(160);

            builder.Append(towerData.DisplayName);

            if (rosterCount > 1)
            {
                builder.Append("  [").Append(rosterIndex + 1).Append('/').Append(rosterCount).Append(']');
            }

            builder.Append("  ").Append(Corebound.UI.LocalizationService.Resolve("Cost ", "Kosten "));
            builder.Append(ResolveTowerCost(ResolveDiscountedBuildCost(towerData)));

            builder.Append("  ·  ").Append(Corebound.UI.LocalizationService.Resolve("DPS ", "SPS "));
            builder.Append(towerData.DamagePerSecond.ToString("0.#"));
            builder.Append("  ").Append(Corebound.UI.LocalizationService.Resolve("Range ", "Reichweite "));
            builder.Append(towerData.Range.ToString("0.#"));

            // The selected slot's own character, in words. The pad tint alone would be colour-only,
            // which SkillTree.md already forbids for node states and which is no more readable here.
            var selectedSlot = _selectedSlotIndex >= 0 && _selectedSlotIndex < _runtimeSlots.Count
                ? _runtimeSlots[_selectedSlotIndex]
                : null;

            if (selectedSlot != null && selectedSlot.Trait != TowerSlotTrait.Standard)
            {
                builder.Append("  ·  ").Append(TowerSlotTraitRules.Describe(selectedSlot.Trait));
            }

            if (!string.IsNullOrWhiteSpace(towerData.Strengths))
            {
                builder.Append(nlSeparator).Append(Corebound.UI.LocalizationService.Resolve("+ ", "+ "));
                builder.Append(towerData.Strengths);
            }

            if (!string.IsNullOrWhiteSpace(towerData.Weaknesses))
            {
                builder.Append(nlSeparator).Append(Corebound.UI.LocalizationService.Resolve("- ", "- "));
                builder.Append(towerData.Weaknesses);
            }

            return builder.ToString();
        }

        private const string nlSeparator = "\n";

        public string BuildInteractKeyLabel => ResolveBuildInteractKeyLabel();

        // ---- Build browser (UI proposal 2, 2026-09-22) -----------------------------------------
        // "I miss e.g. information what the towers are able of" was answered by the comparison
        // line; the browser answers the other half - cycling one tower at a time is still "remember
        // four towers". TowerBuildBrowserPresenter draws the whole roster from these, so the
        // presenter never decides anything about towers itself.

        public bool IsBuildBrowserActive => ResolveIsBuildBrowserActive();

        public int SlotCount => _runtimeSlots.Count;

        public Vector3 GetSlotPosition(int slotIndex) => slotIndex >= 0 && slotIndex < _runtimeSlots.Count ? _runtimeSlots[slotIndex].Position : Vector3.zero;

        public bool IsSlotOccupied(int slotIndex) => slotIndex >= 0 && slotIndex < _runtimeSlots.Count && _runtimeSlots[slotIndex].HasTower;

        public TowerData[] CatalogTowers => _towerCatalog != null ? _towerCatalog.Towers : null;

        public TowerData SelectedBuildTower => ResolveBuildTowerData();

        public string PreviousTowerTypeKeyLabel => ResolvePreviousTowerTypeKeyLabel();

        public string NextTowerTypeKeyLabel => ResolveNextTowerTypeKeyLabel();

        public string LoadoutKeyLabel => ResolveActionKeyLabel("System", "Loadout", "I", "SELECT");

        public int ResolveBuildCost(TowerData towerData)
        {
            if (towerData == null)
            {
                return 0;
            }

            return ApplyFirstTowerDiscount(ResolveTowerCost(ResolveDiscountedBuildCost(towerData)));
        }

        // Forward Foundations (owner, 2026-09-22: a free tower "sounds too broken"): the first
        // tower each stage costs (1 - value) of its price - 50% as authored. The value is the
        // discount fraction; the enum keeps its old name because it is serialized by index.
        private int ApplyFirstTowerDiscount(int cost)
        {
            var discount = Mathf.Clamp01(RunProgressionSnapshot.Current.Skills[SkillEffectTarget.FreeFirstTowerPerStage]);

            if (discount <= 0f || !RunProgressionSnapshot.Current.IsRuleAvailableThisStage(FreeTowerRuleKey))
            {
                return cost;
            }

            return Mathf.Max(1, Mathf.CeilToInt(cost * (1f - discount)));
        }

        public bool CanAffordBuild(TowerData towerData)
        {
            return towerData != null && _resourceWalletService != null && _resourceWalletService.CurrentAmount >= ResolveBuildCost(towerData);
        }

        // Pause menu Towers tab: each built tower (grouped by type and level) as its base values
        // (+ what upgrades, cards, skills and permanent upgrades add), and the global tower
        // modifiers even when nothing is built yet.
        public void CollectStatLines(List<StatLine> lines)
        {
            lines.Clear();
            var progression = RunProgressionSnapshot.Current;
            var damageMultiplier = TowerDamageMultiplier * progression.Skills[SkillEffectTarget.TowerDamageMultiplier];
            lines.Add(StatLine.Header(LocalizationService.Resolve("All towers", "Alle Türme")));
            lines.Add(StatLine.Value(LocalizationService.Resolve("Damage", "Schaden"), 100f, damageMultiplier * 100f, "0", "%"));
            lines.Add(StatLine.Value(LocalizationService.Resolve("Range", "Reichweite"), 100f, progression.Skills[SkillEffectTarget.TowerRangeMultiplier] * 100f, "0", "%"));
            lines.Add(StatLine.Value(LocalizationService.Resolve("Fire interval", "Feuerintervall"), 100f, progression.Skills[SkillEffectTarget.TowerFireIntervalMultiplier] * 100f, "0", "%", true));

            var seen = new List<string>();
            var any = false;

            for (var index = 0; index < _runtimeSlots.Count; index++)
            {
                var tower = _runtimeSlots[index].Occupant;

                if (tower == null || tower.Data == null)
                {
                    continue;
                }

                var key = tower.Data.TowerId + "#" + tower.CurrentUpgradeLevel;

                if (seen.Contains(key))
                {
                    continue;
                }

                seen.Add(key);
                any = true;
                var data = tower.Data;
                var level = tower.CurrentUpgradeLevel;
                var copies = 0;

                for (var other = 0; other < _runtimeSlots.Count; other++)
                {
                    var occupant = _runtimeSlots[other].Occupant;
                    copies += occupant != null && occupant.Data == data && occupant.CurrentUpgradeLevel == level ? 1 : 0;
                }

                var baseDamage = data.DamageProfile != null ? data.DamageProfile.BaseDamage : 0f;
                lines.Add(StatLine.Header(data.DisplayName + "  " + LocalizationService.Resolve("Lv ", "St ") + (level + 1) + "  x" + copies));
                lines.Add(StatLine.Value(LocalizationService.Resolve("Damage per shot", "Schaden pro Schuss"), baseDamage, tower.DamageAtLevel(level), "0.##"));
                lines.Add(StatLine.Value(LocalizationService.Resolve("Fire interval", "Feuerintervall"), data.FireIntervalSeconds, tower.FireIntervalAtLevel(level), "0.00", "s", true));
                lines.Add(StatLine.Value(LocalizationService.Resolve("Range", "Reichweite"), data.Range, tower.RangeAtLevel(level), "0.#"));
                lines.Add(StatLine.Value(LocalizationService.Resolve("DPS", "SPS"), data.DamagePerSecond, tower.DpsAtLevel(level), "0.#"));
            }

            if (!any)
            {
                lines.Add(StatLine.Plain(LocalizationService.Resolve("No towers built yet.", "Noch keine Türme gebaut."), string.Empty));
            }
        }

        // The tower the player is standing at during an Intermission, for the upgrade overlay
        // (playtest 2026-09-22: "add for built towers an overlay with the upgrade information").
        public TowerRuntimeController UpgradeCandidate
        {
            get
            {
                if (_waveManager == null || _waveManager.State != WaveState.Intermission || !CanHandleBuildInput()
                    || _selectedSlotIndex < 0 || _selectedSlotIndex >= _runtimeSlots.Count)
                {
                    return null;
                }

                var slot = _runtimeSlots[_selectedSlotIndex];
                return slot.HasTower && IsPlayerNearSlot(slot.Position) ? slot.Occupant : null;
            }
        }

        public int ResolveUpgradeCost(TowerRuntimeController tower)
        {
            return tower != null && tower.CanUpgrade && _economyService != null ? _economyService.PeekCost(tower.NextUpgradeCost) : (tower != null ? tower.NextUpgradeCost : 0);
        }

        public bool CanAffordUpgrade(TowerRuntimeController tower)
        {
            return tower != null && tower.CanUpgrade && _resourceWalletService != null && _resourceWalletService.CurrentAmount >= ResolveUpgradeCost(tower);
        }

        private bool ResolveIsBuildBrowserActive()
        {
            if (_waveManager == null || _waveManager.State != WaveState.Intermission || !CanHandleBuildInput())
            {
                return false;
            }

            if (_selectedSlotIndex < 0 || _selectedSlotIndex >= _runtimeSlots.Count)
            {
                return false;
            }

            var slot = _runtimeSlots[_selectedSlotIndex];
            return !slot.HasTower && IsPlayerNearSlot(slot.Position);
        }

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

            if (GetComponent<TowerBuildBrowserPresenter>() == null)
            {
                gameObject.AddComponent<TowerBuildBrowserPresenter>().Bind(this);
            }
        }

        private void OnEnable()
        {
            if (InputReader != null)
            {
                InputReader.PreviousPressed += OnPreviousPressed;
                InputReader.NextPressed += OnNextPressed;
                InputReader.InteractPressed += OnInteractPressed;
                InteractRouter.Register(this);
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
                InteractRouter.Unregister(this);
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

        // One seed per run, so re-entering a stage in the same run reproduces its traits.
        //
        // Assigned lazily rather than in a field initializer: Unity forbids Random.Range in a
        // MonoBehaviour constructor and throws when it is called there, which is exactly what the
        // first version of this did.
        private int _slotTraitSeed;

        private void CreateSlotRuntimes()
        {
            _runtimeSlots.Clear();

            var slotPositions = ResolveActiveSlotPositions();

            if (slotPositions == null)
            {
                return;
            }

            // Seeded per stage so the traits are stable for the whole stage - a slot must not
            // change what it offers while the player is deciding - but different next run and on
            // the next stage, which is the point of rolling them at all.
            if (_slotTraitSeed == 0)
            {
                _slotTraitSeed = UnityEngine.Random.Range(1, 100000);
            }

            var stageIndex = _waveManager != null ? _waveManager.StageIndex : 1;
            var traitRandom = new System.Random(_slotTraitSeed * 31 + stageIndex);

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

                _runtimeSlots.Add(new TowerSlotRuntime(
                    slotObject.transform, spriteRenderer, baseScale, TowerSlotTraitRules.Roll(traitRandom)));
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
            InteractRouter.Dispatch();
        }

        // IInteractHandler (spec review A1): a tower slot outranks Core repair and the Core guard,
        // so standing on a slot next to the Core builds and does nothing else.
        public InteractPriority InteractPriority => InteractPriority.TowerSlot;

        public bool CanHandleInteract => CanHandleBuildInput()
            && _waveManager != null
            && _waveManager.State == WaveState.Intermission
            && _selectedSlotIndex >= 0
            && _selectedSlotIndex < _runtimeSlots.Count
            && IsPlayerNearSlot(_runtimeSlots[_selectedSlotIndex].Position);

        public void HandleInteract()
        {
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

        // Mouse on the build browser (playtest 2026-09-29): pick a tower type by clicking its card.
        // Same rule as cycling - only at an empty slot, and only among unlocked towers.
        public bool TrySelectBuildTower(TowerData tower)
        {
            if (tower == null || !CanHandleBuildInput() || _selectedSlotIndex < 0 || _selectedSlotIndex >= _runtimeSlots.Count
                || _runtimeSlots[_selectedSlotIndex].HasTower)
            {
                return false;
            }

            ResolveUnlockedTowers(_unlockedTowerBuffer);
            var index = _unlockedTowerBuffer.IndexOf(tower);

            if (index < 0 || index == _selectedTowerTypeIndex)
            {
                return false;
            }

            _selectedTowerTypeIndex = index;
            UpdateSlotVisuals();
            return true;
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
                && (_cardRewardController == null || !_cardRewardController.IsOpen)
                && !Corebound.UI.Gameplay.WeaponLoadoutPanelPresenter.IsOpen;
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

            if (towerData == null)
            {
                return;
            }

            // Forward Foundations: consumed only once the discounted build actually happens.
            var fullCost = ResolveTowerCost(ResolveDiscountedBuildCost(towerData));
            var cost = ApplyFirstTowerDiscount(fullCost);

            // A Cramped slot is cheaper to build on. Applied after the other discounts so it reads
            // as a property of the place rather than of the tower.
            var selectedTrait = _selectedSlotIndex >= 0 && _selectedSlotIndex < _runtimeSlots.Count
                ? _runtimeSlots[_selectedSlotIndex].Trait
                : TowerSlotTrait.Standard;
            cost = Mathf.Max(1, Mathf.RoundToInt(cost * TowerSlotTraitRules.CostMultiplier(selectedTrait)));

            if (!TryCommitPurchase(cost))
            {
                return;
            }

            if (cost < fullCost)
            {
                RunProgressionSnapshot.Current.TryConsumeOncePerStage(FreeTowerRuleKey);
                NotificationPresenter.Show(LocalizationService.Resolve("Forward Foundations: tower half price", "Vorgeschobene Fundamente: Turm zum halben Preis"));
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
        // Upgrades.md Build Discount is per tower and only reaches build costs, so it is applied by
        // ResolveBuildCost below; upgrades and shop prices go through here unchanged.
        private int ResolveTowerCost(int baseCost)
        {
            return _economyService != null ? _economyService.ResolveCost(baseCost) : baseCost;
        }

        private int ResolveDiscountedBuildCost(TowerData towerData)
        {
            var discount = RunProgressionSnapshot.Current.Upgrades.GetReductionMultiplier(towerData.TowerId, UpgradableStat.BuildDiscount);
            return Mathf.Max(1, Mathf.RoundToInt(towerData.BuildCost * discount));
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

        // Engineer kit (CHARACTER_CLASSES_2026-09-30): the run starts with one tower built, free, in
        // the empty slot nearest the Core. Persisted like any built tower, so a resumed run keeps it.
        public bool TryPlaceKitTower(string towerId, Vector3 corePosition)
        {
            var towerData = _towerCatalog != null ? _towerCatalog.FindById(towerId) : null;

            if (towerData == null)
            {
                return false;
            }

            var best = -1;
            var bestDistance = float.MaxValue;

            for (var index = 0; index < _runtimeSlots.Count; index++)
            {
                if (_runtimeSlots[index].HasTower)
                {
                    continue;
                }

                var distance = (_runtimeSlots[index].Position - corePosition).sqrMagnitude;

                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = index;
                }
            }

            if (best < 0)
            {
                return false;
            }

            SpawnTowerAtSlot(best, towerData, true, 0);
            return true;
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
            towerController.ApplySlotTrait(slot.Trait);
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
            return towerData != null && _resourceWalletService != null && _resourceWalletService.CurrentAmount >= ResolveTowerCost(ResolveDiscountedBuildCost(towerData));
        }

        private bool ResolveHasAvailableIntermissionAction()
        {
            var towerData = ResolveBuildTowerData();

            if (towerData != null && _resourceWalletService != null && _resourceWalletService.CurrentAmount >= ResolveTowerCost(ResolveDiscountedBuildCost(towerData)))
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
            // The full comparison, not just a name and a price: DPS, range, and what this tower
            // is good and bad against, so the choice can be made without building one to find out.
            var summary = LocalizationService.Resolve("Slot ", "Slot ") + (_selectedSlotIndex + 1) + "  ·  "
                + DescribeBuildCandidate(towerData, _selectedTowerTypeIndex, _unlockedTowerBuffer.Count)
                + nlSeparator + rangeState;

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

            public TowerSlotRuntime(Transform transform, SpriteRenderer spriteRenderer, float baseScale, TowerSlotTrait trait)
            {
                Transform = transform;
                _spriteRenderer = spriteRenderer;
                _baseScale = baseScale;
                Trait = trait;
            }

            public TowerSlotTrait Trait { get; }

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

                // The trait tint only shows on an idle, empty pad: a selected or occupied slot has
                // its own state to communicate, and that has to win. Never colour alone - the slot
                // summary names the trait in words as well.
                var idle = new Color(0.78f, 0.9f, 1f, 0.66f);
                var traitTint = TowerSlotTraitRules.Tint(Trait);
                _spriteRenderer.color = new Color(
                    idle.r * traitTint.r, idle.g * traitTint.g, idle.b * traitTint.b, idle.a);
                Transform.localScale = new Vector3(_baseScale, _baseScale, 1f);
            }
        }
    }
}