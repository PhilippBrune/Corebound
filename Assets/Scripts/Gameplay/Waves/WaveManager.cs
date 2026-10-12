using System;
using System.Collections.Generic;
using Corebound.Gameplay.Bosses;
using Corebound.Gameplay.Cards;
using Corebound.Gameplay.Combat;
using Corebound.Gameplay.Enemies;
using Corebound.Gameplay.Resources;
using Corebound.Gameplay.Shared;
using Corebound.Gameplay.Shop;
using Corebound.Gameplay.Towers;
using Corebound.Input;
using Corebound.MetaProgression;
using Corebound.SaveSystem;
using Corebound.UI;
using UnityEngine;

namespace Corebound.Gameplay.Waves
{
    // Known debt: this class has grown past its core wave/boss-state-machine job to also own
    // statistics/meta-currency persistence, core-target construction, player positioning, and
    // stage-transition orchestration (~12 responsibilities in one MonoBehaviour). 2026-09-08: the
    // statistics/meta-currency slice was extracted into WaveStatisticsRecorder (a plain, zero
    // Unity-dependency class) since it was fully self-contained and safe to move without a
    // compiler. 2026-09-09: the HUD/event label-resolution slice (current/next encounter label,
    // elite flag, stage start/completion/transition label) was likewise extracted into
    // WaveEncounterLabelResolver - it was a read-only, side-effect-free cluster, so every extracted
    // method's field reads became explicit parameters at each call site with no behavior change.
    // Core-target construction and player-positioning are still tangled with this class's own
    // instance fields and this is the single most load-bearing class in the game - nearly every
    // gameplay system holds a reference into it - so extracting those remaining, side-effecting
    // groups needs Editor access to compile and playtest against before every call site can be
    // re-verified by hand.
    public sealed class WaveManager : MonoBehaviour
    {
        [SerializeField] private StageData _stageData;
        [SerializeField] private ResourceWalletService _resourceWalletService;
        [SerializeField] private CardRewardController _cardRewardController;
        [SerializeField] private RunPersistenceController _runPersistenceController;
        [SerializeField] private UnityEngine.Object _saveManagerSource;
        [SerializeField] private UnityEngine.Object _inputReaderSource;
        [SerializeField] private Transform _playerTarget;
        [SerializeField] private Transform _coreTarget;
        [SerializeField] private CombatTargetData _coreTargetData;
        [SerializeField] private HitReactionData _coreHitReactionData;
        [SerializeField] private CoreData _coreData;
        [SerializeField] private Transform _enemyParent;
        [SerializeField] private CombatProjectile _sharedProjectilePrototype;
        [SerializeField] private Transform _projectileParent;
        [SerializeField] private MetaProgressionDefinitionData _metaProgressionDefinitionData;

        // Leave empty to turn arena objectives off entirely - the service treats a null as "this
        // wave has none", so no scene needs changing to disable the feature.
        [SerializeField] private Arena.ArenaObjectiveData _arenaObjectiveData;

        // Playtest feedback (2026-09-19): "Enemies spawns are still to static - it should be more
        // random" / "Enemies per Wave should be more random but ballenced". Both knobs are
        // deliberately bounded so a wave still reads as the encounter its data authored: enemies
        // keep entering from the stage's authored spawn points, and a wave's total enemy count
        // still averages out to the authored total rather than drifting.
        [SerializeField] [Range(0f, 0.5f)] private float _spawnCountVariance = 0.25f;

        // Fraction of one fan-angle step a spawn may be nudged by. At 0 the old exactly-repeating
        // geometry comes back, which is useful for A/B checks.
        [SerializeField] [Range(0f, 1f)] private float _spawnAngleJitter = 0.5f;

        private readonly List<SpawnGroupRuntime> _spawnGroups = new List<SpawnGroupRuntime>();

        // Re-rolled per wave. The spawn counter is what actually fixes the "all enemies of a group
        // appear on the exact same tile" problem: spawn placement used to be handed the
        // spawn *group* index, so a group of six enemies produced six identical positions and a
        // whole wave only ever used as many distinct points as it had groups.
        // Where things are placed now lives in WaveSpawnPlacement. Extracted 2026-09-20: it is
        // pure geometry with no wave state, and it is where the worst bug of the week hid.
        private readonly WaveSpawnPlacement _spawnPlacement = new WaveSpawnPlacement();
        private readonly List<int> _upcomingGateIndices = new List<int>();

        // Timed arena objectives (owner 2026-09-25, Docs/OPEN_TOPICS.md).
        // The service owns every decision about them; WaveManager only tells it when a wave starts,
        // how far the wave has got, and when it ends.
        private readonly Arena.ArenaObjectiveService _arenaObjectiveService = new Arena.ArenaObjectiveService();
        private int _spawnedEnemiesThisWave;

        // Counted here rather than read back from statistics.json, which holds a lifetime total
        // across every run on the profile - a history entry wants what *this* run killed.
        private int _runEnemiesKilled;
        private RunTimerService _runTimer;


        private EnemyPool _enemyPool;
        private ResourcePickupPool _resourcePickupPool;
        private BossRuntimeController _bossRuntimeController;
        private ShopService _shopService;
        private TowerService _towerService;
        private CardService _cardService;
        // Playtest feedback (2026-09-14): enemies started spawning the instant a wave began, with
        // no beat for the player to register the WaveStarted banner/HUD update before combat was
        // already underway. A short grace window before the first spawn group is allowed to tick
        // gives that beat without touching spawn cadence itself.
        private const float WaveStartSpawnDelaySeconds = 5f;

        private WaveState _state;
        private int _currentWaveIndex;
        private int _remainingAliveEnemies;
        private StageArenaArtPresenter _arenaArtPresenter;

        private StageArenaArtPresenter ArenaArtPresenter =>
            _arenaArtPresenter != null ? _arenaArtPresenter : (_arenaArtPresenter = FindFirstObjectByType<StageArenaArtPresenter>());
        private float _intermissionTimer;
        private float _waveStartSpawnDelayTimer;
        private bool _completeStageAfterReward;
        private bool _hasRaisedStageStarted;
        private bool _requiresIntermissionShopAction;
        private bool _requiresIntermissionTowerAction;

        // Several systems subscribe to the same IInputReader.ConfirmPressed event, so one physical
        // Enter/A press is delivered to all of them in subscription order. When that press is what
        // completed a mandatory Intermission gate (buying in the Shop, or committing a tower
        // build), the gate flag flips to false *during* that same event - and if this component's
        // own handler happens to run afterwards, it sees an already-satisfied gate and ends the
        // Intermission on the very press the player meant as "buy this"/"build this" (playtest
        // 2026-09-19: "Intermission phase should not end when pressing the btn for placing a
        // tower"). Recording the frame a gate was satisfied lets OnConfirmPressed ignore the
        // remainder of that frame regardless of subscription order, which is not otherwise
        // guaranteed. Same pattern as WaveStatePresenter's _pauseOverlayClosedFrame.
        private int _intermissionGateResolvedFrame = -1;
        private int _intermissionStartedFrame = -1;
        private bool _wasCoreDestroyedOnFailure;

        public event Action StateChanged;

        public WaveState State => _state;

        public int CurrentWaveNumber => _currentWaveIndex + 1;

        public int TotalWaveCount => _stageData != null && _stageData.Waves != null ? _stageData.Waves.Length : 0;

        public int RemainingAliveEnemies => _remainingAliveEnemies;

        public float IntermissionSecondsRemaining => _intermissionTimer;

        public bool RequiresIntermissionShopAction => _requiresIntermissionShopAction;

        public bool RequiresIntermissionTowerAction => _requiresIntermissionTowerAction;

        public string FailureCauseLabel => _wasCoreDestroyedOnFailure
            ? LocalizationService.Resolve("Core Destroyed", "Kern zerstört")
            : LocalizationService.Resolve("Player Died", "Spieler gestorben");

        public StageData StageDataAsset => _stageData;

        public int StageIndex => _stageData != null ? _stageData.StageIndex : 1;

        // Exposed for the HUD's level readout, which needs live save data rather than the
        // run-start snapshot - the level is the one progression value that moves mid-run.
        public MetaProgressionDefinitionData MetaProgressionDefinition => _metaProgressionDefinitionData;

        public ISaveManager SaveManagerSource => SaveManager;

        public bool IsTutorialRun => _isTutorialRun;

        // So sibling services (PlayerRepairService) can share the reader this component was
        // wired with, rather than each one hunting for the right CoreboundInputReader - the
        // MainMenu and gameplay scenes each carry one.
        public UnityEngine.Object InputReaderSource => _inputReaderSource;

        public string StageId => _stageData != null ? _stageData.StageId : string.Empty;

        public string StageLabel => _stageData != null ? _stageData.DisplayName : string.Empty;

        public string BossLabel => _bossRuntimeController != null ? _bossRuntimeController.BossName : string.Empty;

        public string BossPhaseLabel => _bossRuntimeController != null ? _bossRuntimeController.CurrentPhaseLabel : string.Empty;

        public float BossHealthNormalized => _bossRuntimeController != null ? _bossRuntimeController.CurrentHealthNormalized : 1f;

        private ISaveManager SaveManager => SaveManagerResolver.Resolve(_saveManagerSource);

        private StageTransitionCoordinator _stageTransition;

        // Lazy: OnEnable (InitializeFromSave) can need it before Awake-order guarantees anything.
        private StageTransitionCoordinator StageTransition => _stageTransition ??= new StageTransitionCoordinator(
            () => SaveManager,
            () => _resourceWalletService,
            () => _towerService,
            () => _runPersistenceController);

        private IInputReader InputReader => _inputReaderSource as IInputReader;

        private void Awake()
        {
            // Static registry, so a Transform from the previous scene would otherwise survive
            // a scene load and buff the next run from a destroyed object.
            EnemySupportAuraRegistry.Clear();
            BossEncounterState.SetActive(false);
            MetaRewardLedger.ResetRunTotals();

            // Skill tree and upgrades, frozen for this run (idempotent per scene load).
            RunProgressionSnapshot.Capture(SaveManager, _metaProgressionDefinitionData);

            _shopService = GetComponent<ShopService>();

            if (_shopService == null)
            {
                _shopService = gameObject.AddComponent<ShopService>();
            }

            _towerService = GetComponent<TowerService>();
            _cardService = GetComponent<CardService>();
            EnsureCoreTarget();
            EnsureEnemyParent();
            EnsurePlayerStagePosition();

            if (_enemyPool == null)
            {
                _enemyPool = new EnemyPool(_enemyParent);
            }

            if (_resourcePickupPool == null)
            {
                _resourcePickupPool = new ResourcePickupPool(_enemyParent);
            }

            var runSaveData = SaveManager != null ? SaveManager.LoadRunSaveData() : null;
            RestoreCoreHealthFromSave(runSaveData);

            if (GetComponent<GameplayQuestTracker>() == null)
            {
                gameObject.AddComponent<GameplayQuestTracker>().Bind(this);
            }

            if (GetComponent<Corebound.Story.StoryDirector>() == null)
            {
                gameObject.AddComponent<Corebound.Story.StoryDirector>().Bind(this);
            }

            // Protecting the Core (CORE_PROTECTION_FEEL_2026-10-07): the bonuses and the hit sound,
            // and what the player sees of the threat.
            _coreDefense = gameObject.GetOrAdd<CoreDefenseDirector>();
            _coreDefense.Bind(_resourceWalletService);
            gameObject.GetOrAdd<Corebound.UI.Gameplay.CoreThreatPresenter>().Bind(this);

            // Captured now because the run save is wiped the moment the run is lost, before the
            // history is written (see RunHistoryRecorder.RunBuild).
            _isTutorialRun = runSaveData != null && runSaveData.IsTutorialRun;
        }

        private bool _isTutorialRun;
        private CoreDefenseDirector _coreDefense;

        // This wave's "Core untouched" result, for the wave banner.
        public CoreDefenseDirector CoreDefense => _coreDefense;

        private void OnEnable()
        {
            CombatEvents.CombatantDied += OnCombatantDied;

            if (InputReader != null)
            {
                InputReader.ConfirmPressed += OnConfirmPressed;
            }

            if (_cardRewardController != null)
            {
                _cardRewardController.StateChanged += OnRewardStateChanged;
            }

            if (_towerService != null)
            {
                _towerService.IntermissionBuildCommitted += OnIntermissionBuildCommitted;
            }

            if (_shopService != null)
            {
                _shopService.ShopResolved += OnShopResolved;
            }

            if (_bossRuntimeController != null)
            {
                _bossRuntimeController.PhaseChanged -= OnBossPhaseChanged;
                _bossRuntimeController.PhaseChanged += OnBossPhaseChanged;
                _bossRuntimeController.HeavyAttackStarted -= OnBossHeavyAttackStarted;
                _bossRuntimeController.HeavyAttackStarted += OnBossHeavyAttackStarted;
            }

            // A scene change or domain reload can destroy an overlay without it releasing the
            // freeze, which would strand the new run at timeScale 0 with no way back.
            GameplayTimeScaleGuard.ForceRelease();
            EnsureRunTimer();
            MetaRewardLedger.LevelGained -= OnLevelGained;
            MetaRewardLedger.LevelGained += OnLevelGained;
            MetaRewardLedger.ParagonLevelGained -= OnParagonLevelGained;
            MetaRewardLedger.ParagonLevelGained += OnParagonLevelGained;
            CardService.CardCollectedFirstTime -= OnCardCollectedFirstTime;
            CardService.CardCollectedFirstTime += OnCardCollectedFirstTime;
            InitializeFromSave();
        }

        // Lives on this GameObject so it shares WaveManager's lifetime: the run timer must survive
        // the additive canvas scene loads and die with the run, which is exactly this object.
        private void EnsureRunTimer()
        {
            if (_runTimer != null)
            {
                return;
            }

            _runTimer = gameObject.GetOrAdd<RunTimerService>();
            var runSaveData = SaveManager != null ? SaveManager.LoadRunSaveData() : null;

            // A resumed run continues its clocks instead of restarting them, so a run played across
            // three sessions still reports its whole length rather than only the last sitting.
            if (runSaveData != null && runSaveData.HasActiveRun && runSaveData.ElapsedRealSeconds > 0f)
            {
                _runTimer.Restore(
                    runSaveData.ElapsedActiveSeconds,
                    runSaveData.ElapsedRealSeconds,
                    runSaveData.RunStartedUtcTicks > 0
                        ? new System.DateTime(runSaveData.RunStartedUtcTicks, System.DateTimeKind.Utc)
                        : System.DateTime.UtcNow);
                return;
            }

            _runTimer.BeginRun();
        }

        // Run- and stage-start bonuses are paid here, not in InitializeFromSave (OnEnable): every
        // Awake and OnEnable has run by Start, so ResourceWalletService has already loaded the
        // wallet from the save and cannot overwrite the bonus with the older amount afterwards.
        private void Start()
        {
            ApplyRunStartBonuses(SaveManager != null ? SaveManager.LoadRunSaveData() : null);
            StageTransition.PayStageStartBonus(StageIndex);
        }

        private void OnDisable()
        {
            MetaRewardLedger.LevelGained -= OnLevelGained;
            MetaRewardLedger.ParagonLevelGained -= OnParagonLevelGained;
            CardService.CardCollectedFirstTime -= OnCardCollectedFirstTime;
            CombatEvents.CombatantDied -= OnCombatantDied;

            if (InputReader != null)
            {
                InputReader.ConfirmPressed -= OnConfirmPressed;
            }

            if (_cardRewardController != null)
            {
                _cardRewardController.StateChanged -= OnRewardStateChanged;
            }

            if (_towerService != null)
            {
                _towerService.IntermissionBuildCommitted -= OnIntermissionBuildCommitted;
            }

            if (_shopService != null)
            {
                _shopService.ShopResolved -= OnShopResolved;
            }

            if (_bossRuntimeController != null)
            {
                _bossRuntimeController.PhaseChanged -= OnBossPhaseChanged;
            }
        }

        private void Update()
        {
            switch (_state)
            {
                case WaveState.Combat:
                    UpdateCombatSpawning(Time.deltaTime);
                    break;
                case WaveState.Intermission:
                    UpdateIntermission(Time.deltaTime);
                    break;
                case WaveState.Reward:
                    if (_cardRewardController != null && !_cardRewardController.IsOpen)
                    {
                        if (_completeStageAfterReward)
                        {
                            CompleteStage();
                        }
                        else
                        {
                            StartIntermission();
                        }
                    }

                    break;
                case WaveState.BossCombat:
                    StateChanged?.Invoke();
                    break;
                case WaveState.Failed:
                    break;
            }
        }

        private void InitializeFromSave()
        {
            var runSaveData = SaveManager != null ? SaveManager.LoadRunSaveData() : null;
            var resolvedStageData = ResolveStageDataForSave(runSaveData);

            if (resolvedStageData != _stageData)
            {
                _stageData = resolvedStageData;

                // StageArenaArtPresenter.Awake() and TowerService's initial slots run before this
                // OnEnable in Unity's activation order, so both were built for the scene's default
                // Stage 1. Rebuilt at once, not crossfaded: nothing has been on screen yet.
                StageTransition.RebuildWorld(false);
            }

            if (_stageData == null || _stageData.Waves == null || _stageData.Waves.Length == 0)
            {
                _state = WaveState.Completed;
                StateChanged?.Invoke();
                return;
            }

            RestoreCoreHealthFromSave(runSaveData);
            _currentWaveIndex = runSaveData != null ? Mathf.Max(0, runSaveData.WaveIndex - 1) : 0;
            StageTransition.BeginStage(StageIndex);
            ApplyPersistenceContext();
            RaiseStageStartedIfNeeded(WaveEncounterLabelResolver.ResolveStageStartDetail(_stageData, _currentWaveIndex, TotalWaveCount, _state, _bossRuntimeController, runSaveData));

            if (runSaveData != null && runSaveData.HasPendingReward)
            {
                _completeStageAfterReward = _stageData.BossData != null && runSaveData.PendingRewardId == _stageData.BossData.BossRewardData.RewardId;
                _state = WaveState.Reward;
                StateChanged?.Invoke();
                WaveEvents.RaiseRewardPending(CreateEventArgs(CurrentWaveNumber, WaveEncounterLabelResolver.ResolveCurrentEncounterLabel(_stageData, _currentWaveIndex, _state, _bossRuntimeController), WaveEncounterLabelResolver.ResolveCurrentEncounterIsElite(_stageData, _currentWaveIndex, _state)));
                return;
            }

            if (runSaveData != null && runSaveData.IsBossEncounterActive && _stageData.BossData != null)
            {
                StartBossEncounter(runSaveData.BossPhaseIndex);
                return;
            }

            if (runSaveData != null && runSaveData.IsIntermissionOpen)
            {
                StartIntermission();
                return;
            }

            _currentWaveIndex = Mathf.Clamp(_currentWaveIndex, 0, _stageData.Waves.Length - 1);
            StartCurrentWave();
        }

        // Resolves Stages.md's "Bekannte Grenze": a fresh app start resuming mid-Stage-2+ used to
        // always fall back to whatever StageData is serialized on this component in the scene
        // (always Stage 1), because there was no way to turn RunSaveData.StageIndex back into the
        // matching StageData asset. StageProgressionData.Stages - already referenced by every
        // StageData's _progressionData field, and already the list AdvanceToNextStage() walks at
        // runtime - is exactly that registry, so resolving StageIndex against it needs no new data,
        // just this lookup. Falls back to the scene's own _stageData (today's behavior) whenever the
        // save, the progression asset, or the index itself is missing/out of range.
        private StageData ResolveStageDataForSave(RunSaveData runSaveData)
        {
            if (runSaveData == null || _stageData == null || _stageData.ProgressionData == null)
            {
                return _stageData;
            }

            var stages = _stageData.ProgressionData.Stages;

            if (stages == null || stages.Length == 0)
            {
                return _stageData;
            }

            var targetIndex = runSaveData.StageIndex - 1;

            if (targetIndex < 0 || targetIndex >= stages.Length || stages[targetIndex] == null)
            {
                return _stageData;
            }

            return stages[targetIndex];
        }

        private void StartCurrentWave()
        {
            if (_stageData == null || _stageData.Waves == null || _currentWaveIndex < 0 || _currentWaveIndex >= _stageData.Waves.Length)
            {
                CompleteStage();
                return;
            }

            _spawnGroups.Clear();
            _remainingAliveEnemies = 0;
            _completeStageAfterReward = false;
            _state = WaveState.Combat;
            _waveStartSpawnDelayTimer = WaveStartSpawnDelaySeconds;

            // Re-rolled once per wave rather than per enemy, so a single wave still reads as one
            // coherent attack from a consistent direction set - it is the wave-to-wave repetition
            // that felt static, not the spread within a wave.
            _spawnPlacement.BeginWave();

            var waveData = _stageData.Waves[_currentWaveIndex];
            var modifierData = waveData != null ? waveData.ModifierData : null;

            if (waveData != null && waveData.SpawnGroups != null)
            {
                for (var index = 0; index < waveData.SpawnGroups.Length; index++)
                {
                    var spawnGroup = waveData.SpawnGroups[index];

                    if (spawnGroup == null || spawnGroup.EnemyData == null)
                    {
                        continue;
                    }

                    _spawnGroups.Add(new SpawnGroupRuntime(spawnGroup, modifierData, _spawnCountVariance));
                }
            }

            // Rolled once here rather than per frame: a per-frame chance would effectively
            // guarantee an objective in a long wave and never fire in a short one. Boss encounters
            // run through StartBossEncounter and never reach this method, which is how they stay
            // excluded.
            _spawnedEnemiesThisWave = 0;
            _arenaObjectiveService.BeginWave(_arenaObjectiveData, _stageData, waveData, false);

            PulseUpcomingSpawnGates();
            ApplyPersistenceContext();
            WaveEvents.RaiseWaveStarted(CreateEventArgs(CurrentWaveNumber, WaveEncounterLabelResolver.ResolveCurrentEncounterLabel(_stageData, _currentWaveIndex, _state, _bossRuntimeController), WaveEncounterLabelResolver.ResolveCurrentEncounterIsElite(_stageData, _currentWaveIndex, _state)));
            StateChanged?.Invoke();
        }

        // UI proposal 3: the gates the wave is about to come out of pulse for the wave-start spawn
        // delay, so the rotating spawn fan finally has a visible tell instead of reading as noise.
        private void PulseUpcomingSpawnGates()
        {
            var arena = ArenaArtPresenter;

            if (arena == null)
            {
                return;
            }

            // One gate per spawn group, not per enemy: the gates that light are the ones the wave's
            // groups are actually assigned to.
            var groupCount = 0;

            for (var index = 0; index < _spawnGroups.Count; index++)
            {
                if (_spawnGroups[index].RemainingCount > 0)
                {
                    groupCount++;
                }
            }

            _spawnPlacement.CollectUpcomingGateIndices(_stageData, groupCount, _upcomingGateIndices);
            arena.PulseSpawnGates(_upcomingGateIndices, WaveStartSpawnDelaySeconds);
        }

        // The gate closest to where the boss will appear, so the warning points at the right place.
        private void PulseGateNearest(Vector2 position)
        {
            var arena = ArenaArtPresenter;

            if (arena == null || _stageData == null)
            {
                return;
            }

            _spawnPlacement.CollectNearestGateIndex(_stageData, position, _upcomingGateIndices);
            arena.PulseSpawnGates(_upcomingGateIndices, WaveStartSpawnDelaySeconds);
        }

        private void StartBossEncounter(int restoredPhaseIndex)
        {
            if (_stageData == null || _stageData.BossData == null)
            {
                CompleteStage();
                return;
            }

            _currentWaveIndex = Mathf.Max(TotalWaveCount, _currentWaveIndex);
            _completeStageAfterReward = false;

            // Boss waves get no arena objective: the fight already dictates where the player
            // stands, so one would compete with the encounter rather than shape it. Clearing here
            // also covers an objective still live when the last wave rolled into the boss.
            _arenaObjectiveService.EndWave();
            _state = WaveState.BossCombat;
            BossEncounterState.SetActive(true);

            if (_bossRuntimeController == null)
            {
                var bossObject = new GameObject("BossRuntime");
                bossObject.transform.SetParent(_enemyParent, false);
                _bossRuntimeController = bossObject.AddComponent<BossRuntimeController>();
            }
            else if (!_bossRuntimeController.gameObject.activeSelf)
            {
                _bossRuntimeController.gameObject.SetActive(true);
            }

            _bossRuntimeController.PhaseChanged -= OnBossPhaseChanged;
            var bossSpawnPosition = _spawnPlacement.ResolveBossSpawnPosition(_stageData, ArenaArtPresenter, _playerTarget);
            _bossRuntimeController.transform.position = bossSpawnPosition;

            // Owner 2026-09-25: the boss gets the same 2 s warning an ordinary wave gets. Its
            // arrival used to be instant, which is the one spawn in the game that most deserves a
            // tell. The boss holds still and takes no action for the delay; the nearest gate pulses.
            PulseGateNearest(bossSpawnPosition);
            _bossRuntimeController.Initialize(_stageData.BossData, _playerTarget, _coreTarget, _sharedProjectilePrototype, _projectileParent, _enemyPool, restoredPhaseIndex);

            // AFTER Initialize, not before. Initialize assigns _engageDelayTimer outright, so the
            // call sitting above it was silently overwritten and the boss engaged on its own 2 s
            // default no matter what was asked for - reported twice (2026-09-27, 2026-09-28) as
            // "boss spawn is still not delayed". SetSpawnDelay only ever raises the timer, so
            // running it afterwards is what actually applies the wave's longer delay.
            _bossRuntimeController.SetSpawnDelay(WaveStartSpawnDelaySeconds);
            _bossRuntimeController.PhaseChanged += OnBossPhaseChanged;
            ApplyPersistenceContext();
            WaveEvents.RaiseWaveStarted(CreateEventArgs(CurrentWaveNumber, WaveEncounterLabelResolver.ResolveCurrentEncounterLabel(_stageData, _currentWaveIndex, _state, _bossRuntimeController), false));
            BossEvents.RaiseBossSpawned(CreateBossEventArgs());
            StateChanged?.Invoke();
        }

        private void UpdateCombatSpawning(float deltaTime)
        {
            if (_waveStartSpawnDelayTimer > 0f)
            {
                _waveStartSpawnDelayTimer = Mathf.Max(0f, _waveStartSpawnDelayTimer - deltaTime);
                return;
            }

            if (_enemyPool == null)
            {
                _enemyPool = new EnemyPool(_enemyParent);
            }

            var hasPendingSpawns = false;

            for (var index = 0; index < _spawnGroups.Count; index++)
            {
                var spawnGroup = _spawnGroups[index];

                if (!spawnGroup.HasRemaining)
                {
                    continue;
                }

                hasPendingSpawns = true;
                spawnGroup.Tick(deltaTime);

                if (!spawnGroup.ShouldSpawn)
                {
                    continue;
                }

                var enemy = _enemyPool.Get();

                // Two arguments that look interchangeable and are not. The spawn index is a running
                // per-wave counter, NOT the group index: passing the group index here made every
                // enemy in a group resolve to the identical position. The group index picks the
                // gate, so a group stays one stream from one side of the arena.
                enemy.transform.position = _spawnPlacement.ResolveEnemySpawnPosition(
                    _stageData, ArenaArtPresenter, _spawnPlacement.NextSpawnIndex(), _spawnAngleJitter, _playerTarget, index);
                enemy.Initialize(spawnGroup.EnemyData, spawnGroup.EliteModifierData, _playerTarget, _coreTarget, _sharedProjectilePrototype, _projectileParent);
                _remainingAliveEnemies += 1;
                _spawnedEnemiesThisWave += 1;
                spawnGroup.ConsumeSpawn();
            }

            // After the wave-start delay's early return above, so an objective can never compete
            // with the gate pulse that tells the player where the wave is coming from.
            _arenaObjectiveService.TickCombat(
                _arenaObjectiveData, ArenaArtPresenter, _playerTarget, _enemyParent, _spawnedEnemiesThisWave);

            if (!hasPendingSpawns && _remainingAliveEnemies <= 0)
            {
                CompleteCurrentWave();
            }

            StateChanged?.Invoke();
        }

        private void CompleteCurrentWave()
        {
            var waveData = _stageData.Waves[_currentWaveIndex];
            _state = WaveState.Reward;

            // Before ForceCollectAll, and pointedly not part of it. The vacuum exists so the player
            // does not have to walk over every last drop; handing them an arena objective they
            // chose not to go for would delete the decision the objective exists to create (D7).
            _arenaObjectiveService.EndWave();
            _resourcePickupPool?.ForceCollectAll();
            _cardService?.NotifyWaveCleared();
            MetaRewardLedger.GrantExperience(SaveManager, _metaProgressionDefinitionData, MetaRewardLedger.ResolveWaveExperience(StageIndex));

            // Before the event, so the banner that announces the clear already knows (D10), and
            // before the snapshot below, so the bonus Scrap is in the save.
            if (_coreDefense != null)
            {
                _coreDefense.ResolveWaveClear();
            }

            WaveEvents.RaiseWaveCompleted(CreateEventArgs(CurrentWaveNumber, WaveEncounterLabelResolver.ResolveCurrentEncounterLabel(_stageData, _currentWaveIndex, _state, _bossRuntimeController), WaveEncounterLabelResolver.ResolveCurrentEncounterIsElite(_stageData, _currentWaveIndex, _state)));

            if (_runPersistenceController != null)
            {
                _runPersistenceController.WriteWaveCompletionSnapshot(_currentWaveIndex + 1, waveData != null ? waveData.WaveRewardData : null);
            }
            else if (SaveManager != null)
            {
                var runSaveData = SaveManager.LoadRunSaveData() ?? new RunSaveData();
                var nextWaveIndex = _currentWaveIndex + 2;
                runSaveData.SetIntermissionOpen(true, StageIndex, nextWaveIndex);

                if (waveData != null && waveData.WaveRewardData != null)
                {
                    runSaveData.SetPendingReward(waveData.WaveRewardData.RewardId, waveData.WaveRewardData.RewardLabel);
                }

                runSaveData.SetBossEncounterState(false, 0);
                SaveManager.SaveRunSaveData(runSaveData);
            }

            WaveEvents.RaiseRewardPending(CreateEventArgs(CurrentWaveNumber, WaveEncounterLabelResolver.ResolveCurrentEncounterLabel(_stageData, _currentWaveIndex, _state, _bossRuntimeController), WaveEncounterLabelResolver.ResolveCurrentEncounterIsElite(_stageData, _currentWaveIndex, _state)));
            StateChanged?.Invoke();
        }

        private void CompleteBossEncounter()
        {
            BossEncounterState.SetActive(false);
            _state = WaveState.Reward;
            _completeStageAfterReward = true;

            // Playtest 2026-09-21: "if a boss dies, all remaining enemies die with him".
            // Leaving the boss's summoned adds alive after the fight is decided turns a victory
            // into a cleanup chore, and the reward screen opens over a field that is still
            // shooting at the player.
            DespawnRemainingEnemies();
            _resourcePickupPool?.ForceCollectAll();

            if (_runPersistenceController != null)
            {
                _runPersistenceController.WriteBossCompletionSnapshot(_bossRuntimeController != null ? _bossRuntimeController.BossRewardData : null);
            }

            BossEvents.RaiseBossDefeated(CreateBossEventArgs());
            GrantBossDefeatRelic();
            AnnounceMetaReward(MetaRewardLedger.GrantBossDefeat(
                SaveManager,
                _metaProgressionDefinitionData,
                StageIndex,
                _stageData != null && _stageData.BossData != null ? _stageData.BossData.BossId : string.Empty));
            WaveEvents.RaiseRewardPending(CreateEventArgs(CurrentWaveNumber, WaveEncounterLabelResolver.ResolveCurrentEncounterLabel(_stageData, _currentWaveIndex, _state, _bossRuntimeController), false));
            StateChanged?.Invoke();
        }

        private void GrantBossDefeatRelic()
        {
            // CardService.TryGrantRandomRelic() raises RelicEvents.RelicGranted itself, which
            // RelicRevealPresenter listens for - no toast call needed here anymore.
            var granted = _cardService != null ? _cardService.TryGrantRandomRelic() : null;

            if (granted != null)
            {
                return;
            }

            // Playtest 2026-09-28: "defeating a boss should give a relic, currently I don't get
            // any." A silent null here is indistinguishable from the feature not existing, and the
            // three ways it can happen are all configuration rather than logic: no CardService on
            // this object, an empty relic pool, or every relic already held / gated behind an
            // unmet unlock. Saying which one it was turns an invisible miss into a fixable report.
            Debug.LogWarning(_cardService == null
                ? "[Relic] Boss defeated but no CardService is attached to WaveManager - no relic granted."
                : "[Relic] Boss defeated but no relic was eligible: the pool is empty, every relic is already held, or all remaining ones are gated behind unmet unlocks.");
        }

        private void StartIntermission()
        {
            _state = WaveState.Intermission;
            _intermissionStartedFrame = Time.frameCount;
            _intermissionTimer = _stageData != null ? _stageData.IntermissionDurationSeconds : 20f;
            _requiresIntermissionShopAction = _shopService != null && _shopService.TryOpenForIntermission(_stageData, CurrentWaveNumber);

            // Playtest 2026-09-20: "intermission phase to build towers should be in each rotation
            // between enemy spawn after reward screen". The build gate used to be suppressed on
            // any wave the shop opened on, so the one rotation that offered the most to spend
            // resources on was also the only one that skipped building. The two are independent
            // now: the shop resolves first, then the build gate holds the intermission open, so
            // every rotation ends with a chance to place a tower.
            _requiresIntermissionTowerAction = _towerService != null && _towerService.HasAvailableIntermissionAction;
            MovePlayerToPostRewardPosition();
            WaveEvents.RaiseIntermissionStarted(CreateEventArgs(CurrentWaveNumber + 1, WaveEncounterLabelResolver.ResolveNextEncounterLabel(_stageData, _currentWaveIndex, TotalWaveCount), WaveEncounterLabelResolver.ResolveNextEncounterIsElite(_stageData, _currentWaveIndex, TotalWaveCount)));
            StateChanged?.Invoke();
        }

        private void UpdateIntermission(float deltaTime)
        {
            if (_requiresIntermissionShopAction || _requiresIntermissionTowerAction)
            {
                StateChanged?.Invoke();
                return;
            }

            // Playtest 2026-09-22: "don't start the countdown when in the weapon upgrade menu".
            // Managing the loadout is part of the Intermission, not time taken from it.
            if (Corebound.UI.Gameplay.WeaponLoadoutPanelPresenter.IsOpen)
            {
                StateChanged?.Invoke();
                return;
            }

            _intermissionTimer = Mathf.Max(0f, _intermissionTimer - deltaTime);

            if (_intermissionTimer <= 0f)
            {
                AdvanceEncounter();
            }

            StateChanged?.Invoke();
        }

        private void AdvanceEncounter()
        {
            _currentWaveIndex += 1;

            if (_currentWaveIndex >= TotalWaveCount)
            {
                StartBossEncounter(0);
                return;
            }

            StartCurrentWave();
        }

        private void CompleteStage()
        {
            _state = WaveState.Completed;
            _completeStageAfterReward = false;

            var stageRewardData = _stageData != null ? _stageData.StageRewardData : null;

            if (_resourceWalletService != null && stageRewardData != null && stageRewardData.CompletionResourceBonus > 0)
            {
                _resourceWalletService.AddReward(stageRewardData.CompletionResourceBonus);
            }

            WaveEvents.RaiseStageCompleted(CreateEventArgs(CurrentWaveNumber, WaveEncounterLabelResolver.ResolveStageCompletionLabel(_stageData), false));
            StageEvents.RaiseStageCompleted(CreateStageEventArgs(WaveEncounterLabelResolver.ResolveStageCompletionLabel(_stageData)));
            StageEvents.RaiseStageTransitioned(CreateStageEventArgs(WaveEncounterLabelResolver.ResolveStageTransitionLabel(_stageData)));

            MetaRewardLedger.RecordStageCleared(SaveManager, _metaProgressionDefinitionData, ShouldEndRunOnCompletion(), CaptureLoadoutSlots());

            if (ShouldEndRunOnCompletion())
            {
                RecordRunOutcomeStatistic(true);

                if (SaveManager != null)
                {
                    SaveManager.ClearRunSaveData();
                }

                StateChanged?.Invoke();
                return;
            }

            AdvanceToNextStage();
        }

        // The stage-asset lookup this comment used to say was missing now exists:
        // ResolveStageDataForSave() indexes StageProgressionData.Stages by the saved StageIndex, so
        // resuming mid-Stage-2+ loads the right StageData. What was actually broken until
        // 2026-09-28 was the *write* side - nothing checkpointed the save when a new stage began,
        // so the newest record on disk was still the previous stage's last wave.
        private void AdvanceToNextStage()
        {
            var nextStageData = StageTransitionCoordinator.ResolveNextStage(_stageData);

            if (nextStageData == null)
            {
                StateChanged?.Invoke();
                return;
            }

            _stageData = nextStageData;
            _currentWaveIndex = 0;
            StageTransition.BeginStage(StageIndex);
            StageTransition.PayStageStartBonus(StageIndex);
            _remainingAliveEnemies = 0;
            _spawnGroups.Clear();
            _intermissionTimer = 0f;
            _requiresIntermissionShopAction = false;
            _requiresIntermissionTowerAction = false;

            StageTransition.RebuildWorld(true);
            StartCurrentWave();

            // After StartCurrentWave, which has pushed the new stage and wave into persistence.
            StageTransition.CheckpointStageEntry();
        }

        private void OnConfirmPressed()
        {
            // The loadout panel owns Confirm while it is open, and the frame it opens or closes
            // in (single-frame event consumption).
            if (Corebound.UI.Gameplay.WeaponLoadoutPanelPresenter.IsOpen || Corebound.UI.Gameplay.WeaponLoadoutPanelPresenter.LastToggleFrame == Time.frameCount)
            {
                return;
            }

            // Playtest feedback (2026-09-17): "Intermission should be ended via btn press 'Enter'
            // even if nothing was built" - the mandatory build-a-tower gate (below,
            // _requiresIntermissionTowerAction) used to block Confirm entirely until the player
            // placed or upgraded something. Building a tower is now purely optional during
            // Intermission; only the Shop's own mandatory buy/skip gate still blocks Confirm.
            // Playtest 2026-09-20: "intermission phase to build towers should be in each rotation
            // between enemy spawn after reward screen." It already was, on paper - the state machine
            // really did enter Intermission every rotation. It just left again in the same frame, so
            // no human ever saw it.
            //
            // Confirming the card reward closes the reward screen, which synchronously raises
            // StateChanged -> OnRewardStateChanged -> StartIntermission. All of that runs inside the
            // ConfirmPressed invocation list, so by the time this handler is reached the state is
            // already Intermission and the very same press ended it. The existing
            // _intermissionGateResolvedFrame guard did not help: it covers a gate being cleared, not
            // the intermission being started.
            //
            // _intermissionStartedFrame closes that hole, mirroring the identical fix in
            // ShopService.OnConfirmPressed - the shop was being opened and consumed by a single
            // press for exactly the same reason.
            if (_state == WaveState.Intermission
                && !_requiresIntermissionShopAction
                && _intermissionGateResolvedFrame != Time.frameCount
                && _intermissionStartedFrame != Time.frameCount
                && (_cardRewardController == null || !_cardRewardController.IsOpen))
            {
                AdvanceEncounter();
            }
        }

        private void OnRewardStateChanged()
        {
            if (_state == WaveState.Reward && _cardRewardController != null && !_cardRewardController.IsOpen)
            {
                if (_completeStageAfterReward)
                {
                    CompleteStage();
                }
                else
                {
                    StartIntermission();
                }
            }
        }

        private void OnIntermissionBuildCommitted()
        {
            if (_state != WaveState.Intermission || !_requiresIntermissionTowerAction)
            {
                return;
            }

            // Placing/upgrading a tower only satisfies the mandatory build requirement - it must
            // not end the intermission by itself (playtest feedback: "intermission phase should end
            // on player input... currently it ends with placing a tower"). Clearing the flag lets
            // OnConfirmPressed's normal Intermission check succeed, the same way OnShopResolved
            // already does for the shop's mandatory-action gate, so the player explicitly confirms
            // when they're done instead of the phase ending the instant they build one tower.
            _requiresIntermissionTowerAction = false;
            _intermissionGateResolvedFrame = Time.frameCount;
            StateChanged?.Invoke();
        }

        private void OnShopResolved()
        {
            if (_state != WaveState.Intermission || !_requiresIntermissionShopAction)
            {
                return;
            }

            _requiresIntermissionShopAction = false;
            _requiresIntermissionTowerAction = _towerService != null && _towerService.HasAvailableIntermissionAction;
            _intermissionGateResolvedFrame = Time.frameCount;
            StateChanged?.Invoke();
        }

        // No ApplyPersistenceContext here, unlike the phase change: a windup is not a state
        // transition worth writing to the save file every couple of seconds.
        private void OnBossHeavyAttackStarted(int phaseIndex)
        {
            BossEvents.RaiseBossHeavyAttack(CreateBossEventArgs());
        }

        private void OnBossPhaseChanged(int phaseIndex)
        {
            ApplyPersistenceContext();
            BossEvents.RaiseBossPhaseChanged(CreateBossEventArgs());
            StateChanged?.Invoke();
        }

        private void OnCombatantDied(DeathEventArgs eventArgs)
        {
            if (eventArgs.Victim == null)
            {
                return;
            }

            if ((_playerTarget != null && eventArgs.Victim == _playerTarget.gameObject)
                || (_coreTarget != null && eventArgs.Victim == _coreTarget.gameObject))
            {
                // Playtest feedback (2026-09-17): "Run Failed should contain 'player Died' or 'core
                // destroyed'" - the generic message didn't say which. Captured here since this is
                // the one place that already distinguishes the two victims.
                _wasCoreDestroyedOnFailure = _coreTarget != null && eventArgs.Victim == _coreTarget.gameObject;
                _arenaObjectiveService.EndWave();
                _state = WaveState.Failed;
                RecordRunOutcomeStatistic(false);
                StateChanged?.Invoke();
                return;
            }

            if (_state == WaveState.BossCombat)
            {
                var boss = eventArgs.Victim.GetComponent<BossRuntimeController>();

                if (boss != null)
                {
                    CompleteBossEncounter();
                    return;
                }

                var helperEnemy = eventArgs.Victim.GetComponent<EnemyRuntimeController>();

                if (helperEnemy != null)
                {
                    SpawnResourcePickup(ResolveKillScrap(helperEnemy), eventArgs.Victim.transform.position);
                    RecordEnemyKillStatistic(eventArgs.DamageContext, helperEnemy);
                    GrantEliteKillReward(helperEnemy);

                    if (helperEnemy.IsRangedPressureRole)
                    {
                        _cardService?.NotifyRangedEnemyKilled();
                    }

                    StateChanged?.Invoke();
                }

                return;
            }

            if (_state != WaveState.Combat)
            {
                return;
            }

            var enemy = eventArgs.Victim.GetComponent<EnemyRuntimeController>();

            if (enemy == null)
            {
                return;
            }

            SpawnResourcePickup(ResolveKillScrap(enemy), eventArgs.Victim.transform.position);
            RecordEnemyKillStatistic(eventArgs.DamageContext, enemy);
            GrantEliteKillReward(enemy);

            if (enemy.IsRangedPressureRole)
            {
                _cardService?.NotifyRangedEnemyKilled();
            }

            _remainingAliveEnemies = Mathf.Max(0, _remainingAliveEnemies - 1);

            if (!HasPendingSpawns() && _remainingAliveEnemies <= 0)
            {
                CompleteCurrentWave();
            }

            StateChanged?.Invoke();
        }

#if UNITY_EDITOR
        // Open Topics 4, owner decision (a): the autopilot's bossN variant goes straight into the
        // stage's boss fight, so boss phases are reached in seconds instead of after six waves.
        // Only from Combat, so it cannot cut into a reward, shop or intermission.
        public bool DebugSkipToBoss()
        {
            if (_state != WaveState.Combat)
            {
                return false;
            }

            DespawnRemainingEnemies();
            StartBossEncounter(0);
            return true;
        }

        public BossRuntimeController DebugBoss => _bossRuntimeController;
#endif

        // Kills rather than merely despawns, so every enemy still pays its reward and its kill
        // statistic - a player who cleared the wave down to two adds should not lose that Scrap
        // because the boss happened to die first.
        private void DespawnRemainingEnemies()
        {
            var enemies = FindObjectsByType<EnemyRuntimeController>(FindObjectsSortMode.None);

            for (var index = 0; index < enemies.Length; index++)
            {
                var enemy = enemies[index];

                if (enemy == null || !enemy.isActiveAndEnabled || !enemy.IsAlive)
                {
                    continue;
                }

                SpawnResourcePickup(enemy.ResourceRewardAmount, enemy.transform.position);

                // No weapon landed these; they fell with the boss, so no kill quest advances.
                RecordEnemyKillStatistic(default);
                enemy.gameObject.SetActive(false);
            }

            _remainingAliveEnemies = 0;
            _spawnGroups.Clear();
        }

        private bool HasPendingSpawns()
        {
            for (var index = 0; index < _spawnGroups.Count; index++)
            {
                if (_spawnGroups[index].HasRemaining)
                {
                    return true;
                }
            }

            return false;
        }

        private void SpawnResourcePickup(int amount, Vector2 position)
        {
            if (amount <= 0 || _resourceWalletService == null)
            {
                return;
            }

            if (_resourcePickupPool == null)
            {
                _resourcePickupPool = new ResourcePickupPool(_enemyParent);
            }

            var pickup = _resourcePickupPool.Get();
            pickup.Initialize(amount, position, _playerTarget, _resourceWalletService);
        }

        private void RecordEnemyKillStatistic(DamageContext killingBlow, EnemyRuntimeController enemy = null)
        {
            WaveStatisticsRecorder.RecordEnemyKill(SaveManager);
            _runEnemiesKilled += 1;

            // The weapon-kill quests count only what a player weapon finished, the tower-kill quests
            // only what a tower finished; the Core counts for neither.
            var weaponId = killingBlow.SourceType == DamageSourceType.Player ? killingBlow.SourceId : null;
            var towerId = killingBlow.SourceType == DamageSourceType.Tower ? killingBlow.SourceId : null;
            var enemyId = enemy != null && enemy.Data != null ? enemy.Data.EnemyId : null;
            var eliteKey = enemy != null && enemy.EliteModifier != null ? QuestLedger.EliteAffixKey(enemy.EliteModifier) : null;
            MetaRewardLedger.RecordEnemyKill(SaveManager, _metaProgressionDefinitionData, weaponId, towerId, enemyId, eliteKey);
        }

        // A toast, never an overlay: levelling up mid-combat must not touch Time.timeScale
        // (levelling L4, and Vision section 19's readability boundary).
        private void OnLevelGained(int newLevel, int skillPoints)
        {
            Corebound.UI.NotificationPresenter.Show(LocalizationService.Resolve(
                "Level " + newLevel + " - +" + skillPoints + " Skill Points",
                "Level " + newLevel + " - +" + skillPoints + " Skillpunkte")
                + (newLevel >= MetaProgressionSaveData.MaxLevel
                    ? LocalizationService.Resolve(" - level cap reached, Ascension unlocked", " - Höchststufe erreicht, Aufstieg freigeschaltet")
                    : string.Empty));
        }

        private void OnCardCollectedFirstTime(string cardId)
        {
            if (SaveManager == null || _metaProgressionDefinitionData == null)
            {
                return;
            }

            var runSaveData = SaveManager.LoadRunSaveData();

            if (runSaveData != null && runSaveData.IsTutorialRun)
            {
                return;
            }

            var saveData = SaveManager.LoadMetaProgressionSaveData(_metaProgressionDefinitionData.CreateDefaultSaveData());
            QuestLedger.EvaluateCardCollection(saveData, _metaProgressionDefinitionData, SaveManager);
            SaveManager.SaveMetaProgressionSaveData(saveData);
        }

        private void OnParagonLevelGained(int paragonLevel)
        {
            Corebound.UI.NotificationPresenter.Show(LocalizationService.Resolve(
                "Paragon " + paragonLevel + " - +1 Ascension Point",
                "Paragon " + paragonLevel + " - +1 Aufstiegspunkt")
                + (paragonLevel % MetaProgressionSaveData.MilestoneLevelInterval == 0
                    ? LocalizationService.Resolve(" - a Keystone is ready", " - ein Schlüsselstein ist bereit")
                    : string.Empty));
        }

        private void RecordRunOutcomeStatistic(bool completed)
        {
            WaveStatisticsRecorder.RecordRunOutcome(SaveManager, StageIndex, completed);
            RecordRunHistory(completed);
        }

        // Both run-end paths already funnel through RecordRunOutcomeStatistic, so the history write
        // sits here rather than being repeated in the victory and death branches - the same
        // single-chokepoint shape MetaRewardLedger uses, for the same reason.
        private void RecordRunHistory(bool completed)
        {
            _runTimer?.StopRun();

            var endedBy = completed
                ? LocalizationService.Resolve("Run completed", "Run abgeschlossen")
                : (_wasCoreDestroyedOnFailure
                    ? LocalizationService.Resolve("Core destroyed", "Kern zerstört")
                    : LocalizationService.Resolve("Player died", "Spieler gestorben"));

            _historyCardIds.Clear();
            _historyRelicIds.Clear();

            if (_cardService != null)
            {
                var cards = _cardService.SelectedCards;

                for (var index = 0; cards != null && index < cards.Count; index++)
                {
                    if (cards[index] != null)
                    {
                        _historyCardIds.Add(cards[index].CardId);
                    }
                }

                var relics = _cardService.SelectedRelics;

                for (var index = 0; relics != null && index < relics.Count; index++)
                {
                    if (relics[index] != null)
                    {
                        _historyRelicIds.Add(relics[index].RelicId);
                    }
                }
            }

            RunHistoryRecorder.RecordRunEnded(
                SaveManager,
                _metaProgressionDefinitionData,
                completed ? RunOutcome.Completed : RunOutcome.Died,
                StageIndex,
                CurrentWaveNumber,
                endedBy,
                _runTimer != null ? _runTimer.ElapsedActiveSeconds : 0f,
                _runTimer != null ? _runTimer.ElapsedRealSeconds : 0f,
                _runTimer != null ? _runTimer.StartedUtc : System.DateTime.UtcNow,
                _runEnemiesKilled,
                _resourceWalletService != null ? _resourceWalletService.CurrentAmount : 0,
                _historyCardIds,
                _historyRelicIds,
                CaptureRunBuild());
        }

        private readonly List<string> _historyWeaponIds = new List<string>();
        private readonly List<int> _historyWeaponTiers = new List<int>();
        private PlayerCombatController _historyPlayer;

        // Every slot's weapon id, empty ones included, for the "clear the game carrying six" quests.
        private readonly List<string> _slotWeaponIds = new List<string>();

        private IReadOnlyList<string> CaptureLoadoutSlots()
        {
            _slotWeaponIds.Clear();

            if (_historyPlayer == null && _playerTarget != null)
            {
                _historyPlayer = _playerTarget.GetComponentInParent<PlayerCombatController>();
            }

            var loadout = _historyPlayer != null ? _historyPlayer.Loadout : null;

            for (var index = 0; loadout != null && index < Corebound.Gameplay.Weapons.WeaponLoadoutRuntime.Capacity; index++)
            {
                var slot = loadout.GetSlot(index);
                _slotWeaponIds.Add(slot != null && !slot.IsEmpty ? slot.Weapon.WeaponId : string.Empty);
            }

            return _slotWeaponIds;
        }

        // The loadout and towers as they are at the moment the run ends, from the live objects.
        private RunHistoryRecorder.RunBuild CaptureRunBuild()
        {
            _historyWeaponIds.Clear();
            _historyWeaponTiers.Clear();

            if (_historyPlayer == null && _playerTarget != null)
            {
                _historyPlayer = _playerTarget.GetComponentInParent<PlayerCombatController>();
            }

            var loadout = _historyPlayer != null ? _historyPlayer.Loadout : null;

            for (var index = 0; loadout != null && index < Corebound.Gameplay.Weapons.WeaponLoadoutRuntime.Capacity; index++)
            {
                var slot = loadout.GetSlot(index);

                if (slot != null && !slot.IsEmpty)
                {
                    _historyWeaponIds.Add(slot.Weapon.WeaponId);
                    _historyWeaponTiers.Add(slot.Tier);
                }
            }

            return new RunHistoryRecorder.RunBuild(
                _isTutorialRun,
                _historyWeaponIds,
                _historyWeaponTiers,
                _towerService != null ? _towerService.BuiltTowerIds : null,
                _towerService != null ? _towerService.BuiltTowerUpgradeLevels : null);
        }

        private readonly List<string> _historyCardIds = new List<string>();
        private readonly List<string> _historyRelicIds = new List<string>();

        // Salvage Instinct: elites drop (1 + value) times their Scrap.
        private static int ResolveKillScrap(EnemyRuntimeController enemy)
        {
            // Salvager (CHARACTER_CLASSES_2026-09-30): every kill drops half as much again.
            var amount = Mathf.RoundToInt(enemy.ResourceRewardAmount * CharacterEffects.ScrapDropMultiplier);

            if (!enemy.IsElite)
            {
                return amount;
            }

            var bonus = RunProgressionSnapshot.Current.Skills[SkillEffectTarget.EliteScrapMultiplier];
            return bonus > 0f ? Mathf.RoundToInt(amount * (1f + bonus)) : amount;
        }

        // Once per run: Starting Scrap and starting relics from the skill tree. Remembered in the
        // run save so a resumed run is not paid twice; a tutorial run gets nothing.
        private void ApplyRunStartBonuses(RunSaveData runSaveData)
        {
            if (runSaveData == null || runSaveData.RunStartBonusesApplied || runSaveData.IsTutorialRun || SaveManager == null)
            {
                return;
            }

            var skills = RunProgressionSnapshot.Current.Skills;
            var character = RunProgressionSnapshot.Current.Character;
            var startingScrap = Mathf.RoundToInt(skills[SkillEffectTarget.StartingScrap]) + (character != null ? character.StartingScrapBonus : 0);

            // Engineer kit: one tower already standing in the slot nearest the Core.
            if (character != null && !string.IsNullOrEmpty(character.StartingTowerId) && _towerService != null && _coreTarget != null)
            {
                _towerService.TryPlaceKitTower(character.StartingTowerId, _coreTarget.position);
            }

            if (startingScrap > 0 && _resourceWalletService != null)
            {
                _resourceWalletService.AddFlat(startingScrap);
            }

            var relics = Mathf.RoundToInt(skills[SkillEffectTarget.StartingRelic]);

            for (var index = 0; index < relics; index++)
            {
                _cardService?.TryGrantRandomRelic();
            }

            // Re-read: the grants above write their own changes into the run save.
            var latest = SaveManager.LoadRunSaveData() ?? runSaveData;
            latest.MarkRunStartBonusesApplied();
            SaveManager.SaveRunSaveData(latest);

            if (startingScrap > 0 || relics > 0)
            {
                Debug.Log("[Progression] Run start bonuses: +" + startingScrap + " Scrap, " + relics + " relic(s).");
            }
        }

        private void GrantEliteKillReward(EnemyRuntimeController enemy)
        {
            if (enemy != null && enemy.IsElite)
            {
                AnnounceMetaReward(MetaRewardLedger.GrantEliteKill(SaveManager, _metaProgressionDefinitionData));
            }
        }

        private static void AnnounceMetaReward(MetaRewardGrant grant)
        {
            if (grant.IsEmpty)
            {
                return;
            }

            var parts = string.Empty;

            if (grant.SkillPoints > 0)
            {
                parts += "+" + grant.SkillPoints + LocalizationService.Resolve(" Skill Points", " Fertigkeitspunkte");
            }

            if (grant.Alloy > 0)
            {
                parts += (parts.Length > 0 ? "  ·  " : string.Empty) + "+" + grant.Alloy + LocalizationService.Resolve(" Alloy", " Legierung");
            }

            if (grant.IsFirstKill)
            {
                parts += LocalizationService.Resolve("  (first kill)", "  (Erstbesiegung)");
            }

            NotificationPresenter.Show(parts);
        }

        private void EnsureCoreTarget()
        {
            if (_coreTarget == null)
            {
                var coreGameObject = new GameObject(CombatHitReceiver.CoreAnchorObjectName);
                coreGameObject.transform.position = Vector3.zero;
                _coreTarget = coreGameObject.transform;
            }

            var coreHitReceiver = _coreTarget.GetComponent<CombatHitReceiver>();

            if (coreHitReceiver == null)
            {
                coreHitReceiver = _coreTarget.gameObject.AddComponent<CombatHitReceiver>();
            }

            coreHitReceiver.InitializeRuntime(_coreTargetData, _coreHitReactionData, null, 1f, true, Color.white, true);
            coreHitReceiver.SetIncomingDamageMultiplierSource(_cardService);

            if (_runPersistenceController != null)
            {
                _runPersistenceController.SetCoreHitReceiver(coreHitReceiver);
            }

            var coreGuardController = GetComponent<CoreGuardController>();

            if (coreGuardController == null)
            {
                coreGuardController = gameObject.AddComponent<CoreGuardController>();
            }

            coreGuardController.Configure(_playerTarget, coreHitReceiver, InputReader);

            var coreController = _coreTarget.GetComponent<CoreController>();

            if (coreController == null)
            {
                coreController = _coreTarget.gameObject.AddComponent<CoreController>();
            }

            coreController.Configure(_coreData, coreHitReceiver);
        }

        private void RestoreCoreHealthFromSave(RunSaveData runSaveData)
        {
            if (_coreTarget == null)
            {
                return;
            }

            var coreHitReceiver = _coreTarget.GetComponent<CombatHitReceiver>();

            if (coreHitReceiver == null)
            {
                return;
            }

            if (runSaveData != null && runSaveData.HasActiveRun)
            {
                coreHitReceiver.RestoreHealthNormalized(runSaveData.CoreHealthNormalized);
                return;
            }

            coreHitReceiver.RestoreHealthNormalized(1f);
        }

        private void EnsureEnemyParent()
        {
            if (_enemyParent != null)
            {
                return;
            }

            var enemyParentObject = new GameObject("EnemyRuntimeRoot");
            enemyParentObject.transform.SetParent(transform, false);
            _enemyParent = enemyParentObject.transform;
        }

        private void EnsurePlayerStagePosition()
        {
            if (_playerTarget == null || _stageData == null)
            {
                return;
            }

            _playerTarget.position = _stageData.PlayerStartPosition;
        }

        private void MovePlayerToPostRewardPosition()
        {
            if (_playerTarget == null || _stageData == null)
            {
                return;
            }

            _playerTarget.position = _stageData.PostRewardPlayerPosition;
        }

        private void ApplyPersistenceContext()
        {
            if (_runPersistenceController == null)
            {
                return;
            }

            var waveNumber = _state == WaveState.BossCombat ? TotalWaveCount + 1 : CurrentWaveNumber;
            var bossPhaseIndex = _bossRuntimeController != null ? _bossRuntimeController.CurrentPhaseIndex : 0;
            _runPersistenceController.ApplyWaveContext(StageIndex, waveNumber, _state == WaveState.BossCombat, bossPhaseIndex);

            // Pushed alongside the wave context so every checkpoint that writes the run also
            // carries its clocks; without this a resumed run restarts from zero.
            if (_runTimer != null)
            {
                _runPersistenceController.ApplyRunClocks(
                    _runTimer.ElapsedActiveSeconds, _runTimer.ElapsedRealSeconds, _runTimer.StartedUtc.Ticks);
            }
        }

        private void RaiseStageStartedIfNeeded(string detailLabel)
        {
            if (_hasRaisedStageStarted)
            {
                return;
            }

            _hasRaisedStageStarted = true;
            StageEvents.RaiseStageStarted(CreateStageEventArgs(detailLabel));
        }

        private bool ShouldEndRunOnCompletion()
        {
            return _stageData == null || _stageData.IsFinalStage;
        }

        private WaveProgressEventArgs CreateEventArgs(int waveNumber, string label, bool isEliteEncounter)
        {
            return new WaveProgressEventArgs(StageIndex, waveNumber, label, isEliteEncounter);
        }

        private StageEventArgs CreateStageEventArgs(string detailLabel)
        {
            return new StageEventArgs(
                StageIndex,
                StageId,
                StageLabel,
                detailLabel,
                ShouldEndRunOnCompletion());
        }

        private BossEventArgs CreateBossEventArgs()
        {
            var bossData = _stageData != null ? _stageData.BossData : null;
            var bossId = bossData != null ? bossData.BossId : string.Empty;
            var bossName = BossLabel;
            if (string.IsNullOrEmpty(bossName) && bossData != null)
            {
                bossName = bossData.DisplayName;
            }

            var phaseIndex = _bossRuntimeController != null ? _bossRuntimeController.CurrentPhaseIndex : 0;
            var phaseLabel = BossPhaseLabel;
            var healthNormalized = BossHealthNormalized;

            return new BossEventArgs(
                StageIndex,
                TotalWaveCount + 1,
                bossId,
                bossName,
                phaseIndex,
                phaseLabel,
                healthNormalized
            );
        }

        private sealed class SpawnGroupRuntime
        {
            private int _remainingCount;
            private float _timer;

            private readonly WaveModifierData _modifierData;

            public SpawnGroupRuntime(SpawnGroupData spawnGroupData, WaveModifierData modifierData, float countVariance)
            {
                Source = spawnGroupData;
                _modifierData = modifierData;
                _remainingCount = ResolveStartingCount(spawnGroupData, countVariance);
                _timer = spawnGroupData != null ? spawnGroupData.InitialDelaySeconds * ResolveInitialDelayMultiplier(modifierData) : 0f;
            }

            // Varies the authored count symmetrically, so the expected total for a wave is exactly
            // what the data authored - this changes the shape of an encounter, not its difficulty
            // budget. A group that authored any enemies at all always spawns at least one, so
            // variance can never delete a threat type the wave was designed around.
            private static int ResolveStartingCount(SpawnGroupData spawnGroupData, float countVariance)
            {
                var authoredCount = spawnGroupData != null ? spawnGroupData.Count : 0;

                if (authoredCount <= 0 || countVariance <= 0f)
                {
                    return Mathf.Max(0, authoredCount);
                }

                var spread = authoredCount * countVariance;
                return Mathf.Max(1, Mathf.RoundToInt(authoredCount + UnityEngine.Random.Range(-spread, spread)));
            }

            public SpawnGroupData Source { get; }

            public EnemyData EnemyData => Source != null ? Source.EnemyData : null;

            public EnemyEliteModifierData EliteModifierData => Source != null ? Source.EliteModifierData : null;

            public bool HasRemaining => _remainingCount > 0;

            public int RemainingCount => _remainingCount;

            public bool ShouldSpawn => _remainingCount > 0 && _timer <= 0f;

            public void Tick(float deltaTime)
            {
                _timer -= deltaTime;
            }

            public void ConsumeSpawn()
            {
                _remainingCount = Mathf.Max(0, _remainingCount - 1);

                if (_remainingCount > 0)
                {
                    _timer += Source != null ? Source.SpawnIntervalSeconds * ResolveSpawnIntervalMultiplier(_modifierData) : 0.35f;
                }
            }

            private static float ResolveInitialDelayMultiplier(WaveModifierData modifierData)
            {
                return modifierData != null ? modifierData.InitialDelayMultiplier : 1f;
            }

            private static float ResolveSpawnIntervalMultiplier(WaveModifierData modifierData)
            {
                return modifierData != null ? modifierData.SpawnIntervalMultiplier : 1f;
            }
        }
    }
}
