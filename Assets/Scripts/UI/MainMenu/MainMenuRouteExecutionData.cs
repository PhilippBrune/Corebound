using UnityEngine;

namespace Corebound.UI.MainMenu
{
    [CreateAssetMenu(menuName = "Corebound/Main Menu/Main Menu Route Execution Data", fileName = "MainMenuRouteExecutionData")]
    public sealed class MainMenuRouteExecutionData : ScriptableObject
    {
        [SerializeField] private string _continueSceneName = "SampleScene";
        [SerializeField] private string _newRunSceneName = "SampleScene";
        [SerializeField] private string _tutorialSceneName = "SampleScene";
        [SerializeField] private MainMenuRoutePanelData _newRunConfirmationPanelData;
        [SerializeField] private MainMenuRoutePanelData _quitConfirmationPanelData;
        [SerializeField] private MainMenuRoutePanelData _settingsPanelData;
        [SerializeField] private MainMenuRoutePanelData _metaProgressionPanelData;
        [SerializeField] private MainMenuRoutePanelData _profilePanelData;
        [SerializeField] private MainMenuRoutePanelData _recordsPanelData;

        public string ContinueSceneName => _continueSceneName;

        public string NewRunSceneName => _newRunSceneName;

        public string TutorialSceneName => _tutorialSceneName;

        public MainMenuRoutePanelData NewRunConfirmationPanelData => _newRunConfirmationPanelData;

        public MainMenuRoutePanelData QuitConfirmationPanelData => _quitConfirmationPanelData;

        public MainMenuRoutePanelData SettingsPanelData => _settingsPanelData;

        public MainMenuRoutePanelData MetaProgressionPanelData => _metaProgressionPanelData;

        public MainMenuRoutePanelData ProfilePanelData => _profilePanelData;

        public MainMenuRoutePanelData RecordsPanelData => _recordsPanelData;
    }
}