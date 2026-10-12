namespace Corebound.Settings
{
    public enum SettingsOptionKey
    {
        MusicVolume,
        SfxVolume,
        UiVolume,
        QualityPreset,
        ResolutionPreset,
        FullscreenMode,
        VSync,
        ScreenShakeIntensity,
        EffectsIntensity,
        HudScale,
        ControllerAimAssistStrength,
        ControlProfile,

        // Appended, not inserted: this enum is serialized by index in
        // SettingsDefinition_Default.asset, so an insert would silently repoint every option
        // after it. Drives LocalizationService.IsGerman, which until now was settable only
        // from code - the German translation existed but the player could not reach it.
        Language,

        // Appended 2026-09-22 (spec review A12). Auto / Controller / Mouse: "Auto" keeps the
        // stick-vs-pointer thresholds in CoreboundInputReader, the other two freeze the device,
        // which is the escape hatch for a connected controller that drifts.
        InputDevice,

        // Appended 2026-09-28 (run timer D2, and shared by the XP bar per levelling L4). Off by
        // default: a permanent clock turns a roguelite into a speedrun for players who did not
        // ask for one, and Vision section 19's readability boundary argues against another
        // always-on number. Players who want it will find the toggle.
        ShowRunTimer,

        // Appended 2026-10-12 (CORE_PROTECTION_FEEL P1-1): threat lines, the off-screen Core marker
        // and the screen-edge pulse on a Core hit. On by default; the concept promised a switch
        // for players who find them busy.
        ShowCoreAlerts
    }
}