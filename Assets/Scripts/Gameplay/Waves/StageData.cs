using System;
using Corebound.Gameplay.Bosses;
using Corebound.Gameplay.Shop;
using UnityEngine;

namespace Corebound.Gameplay.Waves
{
    // Dynamic Arena Slice (Docs/Slices/15_DYNAMIC_ARENA_SLICE.md) step 1: one origin/fan pair and
    // one fixed tower-slot layout used to be the only option for every stage, which is exactly the
    // "static spawn, static tower position" playtest complaint. Both are now per-stage arrays -
    // empty by default, which falls back to the pre-existing single-origin/EnemySpawnOrigin
    // behavior (see WaveManager.ResolveActiveSpawnPoint/TowerService.CreateSlotRuntimes), so a
    // stage that has not authored either array plays identically to before this change.
    [Serializable]
    public sealed class StageSpawnPointDefinition
    {
        [SerializeField] private Vector2 _position = new Vector2(0f, -4.5f);
        [SerializeField] private Vector2 _fanRadius = new Vector2(5.5f, 3.5f);
        [SerializeField] private float _fanAngleStepDegrees = 120f;

        public Vector2 Position => _position;

        public Vector2 FanRadius => _fanRadius;

        public float FanAngleStepDegrees => _fanAngleStepDegrees;
    }

    [CreateAssetMenu(menuName = "Corebound/Gameplay/Waves/Stage Data", fileName = "StageData")]
    public sealed class StageData : ScriptableObject
    {
        [SerializeField] private string _stageId = "stage_01";
        [SerializeField] private int _stageIndex = 1;
        [SerializeField] private string _displayName = "Foundry";

        // Owner decision 2026-09-30 (Open Topics 5 (a)): every stage has a name, Stage 1 included.
        [SerializeField] private string _displayNameDe = "Gießerei";
        [SerializeField] private StageThemeData _themeData;
        [SerializeField] private StageRewardData _stageRewardData;
        [SerializeField] private StageProgressionData _progressionData;
        [SerializeField] private ShopData _shopData;
        [SerializeField] private float _intermissionDurationSeconds = 20f;
        [SerializeField] private Vector2 _enemySpawnOrigin = new Vector2(0f, -4.5f);
        [SerializeField] private Vector2 _playerStartPosition = new Vector2(0f, -1f);
        [SerializeField] private Vector2 _postRewardPlayerPosition = new Vector2(0f, -2.4f);
        [SerializeField] private Vector2 _spawnFanRadius = new Vector2(5.5f, 3.5f);
        [SerializeField] private Vector2 _bossHelperSpawnFanRadius = new Vector2(2.2f, 1.6f);
        [SerializeField] private float _spawnFanAngleStepDegrees = 120f;
        [SerializeField] private Vector3 _bossSpawnPosition = new Vector3(0f, 3.2f, 0f);
        [SerializeField] private WaveData[] _waves;
        [SerializeField] private BossData _bossData;
        [SerializeField] private StageSpawnPointDefinition[] _spawnPoints = Array.Empty<StageSpawnPointDefinition>();
        [SerializeField] private Vector2[] _towerSlotPositions = Array.Empty<Vector2>();

        public string StageId => _stageId;

        public int StageIndex => Mathf.Max(1, _stageIndex);

        public string DisplayName => Corebound.UI.LocalizationService.Resolve(_displayName, _displayNameDe);

        public StageThemeData ThemeData => _themeData;

        public StageRewardData StageRewardData => _stageRewardData;

        public StageProgressionData ProgressionData => _progressionData;

        public ShopData ShopData => _shopData;

        public float IntermissionDurationSeconds => _intermissionDurationSeconds;

        public Vector2 EnemySpawnOrigin => _enemySpawnOrigin;

        public Vector2 PlayerStartPosition => _playerStartPosition;

        public Vector2 PostRewardPlayerPosition => _postRewardPlayerPosition;

        public Vector2 SpawnFanRadius => _spawnFanRadius;

        public Vector2 BossHelperSpawnFanRadius => _bossHelperSpawnFanRadius;

        public float SpawnFanAngleStepDegrees => _spawnFanAngleStepDegrees;

        public Vector3 BossSpawnPosition => _bossSpawnPosition;

        public WaveData[] Waves => _waves;

        public BossData BossData => _bossData;

        // Empty by default (see class-level comment) - callers fall back to
        // EnemySpawnOrigin/SpawnFanRadius/SpawnFanAngleStepDegrees when this is empty.
        public StageSpawnPointDefinition[] SpawnPoints => _spawnPoints;

        // Empty by default (see class-level comment) - callers fall back to whatever tower-slot
        // layout they already had (TowerService's own serialized _slotPositions) when this is empty.
        public Vector2[] TowerSlotPositions => _towerSlotPositions;

        public StageData NextStageData => _progressionData != null ? _progressionData.GetNextStage(this) : null;

        public bool EndsRunOnCompletion => _stageRewardData == null || _stageRewardData.EndRunOnCompletion;

        public bool IsFinalStage => EndsRunOnCompletion || NextStageData == null;
    }
}