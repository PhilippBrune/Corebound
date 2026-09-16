using Corebound.MetaProgression;
using Corebound.SaveSystem;
using UnityEngine;

namespace Corebound.UI.MainMenu
{
    public sealed class SaveBackedMainMenuStateProvider : MonoBehaviour, IMainMenuStateProvider
    {
        [SerializeField] private UnityEngine.Object _saveManagerSource;
        [SerializeField] private MetaProgressionService _metaProgressionService;

        private ISaveManager SaveManager => SaveManagerResolver.Resolve(_saveManagerSource);

        public MainMenuState GetCurrentState()
        {
            var runSaveData = SaveManager != null ? SaveManager.LoadRunSaveData() : null;
            var hasContinueRun = runSaveData != null && runSaveData.HasActiveRun;
            var hasSeenTutorial = _metaProgressionService != null && _metaProgressionService.HasSeenTutorial;
            var isMetaProgressionUnlocked = _metaProgressionService != null && _metaProgressionService.IsMetaProgressionUnlocked;

            return new MainMenuState(hasContinueRun, hasSeenTutorial, isMetaProgressionUnlocked);
        }
    }
}