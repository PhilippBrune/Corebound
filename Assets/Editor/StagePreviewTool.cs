using Corebound.Gameplay.Shared;
using Corebound.Gameplay.Waves;
using UnityEditor;
using UnityEngine;

namespace Corebound.EditorTools
{
    // Lets a specific stage's arena art (ground/boundary/props) be previewed directly in the Scene
    // view without playing through prior stages to reach it - useful whenever a stage's
    // StageThemeData/prop placements change and need a visual check (see the Stage 2 prop-placement
    // fix, 2026-09-17: StageArenaArtPresenter.RebuildForNewStage() already supported this, it just
    // had no way to be pointed at an arbitrary stage outside of actually playing there).
    public static class StagePreviewTool
    {
        [MenuItem("Corebound/Preview Stage 2 Arena")]
        public static void PreviewStage2()
        {
            PreviewStage("Assets/ScriptableObjects/Gameplay/Waves/Stage_02.asset");
        }

        [MenuItem("Corebound/Preview Stage 1 Arena")]
        public static void PreviewStage1()
        {
            PreviewStage("Assets/ScriptableObjects/Gameplay/Waves/Stage_01.asset");
        }

        private static void PreviewStage(string stageAssetPath)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("[StagePreviewTool] Stop Play Mode first.");
                return;
            }

            var presenter = Object.FindFirstObjectByType<StageArenaArtPresenter>();

            if (presenter == null)
            {
                Debug.LogWarning("[StagePreviewTool] No StageArenaArtPresenter found in the open scenes.");
                return;
            }

            var stageData = AssetDatabase.LoadAssetAtPath<StageData>(stageAssetPath);

            if (stageData == null)
            {
                Debug.LogWarning("[StagePreviewTool] Could not load " + stageAssetPath);
                return;
            }

            var serialized = new SerializedObject(presenter);
            serialized.FindProperty("_stageData").objectReferenceValue = stageData;
            serialized.ApplyModifiedProperties();

            presenter.RebuildForNewStage();
            Debug.Log("[StagePreviewTool] Rebuilt arena preview for " + stageData.name + " (not saved - _stageData reassignment is an in-memory preview only, discard scene changes when done).");
        }
    }
}
