using System.Text;
using Corebound.Gameplay.Cards;
using Corebound.Gameplay.Combat;
using Corebound.Gameplay.Enemies;
using Corebound.Gameplay.Shared;
using Corebound.Gameplay.Shop;
using Corebound.Gameplay.Towers;
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
    // Off by default, and the toggle lives in EditorPrefs rather than SessionState so it cannot
    // quietly turn itself back on after a domain reload.
    [InitializeOnLoad]
    public static class DebugAutopilot
    {
        public const string EnabledKey = "Corebound.DebugAutopilot.Enabled";
        public const string StageKey = "Corebound.DebugAutopilot.Stage";
        public const string MaxRunSecondsKey = "Corebound.DebugAutopilot.MaxRunSeconds";

        internal const string LogPrefix = "[Autopilot]";
        internal const string ActiveSessionKey = "Corebound.DebugAutopilot.ActiveSession";

        // Project root, beside Assets/ and Library/. Temp/ was the original home and was a bad
        // choice: Unity owns it, wipes it, and it is hidden from the Project window, so the one
        // file a human needs in order to drive this was the one file they could not find.
        public const string RequestFilePath = "AutopilotRequest.txt";

        // Bumped whenever the default needs to be re-asserted on machines where EditorPrefs
        // already holds a stale value. EditorPrefs persist per user per Unity version, so a single
        // "enable" written months ago would otherwise keep the autopilot on forever and make it
        // look like it defaults to on - which is exactly what happened.
        private const int SettingsVersion = 2;
        private const string SettingsVersionKey = "Corebound.DebugAutopilot.SettingsVersion";

        private static double _nextRequestPoll;

        public static bool Enabled
        {
            get => EditorPrefs.GetBool(EnabledKey, false);
            set => EditorPrefs.SetBool(EnabledKey, value);
        }

        public static int TargetStage
        {
            get => Mathf.Clamp(EditorPrefs.GetInt(StageKey, 1), 1, 5);
            set => EditorPrefs.SetInt(StageKey, Mathf.Clamp(value, 1, 5));
        }

        public static float MaxRunSeconds
        {
            get => Mathf.Clamp(EditorPrefs.GetFloat(MaxRunSecondsKey, 420f), 30f, 3600f);
            set => EditorPrefs.SetFloat(MaxRunSecondsKey, Mathf.Clamp(value, 30f, 3600f));
        }

        static DebugAutopilot()
        {
            ResetToDefaultsIfStale();
            EnsureRequestFileExists();
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.update += PollRequestFile;
        }

        private static void ResetToDefaultsIfStale()
        {
            if (EditorPrefs.GetInt(SettingsVersionKey, 0) == SettingsVersion)
            {
                return;
            }

            EditorPrefs.SetInt(SettingsVersionKey, SettingsVersion);
            EditorPrefs.SetBool(EnabledKey, false);
            Debug.Log(LogPrefix + " reset to its default: disabled. Enable it under Corebound > Autopilot, "
                + "or write 'enable' into " + RequestFilePath + ".");
        }

        // The file has to *exist* to be discoverable - a trigger you have to know about in order to
        // create is not a trigger, it is a secret. Previous versions deleted it on read, so it was
        // only ever on disk for the second between writing and consuming it, and the one person who
        // needed to find it never could. It is now created on load, left in place, and reset to
        // "command: none" after each command so it is always there to edit.
        private static void EnsureRequestFileExists()
        {
            if (System.IO.File.Exists(RequestFilePath))
            {
                return;
            }

            WriteIdleRequestFile();
        }

        private static void WriteIdleRequestFile()
        {
            try
            {
                System.IO.File.WriteAllText(RequestFilePath, IdleRequestFileContents);
            }
            catch (System.IO.IOException exception)
            {
                Debug.LogWarning(LogPrefix + " could not write " + RequestFilePath + ": " + exception.Message);
            }
        }

        private const string IdleRequestFileContents =
            "# Corebound Autopilot control file.\n"
            + "#\n"
            + "# Edit the command line below and save. The Editor picks it up within a second while\n"
            + "# it is NOT in Play Mode, runs the command, then resets this line to 'none'.\n"
            + "#\n"
            + "# Commands:\n"
            + "#   enable     turn the autopilot on (it is off by default)\n"
            + "#   disable    turn it off again\n"
            + "#   1..5       enable if needed, then play a run starting on that stage\n"
            + "#   none       do nothing\n"
            + "#\n"
            + "# The same controls live under Corebound > Autopilot in the menu bar.\n"
            + "# Progress is logged to the Console behind [Autopilot].\n"
            + "\n"
            + "command: none\n";

        [MenuItem("Corebound/Autopilot/Enabled", priority = 200)]
        private static void ToggleEnabled()
        {
            Enabled = !Enabled;
            Debug.Log(LogPrefix + (Enabled ? " enabled" : " disabled"));
        }

        [MenuItem("Corebound/Autopilot/Enabled", true)]
        private static bool ToggleEnabledValidate()
        {
            Menu.SetChecked("Corebound/Autopilot/Enabled", Enabled);
            return true;
        }

        [MenuItem("Corebound/Autopilot/Run Now", priority = 202)]
        private static void RunNow()
        {
            StartAutopilot(TargetStage);
        }

        [MenuItem("Corebound/Autopilot/Stop", priority = 203)]
        private static void StopAutopilot()
        {
            SessionState.SetBool(ActiveSessionKey, false);

            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
            }

            Debug.Log(LogPrefix + " stopped");
        }

        [MenuItem("Corebound/Autopilot/Settings...", priority = 220)]
        private static void OpenSettings()
        {
            DebugAutopilotWindow.Open();
        }

        internal static void StartAutopilot(int stageIndex)
        {
            if (!Enabled)
            {
                Debug.LogWarning(LogPrefix + " is disabled. Turn it on under Corebound > Autopilot > Enabled.");
                return;
            }

            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
                Debug.LogWarning(LogPrefix + " the Editor was already playing. Stopped it; start the autopilot again.");
                return;
            }

            SessionState.SetBool(ActiveSessionKey, true);
            TargetStage = stageIndex;

            // Always from MainMenu: opening SampleScene directly pre-loads the additive canvas
            // scenes and hides a whole class of binding bug (see CLAUDE.md).
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
            EditorApplication.isPlaying = true;
        }

        // EditorApplication.update runs in Edit Mode without focus, which makes a request file a
        // dependable trigger for an agent that cannot reliably click a menu (Windows foreground
        // lock, moving menu coordinates). Writing a stage index into AutopilotRequest.txt in the
        // project root starts a run within a second.
        private static void PollRequestFile()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.timeSinceStartup < _nextRequestPoll)
            {
                return;
            }

            _nextRequestPoll = EditorApplication.timeSinceStartup + 1.0;

            if (!System.IO.File.Exists(RequestFilePath))
            {
                WriteIdleRequestFile();
                return;
            }

            var contents = ReadCommand();

            if (string.IsNullOrEmpty(contents) || contents.Equals("none", System.StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            WriteIdleRequestFile();

            // "disable" / "enable" set the same toggle the menu item sets. The toggle exists so
            // the autopilot never fires unexpectedly, not to stop automation using it, so writing
            // the word is as explicit an opt-in as ticking the menu box - and it leaves a line in
            // the Console either way.
            if (contents.StartsWith("disable", System.StringComparison.OrdinalIgnoreCase))
            {
                Enabled = false;
                Debug.Log(LogPrefix + " disabled via " + RequestFilePath);
                return;
            }

            if (contents.StartsWith("enable", System.StringComparison.OrdinalIgnoreCase))
            {
                Enabled = true;
                contents = contents.Substring("enable".Length).Trim();
                Debug.Log(LogPrefix + " enabled via " + RequestFilePath);

                if (contents.Length == 0)
                {
                    return;
                }
            }

            if (!int.TryParse(contents, out var requestedStage))
            {
                Debug.LogWarning(LogPrefix + " did not understand '" + contents + "' in " + RequestFilePath
                    + ". Use enable, disable, a stage number 1-5, or none.");
                return;
            }

            // Writing a stage number is itself an explicit request to run, so it implies enable -
            // which is what the help text in the file promises. Requiring two edits to start one
            // run would make the control file more annoying than the menu it exists to bypass.
            if (!Enabled)
            {
                Enabled = true;
                Debug.Log(LogPrefix + " enabled by a stage request in " + RequestFilePath);
            }

            Debug.Log(LogPrefix + " request file picked up, stage " + requestedStage);
            StartAutopilot(requestedStage);
        }

        // Accepts either a bare command on its own line (so "1" in an otherwise empty file still
        // works) or the "command: x" form the generated file uses. Lines starting with # are help
        // text, not input.
        private static string ReadCommand()
        {
            string[] lines;

            try
            {
                lines = System.IO.File.ReadAllLines(RequestFilePath);
            }
            catch (System.IO.IOException)
            {
                // A save in progress; try again on the next poll rather than reporting an error.
                return null;
            }

            for (var index = 0; index < lines.Length; index++)
            {
                var line = lines[index].Trim();

                if (line.Length == 0 || line.StartsWith("#"))
                {
                    continue;
                }

                var colonIndex = line.IndexOf(':');

                if (colonIndex >= 0 && line.Substring(0, colonIndex).Trim().Equals("command", System.StringComparison.OrdinalIgnoreCase))
                {
                    return line.Substring(colonIndex + 1).Trim();
                }

                return line;
            }

            return null;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange change)
        {
            if (change != PlayModeStateChange.EnteredPlayMode
                || !Enabled
                || !SessionState.GetBool(ActiveSessionKey, false))
            {
                return;
            }

            var host = new GameObject("DebugAutopilotRuntime");
            Object.DontDestroyOnLoad(host);
            host.AddComponent<DebugAutopilotRuntime>().Configure(TargetStage, MaxRunSeconds);
        }
    }

    public sealed class DebugAutopilotWindow : EditorWindow
    {
        public static void Open()
        {
            GetWindow<DebugAutopilotWindow>(false, "Autopilot", true).minSize = new Vector2(320f, 220f);
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Corebound Autopilot", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Plays a real run from MainMenu with no keyboard input. Progress is logged to the "
                + "Console behind " + DebugAutopilot.LogPrefix + ".\n\nAgents can also start a run by writing a "
                + "stage number into " + DebugAutopilot.RequestFilePath + " in the project root.",
                MessageType.Info);

            EditorGUI.BeginChangeCheck();
            var enabled = EditorGUILayout.ToggleLeft("Enabled", DebugAutopilot.Enabled);
            var stage = EditorGUILayout.IntSlider("Target stage", DebugAutopilot.TargetStage, 1, 5);
            var maxSeconds = EditorGUILayout.Slider("Max run seconds", DebugAutopilot.MaxRunSeconds, 30f, 1800f);

            if (EditorGUI.EndChangeCheck())
            {
                DebugAutopilot.Enabled = enabled;
                DebugAutopilot.TargetStage = stage;
                DebugAutopilot.MaxRunSeconds = maxSeconds;
            }

            EditorGUILayout.Space();

            using (new EditorGUI.DisabledScope(!DebugAutopilot.Enabled || EditorApplication.isPlaying))
            {
                if (GUILayout.Button("Run now"))
                {
                    DebugAutopilot.StartAutopilot(DebugAutopilot.TargetStage);
                }
            }

            using (new EditorGUI.DisabledScope(!EditorApplication.isPlaying))
            {
                if (GUILayout.Button("Stop"))
                {
                    SessionState.SetBool(DebugAutopilot.ActiveSessionKey, false);
                    EditorApplication.isPlaying = false;
                }
            }
        }
    }

    public sealed class DebugAutopilotRuntime : MonoBehaviour
    {
        private enum Step
        {
            WaitForMainMenu,
            ConfirmNewRun,
            WaitForCombat,
            PlayRun,
            Done
        }

        private const float PreferredCombatDistance = 5.5f;

        // A run that has not changed its wave, its alive count or any enemy position for this long
        // is not slow, it is stuck. Reporting that as a finding is the whole point of the harness;
        // hanging silently is what the first version did and it taught us nothing.
        private const float StallSeconds = 12f;

        private int _targetStageIndex = 1;
        private float _maxRunSeconds = 420f;

        private Step _step = Step.WaitForMainMenu;
        private float _elapsed;
        private float _stepTimer;
        private float _reportTimer;
        private float _confirmTimer;
        private int _routePanelConfirms;
        private bool _stageSelected;

        private CoreboundInputReader _inputReader;
        private WaveManager _waveManager;
        private ShopService _shopService;
        private CardRewardController _rewardController;
        private TowerService _towerService;
        private PlayerCombatController _playerCombat;
        private Transform _playerTransform;

        private string _lastReport = string.Empty;
        private string _lastStallSignature = string.Empty;
        private float _stallTimer;
        private bool _stallReported;

        private float _strafeSign = 1f;
        private float _strafeFlipTimer;
        private Vector2 _lastPlayerPosition;
        private float _playerStuckTimer;

        private bool _sawShopOpen;
        private WaveState _lastLoggedState = (WaveState)(-1);

        private readonly StringBuilder _reportBuilder = new StringBuilder(384);
        private readonly StringBuilder _stallBuilder = new StringBuilder(192);

        public void Configure(int targetStageIndex, float maxRunSeconds)
        {
            _targetStageIndex = Mathf.Max(1, targetStageIndex);
            _maxRunSeconds = maxRunSeconds;
            Debug.Log(DebugAutopilot.LogPrefix + " started, target stage " + _targetStageIndex);
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;
            _stepTimer += Time.deltaTime;
            _confirmTimer += Time.deltaTime;

            if (_elapsed > _maxRunSeconds)
            {
                Debug.LogWarning(DebugAutopilot.LogPrefix + " hit the time cap at step " + _step + ".");
                Finish();
                return;
            }

            switch (_step)
            {
                case Step.WaitForMainMenu:
                    TickWaitForMainMenu();
                    break;
                case Step.ConfirmNewRun:
                    TickConfirmNewRun();
                    break;
                case Step.WaitForCombat:
                    TickWaitForCombat();
                    break;
                case Step.PlayRun:
                    TickPlayRun();
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

            if (!_stageSelected)
            {
                Corebound.UI.MainMenu.MainMenuRouteTargetController.DebugSelectStartStageIndex(_targetStageIndex);
                _stageSelected = true;
                Debug.Log(DebugAutopilot.LogPrefix + " stage select set to "
                    + Corebound.UI.MainMenu.MainMenuRouteTargetController.DebugStartStageIndex);
            }

            Advance(Step.ConfirmNewRun);
        }

        // Blind navigate-and-confirm is dangerous: the entry list is dynamic and one of the entries
        // is Quit, which would end Play Mode. Select New Run by route instead.
        private void TickConfirmNewRun()
        {
            if (_stepTimer < 0.5f)
            {
                return;
            }

            var menuController = FindFirstObjectByType<Corebound.UI.MainMenu.MainMenuController>();

            if (menuController == null)
            {
                Debug.LogWarning(DebugAutopilot.LogPrefix + " no MainMenuController found.");
                Finish();
                return;
            }

            if (menuController.IsIntroVisible)
            {
                menuController.AcknowledgeIntro();
                Debug.Log(DebugAutopilot.LogPrefix + " acknowledged intro");
                _stepTimer = 0f;
                return;
            }

            var entries = menuController.VisibleEntries;

            for (var index = 0; index < entries.Count; index++)
            {
                if (entries[index] == null || entries[index].Route != Corebound.UI.MainMenu.MainMenuRoute.NewRun)
                {
                    continue;
                }

                menuController.SelectIndex(index);
                _inputReader.DebugRaiseConfirm();
                Debug.Log(DebugAutopilot.LogPrefix + " confirmed New Run at index " + index);
                Advance(Step.WaitForCombat);
                return;
            }

            Debug.LogWarning(DebugAutopilot.LogPrefix + " no New Run entry among " + entries.Count + " entries.");
            Finish();
        }

        private void TickWaitForCombat()
        {
            _waveManager = FindFirstObjectByType<WaveManager>();

            if (_waveManager == null)
            {
                // Confirming New Run opens a route panel that needs its own confirm.
                if (_stepTimer > 1f)
                {
                    _inputReader.DebugRaiseConfirm();
                    _stepTimer = 0f;
                    _routePanelConfirms++;

                    if (_routePanelConfirms > 10)
                    {
                        Debug.LogWarning(DebugAutopilot.LogPrefix + " the route panel never started a run.");
                        Finish();
                    }
                }

                return;
            }

            ResolveRuntimeReferences();
            ReportOnce("combat scene loaded");
            Advance(Step.PlayRun);
        }

        private void TickPlayRun()
        {
            if (_waveManager == null)
            {
                Debug.LogWarning(DebugAutopilot.LogPrefix + " lost the WaveManager.");
                Finish();
                return;
            }

            ResolveRuntimeReferences();
            NoteShopVisibility();
            NoteStateTransition();

            _reportTimer += Time.deltaTime;

            if (_reportTimer >= 2f)
            {
                _reportTimer = 0f;
                ReportState();
            }

            DetectStall();

            switch (_waveManager.State)
            {
                case WaveState.Combat:
                case WaveState.BossCombat:
                    DriveCombat();
                    return;
                case WaveState.Failed:
                    Debug.LogWarning(DebugAutopilot.LogPrefix + " run failed at wave " + _waveManager.CurrentWaveNumber);
                    Finish();
                    return;
                case WaveState.Completed:
                    Debug.Log(DebugAutopilot.LogPrefix + " run completed");
                    Finish();
                    return;
                default:
                    DriveNonCombat();
                    return;
            }
        }

        // The shop is open for a fraction of a run, so a two-second sample can miss it entirely.
        // Latching the first sighting is what turns "I never saw the shop" into a fact either way.
        private void NoteShopVisibility()
        {
            if (_sawShopOpen || _shopService == null || !_shopService.IsOpen)
            {
                return;
            }

            _sawShopOpen = true;
            Debug.Log(DebugAutopilot.LogPrefix + " SHOP OPENED at wave " + _waveManager.CurrentWaveNumber
                + " with " + _shopService.CurrentOffers.Count + " offers, canvas=" + DescribePanel("ShopCanvas", "ShopScreen"));
        }

        // Periodic sampling misses anything shorter than the sample interval, and Reward and
        // Intermission are both routinely shorter than two seconds. Logging the transition itself
        // is the only way to prove which states a run actually passed through - without it, "the
        // shop never opened" and "the shop opened between two samples" look identical.
        private void NoteStateTransition()
        {
            var state = _waveManager.State;

            if (state == _lastLoggedState)
            {
                return;
            }

            _lastLoggedState = state;
            Debug.Log(DebugAutopilot.LogPrefix + " -> " + state
                + " wave=" + _waveManager.CurrentWaveNumber
                + " gates[shop=" + _waveManager.RequiresIntermissionShopAction
                + " tower=" + _waveManager.RequiresIntermissionTowerAction + "]"
                + " shopOpen=" + (_shopService != null && _shopService.IsOpen)
                + " shopOffers=" + (_shopService != null ? _shopService.CurrentOffers.Count : -1));
        }

        private void ResolveRuntimeReferences()
        {
            if (_playerTransform == null)
            {
                _playerCombat = FindFirstObjectByType<PlayerCombatController>();
                _playerTransform = _playerCombat != null ? _playerCombat.transform : null;
                RebindInputReaderToPlayerScene();
            }

            if (_shopService == null)
            {
                _shopService = FindFirstObjectByType<ShopService>();
            }

            if (_rewardController == null)
            {
                _rewardController = FindFirstObjectByType<CardRewardController>();
            }

            if (_towerService == null)
            {
                _towerService = FindFirstObjectByType<TowerService>();
            }
        }

        // MainMenu.unity and SampleScene.unity each carry their own CoreboundInputReader, and both
        // are loaded at once. FindFirstObjectByType can return either, so the menu's reader was
        // being driven while the gameplay systems listened to the scene's own - the player stood
        // still and never fired. Bind to the one in the same scene as the player.
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
                if (readers[index] == null || readers[index].gameObject.scene != playerScene)
                {
                    continue;
                }

                if (_inputReader != readers[index])
                {
                    Debug.Log(DebugAutopilot.LogPrefix + " rebound input reader to the one in " + playerScene.name);
                }

                _inputReader = readers[index];
                return;
            }
        }

        private void DriveCombat()
        {
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

            // PlayerAimResolver reads Aim as a screen position in mouse mode and as a stick
            // direction in gamepad mode. A driven test has no real mouse, so it must announce
            // itself as a gamepad or every shot is aimed at whatever screen corner the fed vector
            // maps to - which looks exactly like "projectiles do no damage".
            _inputReader.DebugSetUsingGamepad(true);
            _inputReader.DebugSetAim(direction);
            _inputReader.DebugSetPrimaryFireHeld(true);

            // Kite rather than charge: hold a working distance, circle so chasers never line up a
            // straight run, and bias back toward the middle so it cannot be pinned against a wall.
            var radial = (distance - PreferredCombatDistance) * 0.6f;
            var tangent = new Vector2(-direction.y, direction.x) * _strafeSign;
            var move = direction * Mathf.Clamp(radial, -1f, 1f) + tangent * 0.85f;

            var arena = FindFirstObjectByType<StageArenaArtPresenter>();

            if (arena != null)
            {
                var halfExtents = arena.ArenaSize * 0.5f - new Vector2(2.5f, 2.5f);

                if (Mathf.Abs(playerPosition.x) > halfExtents.x || Mathf.Abs(playerPosition.y) > halfExtents.y)
                {
                    move += -playerPosition.normalized * 2f;
                }
            }

            // Wedged against geometry: reverse the orbit and push hard toward the middle instead of
            // grinding into the same wall for the rest of the run.
            if ((playerPosition - _lastPlayerPosition).sqrMagnitude < 0.0004f)
            {
                _playerStuckTimer += Time.deltaTime;

                if (_playerStuckTimer > 1.5f)
                {
                    _playerStuckTimer = 0f;
                    _strafeSign = -_strafeSign;
                    move = -playerPosition.normalized * 1.5f + tangent;
                }
            }
            else
            {
                _playerStuckTimer = 0f;
            }

            _lastPlayerPosition = playerPosition;
            _strafeFlipTimer += Time.deltaTime;

            if (_strafeFlipTimer > 2.5f)
            {
                _strafeFlipTimer = 0f;
                _strafeSign = -_strafeSign;
            }

            _inputReader.DebugSetMove(move.sqrMagnitude > 1f ? move.normalized : move);
        }

        // Reward screen, shop and intermission all resolve on Confirm, but at a human pace rather
        // than machine-gunned: a confirm fired into a panel that opened this frame is swallowed,
        // and spamming it also made the shop impossible to observe.
        private void DriveNonCombat()
        {
            if (_inputReader == null)
            {
                return;
            }

            _inputReader.DebugSetPrimaryFireHeld(false);
            _inputReader.DebugSetMove(Vector2.zero);

            if (_confirmTimer < 1.25f)
            {
                return;
            }

            _confirmTimer = 0f;

            // Build a tower when the intermission is gating on one, otherwise Confirm alone can
            // never clear the gate and the run sits in Intermission until the time cap.
            if (_waveManager.State == WaveState.Intermission
                && _waveManager.RequiresIntermissionTowerAction
                && _towerService != null)
            {
                // Alternate Interact and Confirm: Interact satisfies the mandatory build gate, and
                // Confirm is what actually ends the intermission once the gate is clear.
                _inputReader.DebugRaiseInteract();
                _inputReader.DebugRaiseConfirm();
                return;
            }

            _inputReader.DebugRaiseConfirm();
        }

        private Transform FindNearestEnemy()
        {
            var enemies = FindObjectsByType<EnemyRuntimeController>(FindObjectsSortMode.None);
            Transform nearest = null;
            var nearestDistanceSquared = float.MaxValue;
            var origin = (Vector2)_playerTransform.position;

            for (var index = 0; index < enemies.Length; index++)
            {
                var enemy = enemies[index];

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

        // A stall is the single most useful thing this harness can find, because it is exactly the
        // class of bug a human reports as "I killed them and nothing happened". The signature
        // includes rounded enemy positions on purpose, so an enemy that is alive but frozen counts
        // as no progress even while the wave timer keeps ticking.
        private void DetectStall()
        {
            _stallBuilder.Length = 0;
            _stallBuilder.Append(_waveManager.State).Append('|')
                .Append(_waveManager.CurrentWaveNumber).Append('|')
                .Append(_waveManager.RemainingAliveEnemies).Append('|')
                .Append(_waveManager.RequiresIntermissionShopAction).Append('|')
                .Append(_waveManager.RequiresIntermissionTowerAction);

            var enemies = FindObjectsByType<EnemyRuntimeController>(FindObjectsSortMode.None);

            for (var index = 0; index < enemies.Length; index++)
            {
                var enemy = enemies[index];

                if (enemy == null || !enemy.isActiveAndEnabled || !enemy.IsAlive)
                {
                    continue;
                }

                var position = enemy.transform.position;
                _stallBuilder.Append('|').Append(Mathf.RoundToInt(position.x)).Append(',').Append(Mathf.RoundToInt(position.y));
            }

            var signature = _stallBuilder.ToString();

            if (signature != _lastStallSignature)
            {
                _lastStallSignature = signature;
                _stallTimer = 0f;
                _stallReported = false;
                return;
            }

            _stallTimer += Time.deltaTime;

            if (_stallTimer < StallSeconds || _stallReported)
            {
                return;
            }

            _stallReported = true;
            _lastReport = string.Empty;
            Debug.LogError(DebugAutopilot.LogPrefix + " STALL: nothing changed for " + StallSeconds.ToString("0")
                + "s. The state below is the evidence for whatever is wedged.");
            ReportState();
        }

        private void ReportState()
        {
            _reportBuilder.Length = 0;
            _reportBuilder.Append("stage=").Append(_waveManager.StageDataAsset != null ? _waveManager.StageDataAsset.name : "null");
            _reportBuilder.Append(" state=").Append(_waveManager.State);
            _reportBuilder.Append(" wave=").Append(_waveManager.CurrentWaveNumber).Append('/').Append(_waveManager.TotalWaveCount);
            _reportBuilder.Append(" alive=").Append(_waveManager.RemainingAliveEnemies);

            // The gates that decide whether an intermission can end at all.
            _reportBuilder.Append(" gates[shop=").Append(_waveManager.RequiresIntermissionShopAction);
            _reportBuilder.Append(" tower=").Append(_waveManager.RequiresIntermissionTowerAction).Append(']');

            _reportBuilder.Append(" shopOpen=").Append(_shopService != null && _shopService.IsOpen);
            _reportBuilder.Append(" shopOffers=").Append(_shopService != null ? _shopService.CurrentOffers.Count : -1);
            _reportBuilder.Append(" shopCanvas=").Append(DescribePanel("ShopCanvas", "ShopScreen"));
            _reportBuilder.Append(" rewardOpen=").Append(_rewardController != null && _rewardController.IsOpen);
            _reportBuilder.Append(" towers=").Append(_towerService != null ? _towerService.BuiltTowerCount : -1);

            var arena = FindFirstObjectByType<StageArenaArtPresenter>();
            _reportBuilder.Append(" arenaSize=").Append(arena != null ? arena.ArenaSize.ToString("0.##") : "null");
            _reportBuilder.Append(" player=").Append(_playerTransform != null ? ((Vector2)_playerTransform.position).ToString("0.#") : "NULL");
            AppendEnemyPositions(_reportBuilder, arena);
            AppendHudState(_reportBuilder);

            var report = _reportBuilder.ToString();

            if (report == _lastReport)
            {
                return;
            }

            _lastReport = report;
            Debug.Log(DebugAutopilot.LogPrefix + " " + report);
        }

        private void ReportOnce(string message)
        {
            Debug.Log(DebugAutopilot.LogPrefix + " " + message);
            _lastReport = string.Empty;
            ReportState();
        }

        private static string DescribePanel(string canvasName, string rootName)
        {
            var canvas = GameObject.Find(canvasName);

            if (canvas == null)
            {
                return "NO_CANVAS";
            }

            var root = canvas.transform.Find(rootName);

            if (root == null)
            {
                return "NO_ROOT";
            }

            if (!root.gameObject.activeInHierarchy)
            {
                return "inactive";
            }

            // ShopPresenter and CardRewardPresenter hide themselves by setting CanvasGroup alpha to
            // 0, never by deactivating the object, so activeInHierarchy is true even when nothing is
            // on screen. The alpha is the only honest answer to "can the player see this".
            var canvasGroup = root.GetComponentInChildren<CanvasGroup>(true);

            if (canvasGroup == null)
            {
                return "VISIBLE(no-group)";
            }

            return canvasGroup.alpha > 0.01f ? "VISIBLE a=" + canvasGroup.alpha.ToString("0.##") : "hidden a=0";
        }

        // An enemy parked outside the boundary can never be reached or killed, which hangs the wave
        // forever on RemainingAliveEnemies. Flagging it inline is what caught that bug.
        private void AppendEnemyPositions(StringBuilder builder, StageArenaArtPresenter arena)
        {
            var enemies = FindObjectsByType<EnemyRuntimeController>(FindObjectsSortMode.None);
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
                builder.Append(enemy.name).Append('@').Append(position.ToString("0.#")).Append(isInside ? " IN" : " OUTSIDE");

                if (_playerTransform != null)
                {
                    builder.Append(" d=").Append(Vector2.Distance(position, _playerTransform.position).ToString("0.#"));
                }

                builder.Append("; ");
            }

            builder.Append(']');
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

            builder.Append('"').Append(text.text).Append("\" ");
        }

        private void Advance(Step next)
        {
            _step = next;
            _stepTimer = 0f;
        }

        private void Finish()
        {
            _step = Step.Done;
            Debug.Log(DebugAutopilot.LogPrefix + " finished after " + _elapsed.ToString("0.0") + "s. sawShop=" + _sawShopOpen);
            SessionState.SetBool(DebugAutopilot.ActiveSessionKey, false);
            EditorApplication.isPlaying = false;
        }
    }
}
