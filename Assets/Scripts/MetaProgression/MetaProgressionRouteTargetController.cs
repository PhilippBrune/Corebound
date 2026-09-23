using System;
using System.Collections.Generic;
using System.Text;
using Corebound.Gameplay.Towers;
using Corebound.Gameplay.Weapons;
using Corebound.Input;
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
            Ascension
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
                    + "     " + LocalizationService.Resolve("Skill Points: ", "Fertigkeitspunkte: ") + _metaProgressionService.SkillPointsAvailable
                    + (_metaProgressionService.AscensionRank > 0 ? "     " + LocalizationService.Resolve("Ascension ", "Aufstieg ") + _metaProgressionService.AscensionRank : string.Empty);
            }
        }

        // The header's parts, so the presenter can put the Alloy and Skill Point icons (Assets 92,
        // 93) next to their own numbers.
        public string AlloyText => _metaProgressionService == null ? string.Empty
            : (_metaProgressionService.DefinitionData != null ? _metaProgressionService.DefinitionData.MetaCurrencyLabel : "Alloy") + ": " + _metaProgressionService.AlloyBalance;

        public string SkillPointText => _metaProgressionService == null ? string.Empty
            : LocalizationService.Resolve("Skill Points: ", "Fertigkeitspunkte: ") + _metaProgressionService.SkillPointsAvailable;

        public string AscensionText => _metaProgressionService == null || _metaProgressionService.AscensionRank <= 0 ? string.Empty
            : LocalizationService.Resolve("Ascension ", "Aufstieg ") + _metaProgressionService.AscensionRank;

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
                        return "Assets/Art/UI/Skilltree/ascension_rank_badge.png";
                    default:
                        return string.Empty;
                }
            }
        }

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

        public string FooterHint => LocalizationService.Resolve(
            "Left/Right: tab   Up/Down: select   Confirm: buy   Cancel: back",
            "Links/Rechts: Reiter   Hoch/Runter: wählen   Bestätigen: kaufen   Abbrechen: zurück");

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

            if (!vertical || _commands.Count == 0)
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

        // Tabs: Unlocks, Upgrades, one per skill branch, Ascension.
        private int ResolveTabCount()
        {
            var tree = _metaProgressionService != null ? _metaProgressionService.ActiveSkillTree : null;
            return 3 + (tree != null && tree.Branches != null ? tree.Branches.Length : 0);
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
            _tabIndex = Mathf.Clamp(_tabIndex, 0, 2 + branchCount);

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

            return Tab.Ascension;
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
                default:
                    BuildAscensionRows();
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
                // change anything would be a trap for Alloy.
                if (track.Stat == UpgradableStat.Accuracy && kind == UpgradeTargetKind.Weapon && baseValue(track.Stat) <= 0f)
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
                    () => SkillTreeRules.TryPurchaseKeyNode(save, tree, branch, capturedKey)));

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
                        () => SkillTreeRules.TryPurchaseSubNode(save, tree, capturedKey, capturedSub)));
                }
            }

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

        // ---- Ascension (Skill Points, numbers only) -------------------------------------------------

        private void BuildAscensionRows()
        {
            var tree = _metaProgressionService.ActiveSkillTree;
            var save = _metaProgressionService.SaveData;

            if (tree == null)
            {
                return;
            }

            var complete = SkillTreeRules.IsTreeComplete(save, tree);
            var cost = tree.AscensionCostForRank(save.AscensionRank);
            _commands.Add(new RowCommand(
                LocalizationService.Resolve("Ascension rank ", "Aufstiegsrang ") + save.AscensionRank,
                complete ? LocalizationService.Resolve("Next: ", "Nächster: ") + cost + " SP" : LocalizationService.Resolve("Locked", "Gesperrt"),
                RowState.Header,
                string.Empty,
                null));

            if (!complete)
            {
                _commands.Add(new RowCommand(
                    LocalizationService.Resolve("Complete every node of the tree to begin Ascending.", "Schließe jeden Knoten des Baums ab, um aufzusteigen."),
                    (SkillTreeRules.ResolveTotalTreeSpend(save, tree)) + " SP " + LocalizationService.Resolve("spent", "ausgegeben"),
                    RowState.Locked,
                    LocalizationService.Resolve(
                        "Ascension is an endless list of small numeric bonuses, one choice of three per rank.",
                        "Aufstieg ist eine endlose Liste kleiner Zahlenboni, eine Wahl aus drei pro Rang."),
                    null));
                return;
            }

            var offer = SkillTreeRules.EnsureAscensionOffer(save, tree);

            for (var index = 0; index < offer.Count; index++)
            {
                var bonus = tree.FindAscensionBonus(offer[index]);
                var offerIndex = index;
                var affordable = save.SkillPointsAvailable >= cost;
                _commands.Add(new RowCommand(
                    "    " + (bonus != null ? bonus.DisplayName : offer[index]),
                    cost + " SP",
                    affordable ? RowState.Available : RowState.Unaffordable,
                    LocalizationService.Resolve("Take this bonus for the next Ascension rank. The other two are rerolled.", "Nimm diesen Bonus für den nächsten Aufstiegsrang. Die anderen beiden werden neu gewürfelt."),
                    () => SkillTreeRules.TryPurchaseAscension(save, tree, offerIndex)));
            }
        }

        private readonly struct RowCommand
        {
            public RowCommand(string label, string status, RowState state, string description, Func<bool> execute)
            {
                Label = label;
                Status = status;
                State = state;
                Description = description;
                Execute = execute;
            }

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
