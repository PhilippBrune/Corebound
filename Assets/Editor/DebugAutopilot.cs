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

            // "loadout3": stage 3 with all six slots filled (3 Rapid, 2 Spread, 1 Beam), so Set
            // Bonuses, the projectile budget and the loadout HUD can be exercised without first
            // earning slot unlocks.
            // "arsenal3": the same, with the six weapons added 2026-09-22 - one of each, so Full
            // Arsenal is active and every new archetype fires in one run.
            // "pad3": drive movement, aim and fire through a virtual Gamepad device instead of the
            // reader's debug surface, so a run also proves the bindings and the device detection.
            var padTest = contents.StartsWith("pad", System.StringComparison.OrdinalIgnoreCase);
            SessionState.SetBool(PadTestSessionKey, padTest);

            if (padTest)
            {
                contents = contents.Substring("pad".Length).Trim();
            }

            var arsenalTest = contents.StartsWith("arsenal", System.StringComparison.OrdinalIgnoreCase);
            var loadoutTest = arsenalTest || contents.StartsWith("loadout", System.StringComparison.OrdinalIgnoreCase);
            SessionState.SetBool(LoadoutTestSessionKey, loadoutTest);
            SessionState.SetBool(ArsenalTestSessionKey, arsenalTest);

            if (loadoutTest)
            {
                contents = contents.Substring(arsenalTest ? "arsenal".Length : "loadout".Length).Trim();
            }

            // "progression1": grant test currency in the main menu, buy a scripted set of skill
            // nodes, upgrades and a slot unlock through the Progression screen, photograph every
            // tab, then start the run. Writes to the real profile save - back it up first.
            var progressionTest = contents.StartsWith("progression", System.StringComparison.OrdinalIgnoreCase);
            SessionState.SetBool(ProgressionTestSessionKey, progressionTest);

            if (progressionTest)
            {
                contents = contents.Substring("progression".Length).Trim();
            }

            // "ascension1": fill the whole tree through SkillTreeRules, buy one Ascension rank on
            // the Progression screen, photograph it and the main-menu badge, then start the run.
            // "god3": stage 3 with the debug god mode on, so the run reaches the boss (boss
            // rewards, telegraph art, the guaranteed boss weapon offer).
            var godMode = contents.StartsWith("god", System.StringComparison.OrdinalIgnoreCase);
            SessionState.SetBool(GodModeSessionKey, godMode);

            if (godMode)
            {
                contents = contents.Substring("god".Length).Trim();
            }

            var ascensionTest = contents.StartsWith("ascension", System.StringComparison.OrdinalIgnoreCase);
            SessionState.SetBool(AscensionTestSessionKey, ascensionTest);

            if (ascensionTest)
            {
                contents = contents.Substring("ascension".Length).Trim();
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
        public const string LoadoutTestSessionKey = "Corebound.Autopilot.LoadoutTest";
        public const string ArsenalTestSessionKey = "Corebound.Autopilot.ArsenalTest";
        public const string PadTestSessionKey = "Corebound.Autopilot.PadTest";
        public const string ProgressionTestSessionKey = "Corebound.Autopilot.ProgressionTest";
        public const string AscensionTestSessionKey = "Corebound.Autopilot.AscensionTest";
        public const string GodModeSessionKey = "Corebound.Autopilot.GodMode";

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
            var runtime = host.AddComponent<DebugAutopilotRuntime>();
            runtime.Configure(TargetStage, MaxRunSeconds, SessionState.GetBool(LoadoutTestSessionKey, false));
            runtime.ConfigureProgressionTest(SessionState.GetBool(ProgressionTestSessionKey, false), SessionState.GetBool(AscensionTestSessionKey, false));

            if (SessionState.GetBool(GodModeSessionKey, false))
            {
                Corebound.Gameplay.Combat.DebugGodMode.SetEnabled(true);
                Debug.Log(LogPrefix + " god mode on for this run");
            }
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
            AfterRun,
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

        // Visual probes (2026-09-22). Several features are only verifiable by looking at them -
        // lit spawn gates, the build browser, tag icons, the shop layout, boss telegraphs - and the
        // states they live in are too short for a manual PrintWindow. Game-view screenshots are
        // written to <project>/Temp/AutopilotShots at the moment each state is reached.
        private int _lastWaveShot = -1;
        private float _waveShotTimer = -1f;
        private bool _rewardShotTaken;
        private float _rewardShotTimer = -1f;
        private bool _shopShotTaken;
        private float _shopShotTimer = -1f;
        private int _telegraphShots;
        private int _telegraphStage = -1;
        private bool _browserProbed;
        private float _browserTimer;
        private float _browserHoldTimer;
        private bool _pauseProbed;
        private int _pauseProbeStage;
        private float _pauseUnscaledTimer = -1f;
        private bool _pauseShotTaken;
        private int _pauseTabShots;
        private int _shopUnchangedConfirms;
        private int _lastShopOfferCount = -1;

        private readonly StringBuilder _reportBuilder = new StringBuilder(384);
        private readonly StringBuilder _stallBuilder = new StringBuilder(192);

        private bool _loadoutTest;
        private bool _loadoutTestApplied;
        private int _loadoutProbeStage = -1;
        private float _loadoutProbeTimer = -1f;

        public void Configure(int targetStageIndex, float maxRunSeconds, bool loadoutTest = false)
        {
            _targetStageIndex = Mathf.Max(1, targetStageIndex);
            _maxRunSeconds = maxRunSeconds;
            _loadoutTest = loadoutTest;

            if (loadoutTest)
            {
                Debug.Log(DebugAutopilot.LogPrefix + " loadout test requested: six slots, " + (SessionState.GetBool(DebugAutopilot.ArsenalTestSessionKey, false) ? "one of each new weapon" : "3 Rapid + 2 Spread + 1 Beam"));
            }

            Debug.Log(DebugAutopilot.LogPrefix + " started, target stage " + _targetStageIndex);
            HookRunSummary();
        }

        private void OnDestroy()
        {
            UnhookRunSummary();
        }

        private void Update()
        {
            // Unscaled: every blocking overlay in the game holds Time.timeScale at 0 (see
            // WaveStatePresenter.OpenOverlay), so a scaled clock stops exactly when the autopilot
            // most needs to act - and the time cap that is supposed to rescue a wedged run stops
            // with it. This is what left the 2026-09-25 run frozen in front of the "Collect
            // Resources" beat with the dismissal code right there and never reaching 0.6s.
            _elapsed += Time.unscaledDeltaTime;
            TickRunSummary();

            // A tutorial beat blocks everything until it is confirmed. It can appear in Combat,
            // where DriveNonCombat never runs, so it is handled before the step machine - without
            // this the run sat in front of the first beat until the time cap (2026-09-23).
            if (Corebound.Gameplay.Tutorial.TutorialService.IsBeatShowing && _inputReader != null)
            {
                _confirmTimer += Time.unscaledDeltaTime;

                if (_confirmTimer >= 0.6f)
                {
                    _confirmTimer = 0f;
                    _inputReader.DebugRaiseConfirm();
                }

                return;
            }
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
                case Step.AfterRun:
                    TickAfterRun();
                    break;
                case Step.PlayRun:
                    TickPlayRun();
                    break;
            }
        }

        private bool _progressionTest;
        private int _progressionStep = -1;
        private float _progressionTimer;
        private Corebound.MetaProgression.MetaProgressionRouteTargetController _progressionRoute;
        private Corebound.UI.MainMenu.MainMenuRouteTargetController _menuRoute;

        private bool _ascensionTest;

        public void ConfigureProgressionTest(bool enabled, bool ascension = false)
        {
            _progressionTest = enabled || ascension;
            _ascensionTest = ascension;

            if (enabled)
            {
                Debug.Log(DebugAutopilot.LogPrefix + " progression test requested");
            }
        }

        // One scripted action per beat, so each purchase and each tab gets its own frame and its
        // own screenshot. Returns true while the script is still running.
        private bool TickProgressionTest()
        {
            if (!_progressionTest || _progressionStep >= ProgressionScript.Length)
            {
                return false;
            }

            if (!_ascensionTest && _progressionStep > 0 && ProgressionScript[_progressionStep - 1] == "X")
            {
                _progressionStep = ProgressionScript.Length;
                return false;
            }

            _progressionTimer += Time.unscaledDeltaTime;

            if (_progressionTimer < 0.35f)
            {
                return true;
            }

            _progressionTimer = 0f;

            if (_progressionStep < 0)
            {
                var service = FindFirstObjectByType<Corebound.MetaProgression.MetaProgressionService>();
                _progressionRoute = FindFirstObjectByType<Corebound.MetaProgression.MetaProgressionRouteTargetController>();

                if (service == null || _progressionRoute == null)
                {
                    Debug.LogWarning(DebugAutopilot.LogPrefix + " progression test: no MetaProgressionService/route in the main menu");
                    _progressionStep = ProgressionScript.Length;
                    return false;
                }

                if (_ascensionTest)
                {
                    service.DebugGrant(260, 0);
                    var tree = service.ActiveSkillTree;
                    var save = service.SaveData;
                    var bought = 0;

                    // Buy everything, repeatedly, until nothing more can be bought - the rules
                    // themselves enforce prerequisites and branch spend, so this also exercises them.
                    for (var pass = 0; pass < 12; pass++)
                    {
                        foreach (var branch in tree.Branches)
                        {
                            foreach (var keyNode in branch.KeyNodes)
                            {
                                bought += Corebound.MetaProgression.SkillTreeRules.TryPurchaseKeyNode(save, tree, branch, keyNode) ? 1 : 0;

                                foreach (var subNode in keyNode.SubNodes)
                                {
                                    while (Corebound.MetaProgression.SkillTreeRules.TryPurchaseSubNode(save, tree, keyNode, subNode))
                                    {
                                        bought++;
                                    }
                                }
                            }
                        }
                    }

                    service.Commit();

                    // Levelling v3: Ascension opens at the level cap and a Keystone needs ten
                    // Paragon levels: the road to level 70, then ten Paragon levels on top.
                    service.DebugGrantExperience(Corebound.SaveSystem.MetaProgressionSaveData.ExperienceToReachLevel(Corebound.SaveSystem.MetaProgressionSaveData.MaxLevel)
                        + 10 * Corebound.SaveSystem.MetaProgressionSaveData.ParagonLevelCost);
                    Debug.Log(DebugAutopilot.LogPrefix + " ASCENSION tree filled: " + bought + " purchases, complete="
                        + Corebound.MetaProgression.SkillTreeRules.IsTreeComplete(save, tree) + " SP left=" + save.SkillPointsAvailable
                        + " level=" + save.Level + " paragon=" + save.ParagonLevel + " AP=" + save.AscensionPointsAvailable + " keystones=" + save.KeystonesAvailable);
                    _progressionRoute.Open();
                    _progressionStep = System.Array.IndexOf(ProgressionScript, "ASC");
                    return true;
                }

                service.DebugGrant(60, 400);
                _progressionRoute.Open();
                Debug.Log(DebugAutopilot.LogPrefix + " PROGRESSION granted 60 SP + 400 Alloy, route open=" + _progressionRoute.IsOpen + " " + _progressionRoute.CurrencyLabel);
                _progressionStep = 0;
                return true;
            }

            var action = ProgressionScript[_progressionStep++];

            switch (action[0])
            {
                case 'R':
                    // The shoulder-button path. Left/Right on a branch tab moves across the graph
                    // first and only switches tab at its edge (N3), so it is no longer a reliable
                    // "next tab" for a script.
                    _progressionRoute.SwitchTab(1);
                    break;
                case 'D':
                    _progressionRoute.TryHandleNavigation(Vector2.down);
                    break;
                case 'N':
                    // "N<key>,<sub>": select a graph node directly, as a mouse hover does.
                    var parts = action.Substring(1).Split(',');
                    var row = _progressionRoute.FindGraphRow(int.Parse(parts[0]), int.Parse(parts[1]));
                    _progressionRoute.TrySelectRow(row);
                    break;
                case 'M':
                    // "M<u|d|l|r>": push the stick, and log where the selection landed.
                    var direction = action[1] == 'u' ? Vector2.up : action[1] == 'd' ? Vector2.down : action[1] == 'l' ? Vector2.left : Vector2.right;
                    var moved = _progressionRoute.TryHandleNavigation(direction);
                    Debug.Log(DebugAutopilot.LogPrefix + " PROGRESSION stick " + action[1] + " moved=" + moved + " now on '" + _progressionRoute.SelectedLabel + "'");
                    break;
                case 'C':
                    var bought = _progressionRoute.TryHandleConfirm();
                    Debug.Log(DebugAutopilot.LogPrefix + " PROGRESSION confirm bought=" + bought + " " + _progressionRoute.CurrencyLabel);
                    break;
                case 'S':
                    Shot("progression_" + action.Substring(1));
                    break;
                case 'X':
                    _progressionRoute.Close();
                    Debug.Log(DebugAutopilot.LogPrefix + " PROGRESSION done: " + _progressionRoute.CurrencyLabel);
                    break;
                case 'A':
                    // "ASC" is a marker, not an action.
                    break;
                case 'O':
                    // "Orecords": open the Records route panel.
                    _menuRoute = _menuRoute != null ? _menuRoute : FindFirstObjectByType<Corebound.UI.MainMenu.MainMenuRouteTargetController>();
                    _menuRoute?.DebugOpenRoute(action == "Osettings"
                        ? Corebound.UI.MainMenu.MainMenuRoute.Settings
                        : Corebound.UI.MainMenu.MainMenuRoute.Records);
                    Debug.Log(DebugAutopilot.LogPrefix + " RECORDS opened=" + (_menuRoute != null && _menuRoute.ActivePanelRoute == Corebound.UI.MainMenu.MainMenuRoute.Records)
                        + " cards=" + (_menuRoute != null ? _menuRoute.RecordsCards.Count : 0)
                        + " collected=" + (_menuRoute != null ? _menuRoute.RecordsCollectedCount : 0));
                    break;
                case 'T':
                    _menuRoute?.TryHandleTabSwitch(1);
                    break;
                case 'V':
                    // "V<u|d|l|r>": navigate inside a route panel.
                    var push = action[1] == 'u' ? Vector2.up : action[1] == 'd' ? Vector2.down : action[1] == 'l' ? Vector2.left : Vector2.right;
                    _menuRoute?.TryHandleNavigation(push);
                    break;
                case 'K':
                    _menuRoute?.TryHandleCancel();
                    break;
                case 'W':
                    // "W<notches>": a mouse-wheel scroll on the Progression list, negative is down.
                    var presenter = FindFirstObjectByType<Corebound.MetaProgression.MetaProgressionRouteTargetPresenter>();
                    presenter?.DebugScrollWheel(120f * float.Parse(action.Substring(1), System.Globalization.CultureInfo.InvariantCulture));
                    break;
                case 'Z':
                    // Close Progression without ending the script ('X' ends it).
                    _progressionRoute.Close();
                    break;
            }

            return true;
        }

        // Tab order: Unlocks, Upgrades, Combat, Construction, Industry, Command, Ascension.
        // Unlocks: down to "Fifth Weapon Mount" (row 4) and buy it. Upgrades: row 0 is the Rapid
        // header, so the cursor starts on Rapid Damage - buy two ranks. Each branch: the root key
        // node, three ranks of its first sub node, one of its second. Combat also walks the stick
        // around the graph, so the log shows where each push lands.
        private static readonly string[] ProgressionScript =
        {
            "Sunlocks_before", "D", "D", "D", "D", "C", "Sunlocks_after",
            "R", "Supgrades_before", "C", "C", "Supgrades_after",
            "R", "C", "N0,0", "C", "C", "C", "N0,1", "C", "Scombat",
            "N0,-1", "Mu", "Mu", "Ml", "Ml", "Mr", "Mr", "Mr", "Md", "Md", "Md", "Scombat_stick", "Mu",
            "R", "C", "N0,0", "C", "C", "C", "N0,1", "C", "Sconstruction",
            "R", "C", "N0,0", "C", "C", "C", "N0,1", "C", "Sindustry",
            "R", "C", "N0,0", "C", "C", "C", "N0,1", "C", "Scommand",
            "R", "Sascension",
            "Z", "Osettings", "Ssettings", "K", "Orecords", "Srecords_runs", "T", "Vr", "Vr", "Vd", "Srecords_cards", "K",
            "X",
            "ASC", "R", "R", "N4,0", "C", "Scombat_ring", "R", "R", "R", "R", "Sascension_before", "D", "C", "Sascension_after", "R", "Squests", "D", "D", "D", "D", "D", "D", "D", "D", "D", "D", "D", "D", "D", "D", "D", "Squests_down15",
            "D", "D", "D", "D", "D", "D", "D", "D", "D", "D", "D", "D", "D", "D", "D", "Squests_down30", "W4", "Squests_wheel_up4",
            "W-8", "Squests_wheel_down8", "X", "Smenu_badge",
        };

        private void TickWaitForMainMenu()
        {
            _inputReader = FindFirstObjectByType<CoreboundInputReader>();

            if (_inputReader == null || _stepTimer < 1.5f)
            {
                return;
            }

            if (TickProgressionTest())
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
            ApplyLoadoutTestOnce();
            NoteShopVisibility();
            NoteStateTransition();
            TickVisualProbes();

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
                    Advance(Step.AfterRun);
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
            if (UsePad)
            {
                _padAim = direction;
                _padFire = true;
                PushPadState();
            }
            else
            {
                _inputReader.DebugSetUsingGamepad(true);
                _inputReader.DebugSetAim(direction);
                _inputReader.DebugSetPrimaryFireHeld(true);
            }

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

            var clampedMove = move.sqrMagnitude > 1f ? move.normalized : move;

            if (UsePad)
            {
                _padMove = clampedMove;
                PushPadState();
            }
            else
            {
                _inputReader.DebugSetMove(clampedMove);
            }
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

            if (TickIntermissionProbes())
            {
                return;
            }

            // Hold Confirm while a screenshot of the open panel is pending, or it photographs
            // whatever replaced the panel.
            if (_confirmTimer < 1.25f || _rewardShotTimer >= 0f || _shopShotTimer >= 0f)
            {
                if (_rewardShotTimer >= 0f || _shopShotTimer >= 0f)
                {
                    _confirmTimer = 0f;
                }

                return;
            }

            _confirmTimer = 0f;

            // A shop with nothing affordable can only be left with Cancel (skip). Confirm on an
            // unaffordable offer is declined, so pressing only Confirm wedged every such shop.
            // An offer can also be affordable yet declined (a tower upgrade with no tower built),
            // so two Confirms that change nothing count as "nothing buyable" too.
            if (_shopService != null && _shopService.IsOpen)
            {
                var offerCount = _shopService.CurrentOffers.Count;
                _shopUnchangedConfirms = offerCount == _lastShopOfferCount ? _shopUnchangedConfirms + 1 : 0;
                _lastShopOfferCount = offerCount;
            }
            else
            {
                _shopUnchangedConfirms = 0;
                _lastShopOfferCount = -1;
            }

            if (_shopService != null && _shopService.IsOpen && (!CanAffordAnyShopOffer() || _shopUnchangedConfirms >= 2))
            {
                _inputReader.DebugRaiseCancel();
                Debug.Log(DebugAutopilot.LogPrefix + " SHOP skipped (nothing affordable)");
                return;
            }

            // Build a tower when the intermission is gating on one, otherwise Confirm alone can
            // never clear the gate and the run sits in Intermission until the time cap.
            if (_waveManager.State == WaveState.Intermission
                && _waveManager.RequiresIntermissionTowerAction
                && _towerService != null
                && (_shopService == null || !_shopService.IsOpen))
            {
                // Alternate Interact and Confirm: Interact satisfies the mandatory build gate, and
                // Confirm is what actually ends the intermission once the gate is clear.
                _inputReader.DebugRaiseInteract();
                _inputReader.DebugRaiseConfirm();
                return;
            }

            _inputReader.DebugRaiseConfirm();
        }

        private bool CanAffordAnyShopOffer()
        {
            var offers = _shopService.CurrentOffers;

            for (var index = 0; index < offers.Count; index++)
            {
                if (offers[index] != null && _shopService.CanAffordOffer(offers[index]))
                {
                    return true;
                }
            }

            return false;
        }

        private void TickVisualProbes()
        {
            var state = _waveManager.State;
            var wave = _waveManager.CurrentWaveNumber;

            if (state != WaveState.Intermission)
            {
                _browserProbed = false;
                _browserTimer = 0f;
                _browserHoldTimer = 0f;
            }

            // Spawn gates pulse during the wave-start delay, so the shot is taken inside it.
            if ((state == WaveState.Combat || state == WaveState.BossCombat) && wave != _lastWaveShot)
            {
                _lastWaveShot = wave;
                _waveShotTimer = 0.8f;
                var arena = FindFirstObjectByType<StageArenaArtPresenter>();
                Debug.Log(DebugAutopilot.LogPrefix + " GATES wave " + wave + ": " + (arena != null ? arena.SpawnGateCount : -1) + " gate(s)");
            }

            if (TickShotTimer(ref _waveShotTimer))
            {
                Shot("stage" + _waveManager.StageIndex + "_wave" + wave + "_gates");
            }

            // Keyed on the Reward state, not IsOpen: the confirm that closes the screen can land in
            // the same frame IsOpen first reports true.
            var rewardOpen = state == WaveState.Reward;

            if (rewardOpen && !_rewardShotTaken)
            {
                _rewardShotTaken = true;
                _rewardShotTimer = 0.6f;
            }
            else if (!rewardOpen)
            {
                _rewardShotTaken = false;
            }

            if (TickShotTimer(ref _rewardShotTimer))
            {
                Shot("stage" + _waveManager.StageIndex + "_wave" + wave + "_reward");
            }

            var shopOpen = _shopService != null && _shopService.IsOpen;

            if (shopOpen && !_shopShotTaken)
            {
                _shopShotTaken = true;
                _shopShotTimer = 0.6f;
            }
            else if (!shopOpen)
            {
                _shopShotTaken = false;
            }

            if (TickShotTimer(ref _shopShotTimer))
            {
                Shot("stage" + _waveManager.StageIndex + "_wave" + wave + "_shop");
            }

            if (_telegraphStage != _waveManager.StageIndex)
            {
                _telegraphStage = _waveManager.StageIndex;
                _telegraphShots = 0;
            }

            if (state == WaveState.BossCombat && _telegraphShots < 2)
            {
                var telegraph = GameObject.Find("BossShapeTelegraph");

                if (telegraph != null && telegraph.activeInHierarchy)
                {
                    var renderer = telegraph.GetComponent<SpriteRenderer>();

                    // Late in the wind-up, so the art is at full size and opacity.
                    if (renderer != null && renderer.color.a > 0.7f)
                    {
                        _telegraphShots++;
                        Debug.Log(DebugAutopilot.LogPrefix + " BOSS TELEGRAPH " + (renderer.sprite != null ? renderer.sprite.name : "null") + " scale=" + telegraph.transform.localScale.x.ToString("0.00"));
                        Shot("stage" + _waveManager.StageIndex + "_boss_telegraph" + _telegraphShots);
                    }
                }
            }
        }

        // Once per Intermission: walk to the nearest empty tower slot, hold still while the build
        // browser is up, photograph it, build. Once per stage: open and photograph the pause menu.
        // Returns true while a probe owns the input.
        private bool TickIntermissionProbes()
        {
            if (_waveManager.State != WaveState.Intermission || _towerService == null || _playerTransform == null)
            {
                return false;
            }

            if (_shopService != null && _shopService.IsOpen)
            {
                return false;
            }

            if (_pauseUnscaledTimer >= 0f)
            {
                _pauseUnscaledTimer -= Time.unscaledDeltaTime;

                // CaptureScreenshot renders at the end of the frame, so closing the menu in the
                // same frame photographs it already closed. Shoot first, close a beat later.
                // Perks, then Player and Towers tabs: shoot, switch, shoot, switch, shoot.
                if (_pauseUnscaledTimer < 2.2f && _pauseTabShots == 0)
                {
                    _pauseTabShots = 1;
                    Shot("stage" + _waveManager.StageIndex + "_pause");
                }
                else if (_pauseUnscaledTimer < 1.9f && _pauseTabShots == 1)
                {
                    _pauseTabShots = 2;
                    _inputReader.DebugRaiseNext();
                }
                else if (_pauseUnscaledTimer < 1.5f && _pauseTabShots == 2)
                {
                    _pauseTabShots = 3;
                    Shot("stage" + _waveManager.StageIndex + "_pause_player");
                }
                else if (_pauseUnscaledTimer < 1.2f && _pauseTabShots == 3)
                {
                    _pauseTabShots = 4;
                    _inputReader.DebugRaiseNext();
                }
                else if (_pauseUnscaledTimer < 0.8f && _pauseTabShots == 4)
                {
                    _pauseTabShots = 5;
                    Shot("stage" + _waveManager.StageIndex + "_pause_towers");
                }
                else if (_pauseUnscaledTimer < 0.5f && _pauseTabShots == 5)
                {
                    // Back to Perks, so the next pause opens where a player expects it.
                    _pauseTabShots = 6;
                    _inputReader.DebugRaiseNext();
                }

                if (_pauseUnscaledTimer < 0f)
                {
                    _inputReader.DebugRaisePause();
                    Debug.Log(DebugAutopilot.LogPrefix + " PAUSE closed");
                }

                return true;
            }

            if (!_pauseProbed || _pauseProbeStage != _waveManager.StageIndex)
            {
                _pauseProbed = true;
                _pauseProbeStage = _waveManager.StageIndex;
                _inputReader.DebugRaisePause();
                _pauseUnscaledTimer = 2.5f;
                _pauseShotTaken = false;
                _pauseTabShots = 0;
                Debug.Log(DebugAutopilot.LogPrefix + " PAUSE opened, timeScale=" + Time.timeScale);
                return true;
            }

            // Once per stage: open the loadout panel, photograph it, close it.
            if (_loadoutProbeTimer >= 0f)
            {
                _loadoutProbeTimer -= Time.unscaledDeltaTime;

                if (_loadoutProbeTimer < 0.4f && _loadoutProbeTimer + Time.unscaledDeltaTime >= 0.4f)
                {
                    Shot("stage" + _waveManager.StageIndex + "_loadout_panel");
                }

                if (_loadoutProbeTimer < 0f)
                {
                    if (Corebound.UI.Gameplay.WeaponLoadoutPanelPresenter.IsOpen)
                    {
                        _inputReader.DebugRaiseLoadout();
                    }

                    Debug.Log(DebugAutopilot.LogPrefix + " LOADOUT PANEL closed");
                }

                return true;
            }

            // The panel also opens by itself when a weapon arrives with every slot full. Confirm
            // places the weapon; after that the panel stays open and would swallow every further
            // Confirm (buying tier upgrades), and since 2026-09-22 the Intermission countdown
            // pauses while it is open - so leave it the way a player would, with the Loadout key.
            if (Corebound.UI.Gameplay.WeaponLoadoutPanelPresenter.IsOpen
                && _playerCombat != null
                && !_playerCombat.HasPendingWeapon
                && Time.frameCount != Corebound.UI.Gameplay.WeaponLoadoutPanelPresenter.LastToggleFrame)
            {
                _inputReader.DebugRaiseLoadout();
                Debug.Log(DebugAutopilot.LogPrefix + " LOADOUT PANEL closed after placing a pending weapon");
                return true;
            }

            if (_loadoutProbeStage != _waveManager.StageIndex && !Corebound.UI.Gameplay.WeaponLoadoutPanelPresenter.IsOpen)
            {
                _loadoutProbeStage = _waveManager.StageIndex;
                _inputReader.DebugRaiseLoadout();
                _loadoutProbeTimer = 1.2f;
                Debug.Log(DebugAutopilot.LogPrefix + " LOADOUT PANEL opened=" + Corebound.UI.Gameplay.WeaponLoadoutPanelPresenter.IsOpen);
                return true;
            }

            if (_browserProbed)
            {
                return false;
            }

            _browserTimer += Time.deltaTime;

            if (_browserTimer > 7f)
            {
                _browserProbed = true;
                Debug.LogWarning(DebugAutopilot.LogPrefix + " BUILD BROWSER never became active this intermission");
                return false;
            }

            if (_towerService.IsBuildBrowserActive)
            {
                _inputReader.DebugSetMove(Vector2.zero);
                _browserHoldTimer += Time.deltaTime;

                if (_browserHoldTimer > 1f)
                {
                    _browserProbed = true;
                    var selected = _towerService.SelectedBuildTower;
                    Debug.Log(DebugAutopilot.LogPrefix + " BUILD BROWSER active, selected=" + (selected != null ? selected.TowerId : "none"));
                    Shot("stage" + _waveManager.StageIndex + "_wave" + _waveManager.CurrentWaveNumber + "_browser");
                    _inputReader.DebugRaiseInteract();
                }

                return true;
            }

            var target = -1;
            var bestDistance = float.MaxValue;

            for (var index = 0; index < _towerService.SlotCount; index++)
            {
                if (_towerService.IsSlotOccupied(index))
                {
                    continue;
                }

                var distance = ((Vector2)_towerService.GetSlotPosition(index) - (Vector2)_playerTransform.position).sqrMagnitude;

                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    target = index;
                }
            }

            if (target < 0)
            {
                _browserProbed = true;
                return false;
            }

            var offset = (Vector2)_towerService.GetSlotPosition(target) - (Vector2)_playerTransform.position;
            _inputReader.DebugSetMove(offset.sqrMagnitude > 0.04f ? offset.normalized : Vector2.zero);
            return true;
        }

        private void ApplyLoadoutTestOnce()
        {
            if (!_loadoutTest || _loadoutTestApplied || _playerCombat == null)
            {
                return;
            }

            _loadoutTestApplied = true;
            if (SessionState.GetBool(DebugAutopilot.ArsenalTestSessionKey, false))
            {
                _playerCombat.DebugConfigureLoadout("weapon_scorcher", "weapon_cryo", "weapon_railgun", "weapon_nova", "weapon_launcher", "weapon_smg");
            }
            else
            {
                _playerCombat.DebugConfigureLoadout("weapon_rapid", "weapon_rapid", "weapon_rapid", "weapon_spread", "weapon_spread", "weapon_beam");
            }
            Debug.Log(DebugAutopilot.LogPrefix + " loadout test applied: " + DescribeLoadout());
        }

        // B10 (spec review 2026-09-22): WeaponLoadout.md, Waves.md and SkillTree.md all state
        // balancing targets that nothing measured - run length, where the damage comes from, and
        // whether the projectile budget is ever reached. One summary per run makes them checkable.
        private readonly System.Collections.Generic.Dictionary<int, float> _stageSeconds = new System.Collections.Generic.Dictionary<int, float>();
        private int _summaryStageIndex = -1;
        private float _summaryStageStarted;
        private float _damageByPlayer;
        private float _damageByTower;
        private float _damageToPlayerAndCore;
        private int _peakLiveProjectiles;
        private int _enemiesKilled;

        private void HookRunSummary()
        {
            Corebound.Gameplay.Combat.CombatEvents.DamageTaken += OnSummaryDamage;
            Corebound.Gameplay.Combat.CombatEvents.CombatantDied += OnSummaryDeath;
        }

        private void UnhookRunSummary()
        {
            Corebound.Gameplay.Combat.CombatEvents.DamageTaken -= OnSummaryDamage;
            Corebound.Gameplay.Combat.CombatEvents.CombatantDied -= OnSummaryDeath;
        }

        private void OnSummaryDamage(Corebound.Gameplay.Combat.DamageEventArgs args)
        {
            switch (args.DamageContext.SourceType)
            {
                case Corebound.Gameplay.Combat.DamageSourceType.Player:
                    _damageByPlayer += args.DamageContext.Amount;
                    return;
                case Corebound.Gameplay.Combat.DamageSourceType.Tower:
                    _damageByTower += args.DamageContext.Amount;
                    return;
                default:
                    _damageToPlayerAndCore += args.DamageContext.Amount;
                    return;
            }
        }

        private void OnSummaryDeath(Corebound.Gameplay.Combat.DeathEventArgs args)
        {
            _enemiesKilled++;
        }

        // Virtual-pad driving. The device is added once; every frame that changes an axis pushes a
        // full state, because a gamepad state event carries all controls at once.
        private bool UsePad => SessionState.GetBool(DebugAutopilot.PadTestSessionKey, false);

        private Vector2 _padMove;
        private Vector2 _padAim;
        private bool _padFire;

        private void PushPadState()
        {
            Corebound.EditorTools.VirtualGamepadDriver.Hold(_padMove, _padAim, _padFire);
        }

        private void TickRunSummary()
        {
            _peakLiveProjectiles = Mathf.Max(_peakLiveProjectiles, Corebound.Gameplay.Combat.CombatProjectile.ActivePlayerProjectileCount);

            if (_waveManager == null)
            {
                return;
            }

            var stage = _waveManager.StageIndex;

            if (stage == _summaryStageIndex)
            {
                return;
            }

            if (_summaryStageIndex > 0)
            {
                _stageSeconds[_summaryStageIndex] = _elapsed - _summaryStageStarted;
            }

            _summaryStageIndex = stage;
            _summaryStageStarted = _elapsed;
        }

        private void LogRunSummary()
        {
            if (_summaryStageIndex > 0)
            {
                _stageSeconds[_summaryStageIndex] = _elapsed - _summaryStageStarted;
            }

            var builder = new System.Text.StringBuilder(256);
            builder.Append(DebugAutopilot.LogPrefix).Append(" SUMMARY ");
            builder.Append("total=").Append(_elapsed.ToString("0.0")).Append("s");

            foreach (var pair in _stageSeconds)
            {
                builder.Append("  stage").Append(pair.Key).Append('=').Append(pair.Value.ToString("0.0")).Append('s');
            }

            var totalDamage = Mathf.Max(0.01f, _damageByPlayer + _damageByTower);
            builder.Append("  damage[weapons=").Append(_damageByPlayer.ToString("0"))
                .Append(" (").Append((100f * _damageByPlayer / totalDamage).ToString("0")).Append("%)")
                .Append(" towers=").Append(_damageByTower.ToString("0"))
                .Append(" (").Append((100f * _damageByTower / totalDamage).ToString("0")).Append("%)")
                .Append(" taken=").Append(_damageToPlayerAndCore.ToString("0")).Append(']');
            builder.Append("  kills=").Append(_enemiesKilled);
            builder.Append("  peakProjectiles=").Append(_peakLiveProjectiles);

            if (_playerCombat != null)
            {
                builder.Append('/').Append(_playerCombat.ProjectileBudget);
            }

            Debug.Log(builder.ToString());
        }

        private string DescribeLoadout()
        {
            if (_playerCombat == null)
            {
                return "no player";
            }

            var loadout = _playerCombat.Loadout;
            var builder = new StringBuilder(160);
            builder.Append("slots=").Append(loadout.UnlockedSlotCount).Append(" [");

            for (var index = 0; index < loadout.UnlockedSlotCount; index++)
            {
                var slot = loadout.GetSlot(index);

                if (index > 0)
                {
                    builder.Append(' ');
                }

                if (slot.IsEmpty)
                {
                    builder.Append("-");
                    continue;
                }

                builder.Append(slot.Weapon.WeaponId.Replace("weapon_", string.Empty)).Append(":t").Append(slot.Tier)
                    .Append(":a").Append(slot.Ammo == int.MaxValue ? -1 : slot.Ammo).Append('/').Append(slot.MagazineSize);

                if (slot.IsReloading)
                {
                    builder.Append("R");
                }
            }

            builder.Append("] live=").Append(_playerCombat.LiveProjectileCount).Append('/').Append(_playerCombat.ProjectileBudget);
            builder.Append(" sets=");

            for (var index = 0; index < loadout.ActiveSetBonuses.Count; index++)
            {
                var bonus = loadout.ActiveSetBonuses[index];
                builder.Append(bonus.Weapon.WeaponId.Replace("weapon_", string.Empty)).Append('x').Append(bonus.Copies)
                    .Append('=').Append(bonus.Tier != null ? bonus.Tier.DisplayName : "none").Append(' ');
            }

            if (loadout.HasPendingWeapon)
            {
                builder.Append(" pending=").Append(loadout.PendingWeapon.WeaponId);
            }

            var audio = FindFirstObjectByType<Corebound.Gameplay.Weapons.WeaponAudioCuePlayer>();

            if (audio != null)
            {
                builder.Append(" cuesSuppressed=").Append(audio.SuppressedCueCount);
            }

            return builder.ToString();
        }

        private static bool TickShotTimer(ref float timer)
        {
            if (timer < 0f)
            {
                return false;
            }

            timer -= Time.unscaledDeltaTime;
            return timer < 0f;
        }

        private static void Shot(string name)
        {
            var directory = System.IO.Path.Combine(Application.dataPath, "..", "Temp", "AutopilotShots");
            System.IO.Directory.CreateDirectory(directory);
            var path = System.IO.Path.GetFullPath(System.IO.Path.Combine(directory, name + ".png"));
            ScreenCapture.CaptureScreenshot(path);
            Debug.Log(DebugAutopilot.LogPrefix + " SHOT " + path);
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

            _reportBuilder.Append(" loadout[").Append(DescribeLoadout()).Append(']');
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

            // "The text is right" and "the player can see it" are different claims. The shop bug
            // was a CanvasGroup at alpha 0 while every value underneath was correct, so the HUD
            // probe now reports what actually reaches the screen: canvas sort order, group alpha,
            // and whether the label's rect is inside the screen at all.
            var canvas = hudRoot.GetComponentInParent<Canvas>();
            builder.Append(" hudCanvas[order=").Append(canvas != null ? canvas.rootCanvas.sortingOrder : -999);
            builder.Append(" enabled=").Append(canvas != null && canvas.rootCanvas.enabled);

            var group = hudRoot.GetComponentInParent<CanvasGroup>();
            builder.Append(" alpha=").Append(group != null ? group.alpha.ToString("0.##") : "none");
            builder.Append(" scale=").Append(hudRoot.lossyScale.x.ToString("0.##"));
            builder.Append(']');

            AppendScreenRect(builder, hudRoot);

            builder.Append(" hud[");
            AppendHudLabel(builder, hudRoot, "PlayerHealth");
            AppendHudLabel(builder, hudRoot, "CoreHealth");
            AppendHudLabel(builder, hudRoot, "Currency");
            AppendHudLabel(builder, hudRoot, "Wave");
            builder.Append(']');
        }

        // A rect that sits outside the screen explains an invisible HUD better than any amount of
        // guessing at canvas settings.
        private static void AppendScreenRect(StringBuilder builder, Transform hudRoot)
        {
            var rect = hudRoot as RectTransform;

            if (rect == null)
            {
                builder.Append(" hudRect=NOT_RECT");
                return;
            }

            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            var canvas = rect.GetComponentInParent<Canvas>();
            var camera = canvas != null && canvas.rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : Camera.main;

            var min = RectTransformUtility.WorldToScreenPoint(camera, corners[0]);
            var max = RectTransformUtility.WorldToScreenPoint(camera, corners[2]);
            var onScreen = max.x > 0f && max.y > 0f && min.x < Screen.width && min.y < Screen.height;

            builder.Append(" hudRect=").Append(min.ToString("0")).Append("..").Append(max.ToString("0"));
            builder.Append(onScreen ? " ONSCREEN" : " OFFSCREEN");
            builder.Append(" screen=").Append(Screen.width).Append('x').Append(Screen.height);
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

        // After a failed run: photograph the Run Failed overlay, confirm back to the main menu and
        // stay there long enough for [AudioProbe] to report what is still playing (playtest
        // 2026-09-22: two sounds keep playing in the menu after a death).
        private bool _afterRunShot;
        private bool _afterRunConfirmed;

        private float _afterRunTimer;

        private void TickAfterRun()
        {
            // The Run Failed overlay pauses time; this step must not.
            _afterRunTimer += Time.unscaledDeltaTime;

            if (!_afterRunShot && _afterRunTimer > 1.2f)
            {
                _afterRunShot = true;
                Shot("run_failed");
            }

            if (!_afterRunConfirmed && _afterRunTimer > 2.2f && _inputReader != null)
            {
                _afterRunConfirmed = true;
                _inputReader.DebugRaiseConfirm();
                Debug.Log(DebugAutopilot.LogPrefix + " confirmed Run Failed, returning to the main menu");
            }

            if (_afterRunTimer > 16f)
            {
                Debug.Log(DebugAutopilot.LogPrefix + " after-run observation done");
                Finish();
            }
        }

        private void Finish()
        {
            _step = Step.Done;
            Debug.Log(DebugAutopilot.LogPrefix + " finished after " + _elapsed.ToString("0.0") + "s. sawShop=" + _sawShopOpen);
            LogRunSummary();
            SessionState.SetBool(DebugAutopilot.ActiveSessionKey, false);
            EditorApplication.isPlaying = false;
        }
    }
}
