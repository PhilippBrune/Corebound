using Corebound.Gameplay.Cards;
using Corebound.Gameplay.Shop;
using Corebound.Gameplay.Waves;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Corebound.EditorTools
{
    // Playtest feedback (2026-09-16/17): "I still cannot see the image in the Unity Scene window"
    // - RewardCanvas.unity/ShopCanvas.unity/HUDCanvas.unity were extracted from SampleScene.unity
    // with several sprites unset and stale positions, and the runtime presenters only repair that
    // data live in memory (see CardRewardPresenter/ShopPresenter/WaveStatePresenter's
    // TryBindAuthoredLayout/TryBindAuthoredHud) - never writing the fix back to the scene files, so
    // opening a canvas scene on its own (without SampleScene, or without ever entering Play Mode)
    // still shows the original broken data. This menu command runs that same repair once, with all
    // four scenes loaded together, and explicitly saves the three canvas scenes - baking correct
    // sprite and position data directly into the .unity files so they're correct even standalone.
    public static class AuthoredCanvasRepairTool
    {
        private const string SampleScenePath = "Assets/Scenes/SampleScene.unity";
        private const string RewardCanvasPath = "Assets/Scenes/RewardCanvas.unity";
        private const string ShopCanvasPath = "Assets/Scenes/ShopCanvas.unity";
        private const string HudCanvasPath = "Assets/Scenes/HUDCanvas.unity";

        [MenuItem("Corebound/Repair Authored Canvases")]
        public static void RepairAndSave()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("[AuthoredCanvasRepairTool] Stop Play Mode first.");
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Debug.LogWarning("[AuthoredCanvasRepairTool] Cancelled - you have unsaved changes. Save or discard them first.");
                return;
            }

            EnsureSceneOpen(SampleScenePath, OpenSceneMode.Single);
            var rewardScene = EnsureSceneOpen(RewardCanvasPath, OpenSceneMode.Additive);
            var shopScene = EnsureSceneOpen(ShopCanvasPath, OpenSceneMode.Additive);
            var hudScene = EnsureSceneOpen(HudCanvasPath, OpenSceneMode.Additive);

            var rewardPresenter = Object.FindFirstObjectByType<CardRewardPresenter>();
            var shopPresenter = Object.FindFirstObjectByType<ShopPresenter>();
            var wavePresenter = Object.FindFirstObjectByType<WaveStatePresenter>();

            var rewardOk = rewardPresenter != null && rewardPresenter.RepairAuthoredLayoutForEditorTool();
            var shopOk = shopPresenter != null && shopPresenter.RepairAuthoredLayoutForEditorTool();
            var hudOk = wavePresenter != null && wavePresenter.RepairAuthoredLayoutForEditorTool();

            EditorSceneManager.MarkSceneDirty(rewardScene);
            EditorSceneManager.MarkSceneDirty(shopScene);
            EditorSceneManager.MarkSceneDirty(hudScene);

            EditorSceneManager.SaveScene(rewardScene);
            EditorSceneManager.SaveScene(shopScene);
            EditorSceneManager.SaveScene(hudScene);

            Debug.Log("[AuthoredCanvasRepairTool] Done. RewardCanvas: " + (rewardOk ? "repaired+saved" : "FAILED (presenter/bind not found)")
                + " | ShopCanvas: " + (shopOk ? "repaired+saved" : "FAILED (presenter/bind not found)")
                + " | HUDCanvas: " + (hudOk ? "repaired+saved" : "FAILED (presenter/bind not found)"));
        }

        private static Scene EnsureSceneOpen(string path, OpenSceneMode mode)
        {
            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);

                if (scene.path == path)
                {
                    return scene;
                }
            }

            return EditorSceneManager.OpenScene(path, mode);
        }
    }
}
