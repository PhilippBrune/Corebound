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
        // appear on the exact same tile" problem: ResolveSpawnPosition() used to be handed the
        // spawn *group* index, so a group of six enemies produced six identical positions and a
        // whole wave only ever used as many distinct points as it had groups.
        private int _waveSpawnCounter;
        private int _waveSpawnPointOffset;
        private float _waveSpawnAngleOffsetDegrees;

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
        private const float WaveStartSpawnDelaySeconds = 2f;

        private WaveState _state;
        private int _currentWaveIndex;
        private int _remainingAliveEnemies;
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
            ? LocalizationService.Resolve("Core Destroyed", "Core zerstört")
            : LocalizationService.Resolve("Player Died", "Spieler gestorben");

        public StageData StageDataAsset => _stageData;

        public int StageIndex => _stageData != null ? _stageData.StageIndex : 1;

        public string StageId => _stageData != null ? _stageData.StageId : string.Empty;

        public string StageLabel => _stageData != null ? _stageData.DisplayName : string.Empty;

        public string BossLabel => _bossRuntimeController != null ? _bossRuntimeController.BossName : string.Empty;

        public string BossPhaseLabel => _bossRuntimeController != null ? _bossRuntimeController.CurrentPhaseLabel : string.Empty;

        public float BossHealthNormalized => _bossRuntimeController != null ? _bossRuntimeController.CurrentHealthNormalized : 1f;

        private ISaveManager SaveManager => SaveManagerResolver.Resolve(_saveManagerSource);

        private IInputReader InputReader => _inputReaderSource as IInputReader;

        private void Awake()
        {
            // Static registry, so a Transform from the previous scene would otherwise survive
            // a scene load and buff the next run from a destroyed object.
            EnemySupportAuraRegistry.Clear();

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
        }

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

            InitializeFromSave();
        }

        private void OnDisable()
        {
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

                // StageArenaArtPresenter.Awake() (BuildArenaVisuals) runs before WaveManager's own
                // OnEnable() (this method) in Unity's activation order - by the time _stageData is
                // reassigned here, the arena has already built with the scene's default Stage 1
                // theme. RebuildForNewStage() re-reads WaveManager.StageDataAsset, which is now the
                // resolved stage, so this brings the visuals in line with the save immediately
                // instead of showing Stage 1's arena under a Stage 2+ HUD until the next transition.
                FindFirstObjectByType<StageArenaArtPresenter>()?.RebuildForNewStage();

                // Tower slots need the same correction on this path. Now that a stage's slot
                // layout comes from its StageArenaLayoutData BuildableZones, resuming a Stage 3
                // save would otherwise place Stage 1's slots on Stage 3's arena. Unity runs every
                // Awake before any OnEnable, so TowerService has already built its initial slots
                // by the time this runs on the same GameObject.
                _towerService?.RebuildSlotsForNewStage();
            }

            if (_stageData == null || _stageData.Waves == null || _stageData.Waves.Length == 0)
            {
                _state = WaveState.Completed;
                StateChanged?.Invoke();
                return;
            }

            RestoreCoreHealthFromSave(runSaveData);
            _currentWaveIndex = runSaveData != null ? Mathf.Max(0, runSaveData.WaveIndex - 1) : 0;
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
            _waveSpawnCounter = 0;
            _waveSpawnPointOffset = UnityEngine.Random.Range(0, 1024);
            _waveSpawnAngleOffsetDegrees = UnityEngine.Random.Range(0f, 360f);

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

            ApplyPersistenceContext();
            WaveEvents.RaiseWaveStarted(CreateEventArgs(CurrentWaveNumber, WaveEncounterLabelResolver.ResolveCurrentEncounterLabel(_stageData, _currentWaveIndex, _state, _bossRuntimeController), WaveEncounterLabelResolver.ResolveCurrentEncounterIsElite(_stageData, _currentWaveIndex, _state)));
            StateChanged?.Invoke();
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
            _state = WaveState.BossCombat;

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
            _bossRuntimeController.transform.position = _stageData.BossSpawnPosition;
            _bossRuntimeController.Initialize(_stageData.BossData, _playerTarget, _coreTarget, _sharedProjectilePrototype, _projectileParent, _enemyPool, restoredPhaseIndex);
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

                // A running per-wave counter, NOT the spawn-group index: passing the group index
                // meant every enemy in a group resolved to the identical position.
                enemy.transform.position = ResolveSpawnPosition(_waveSpawnCounter++, false);
                enemy.Initialize(spawnGroup.EnemyData, spawnGroup.EliteModifierData, _playerTarget, _coreTarget, _sharedProjectilePrototype, _projectileParent);
                _remainingAliveEnemies += 1;
                spawnGroup.ConsumeSpawn();
            }

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
            _resourcePickupPool?.ForceCollectAll();
            _cardService?.NotifyWaveCleared();
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
            _state = WaveState.Reward;
            _completeStageAfterReward = true;
            _resourcePickupPool?.ForceCollectAll();

            if (_runPersistenceController != null)
            {
                _runPersistenceController.WriteBossCompletionSnapshot(_bossRuntimeController != null ? _bossRuntimeController.BossRewardData : null);
            }

            BossEvents.RaiseBossDefeated(CreateBossEventArgs());
            GrantBossDefeatRelic();
            WaveEvents.RaiseRewardPending(CreateEventArgs(CurrentWaveNumber, WaveEncounterLabelResolver.ResolveCurrentEncounterLabel(_stageData, _currentWaveIndex, _state, _bossRuntimeController), false));
            StateChanged?.Invoke();
        }

        private void GrantBossDefeatRelic()
        {
            // CardService.TryGrantRandomRelic() raises RelicEvents.RelicGranted itself, which
            // RelicRevealPresenter listens for - no toast call needed here anymore.
            _cardService?.TryGrantRandomRelic();
        }

        private void StartIntermission()
        {
            _state = WaveState.Intermission;
            _intermissionTimer = _stageData != null ? _stageData.IntermissionDurationSeconds : 20f;
            _requiresIntermissionShopAction = _shopService != null && _shopService.TryOpenForIntermission(_stageData, CurrentWaveNumber);
            _requiresIntermissionTowerAction = !_requiresIntermissionShopAction && _towerService != null && _towerService.HasAvailableIntermissionAction;
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

            if (ShouldEndRunOnCompletion())
            {
                RecordRunOutcomeStatistic(true);
                GrantMetaCurrencyReward(_metaProgressionDefinitionData != null ? _metaProgressionDefinitionData.RunCompletionCurrencyReward : 0);

                if (SaveManager != null)
                {
                    SaveManager.ClearRunSaveData();
                }

                StateChanged?.Invoke();
                return;
            }

            AdvanceToNextStage();
        }

        // Known limitation (2026-09-09): a fresh app launch resumed mid-Stage-2+ falls back to
        // whatever StageData is serialized on this component in the scene (always Stage 1) - the
        // save system persists StageIndex as an int (RunPersistenceController.ApplyWaveContext,
        // called below via ApplyPersistenceContext/StartCurrentWave) but InitializeFromSave() has
        // no stage-asset lookup-by-index to resolve which StageData that number refers to. Staying
        // in the current session across the transition works correctly; quitting and relaunching
        // mid-Stage-2 does not yet. Adding that lookup needs a stage registry this project doesn't
        // have yet, so it's deliberately left as a known gap rather than guessed at blind.
        private void AdvanceToNextStage()
        {
            var nextStageData = _stageData != null ? _stageData.NextStageData : null;

            if (nextStageData == null)
            {
                StateChanged?.Invoke();
                return;
            }

            _stageData = nextStageData;
            _currentWaveIndex = 0;
            _remainingAliveEnemies = 0;
            _spawnGroups.Clear();
            _intermissionTimer = 0f;
            _requiresIntermissionShopAction = false;
            _requiresIntermissionTowerAction = false;

            var arenaPresenter = FindFirstObjectByType<StageArenaArtPresenter>();
            arenaPresenter?.RebuildForNewStage();
            _towerService?.RebuildSlotsForNewStage();

            StartCurrentWave();
        }

        private void OnConfirmPressed()
        {
            // Playtest feedback (2026-09-17): "Intermission should be ended via btn press 'Enter'
            // even if nothing was built" - the mandatory build-a-tower gate (below,
            // _requiresIntermissionTowerAction) used to block Confirm entirely until the player
            // placed or upgraded something. Building a tower is now purely optional during
            // Intermission; only the Shop's own mandatory buy/skip gate still blocks Confirm.
            if (_state == WaveState.Intermission
                && !_requiresIntermissionShopAction
                && _intermissionGateResolvedFrame != Time.frameCount
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
                _state = WaveState.Failed;
                RecordRunOutcomeStatistic(false);
                GrantMetaCurrencyReward(_metaProgressionDefinitionData != null ? _metaProgressionDefinitionData.RunFailureCurrencyReward : 0);
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
                    SpawnResourcePickup(helperEnemy.ResourceRewardAmount, eventArgs.Victim.transform.position);
                    RecordEnemyKillStatistic();

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

            SpawnResourcePickup(enemy.ResourceRewardAmount, eventArgs.Victim.transform.position);
            RecordEnemyKillStatistic();

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

        private Vector3 ResolveSpawnPosition(int spawnIndex, bool isBossHelper)
        {
            // Boss helper adds are summoned from a single point near the boss encounter itself, not
            // from the arena's entry points - that stays a single origin regardless of how many
            // regular-wave spawn points the stage defines.
            if (isBossHelper)
            {
                var helperFanRadius = _stageData != null ? _stageData.BossHelperSpawnFanRadius : new Vector2(2.2f, 1.6f);
                var helperAngleStepDegrees = _stageData != null ? _stageData.SpawnFanAngleStepDegrees : 120f;
                var helperAngle = spawnIndex * helperAngleStepDegrees * Mathf.Deg2Rad;
                var helperOrigin = _stageData != null ? _stageData.EnemySpawnOrigin : new Vector2(0f, -4.5f);
                return new Vector3(helperOrigin.x + Mathf.Cos(helperAngle) * helperFanRadius.x, helperOrigin.y + Mathf.Sin(helperAngle) * helperFanRadius.y, 0f);
            }

            var spawnPoints = _stageData != null ? _stageData.SpawnPoints : null;
            Vector2 origin;
            Vector2 fanRadius;
            float angleStepDegrees;

            if (spawnPoints != null && spawnPoints.Length > 0)
            {
                // A stage with multiple authored spawn points round-robins enemies across them by
                // spawn index, so a burst of several enemies spreads across the stage's different
                // entry points instead of fanning out from one (Dynamic Arena Slice, Docs/Slices/
                // 15_DYNAMIC_ARENA_SLICE.md).
                // Offsetting the round-robin by a per-wave value (rather than picking a point at
                // random per enemy) keeps every authored entry point used an even number of times
                // within a wave - pure per-enemy randomness clumps, which reads as unfair rather
                // than varied - while still starting each wave on a different point.
                var wrappedIndex = (((spawnIndex + _waveSpawnPointOffset) % spawnPoints.Length) + spawnPoints.Length) % spawnPoints.Length;
                var spawnPoint = spawnPoints[wrappedIndex];
                origin = spawnPoint.Position;
                fanRadius = spawnPoint.FanRadius;
                angleStepDegrees = spawnPoint.FanAngleStepDegrees;
            }
            else
            {
                // No SpawnPoints authored for this stage - unchanged pre-Dynamic-Arena-Slice
                // behavior, a single origin fanned by spawn index.
                origin = _stageData != null ? _stageData.EnemySpawnOrigin : new Vector2(0f, -4.5f);
                fanRadius = _stageData != null ? _stageData.SpawnFanRadius : new Vector2(5.5f, 3.5f);
                angleStepDegrees = _stageData != null ? _stageData.SpawnFanAngleStepDegrees : 120f;
            }

            // Jitter within half a fan step either side, plus a per-wave rotation of the whole fan.
            // The enemy still arrives from this spawn point's arc - it just no longer lands on the
            // exact same handful of coordinates every single wave of every single run.
            var jitterDegrees = UnityEngine.Random.Range(-0.5f, 0.5f) * angleStepDegrees * _spawnAngleJitter;
            var angle = (spawnIndex * angleStepDegrees + _waveSpawnAngleOffsetDegrees + jitterDegrees) * Mathf.Deg2Rad;

            // Pull some spawns slightly inside the fan radius so they do not all sit on one ring.
            var radiusScale = UnityEngine.Random.Range(1f - 0.25f * _spawnAngleJitter, 1f);
            return new Vector3(origin.x + Mathf.Cos(angle) * fanRadius.x * radiusScale, origin.y + Mathf.Sin(angle) * fanRadius.y * radiusScale, 0f);
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

        private void RecordEnemyKillStatistic()
        {
            WaveStatisticsRecorder.RecordEnemyKill(SaveManager);
        }

        private void RecordRunOutcomeStatistic(bool completed)
        {
            WaveStatisticsRecorder.RecordRunOutcome(SaveManager, StageIndex, completed);
        }

        private void GrantMetaCurrencyReward(int amount)
        {
            WaveStatisticsRecorder.GrantMetaCurrencyReward(SaveManager, amount);
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
