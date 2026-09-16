using Corebound.Settings;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Corebound.Input
{
    public sealed class InputRebindManager : MonoBehaviour
    {
        [SerializeField] private InputActionAsset _inputActions;
        [SerializeField] private SettingsManager _settingsManager;
        [SerializeField] private RebindProfileData[] _profiles;

        private void Awake()
        {
            ApplySelectedProfile();
        }

        private void OnEnable()
        {
            if (_settingsManager != null)
            {
                _settingsManager.SettingsApplied += OnSettingsApplied;
            }

            ApplySelectedProfile();
        }

        private void OnDisable()
        {
            if (_settingsManager != null)
            {
                _settingsManager.SettingsApplied -= OnSettingsApplied;
            }
        }

        private void OnSettingsApplied()
        {
            ApplySelectedProfile();
        }

        private void ApplySelectedProfile()
        {
            _inputActions = DefaultInputActionsProvider.Resolve(_inputActions);

            if (_inputActions == null || _settingsManager == null)
            {
                return;
            }

            _inputActions.RemoveAllBindingOverrides();

            var runtimeSettings = _settingsManager.RuntimeSettings;
            var selectedIndex = runtimeSettings != null ? runtimeSettings.ControlProfileIndex : 0;

            if (_profiles == null || selectedIndex < 0 || selectedIndex >= _profiles.Length || _profiles[selectedIndex] == null)
            {
                return;
            }

            var bindingOverrides = _profiles[selectedIndex].BindingOverrides;

            if (bindingOverrides == null)
            {
                return;
            }

            for (var index = 0; index < bindingOverrides.Length; index++)
            {
                var bindingOverride = bindingOverrides[index];
                var actionMap = _inputActions.FindActionMap(bindingOverride.MapName, false);

                if (actionMap == null)
                {
                    continue;
                }

                var action = actionMap.FindAction(bindingOverride.ActionName, false);

                if (action == null)
                {
                    continue;
                }

                if (bindingOverride.BindingIndex < 0 || bindingOverride.BindingIndex >= action.bindings.Count)
                {
                    continue;
                }

                action.ApplyBindingOverride(bindingOverride.BindingIndex, bindingOverride.BindingPath);
            }
        }
    }
}