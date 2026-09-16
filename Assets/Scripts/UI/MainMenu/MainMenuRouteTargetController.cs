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
            return _activeTarget != null && _activeTarget.IsOpen && _activeTarget.TryHandleNavigation(navigationInput);
        }

        // Debug-only: [ and ] cycle the stage a fresh "New Run" starts on. Deliberately raw
        // Keyboard polling instead of the InputReader/Input Actions pipeline - this is a
        // development aid that must never consume or compete with real gameplay/menu input
        // bindings, and must compile out cleanly on the rare chance UnityEngine.InputSystem isn't
        // present, which a raw Keyboard check does not risk.
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

            if (keyboard.leftBracketKey.wasPressedThisFrame)
            {
                SetDebugStartStageIndex(_debugStartStageIndex - 1);
            }
            else if (keyboard.rightBracketKey.wasPressedThisFrame)
            {
                SetDebugStartStageIndex(_debugStartStageIndex + 1);
            }
        }

        private static void SetDebugStartStageIndex(int stageIndex)
        {
            _debugStartStageIndex = ((stageIndex - 1 + DebugMaxStageIndex) % DebugMaxStageIndex) + 1;
            NotificationPresenter.Show("Debug: New Run will start on Stage " + _debugStartStageIndex + " ( [ / ] to change)");
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
                case MainMenuRoute.Quit:
                    BeginQuitRoute();
                    return;
            }
        }

        private void BeginNewRunRoute(MainMenuRoute route)
        {
            if (HasContinueRun())
            {
                OpenPanel(_executionData != null ? _executionData.NewRunConfirmationPanelData : null, route);
                return;
            }

            ExecuteImmediateRoute(route);
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