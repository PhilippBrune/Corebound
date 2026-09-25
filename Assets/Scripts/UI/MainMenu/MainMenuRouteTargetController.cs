using System;
using Corebound.MetaProgression;
using Corebound.SaveSystem;
using Corebound.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Corebound.UI.MainMenu
{
    public sealed class MainMenuRouteTargetController : MonoBehaviour, IMainMenuRouteTargetController
    {
        [SerializeField] private MainMenuController _mainMenuController;
        [SerializeField] private MainMenuRouteExecutionData _executionData;
        [SerializeField] private UnityEngine.Object _saveManagerSource;
        [SerializeField] private UnityEngine.Object _settingsTargetSource;
        [SerializeField] private UnityEngine.Object _metaProgressionTargetSource;
        [SerializeField] private MetaProgressionService _metaProgressionService;

        // Debug stage-select (2026-09-16 playtest request): lets a "New Run" start directly on a
        // chosen stage instead of always Stage 1, so Stage 2+ content is reachable without playing
        // through everything before it every time. Only active in the Editor or a debug build
        // (Debug.isDebugBuild) - never visible or reachable in a shipping build. WaveManager.
        // ResolveStageDataForSave() safely falls back to Stage 1 for any index that doesn't resolve
        // to a real StageData asset, so cycling past however many stages actually exist is harmless.
        private const int DebugMaxStageIndex = 5;
        private static int _debugStartStageIndex = 1;

        private static bool IsDebugModeAvailable => Application.isEditor || Debug.isDebugBuild;

        private IMainMenuInteractiveRouteTarget _activeTarget;
        private MainMenuRoutePanelData _activePanelData;
        private MainMenuRoute? _pendingPanelRoute;

        public event Action RouteTargetChanged;

        public bool IsRouteTargetOpen => (_activeTarget != null && _activeTarget.IsOpen) || _activePanelData != null;

        public MainMenuRoutePanelData ActivePanelData => _activePanelData;

        private ISaveManager SaveManager => SaveManagerResolver.Resolve(_saveManagerSource);

        private IMainMenuInteractiveRouteTarget SettingsTarget => _settingsTargetSource as IMainMenuInteractiveRouteTarget;

        private IMainMenuInteractiveRouteTarget MetaProgressionTarget => _metaProgressionTargetSource as IMainMenuInteractiveRouteTarget;

        public bool TryHandleNavigation(Vector2 navigationInput)
        {
            if (_activeTarget != null && _activeTarget.IsOpen)
            {
                return _activeTarget.TryHandleNavigation(navigationInput);
            }

            // The profile panel is the one panel with a selection in it (SaveSystem.md, "Drei
            // Profile"). Everything else the route panel shows is static text.
            if (_activePanelData == null || Mathf.Abs(navigationInput.y) < 0.5f)
            {
                return false;
            }

            var step = navigationInput.y > 0f ? -1 : 1;

            if (_pendingPanelRoute == MainMenuRoute.Profile)
            {
                var nextProfile = Mathf.Clamp(_profileRowIndex + step, 0, ProfileSelection.ProfileCount - 1);

                if (nextProfile == _profileRowIndex)
                {
                    return false;
                }

                _profileRowIndex = nextProfile;
                _profileDeleteArmedIndex = -1;
                RouteTargetChanged?.Invoke();
                return true;
            }

            if (_pendingPanelRoute == MainMenuRoute.NewRun && _startWeaponOptions.Count > 0)
            {
                var rowCount = _startWeaponOptions.Count + (OffersTutorialRow ? 1 : 0);
                var nextWeapon = Mathf.Clamp(_startWeaponRowIndex + step, 0, rowCount - 1);

                if (nextWeapon == _startWeaponRowIndex)
                {
                    return false;
                }

                _startWeaponRowIndex = nextWeapon;
                RouteTargetChanged?.Invoke();
                return true;
            }

            return false;
        }

        private int _profileRowIndex;
        private int _profileDeleteArmedIndex = -1;

        public bool TrySelectPanelRow(int index)
        {
            if (_activePanelData == null || index < 0)
            {
                return false;
            }

            if (_pendingPanelRoute == MainMenuRoute.Profile)
            {
                if (index >= ProfileSelection.ProfileCount || index == _profileRowIndex)
                {
                    return false;
                }

                _profileRowIndex = index;
                _profileDeleteArmedIndex = -1;
                RouteTargetChanged?.Invoke();
                return true;
            }

            if (_pendingPanelRoute == MainMenuRoute.NewRun)
            {
                var rowCount = _startWeaponOptions.Count + (OffersTutorialRow ? 1 : 0);

                if (index >= rowCount || index == _startWeaponRowIndex)
                {
                    return false;
                }

                _startWeaponRowIndex = index;
                RouteTargetChanged?.Invoke();
                return true;
            }

            return false;
        }

        public int ActivePanelSelectedIndex
        {
            get
            {
                if (_activePanelData == null)
                {
                    return -1;
                }

                if (_pendingPanelRoute == MainMenuRoute.Profile)
                {
                    return _profileRowIndex;
                }

                return _pendingPanelRoute == MainMenuRoute.NewRun && _startWeaponOptions.Count > 0 ? _startWeaponRowIndex : -1;
            }
        }

        // Deleting a profile is two presses on purpose: the first arms it, the second does it, and
        // moving the selection disarms it again.
        public bool TryHandleTabSwitch(int direction)
        {
            // On the profile panel the same key is "delete", because there is nothing to switch.
            if (_activePanelData != null && _pendingPanelRoute == MainMenuRoute.Profile)
            {
                if (direction < 0)
                {
                    OnProfileDeletePressed();
                }

                return true;
            }

            if (_activeTarget is Corebound.Settings.SettingsRouteTargetController settings && settings.IsOpen)
            {
                settings.SwitchTab(direction);
                return true;
            }

            if (_activeTarget is MetaProgression.MetaProgressionRouteTargetController progression && progression.IsOpen)
            {
                progression.SwitchTab(direction);
                return true;
            }

            return false;
        }

        private void OnProfileDeletePressed()
        {
            if (_activePanelData == null || _pendingPanelRoute != MainMenuRoute.Profile)
            {
                return;
            }

            if (_profileDeleteArmedIndex != _profileRowIndex)
            {
                _profileDeleteArmedIndex = _profileRowIndex;
                RouteTargetChanged?.Invoke();
                return;
            }

            ProfileSelection.DeleteProfile(_profileRowIndex + 1);
            _profileDeleteArmedIndex = -1;
            RouteTargetChanged?.Invoke();
        }

        // B11: the starting-weapon list, plus the overwrite warning when there is a run to lose.
        // True when this profile has never finished the tutorial: the guided run is then offered
        // as the first row rather than forced (playtest 2026-09-23, "for a clean profile the player
        // should be asked if he wants to play the tutorial").
        private bool OffersTutorialRow => _metaProgressionService != null && !_metaProgressionService.HasSeenTutorial;

        private string[] BuildStartWeaponLines()
        {
            if (_startWeaponOptions.Count == 0)
            {
                RefreshStartWeaponOptions();
            }

            var tutorialRow = OffersTutorialRow ? 1 : 0;
            var warning = HasContinueRun() ? 1 : 0;
            var lines = new string[_startWeaponOptions.Count + tutorialRow + warning];

            if (tutorialRow > 0)
            {
                lines[0] = LocalizationService.Resolve(
                    "Guided first run (tutorial)   -   learn the loop; pays no Skill Points or Alloy",
                    "Geführter erster Run (Tutorial)   -   lernt den Ablauf; zahlt keine Skillpunkte und keine Legierung");
            }

            for (var index = 0; index < _startWeaponOptions.Count; index++)
            {
                var weapon = _startWeaponOptions[index];
                // Name plus one short stat line: the full summary wrapped into three lines per
                // weapon and the rows ran into each other (playtest 2026-09-22, "New Run window not
                // readable").
                lines[index + tutorialRow] = weapon != null ? weapon.DisplayName + "   " + DescribeStartWeapon(weapon) : "?";
            }

            if (warning > 0)
            {
                lines[lines.Length - 1] = LocalizationService.Resolve(
                    "Starting a new run overwrites the run you can continue.",
                    "Ein neuer Run überschreibt den fortsetzbaren Run.");
            }

            return lines;
        }

        // The rows the route panel draws while the profile panel is open; null for every other
        // panel, which keeps showing its authored text.
        public string[] ActivePanelDetailLinesOverride
        {
            get
            {
                if (_activePanelData == null)
                {
                    return null;
                }

                if (_pendingPanelRoute == MainMenuRoute.NewRun)
                {
                    return BuildStartWeaponLines();
                }

                if (_pendingPanelRoute != MainMenuRoute.Profile)
                {
                    return null;
                }

                var lines = new string[ProfileSelection.ProfileCount];

                for (var index = 0; index < lines.Length; index++)
                {
                    var summary = ProfileSelection.Summarize(index + 1);
    
                    var active = index + 1 == ProfileSelection.ActiveIndex
                        ? LocalizationService.Resolve("  (active)", "  (aktiv)")
                        : string.Empty;

                    var detail = !summary.IsUsed
                        ? LocalizationService.Resolve("empty", "leer")
                        : LocalizationService.Resolve("Ascension ", "Aufstieg ") + summary.AscensionRank
                            + "  ·  " + summary.Alloy + LocalizationService.Resolve(" Alloy", " Legierung")
                            + "  ·  " + summary.SkillPoints + " SP"
                            + (summary.HasRun
                                ? LocalizationService.Resolve("  ·  run in stage ", "  ·  Run in Stage ") + summary.RunStageIndex
                                : string.Empty);

                    var armed = index == _profileDeleteArmedIndex
                        ? LocalizationService.Resolve("   [Q again to delete]", "   [Q erneut zum Löschen]")
                        : string.Empty;
                    lines[index] = LocalizationService.Resolve("Profile ", "Profil ") + (index + 1) + active + "  -  " + detail + armed;
                }

                return lines;
            }
        }

        // Debug-only: F9/F10 cycle the stage a fresh "New Run" starts on. Deliberately raw
        // Keyboard polling instead of the InputReader/Input Actions pipeline - this is a
        // development aid that must never consume or compete with real gameplay/menu input
        // bindings, and must compile out cleanly on the rare chance UnityEngine.InputSystem isn't
        // present, which a raw Keyboard check does not risk.
        //
        // Was [ and ] until 2026-09-19, which made this feature effectively non-existent for this
        // project's own developer: Keyboard.leftBracketKey/rightBracketKey address a US-layout
        // *physical* key position, which on the German QWERTZ keyboard actually in use is the "ü"
        // and "+" keys - so the advertised "[ / ]" hint named keys that are not on the keyboard.
        // Function keys occupy the same physical position on every layout, so F9/F10 are reachable
        // and printable regardless of locale.
        private void Update()
        {
            if (!IsDebugModeAvailable || _activeTarget != null)
            {
                return;
            }

            var keyboard = UnityEngine.InputSystem.Keyboard.current;

            if (keyboard == null)
            {
                return;
            }

            if (keyboard.f9Key.wasPressedThisFrame)
            {
                SetDebugStartStageIndex(_debugStartStageIndex - 1);
            }
            else if (keyboard.f10Key.wasPressedThisFrame)
            {
                SetDebugStartStageIndex(_debugStartStageIndex + 1);
            }
        }

        // The Main Menu scene has no NotificationPresenter in it (that lives in the gameplay
        // scene), so the toast this used to rely on silently did nothing here - the debug mode gave
        // no feedback at all and read as "not available". Drawing the state directly makes it
        // self-evident that the mode exists, which key changes it, and what it is currently set to.
        private void OnGUI()
        {
            if (!IsDebugModeAvailable)
            {
                return;
            }

            const float width = 430f;
            const float height = 24f;
            var rect = new Rect(8f, Screen.height - height - 8f, width, height);
            var previousColor = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.55f);
            GUI.Box(rect, GUIContent.none);
            GUI.color = previousColor;
            GUI.Label(
                new Rect(rect.x + 8f, rect.y + 3f, width - 16f, height),
                "DEBUG  ·  New Run starts on Stage " + _debugStartStageIndex + " of " + DebugMaxStageIndex + "  ·  F9 / F10 to change");
        }

#if UNITY_EDITOR
        // Editor-only seam for DebugAutopilot, which cannot press F9/F10 because synthetic OS
        // input does not reach Unity's Input System. Sets exactly the same static the keys set.
        public static int DebugStartStageIndex => _debugStartStageIndex;

        public static void DebugSelectStartStageIndex(int stageIndex)
        {
            SetDebugStartStageIndex(stageIndex);
        }
#endif

        private static void SetDebugStartStageIndex(int stageIndex)
        {
            _debugStartStageIndex = ((stageIndex - 1 + DebugMaxStageIndex) % DebugMaxStageIndex) + 1;
            NotificationPresenter.Show("Debug: New Run will start on Stage " + _debugStartStageIndex + " (F9 / F10 to change)");
        }

        private void OnEnable()
        {
            if (_mainMenuController == null)
            {
                enabled = false;
                return;
            }

            HookTarget(SettingsTarget);
            HookTarget(MetaProgressionTarget);
            _mainMenuController.RouteConfirmed += OnRouteConfirmed;
        }

        private void OnDisable()
        {
            if (_mainMenuController == null)
            {
                return;
            }

            _mainMenuController.RouteConfirmed -= OnRouteConfirmed;
            UnhookTarget(SettingsTarget);
            UnhookTarget(MetaProgressionTarget);
        }

        public bool TryHandleConfirm()
        {
            if (_activeTarget != null && _activeTarget.IsOpen)
            {
                return _activeTarget.TryHandleConfirm();
            }

            if (_activePanelData == null || !_pendingPanelRoute.HasValue)
            {
                return false;
            }

            var route = _pendingPanelRoute.Value;

            if (route == MainMenuRoute.Profile)
            {
                ProfileSelection.SetActive(_profileRowIndex + 1);
                CloseActivePanel();
                return true;
            }

            if (route == MainMenuRoute.NewRun)
            {
                if (OffersTutorialRow && _startWeaponRowIndex == 0)
                {
                    CloseActivePanel();
                    ExecuteImmediateRoute(MainMenuRoute.Tutorial);
                    return true;
                }

                SavePreferredStartWeapon();
            }

            CloseActivePanel();
            ExecuteImmediateRoute(route);
            return true;
        }

        public bool TryHandleCancel()
        {
            if (_activeTarget != null && _activeTarget.IsOpen)
            {
                return _activeTarget.TryHandleCancel();
            }

            return TryCloseActivePanel();
        }

        private void OnRouteConfirmed(MainMenuRoute route)
        {
            switch (route)
            {
                case MainMenuRoute.Continue:
                    TryLoadContinueScene();
                    return;
                case MainMenuRoute.NewRun:
                case MainMenuRoute.Tutorial:
                    BeginNewRunRoute(route);
                    return;
                case MainMenuRoute.Settings:
                    OpenTarget(SettingsTarget);
                    return;
                case MainMenuRoute.MetaProgression:
                    OpenTarget(MetaProgressionTarget);
                    return;
                case MainMenuRoute.Profile:
                    _profileRowIndex = ProfileSelection.ActiveIndex - 1;
                    OpenPanel(_executionData != null ? _executionData.ProfilePanelData : null, MainMenuRoute.Profile);
                    return;
                case MainMenuRoute.Quit:
                    BeginQuitRoute();
                    return;
            }
        }

        private void BeginNewRunRoute(MainMenuRoute route)
        {
            // B11: the New Run panel is where the starting weapon is picked, so it opens whether or
            // not there is a run to overwrite. The Tutorial keeps its old straight-to-scene path -
            // a guided first run should not start with a loadout decision.
            if (route == MainMenuRoute.NewRun)
            {
                _startWeaponRowIndex = ResolvePreferredStartWeaponRow();
                OpenPanel(_executionData != null ? _executionData.NewRunConfirmationPanelData : null, route);
                return;
            }

            if (HasContinueRun())
            {
                OpenPanel(_executionData != null ? _executionData.NewRunConfirmationPanelData : null, route);
                return;
            }

            ExecuteImmediateRoute(route);
        }

        private readonly System.Collections.Generic.List<Corebound.Gameplay.Weapons.WeaponData> _startWeaponOptions =
            new System.Collections.Generic.List<Corebound.Gameplay.Weapons.WeaponData>();

        private int _startWeaponRowIndex;

        private void RefreshStartWeaponOptions()
        {
            var definition = _metaProgressionService != null ? _metaProgressionService.DefinitionData : null;
            var catalog = definition != null ? definition.UpgradeWeapons : null;
            var saveData = SaveManager != null && definition != null
                ? SaveManager.LoadMetaProgressionSaveData(definition.CreateDefaultSaveData())
                : null;
            StartWeaponOptions.Collect(catalog, saveData, definition, _startWeaponOptions);
        }

        // "3.4 dmg · 0.85 s · 4 rounds" - enough to compare, short enough for one line.
        private static string DescribeStartWeapon(Corebound.Gameplay.Weapons.WeaponData weapon)
        {
            var damage = weapon.DamageProfile != null ? weapon.DamageProfile.BaseDamage : 0f;
            var pattern = weapon.AttackPattern;

            if (pattern == null)
            {
                return damage.ToString("0.##") + LocalizationService.Resolve(" dmg", " Sch");
            }

            return damage.ToString("0.##") + LocalizationService.Resolve(" dmg  ·  every ", " Sch  ·  alle ")
                + pattern.FireIntervalSeconds.ToString("0.##") + "s  ·  "
                + pattern.MagazineSize + LocalizationService.Resolve(" rounds", " Schuss");
        }

        private int ResolvePreferredStartWeaponRow()
        {
            RefreshStartWeaponOptions();
            var definition = _metaProgressionService != null ? _metaProgressionService.DefinitionData : null;
            var saveData = SaveManager != null && definition != null
                ? SaveManager.LoadMetaProgressionSaveData(definition.CreateDefaultSaveData())
                : null;
            var preferred = saveData != null ? saveData.PreferredStartWeaponId : string.Empty;

            var offset = OffersTutorialRow ? 1 : 0;

            for (var index = 0; index < _startWeaponOptions.Count; index++)
            {
                if (_startWeaponOptions[index] != null && _startWeaponOptions[index].WeaponId == preferred)
                {
                    return index + offset;
                }
            }

            return 0;
        }

        private void SavePreferredStartWeapon()
        {
            var definition = _metaProgressionService != null ? _metaProgressionService.DefinitionData : null;

            if (SaveManager == null || definition == null)
            {
                return;
            }

            var saveData = SaveManager.LoadMetaProgressionSaveData(definition.CreateDefaultSaveData());

            if (saveData == null)
            {
                return;
            }

            var weaponIndex = _startWeaponRowIndex - (OffersTutorialRow ? 1 : 0);

            if (weaponIndex < 0 || weaponIndex >= _startWeaponOptions.Count)
            {
                return;
            }

            var weapon = _startWeaponOptions[weaponIndex];
            saveData.SetPreferredStartWeaponId(weapon != null ? weapon.WeaponId : string.Empty);
            SaveManager.SaveMetaProgressionSaveData(saveData);
        }

        private void BeginQuitRoute()
        {
            OpenPanel(_executionData != null ? _executionData.QuitConfirmationPanelData : null, MainMenuRoute.Quit);
        }

        private void TryLoadContinueScene()
        {
            var continueSceneName = _executionData != null ? _executionData.ContinueSceneName : string.Empty;
            var runSaveData = SaveManager != null ? SaveManager.LoadRunSaveData() : null;

            if (runSaveData != null && runSaveData.HasActiveRun && !string.IsNullOrWhiteSpace(runSaveData.SceneName))
            {
                continueSceneName = runSaveData.SceneName;
            }

            if (string.IsNullOrWhiteSpace(continueSceneName))
            {
                return;
            }

            SceneManager.LoadScene(continueSceneName);
        }

        private void OpenPanel(MainMenuRoutePanelData panelData, MainMenuRoute pendingRoute)
        {
            if (panelData == null)
            {
                ExecuteImmediateRoute(pendingRoute);
                return;
            }

            if (_activeTarget != null && _activeTarget.IsOpen)
            {
                _activeTarget.Close();
                _activeTarget = null;
            }

            _activePanelData = panelData;
            _pendingPanelRoute = pendingRoute;
            RouteTargetChanged?.Invoke();
        }

        private void OpenTarget(IMainMenuInteractiveRouteTarget target)
        {
            if (target == null)
            {
                return;
            }

            if (_activeTarget != null && _activeTarget != target)
            {
                _activeTarget.Close();
            }

            _activePanelData = null;
            _activeTarget = target;
            target.Open();
            RouteTargetChanged?.Invoke();
        }

        private bool TryCloseActivePanel()
        {
            if (_activePanelData == null)
            {
                return false;
            }

            CloseActivePanel();
            return true;
        }

        private void CloseActivePanel()
        {
            _activePanelData = null;
            _pendingPanelRoute = null;
            RouteTargetChanged?.Invoke();
        }

        private void HookTarget(IMainMenuInteractiveRouteTarget target)
        {
            if (target != null)
            {
                target.TargetChanged += OnTargetChanged;
            }
        }

        private void UnhookTarget(IMainMenuInteractiveRouteTarget target)
        {
            if (target != null)
            {
                target.TargetChanged -= OnTargetChanged;
            }
        }

        private void OnTargetChanged()
        {
            if (_activeTarget != null && !_activeTarget.IsOpen)
            {
                _activeTarget = null;
            }

            RouteTargetChanged?.Invoke();
        }

        private void ExecuteImmediateRoute(MainMenuRoute route)
        {
            switch (route)
            {
                case MainMenuRoute.NewRun:
                case MainMenuRoute.Tutorial:
                    StartNewRun(route);
                    return;
                case MainMenuRoute.Quit:
                    QuitApplication();
                    return;
            }
        }

        private void StartNewRun(MainMenuRoute route)
        {
            if (SaveManager != null)
            {
                SaveManager.ResetRunSaveDataToBootstrap();
                ApplyUnlockedStartLoadout();

                if (route == MainMenuRoute.Tutorial)
                {
                    var runSaveData = SaveManager.LoadRunSaveData();

                    if (runSaveData != null)
                    {
                        runSaveData.SetTutorialRun(true);
                        SaveManager.SaveRunSaveData(runSaveData);
                    }
                }
                else if (route == MainMenuRoute.NewRun && IsDebugModeAvailable && _debugStartStageIndex > 1)
                {
                    ApplyDebugStageOverride();
                }
            }

            var sceneName = ResolveNewRunSceneName(route);

            if (string.IsNullOrWhiteSpace(sceneName))
            {
                return;
            }

            SceneManager.LoadScene(sceneName);
        }

        // See the class-level comment on _debugStartStageIndex. WaveManager.ResolveStageDataForSave()
        // does the actual index->StageData resolution (against StageProgressionData.Stages) once
        // the gameplay scene loads; this just stamps the chosen index onto the fresh save so that
        // resolution has something other than the bootstrap default (always 1) to work with.
        private void ApplyDebugStageOverride()
        {
            var runSaveData = SaveManager.LoadRunSaveData();

            if (runSaveData == null)
            {
                return;
            }

            runSaveData.SetStageIndex(_debugStartStageIndex);
            SaveManager.SaveRunSaveData(runSaveData);
        }

        // Picks the last (most recently unlocked) start loadout the player has purchased, if
        // any, and seeds the fresh RunSaveData with it - otherwise a new run always used the
        // static RunSaveBootstrapData regardless of what the player had unlocked in the Meta
        // Progression menu.
        private void ApplyUnlockedStartLoadout()
        {
            if (_metaProgressionService == null)
            {
                return;
            }

            var unlockedLoadouts = _metaProgressionService.GetUnlockedStartLoadouts();

            if (unlockedLoadouts == null || unlockedLoadouts.Count == 0)
            {
                return;
            }

            var loadout = unlockedLoadouts[unlockedLoadouts.Count - 1];

            if (loadout == null)
            {
                return;
            }

            var runSaveData = SaveManager.LoadRunSaveData();

            if (runSaveData == null)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(loadout.StartingWeaponId))
            {
                runSaveData.SetEquippedWeaponId(loadout.StartingWeaponId);
            }

            if (loadout.StartingCardIds != null && loadout.StartingCardIds.Length > 0)
            {
                runSaveData.SetSelectedCardIds(loadout.StartingCardIds);
            }

            if (loadout.StartingTowerIds != null && loadout.StartingTowerIds.Length > 0)
            {
                var upgradeLevels = new int[loadout.StartingTowerIds.Length];
                runSaveData.SetBuiltTowerIds(loadout.StartingTowerIds);
                runSaveData.SetBuiltTowerUpgradeLevels(upgradeLevels);
            }

            SaveManager.SaveRunSaveData(runSaveData);
        }

        private string ResolveNewRunSceneName(MainMenuRoute route)
        {
            if (_executionData == null)
            {
                return string.Empty;
            }

            if (route == MainMenuRoute.Tutorial && !string.IsNullOrWhiteSpace(_executionData.TutorialSceneName))
            {
                return _executionData.TutorialSceneName;
            }

            if (!string.IsNullOrWhiteSpace(_executionData.NewRunSceneName))
            {
                return _executionData.NewRunSceneName;
            }

            return _executionData.ContinueSceneName;
        }

        private bool HasContinueRun()
        {
            var runSaveData = SaveManager != null ? SaveManager.LoadRunSaveData() : null;
            return runSaveData != null && runSaveData.HasActiveRun;
        }

        private static void QuitApplication()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}