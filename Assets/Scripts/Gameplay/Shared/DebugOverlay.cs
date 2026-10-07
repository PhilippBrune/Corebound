namespace Corebound
{
    // Editor captures for the Steam page and the trailer (2026-10-07) hide the debug overlays drawn
    // with OnGUI - the god mode banner and the main menu's stage-select label - without switching
    // the features themselves off.
    public static class DebugOverlay
    {
        public static bool Hidden { get; set; }
    }
}
