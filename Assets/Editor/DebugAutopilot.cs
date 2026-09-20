using System.Collections.Generic;
using System.Text;
using Corebound.Gameplay.Combat;
using Corebound.Gameplay.Enemies;
using Corebound.Gameplay.Shared;
using Corebound.Gameplay.Waves;
using Corebound.Input;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Corebound.EditorTools
{
    // Plays a real run without a human at the keyboard.
    //
    // Synthetic OS input (SendInput/keybd_event) never reaches Unity's Input System, and
    // InputTestFixture needs its own assembly this asmdef-less project cannot provide. The way
    // through is to write the same state the InputAction callbacks write, on the one
    // CoreboundInputReader instance every system is already wired to and subscribed on. So this is
    // not a simulation of the game: it is the game, driven from the same seam a controller drives.
    //
    // Editor-only, started explicitly from the Corebound menu, and it stops itself at the step cap
    // so a forgotten autopilot cannot spin forever in the background.
    [InitializeOnLoad]
    public static class DebugAutopilot
    {
        private const string ActiveKey = "Corebound.DebugAutopilot.Active";
        private const string StageKey = "Corebound.DebugAutopilot.Stage";
        private const string LogPrefix = "[Autopilot]";

        // Enough for several waves at the pace a real player clears them; the run is aborted rather
        // than left running if something stalls.
        private const float MaxRunSeconds = 420f;

        // Agents cannot reliably land a click on the Unity menu bar (Windows foreground lock, and
        // menu coordinates move). EditorApplication.update runs in Edit Mode without focus, so a
        // request file is a dependable trigger: write the target stage index into
        // Temp/autopilot_request.txt and the Editor picks it up within a second. Temp/ is outside
        // Assets/, so writing it causes no import churn.
        private const string RequestFilePath = "Temp/autopilot_request.txt";
        private static double _nextRequestPoll;

        static DebugAutopilot()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.update += PollRequestFile;
        }

        private static void PollRequestFile()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.timeSinceStartup < _nextRequestPoll)
            {
                return;
            }

            _nextRequestPoll = EditorApplication.timeSinceStartup + 1.0;

            if (!System.IO.File.Exists(RequestFilePath))
            {
                return;
            }

            var requestedStage = 1;
            var contents = System.IO.File.ReadAllText(RequestFilePath).Trim();
            System.IO.File.Delete(RequestFilePath);

            if (int.TryParse(contents, out var parsedStage))
            {
                requestedStage = parsedStage;
            }

            Debug.Log(LogPrefix + " request file picked up, stage " + requestedStage);
            StartAutopilot(requestedStage);
        }

        [MenuItem("Corebound/Autopilot/Play Stage 1", priority = 200)]
        private static void PlayStage1()
        {
            StartAutopilot(1);
        }

        [MenuItem("Corebound/Autopilot/Play Stage 2", priority = 201)]
        private static void PlayStage2()
        {
            StartAutopilot(2);
        }

        [MenuItem("Corebound/Autopilot/Play Stage 3", priority = 202)]
        private static void PlayStage3()
        {
            StartAutopilot(3);
        }

        [MenuItem("Corebound/Autopilot/Stop", priority = 220)]
        private static void StopAutopilot()
        {
            SessionState.SetBool(ActiveKey, false);

            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
            }
        }

        private static void StartAutopilot(int stageIndex)
        {
            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
                Debug.LogWarning(LogPrefix + " Editor was already playing. Stopped it; run the menu item again.");
                return;
            }

            SessionState.SetBool(ActiveKey, true);
            SessionState.SetInt(StageKey, stageIndex);

            // Always from MainMenu: opening SampleScene directly pre-loads the additive canvas
            // scenes and hides a whole class of binding bug (see CLAUDE.md).
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
            EditorApplication.isPlaying = true;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange change)
        {
            if (change != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(ActiveKey, false))
            {
                return;
            }

            var host = new GameObject("DebugAutopilotRuntime");
            Object.DontDestroyOnLoad(host);
            var runtime = host.AddComponent<DebugAutopilotRuntime>();
            runtime.Configure(SessionState.GetInt(StageKey, 1), MaxRunSeconds, LogPrefix);
        }
    }

    public sealed class DebugAutopilotRuntime : MonoBehaviour
    {
        private enum Step
        {
            WaitForMainMenu,
            SelectStage,
            ConfirmNewRun,
            WaitForCombat,
            PlayCombat,
            Done
        }

        private int _targetStageIndex = 1;
        private float _maxRunSeconds = 420f;
        private string _logPrefix = "[Autopilot]";

        private Step _step = Step.WaitForMainMenu;
        private float _elapsed;
        private float _stepTimer;
        private float _reportTimer;
        private int _stageSelectPresses;
        private int _routePanelConfirms;
        private float _strafeSign = 1f;
        private float _strafeFlipTimer;
        private const float PreferredCombatDistance = 5.5f;

        private CoreboundInputReader _inputReader;
        private WaveManager _waveManager;
        private PlayerCombatController _playerCombat;
        private Transform _playerTransform;

        private string _lastReport = string.Empty;
        private readonly StringBuilder _reportBuilder = new StringBuilder(256);
        private readonly List<EnemyRuntimeController> _enemyBuffer = new List<EnemyRuntimeController>();

        public void Configure(int targetStageIndex, float maxRunSeconds, string logPrefix)
        {
            _targetStageIndex = Mathf.Max(1, targetStageIndex);
            _maxRunSeconds = maxRunSeconds;
            _logPrefix = logPrefix;
            Debug.Log(_logPrefix + " started, target stage " + _targetStageIndex);
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;
            _stepTimer += Time.deltaTime;

            if (_elapsed > _maxRunSeconds)
            {
                Debug.LogWarning(_logPrefix + " hit the time cap at step " + _step + ". Stopping.");
                Finish();
                return;
            }

            switch (_step)
            {
                case Step.WaitForMainMenu:
                    TickWaitForMainMenu();
                    break;
                case Step.SelectStage:
                    TickSelectStage();
                    break;
                case Step.ConfirmNewRun:
                    TickConfirmNewRun();
                    break;
                case Step.WaitForCombat:
                    TickWaitForCombat();
                    break;
                case Step.PlayCombat:
                    TickPlayCombat();
                    break;
            }
        }

        private void TickWaitForMainMenu()
        {
            _inputReader = FindFirstObjectByType<CoreboundInputReader>();

            if (_inputReader == null || _stepTimer < 1.5f)
            {
                return;
            }

            Advance(Step.SelectStage);
        }

        // The debug stage-select is a static counter driven by F9/F10 in the Main Menu. Calling its
        // own API is more reliable than trying to land a keypress, and it is the same state the
        // key would change.
        private void TickSelectStage()
        {
            if (_stageSelectPresses == 0)
            {
                Corebound.UI.MainMenu.MainMenuRouteTargetController.DebugSelectStartStageIndex(_targetStageIndex);
                _stageSelectPresses = 1;
                Debug.Log(_logPrefix + " stage select set to "
                    + Corebound.UI.MainMenu.MainMenuRouteTargetController.DebugStartStageIndex);
            }

            Advance(Step.ConfirmNewRun);
        }

        // Blind navigate-and-confirm is dangerous here: the entry list is dynamic (Continue only
        // appears when a run exists) and one of the entries is Quit, which would end Play Mode.
        // Select the New Run entry by route, then confirm; the route panel that opens needs a
        // second confirm to actually start the run.
        private void TickConfirmNewRun()
        {
            if (_stepTimer < 0.5f)
            {
                return;
            }

            var menuController = FindFirstObjectByType<Corebound.UI.MainMenu.MainMenuController>();

            if (menuController == null)
            {
                Debug.LogWarning(_logPrefix + " no MainMenuController found");
                Finish();
                return;
            }

            if (menuController.IsIntroVisible)
            {
                menuController.AcknowledgeIntro();
                Debug.Log(_logPrefix + " acknowledged intro");
                _stepTimer = 0f;
                return;
            }

            var entries = menuController.VisibleEntries;
            var newRunIndex = -1;

            for (var index = 0; index < entries.Count; index++)
            {
                if (entries[index] != null && entries[index].Route == Corebound.UI.MainMenu.MainMenuRoute.NewRun)
                {
                    newRunIndex = index;
                    break;
                }
            }

            if (newRunIndex < 0)
            {
                Debug.LogWarning(_logPrefix + " no New Run entry among " + entries.Count + " entries");
                Finish();
                return;
            }

            menuController.SelectIndex(newRunIndex);
            _inputReader.DebugRaiseConfirm();
            Debug.Log(_logPrefix + " confirmed New Run at index " + newRunIndex);
            Advance(Step.WaitForCombat);
        }

        private void TickWaitForCombat()
        {
            _waveManager = FindFirstObjectByType<WaveManager>();

            if (_waveManager == null)
            {
                // Confirming New Run opens a route panel that needs its own confirm. Pulse Confirm
                // about once a second until the gameplay scene actually arrives.
                if (_stepTimer > 1f)
                {
                    _inputReader.DebugRaiseConfirm();
                    _stepTimer = 0f;
                    _routePanelConfirms++;

                    if (_routePanelConfirms > 10)
                    {
                        Debug.LogWarning(_logPrefix + " route panel never started a run");
                        Finish();
                    }
                }

                return;
            }

            _playerCombat = FindFirstObjectByType<PlayerCombatController>();
            _playerTransform = _playerCombat != null ? _playerCombat.transform : null;
            RebindInputReaderToPlayerScene();
            ReportOnce("combat scene loaded");
            Advance(Step.PlayCombat);
        }

        private void TickPlayCombat()
        {
            if (_waveManager == null)
            {
                Debug.LogWarning(_logPrefix + " lost the WaveManager. Stopping.");
                Finish();
                return;
            }

            _reportTimer += Time.deltaTime;

            if (_reportTimer >= 2f)
            {
                _reportTimer = 0f;
                ReportState();
            }

            switch (_waveManager.State)
            {
                case WaveState.Combat:
                case WaveState.BossCombat:
                    DriveCombat();
                    return;
                case WaveState.Intermission:
                    DriveIntermission();
                    return;
                case WaveState.Failed:
                    Debug.LogWarning(_logPrefix + " run failed at wave " + _waveManager.CurrentWaveNumber);
                    Finish();
                    return;
                case WaveState.Completed:
                    Debug.Log(_logPrefix + " run completed");
                    Finish();
                    return;
                default:
                    DriveIntermission();
                    return;
            }
        }

        // Walks at the nearest enemy and holds fire. Crude on purpose: the point is to exercise
        // spawning, damage, death, reward and wave transitions, not to play well.
        // MainMenu.unity and SampleScene.unity each carry their own CoreboundInputReader, and both
        // are loaded at once. FindFirstObjectByType can return either, so the menu's reader was
        // being driven while the gameplay systems listened to the scene's own - the player stood
        // still and never fired. Bind to the one that lives in the same scene as the player.
        private void RebindInputReaderToPlayerScene()
        {
            if (_playerTransform == null)
            {
                return;
            }

            var readers = FindObjectsByType<CoreboundInputReader>(FindObjectsSortMode.None);
            var playerScene = _playerTransform.gameObject.scene;

            for (var index = 0; index < readers.Length; index++)
            {
                if (readers[index] != null && readers[index].gameObject.scene == playerScene)
                {
                    if (_inputReader != readers[index])
                    {
                        Debug.Log(_logPrefix + " rebound input reader to the one in " + playerScene.name);
                    }

                    _inputReader = readers[index];
                    return;
                }
            }

            Debug.LogWarning(_logPrefix + " found no input reader in the player's scene (" + readers.Length + " total)");
        }

        private void DriveCombat()
        {
            if (_playerTransform == null)
            {
                _playerCombat = FindFirstObjectByType<PlayerCombatController>();
                _playerTransform = _playerCombat != null ? _playerCombat.transform : null;
                RebindInputReaderToPlayerScene();
            }

            if (_inputReader == null || _playerTransform == null)
            {
                return;
            }

            var target = FindNearestEnemy();

            if (target == null)
            {
                _inputReader.DebugSetMove(Vector2.zero);
                _inputReader.DebugSetPrimaryFireHeld(false);
                return;
            }

            var playerPosition = (Vector2)_playerTransform.position;
            var offset = (Vector2)target.position - playerPosition;
            var distance = offset.magnitude;
            var direction = distance > 0.01f ? offset / distance : Vector2.right;

            // Must be set every frame: the real device callbacks flip this back the moment any
            // stray OS input arrives.
            _inputReader.DebugSetUsingGamepad(true);
            _inputReader.DebugSetAim(direction);
            _inputReader.DebugSetPrimaryFireHeld(true);

            // Kite rather than charge. Walking into contact damage killed the harness on wave 1
            // every time, which ends the test for a reason that is not what is being tested.
            // Three components: hold a working distance, circle so the chasers never line up a
            // straight run, and bias back toward the middle so it cannot be cornered against a wall.
            var radial = (distance - PreferredCombatDistance) * 0.6f;
            var tangent = new Vector2(-direction.y, direction.x) * _strafeSign;
            var move = direction * Mathf.Clamp(radial, -1f, 1f) + tangent * 0.85f;

            var arena = FindFirstObjectByType<StageArenaArtPresenter>();

            if (arena != null)
            {
                var halfExtents = arena.ArenaSize * 0.5f - new Vector2(2f, 2f);

                if (Mathf.Abs(playerPosition.x) > halfExtents.x || Mathf.Abs(playerPosition.y) > halfExtents.y)
                {
                    move += -playerPosition.normalized * 1.4f;
                    _strafeFlipTimer = 0f;
                }
            }

            // Reversing the strafe direction periodically stops it orbiting into the same corner.
            _strafeFlipTimer += Time.deltaTime;

            if (_strafeFlipTimer > 2.5f)
            {
                _strafeFlipTimer = 0f;
                _strafeSign = -_strafeSign;
            }

            _inputReader.DebugSetMove(move.sqrMagnitude > 1f ? move.normalized : move);
        }

        private void DriveIntermission()
        {
            _inputReader.DebugSetPrimaryFireHeld(false);
            _inputReader.DebugSetMove(Vector2.zero);
            _stepTimer += 0f;

            // Confirm roughly once a second so shop/reward/intermission gates all clear, without
            // machine-gunning Confirm into a screen that has only just opened.
            if (_reportTimer < 1f)
            {
                return;
            }

            _inputReader.DebugRaiseConfirm();
        }

        private Transform FindNearestEnemy()
        {
            _enemyBuffer.Clear();
            _enemyBuffer.AddRange(FindObjectsByType<EnemyRuntimeController>(FindObjectsSortMode.None));

            Transform nearest = null;
            var nearestDistanceSquared = float.MaxValue;
            var origin = (Vector2)_playerTransform.position;

            for (var index = 0; index < _enemyBuffer.Count; index++)
            {
                var enemy = _enemyBuffer[index];

                if (enemy == null || !enemy.isActiveAndEnabled || !enemy.IsAlive)
                {
                    continue;
                }

                var distanceSquared = ((Vector2)enemy.transform.position - origin).sqrMagnitude;

                if (distanceSquared >= nearestDistanceSquared)
                {
                    continue;
                }

                nearestDistanceSquared = distanceSquared;
                nearest = enemy.transform;
            }

            return nearest;
        }

        // The diagnostic that matters: every number the playtest report questioned, once every two
        // seconds, deduplicated so a stalled run prints one line instead of a wall.
        private void ReportState()
        {
            _reportBuilder.Length = 0;
            _reportBuilder.Append("stage=").Append(_waveManager.StageDataAsset != null ? _waveManager.StageDataAsset.name : "null");
            _reportBuilder.Append(" state=").Append(_waveManager.State);
            _reportBuilder.Append(" wave=").Append(_waveManager.CurrentWaveNumber).Append('/').Append(_waveManager.TotalWaveCount);
            _reportBuilder.Append(" alive=").Append(_waveManager.RemainingAliveEnemies);
            _reportBuilder.Append(" enemiesInScene=").Append(CountLiveEnemies());

            var arena = FindFirstObjectByType<StageArenaArtPresenter>();
            _reportBuilder.Append(" arenaSize=").Append(arena != null ? arena.ArenaSize.ToString("0.##") : "null");

            if (_playerTransform == null)
            {
                _playerCombat = FindFirstObjectByType<PlayerCombatController>();
                _playerTransform = _playerCombat != null ? _playerCombat.transform : null;
            }

            _reportBuilder.Append(" player=").Append(_playerTransform != null ? ((Vector2)_playerTransform.position).ToString("0.#") : "NULL");
            _reportBuilder.Append(" weapon=").Append(_playerCombat != null ? _playerCombat.ActiveWeaponId : "NULL");
            _reportBuilder.Append(" firing=").Append(_inputReader != null && _inputReader.IsPrimaryFireHeld);
            AppendEnemyPositions(_reportBuilder);

            AppendHudState(_reportBuilder);

            var report = _reportBuilder.ToString();

            if (report == _lastReport)
            {
                return;
            }

            _lastReport = report;
            Debug.Log(_logPrefix + " " + report);
        }

        private void ReportOnce(string message)
        {
            Debug.Log(_logPrefix + " " + message);
            _lastReport = string.Empty;
            ReportState();
        }

        // Prints every live enemy's position and whether it sits inside the arena bounds. An enemy
        // parked outside the boundary can never be reached or killed, which would hang the wave
        // forever on RemainingAliveEnemies.
        private void AppendEnemyPositions(StringBuilder builder)
        {
            var enemies = FindObjectsByType<EnemyRuntimeController>(FindObjectsSortMode.None);
            var arena = FindFirstObjectByType<StageArenaArtPresenter>();
            var halfExtents = arena != null ? arena.ArenaSize * 0.5f : new Vector2(8f, 5f);

            builder.Append(" enemies[");

            for (var index = 0; index < enemies.Length; index++)
            {
                var enemy = enemies[index];

                if (enemy == null || !enemy.isActiveAndEnabled || !enemy.IsAlive)
                {
                    continue;
                }

                var position = (Vector2)enemy.transform.position;
                var isInside = Mathf.Abs(position.x) <= halfExtents.x && Mathf.Abs(position.y) <= halfExtents.y;
                builder.Append(enemy.name).Append('@').Append(position.ToString("0.#"));
                builder.Append(isInside ? " IN" : " OUTSIDE");

                if (_playerTransform != null)
                {
                    builder.Append(" d=").Append(Vector2.Distance(position, _playerTransform.position).ToString("0.#"));
                }

                builder.Append("; ");
            }

            builder.Append(']');
        }

        private int CountLiveEnemies()
        {
            var enemies = FindObjectsByType<EnemyRuntimeController>(FindObjectsSortMode.None);
            var count = 0;

            for (var index = 0; index < enemies.Length; index++)
            {
                if (enemies[index] != null && enemies[index].isActiveAndEnabled && enemies[index].IsAlive)
                {
                    count++;
                }
            }

            return count;
        }

        // Reads the authored HUD texts straight out of the loaded HUDCanvas, which is the only way
        // to tell "the presenter never bound" apart from "the presenter bound and wrote nothing".
        private void AppendHudState(StringBuilder builder)
        {
            var hudCanvas = GameObject.Find("HUDCanvas");
            var hudRoot = hudCanvas != null ? hudCanvas.transform.Find("HUD") : null;

            if (hudRoot == null)
            {
                builder.Append(" hud=NOT_FOUND");
                return;
            }

            builder.Append(" hud[");
            AppendHudLabel(builder, hudRoot, "PlayerHealth");
            AppendHudLabel(builder, hudRoot, "CoreHealth");
            AppendHudLabel(builder, hudRoot, "Currency");
            AppendHudLabel(builder, hudRoot, "Wave");
            builder.Append(']');
        }

        private static void AppendHudLabel(StringBuilder builder, Transform hudRoot, string childName)
        {
            var child = hudRoot.Find(childName);
            var text = child != null ? child.GetComponent<Text>() : null;

            builder.Append(childName).Append('=');

            if (child == null)
            {
                builder.Append("MISSING ");
                return;
            }

            if (text == null)
            {
                builder.Append("NO_TEXT ");
                return;
            }

            builder.Append('"').Append(text.text).Append("\" active=").Append(child.gameObject.activeInHierarchy).Append(' ');
        }

        private void Advance(Step next)
        {
            _step = next;
            _stepTimer = 0f;
        }

        private void Finish()
        {
            _step = Step.Done;
            Debug.Log(_logPrefix + " finished after " + _elapsed.ToString("0.0") + "s");
            EditorApplication.isPlaying = false;
        }
    }
}
