namespace Corebound.UI
{
    // The minimal localization mechanism UI.md's "localization-capable, German+English baseline"
    // requirement calls for. Deliberately additive rather than a field-type change: existing
    // ScriptableObject text fields keep their exact current type and already-authored values,
    // and each gains a sibling *_de field plus a resolver that picks between them here, so no
    // already-authored asset data is at risk of being silently dropped by a schema change.
    // IsGerman is not yet wired to a Settings UI control (Settings.md does not define a language
    // option); toggling it programmatically already fully exercises the mechanism end to end.
    public static class LocalizationService
    {
        public static bool IsGerman { get; set; }

        public static string Resolve(string englishText, string germanText)
        {
            return IsGerman && !string.IsNullOrEmpty(germanText) ? germanText : englishText;
        }
    }
}
