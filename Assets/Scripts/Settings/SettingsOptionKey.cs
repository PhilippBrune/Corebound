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
        Language
    }
}