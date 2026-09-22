using System;
using System.Collections.Generic;
using UnityEngine;

namespace Corebound.SaveSystem
{
    [Serializable]
    public sealed class MetaProgressionSaveData
    {
        // v2 (2026-09-22): the meta currency became Alloy (Upgrades.md) and three progression
        // systems were added - Skill Points and the per-character skill tree (SkillTree.md), Alloy
        // stat upgrades (Upgrades.md) and weapon slot unlocks (WeaponLoadout.md).
        public const int CurrentSaveFormatVersion = 2;

        public const string DefaultCharacterId = "vanguard";

        [SerializeField] private int _saveFormatVersion = CurrentSaveFormatVersion;

        // v1's balance. Kept only so a v1 file still deserializes it; MigrateToCurrentFormat moves
        // it into _alloyEarned and nothing reads it afterwards.
        [SerializeField] private int _metaCurrencyAmount;
        [SerializeField] private bool _hasSeenTutorial;
        [SerializeField] private List<string> _purchasedUnlockIds = new List<string>();

        // Balances are never stored, only earned and spent, so the two can never disagree
        // (SkillTree.md / Upgrades.md: "Available SP is always Earned - Spent").
        [SerializeField] private int _alloyEarned;
        [SerializeField] private int _alloySpent;
        [SerializeField] private int _skillPointsEarned;
        [SerializeField] private int _skillPointsSpent;

        [SerializeField] private string _selectedCharacterId = DefaultCharacterId;
        [SerializeField] private List<SkillNodeRank> _skillNodeRanks = new List<SkillNodeRank>();
        [SerializeField] private int _skillTreeVersion;

        // Owner decision 2026-09-22: one free respec per tree version; later ones cost Alloy.
        // Holds the tree version whose free respec has been used (0 = none yet).
        [SerializeField] private int _freeRespecUsedTreeVersion;
        [SerializeField] private int _ascensionRank;
        [SerializeField] private List<string> _ascensionChoices = new List<string>();
        [SerializeField] private List<string> _pendingAscensionOffer = new List<string>();

        [SerializeField] private List<UpgradeRank> _upgradeRanks = new List<UpgradeRank>();

        // First-time bonuses are paid once per boss; this is how "once" is remembered.
        [SerializeField] private List<string> _defeatedBossIds = new List<string>();
        [SerializeField] private bool _hasClearedAnyStage;
        [SerializeField] private bool _hasCompletedFullRun;

        public int SaveFormatVersion => _saveFormatVersion;

        public bool HasSeenTutorial => _hasSeenTutorial;

        public IReadOnlyList<string> PurchasedUnlockIds => _purchasedUnlockIds;

        public int AlloyEarned => _alloyEarned;

        public int AlloySpent => _alloySpent;

        public int AlloyBalance => Mathf.Max(0, _alloyEarned - _alloySpent);

        // The v1 name, kept so existing callers keep compiling. It is the Alloy balance.
        public int MetaCurrencyAmount => AlloyBalance;

        public int SkillPointsEarned => _skillPointsEarned;

        public int SkillPointsSpent => _skillPointsSpent;

        public int SkillPointsAvailable => Mathf.Max(0, _skillPointsEarned - _skillPointsSpent);

        public string SelectedCharacterId => string.IsNullOrWhiteSpace(_selectedCharacterId) ? DefaultCharacterId : _selectedCharacterId;

        public IReadOnlyList<SkillNodeRank> SkillNodeRanks => _skillNodeRanks;

        public int SkillTreeVersion => _skillTreeVersion;

        public int FreeRespecUsedTreeVersion => _freeRespecUsedTreeVersion;

        public void MarkFreeRespecUsed(int treeVersion)
        {
            _freeRespecUsedTreeVersion = treeVersion;
        }

        public int AscensionRank => _ascensionRank;

        public IReadOnlyList<string> AscensionChoices => _ascensionChoices;

        public IReadOnlyList<string> PendingAscensionOffer => _pendingAscensionOffer;

        public IReadOnlyList<UpgradeRank> UpgradeRanks => _upgradeRanks;

        public bool HasClearedAnyStage => _hasClearedAnyStage;

        public bool HasCompletedFullRun => _hasCompletedFullRun;

        public MetaProgressionSaveData Clone()
        {
            var clone = (MetaProgressionSaveData)MemberwiseClone();
            clone._purchasedUnlockIds = new List<string>(_purchasedUnlockIds ?? new List<string>());
            clone._skillNodeRanks = CloneList(_skillNodeRanks);
            clone._ascensionChoices = new List<string>(_ascensionChoices ?? new List<string>());
            clone._pendingAscensionOffer = new List<string>(_pendingAscensionOffer ?? new List<string>());
            clone._upgradeRanks = CloneList(_upgradeRanks);
            clone._defeatedBossIds = new List<string>(_defeatedBossIds ?? new List<string>());
            return clone;
        }

        // Returns true when the data changed and should be written back.
        public bool MigrateToCurrentFormat()
        {
            var changed = false;

            _purchasedUnlockIds ??= new List<string>();
            _skillNodeRanks ??= new List<SkillNodeRank>();
            _ascensionChoices ??= new List<string>();
            _pendingAscensionOffer ??= new List<string>();
            _upgradeRanks ??= new List<UpgradeRank>();
            _defeatedBossIds ??= new List<string>();

            if (_saveFormatVersion < 2)
            {
                // v1 stored a spendable balance; earned = that balance and spent = 0 preserves
                // exactly what the player could spend. Past spending is not reconstructed - it was
                // never recorded, and it does not change anything the player can do.
                _alloyEarned = Mathf.Max(0, _metaCurrencyAmount);
                _alloySpent = 0;
                _metaCurrencyAmount = 0;
                _saveFormatVersion = 2;
                changed = true;
            }

            return changed;
        }

        // ---- Starting values -------------------------------------------------------------------

        public void SetStartingAlloy(int value)
        {
            _alloyEarned = Mathf.Max(0, value);
            _alloySpent = 0;
        }

        // v1 API: sets the balance by adjusting the earned total.
        public void SetMetaCurrencyAmount(int value)
        {
            _alloyEarned = _alloySpent + Mathf.Max(0, value);
        }

        public void SetHasSeenTutorial(bool value)
        {
            _hasSeenTutorial = value;
        }

        // ---- Alloy -----------------------------------------------------------------------------

        public void GrantAlloy(int amount)
        {
            _alloyEarned += Mathf.Max(0, amount);
        }

        public bool TrySpendAlloy(int cost)
        {
            if (cost < 0 || AlloyBalance < cost)
            {
                return false;
            }

            _alloySpent += cost;
            return true;
        }

        public bool HasPurchased(string unlockId)
        {
            return !string.IsNullOrWhiteSpace(unlockId) && _purchasedUnlockIds.Contains(unlockId);
        }

        public bool TryPurchase(string unlockId, int cost)
        {
            if (string.IsNullOrWhiteSpace(unlockId) || HasPurchased(unlockId) || !TrySpendAlloy(cost))
            {
                return false;
            }

            _purchasedUnlockIds.Add(unlockId);
            return true;
        }

        // ---- Skill Points ----------------------------------------------------------------------

        public void GrantSkillPoints(int amount)
        {
            _skillPointsEarned += Mathf.Max(0, amount);
        }

        public bool TrySpendSkillPoints(int cost)
        {
            if (cost < 0 || SkillPointsAvailable < cost)
            {
                return false;
            }

            _skillPointsSpent += cost;
            return true;
        }

        public void SetSelectedCharacterId(string characterId)
        {
            _selectedCharacterId = string.IsNullOrWhiteSpace(characterId) ? DefaultCharacterId : characterId;
        }

        public int GetSkillNodeRank(string characterId, string nodeId)
        {
            var index = FindSkillNode(characterId, nodeId);
            return index >= 0 ? _skillNodeRanks[index].Rank : 0;
        }

        public void SetSkillNodeRank(string characterId, string nodeId, int rank)
        {
            var index = FindSkillNode(characterId, nodeId);

            if (rank <= 0)
            {
                if (index >= 0)
                {
                    _skillNodeRanks.RemoveAt(index);
                }

                return;
            }

            if (index >= 0)
            {
                _skillNodeRanks[index] = new SkillNodeRank(characterId, nodeId, rank);
                return;
            }

            _skillNodeRanks.Add(new SkillNodeRank(characterId, nodeId, rank));
        }

        // Full respec for one character: removes every node rank and refunds exactly the SP those
        // ranks cost. Ascension is untouched (SkillTree.md: its offers were rolled).
        public void ClearSkillNodes(string characterId, int refund)
        {
            for (var index = _skillNodeRanks.Count - 1; index >= 0; index--)
            {
                if (string.Equals(_skillNodeRanks[index].CharacterId, characterId, StringComparison.Ordinal))
                {
                    _skillNodeRanks.RemoveAt(index);
                }
            }

            _skillPointsSpent = Mathf.Max(0, _skillPointsSpent - Mathf.Max(0, refund));
        }

        public void SetSkillTreeVersion(int version)
        {
            _skillTreeVersion = version;
        }

        public void AddAscensionRank(string chosenBonusId)
        {
            _ascensionRank++;
            _ascensionChoices.Add(chosenBonusId ?? string.Empty);
            _pendingAscensionOffer.Clear();
        }

        public void SetPendingAscensionOffer(IEnumerable<string> bonusIds)
        {
            _pendingAscensionOffer.Clear();

            if (bonusIds != null)
            {
                _pendingAscensionOffer.AddRange(bonusIds);
            }
        }

        // ---- Upgrades --------------------------------------------------------------------------

        public int GetUpgradeRank(string itemId, int stat)
        {
            var index = FindUpgrade(itemId, stat);
            return index >= 0 ? _upgradeRanks[index].Rank : 0;
        }

        public void SetUpgradeRank(string itemId, int stat, int rank)
        {
            var index = FindUpgrade(itemId, stat);

            if (index >= 0)
            {
                _upgradeRanks[index] = new UpgradeRank(itemId, stat, rank);
                return;
            }

            _upgradeRanks.Add(new UpgradeRank(itemId, stat, rank));
        }

        // ---- Milestones ------------------------------------------------------------------------

        // Returns true the first time a boss id is recorded - the moment the first-kill bonus pays.
        public bool RecordBossDefeated(string bossId)
        {
            if (string.IsNullOrWhiteSpace(bossId) || _defeatedBossIds.Contains(bossId))
            {
                return false;
            }

            _defeatedBossIds.Add(bossId);
            return true;
        }

        public bool HasDefeatedBoss(string bossId)
        {
            return !string.IsNullOrWhiteSpace(bossId) && _defeatedBossIds.Contains(bossId);
        }

        public void MarkStageCleared()
        {
            _hasClearedAnyStage = true;
        }

        public void MarkFullRunCompleted()
        {
            _hasCompletedFullRun = true;
        }

        private int FindSkillNode(string characterId, string nodeId)
        {
            for (var index = 0; index < _skillNodeRanks.Count; index++)
            {
                var entry = _skillNodeRanks[index];

                if (string.Equals(entry.CharacterId, characterId, StringComparison.Ordinal) && string.Equals(entry.NodeId, nodeId, StringComparison.Ordinal))
                {
                    return index;
                }
            }

            return -1;
        }

        private int FindUpgrade(string itemId, int stat)
        {
            for (var index = 0; index < _upgradeRanks.Count; index++)
            {
                if (_upgradeRanks[index].Stat == stat && string.Equals(_upgradeRanks[index].ItemId, itemId, StringComparison.Ordinal))
                {
                    return index;
                }
            }

            return -1;
        }

        private static List<T> CloneList<T>(List<T> source)
        {
            return source != null ? new List<T>(source) : new List<T>();
        }
    }

    // Keyed by character as well as node: SkillTree.md, "one tree per character".
    [Serializable]
    public struct SkillNodeRank
    {
        public string CharacterId;
        public string NodeId;
        public int Rank;

        public SkillNodeRank(string characterId, string nodeId, int rank)
        {
            CharacterId = characterId;
            NodeId = nodeId;
            Rank = rank;
        }
    }

    // Stat is the UpgradableStat enum stored as its int index, so SaveSystem needs no reference to
    // gameplay code. UpgradableStat is append-only for exactly this reason.
    [Serializable]
    public struct UpgradeRank
    {
        public string ItemId;
        public int Stat;
        public int Rank;

        public UpgradeRank(string itemId, int stat, int rank)
        {
            ItemId = itemId;
            Stat = stat;
            Rank = rank;
        }
    }
}
