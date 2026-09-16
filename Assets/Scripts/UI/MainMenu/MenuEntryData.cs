using UnityEngine;

namespace Corebound.UI.MainMenu
{
    [CreateAssetMenu(menuName = "Corebound/Main Menu/Menu Entry Data", fileName = "MenuEntryData")]
    public sealed class MenuEntryData : ScriptableObject
    {
        [SerializeField] private string _label = string.Empty;
        [SerializeField] private MainMenuRoute _route = MainMenuRoute.NewRun;
        [SerializeField] private MainMenuEntryAvailability _availability = MainMenuEntryAvailability.Always;

        public string Label => _label;

        public MainMenuRoute Route => _route;

        public MainMenuEntryAvailability Availability => _availability;
    }
}