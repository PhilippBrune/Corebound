using System;
using System.Collections.Generic;
using UnityEngine;

namespace Corebound.SaveSystem
{
    public enum RunOutcome
    {
        Died,
        Completed
    }

    // One finished run. Owner decisions 2026-09-28 (Docs/Specs/DIY/RUN_TIMER_AND_HISTORY_2026-09-27.md):
    // D3 records victories and deaths only - a run abandoned to the main menu leaves no trace,
    // because the history is a record of finished attempts rather than of every session.
    [Serializable]
    public sealed class RunHistoryEntry
    {
        [SerializeField] private long _startedUtcTicks;
        [SerializeField] private int _outcome;
        [SerializeField] private float _elapsedActiveSeconds;
        [SerializeField] private float _elapsedRealSeconds;
        [SerializeField] private int _stageReached = 1;
        [SerializeField] private int _waveReached = 1;
        [SerializeField] private string _endedByLabel = string.Empty;
        [SerializeField] private List<string> _weaponIds = new List<string>();
        [SerializeField] private List<int> _weaponTiers = new List<int>();
        [SerializeField] private List<string> _cardIds = new List<string>();
        [SerializeField] private List<string> _relicIds = new List<string>();
        [SerializeField] private List<string> _towerIds = new List<string>();
        [SerializeField] private List<int> _towerLevels = new List<int>();
        [SerializeField] private int _scrapEarned;
        [SerializeField] private int _alloyEarned;
        [SerializeField] private int _skillPointsEarned;
        [SerializeField] private int _enemiesKilled;
        [SerializeField] private int _profileIndex = 1;
        [SerializeField] private int _ascensionRank;

        public DateTime StartedUtc => new DateTime(_startedUtcTicks, DateTimeKind.Utc);

        public long StartedUtcTicks => _startedUtcTicks;

        public RunOutcome Outcome => (RunOutcome)_outcome;

        public float ElapsedActiveSeconds => _elapsedActiveSeconds;

        public float ElapsedRealSeconds => _elapsedRealSeconds;

        public int StageReached => _stageReached;

        public int WaveReached => _waveReached;

        public string EndedByLabel => _endedByLabel ?? string.Empty;

        public IReadOnlyList<string> WeaponIds => _weaponIds;

        public IReadOnlyList<int> WeaponTiers => _weaponTiers;

        public IReadOnlyList<string> CardIds => _cardIds;

        public IReadOnlyList<string> RelicIds => _relicIds;

        public IReadOnlyList<string> TowerIds => _towerIds;

        public IReadOnlyList<int> TowerLevels => _towerLevels;

        public int ScrapEarned => _scrapEarned;

        public int AlloyEarned => _alloyEarned;

        public int SkillPointsEarned => _skillPointsEarned;

        public int EnemiesKilled => _enemiesKilled;

        public int ProfileIndex => _profileIndex;

        public int AscensionRank => _ascensionRank;

        public static RunHistoryEntry Create(
            DateTime startedUtc,
            RunOutcome outcome,
            float elapsedActiveSeconds,
            float elapsedRealSeconds,
            int stageReached,
            int waveReached,
            string endedByLabel,
            int profileIndex,
            int ascensionRank)
        {
            return new RunHistoryEntry
            {
                _startedUtcTicks = startedUtc.Ticks,
                _outcome = (int)outcome,
                _elapsedActiveSeconds = Mathf.Max(0f, elapsedActiveSeconds),
                _elapsedRealSeconds = Mathf.Max(0f, elapsedRealSeconds),
                _stageReached = Mathf.Max(1, stageReached),
                _waveReached = Mathf.Max(1, waveReached),
                _endedByLabel = endedByLabel ?? string.Empty,
                _profileIndex = Mathf.Max(1, profileIndex),
                _ascensionRank = Mathf.Max(0, ascensionRank)
            };
        }

        public void SetBuild(
            IReadOnlyList<string> weaponIds,
            IReadOnlyList<int> weaponTiers,
            IReadOnlyList<string> cardIds,
            IReadOnlyList<string> relicIds,
            IReadOnlyList<string> towerIds,
            IReadOnlyList<int> towerLevels)
        {
            Fill(_weaponIds, weaponIds);
            Fill(_weaponTiers, weaponTiers);
            Fill(_cardIds, cardIds);
            Fill(_relicIds, relicIds);
            Fill(_towerIds, towerIds);
            Fill(_towerLevels, towerLevels);
        }

        public void SetEarnings(int scrap, int alloy, int skillPoints, int enemiesKilled)
        {
            _scrapEarned = Mathf.Max(0, scrap);
            _alloyEarned = Mathf.Max(0, alloy);
            _skillPointsEarned = Mathf.Max(0, skillPoints);
            _enemiesKilled = Mathf.Max(0, enemiesKilled);
        }

        // "How far" as one comparable number, so ranking runs does not need a tuple comparison at
        // every call site. Stage dominates; wave breaks ties within a stage.
        public int DepthScore => _stageReached * 1000 + _waveReached;

        private static void Fill<T>(List<T> target, IReadOnlyList<T> source)
        {
            target.Clear();

            if (source == null)
            {
                return;
            }

            for (var index = 0; index < source.Count; index++)
            {
                target.Add(source[index]);
            }
        }
    }

    // D5: its own per-profile file rather than an array inside statistics.json, which is rewritten
    // on every enemy kill - appending history there would re-serialise the whole list per death.
    [Serializable]
    public sealed class RunHistorySaveData
    {
        // D6: the last RecentLimit runs, plus the best run per stage reached so a personal best is
        // never aged out by a streak of bad runs.
        public const int RecentLimit = 50;

        [SerializeField] private int _saveFormatVersion = 1;
        [SerializeField] private List<RunHistoryEntry> _entries = new List<RunHistoryEntry>();

        public int SaveFormatVersion => _saveFormatVersion;

        public IReadOnlyList<RunHistoryEntry> Entries => _entries;

        public int RunCount => _entries.Count;

        public RunHistoryEntry BestRun
        {
            get
            {
                RunHistoryEntry best = null;

                for (var index = 0; index < _entries.Count; index++)
                {
                    if (best == null || _entries[index].DepthScore > best.DepthScore)
                    {
                        best = _entries[index];
                    }
                }

                return best;
            }
        }

        public void Add(RunHistoryEntry entry)
        {
            if (entry == null)
            {
                return;
            }

            _entries.Add(entry);
            Prune();
        }

        // Newest first, which is the order the Records screen reads in.
        public void SortNewestFirst()
        {
            _entries.Sort((a, b) => b.StartedUtcTicks.CompareTo(a.StartedUtcTicks));
        }

        private void Prune()
        {
            if (_entries.Count <= RecentLimit)
            {
                return;
            }

            SortNewestFirst();

            // Keep the best run for each stage even when it falls outside the recent window - that
            // is the half of D6 that makes the screen able to answer "what is my best", not just
            // "how am I doing lately".
            var keptBestByStage = new Dictionary<int, RunHistoryEntry>();

            for (var index = 0; index < _entries.Count; index++)
            {
                var entry = _entries[index];

                if (!keptBestByStage.TryGetValue(entry.StageReached, out var best) || entry.DepthScore > best.DepthScore)
                {
                    keptBestByStage[entry.StageReached] = entry;
                }
            }

            var kept = new List<RunHistoryEntry>();

            for (var index = 0; index < _entries.Count; index++)
            {
                var entry = _entries[index];

                if (index < RecentLimit || keptBestByStage.ContainsValue(entry))
                {
                    kept.Add(entry);
                }
            }

            _entries = kept;
        }

        public RunHistorySaveData Clone()
        {
            var clone = new RunHistorySaveData { _saveFormatVersion = _saveFormatVersion };

            for (var index = 0; index < _entries.Count; index++)
            {
                clone._entries.Add(_entries[index]);
            }

            return clone;
        }
    }
}
