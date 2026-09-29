using System;
using Corebound.MetaProgression;
using Corebound.SaveSystem;
using Corebound.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Corebound.UI.MainMenu
{
    public sealed class MainMenuRouteTargetController : MonoBehaviour, IMainMenuRouteTargetController, IRecordsViewSource
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

            if (_activePanelData != null && _pendingPanelRoute == MainMenuRoute.Records)
            {
                return TryNavigateRecords(navigationInput);
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

            if (_activePanelData != null && _pendingPanelRoute == MainMenuRoute.Records)
            {
                TrySelectRecordsTab(_recordsTab + (direction < 0 ? -1 : 1));
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

        // The Records screen (run history D7-1). Read-only, so it reuses the route panel rather
        // than needing its own canvas and interactive target: a title, a summary and a list of rows
        // is exactly what a history screen is.
        //
        // The first rows are the aggregate totals StatisticsSaveData has been collecting invisibly
        // since it was written - runs started, completed, failed, enemies killed, total playtime -
        // followed by the last runs newest first.
        private string[] BuildRecordLines()
        {
            var history = SaveManager != null ? SaveManager.LoadRunHistorySaveData() : null;
            var statistics = SaveManager != null ? SaveManager.LoadStatisticsSaveData(new SaveSystem.StatisticsSaveData()) : null;
            var lines = new System.Collections.Generic.List<string>();

            if (statistics != null)
            {
                lines.Add(LocalizationService.Resolve("Runs started", "Runs gestartet") + "   " + statistics.TotalRunsStarted
                    + "     " + LocalizationService.Resolve("completed", "abgeschlossen") + "   " + statistics.TotalRunsCompleted
                    + "     " + LocalizationService.Resolve("lost", "verloren") + "   " + statistics.TotalRunsFailed);
                lines.Add(LocalizationService.Resolve("Enemies defeated", "Gegner besiegt") + "   " + statistics.TotalEnemyKills
                    + "     " + LocalizationService.Resolve("Deepest stage", "Tiefste Stage") + "   " + statistics.HighestStageReached
                    + "     " + LocalizationService.Resolve("Playtime", "Spielzeit") + "   "
                    + Corebound.Gameplay.Waves.RunTimerService.Format((float)statistics.TotalPlaytimeSeconds));
            }

            if (history == null || history.RunCount == 0)
            {
                lines.Add(string.Empty);
                lines.Add(LocalizationService.Resolve(
                    "No finished runs yet - a run is recorded when it is won or lost.",
                    "Noch keine beendeten Runs - ein Run wird aufgezeichnet, wenn er gewonnen oder verloren ist."));
                return lines.ToArray();
            }

            var best = history.BestRun;

            if (best != null)
            {
                lines.Add(string.Empty);
                lines.Add(LocalizationService.Resolve("Best", "Bester") + "   "
                    + LocalizationService.Resolve("Stage ", "Stage ") + best.StageReached
                    + LocalizationService.Resolve(", wave ", ", Welle ") + best.WaveReached
                    + "   " + Corebound.Gameplay.Waves.RunTimerService.Format(best.ElapsedActiveSeconds));
            }

            lines.Add(string.Empty);
            var entries = history.Entries;
            var shown = Mathf.Min(entries.Count, RecordsRunRows);

            for (var index = 0; index < shown; index++)
            {
                var entry = entries[index];
                var outcome = entry.Outcome == SaveSystem.RunOutcome.Completed
                    ? LocalizationService.Resolve("Won ", "Gewonnen ")
                    : LocalizationService.Resolve("Lost", "Verloren");

                lines.Add(entry.StartedUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm")
                    + "   " + outcome
                    + "   " + LocalizationService.Resolve("Stage ", "Stage ") + entry.StageReached
                    + LocalizationService.Resolve(" wave ", " Welle ") + entry.WaveReached
                    + "   " + Corebound.Gameplay.Waves.RunTimerService.Format(entry.ElapsedActiveSeconds)
                    + "   " + entry.EnemiesKilled + LocalizationService.Resolve(" kills", " Kills")
                    + (string.IsNullOrEmpty(entry.EndedByLabel) ? string.Empty : "   " + entry.EndedByLabel)
                    + DescribeRunWeapons(entry));
            }

            return lines.ToArray();
        }

        private const int RecordsRunRows = 10;

        // Playtest 2026-09-29: "Records should contain which weapons were used too." The history
        // entry has carried the loadout since D6; it was simply never printed.
        private string DescribeRunWeapons(SaveSystem.RunHistoryEntry entry)
        {
            var ids = entry != null ? entry.WeaponIds : null;

            if (ids == null || ids.Count == 0)
            {
                return string.Empty;
            }

            var definition = _metaProgressionService != null ? _metaProgressionService.DefinitionData : null;
            var catalog = definition != null ? definition.UpgradeWeapons : null;
            var builder = new System.Text.StringBuilder();

            for (var index = 0; index < ids.Count; index++)
            {
                if (string.IsNullOrEmpty(ids[index]))
                {
                    continue;
                }

                var weapon = catalog != null ? catalog.FindById(ids[index]) : null;
                var tier = entry.WeaponTiers != null && index < entry.WeaponTiers.Count ? entry.WeaponTiers[index] : 0;
                builder.Append(builder.Length == 0 ? "   ·   " : ", ");
                builder.Append(weapon != null ? weapon.DisplayName : ids[index]);

                // Tier 0 is the Common copy; only an upgraded one is worth the extra characters.
                if (tier > 0)
                {
                    builder.Append(" (").Append(TierName(tier)).Append(")");
                }
            }

            return builder.ToString();
        }

        // Same names as the in-run loadout panel, so a run reads the same in both places.
        private static string TierName(int tier)
        {
            switch ((Corebound.Gameplay.Cards.CardRarity)Mathf.Clamp(tier, 0, 3))
            {
                case Corebound.Gameplay.Cards.CardRarity.Rare:
                    return LocalizationService.Resolve("Rare", "Selten");
                case Corebound.Gameplay.Cards.CardRarity.Epic:
                    return LocalizationService.Resolve("Epic", "Episch");
                case Corebound.Gameplay.Cards.CardRarity.Legendary:
                    return LocalizationService.Resolve("Legendary", "Legendär");
                default:
                    return LocalizationService.Resolve("Common", "Gewöhnlich");
            }
        }

        // ---- Records: card collection (playtest 2026-09-29) -----------------------------------
        //
        // "I would like to have in Records an overview of all cards - blurred if not collected,
        // clear if collected once." Two tabs: the run history as before, and a grid of every card
        // in the pool. The state lives here so controller, keyboard and mouse agree about it; the
        // route panel presenter only draws it.

        public const int RecordsCardColumns = 11;

        private readonly System.Collections.Generic.List<RecordsCardEntry> _recordsCards =
            new System.Collections.Generic.List<RecordsCardEntry>();

        private int _recordsTab;
        private int _recordsCardIndex;

        public MainMenuRoute? ActivePanelRoute => _activePanelData != null ? _pendingPanelRoute : null;

        public int RecordsTab => _recordsTab;

        public System.Collections.Generic.IReadOnlyList<RecordsCardEntry> RecordsCards => _recordsCards;

        public int RecordsSelectedCard => _recordsTab == 1 && _recordsCards.Count > 0 ? _recordsCardIndex : -1;

        public int RecordsCollectedCount
        {
            get
            {
                var count = 0;

                for (var index = 0; index < _recordsCards.Count; index++)
                {
                    if (_recordsCards[index].Collected)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        public bool TrySelectRecordsTab(int tab)
        {
            var clamped = Mathf.Clamp(tab, 0, 1);

            if (_activePanelData == null || _pendingPanelRoute != MainMenuRoute.Records || clamped == _recordsTab)
            {
                return false;
            }

            _recordsTab = clamped;
            RouteTargetChanged?.Invoke();
            return true;
        }

        public bool TrySelectRecordsCard(int index)
        {
            if (_recordsTab != 1 || index < 0 || index >= _recordsCards.Count || index == _recordsCardIndex)
            {
                return false;
            }

            _recordsCardIndex = index;
            RouteTargetChanged?.Invoke();
            return true;
        }

        private bool TryNavigateRecords(Vector2 navigationInput)
        {
            if (_recordsTab != 1 || _recordsCards.Count == 0)
            {
                return false;
            }

            var step = 0;

            if (Mathf.Abs(navigationInput.x) >= 0.5f)
            {
                step = navigationInput.x > 0f ? 1 : -1;
            }
            else if (Mathf.Abs(navigationInput.y) >= 0.5f)
            {
                step = navigationInput.y > 0f ? -RecordsCardColumns : RecordsCardColumns;
            }

            var next = _recordsCardIndex + step;
            return step != 0 && next >= 0 && next < _recordsCards.Count && TrySelectRecordsCard(next);
        }

        // "Collected" is the union of the new per-profile record and everything the profile could
        // already prove before it existed: cards in any history entry and in the run in progress.
        // Without the backfill every existing profile would open onto a wall of blurred cards.
        private void RefreshRecordsCards()
        {
            _recordsCards.Clear();
            var definition = _metaProgressionService != null ? _metaProgressionService.DefinitionData : null;
            var pool = definition != null ? definition.CardCollection : null;

            if (pool == null || pool.Cards == null)
            {
                return;
            }

            var collected = new System.Collections.Generic.HashSet<string>();
            var statistics = SaveManager != null ? SaveManager.LoadStatisticsSaveData(new SaveSystem.StatisticsSaveData()) : null;
            AddAll(collected, statistics != null ? statistics.CollectedCardIds : null);
            var history = SaveManager != null ? SaveManager.LoadRunHistorySaveData() : null;

            if (history != null && history.Entries != null)
            {
                for (var index = 0; index < history.Entries.Count; index++)
                {
                    AddAll(collected, history.Entries[index] != null ? history.Entries[index].CardIds : null);
                }
            }

            var runSaveData = SaveManager != null ? SaveManager.LoadRunSaveData() : null;

            if (runSaveData != null && runSaveData.HasActiveRun)
            {
                AddAll(collected, runSaveData.SelectedCardIds);
            }

            for (var index = 0; index < pool.Cards.Length; index++)
            {
                var card = pool.Cards[index];

                if (card != null)
                {
                    _recordsCards.Add(new RecordsCardEntry(card, collected.Contains(card.CardId)));
                }
            }

            // Rarity bands read as rows of one colour; the name inside a band is stable.
            _recordsCards.Sort((left, right) =>
            {
                var byRarity = left.Card.Rarity.CompareTo(right.Card.Rarity);
                return byRarity != 0 ? byRarity : string.CompareOrdinal(left.Card.DisplayName, right.Card.DisplayName);
            });
        }

        private static void AddAll(System.Collections.Generic.HashSet<string> set, System.Collections.Generic.IReadOnlyList<string> ids)
        {
            if (ids == null)
            {
                return;
            }

            for (var index = 0; index < ids.Count; index++)
            {
                if (!string.IsNullOrEmpty(ids[index]))
                {
                    set.Add(ids[index]);
                }
            }
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

                if (_pendingPanelRoute == MainMenuRoute.Records)
                {
                    // The Cards tab draws a grid instead of rows.
                    return _recordsTab == 0 ? BuildRecordLines() : System.Array.Empty<string>();
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

        // Autopilot: open a route exactly as confirming its menu entry would.
        public void DebugOpenRoute(MainMenuRoute route)
        {
            OnRouteConfirmed(route);
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

            // Records is read-only: Confirm must not close it the way it closes a confirmation
            // panel, or a mouse click on a card would throw the player back to the menu.
            if (route == MainMenuRoute.Records)
            {
                return true;
            }

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
                case MainMenuRoute.Records:
                    _recordsTab = 0;
                    _recordsCardIndex = 0;
                    RefreshRecordsCards();
                    OpenPanel(_executionData != null ? _executionData.RecordsPanelData : null, MainMenuRoute.Records);
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
                else if (route == MainMenuRoute.NewRun)
                {
                    ApplyPreferredStartWeapon();

                    if (IsDebugModeAvailable && _debugStartStageIndex > 1)
                    {
                        ApplyDebugStageOverride();
                    }
                }

                RecordRunStarted(route);
            }

            var sceneName = ResolveNewRunSceneName(route);

            if (string.IsNullOrWhiteSpace(sceneName))
            {
                return;
            }

            SceneManager.LoadScene(sceneName);
        }

        // Playtest 2026-09-29, third report: "choosing the start weapon still does not work".
        //
        // The 2026-09-28 fix stopped a purchased start loadout from overwriting the choice, and was
        // correct as far as it went - but RunSaveBootstrap_Default itself carries
        // _equippedWeaponId: weapon_rapid, so every fresh run save already had Rapid in slot 0
        // before any loadout was considered. PlayerCombatController only consults the preferred
        // weapon when no slot is filled, so the choice was never read at all.
        //
        // The menu is where the choice is made, so the menu now writes it into the run save
        // directly instead of hoping the combat controller's fallback is reached. The bootstrap
        // weapon stays as the default for a profile that has never picked one.
        private void ApplyPreferredStartWeapon()
        {
            if (!HasPreferredStartWeapon())
            {
                return;
            }

            var definition = _metaProgressionService.DefinitionData;
            var saveData = SaveManager.LoadMetaProgressionSaveData(definition.CreateDefaultSaveData());
            var runSaveData = SaveManager.LoadRunSaveData();

            if (saveData == null || runSaveData == null)
            {
                return;
            }

            runSaveData.SetEquippedWeaponId(saveData.PreferredStartWeaponId);
            SaveManager.SaveRunSaveData(runSaveData);
        }

        // Records showed "runs started: 0" on every profile - RecordRunStarted existed and nothing
        // called it. The tutorial is excluded for the same reason it is excluded from the history.
        private void RecordRunStarted(MainMenuRoute route)
        {
            if (route == MainMenuRoute.Tutorial)
            {
                return;
            }

            var statistics = SaveManager.LoadStatisticsSaveData(new SaveSystem.StatisticsSaveData());

            if (statistics != null)
            {
                statistics.RecordRunStarted();
                SaveManager.SaveStatisticsSaveData(statistics);
            }
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
        // True when the profile has chosen a starting weapon that it is actually allowed to use.
        // Mirrors PlayerCombatController.ResolveWeaponFromMetaProgressionSave so the menu and the
        // run agree about what "the player chose something" means.
        private bool HasPreferredStartWeapon()
        {
            var definition = _metaProgressionService != null ? _metaProgressionService.DefinitionData : null;
            var catalog = definition != null ? definition.UpgradeWeapons : null;

            if (SaveManager == null || definition == null || catalog == null)
            {
                return false;
            }

            var saveData = SaveManager.LoadMetaProgressionSaveData(definition.CreateDefaultSaveData());
            var preferred = saveData != null ? catalog.FindById(saveData.PreferredStartWeaponId) : null;
            return preferred != null && MetaProgression.StartWeaponOptions.IsAvailable(preferred, saveData, definition);
        }

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

            // Playtest 2026-09-28: "New Run -> select weapon. The selected weapon is not in the
            // player's hand, it is always Rapid."
            //
            // A purchased start loadout used to stamp its own weapon into the fresh run save here,
            // which fills slot 0 - and PlayerCombatController.LoadLoadout only consults the
            // player's preferred start weapon when *no* slot is filled. So the moment any start
            // loadout was unlocked, the New Run choice became decorative. B11 already settled that
            // the player's own choice wins and licences only widen the pool; this makes the two
            // paths agree. The loadout's cards and towers still apply - only the weapon defers.
            if (!string.IsNullOrWhiteSpace(loadout.StartingWeaponId) && !HasPreferredStartWeapon())
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