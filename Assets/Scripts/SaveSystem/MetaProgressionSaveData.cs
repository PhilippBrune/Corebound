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
        // v3 (2026-09-29): level cap 70 on a new curve, 2 SP per level, Paragon levels past the cap
        // paying Ascension Points, SP quests (LEVELLING_QUESTS_PARAGON_2026-09-29.md).
        public const int CurrentSaveFormatVersion = 3;

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
        [SerializeField] private string _preferredStartWeaponId = string.Empty;

        // Owner 2026-09-25: a new profile starts with the default weapon only. Any other unlicensed
        // weapon becomes a *starting* option once the player has held it in a run at least once;
        // licensed weapons stay behind their licence and are not discovered this way.
        [SerializeField] private List<string> _discoveredWeaponIds = new List<string>();
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

        // B11 (spec review 2026-09-22): which weapon a new run starts with, chosen in the New Run
        // panel. Empty means the default (Rapid). A weapon *licence* no longer decides this - it
        // only adds the weapon to reward and shop offers.
        public string PreferredStartWeaponId => _preferredStartWeaponId;

        public bool HasDiscoveredWeapon(string weaponId)
        {
            return !string.IsNullOrWhiteSpace(weaponId) && _discoveredWeaponIds != null && _discoveredWeaponIds.Contains(weaponId);
        }

        // True when this is the first time, so the caller can announce it.
        public bool RecordWeaponDiscovered(string weaponId)
        {
            if (string.IsNullOrWhiteSpace(weaponId))
            {
                return false;
            }

            _discoveredWeaponIds ??= new List<string>();

            if (_discoveredWeaponIds.Contains(weaponId))
            {
                return false;
            }

            _discoveredWeaponIds.Add(weaponId);
            return true;
        }

        public void SetPreferredStartWeaponId(string weaponId)
        {
            _preferredStartWeaponId = string.IsNullOrWhiteSpace(weaponId) ? string.Empty : weaponId;
        }

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
            clone._discoveredWeaponIds = new List<string>(_discoveredWeaponIds ?? new List<string>());
            clone._quests = CloneList(_quests);
            clone._ascensionNodeRanks = CloneList(_ascensionNodeRanks);
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
            _quests ??= new List<QuestProgress>();
            _ascensionNodeRanks ??= new List<AscensionNodeRank>();

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

            if (_saveFormatVersion < 3)
            {
                MigrateLevellingToV3();
                _saveFormatVersion = 3;
                changed = true;
            }

            // Every load, not only the migrating one: a brand-new profile is owed its level-1
            // Skill Points too, and this is idempotent once they are paid.
            changed |= SyncLevelSkillPoints();
            return changed;
        }

        // v2 -> v3. The old curve (x1.15, 1 SP per level-up, no cap) is replaced; the XP the player
        // earned is kept and re-read against the new curve. Two promises from the concept's
        // "Existing profiles" section are kept here:
        //  - a level is never taken away (the old curve was cheaper for the first few levels), and
        //  - Ascension ranks bought with SP under the old rules become Ascension ring ranks, with
        //    the SP they cost refunded - Ascension is paid in Ascension Points now, and leaving the
        //    SP spent would make the tree impossible to complete on that profile.
        private void MigrateLevellingToV3()
        {
            var oldLevel = Mathf.Max(1, _level);
            long totalExperience = Mathf.Max(0, _experience);

            for (var level = 1; level < oldLevel; level++)
            {
                totalExperience += LegacyLevelCost(level);
            }

            _level = 1;
            _experience = 0;
            _paragonLevel = 0;
            _paragonExperience = 0;
            AddExperienceWithoutPaying(totalExperience);

            if (_level < oldLevel)
            {
                _level = Mathf.Min(oldLevel, MaxLevel);
                _experience = 0;
            }

            // What the old rules already paid: one SP per level-up. SyncLevelSkillPoints tops this
            // up to the new entitlement.
            _levelSkillPointsPaid = oldLevel - 1;

            var legacyRanks = _ascensionChoices != null ? _ascensionChoices.Count : 0;

            if (legacyRanks > 0)
            {
                var refund = 0;

                for (var rank = 0; rank < legacyRanks; rank++)
                {
                    refund += LegacyAscensionBaseCost + rank / 10;

                    if (!string.IsNullOrEmpty(_ascensionChoices[rank]))
                    {
                        SetAscensionNodeRank(_ascensionChoices[rank], GetAscensionNodeRank(_ascensionChoices[rank]) + 1);
                    }
                }

                _skillPointsSpent = Mathf.Max(0, _skillPointsSpent - refund);
                _ascensionChoices.Clear();
                _pendingAscensionOffer.Clear();
                _ascensionRank = 0;
            }
        }

        private const int LegacyAscensionBaseCost = 5;

        private static int LegacyLevelCost(int level)
        {
            return Mathf.Max(1, Mathf.RoundToInt(120f * Mathf.Pow(1.15f, Mathf.Max(0, level - 1))));
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

        // Playtest 2026-09-27: "after the very first time earning Alloy or SP the game should show
        // the player how to spend them." Both currencies are meta, so the hint is owed once per
        // profile and has to survive the run that earned it - a run-scoped flag would re-fire on
        // every later run, and a static would re-fire after a domain reload.
        //
        // Appended, never inserted: this is serialized by field and old saves simply deserialize it
        // as false, which is the correct answer for a profile that has not seen the hint.
        [SerializeField] private bool _hasSeenMetaCurrencyHint;

        public bool HasSeenMetaCurrencyHint => _hasSeenMetaCurrencyHint;

        public void MarkMetaCurrencyHintSeen()
        {
            _hasSeenMetaCurrencyHint = true;
        }

        // ---- Levelling (owner decisions 2026-09-28, L1-L4) --------------------------------------
        //
        // Per profile, not per run (L2): "each level grants a SP", SP is meta currency, and a
        // per-run level granting meta currency would make farming an early stage optimal. It also
        // fixes the older problem that a run dying in Stage 2 earned nothing at all - every run
        // moves this bar, including a bad one.
        //
        // Appended, never inserted: old saves deserialize level 0 / XP 0, and EnsureLevelFloor()
        // lifts them to level 1 on first read so no profile is stuck below the first level.
        [SerializeField] private int _level;
        [SerializeField] private int _experience;

        // ---- Levelling v3 (owner 2026-09-29, LEVELLING_QUESTS_PARAGON_2026-09-29.md) ------------
        //
        // "Max level == the SP there are to spend." The tree costs 212 SP. Quests pay 51 of them,
        // levels the other 161: 2 SP for every level reached, level 1 included, plus 3 more at every
        // tenth level - 70 x 2 + 7 x 3 = 161. So at the cap, with every quest done, the tree is
        // exactly complete and can never be over-funded.
        //
        // Past the cap, XP keeps flowing into Paragon levels, each paying one Ascension Point.
        public const int MaxLevel = 70;
        public const int SkillPointsPerLevel = 2;
        public const int MilestoneLevelInterval = 10;
        public const int MilestoneSkillPoints = 3;

        // P2: linear-growing rather than geometric, so the cap is reachable at all. 400 XP is about
        // one cleared stage for the first level; the 69th level-up costs 4,480, and the whole road
        // to 70 is 168,360 XP - about 68 full runs, the ~70 hours the concept aimed at. The XP per
        // run behind that estimate has not been measured yet.
        private const int FirstLevelCost = 400;
        private const int LevelCostStep = 60;

        // About one full run per Ascension Point, so Paragon keeps paying at a steady pace instead
        // of at the price of the most expensive level (4,480), which would halve it.
        public const int ParagonLevelCost = 2500;

        // Levels are per profile today; the vision's per-character levelling arrives with the
        // second character. Quest progress is already keyed by character (P6).
        [SerializeField] private int _levelSkillPointsPaid;
        [SerializeField] private int _paragonLevel;
        [SerializeField] private int _paragonExperience;
        [SerializeField] private int _ascensionPointsEarned;
        [SerializeField] private int _ascensionPointsSpent;

        public int Level => Mathf.Max(1, _level);

        public bool IsAtLevelCap => Level >= MaxLevel;

        // At the cap this is progress towards the next Paragon level instead.
        public int Experience => IsAtLevelCap ? Mathf.Max(0, _paragonExperience) : Mathf.Max(0, _experience);

        public int ExperienceForNextLevel => IsAtLevelCap ? ParagonLevelCost : ResolveLevelCost(Level);

        public int ParagonLevel => Mathf.Max(0, _paragonLevel);

        public int AscensionPointsAvailable => Mathf.Max(0, _ascensionPointsEarned - _ascensionPointsSpent);

        public static int ResolveLevelCost(int level)
        {
            return FirstLevelCost + LevelCostStep * Mathf.Max(0, level - 1);
        }

        // Every SP levels will ever have paid by the time this level is reached.
        public static int ResolveLevelSkillPoints(int level)
        {
            var clamped = Mathf.Clamp(level, 0, MaxLevel);
            return clamped * SkillPointsPerLevel + clamped / MilestoneLevelInterval * MilestoneSkillPoints;
        }

        // What one level-up to this level pays: 2, or 5 on a tenth level.
        public static int ResolveSkillPointsForLevel(int level)
        {
            return ResolveLevelSkillPoints(level) - ResolveLevelSkillPoints(level - 1);
        }

        public readonly struct ExperienceResult
        {
            public ExperienceResult(int levelsGained, int paragonLevelsGained, int skillPointsGranted)
            {
                LevelsGained = levelsGained;
                ParagonLevelsGained = paragonLevelsGained;
                SkillPointsGranted = skillPointsGranted;
            }

            public int LevelsGained { get; }

            public int ParagonLevelsGained { get; }

            public int SkillPointsGranted { get; }
        }

        public ExperienceResult GrantExperience(int amount)
        {
            if (amount <= 0)
            {
                return default;
            }

            EnsureLevelFloor();
            var levelBefore = Level;
            var paragonBefore = ParagonLevel;
            var paid = _levelSkillPointsPaid;
            AddExperienceWithoutPaying(amount);
            SyncLevelSkillPoints();

            var paragonGained = ParagonLevel - paragonBefore;
            _ascensionPointsEarned += paragonGained;
            return new ExperienceResult(Level - levelBefore, paragonGained, _levelSkillPointsPaid - paid);
        }

        // A while loop, not an if: a stage clear can be worth more than one level on a fresh
        // profile, and swallowing the surplus would quietly lose Skill Points.
        private void AddExperienceWithoutPaying(long amount)
        {
            EnsureLevelFloor();

            while (amount > 0 && _level < MaxLevel)
            {
                var needed = ResolveLevelCost(_level) - _experience;

                if (amount < needed)
                {
                    _experience += (int)amount;
                    return;
                }

                amount -= needed;
                _experience = 0;
                _level += 1;
            }

            if (amount <= 0)
            {
                return;
            }

            // At the cap: the level bar is full and everything else is Paragon.
            _experience = 0;
            var paragonTotal = _paragonExperience + amount;
            _paragonLevel += (int)(paragonTotal / ParagonLevelCost);
            _paragonExperience = (int)(paragonTotal % ParagonLevelCost);
        }

        // Pays whatever the current level is owed and has not been paid yet. Idempotent.
        private bool SyncLevelSkillPoints()
        {
            EnsureLevelFloor();
            var owed = ResolveLevelSkillPoints(Level) - _levelSkillPointsPaid;

            if (owed <= 0)
            {
                return false;
            }

            GrantSkillPoints(owed);
            _levelSkillPointsPaid += owed;
            return true;
        }

        public bool TrySpendAscensionPoints(int cost)
        {
            if (cost < 0 || AscensionPointsAvailable < cost)
            {
                return false;
            }

            _ascensionPointsSpent += cost;
            return true;
        }

        // ---- Ascension ring (P7-3) ---------------------------------------------------------------
        //
        // Repeatable ranks of the Ascension pool's bonuses, bought with Ascension Points on the
        // outer ring of the skill graph. Keystones - the choice of three every tenth Paragon level -
        // stay in _ascensionChoices, and _ascensionRank counts how many were taken.
        [SerializeField] private List<AscensionNodeRank> _ascensionNodeRanks = new List<AscensionNodeRank>();

        public IReadOnlyList<AscensionNodeRank> AscensionNodeRanks => _ascensionNodeRanks;

        public int GetAscensionNodeRank(string bonusId)
        {
            var index = FindAscensionNode(bonusId);
            return index >= 0 ? _ascensionNodeRanks[index].Rank : 0;
        }

        public void SetAscensionNodeRank(string bonusId, int rank)
        {
            if (string.IsNullOrEmpty(bonusId))
            {
                return;
            }

            _ascensionNodeRanks ??= new List<AscensionNodeRank>();
            var index = FindAscensionNode(bonusId);

            if (index >= 0)
            {
                _ascensionNodeRanks[index] = new AscensionNodeRank(bonusId, rank);
                return;
            }

            _ascensionNodeRanks.Add(new AscensionNodeRank(bonusId, rank));
        }

        // One Keystone per ten Paragon levels, minus those already taken.
        public int KeystonesAvailable => Mathf.Max(0, ParagonLevel / MilestoneLevelInterval - _ascensionRank);

        private int FindAscensionNode(string bonusId)
        {
            if (_ascensionNodeRanks == null)
            {
                return -1;
            }

            for (var index = 0; index < _ascensionNodeRanks.Count; index++)
            {
                if (string.Equals(_ascensionNodeRanks[index].BonusId, bonusId, StringComparison.Ordinal))
                {
                    return index;
                }
            }

            return -1;
        }

        // ---- Quests (P4 as amended by the owner, P6: per character) -----------------------------
        [SerializeField] private List<QuestProgress> _quests = new List<QuestProgress>();

        public int GetQuestProgress(string characterId, string questId)
        {
            var index = FindQuest(characterId, questId);
            return index >= 0 ? _quests[index].Progress : 0;
        }

        public bool IsQuestCompleted(string characterId, string questId)
        {
            var index = FindQuest(characterId, questId);
            return index >= 0 && _quests[index].Completed;
        }

        public void SetQuestProgress(string characterId, string questId, int progress, bool completed)
        {
            _quests ??= new List<QuestProgress>();
            var index = FindQuest(characterId, questId);
            var entry = new QuestProgress(characterId, questId, Mathf.Max(0, progress), completed);

            if (index >= 0)
            {
                _quests[index] = entry;
                return;
            }

            _quests.Add(entry);
        }

        // Set once the legacy boss kills have been carried over into boss quests.
        [SerializeField] private bool _questsMigrated;

        public bool QuestsMigrated => _questsMigrated;

        public void MarkQuestsMigrated()
        {
            _questsMigrated = true;
        }

        private int FindQuest(string characterId, string questId)
        {
            if (_quests == null)
            {
                return -1;
            }

            for (var index = 0; index < _quests.Count; index++)
            {
                var entry = _quests[index];

                if (string.Equals(entry.QuestId, questId, StringComparison.Ordinal) && string.Equals(entry.CharacterId, characterId, StringComparison.Ordinal))
                {
                    return index;
                }
            }

            return -1;
        }

        public void EnsureLevelFloor()
        {
            if (_level < 1)
            {
                _level = 1;
            }
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

    [Serializable]
    public struct AscensionNodeRank
    {
        public string BonusId;
        public int Rank;

        public AscensionNodeRank(string bonusId, int rank)
        {
            BonusId = bonusId;
            Rank = rank;
        }
    }

    // Keyed by character (P6): quests pay SP, and SP is spent in a character's own tree.
    [Serializable]
    public struct QuestProgress
    {
        public string CharacterId;
        public string QuestId;
        public int Progress;
        public bool Completed;

        public QuestProgress(string characterId, string questId, int progress, bool completed)
        {
            CharacterId = characterId;
            QuestId = questId;
            Progress = progress;
            Completed = completed;
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
