using System;
using Corebound.SaveSystem;
using UnityEngine;

namespace Corebound.Settings
{
    public sealed class SettingsManager : MonoBehaviour
    {
        private static readonly ResolutionPreset[] ResolutionPresets =
        {
            new ResolutionPreset(1280, 720),
            new ResolutionPreset(1600, 900),
            new ResolutionPreset(1920, 1080)
        };

        [SerializeField] private SettingsDefinitionData _definitionData;
        [SerializeField] private UnityEngine.Object _saveManagerSource;

        private SettingsSaveData _savedSettings;
        private SettingsSaveData _runtimeSettings;

        public event Action SettingsApplied;

        public SettingsDefinitionData DefinitionData => _definitionData;

        public SettingsSaveData RuntimeSettings => _runtimeSettings;

        private ISaveManager SaveManager => SaveManagerResolver.Resolve(_saveManagerSource);

        private void Awake()
        {
            EnsureLoaded();
        }

        public SettingsSaveData CreateWorkingCopy()
        {
            EnsureLoaded();
            return _savedSettings.Clone();
        }

        public void ApplyPreview(SettingsSaveData candidate)
        {
            EnsureLoaded();

            if (candidate == null)
            {
                return;
            }

            _runtimeSettings = candidate.Clone();
            ApplyRuntimeSettings(_runtimeSettings);
            SettingsApplied?.Invoke();
        }

        public void Commit(SettingsSaveData candidate)
        {
            EnsureLoaded();

            if (candidate == null)
            {
                return;
            }

            _savedSettings = candidate.Clone();
            _runtimeSettings = _savedSettings.Clone();
            ApplyRuntimeSettings(_runtimeSettings);

            if (SaveManager != null)
            {
                SaveManager.SaveSettingsSaveData(_savedSettings);
            }

            SettingsApplied?.Invoke();
        }

        public void RevertToSaved()
        {
            EnsureLoaded();
            ApplyPreview(_savedSettings);
        }

        public void AdjustValue(SettingsSaveData candidate, SettingsDefinitionData.OptionDefinition optionDefinition, int direction)
        {
            if (candidate == null || direction == 0)
            {
                return;
            }

            switch (optionDefinition.Key)
            {
                case SettingsOptionKey.MusicVolume:
                    candidate.SetMusicVolume(candidate.MusicVolume + optionDefinition.StepValue * direction);
                    return;
                case SettingsOptionKey.SfxVolume:
                    candidate.SetSfxVolume(candidate.SfxVolume + optionDefinition.StepValue * direction);
                    return;
                case SettingsOptionKey.UiVolume:
                    candidate.SetUiVolume(candidate.UiVolume + optionDefinition.StepValue * direction);
                    return;
                case SettingsOptionKey.QualityPreset:
                    candidate.SetQualityPresetIndex(AdjustChoice(candidate.QualityPresetIndex, optionDefinition, direction));
                    return;
                case SettingsOptionKey.ResolutionPreset:
                    candidate.SetResolutionPresetIndex(AdjustChoice(candidate.ResolutionPresetIndex, optionDefinition, direction));
                    return;
                case SettingsOptionKey.FullscreenMode:
                    candidate.SetFullscreenModeIndex(AdjustChoice(candidate.FullscreenModeIndex, optionDefinition, direction));
                    return;
                case SettingsOptionKey.VSync:
                    candidate.SetVSyncEnabled(!candidate.VSyncEnabled);
                    return;
                case SettingsOptionKey.ScreenShakeIntensity:
                    candidate.SetScreenShakeIntensity(candidate.ScreenShakeIntensity + optionDefinition.StepValue * direction);
                    return;
                case SettingsOptionKey.EffectsIntensity:
                    candidate.SetEffectsIntensity(candidate.EffectsIntensity + optionDefinition.StepValue * direction);
                    return;
                case SettingsOptionKey.HudScale:
                    candidate.SetHudScale(candidate.HudScale + optionDefinition.StepValue * direction);
                    return;
                case SettingsOptionKey.ControllerAimAssistStrength:
                    candidate.SetControllerAimAssistStrength(candidate.ControllerAimAssistStrength + optionDefinition.StepValue * direction);
                    return;
                case SettingsOptionKey.ControlProfile:
                    candidate.SetControlProfileIndex(AdjustChoice(candidate.ControlProfileIndex, optionDefinition, direction));
                    return;
                case SettingsOptionKey.Language:
                    candidate.SetLanguageIndex(AdjustChoice(candidate.LanguageIndex, optionDefinition, direction));
                    return;
            }
        }

        public string GetDisplayValue(SettingsSaveData candidate, SettingsDefinitionData.OptionDefinition optionDefinition)
        {
            var settings = candidate ?? _savedSettings;

            switch (optionDefinition.Key)
            {
                case SettingsOptionKey.MusicVolume:
                    return ToPercentLabel(settings.MusicVolume);
                case SettingsOptionKey.SfxVolume:
                    return ToPercentLabel(settings.SfxVolume);
                case SettingsOptionKey.UiVolume:
                    return ToPercentLabel(settings.UiVolume);
                case SettingsOptionKey.QualityPreset:
                    return ResolveChoiceLabel(optionDefinition, settings.QualityPresetIndex);
                case SettingsOptionKey.ResolutionPreset:
                    return ResolveChoiceLabel(optionDefinition, settings.ResolutionPresetIndex);
                case SettingsOptionKey.FullscreenMode:
                    return ResolveChoiceLabel(optionDefinition, settings.FullscreenModeIndex);
                case SettingsOptionKey.VSync:
                    return settings.VSyncEnabled ? "On" : "Off";
                case SettingsOptionKey.ScreenShakeIntensity:
                    return ToPercentLabel(settings.ScreenShakeIntensity);
                case SettingsOptionKey.EffectsIntensity:
                    return ToPercentLabel(settings.EffectsIntensity);
                case SettingsOptionKey.HudScale:
                    return string.Format("{0:0.00}x", settings.HudScale);
                case SettingsOptionKey.ControllerAimAssistStrength:
                    return ToPercentLabel(settings.ControllerAimAssistStrength);
                case SettingsOptionKey.ControlProfile:
                    return ResolveChoiceLabel(optionDefinition, settings.ControlProfileIndex);
                case SettingsOptionKey.Language:
                    return ResolveChoiceLabel(optionDefinition, settings.LanguageIndex);
                default:
                    return string.Empty;
            }
        }

        private void EnsureLoaded()
        {
            if (_savedSettings != null)
            {
                return;
            }

            var defaults = _definitionData != null ? _definitionData.CreateDefaultSaveData() : new SettingsSaveData();
            _savedSettings = SaveManager != null ? SaveManager.LoadSettingsSaveData(defaults) : defaults;
            _runtimeSettings = _savedSettings.Clone();
            ApplyRuntimeSettings(_runtimeSettings);
        }

        private void ApplyRuntimeSettings(SettingsSaveData settings)
        {
            if (settings == null)
            {
                return;
            }

            // Applied on preview as well as on commit, so arrowing through the option changes
            // the Settings screen's own labels immediately - the clearest possible confirmation
            // that the setting took effect.
            Corebound.UI.LocalizationService.IsGerman = settings.LanguageIndex == 1;

            QualitySettings.SetQualityLevel(Mathf.Clamp(settings.QualityPresetIndex, 0, QualitySettings.names.Length > 0 ? QualitySettings.names.Length - 1 : 0), true);
            QualitySettings.vSyncCount = settings.VSyncEnabled ? 1 : 0;

            switch (settings.FullscreenModeIndex)
            {
                case 0:
                    Screen.fullScreenMode = FullScreenMode.Windowed;
                    break;
                case 1:
                    Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
                    break;
                default:
                    Screen.fullScreenMode = FullScreenMode.ExclusiveFullScreen;
                    break;
            }

                    var resolutionPreset = ResolutionPresets[Mathf.Clamp(settings.ResolutionPresetIndex, 0, ResolutionPresets.Length - 1)];
                    Screen.SetResolution(resolutionPreset.Width, resolutionPreset.Height, Screen.fullScreenMode);
        }

        private static int AdjustChoice(int currentIndex, SettingsDefinitionData.OptionDefinition optionDefinition, int direction)
        {
            var choiceCount = optionDefinition.ChoiceLabels != null ? optionDefinition.ChoiceLabels.Length : 0;

            if (choiceCount <= 0)
            {
                return 0;
            }

            var nextIndex = currentIndex + direction;

            if (nextIndex < 0)
            {
                return choiceCount - 1;
            }

            if (nextIndex >= choiceCount)
            {
                return 0;
            }

            return nextIndex;
        }

        private static string ResolveChoiceLabel(SettingsDefinitionData.OptionDefinition optionDefinition, int index)
        {
            if (optionDefinition.ChoiceLabels == null || optionDefinition.ChoiceLabels.Length == 0)
            {
                return string.Empty;
            }

            return optionDefinition.ChoiceLabels[Mathf.Clamp(index, 0, optionDefinition.ChoiceLabels.Length - 1)];
        }

        private static string ToPercentLabel(float value)
        {
            return string.Format("{0:0}%", value * 100f);
        }

        private readonly struct ResolutionPreset
        {
            public ResolutionPreset(int width, int height)
            {
                Width = width;
                Height = height;
            }

            public int Width { get; }

            public int Height { get; }
        }
    }
}