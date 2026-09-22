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
        InputDevice
    }
}