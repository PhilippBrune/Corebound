using System;
using System.Collections.Generic;
using UnityEngine;

namespace Corebound.SaveSystem
{
    [Serializable]
    public sealed class StatisticsSaveData
    {
        [SerializeField] private int _saveFormatVersion = 1;
        [SerializeField] private int _totalEnemyKills;
        [SerializeField] private int _totalRunsStarted;
        [SerializeField] private int _totalRunsCompleted;
        [SerializeField] private int _totalRunsFailed;
        [SerializeField] private int _highestStageReached;
        [SerializeField] private double _totalPlaytimeSeconds;

        // Card ids this profile has ever taken into a run, for the Records collection. A set in
        // spirit; a list because JsonUtility cannot serialise a HashSet.
        [SerializeField] private List<string> _collectedCardIds = new List<string>();

        public int SaveFormatVersion => _saveFormatVersion;

        public int TotalEnemyKills => _totalEnemyKills;

        public int TotalRunsStarted => _totalRunsStarted;

        public int TotalRunsCompleted => _totalRunsCompleted;

        public int TotalRunsFailed => _totalRunsFailed;

        public int HighestStageReached => _highestStageReached;

        public double TotalPlaytimeSeconds => _totalPlaytimeSeconds;

        public IReadOnlyList<string> CollectedCardIds => _collectedCardIds;

        public bool HasCollectedCard(string cardId)
        {
            return !string.IsNullOrEmpty(cardId) && _collectedCardIds != null && _collectedCardIds.Contains(cardId);
        }

        // Returns true when the id was new, so a caller can skip the save write otherwise.
        public bool RecordCardCollected(string cardId)
        {
            if (string.IsNullOrEmpty(cardId))
            {
                return false;
            }

            if (_collectedCardIds == null)
            {
                _collectedCardIds = new List<string>();
            }

            if (_collectedCardIds.Contains(cardId))
            {
                return false;
            }

            _collectedCardIds.Add(cardId);
            return true;
        }

        public StatisticsSaveData Clone()
        {
            return new StatisticsSaveData
            {
                _saveFormatVersion = _saveFormatVersion,
                _totalEnemyKills = _totalEnemyKills,
                _totalRunsStarted = _totalRunsStarted,
                _totalRunsCompleted = _totalRunsCompleted,
                _totalRunsFailed = _totalRunsFailed,
                _highestStageReached = _highestStageReached,
                _totalPlaytimeSeconds = _totalPlaytimeSeconds,
                _collectedCardIds = _collectedCardIds != null ? new List<string>(_collectedCardIds) : new List<string>()
            };
        }

        public void RecordEnemyKill()
        {
            _totalEnemyKills += 1;
        }

        public void RecordRunStarted()
        {
            _totalRunsStarted += 1;
        }

        public void RecordRunCompleted(int stageReached)
        {
            _totalRunsCompleted += 1;
            _highestStageReached = Mathf.Max(_highestStageReached, stageReached);
        }

        public void RecordRunFailed(int stageReached)
        {
            _totalRunsFailed += 1;
            _highestStageReached = Mathf.Max(_highestStageReached, stageReached);
        }

        public void AddPlaytimeSeconds(double seconds)
        {
            if (seconds > 0d)
            {
                _totalPlaytimeSeconds += seconds;
            }
        }
    }
}
