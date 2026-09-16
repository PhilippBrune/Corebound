using System;
using Corebound.SaveSystem;
using UnityEngine;

namespace Corebound.Settings
{
    [CreateAssetMenu(menuName = "Corebound/Settings/Settings Definition Data", fileName = "SettingsDefinitionData")]
    public sealed class SettingsDefinitionData : ScriptableObject
    {
        [SerializeField] private CategoryDefinition[] _categories = Array.Empty<CategoryDefinition>();

        public CategoryDefinition[] Categories => _categories;

        public SettingsSaveData CreateDefaultSaveData()
        {
            var saveData = new SettingsSaveData();

            for (var categoryIndex = 0; categoryIndex < _categories.Length; categoryIndex++)
            {
                var options = _categories[categoryIndex].Options;

                if (options == null)
                {
                    continue;
                }

                for (var optionIndex = 0; optionIndex < options.Length; optionIndex++)
                {
                    ApplyDefaultValue(saveData, options[optionIndex]);
                }
            }

            return saveData;
        }

        private static void ApplyDefaultValue(SettingsSaveData saveData, OptionDefinition optionDefinition)
        {
            switch (optionDefinition.Key)
            {
                case SettingsOptionKey.MusicVolume:
                    saveData.SetMusicVolume(optionDefinition.DefaultFloatValue);
                    return;
                case SettingsOptionKey.SfxVolume:
                    saveData.SetSfxVolume(optionDefinition.DefaultFloatValue);
                    return;
                case SettingsOptionKey.UiVolume:
                    saveData.SetUiVolume(optionDefinition.DefaultFloatValue);
                    return;
                case SettingsOptionKey.QualityPreset:
                    saveData.SetQualityPresetIndex(optionDefinition.DefaultChoiceIndex);
                    return;
                case SettingsOptionKey.ResolutionPreset:
                    saveData.SetResolutionPresetIndex(optionDefinition.DefaultChoiceIndex);
                    return;
                case SettingsOptionKey.FullscreenMode:
                    saveData.SetFullscreenModeIndex(optionDefinition.DefaultChoiceIndex);
                    return;
                case SettingsOptionKey.VSync:
                    saveData.SetVSyncEnabled(optionDefinition.DefaultToggleValue);
                    return;
                case SettingsOptionKey.ScreenShakeIntensity:
                    saveData.SetScreenShakeIntensity(optionDefinition.DefaultFloatValue);
                    return;
                case SettingsOptionKey.EffectsIntensity:
                    saveData.SetEffectsIntensity(optionDefinition.DefaultFloatValue);
                    return;
                case SettingsOptionKey.HudScale:
                    saveData.SetHudScale(optionDefinition.DefaultFloatValue);
                    return;
                case SettingsOptionKey.ControllerAimAssistStrength:
                    saveData.SetControllerAimAssistStrength(optionDefinition.DefaultFloatValue);
                    return;
                case SettingsOptionKey.ControlProfile:
                    saveData.SetControlProfileIndex(optionDefinition.DefaultChoiceIndex);
                    return;
            }
        }

        [Serializable]
        public sealed class CategoryDefinition
        {
            [SerializeField] private SettingsCategoryType _categoryType;
            [SerializeField] private string _label;
            [SerializeField] private OptionDefinition[] _options;

            public SettingsCategoryType CategoryType => _categoryType;

            public string Label => _label;

            public OptionDefinition[] Options => _options;
        }

        [Serializable]
        public sealed class OptionDefinition
        {
            [SerializeField] private SettingsOptionKey _key;
            [SerializeField] private string _label;
            [SerializeField] [TextArea(2, 3)] private string _description;
            [SerializeField] private SettingsOptionKind _optionKind;
            [SerializeField] private float _minValue;
            [SerializeField] private float _maxValue = 1f;
            [SerializeField] private float _stepValue = 0.1f;
            [SerializeField] private float _defaultFloatValue = 1f;
            [SerializeField] private bool _defaultToggleValue = true;
            [SerializeField] private int _defaultChoiceIndex;
            [SerializeField] private string[] _choiceLabels;

            public SettingsOptionKey Key => _key;

            public string Label => _label;

            public string Description => _description;

            public SettingsOptionKind OptionKind => _optionKind;

            public float MinValue => _minValue;

            public float MaxValue => _maxValue;

            public float StepValue => _stepValue;

            public float DefaultFloatValue => _defaultFloatValue;

            public bool DefaultToggleValue => _defaultToggleValue;

            public int DefaultChoiceIndex => _defaultChoiceIndex;

            public string[] ChoiceLabels => _choiceLabels;
        }
    }
}