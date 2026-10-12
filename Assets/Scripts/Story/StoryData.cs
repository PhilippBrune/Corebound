using System;
using Corebound.Audio;
using Corebound.UI;
using UnityEngine;

namespace Corebound.Story
{
    // Docs/Specs/DIY/STORY_2026-09-30.md, owner decisions S1-2, S2-1, S3-2, S4-1, S5-1: a light story
    // told by the Core in one or two lines at moments the game already pauses, plus Core Logs in
    // Records for the players who want more. Every line the game shows lives in this one asset,
    // English and German side by side, so the story can be read, reviewed and edited in one place
    // instead of being scattered over stage and boss assets.
    [Serializable]
    public sealed class StoryLine
    {
        [SerializeField] private string _en = string.Empty;
        [SerializeField] private string _de = string.Empty;

        public StoryLine()
        {
        }

        // For field defaults in code: a line added after the asset was last saved still has its
        // words (CLAUDE.md: German belongs in the C# default, not only in the asset).
        public StoryLine(string en, string de)
        {
            _en = en;
            _de = de;
        }

        public string Text => LocalizationService.Resolve(_en, _de);

        public bool IsEmpty => string.IsNullOrWhiteSpace(_en);
    }

    // What the Core says around one stage: arriving in it, when its boss appears, when the boss falls.
    [Serializable]
    public sealed class StageStory
    {
        [SerializeField] private int _stageIndex = 1;
        [SerializeField] private StoryLine _arrival = new StoryLine();
        [SerializeField] private StoryLine _bossArrival = new StoryLine();
        [SerializeField] private StoryLine _bossDefeat = new StoryLine();

        public int StageIndex => _stageIndex;

        public StoryLine Arrival => _arrival;

        public StoryLine BossArrival => _bossArrival;

        public StoryLine BossDefeat => _bossDefeat;
    }

    // Serialized by index: append only.
    public enum CoreLogUnlock
    {
        Always = 0,
        RunsStarted = 1,        // value: runs started at least
        BossDefeated = 2,       // value: stage whose boss has been defeated
        Level = 3,              // value: level reached (Paragon counts as past it)
        TowerMastered = 4,      // any tower upgraded to its highest level in a run
        FactoryTier = 5,        // value: chain tier reached in a run
        EliteKills = 6,         // value: elites defeated in total
        FullClear = 7,          // the game cleared once
        ParagonLevel = 8        // value: Paragon level reached
    }

    [Serializable]
    public sealed class CoreLogEntry
    {
        [SerializeField] private string _logId = "log";
        [SerializeField] private StoryLine _title = new StoryLine();
        [SerializeField] private StoryLine _text = new StoryLine();
        [SerializeField] private CoreLogUnlock _unlock;
        [SerializeField] private int _unlockValue;
        [SerializeField] private StoryLine _unlockHint = new StoryLine();

        public string LogId => _logId;

        public StoryLine Title => _title;

        public StoryLine Text => _text;

        public CoreLogUnlock Unlock => _unlock;

        public int UnlockValue => _unlockValue;

        public StoryLine UnlockHint => _unlockHint;
    }

    [CreateAssetMenu(menuName = "Corebound/Story/Story Data", fileName = "StoryData")]
    public sealed class StoryData : ScriptableObject
    {
        [Header("Before the first run (S5-1: once per profile, replayable from Records)")]
        [SerializeField] private StoryLine _premiseTitle = new StoryLine();
        [SerializeField] private StoryLine[] _premise = Array.Empty<StoryLine>();

        [Header("The descent")]
        [SerializeField] private StageStory[] _stages = Array.Empty<StageStory>();

        [Header("The Core hurt (CORE_PROTECTION_FEEL P2-1: once per stage each, mid-wave)")]
        [SerializeField] private StoryLine _coreHurtFirst = new StoryLine("They are inside the ring.", "Sie sind schon im Ring.");
        [SerializeField] private StoryLine _coreHurtSecond = new StoryLine("I cannot hold this alone.", "Allein halte ich das nicht.");

        [Header("Run end")]
        [SerializeField] private StoryLine[] _runLostLines = Array.Empty<StoryLine>();
        [SerializeField] private StoryLine _runWonLine = new StoryLine();

        [Header("The ending (S4-1: once, at the first full clear)")]
        [SerializeField] private StoryLine _endingTitle = new StoryLine();
        [SerializeField] private StoryLine[] _ending = Array.Empty<StoryLine>();
        [SerializeField] private AudioCueData _victorySting;

        [Header("Records")]
        [SerializeField] private CoreLogEntry[] _coreLogs = Array.Empty<CoreLogEntry>();
        [SerializeField] private StoryLine[] _credits = Array.Empty<StoryLine>();
        [SerializeField] private AudioClip _creditsLoop;
        [SerializeField] private float _creditsVolume = 0.5f;

        public StoryLine PremiseTitle => _premiseTitle;

        public StoryLine[] Premise => _premise;

        // Spoken when the Core falls below CoreData's first and second hurt thresholds (50% / 25%).
        public StoryLine CoreHurtFirst => _coreHurtFirst;

        public StoryLine CoreHurtSecond => _coreHurtSecond;

        public StoryLine[] RunLostLines => _runLostLines;

        public StoryLine RunWonLine => _runWonLine;

        public StoryLine EndingTitle => _endingTitle;

        public StoryLine[] Ending => _ending;

        public AudioCueData VictorySting => _victorySting;

        public CoreLogEntry[] CoreLogs => _coreLogs;

        public StoryLine[] Credits => _credits;

        public AudioClip CreditsLoop => _creditsLoop;

        public float CreditsVolume => Mathf.Clamp01(_creditsVolume);

        public StageStory FindStage(int stageIndex)
        {
            for (var index = 0; _stages != null && index < _stages.Length; index++)
            {
                if (_stages[index] != null && _stages[index].StageIndex == stageIndex)
                {
                    return _stages[index];
                }
            }

            return null;
        }
    }
}
