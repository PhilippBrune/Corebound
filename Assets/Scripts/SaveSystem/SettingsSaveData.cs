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
        [SerializeField] private float _controllerAimAssistStrength = 0.65f;
        [SerializeField] private int _controlProfileIndex;
        [SerializeField] private int _languageIndex;
        [SerializeField] private int _inputDeviceIndex;

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

        public float ControllerAimAssistStrength => _controllerAimAssistStrength;

        public int ControlProfileIndex => _controlProfileIndex;

        // 0 = English, 1 = German. Index rather than a bool so a third language needs no
        // save-format change.
        public int LanguageIndex => _languageIndex;

        // 0 = automatic, 1 = controller, 2 = mouse and keyboard (spec review A12).
        public int InputDeviceIndex => _inputDeviceIndex;

        public void SetInputDeviceIndex(int value)
        {
            _inputDeviceIndex = Mathf.Clamp(value, 0, 2);
        }

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
                _controllerAimAssistStrength = _controllerAimAssistStrength,
                _controlProfileIndex = _controlProfileIndex,
                _languageIndex = _languageIndex
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

        public void SetFullscreenModeIndex(int value)
        {
            _fullscreenModeIndex = Mathf.Clamp(value, 0, 2);
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