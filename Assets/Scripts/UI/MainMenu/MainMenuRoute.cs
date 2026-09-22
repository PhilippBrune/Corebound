namespace Corebound.UI.MainMenu
{
    public enum MainMenuRoute
    {
        Continue,
        NewRun,
        Tutorial,
        Settings,
        MetaProgression,
        Quit,

        // Appended 2026-09-22 (SaveSystem.md, "Drei Profile"). Never inserted: MenuEntryData and
        // MainMenuRoutePanelData serialize this enum by index.
        Profile
    }
}