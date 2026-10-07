using UnityEngine;

namespace Corebound.UI.MainMenu
{
    [CreateAssetMenu(menuName = "Corebound/Main Menu/Menu Entry Data", fileName = "MenuEntryData")]
    public sealed class MenuEntryData : ScriptableObject
    {
        [SerializeField] private string _label = string.Empty;

        // German was missing on the main menu's own buttons (seen on screen 2026-10-07).
        [SerializeField] private string _labelDe = string.Empty;
        [SerializeField] private MainMenuRoute _route = MainMenuRoute.NewRun;
        [SerializeField] private MainMenuEntryAvailability _availability = MainMenuEntryAvailability.Always;

        public string Label => Corebound.UI.LocalizationService.Resolve(_label, _labelDe);

        public MainMenuRoute Route => _route;

        public MainMenuEntryAvailability Availability => _availability;
    }
}