using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Corebound.Gameplay.Shared
{
    // Loads the standalone UI canvas scenes (RewardCanvas, ShopCanvas, HUDCanvas) additively
    // whenever the gameplay scene loads. These canvases used to be procedurally built in C# (with
    // hardcoded pixel offsets) as children of their presenter's own GameObject inside SampleScene -
    // which is what let a domain-reload rebuild silently leave a fresh CanvasGroup at its default
    // (fully visible) state and block gameplay (2026-09-15 playtest). Moving them into their own
    // authored scenes means their layout lives entirely in data the user can open and edit
    // directly in the Editor.
    //
    // ORDERING - corrected 2026-09-19, this was wrong before and caused real bugs:
    // SceneManager.LoadScene() is NOT synchronous, not even in Additive mode. Unity defers the
    // actual load to the end of the current frame, so the newly requested scene's GameObjects do
    // not exist when the call returns. The previous version of this file assumed the opposite and
    // raised CanvasSceneReady immediately after calling LoadScene - and nothing ever subscribed to
    // that event anyway, so the presenters were left binding in their own Start(), which runs in
    // the SAME frame the load was requested, i.e. strictly before the canvas objects exist.
    //
    // On the "open SampleScene directly in the Editor" path that never showed up, because
    // AdditiveCanvasSceneAutoLoader had already opened the canvas scenes before Play Mode started,
    // so the very first bind attempt succeeded. On the real MainMenu -> New Run path (a Single-mode
    // transition that unloads everything first) the bind always failed and each presenter fell back
    // to building its own local placeholder UI - which is what showed up in playtests as the HUD
    // appearing "shortly in white" before the real HUDCanvas arrived at end of frame and covered
    // it, and as the Shop/Reward panels never binding to their authored content.
    //
    // CanvasSceneReady is now raised from SceneManager.sceneLoaded for the canvas scene itself,
    // which is the first moment its objects are guaranteed to exist. Presenters subscribe to it and
    // (re)bind then. IsScenePending lets a presenter distinguish "this canvas is still on its way,
    // wait for it" from "this canvas is genuinely absent, build a local fallback".
    public static class AdditiveCanvasSceneLoader
    {
        public const string RewardCanvasSceneName = "RewardCanvas";
        public const string ShopCanvasSceneName = "ShopCanvas";
        public const string HudCanvasSceneName = "HUDCanvas";

        // Boss HUD, system notification and relic reveal (Open Topics 4 (b), 2026-09-30).
        public const string OverlayCanvasSceneName = "OverlayCanvas";
        private const string GameplaySceneName = "SampleScene";

        private static readonly string[] CanvasSceneNames =
        {
            RewardCanvasSceneName,
            ShopCanvasSceneName,
            HudCanvasSceneName,
            OverlayCanvasSceneName
        };

        // Scenes whose additive load has been requested but has not completed yet. Static state
        // survives scene loads by design; it is cleared per-scene as each load completes, and
        // re-requested from scratch on the next gameplay-scene load.
        private static readonly HashSet<string> PendingScenes = new HashSet<string>();

        public static event Action<string> CanvasSceneReady;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Bootstrap()
        {
            PendingScenes.Clear();
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (IsCanvasScene(scene.name))
            {
                PendingScenes.Remove(scene.name);
                CanvasSceneReady?.Invoke(scene.name);
                return;
            }

            if (scene.name != GameplaySceneName)
            {
                return;
            }

            for (var index = 0; index < CanvasSceneNames.Length; index++)
            {
                EnsureLoaded(CanvasSceneNames[index]);
            }
        }

        public static bool IsCanvasScene(string sceneName)
        {
            for (var index = 0; index < CanvasSceneNames.Length; index++)
            {
                if (CanvasSceneNames[index] == sceneName)
                {
                    return true;
                }
            }

            return false;
        }

        // A root object of a loaded canvas scene, by name. Scene roots rather than GameObject.Find,
        // which cannot see inactive objects and would also match a same-named object elsewhere.
        public static GameObject FindRoot(string sceneName, string rootName)
        {
            var scene = SceneManager.GetSceneByName(sceneName);

            if (!scene.IsValid() || !scene.isLoaded)
            {
                return null;
            }

            foreach (var rootObject in scene.GetRootGameObjects())
            {
                if (rootObject.name == rootName)
                {
                    return rootObject;
                }
            }

            return null;
        }

        public static bool IsSceneReady(string sceneName)
        {
            var scene = SceneManager.GetSceneByName(sceneName);
            return scene.IsValid() && scene.isLoaded;
        }

        // True while an additive load has been requested but Unity has not finished it yet. A
        // presenter that sees this must wait for CanvasSceneReady instead of building a local
        // fallback, otherwise it builds a placeholder that the real canvas then renders on top of.
        public static bool IsScenePending(string sceneName)
        {
            return PendingScenes.Contains(sceneName) && !IsSceneReady(sceneName);
        }

        private static void EnsureLoaded(string sceneName)
        {
            if (IsSceneReady(sceneName))
            {
                // Already open (the Editor's AdditiveCanvasSceneAutoLoader path). Its objects
                // exist right now, so announcing readiness immediately is correct here.
                PendingScenes.Remove(sceneName);
                CanvasSceneReady?.Invoke(sceneName);
                return;
            }

            PendingScenes.Add(sceneName);

            // Completes at the end of this frame; readiness is announced from OnSceneLoaded.
            SceneManager.LoadScene(sceneName, LoadSceneMode.Additive);
        }
    }
}
