using UnityEngine;

namespace Corebound.UI.MainMenu
{
    [CreateAssetMenu(menuName = "Corebound/Main Menu/Tutorial Entry Data", fileName = "TutorialEntryData")]
    public sealed class TutorialEntryData : ScriptableObject
    {
        [SerializeField] private string _label = "Tutorial";
        [SerializeField] private MainMenuRoute _route = MainMenuRoute.Tutorial;
        [SerializeField] private bool _showAfterCompletion = true;

        public string Label => _label;

        public MainMenuRoute Route => _route;

        public bool ShowAfterCompletion => _showAfterCompletion;
    }
}