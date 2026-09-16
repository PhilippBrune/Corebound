using System;
using UnityEngine;

namespace Corebound.UI.MainMenu
{
    [Serializable]
    public struct MainMenuState
    {
        [SerializeField] private bool _hasContinueRun;
        [SerializeField] private bool _hasSeenTutorial;
        [SerializeField] private bool _isMetaProgressionUnlocked;

        public MainMenuState(bool hasContinueRun, bool hasSeenTutorial, bool isMetaProgressionUnlocked)
        {
            _hasContinueRun = hasContinueRun;
            _hasSeenTutorial = hasSeenTutorial;
            _isMetaProgressionUnlocked = isMetaProgressionUnlocked;
        }

        public bool HasContinueRun => _hasContinueRun;

        public bool HasSeenTutorial => _hasSeenTutorial;

        public bool IsMetaProgressionUnlocked => _isMetaProgressionUnlocked;
    }
}