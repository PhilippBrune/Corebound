using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Corebound.Gameplay.Shared
{
    // Loads the standalone UI canvas scenes (Assets/Scenes/RewardCanvas.unity,
    // Assets/Scenes/ShopCanvas.unity) additively whenever the gameplay scene loads. These canvases
    // used to be procedurally built in C# (with hardcoded pixel offsets) as children of their
    // presenter's own GameObject inside SampleScene - which is what let a domain-reload rebuild
    // silently leave a fresh CanvasGroup at its default (fully visible) state and block gameplay
    // (2026-09-15 playtest). Moving them into their own authored scenes means their layout lives
    // entirely in data the user can open and edit directly in the Editor, and the presenters
    // (CardRewardPresenter, ShopPresenter) only ever bind to already-existing content - they never
    // create anything.
    //
    // Ordering: SceneManager.sceneLoaded fires after the newly loaded scene's own objects have had
    // Awake()/OnEnable() called, but before any object's Start(). Triggering the additive loads
    // here (synchronously) means CardRewardPresenter/ShopPresenter - which defer their canvas
    // binding to Start() - see the canvases already loaded by the time Start() runs, regardless of
    // whether the gameplay scene was reached directly (Play button on SampleScene) or via the
    // MainMenu -> SampleScene Single-mode transition (which unloads every other loaded scene,
    // including any additively-loaded canvases from a previous load).
    public static class AdditiveCanvasSceneLoader
    {
        public const string RewardCanvasSceneName = "RewardCanvas";
        public const string ShopCanvasSceneName = "ShopCanvas";
        public const string HudCanvasSceneName = "HUDCanvas";
        private const string GameplaySceneName = "SampleScene";

        public static event Action<string> CanvasSceneReady;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Bootstrap()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != GameplaySceneName)
            {
                return;
            }

            EnsureLoaded(RewardCanvasSceneName);
            EnsureLoaded(ShopCanvasSceneName);
            EnsureLoaded(HudCanvasSceneName);
        }

        public static bool IsSceneReady(string sceneName)
        {
            var scene = SceneManager.GetSceneByName(sceneName);
            return scene.IsValid() && scene.isLoaded;
        }

        private static void EnsureLoaded(string sceneName)
        {
            if (IsSceneReady(sceneName))
            {
                CanvasSceneReady?.Invoke(sceneName);
                return;
            }

            // Synchronous on purpose: this runs between the gameplay scene's OnEnable and Start
            // phases (see class comment), and needs the canvas scene's objects to exist before
            // that Start phase begins.
            SceneManager.LoadScene(sceneName, LoadSceneMode.Additive);
            CanvasSceneReady?.Invoke(sceneName);
        }
    }
}
