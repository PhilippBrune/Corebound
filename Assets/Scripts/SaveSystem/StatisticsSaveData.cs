using System;
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

        public int SaveFormatVersion => _saveFormatVersion;

        public int TotalEnemyKills => _totalEnemyKills;

        public int TotalRunsStarted => _totalRunsStarted;

        public int TotalRunsCompleted => _totalRunsCompleted;

        public int TotalRunsFailed => _totalRunsFailed;

        public int HighestStageReached => _highestStageReached;

        public double TotalPlaytimeSeconds => _totalPlaytimeSeconds;

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
                _totalPlaytimeSeconds = _totalPlaytimeSeconds
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
