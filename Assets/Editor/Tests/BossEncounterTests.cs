using Corebound.Gameplay.Bosses;
using Corebound.Gameplay.Waves;
using NUnit.Framework;
using UnityEngine;

namespace Corebound.Editor.Tests
{
    public sealed class BossEncounterTests
    {
        [Test]
        public void BossEventArgs_HoldsCorrectPayload()
        {
            var args = new BossEventArgs(1, 7, "boss_stage1", "Corebreaker Warden", 1, "Phase 2 - Enrage", 0.45f);

            Assert.AreEqual(1, args.StageIndex);
            Assert.AreEqual(7, args.WaveIndex);
            Assert.AreEqual("boss_stage1", args.BossId);
            Assert.AreEqual("Corebreaker Warden", args.BossName);
            Assert.AreEqual(1, args.PhaseIndex);
            Assert.AreEqual("Phase 2 - Enrage", args.PhaseLabel);
            Assert.AreEqual(0.45f, args.HealthNormalized, 0.001f);
        }

        [Test]
        public void BossEvents_FiresEventsCorrectly()
        {
            var spawnedFired = false;
            var phaseChangedFired = false;
            var defeatedFired = false;

            BossEventArgs receivedSpawnArgs = null;
            BossEventArgs receivedPhaseArgs = null;
            BossEventArgs receivedDefeatedArgs = null;

            BossEvents.OnBossSpawned += args =>
            {
                spawnedFired = true;
                receivedSpawnArgs = args;
            };

            BossEvents.OnBossPhaseChanged += args =>
            {
                phaseChangedFired = true;
                receivedPhaseArgs = args;
            };

            BossEvents.OnBossDefeated += args =>
            {
                defeatedFired = true;
                receivedDefeatedArgs = args;
            };

            var testArgs = new BossEventArgs(1, 7, "boss_stage1", "Corebreaker Warden", 0, "Phase 1", 1.0f);

            BossEvents.RaiseBossSpawned(testArgs);
            Assert.IsTrue(spawnedFired);
            Assert.AreEqual("Corebreaker Warden", receivedSpawnArgs.BossName);

            var phaseArgs = new BossEventArgs(1, 7, "boss_stage1", "Corebreaker Warden", 1, "Phase 2", 0.5f);
            BossEvents.RaiseBossPhaseChanged(phaseArgs);
            Assert.IsTrue(phaseChangedFired);
            Assert.AreEqual(1, receivedPhaseArgs.PhaseIndex);

            var defeatArgs = new BossEventArgs(1, 7, "boss_stage1", "Corebreaker Warden", 1, "Phase 2", 0f);
            BossEvents.RaiseBossDefeated(defeatArgs);
            Assert.IsTrue(defeatedFired);
            Assert.AreEqual(0f, receivedDefeatedArgs.HealthNormalized);
        }

        [Test]
        public void StageData_ContainsSixWavesAndBossData()
        {
            var stageData = ScriptableObject.CreateInstance<StageData>();
            var bossData = ScriptableObject.CreateInstance<BossData>();

            Assert.IsNotNull(stageData);
            Assert.IsNotNull(bossData);
            Assert.AreEqual(1, stageData.StageIndex);
            Assert.IsTrue(stageData.IsFinalStage);
        }

        [Test]
        public void StageEvents_FireLifecyclePayload()
        {
            var startedFired = false;
            var completedFired = false;
            var transitionedFired = false;
            var payload = new StageEventArgs(1, "stage_01", "Stage 1", "Run Complete", true);
            var receivedStartedArgs = default(StageEventArgs);
            var receivedCompletedArgs = default(StageEventArgs);
            var receivedTransitionArgs = default(StageEventArgs);

            void OnStarted(StageEventArgs eventArgs)
            {
                startedFired = true;
                receivedStartedArgs = eventArgs;
            }

            void OnCompleted(StageEventArgs eventArgs)
            {
                completedFired = true;
                receivedCompletedArgs = eventArgs;
            }

            void OnTransitioned(StageEventArgs eventArgs)
            {
                transitionedFired = true;
                receivedTransitionArgs = eventArgs;
            }

            StageEvents.StageStarted += OnStarted;
            StageEvents.StageCompleted += OnCompleted;
            StageEvents.StageTransitioned += OnTransitioned;

            try
            {
                StageEvents.RaiseStageStarted(payload);
                StageEvents.RaiseStageCompleted(payload);
                StageEvents.RaiseStageTransitioned(payload);
            }
            finally
            {
                StageEvents.StageStarted -= OnStarted;
                StageEvents.StageCompleted -= OnCompleted;
                StageEvents.StageTransitioned -= OnTransitioned;
            }

            Assert.IsTrue(startedFired);
            Assert.IsTrue(completedFired);
            Assert.IsTrue(transitionedFired);
            Assert.AreEqual("stage_01", receivedStartedArgs.StageId);
            Assert.AreEqual("Stage 1", receivedCompletedArgs.StageLabel);
            Assert.AreEqual("Run Complete", receivedTransitionArgs.DetailLabel);
            Assert.IsTrue(receivedTransitionArgs.IsFinalStage);
        }

        [Test]
        public void BossAudioCueData_ExposesDefaultFrequencies()
        {
            var audioCueData = ScriptableObject.CreateInstance<BossAudioCueData>();

            Assert.IsNotNull(audioCueData);
            Assert.Greater(audioCueData.WarningFrequency, 0f);
            Assert.Greater(audioCueData.SpawnFrequency, 0f);
            Assert.Greater(audioCueData.PhaseChangeFrequency, 0f);
            Assert.Greater(audioCueData.DefeatFrequency, 0f);
        }
    }
}
