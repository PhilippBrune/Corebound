using System.Collections.Generic;
using Corebound.Gameplay.Shared;
using UnityEngine;

namespace Corebound.Gameplay.Waves
{
    // Where a spawned thing is put, extracted from WaveManager on 2026-09-20.
    //
    // WaveManager carries around fifteen responsibilities and this is the one that is genuinely
    // separable: it is pure geometry plus the arena bounds, it reads no wave state, and it changes
    // nothing when it runs. It is also the code that produced the worst bug of the week - enemies
    // spawning outside the boundary walls, unreachable, hanging the wave forever - which is exactly
    // the kind of thing that hides inside a thousand-line class and shows up plainly in a small one.
    //
    // Deliberately a plain class rather than a MonoBehaviour or a static helper: it owns the
    // per-wave randomisation state (the fan rotation and spawn counter), so it has to be an
    // instance, and it must be constructible in isolation to be testable.
    public sealed class WaveSpawnPlacement
    {
        // Keeps a spawn off the wall itself: half an enemy plus a little, so nothing starts life
        // overlapping a boundary collider and getting pushed through it.
        private const float ArenaMargin = 1f;

        private const float BossScatterRadius = 2.6f;
        private const float BossMinPlayerDistance = 5f;

        private int _spawnCounter;
        private int _spawnPointOffset;
        private float _fanRotationDegrees;

        // Rolled once per wave rather than per enemy. Offsetting the round-robin by a per-wave
        // value keeps every authored entry point used an even number of times within a wave -
        // pure per-enemy randomness clumps, which reads as unfair rather than varied - while still
        // starting each wave somewhere new.
        public void BeginWave()
        {
            _spawnCounter = 0;
            _spawnPointOffset = Random.Range(0, 1000);
            _fanRotationDegrees = Random.Range(0f, 360f);
        }

        public int NextSpawnIndex()
        {
            return _spawnCounter++;
        }

        // The spawn-point (gate) indices the next enemyCount spawns will round-robin through, in
        // the order they will be used. Same wrap as ResolveEnemySpawnPosition, so the lit gates are
        // exactly the ones the wave then comes out of. A stage with no authored spawn points has a
        // single gate at index 0.
        public void CollectUpcomingGateIndices(StageData stageData, int enemyCount, List<int> result)
        {
            result.Clear();
            var spawnPoints = stageData != null ? stageData.SpawnPoints : null;
            var gateCount = spawnPoints != null && spawnPoints.Length > 0 ? spawnPoints.Length : 1;
            var count = Mathf.Min(Mathf.Max(1, enemyCount), gateCount);

            for (var index = 0; index < count; index++)
            {
                var wrappedIndex = gateCount == 1
                    ? 0
                    : ((((_spawnCounter + index + _spawnPointOffset) % gateCount) + gateCount) % gateCount);

                if (!result.Contains(wrappedIndex))
                {
                    result.Add(wrappedIndex);
                }
            }
        }

        public Vector3 ResolveEnemySpawnPosition(StageData stageData, StageArenaArtPresenter arena, int spawnIndex, float angleJitter)
        {
            var spawnPoints = stageData != null ? stageData.SpawnPoints : null;
            Vector2 origin;
            Vector2 fanRadius;
            float angleStepDegrees;

            if (spawnPoints != null && spawnPoints.Length > 0)
            {
                // A stage with multiple authored spawn points round-robins enemies across them by
                // spawn index, so a burst spreads across the stage's different entry points instead
                // of fanning out from one.
                var wrappedIndex = (((spawnIndex + _spawnPointOffset) % spawnPoints.Length) + spawnPoints.Length) % spawnPoints.Length;
                var spawnPoint = spawnPoints[wrappedIndex];
                origin = spawnPoint.Position;
                fanRadius = spawnPoint.FanRadius;
                angleStepDegrees = spawnPoint.FanAngleStepDegrees;
            }
            else
            {
                origin = stageData != null ? stageData.EnemySpawnOrigin : new Vector2(0f, -4.5f);
                fanRadius = stageData != null ? stageData.SpawnFanRadius : new Vector2(5.5f, 3.5f);
                angleStepDegrees = stageData != null ? stageData.SpawnFanAngleStepDegrees : 120f;
            }

            var jitterDegrees = Random.Range(-0.5f, 0.5f) * angleStepDegrees * angleJitter;
            var angle = (spawnIndex * angleStepDegrees + _fanRotationDegrees + jitterDegrees) * Mathf.Deg2Rad;

            // Pull some spawns slightly inside the fan radius so they do not all sit on one ring.
            var radiusScale = Random.Range(1f - 0.25f * angleJitter, 1f);
            var position = new Vector2(
                origin.x + Mathf.Cos(angle) * fanRadius.x * radiusScale,
                origin.y + Mathf.Sin(angle) * fanRadius.y * radiusScale);

            return ClampToArena(position, arena);
        }

        // Boss helper adds are summoned from a single point near the encounter itself, not from the
        // arena's entry points, regardless of how many spawn points the stage defines.
        public Vector3 ResolveBossHelperSpawnPosition(StageData stageData, StageArenaArtPresenter arena, int spawnIndex)
        {
            var fanRadius = stageData != null ? stageData.BossHelperSpawnFanRadius : new Vector2(2.2f, 1.6f);
            var angleStepDegrees = stageData != null ? stageData.SpawnFanAngleStepDegrees : 120f;
            var origin = stageData != null ? stageData.EnemySpawnOrigin : new Vector2(0f, -4.5f);
            var angle = spawnIndex * angleStepDegrees * Mathf.Deg2Rad;

            return ClampToArena(
                new Vector2(origin.x + Mathf.Cos(angle) * fanRadius.x, origin.y + Mathf.Sin(angle) * fanRadius.y),
                arena);
        }

        // Playtest 2026-09-20: "Boss and phase changes are spawning static. Make them random too."
        // Scattered on an arc around the authored point rather than anywhere in the arena, because
        // the authored position still carries the stage's intent (Stage 5's central dais, Stage 2's
        // off-centre reactor), and a boss that could appear on top of the player would be unfair
        // rather than varied.
        public Vector3 ResolveBossSpawnPosition(StageData stageData, StageArenaArtPresenter arena, Transform playerTarget)
        {
            var authored = stageData != null ? (Vector2)stageData.BossSpawnPosition : Vector2.zero;
            var angle = Random.Range(0f, Mathf.PI * 2f);
            var distance = Random.Range(0f, BossScatterRadius);
            var scattered = authored + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;

            // Never closer to the player than this, so the fight always opens with a readable
            // approach instead of an instant body-block.
            if (playerTarget != null)
            {
                var fromPlayer = scattered - (Vector2)playerTarget.position;

                if (fromPlayer.sqrMagnitude < BossMinPlayerDistance * BossMinPlayerDistance)
                {
                    var pushDirection = fromPlayer.sqrMagnitude > 0.0001f ? fromPlayer.normalized : Vector2.up;
                    scattered = (Vector2)playerTarget.position + pushDirection * BossMinPlayerDistance;
                }
            }

            return ClampToArena(scattered, arena);
        }

        // Stage 1's spawn origin is (0, -4.5) in a 16x10 arena and its fan radius is (5.5, 3.5), so
        // the fan reaches y = -8 while the arena floor stops at y = -5. That never mattered while
        // every enemy in a group resolved to the same angle, but once spawns were spread across the
        // fan and the whole fan was rotated per wave, some enemies landed outside the boundary
        // colliders. An enemy out there is walled off from the arena, so it can never be reached or
        // killed, RemainingAliveEnemies never returns to zero, and the wave hangs forever - the
        // "2 enemies spawned, I killed them and nothing happened" report of 2026-09-20. Clamping
        // here rather than shrinking each stage's fan radius fixes every stage at once, including
        // ones authored later.
        public static Vector3 ClampToArena(Vector2 position, StageArenaArtPresenter arena)
        {
            if (arena == null)
            {
                return new Vector3(position.x, position.y, 0f);
            }

            var halfExtents = arena.ArenaSize * 0.5f - new Vector2(ArenaMargin, ArenaMargin);
            halfExtents = new Vector2(Mathf.Max(0.5f, halfExtents.x), Mathf.Max(0.5f, halfExtents.y));

            return new Vector3(
                Mathf.Clamp(position.x, -halfExtents.x, halfExtents.x),
                Mathf.Clamp(position.y, -halfExtents.y, halfExtents.y),
                0f);
        }
    }
}
