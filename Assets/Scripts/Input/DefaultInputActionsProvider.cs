using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Corebound.Input
{
    internal static class DefaultInputActionsProvider
    {
        private const string AssetPath = "Assets/InputSystem_Actions.inputactions";
        private const string AssetFileName = "InputSystem_Actions.inputactions";

        private static InputActionAsset _cachedInputActions;

        public static InputActionAsset Resolve(InputActionAsset currentInputActions)
        {
            if (currentInputActions != null)
            {
                return currentInputActions;
            }

            if (_cachedInputActions != null)
            {
                return _cachedInputActions;
            }

#if UNITY_EDITOR
            _cachedInputActions = TryLoadImportedAsset();

            if (_cachedInputActions != null)
            {
                return _cachedInputActions;
            }
#endif

            _cachedInputActions = TryLoadFromJson();
            return _cachedInputActions;
        }

        public static bool HasExpectedActionMaps(InputActionAsset inputActions)
        {
            if (inputActions == null)
            {
                return false;
            }

            return inputActions.FindActionMap("Gameplay", false) != null
                && inputActions.FindActionMap("Build", false) != null
                && inputActions.FindActionMap("UI", false) != null
                && inputActions.FindActionMap("System", false) != null;
        }

#if UNITY_EDITOR
        private static InputActionAsset TryLoadImportedAsset()
        {
            var assetsAtPath = AssetDatabase.LoadAllAssetsAtPath(AssetPath);

            for (var index = 0; index < assetsAtPath.Length; index++)
            {
                if (assetsAtPath[index] is not InputActionAsset candidate)
                {
                    continue;
                }

                if (!HasExpectedActionMaps(candidate))
                {
                    continue;
                }

                return candidate;
            }

            var loadedAssets = Resources.FindObjectsOfTypeAll<InputActionAsset>();

            for (var index = 0; index < loadedAssets.Length; index++)
            {
                if (!HasExpectedActionMaps(loadedAssets[index]))
                {
                    continue;
                }

                return loadedAssets[index];
            }

            return null;
        }
#endif

        private static InputActionAsset TryLoadFromJson()
        {
            var assetFilePath = Path.Combine(Application.dataPath, AssetFileName);

            if (!File.Exists(assetFilePath))
            {
                return null;
            }

            var json = File.ReadAllText(assetFilePath);

            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            try
            {
                var inputActions = InputActionAsset.FromJson(json);
                return HasExpectedActionMaps(inputActions) ? inputActions : null;
            }
            catch (System.ArgumentException)
            {
                return null;
            }
        }
    }
}