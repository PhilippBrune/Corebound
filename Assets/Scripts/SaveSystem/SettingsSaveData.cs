using System;
using UnityEngine;

namespace Corebound.SaveSystem
{
    [Serializable]
    public sealed class SettingsSaveData
    {
        [SerializeField] private int _saveFormatVersion = 1;
        [SerializeField] private float _musicVolume = 0.8f;
        [SerializeField] private float _sfxVolume = 0.85f;
        [SerializeField] private float _uiVolume = 0.9f;
        [SerializeField] private int _qualityPresetIndex = 1;
        [SerializeField] private int _resolutionPresetIndex = 2;
        [SerializeField] private int _fullscreenModeIndex = 1;
        [SerializeField] private bool _vSyncEnabled = true;
        [SerializeField] private float _screenShakeIntensity = 0.5f;
        [SerializeField] private float _effectsIntensity = 0.75f;
        [SerializeField] private float _hudScale = 1f;

        // Run timer / XP bar visibility. Default false - see SettingsOptionKey.ShowRunTimer.
        [SerializeField] private bool _showRunTimer;
        [SerializeField] private float _controllerAimAssistStrength = 0.65f;
        [SerializeField] private int _controlProfileIndex;
        [SerializeField] private int _languageIndex;
        [SerializeField] private int _inputDeviceIndex;

        // Core alerts (CORE_PROTECTION_FEEL P1-1). True by default - a settings file written before
        // the field existed loads it as true too, since JsonUtility keeps the initialiser.
        [SerializeField] private bool _showCoreAlerts = true;

        public int SaveFormatVersion => _saveFormatVersion;

        public float MusicVolume => _musicVolume;

        public float SfxVolume => _sfxVolume;

        public float UiVolume => _uiVolume;

        public int QualityPresetIndex => _qualityPresetIndex;

        public int ResolutionPresetIndex => _resolutionPresetIndex;

        public int FullscreenModeIndex => _fullscreenModeIndex;

        public bool VSyncEnabled => _vSyncEnabled;

        public float ScreenShakeIntensity => _screenShakeIntensity;

        public float EffectsIntensity => _effectsIntensity;

        public float HudScale => _hudScale;

        public bool ShowRunTimer => _showRunTimer;

        public float ControllerAimAssistStrength => _controllerAimAssistStrength;

        public int ControlProfileIndex => _controlProfileIndex;

        // 0 = English, 1 = German. Index rather than a bool so a third language needs no
        // save-format change.
        public int LanguageIndex => _languageIndex;

        // 0 = automatic, 1 = controller, 2 = mouse and keyboard (spec review A12).
        public int InputDeviceIndex => _inputDeviceIndex;

        public bool ShowCoreAlerts => _showCoreAlerts;

        public void SetInputDeviceIndex(int value)
        {
            _inputDeviceIndex = Mathf.Clamp(value, 0, 2);
        }

        public void SetShowCoreAlerts(bool value)
        {
            _showCoreAlerts = value;
        }

        // Every field. Apply saves a clone of the edited copy, so a field missing here is a setting
        // that silently reverts: _showRunTimer and _inputDeviceIndex were missing until 2026-10-12,
        // which made "Show Run Timer" and "Input Device" impossible to keep.
        public SettingsSaveData Clone()
        {
            return new SettingsSaveData
            {
                _saveFormatVersion = _saveFormatVersion,
                _musicVolume = _musicVolume,
                _sfxVolume = _sfxVolume,
                _uiVolume = _uiVolume,
                _qualityPresetIndex = _qualityPresetIndex,
                _resolutionPresetIndex = _resolutionPresetIndex,
                _fullscreenModeIndex = _fullscreenModeIndex,
                _vSyncEnabled = _vSyncEnabled,
                _screenShakeIntensity = _screenShakeIntensity,
                _effectsIntensity = _effectsIntensity,
                _hudScale = _hudScale,
                _showRunTimer = _showRunTimer,
                _controllerAimAssistStrength = _controllerAimAssistStrength,
                _controlProfileIndex = _controlProfileIndex,
                _languageIndex = _languageIndex,
                _inputDeviceIndex = _inputDeviceIndex,
                _showCoreAlerts = _showCoreAlerts
            };
        }

        public void SetMusicVolume(float value)
        {
            _musicVolume = Mathf.Clamp01(value);
        }

        public void SetSfxVolume(float value)
        {
            _sfxVolume = Mathf.Clamp01(value);
        }

        public void SetUiVolume(float value)
        {
            _uiVolume = Mathf.Clamp01(value);
        }

        public void SetQualityPresetIndex(int value)
        {
            _qualityPresetIndex = Mathf.Max(0, value);
        }

        public void SetResolutionPresetIndex(int value)
        {
            _resolutionPresetIndex = Mathf.Max(0, value);
        }

        // 0 Windowed, 1 Borderless Fullscreen, 2 Exclusive Fullscreen, 3 Frameless Window
        // (appended 2026-10-12; see WindowModeController).
        public void SetFullscreenModeIndex(int value)
        {
            _fullscreenModeIndex = Mathf.Clamp(value, 0, 3);
        }

        public void SetVSyncEnabled(bool value)
        {
            _vSyncEnabled = value;
        }

        public void SetScreenShakeIntensity(float value)
        {
            _screenShakeIntensity = Mathf.Clamp01(value);
        }

        public void SetEffectsIntensity(float value)
        {
            _effectsIntensity = Mathf.Clamp01(value);
        }

        public void SetShowRunTimer(bool value)
        {
            _showRunTimer = value;
        }

        public void SetHudScale(float value)
        {
            _hudScale = Mathf.Clamp(value, 0.8f, 1.4f);
        }

        public void SetControllerAimAssistStrength(float value)
        {
            _controllerAimAssistStrength = Mathf.Clamp01(value);
        }

        public void SetLanguageIndex(int value)
        {
            _languageIndex = Mathf.Max(0, value);
        }

        public void SetControlProfileIndex(int value)
        {
            _controlProfileIndex = Mathf.Max(0, value);
        }
    }
}