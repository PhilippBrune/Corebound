using System.Collections.Generic;
using Corebound.Gameplay.Combat;
using Corebound.Gameplay.Resources;
using Corebound.Gameplay.Shared;
using Corebound.Gameplay.Waves;
using Corebound.UI;
using UnityEngine;

namespace Corebound.Gameplay.Arena
{
    // Decides whether a wave gets an arena objective, where it goes, how long it lives, and what
    // claiming it pays out. Owner decisions of 2026-09-25 against
    // Docs/Specs/DIY/ARENA_OBJECTIVES_2026-09-25.md: D1 "1 and 3" (Scrap lump *and* a timed buff),
    // D2 1 (uniformly random placement), D3 1 (walk over it), D4 2 (computed lifetime),
    // D6 3 (per-wave chance rising with stage), D7 1 (ignored means gone).
    //
    // A plain class rather than a MonoBehaviour: it owns only per-wave decision state, WaveManager
    // already drives it every frame, and it has to be constructible in isolation to reason about.
    public sealed class ArenaObjectiveService
    {
        private readonly List<Vector2> _candidatePositions = new List<Vector2>();

        private ArenaObjectiveRuntime _live;
        private bool _waveHasObjective;
        private bool _objectiveSpawned;
        private int _waveScrapBudget;
        private int _waveEnemyTotal;

        // Rolled at wave start, spent when the wave's spawn progress crosses the data's threshold.
        // Deciding up front rather than per frame keeps the roll honest: a per-frame chance would
        // effectively guarantee one in a long wave and never fire in a short one.
        public void BeginWave(ArenaObjectiveData data, StageData stageData, WaveData waveData, bool isBossWave)
        {
            DismissLive();
            _waveHasObjective = false;
            _objectiveSpawned = false;
            _waveScrapBudget = 0;
            _waveEnemyTotal = 0;

            if (data == null || stageData == null || waveData == null || isBossWave)
            {
                return;
            }

            _waveScrapBudget = ResolveWaveScrapBudget(waveData);
            _waveEnemyTotal = ResolveWaveEnemyTotal(waveData);

            if (_waveScrapBudget <= 0 || _waveEnemyTotal <= 0)
            {
                return;
            }

            _waveHasObjective = Random.value < data.ResolveSpawnChance(stageData.StageIndex);
        }

        // Called every combat frame by WaveManager. spawnedEnemyCount is how many of the wave's
        // enemies have been put into the arena so far - the trigger is wave *progress*, not a
        // clock, so a build fast enough to delete a wave in six seconds still sees one.
        public void TickCombat(
            ArenaObjectiveData data,
            StageArenaArtPresenter arena,
            Transform playerTarget,
            Transform parent,
            int spawnedEnemyCount)
        {
            if (!_waveHasObjective || _objectiveSpawned || data == null || arena == null)
            {
                return;
            }

            var progress = _waveEnemyTotal > 0 ? spawnedEnemyCount / (float)_waveEnemyTotal : 1f;

            if (progress < data.SpawnAtWaveProgress)
            {
                return;
            }

            _objectiveSpawned = true;
            Spawn(data, arena, playerTarget, parent);
        }

        // The wave ended before the objective did. Neither claimed nor expired: nothing is paid
        // out and no toast fires, because the player did not make a decision about it.
        public void EndWave()
        {
            DismissLive();
            _waveHasObjective = false;
            _objectiveSpawned = false;
        }

        private void Spawn(ArenaObjectiveData data, StageArenaArtPresenter arena, Transform playerTarget, Transform parent)
        {
            if (!TryResolvePosition(arena, out var position))
            {
                return;
            }

            var gameObject = new GameObject("ArenaObjective_" + data.ObjectiveId);
            gameObject.transform.SetParent(parent, false);
            _live = gameObject.AddComponent<ArenaObjectiveRuntime>();
            _live.Initialize(
                data,
                position,
                ResolveLifetimeSeconds(data, arena, position, playerTarget),
                playerTarget,
                OnClaimed,
                OnExpired);

            NotificationPresenter.Show(LocalizationService.Resolve(
                data.DisplayName + " sighted",
                data.DisplayName + " gesichtet"));
        }

        // Owner chose D2 option 1: uniformly random inside the arena, with no minimum distance from
        // the player and no Core exclusion.
        //
        // The walkable-cell list is still preferred over a raw uniform roll, and that is not the
        // rejected constraint sneaking back in - it is reachability. StageArenaArtPresenter puts
        // real BoxCollider2Ds on props and maze blockers, so a raw uniform point lands inside a
        // wall often enough to matter, and a cache you can see but cannot walk to is the same
        // failure shape as the 2026-09-20 "enemies spawned outside the boundary" bug. The cells are
        // themselves spread uniformly across the arena, so the distribution is what was asked for.
        private bool TryResolvePosition(StageArenaArtPresenter arena, out Vector2 position)
        {
            _candidatePositions.Clear();
            var walkable = arena.WalkableInteriorPositions;

            for (var index = 0; index < walkable.Count; index++)
            {
                _candidatePositions.Add(walkable[index]);
            }

            if (_candidatePositions.Count > 0)
            {
                position = _candidatePositions[Random.Range(0, _candidatePositions.Count)];
                return true;
            }

            // A stage with no maze cells authored: fall back to a uniform roll inside the arena
            // bounds, clamped the same way spawns are.
            var halfExtents = arena.ArenaSize * 0.5f - Vector2.one;
            halfExtents = new Vector2(Mathf.Max(0.5f, halfExtents.x), Mathf.Max(0.5f, halfExtents.y));
            position = new Vector2(
                Random.Range(-halfExtents.x, halfExtents.x),
                Random.Range(-halfExtents.y, halfExtents.y));
            return true;
        }

        // D4 option 2: crossing time at base move speed plus slack, never a fixed number, so a
        // window that works in Stage 1's 16x10 arena still works in Stage 5's larger one.
        //
        // The trip is measured from where the player actually is, floored at half the arena
        // diagonal so a cache that lands near them is not over before it registers. Crossing time
        // is then scaled by a detour factor, because nobody walks the straight line: the player is
        // mid-fight, has to disengage, and steers around props and the maze. Without that factor
        // the raw straight-line time in Stage 1 came out at 1.5s and the whole lifetime collapsed
        // onto the minimum, which is the fixed duration D4 rejected wearing a different hat.
        private float ResolveLifetimeSeconds(ArenaObjectiveData data, StageArenaArtPresenter arena, Vector2 position, Transform playerTarget)
        {
            var moveSpeed = ResolveBaseMoveSpeed();
            var distance = playerTarget != null
                ? Vector2.Distance(position, playerTarget.position)
                : arena.ArenaSize.magnitude * 0.5f;
            distance = Mathf.Max(distance, arena.ArenaSize.magnitude * 0.5f);
            var crossingSeconds = distance / Mathf.Max(0.5f, moveSpeed) * data.TravelDetourFactor;
            return Mathf.Max(data.MinLifetimeSeconds, crossingSeconds + data.LifetimeSlackSeconds);
        }

        private static float ResolveBaseMoveSpeed()
        {
            var player = Object.FindFirstObjectByType<Player.PlayerController>();
            return player != null ? player.CurrentMoveSpeed : 4f;
        }

        private void OnClaimed(ArenaObjectiveRuntime runtime)
        {
            var data = runtime != null ? runtime.Data : null;
            _live = null;

            if (data == null)
            {
                return;
            }

            var message = string.Empty;
            var scrap = Mathf.RoundToInt(_waveScrapBudget * data.ScrapFractionOfWaveBudget);

            if (scrap > 0)
            {
                var wallet = Object.FindFirstObjectByType<ResourceWalletService>();

                if (wallet != null)
                {
                    // AddReward, not AddFlat: this is income and should scale with the player's
                    // Scrap-gain cards and skills exactly as an enemy drop does.
                    message += "+" + wallet.AddReward(scrap) + " " + wallet.DisplayLabel;
                }
            }

            if (data.BuffDurationSeconds > 0f && data.FireIntervalMultiplier < 1f)
            {
                var combat = Object.FindFirstObjectByType<PlayerCombatController>();

                if (combat != null)
                {
                    combat.ApplyTimedFireIntervalBuff(data.FireIntervalMultiplier, data.BuffDurationSeconds);
                    var percent = Mathf.RoundToInt((1f / data.FireIntervalMultiplier - 1f) * 100f);
                    message += (message.Length > 0 ? " · " : string.Empty)
                        + LocalizationService.Resolve("+" + percent + "% fire rate", "+" + percent + "% Feuerrate");
                }
            }

            if (message.Length > 0)
            {
                NotificationPresenter.Show(message);
            }
        }

        // D7 option 1: a missed opportunity is a missed opportunity. Nothing is credited, nothing
        // is punished; the toast exists only so the player knows the choice they made had a cost.
        private void OnExpired(ArenaObjectiveRuntime runtime)
        {
            var data = runtime != null ? runtime.Data : null;
            _live = null;

            if (data != null)
            {
                NotificationPresenter.Show(LocalizationService.Resolve(
                    data.DisplayName + " lost",
                    data.DisplayName + " verloren"));
            }
        }

        private void DismissLive()
        {
            if (_live != null)
            {
                _live.Dismiss();
                Object.Destroy(_live.gameObject);
                _live = null;
            }
        }

        private static int ResolveWaveScrapBudget(WaveData waveData)
        {
            var total = 0;
            var spawnGroups = waveData.SpawnGroups;

            if (spawnGroups == null)
            {
                return 0;
            }

            for (var index = 0; index < spawnGroups.Length; index++)
            {
                var spawnGroup = spawnGroups[index];
                var enemyData = spawnGroup != null ? spawnGroup.EnemyData : null;
                var rewardData = enemyData != null ? enemyData.RewardData : null;

                if (rewardData != null)
                {
                    total += spawnGroup.Count * rewardData.ResourceRewardAmount;
                }
            }

            return total;
        }

        private static int ResolveWaveEnemyTotal(WaveData waveData)
        {
            var total = 0;
            var spawnGroups = waveData.SpawnGroups;

            if (spawnGroups == null)
            {
                return 0;
            }

            for (var index = 0; index < spawnGroups.Length; index++)
            {
                if (spawnGroups[index] != null)
                {
                    total += spawnGroups[index].Count;
                }
            }

            return total;
        }
    }
}
