using Corebound.Gameplay.Shared;
using UnityEngine;

namespace Corebound.Gameplay.Waves
{
    [CreateAssetMenu(menuName = "Corebound/Gameplay/Waves/Stage Theme Data", fileName = "StageThemeData")]
    public sealed class StageThemeData : ScriptableObject
    {
        [SerializeField] private string _themeId = "theme_stage_01";
        [SerializeField] private string _displayName = "Stage Theme";

        // Per-stage arena geometry (Docs/Slices/15_DYNAMIC_ARENA_SLICE.md). Optional: when unset,
        // StageArenaArtPresenter falls back to this asset's own _arenaSize/_boundaryThickness and
        // to its built-in Stage 1 obstacle grid and prop layout, which is exactly the behaviour
        // that shipped before layouts existed.
        [SerializeField] private StageArenaLayoutData _layoutData;
        [SerializeField] private Texture2D _groundTexture;
        [SerializeField] private string _groundTexturePath = "Assets/Art/Environment/Arena/stage1_ground_tileset.png";
        [SerializeField] private Texture2D _boundaryTexture;
        [SerializeField] private string _boundaryTexturePath = "Assets/Art/Environment/Arena/stage1_boundary_set.png";
        [SerializeField] private Texture2D _propTexture;
        [SerializeField] private string _propTexturePath = "Assets/Art/Environment/Arena/stage1_prop_set.png";
        [SerializeField] private Texture2D _coreTexture;
        [SerializeField] private string _coreTexturePath = "Assets/Art/Environment/Arena/core_main_sprite.png";
        [SerializeField] private Texture2D _spawnGateTexture;
        [SerializeField] private string _spawnGateTexturePath = "Assets/Art/Environment/Arena/stage1_spawn_gate.png";
        [SerializeField] private Color _groundTint = Color.white;
        [SerializeField] private Color _boundaryTint = new Color(0.12f, 0.16f, 0.22f, 0.95f);
        [SerializeField] private Color _propTint = Color.white;
        [SerializeField] private Color _spawnGateTint = new Color(1f, 1f, 1f, 0.96f);
        [SerializeField] private Vector2 _arenaSize = new Vector2(16f, 10f);
        [SerializeField] private float _boundaryThickness = 0.8f;
        [SerializeField] private float _coreScale = 1.4f;
        [SerializeField] private Vector2 _spawnGateWorldSize = new Vector2(3.2f, 1.8f);
        [SerializeField] private Vector2 _spawnGateLocalPosition = new Vector2(0f, -5f);

        public string ThemeId => _themeId;

        public string DisplayName => _displayName;

        public StageArenaLayoutData LayoutData => _layoutData;

        public Texture2D GroundTexture => _groundTexture;

        public string GroundTexturePath => _groundTexturePath;

        public Texture2D BoundaryTexture => _boundaryTexture;

        public string BoundaryTexturePath => _boundaryTexturePath;

        public Texture2D PropTexture => _propTexture;

        public string PropTexturePath => _propTexturePath;

        public Texture2D CoreTexture => _coreTexture;

        public string CoreTexturePath => _coreTexturePath;

        public Texture2D SpawnGateTexture => _spawnGateTexture;

        public string SpawnGateTexturePath => _spawnGateTexturePath;

        public Color GroundTint => _groundTint;

        public Color BoundaryTint => _boundaryTint;

        public Color PropTint => _propTint;

        public Color SpawnGateTint => _spawnGateTint;

        public Vector2 ArenaSize => _arenaSize;

        public float BoundaryThickness => _boundaryThickness;

        public float CoreScale => _coreScale;

        public Vector2 SpawnGateWorldSize => _spawnGateWorldSize;

        public Vector2 SpawnGateLocalPosition => _spawnGateLocalPosition;
    }
}