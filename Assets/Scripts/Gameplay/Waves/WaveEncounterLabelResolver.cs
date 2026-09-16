using Corebound.Gameplay.Bosses;
using Corebound.SaveSystem;

namespace Corebound.Gameplay.Waves
{
    // Extracted from WaveManager (God-object split, 2026-09-09): pure label/elite-flag resolution
    // that only reads StageData/wave index/state, with no dependency on WaveManager's
    // spawn/state-machine mutation methods, so it can be called with just the read-only context
    // each call site already has on hand. Mirrors the WaveStatisticsRecorder extraction pattern.
    internal static class WaveEncounterLabelResolver
    {
        public static string ResolveCurrentEncounterLabel(StageData stageData, int currentWaveIndex, WaveState state, BossRuntimeController bossRuntimeController)
        {
            if (state == WaveState.BossCombat && bossRuntimeController != null)
            {
                return bossRuntimeController.BossName;
            }

            return ResolveEncounterLabel(ResolveWaveData(stageData, currentWaveIndex));
        }

        public static string ResolveNextEncounterLabel(StageData stageData, int currentWaveIndex, int totalWaveCount)
        {
            var nextIndex = currentWaveIndex + 1;

            if (nextIndex >= totalWaveCount && stageData != null && stageData.BossData != null)
            {
                return stageData.BossData.DisplayName;
            }

            return ResolveEncounterLabel(ResolveWaveData(stageData, nextIndex));
        }

        public static bool ResolveCurrentEncounterIsElite(StageData stageData, int currentWaveIndex, WaveState state)
        {
            if (state == WaveState.BossCombat)
            {
                return false;
            }

            return ResolveEncounterIsElite(ResolveWaveData(stageData, currentWaveIndex));
        }

        public static bool ResolveNextEncounterIsElite(StageData stageData, int currentWaveIndex, int totalWaveCount)
        {
            var nextIndex = currentWaveIndex + 1;

            if (nextIndex >= totalWaveCount && stageData != null && stageData.BossData != null)
            {
                return false;
            }

            return ResolveEncounterIsElite(ResolveWaveData(stageData, nextIndex));
        }

        // state/bossRuntimeController are passed through unchanged to ResolveCurrentEncounterLabel
        // to preserve the original inline call's behavior of reading WaveManager's fields at
        // whatever point InitializeFromSave has reached when this runs (before _state is set for
        // the branch about to be taken).
        public static string ResolveStageStartDetail(StageData stageData, int currentWaveIndex, int totalWaveCount, WaveState state, BossRuntimeController bossRuntimeController, RunSaveData runSaveData)
        {
            if (runSaveData != null && runSaveData.IsBossEncounterActive && stageData != null && stageData.BossData != null)
            {
                return stageData.BossData.DisplayName;
            }

            if (runSaveData != null && runSaveData.IsIntermissionOpen)
            {
                return ResolveNextEncounterLabel(stageData, currentWaveIndex, totalWaveCount);
            }

            if (runSaveData != null && runSaveData.HasPendingReward)
            {
                return !string.IsNullOrWhiteSpace(runSaveData.PendingRewardLabel)
                    ? runSaveData.PendingRewardLabel
                    : ResolveCurrentEncounterLabel(stageData, currentWaveIndex, state, bossRuntimeController);
            }

            return ResolveCurrentEncounterLabel(stageData, currentWaveIndex, state, bossRuntimeController);
        }

        public static string ResolveStageCompletionLabel(StageData stageData)
        {
            var stageRewardData = stageData != null ? stageData.StageRewardData : null;

            return stageRewardData != null && !string.IsNullOrWhiteSpace(stageRewardData.CompletionLabel)
                ? stageRewardData.CompletionLabel
                : "Stage Complete";
        }

        public static string ResolveStageTransitionLabel(StageData stageData)
        {
            var stageRewardData = stageData != null ? stageData.StageRewardData : null;

            if (stageRewardData != null && !string.IsNullOrWhiteSpace(stageRewardData.TransitionLabel))
            {
                return stageRewardData.TransitionLabel;
            }

            var nextStageData = stageData != null ? stageData.NextStageData : null;
            return nextStageData != null ? nextStageData.DisplayName : "Run Complete";
        }

        private static bool ResolveEncounterIsElite(WaveData waveData)
        {
            return waveData != null && waveData.ModifierData != null && waveData.ModifierData.IsEliteEncounter;
        }

        private static WaveData ResolveWaveData(StageData stageData, int waveIndex)
        {
            if (stageData == null || stageData.Waves == null || waveIndex < 0 || waveIndex >= stageData.Waves.Length)
            {
                return null;
            }

            return stageData.Waves[waveIndex];
        }

        private static string ResolveEncounterLabel(WaveData waveData)
        {
            if (waveData == null)
            {
                return "Wave";
            }

            var modifierData = waveData.ModifierData;

            if (modifierData != null && !string.IsNullOrWhiteSpace(modifierData.EncounterLabelOverride))
            {
                return modifierData.EncounterLabelOverride;
            }

            return !string.IsNullOrWhiteSpace(waveData.DisplayLabel) ? waveData.DisplayLabel : "Wave";
        }
    }
}
