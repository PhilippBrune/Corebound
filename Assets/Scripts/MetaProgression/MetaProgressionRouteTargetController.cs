using System;
using System.Collections.Generic;
using System.Text;
using Corebound.Gameplay.Towers;
using Corebound.Gameplay.Weapons;
using Corebound.Input;
using Corebound.MetaProgression.SkillTree;
using Corebound.SaveSystem;
using Corebound.UI;
using Corebound.UI.MainMenu;
using UnityEngine;

namespace Corebound.MetaProgression
{
    // The main menu's Progression route (2026-09-22). One route, seven tabs:
    //
    //   Unlocks | Upgrades | Combat | Construction | Industry | Command | Ascension
    //
    // SkillTree.md asks for its own route and Upgrades.md for a route with two tabs; one route
    // with tabs is fewer menu entries and keeps "one branch on screen at a time" (each branch is a
    // tab). Left/Right switches tab, Up/Down moves, Confirm buys. Nothing here decides whether a
    // purchase is allowed - SkillTreeRules and UpgradeRules do; this only lays out rows and says,
    // in words, why something cannot be bought.
    public sealed class MetaProgressionRouteTargetController : MonoBehaviour, IMainMenuInteractiveRouteTarget
    {
        public enum RowState
        {
            Owned,
            Available,
            Unaffordable,
            Locked,
            Header,
            Action
        }

        private enum Tab
        {
            Unlocks,
            Upgrades,
            Branch,
            Ascension,
            Quests
        }

        private readonly List<DisplayRow> _rows = new List<DisplayRow>();
        private readonly List<RowCommand> _commands = new List<RowCommand>();
        private readonly List<string> _tabNames = new List<string>();
        private readonly StringBuilder _builder = new StringBuilder(256);

        [SerializeField] private MetaProgressionService _metaProgressionService;
        [SerializeField] private InputAssistData _inputAssistData;

        private float NavigationThreshold => _inputAssistData != null ? _inputAssistData.MenuNavigateThreshold : InputAssistData.DefaultMenuNavigateThreshold;

        private bool _isOpen;
        private int _tabIndex;
        private int _selectedRow;
        private bool _respecArmed;
        private string _statusMessage = string.Empty;

        public event Action TargetChanged;

        public bool IsOpen => _isOpen;

        public MainMenuRoute Route => MainMenuRoute.MetaProgression;

        public IReadOnlyList<DisplayRow> Rows => _rows;

        public string Title => LocalizationService.Resolve("Progression", "Fortschritt");

        public string TabStrip => BuildTabStrip();

        public string CurrencyLabel
        {
            get
            {
                if (_metaProgressionService == null)
                {
                    return string.Empty;
                }

                var alloyName = _metaProgressionService.DefinitionData != null ? _metaProgressionService.DefinitionData.MetaCurrencyLabel : "Alloy";
                return alloyName + ": " + _metaProgressionService.AlloyBalance
                    + "     " + SkillPointText
                    + "     " + AscensionText;
            }
        }

        // The header's parts, so the presenter can put the Alloy and Skill Point icons (Assets 92,
        // 93) next to their own numbers.
        public string AlloyText => _metaProgressionService == null ? string.Empty
            : (_metaProgressionService.DefinitionData != null ? _metaProgressionService.DefinitionData.MetaCurrencyLabel : "Alloy") + ": " + _metaProgressionService.AlloyBalance;

        // K3-2 (CHARACTER_CLASSES_2026-09-30): Skill Points are earned by the account but spent per
        // class, so the header names whose tree and whose balance this is. The class is picked on
        // the New Run panel.
        public string SkillPointText => _metaProgressionService == null ? string.Empty
            : LocalizationService.Resolve("Skill Points", "Fertigkeitspunkte") + ActiveClassSuffix + ": " + _metaProgressionService.SkillPointsAvailable;

        private string ActiveClassSuffix
        {
            get
            {
                var definition = _metaProgressionService.DefinitionData;
                var save = _metaProgressionService.SaveData;

                if (definition == null || save == null || definition.Characters == null || definition.Characters.Length < 2)
                {
                    return string.Empty;
                }

                var character = definition.FindCharacter(save.SelectedCharacterId);
                return character != null ? " (" + character.DisplayName + ")" : string.Empty;
            }
        }

        // Level until the cap, then Paragon level and the Ascension Points it has paid.
        public string AscensionText
        {
            get
            {
                var save = _metaProgressionService != null ? _metaProgressionService.SaveData : null;

                if (save == null)
                {
                    return string.Empty;
                }

                return save.IsAtLevelCap
                    ? "Paragon " + save.ParagonLevel + "   " + save.AscensionPointsAvailable + LocalizationService.Resolve(" AP", " AP")
                    : LocalizationService.Resolve("Level ", "Stufe ") + save.Level + " / " + MetaProgressionSaveData.MaxLevel;
            }
        }

        // 0-3 while a skill branch tab is shown (the branch icon sheet's order), otherwise -1.
        // Every tab shows an emblem, not only the four branches (playtest 2026-09-22). Unlocks,
        // Upgrades and Ascension borrow icons that already exist rather than waiting for new art.
        public string ActiveTabIconPath
        {
            get
            {
                switch (ResolveTab(out _))
                {
                    case Tab.Unlocks:
                        return "Assets/Art/Resources/resource_alloy_icon.png";
                    case Tab.Upgrades:
                        return "Assets/Art/UI/HUD/weapon_icon_sheet.png";
                    case Tab.Ascension:
                        return "Assets/Art/UI/SkillTree/ascension_rank_badge.png";
                    case Tab.Quests:
                        return "Assets/Art/Resources/resource_skill_point_icon.png";
                    default:
                        return string.Empty;
                }
            }
        }

        // True while a skill branch tab is showing, which is when the node graph is worth
        // drawing. Exposed so SkillTreeGraphPresenter can follow the tab without owning it -
        // selection and purchasing stay here, where they already work.
        public bool IsSkillBranchTabActive => ResolveTab(out _) == Tab.Branch;

        // The save the graph reads node state from. Exposed rather than re-loaded so the graph
        // and the rows can never disagree about what is bought.
        public SaveSystem.MetaProgressionSaveData SkillSaveData =>
            _metaProgressionService != null ? _metaProgressionService.SaveData : null;

        public SkillTreeDefinitionData ActiveSkillTree =>
            _metaProgressionService != null ? _metaProgressionService.ActiveSkillTree : null;

        // The Upgrades icon is one cell of the weapon sheet; everything else is a whole file.
        public bool ActiveTabIconIsWeaponSheet => ResolveTab(out _) == Tab.Upgrades;

        public int ActiveBranchIndex
        {
            get
            {
                if (_metaProgressionService == null || _metaProgressionService.ActiveSkillTree == null)
                {
                    return -1;
                }

                return ResolveTab(out var branch) == Tab.Branch ? System.Array.IndexOf(_metaProgressionService.ActiveSkillTree.Branches, branch) : -1;
            }
        }

        public string Summary
        {
            get
            {
                var description = _selectedRow >= 0 && _selectedRow < _commands.Count ? _commands[_selectedRow].Description : string.Empty;
                var status = string.IsNullOrEmpty(_statusMessage) ? string.Empty : "\n<color=#FFC75A>" + _statusMessage + "</color>";
                return description + status;
            }
        }

        public string FooterHint => IsSkillBranchTabActive
            ? LocalizationService.Resolve(
                "Stick / arrows: move on the tree   Q / Tab, LB / RB: branch   Confirm or click: buy   Cancel: back",
                "Stick / Pfeile: im Baum bewegen   Q / Tab, LB / RB: Zweig   Bestätigen oder Klick: kaufen   Abbrechen: zurück")
            : LocalizationService.Resolve(
                "Left/Right, Q / Tab: tab   Up/Down: select   Confirm or click: buy   Cancel: back",
                "Links/Rechts, Q / Tab: Reiter   Hoch/Runter: wählen   Bestätigen oder Klick: kaufen   Abbrechen: zurück");

        // ---- Presenter surface (playtest 2026-09-29) ----------------------------------------------
        //
        // "Check that I can click with the mouse everywhere. The Progression tabs are not
        // clickable." The tab strip was one rich-text string, so there was nothing to click. The
        // presenter now draws one label per tab and asks for these.

        public IReadOnlyList<string> TabNames
        {
            get
            {
                BuildTabStrip();
                return _tabNames;
            }
        }

        public int ActiveTabIndex => _tabIndex;

        public int SelectedRow => _selectedRow;

        public bool SelectTab(int tabIndex)
        {
            if (!_isOpen || tabIndex < 0 || tabIndex >= ResolveTabCount() || tabIndex == _tabIndex)
            {
                return false;
            }

            SwitchTab(tabIndex - _tabIndex);
            return true;
        }

        // Mouse hover on a row or node. Headers are information, not choices, so they are skipped
        // exactly as the keyboard skips them.
        public bool TrySelectRow(int rowIndex)
        {
            if (!_isOpen || rowIndex < 0 || rowIndex >= _commands.Count || rowIndex == _selectedRow)
            {
                return false;
            }

            if (_commands[rowIndex].State == RowState.Header)
            {
                return false;
            }

            _selectedRow = rowIndex;
            _respecArmed = false;
            _statusMessage = string.Empty;
            RebuildRows();
            TargetChanged?.Invoke();
            return true;
        }

        public int FindGraphRow(int keyIndex, int subIndex)
        {
            for (var index = 0; index < _commands.Count; index++)
            {
                if (_commands[index].GraphKey == keyIndex && _commands[index].GraphSub == subIndex && keyIndex >= 0)
                {
                    return index;
                }
            }

            return -1;
        }

        public bool TryGetSelectedGraphNode(out int keyIndex, out int subIndex)
        {
            keyIndex = -1;
            subIndex = -1;

            if (_selectedRow < 0 || _selectedRow >= _commands.Count || _commands[_selectedRow].GraphKey < 0)
            {
                return false;
            }

            keyIndex = _commands[_selectedRow].GraphKey;
            subIndex = _commands[_selectedRow].GraphSub;
            return true;
        }

        // The branch tab's one row that is not a node: Respec. The presenter draws it as a
        // button under the details column.
        public int RespecRowIndex => IsSkillBranchTabActive && _commands.Count > 0 && _commands[_commands.Count - 1].GraphKey < 0
            ? _commands.Count - 1
            : -1;

        public string SelectedLabel => _selectedRow >= 0 && _selectedRow < _commands.Count ? _commands[_selectedRow].Label.Trim() : string.Empty;

        public string SelectedStatus => _selectedRow >= 0 && _selectedRow < _commands.Count ? _commands[_selectedRow].Status : string.Empty;

        public RowState SelectedState => _selectedRow >= 0 && _selectedRow < _commands.Count ? _commands[_selectedRow].State : RowState.Header;

        // Nearest node of the active branch in the pushed direction, within a 50 degree cone.
        // Down from the outermost tier reaches Respec; any direction from Respec returns to the
        // node the player left.
        private int _lastGraphRow = -1;

        private bool TryNavigateGraph(Vector2 direction)
        {
            var tree = _metaProgressionService.ActiveSkillTree;
            var branchIndex = ActiveBranchIndex;

            if (tree == null || branchIndex < 0 || _commands.Count == 0)
            {
                return false;
            }

            if (_selectedRow == RespecRowIndex)
            {
                var back = _lastGraphRow >= 0 && _lastGraphRow < _commands.Count ? _lastGraphRow : 0;
                return direction.y > 0f && TrySelectRow(back);
            }

            if (!TryResolveGraphPosition(tree, branchIndex, _selectedRow, out var from))
            {
                return false;
            }

            var best = -1;
            var bestScore = float.MaxValue;

            for (var index = 0; index < _commands.Count; index++)
            {
                if (index == _selectedRow || !TryResolveGraphPosition(tree, branchIndex, index, out var to))
                {
                    continue;
                }

                var delta = to - from;
                var along = Vector2.Dot(delta, direction);

                if (along <= 1f)
                {
                    continue;
                }

                var sideways = Mathf.Abs(direction.x * delta.y - direction.y * delta.x);

                if (sideways > along * 1.2f)
                {
                    continue;
                }

                var score = along + sideways * 2f;

                if (score < bestScore)
                {
                    bestScore = score;
                    best = index;
                }
            }

            if (best >= 0)
            {
                _lastGraphRow = best;
                return TrySelectRow(best);
            }

            if (direction.y < 0f && RespecRowIndex >= 0)
            {
                _lastGraphRow = _selectedRow;
                return TrySelectRow(RespecRowIndex);
            }

            return false;
        }

        private bool TryResolveGraphPosition(SkillTreeDefinitionData tree, int branchIndex, int rowIndex, out Vector2 position)
        {
            position = Vector2.zero;

            if (rowIndex < 0 || rowIndex >= _commands.Count || _commands[rowIndex].GraphKey < 0)
            {
                return false;
            }

            var branch = tree.Branches[branchIndex];
            var keyIndex = _commands[rowIndex].GraphKey;

            if (branch == null || branch.KeyNodes == null || keyIndex > branch.KeyNodes.Length)
            {
                return false;
            }

            // One past the last key node is the Ascension ring.
            if (keyIndex == branch.KeyNodes.Length)
            {
                tree.CollectAscensionBonuses(branch.BranchId, _ringBuffer);
                position = SkillTreeGraphLayout.RingNode(branchIndex, tree.Branches.Length, branch.KeyNodes.Length, _commands[rowIndex].GraphSub, _ringBuffer.Count);
                return true;
            }

            var keyPosition = SkillTreeGraphLayout.KeyNode(branch.KeyNodes[keyIndex], branchIndex, tree.Branches.Length, keyIndex);
            var subIndex = _commands[rowIndex].GraphSub;
            position = subIndex < 0 ? keyPosition : SkillTreeGraphLayout.SubNode(keyPosition, subIndex);
            return true;
        }

        private void OnEnable()
        {
            if (_metaProgressionService != null)
            {
                _metaProgressionService.ProgressionChanged += OnProgressionChanged;
            }
        }

        private void OnDisable()
        {
            if (_metaProgressionService != null)
            {
                _metaProgressionService.ProgressionChanged -= OnProgressionChanged;
            }
        }

        public void Open()
        {
            _isOpen = true;
            _selectedRow = 0;
            _respecArmed = false;
            _statusMessage = string.Empty;
            RebuildRows();
            TargetChanged?.Invoke();
        }

        public void Close()
        {
            _isOpen = false;
            TargetChanged?.Invoke();
        }

        public bool TryHandleNavigation(Vector2 navigationInput)
        {
            if (!_isOpen || _metaProgressionService == null || _metaProgressionService.DefinitionData == null)
            {
                return false;
            }

            var horizontal = Mathf.Abs(navigationInput.x) >= NavigationThreshold && Mathf.Abs(navigationInput.x) > Mathf.Abs(navigationInput.y);
            var vertical = Mathf.Abs(navigationInput.y) >= NavigationThreshold && Mathf.Abs(navigationInput.y) >= Mathf.Abs(navigationInput.x);

            // N3: on a branch tab the stick moves across the graph. Only when there is nothing
            // further in that direction does Left/Right fall through to switching tab, so the
            // screen still reads the way the list taught.
            if ((horizontal || vertical) && IsSkillBranchTabActive && TryNavigateGraph(horizontal
                    ? new Vector2(Mathf.Sign(navigationInput.x), 0f)
                    : new Vector2(0f, Mathf.Sign(navigationInput.y))))
            {
                return true;
            }

            if (horizontal)
            {
                var tabCount = Mathf.Max(1, ResolveTabCount());
                _tabIndex = (_tabIndex + (navigationInput.x > 0f ? 1 : -1) + tabCount) % tabCount;
                _selectedRow = 0;
                _respecArmed = false;
                _statusMessage = string.Empty;
                RebuildRows();
                SkipToSelectableRow(1);
                TargetChanged?.Invoke();
                return true;
            }

            if (!vertical || _commands.Count == 0 || IsSkillBranchTabActive)
            {
                return false;
            }

            var direction = navigationInput.y > 0f ? -1 : 1;
            _selectedRow = (_selectedRow + direction + _commands.Count) % _commands.Count;
            SkipToSelectableRow(direction);
            _respecArmed = false;
            _statusMessage = string.Empty;
            RebuildRows();
            TargetChanged?.Invoke();
            return true;
        }

        public bool TryHandleConfirm()
        {
            if (!_isOpen || _metaProgressionService == null || _selectedRow < 0 || _selectedRow >= _commands.Count)
            {
                return false;
            }

            var result = _commands[_selectedRow].Execute != null && _commands[_selectedRow].Execute();

            if (result)
            {
                _metaProgressionService.Commit();
            }

            RebuildRows();
            TargetChanged?.Invoke();
            return result;
        }

        public bool TryHandleCancel()
        {
            if (!_isOpen)
            {
                return false;
            }

            Close();
            return true;
        }

        private void OnProgressionChanged()
        {
            RebuildRows();
            TargetChanged?.Invoke();
        }

        // Tabs: Unlocks, Upgrades, one per skill branch, Ascension, Quests.
        private int ResolveTabCount()
        {
            var tree = _metaProgressionService != null ? _metaProgressionService.ActiveSkillTree : null;
            return 4 + (tree != null && tree.Branches != null ? tree.Branches.Length : 0);
        }

        // Same shoulder-button switch as every other tabbed screen; Left/Right still works too.
        public void SwitchTab(int direction)
        {
            if (!_isOpen)
            {
                return;
            }

            var tabCount = Mathf.Max(1, ResolveTabCount());
            _tabIndex = (_tabIndex + direction + tabCount) % tabCount;
            _selectedRow = 0;
            _respecArmed = false;
            _statusMessage = string.Empty;
            RebuildRows();
            SkipToSelectableRow(1);
            TargetChanged?.Invoke();
        }

        private Tab ResolveTab(out SkillBranchData branch)
        {
            branch = null;
            var tree = _metaProgressionService.ActiveSkillTree;
            var branchCount = tree != null && tree.Branches != null ? tree.Branches.Length : 0;
            _tabIndex = Mathf.Clamp(_tabIndex, 0, 3 + branchCount);

            if (_tabIndex == 0)
            {
                return Tab.Unlocks;
            }

            if (_tabIndex == 1)
            {
                return Tab.Upgrades;
            }

            if (_tabIndex - 2 < branchCount)
            {
                branch = tree.Branches[_tabIndex - 2];
                return Tab.Branch;
            }

            return _tabIndex == 2 + branchCount ? Tab.Ascension : Tab.Quests;
        }

        private string BuildTabStrip()
        {
            if (_metaProgressionService == null)
            {
                return string.Empty;
            }

            _tabNames.Clear();
            _tabNames.Add(LocalizationService.Resolve("Unlocks", "Freischaltungen"));
            _tabNames.Add(LocalizationService.Resolve("Upgrades", "Verbesserungen"));
            var tree = _metaProgressionService.ActiveSkillTree;

            if (tree != null && tree.Branches != null)
            {
                for (var index = 0; index < tree.Branches.Length; index++)
                {
                    _tabNames.Add(tree.Branches[index] != null ? tree.Branches[index].DisplayName : "?");
                }
            }

            _tabNames.Add(LocalizationService.Resolve("Ascension", "Aufstieg"));
            _tabNames.Add(LocalizationService.Resolve("Quests", "Quests"));
            _builder.Clear();

            for (var index = 0; index < _tabNames.Count; index++)
            {
                if (index > 0)
                {
                    _builder.Append("   ");
                }

                _builder.Append(index == _tabIndex ? "<color=#FFC75A>[" + _tabNames[index] + "]</color>" : _tabNames[index]);
            }

            return _builder.ToString();
        }

        private void SkipToSelectableRow(int direction)
        {
            for (var guard = 0; guard < _commands.Count; guard++)
            {
                if (_selectedRow >= 0 && _selectedRow < _commands.Count && _commands[_selectedRow].State != RowState.Header)
                {
                    break;
                }

                _selectedRow = (_selectedRow + direction + _commands.Count) % _commands.Count;
            }

            RebuildRows();
        }

        private void RebuildRows()
        {
            _commands.Clear();

            if (_metaProgressionService == null || _metaProgressionService.DefinitionData == null)
            {
                _rows.Clear();
                return;
            }

            switch (ResolveTab(out var branch))
            {
                case Tab.Unlocks:
                    BuildUnlockRows();
                    break;
                case Tab.Upgrades:
                    BuildUpgradeRows();
                    break;
                case Tab.Branch:
                    BuildBranchRows(branch);
                    break;
                case Tab.Ascension:
                    BuildAscensionRows();
                    break;
                default:
                    BuildQuestRows();
                    break;
            }

            _selectedRow = Mathf.Clamp(_selectedRow, 0, Mathf.Max(0, _commands.Count - 1));
            _rows.Clear();

            for (var index = 0; index < _commands.Count; index++)
            {
                var command = _commands[index];
                _rows.Add(new DisplayRow(command.Label, command.Status, index == _selectedRow, command.State == RowState.Owned, command.State));
            }
        }

        // ---- Unlocks (Alloy) --------------------------------------------------------------------

        private void BuildUnlockRows()
        {
            var unlocks = _metaProgressionService.DefinitionData.Unlocks;

            if (unlocks == null)
            {
                return;
            }

            AddWeaponSlotSourceRows();

            for (var index = 0; index < unlocks.Length; index++)
            {
                var unlock = unlocks[index];
                var unlockIndex = index;
                var owned = _metaProgressionService.IsUnlocked(unlock.UnlockId);
                var affordable = _metaProgressionService.AlloyBalance >= unlock.Cost;
                _commands.Add(new RowCommand(
                    unlock.Title + "  ·  " + unlock.TargetSystem,
                    owned ? LocalizationService.Resolve("Owned", "Besessen") : unlock.Cost + " Alloy",
                    owned ? RowState.Owned : (affordable ? RowState.Available : RowState.Unaffordable),
                    unlock.Summary + (owned || affordable ? string.Empty : "\n" + LocalizationService.Resolve("Not enough Alloy.", "Nicht genug Legierung.")),
                    () => !owned && _metaProgressionService.TryPurchase(unlockIndex)));
            }
        }

        // Playtest 2026-09-22: "I can see how to unlock the 5th and 6th weapon slot, but how do I
        // unlock the 2nd, 3rd and 4th?" Slots 2-4 are not bought here, so they had no row at all.
        // They are listed as information (not selectable) above the Alloy unlocks, each with its
        // source and whether it is already earned.
        private void AddWeaponSlotSourceRows()
        {
            var save = _metaProgressionService.SaveData;
            var tree = _metaProgressionService.ActiveSkillTree;

            AddSlotSourceRow(
                LocalizationService.Resolve("Second Weapon Mount", "Zweite Waffenhalterung"),
                save.HasClearedAnyStage,
                LocalizationService.Resolve("Clear any stage once (beat its boss).", "Schließe einmal eine beliebige Stage ab (besiege ihren Boss)."));
            AddSlotSourceRow(
                LocalizationService.Resolve("Third Weapon Mount", "Dritte Waffenhalterung"),
                tree != null && save.GetSkillNodeRank(tree.CharacterId, "combat_armory") > 0,
                LocalizationService.Resolve("Skill tree, Combat branch: Armory.", "Fertigkeitsbaum, Zweig Kampf: Waffenkammer."));
            AddSlotSourceRow(
                LocalizationService.Resolve("Fourth Weapon Mount", "Vierte Waffenhalterung"),
                tree != null && save.GetSkillNodeRank(tree.CharacterId, "combat_full_arsenal") > 0,
                LocalizationService.Resolve("Skill tree, Combat branch: Full Arsenal.", "Fertigkeitsbaum, Zweig Kampf: Volles Arsenal."));
        }

        private void AddSlotSourceRow(string name, bool owned, string source)
        {
            _commands.Add(new RowCommand(
                name + "  ·  " + source,
                owned ? LocalizationService.Resolve("Owned", "Besessen") : LocalizationService.Resolve("Not yet", "Noch nicht"),
                owned ? RowState.Owned : RowState.Header,
                source,
                null));
        }

        // ---- Upgrades (Alloy, per weapon and per tower) -------------------------------------------

        private void BuildUpgradeRows()
        {
            var definition = _metaProgressionService.DefinitionData;
            var catalog = definition.UpgradeCatalog;
            var save = _metaProgressionService.SaveData;

            if (catalog == null)
            {
                return;
            }

            var weapons = definition.UpgradeWeapons != null ? definition.UpgradeWeapons.Weapons : null;

            if (weapons != null)
            {
                for (var index = 0; index < weapons.Length; index++)
                {
                    var weapon = weapons[index];

                    if (weapon == null)
                    {
                        continue;
                    }

                    var unlocked = definition.IsWeaponUnlocked(save, weapon);
                    AddItemHeader(weapon.DisplayName, unlocked, definition.FindUnlockFor(weapon, null));
                    AddTrackRows(catalog, UpgradeTargetKind.Weapon, weapon.WeaponId, unlocked, stat => DescribeWeaponValue(weapon, stat));
                }
            }

            var towers = definition.UpgradeTowers != null ? definition.UpgradeTowers.Towers : null;

            if (towers != null)
            {
                for (var index = 0; index < towers.Length; index++)
                {
                    var tower = towers[index];

                    if (tower == null)
                    {
                        continue;
                    }

                    var unlocked = string.IsNullOrWhiteSpace(tower.RequiredUnlockId) || save.HasPurchased(tower.RequiredUnlockId);
                    AddItemHeader(tower.DisplayName, unlocked, definition.FindUnlockFor(null, tower));
                    AddTrackRows(catalog, UpgradeTargetKind.Tower, tower.TowerId, unlocked, stat => DescribeTowerValue(tower, stat));
                }
            }
        }

        private void AddItemHeader(string name, bool unlocked, MetaProgressionDefinitionData.UnlockDefinition unlock)
        {
            _commands.Add(new RowCommand(
                "<b>" + name + "</b>",
                unlocked ? string.Empty : LocalizationService.Resolve("Locked", "Gesperrt") + (unlock != null ? " (" + unlock.Title + ", " + unlock.Cost + " Alloy)" : string.Empty),
                RowState.Header,
                string.Empty,
                null));
        }

        private void AddTrackRows(UpgradeCatalogData catalog, UpgradeTargetKind kind, string itemId, bool unlocked, Func<UpgradableStat, float> baseValue)
        {
            var save = _metaProgressionService.SaveData;

            for (var index = 0; index < catalog.Tracks.Length; index++)
            {
                var track = catalog.Tracks[index];

                if (track == null || track.AppliesTo != kind)
                {
                    continue;
                }

                // A weapon with no spread has nothing for Accuracy to narrow; a track that cannot
                // change anything would be a trap for Alloy. Same rule as the mastery quest.
                if (kind == UpgradeTargetKind.Weapon && track.Stat == UpgradableStat.Accuracy && baseValue(track.Stat) <= 0f)
                {
                    continue;
                }

                var rank = save.GetUpgradeRank(itemId, (int)track.Stat);
                var result = UpgradeRules.Evaluate(save, track, itemId, unlocked);
                var pips = BuildPips(rank, track.MaxRank);
                var current = ApplyTrack(track, baseValue(track.Stat), rank);
                var next = ApplyTrack(track, baseValue(track.Stat), Mathf.Min(rank + 1, track.MaxRank));

                // Upgrades.md: show the resulting number, not the percentage.
                var values = rank >= track.MaxRank ? FormatValue(track.Stat, current) : FormatValue(track.Stat, current) + " → " + FormatValue(track.Stat, next);
                var status = result == UpgradePurchaseResult.MaxRank
                    ? LocalizationService.Resolve("Max", "Max")
                    : track.CostForRank(rank + 1) + " Alloy";
                var state = result == UpgradePurchaseResult.MaxRank ? RowState.Owned
                    : result == UpgradePurchaseResult.Purchased ? RowState.Available
                    : result == UpgradePurchaseResult.Locked ? RowState.Locked
                    : RowState.Unaffordable;
                var reason = result == UpgradePurchaseResult.Locked
                    ? LocalizationService.Resolve("Unlock this item first.", "Schalte diesen Gegenstand zuerst frei.")
                    : result == UpgradePurchaseResult.InsufficientAlloy
                        ? LocalizationService.Resolve("Not enough Alloy.", "Nicht genug Legierung.")
                        : string.Empty;

                _commands.Add(new RowCommand(
                    "    " + track.DisplayName + "  " + pips + "  " + values,
                    status,
                    state,
                    track.DisplayName + " " + rank + "/" + track.MaxRank + ": " + values
                        + LocalizationService.Resolve("\nPermanent - upgrades are never refunded.", "\nDauerhaft - Verbesserungen werden nie erstattet.")
                        + (reason.Length > 0 ? "\n" + reason : string.Empty),
                    () => UpgradeRules.TryPurchase(save, track, itemId, unlocked) == UpgradePurchaseResult.Purchased));
            }
        }

        private static string BuildPips(int rank, int maxRank)
        {
            var builder = new StringBuilder(maxRank + 24);
            builder.Append("<color=#FFC75A>");

            for (var index = 0; index < maxRank; index++)
            {
                if (index == rank)
                {
                    builder.Append("</color><color=#5C6470>");
                }

                builder.Append('■');
            }

            builder.Append("</color>");
            return builder.ToString();
        }

        private static float ApplyTrack(UpgradeTrackData track, float baseValue, int rank)
        {
            var total = track.TotalValueAtRank(rank);

            switch (track.Stat)
            {
                case UpgradableStat.Magazine:
                    return baseValue + total;
                case UpgradableStat.CriticalChance:
                    return baseValue + total * 100f;
                default:
                    return baseValue * (1f + total);
            }
        }

        private static string FormatValue(UpgradableStat stat, float value)
        {
            switch (stat)
            {
                case UpgradableStat.Magazine:
                    return Mathf.RoundToInt(value).ToString();
                case UpgradableStat.CriticalChance:
                    return value.ToString("0.#") + "%";
                case UpgradableStat.CriticalDamage:
                    return "x" + value.ToString("0.00");
                case UpgradableStat.FireSpeed:
                case UpgradableStat.ReloadSpeed:
                    return value.ToString("0.00") + "s";
                case UpgradableStat.Accuracy:
                    return value.ToString("0.#") + "°";
                case UpgradableStat.BuildDiscount:
                    return Mathf.RoundToInt(value).ToString();
                default:
                    return value.ToString("0.#");
            }
        }

        private static float DescribeWeaponValue(WeaponData weapon, UpgradableStat stat)
        {
            var pattern = weapon.AttackPattern;

            switch (stat)
            {
                case UpgradableStat.Damage:
                    return weapon.DamageProfile != null ? weapon.DamageProfile.BaseDamage : 1f;
                case UpgradableStat.FireSpeed:
                    return pattern != null ? pattern.FireIntervalSeconds : 0.2f;
                case UpgradableStat.Magazine:
                    return pattern != null ? pattern.MagazineSize : 0f;
                case UpgradableStat.ReloadSpeed:
                    return pattern != null ? pattern.ReloadSeconds : 1f;
                case UpgradableStat.Accuracy:
                    return pattern != null ? pattern.SpreadAngleDegrees : 0f;
                case UpgradableStat.CriticalChance:
                    return 0f;
                case UpgradableStat.CriticalDamage:
                    return 2f;
                default:
                    return 0f;
            }
        }

        private static float DescribeTowerValue(TowerData tower, UpgradableStat stat)
        {
            switch (stat)
            {
                case UpgradableStat.Damage:
                    return tower.DamageProfile != null ? tower.DamageProfile.BaseDamage : 1f;
                case UpgradableStat.FireSpeed:
                    return tower.FireIntervalSeconds;
                case UpgradableStat.Range:
                    return tower.Range;
                case UpgradableStat.BuildDiscount:
                    return tower.BuildCost;
                default:
                    return 0f;
            }
        }

        // ---- Skill tree branches (Skill Points) ----------------------------------------------------

        private void BuildBranchRows(SkillBranchData branch)
        {
            var tree = _metaProgressionService.ActiveSkillTree;
            var save = _metaProgressionService.SaveData;

            if (tree == null || branch == null || branch.KeyNodes == null)
            {
                return;
            }

            var branchSpend = SkillTreeRules.ResolveBranchSpend(save, tree, branch);

            for (var keyIndex = 0; keyIndex < branch.KeyNodes.Length; keyIndex++)
            {
                var keyNode = branch.KeyNodes[keyIndex];

                if (keyNode == null)
                {
                    continue;
                }

                var keyState = SkillTreeRules.ResolveKeyNodeState(save, tree, branch, keyNode);
                var capturedKey = keyNode;
                _commands.Add(new RowCommand(
                    "<b>" + keyNode.DisplayName + "</b>",
                    DescribeNodeStatus(keyState, keyNode.Cost),
                    ToRowState(keyState),
                    keyNode.Description + "\n" + DescribeKeyNodeLock(keyState, keyNode, tree, branchSpend),
                    () => SkillTreeRules.TryPurchaseKeyNode(save, tree, branch, capturedKey),
                    keyIndex,
                    -1));

                if (keyNode.SubNodes == null)
                {
                    continue;
                }

                for (var subIndex = 0; subIndex < keyNode.SubNodes.Length; subIndex++)
                {
                    var subNode = keyNode.SubNodes[subIndex];

                    if (subNode == null)
                    {
                        continue;
                    }

                    var rank = save.GetSkillNodeRank(tree.CharacterId, subNode.NodeId);
                    var subState = SkillTreeRules.ResolveSubNodeState(save, tree, keyNode, subNode);
                    var capturedSub = subNode;
                    _commands.Add(new RowCommand(
                        "    " + subNode.DisplayName + "  " + rank + "/" + subNode.MaxRank,
                        DescribeNodeStatus(subState, subNode.CostPerRank),
                        ToRowState(subState),
                        subNode.Description + (subState == SkillNodeState.LockedParent
                            ? "\n" + LocalizationService.Resolve("Requires ", "Benötigt ") + keyNode.DisplayName + "."
                            : string.Empty),
                        () => SkillTreeRules.TryPurchaseSubNode(save, tree, capturedKey, capturedSub),
                        keyIndex,
                        subIndex));
                }
            }

            AddAscensionRingRows(tree, branch, save);

            // Respec: full only, from the main menu only, confirm twice (SkillTree.md).
            var refund = SkillTreeRules.ResolveTotalTreeSpend(save, tree);
            var respecFree = SkillTreeRules.IsNextRespecFree(save, tree);
            var respecPrice = respecFree ? LocalizationService.Resolve("free", "gratis") : tree.PaidRespecAlloyCost + " Alloy";
            _commands.Add(new RowCommand(
                _respecArmed
                    ? LocalizationService.Resolve("Confirm again to reset the whole tree", "Zum Zurücksetzen des ganzen Baums erneut bestätigen")
                    : LocalizationService.Resolve("Respec tree", "Baum zurücksetzen"),
                LocalizationService.Resolve("Refund ", "Erstattung ") + refund + " SP  ·  " + respecPrice,
                refund > 0 ? RowState.Action : RowState.Locked,
                LocalizationService.Resolve(
                    "Refunds every Skill Point spent in the tree, across all branches. Ascension ranks stay. The first respec per tree version is free; after that it costs ",
                    "Erstattet alle im Baum ausgegebenen Fertigkeitspunkte, über alle Zweige. Aufstiegsränge bleiben. Der erste Reset pro Baumversion ist gratis, danach kostet er ")
                    + tree.PaidRespecAlloyCost + " Alloy.",
                () =>
                {
                    if (refund <= 0)
                    {
                        return false;
                    }

                    if (!_respecArmed)
                    {
                        _respecArmed = true;
                        return false;
                    }

                    _respecArmed = false;

                    if (!SkillTreeRules.TryPlayerRespec(save, tree, out _))
                    {
                        _statusMessage = LocalizationService.Resolve("Not enough Alloy for a respec.", "Nicht genug Legierung für einen Reset.");
                        return false;
                    }

                    _statusMessage = LocalizationService.Resolve("Tree reset. ", "Baum zurückgesetzt. ") + refund + LocalizationService.Resolve(" SP refunded.", " FP erstattet.");
                    return true;
                }));
        }

        private static RowState ToRowState(SkillNodeState state)
        {
            switch (state)
            {
                case SkillNodeState.Purchased:
                    return RowState.Owned;
                case SkillNodeState.Available:
                    return RowState.Available;
                case SkillNodeState.Unaffordable:
                    return RowState.Unaffordable;
                default:
                    return RowState.Locked;
            }
        }

        // "You cannot afford this" and "this is not available yet" must never look the same
        // (SkillTree.md UI) - they get different words here and different colours in the presenter.
        private static string DescribeNodeStatus(SkillNodeState state, int cost)
        {
            switch (state)
            {
                case SkillNodeState.Purchased:
                    return LocalizationService.Resolve("Owned", "Besessen");
                case SkillNodeState.Available:
                case SkillNodeState.Unaffordable:
                    return cost + " SP";
                default:
                    return LocalizationService.Resolve("Locked", "Gesperrt");
            }
        }

        private static string DescribeKeyNodeLock(SkillNodeState state, SkillKeyNodeData keyNode, SkillTreeDefinitionData tree, int branchSpend)
        {
            switch (state)
            {
                case SkillNodeState.LockedPrerequisite:
                    return LocalizationService.Resolve("Requires ", "Benötigt ") + ResolveNodeName(tree, keyNode.PrerequisiteNodeId) + ".";
                case SkillNodeState.LockedBranchSpend:
                    return LocalizationService.Resolve("Spend ", "Gib ") + (keyNode.BranchSpendRequirement - branchSpend)
                        + LocalizationService.Resolve(" more SP in this branch first.", " FP mehr in diesem Zweig aus.");
                case SkillNodeState.Unaffordable:
                    return LocalizationService.Resolve("Not enough Skill Points.", "Nicht genug Fertigkeitspunkte.");
                default:
                    return string.Empty;
            }
        }

        private static string ResolveNodeName(SkillTreeDefinitionData tree, string nodeId)
        {
            if (tree.Branches == null)
            {
                return nodeId;
            }

            for (var branchIndex = 0; branchIndex < tree.Branches.Length; branchIndex++)
            {
                var keyNodes = tree.Branches[branchIndex] != null ? tree.Branches[branchIndex].KeyNodes : null;

                if (keyNodes == null)
                {
                    continue;
                }

                for (var index = 0; index < keyNodes.Length; index++)
                {
                    if (keyNodes[index] != null && keyNodes[index].NodeId == nodeId)
                    {
                        return keyNodes[index].DisplayName;
                    }
                }
            }

            return nodeId;
        }

        // ---- Ascension (P7-3, 2026-09-29) -------------------------------------------------------
        //
        // Unlocked at the level cap. XP past it fills Paragon levels, each paying one Ascension
        // Point, spent on the ring of repeatable bonuses (here and on each branch's graph). Every
        // tenth Paragon level also grants a Keystone: a choice of three, applied KeystoneStrength
        // times.

        private readonly List<AscensionBonusData> _ringBuffer = new List<AscensionBonusData>();

        private void BuildAscensionRows()
        {
            var tree = _metaProgressionService.ActiveSkillTree;
            var save = _metaProgressionService.SaveData;

            if (tree == null)
            {
                return;
            }

            if (!SkillTreeRules.IsAscensionUnlocked(save))
            {
                _commands.Add(new RowCommand(
                    LocalizationService.Resolve("Ascension unlocks at level ", "Aufstieg wird freigeschaltet auf Stufe ") + MetaProgressionSaveData.MaxLevel,
                    LocalizationService.Resolve("Level ", "Stufe ") + save.Level,
                    RowState.Header,
                    string.Empty,
                    null));
                _commands.Add(new RowCommand(
                    LocalizationService.Resolve(
                        "Past the cap, experience keeps filling Paragon levels. Each pays one Ascension Point for the ring below; every tenth pays a Keystone.",
                        "Nach der Höchststufe füllt Erfahrung Paragon-Stufen. Jede zahlt einen Aufstiegspunkt für den Ring unten; jede zehnte einen Schlüsselstein."),
                    string.Empty,
                    RowState.Locked,
                    LocalizationService.Resolve("Every Skill Point comes from levels and quests - see the Quests tab.", "Jeder Skillpunkt kommt aus Stufen und Quests - siehe den Reiter Quests."),
                    null));
            }
            else
            {
                _commands.Add(new RowCommand(
                    "Paragon " + save.ParagonLevel + "   " + save.Experience + " / " + save.ExperienceForNextLevel + " XP",
                    save.AscensionPointsAvailable + LocalizationService.Resolve(" Ascension Points", " Aufstiegspunkte"),
                    RowState.Header,
                    string.Empty,
                    null));

                var keystones = save.KeystonesAvailable;
                var nextKeystone = (save.ParagonLevel / MetaProgressionSaveData.MilestoneLevelInterval + 1) * MetaProgressionSaveData.MilestoneLevelInterval;
                _commands.Add(new RowCommand(
                    keystones > 0
                        ? LocalizationService.Resolve("Keystone ready - choose one", "Schlüsselstein bereit - wähle einen")
                        : LocalizationService.Resolve("Next Keystone at Paragon ", "Nächster Schlüsselstein bei Paragon ") + nextKeystone,
                    LocalizationService.Resolve("taken: ", "gewählt: ") + save.AscensionRank,
                    RowState.Header,
                    string.Empty,
                    null));

                if (keystones > 0)
                {
                    var offer = SkillTreeRules.EnsureAscensionOffer(save, tree);

                    for (var index = 0; index < offer.Count; index++)
                    {
                        var bonus = tree.FindAscensionBonus(offer[index]);
                        var offerIndex = index;
                        _commands.Add(new RowCommand(
                            "    " + (bonus != null ? bonus.DisplayName : offer[index]) + "  x" + tree.KeystoneStrength,
                            LocalizationService.Resolve("Keystone", "Schlüsselstein"),
                            RowState.Available,
                            LocalizationService.Resolve(
                                "Take this bonus " + tree.KeystoneStrength + " times over. The other two are rerolled for the next Keystone.",
                                "Nimm diesen Bonus " + tree.KeystoneStrength + "-fach. Die anderen beiden werden für den nächsten Schlüsselstein neu gewürfelt."),
                            () => SkillTreeRules.TryPurchaseAscension(save, tree, offerIndex)));
                    }
                }
            }

            _commands.Add(new RowCommand(
                "<b>" + LocalizationService.Resolve("Ascension ring", "Aufstiegsring") + "</b>",
                string.Empty,
                RowState.Header,
                string.Empty,
                null));

            for (var branchIndex = 0; tree.Branches != null && branchIndex < tree.Branches.Length; branchIndex++)
            {
                var branch = tree.Branches[branchIndex];

                if (branch != null)
                {
                    AddAscensionRingRows(tree, branch, save, "    " + branch.DisplayName + ":  ", false);
                }
            }
        }

        // The ring bonuses of one branch. On a branch tab they are graph nodes past the last key
        // node; on the Ascension tab the same rows appear as a list.
        private void AddAscensionRingRows(SkillTreeDefinitionData tree, SkillBranchData branch, MetaProgressionSaveData save, string prefix = "", bool asGraphNodes = true)
        {
            tree.CollectAscensionBonuses(branch.BranchId, _ringBuffer);

            for (var index = 0; index < _ringBuffer.Count; index++)
            {
                var bonus = _ringBuffer[index];
                var rank = save.GetAscensionNodeRank(bonus.BonusId);
                var state = SkillTreeRules.ResolveAscensionNodeState(save, bonus);
                var cost = SkillTreeDefinitionData.AscensionNodeCost(rank);
                var captured = bonus;
                var reason = state == SkillNodeState.LockedPrerequisite
                    ? LocalizationService.Resolve("Unlocks at level " + MetaProgressionSaveData.MaxLevel + " (you are level " + save.Level + ").",
                        "Wird auf Stufe " + MetaProgressionSaveData.MaxLevel + " freigeschaltet (du bist Stufe " + save.Level + ").")
                    : state == SkillNodeState.Unaffordable
                        ? LocalizationService.Resolve("Not enough Ascension Points - each Paragon level pays one.", "Nicht genug Aufstiegspunkte - jede Paragon-Stufe zahlt einen.")
                        : string.Empty;

                _commands.Add(new RowCommand(
                    prefix + LocalizationService.Resolve("Ascension: ", "Aufstieg: ") + bonus.DisplayName + "  " + LocalizationService.Resolve("rank ", "Rang ") + rank,
                    state == SkillNodeState.LockedPrerequisite ? LocalizationService.Resolve("Level ", "Stufe ") + MetaProgressionSaveData.MaxLevel : cost + " AP",
                    state == SkillNodeState.LockedPrerequisite ? RowState.Locked : ToRowState(state),
                    bonus.DisplayName + LocalizationService.Resolve(" per rank, repeatable. The price rises by one every ten ranks.", " pro Rang, wiederholbar. Der Preis steigt alle zehn Ränge um eins.")
                        + (reason.Length > 0 ? "\n" + reason : string.Empty),
                    () => SkillTreeRules.TryPurchaseAscensionNode(save, captured),
                    asGraphNodes ? branch.KeyNodes.Length : -1,
                    asGraphNodes ? index : -1));
            }
        }

        // ---- Quests (P8-1 and A4-1, 2026-09-29) --------------------------------------------------
        //
        // The Skill Point quests, then the Alloy quests below them on the same tab (A4-1). Information
        // only: a quest completes by playing, never by pressing Confirm here.
        private readonly HashSet<string> _collectedCards = new HashSet<string>();

        private void BuildQuestRows()
        {
            var definition = _metaProgressionService.DefinitionData;
            var save = _metaProgressionService.SaveData;
            var catalog = QuestLedger.BuildCatalog(definition);
            _metaProgressionService.ResolveCollectedCardIds(_collectedCards);

            AddQuestSection(definition, save, catalog, false,
                LocalizationService.Resolve("Skill Point quests", "Skillpunkt-Quests"), " SP");
            AddQuestSection(definition, save, catalog, true,
                LocalizationService.Resolve("Alloy quests", "Legierungs-Quests"), LocalizationService.Resolve(" Alloy", " Legierung"));
        }

        private void AddQuestSection(MetaProgressionDefinitionData definition, MetaProgressionSaveData save,
            IReadOnlyList<QuestDefinition> catalog, bool alloy, string title, string unit)
        {
            var count = 0;
            var done = 0;
            var earned = 0;
            var total = 0;

            for (var index = 0; index < catalog.Count; index++)
            {
                var quest = catalog[index];

                if (quest.PaysAlloy != alloy)
                {
                    continue;
                }

                var reward = alloy ? quest.RewardAlloy : quest.RewardSkillPoints;
                count++;
                total += reward;

                if (save.IsQuestCompleted(QuestLedger.QuestOwnerId, quest.QuestId))
                {
                    done++;
                    earned += reward;
                }
            }

            _commands.Add(new RowCommand(
                "<b>" + title + "</b>   " + done + " / " + count,
                earned + " / " + total + unit,
                RowState.Header,
                string.Empty,
                null));

            QuestGroup? group = null;

            for (var index = 0; index < catalog.Count; index++)
            {
                var quest = catalog[index];

                if (quest.PaysAlloy != alloy)
                {
                    continue;
                }

                if (group != quest.Group)
                {
                    group = quest.Group;
                    _commands.Add(new RowCommand("  " + DescribeQuestGroup(quest.Group), string.Empty, RowState.Header, string.Empty, null));
                }

                var completed = save.IsQuestCompleted(QuestLedger.QuestOwnerId, quest.QuestId);
                var target = quest.Group == QuestGroup.WeaponMastery ? QuestLedger.ResolveMasteryTarget(definition, quest) : quest.Target;
                var progress = QuestLedger.ResolveProgress(save, definition, quest, _collectedCards);

                _commands.Add(new RowCommand(
                    "    " + quest.Title + "  " + BuildProgressBar(progress, target) + "  " + progress + " / " + target,
                    completed ? LocalizationService.Resolve("Done", "Erledigt") : quest.RewardLabel,
                    completed ? RowState.Owned : RowState.Locked,
                    DescribeQuest(quest, progress, target),
                    null));
            }
        }

        private static string DescribeQuestGroup(QuestGroup group)
        {
            switch (group)
            {
                case QuestGroup.WeaponKills:
                    return LocalizationService.Resolve("Weapon kills - 1 SP each", "Waffen-Kills - je 1 SP");
                case QuestGroup.WeaponArsenal:
                    return LocalizationService.Resolve("Arsenal - clear the game with six of one weapon - 1 SP each", "Arsenal - schließe das Spiel mit sechs gleichen Waffen ab - je 1 SP");
                case QuestGroup.WeaponMastery:
                    return LocalizationService.Resolve("Weapon mastery - every Alloy upgrade - 3 SP each", "Waffenmeisterschaft - jede Legierungs-Verbesserung - je 3 SP");
                case QuestGroup.Collection:
                    return LocalizationService.Resolve("Collection - 1 SP", "Sammlung - 1 SP");
                case QuestGroup.Bosses:
                    return LocalizationService.Resolve("Bosses - first kill - 1 SP each", "Bosse - erster Sieg - je 1 SP");
                case QuestGroup.TowerMastery:
                    return LocalizationService.Resolve("Towers - highest level in one run - 150 Alloy each", "Türme - höchste Stufe in einem Run - je 150 Legierung");
                case QuestGroup.TowerKills:
                    return LocalizationService.Resolve("Tower kills - 100 Alloy each", "Turm-Kills - je 100 Legierung");
                case QuestGroup.EnemyKills:
                    return LocalizationService.Resolve("Enemy types - 150 Alloy each", "Gegnerarten - je 150 Legierung");
                case QuestGroup.EliteKills:
                    return LocalizationService.Resolve("Elites - 100 Alloy each", "Elite-Gegner - je 100 Legierung");
                case QuestGroup.Factory:
                    return LocalizationService.Resolve("Factory - 200 Alloy", "Fabrik - 200 Legierung");
                case QuestGroup.RarityCollection:
                    return LocalizationService.Resolve("Card sets by rarity - 100 to 300 Alloy", "Kartensätze nach Seltenheit - 100 bis 300 Legierung");
                case QuestGroup.StageUntouchedCore:
                    return LocalizationService.Resolve("Untouched Core - 100 Alloy each", "Unversehrter Kern - je 100 Legierung");
                default:
                    return LocalizationService.Resolve("One weapon type - 100 Alloy each", "Eine Waffenart - je 100 Legierung");
            }
        }

        private static string DescribeQuest(QuestDefinition quest, int progress, int target)
        {
            var weaponName = quest.Weapon != null ? quest.Weapon.DisplayName : string.Empty;

            switch (quest.Group)
            {
                case QuestGroup.WeaponKills:
                    return LocalizationService.Resolve(
                        "Kills by " + weaponName + "'s shots, burns and freezes count. Towers do not.",
                        "Es zählen Kills durch Schüsse, Brand und Frost von " + weaponName + ". Türme zählen nicht.");
                case QuestGroup.WeaponArsenal:
                    return LocalizationService.Resolve(
                        "Win the final stage with all six weapon slots holding " + weaponName + ". Needs all six slots unlocked.",
                        "Gewinne die letzte Stage, während alle sechs Waffenplätze " + weaponName + " tragen. Braucht alle sechs Plätze.");
                case QuestGroup.WeaponMastery:
                    return LocalizationService.Resolve(
                        "Buy every Alloy upgrade for " + weaponName + " on the Upgrades tab: " + progress + " of " + target + " ranks.",
                        "Kaufe jede Legierungs-Verbesserung für " + weaponName + " im Reiter Verbesserungen: " + progress + " von " + target + " Rängen.");
                case QuestGroup.Collection:
                case QuestGroup.RarityCollection:
                    return LocalizationService.Resolve(
                        "Take every card at least once, from a reward or the shop. Records shows which are still missing.",
                        "Nimm jede Karte mindestens einmal, aus einer Belohnung oder dem Shop. Aufzeichnungen zeigen, welche fehlen.");
                case QuestGroup.Bosses:
                    return LocalizationService.Resolve("The first defeat of this boss, on any run.", "Der erste Sieg über diesen Boss, in irgendeinem Run.");
                case QuestGroup.TowerMastery:
                    return LocalizationService.Resolve(
                        "Build it and buy every upgrade level within one run.",
                        "Baue ihn und kaufe in einem Run jede Verbesserungsstufe.");
                case QuestGroup.TowerKills:
                    return LocalizationService.Resolve("Kills landed by this tower's shots.", "Kills durch die Schüsse dieses Turms.");
                case QuestGroup.EnemyKills:
                case QuestGroup.EliteKills:
                    return LocalizationService.Resolve("Every kill counts, whoever lands it.", "Jeder Kill zählt, egal wer ihn landet.");
                case QuestGroup.Factory:
                    return LocalizationService.Resolve(
                        "Grow one production chain to tier 4 before the run ends - it counts even if the run is lost afterwards.",
                        "Baue eine Produktionskette bis Stufe 4 aus, bevor der Run endet - zählt auch, wenn der Run danach verloren geht.");
                case QuestGroup.StageUntouchedCore:
                    return LocalizationService.Resolve("From the stage's first wave to its boss, not a single hit on the Core.", "Von der ersten Welle bis zum Boss kein einziger Treffer auf den Kern.");
                default:
                    return LocalizationService.Resolve(
                        "Clear the stage while every weapon you carry is the same type - at every moment of it.",
                        "Schließe die Stage ab, während jede getragene Waffe dieselbe Art ist - die ganze Stage lang.");
            }
        }

        private static string BuildProgressBar(int progress, int target)
        {
            const int Segments = 10;
            var filled = target > 0 ? Mathf.Clamp(Mathf.FloorToInt(Segments * (float)progress / target), 0, Segments) : 0;
            return BuildPips(filled, Segments);
        }

        private readonly struct RowCommand
        {
            public RowCommand(string label, string status, RowState state, string description, Func<bool> execute, int graphKey = -1, int graphSub = -1)
            {
                Label = label;
                Status = status;
                State = state;
                Description = description;
                Execute = execute;
                GraphKey = graphKey;
                GraphSub = graphSub;
            }

            // Where this row sits in the skill graph: key-node index in the branch, and sub-node
            // index under it (-1 for the key node itself). GraphKey is -1 for every non-node row.
            public int GraphKey { get; }

            public int GraphSub { get; }

            public string Label { get; }

            public string Status { get; }

            public RowState State { get; }

            public string Description { get; }

            public Func<bool> Execute { get; }
        }

        public readonly struct DisplayRow
        {
            public DisplayRow(string label, string statusLabel, bool isSelected, bool isUnlocked, RowState state = RowState.Available)
            {
                Label = label;
                StatusLabel = statusLabel;
                IsSelected = isSelected;
                IsUnlocked = isUnlocked;
                State = state;
            }

            public string Label { get; }

            public string StatusLabel { get; }

            public bool IsSelected { get; }

            public bool IsUnlocked { get; }

            public RowState State { get; }
        }
    }
}
