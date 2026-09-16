using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace Corebound.EditorTools
{
    // Playtest request (2026-09-16): "so I see what I'm changing" - RewardCanvas.unity and
    // ShopCanvas.unity are now separately authored scenes (see AdditiveCanvasSceneLoader for the
    // runtime/Play-mode equivalent of this), which means opening SampleScene alone no longer shows
    // their content in the Editor at all. This mirrors that scene automatically whenever SampleScene
    // is opened as the primary (Single-mode) scene, so both canvases are visible and directly
    // editable in the Hierarchy/Scene view without an extra manual step every time.
    [InitializeOnLoad]
    public static class AdditiveCanvasSceneAutoLoader
    {
        private const string GameplaySceneName = "SampleScene";

        private static readonly string[] CompanionScenePaths =
        {
            "Assets/Scenes/RewardCanvas.unity",
            "Assets/Scenes/ShopCanvas.unity",
            "Assets/Scenes/HUDCanvas.unity",
        };

        static AdditiveCanvasSceneAutoLoader()
        {
            EditorSceneManager.sceneOpened -= OnSceneOpened;
            EditorSceneManager.sceneOpened += OnSceneOpened;
        }

        private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || mode != OpenSceneMode.Single || scene.name != GameplaySceneName)
            {
                return;
            }

            foreach (var path in CompanionScenePaths)
            {
                if (!IsSceneAlreadyLoaded(path))
                {
                    EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                }
            }
        }

        private static bool IsSceneAlreadyLoaded(string scenePath)
        {
            for (var index = 0; index < SceneManager.sceneCount; index++)
            {
                if (SceneManager.GetSceneAt(index).path == scenePath)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
