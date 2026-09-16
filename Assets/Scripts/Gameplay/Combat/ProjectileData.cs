using Corebound.Gameplay.Shared;
using UnityEngine;

namespace Corebound.Gameplay.Combat
{
    [CreateAssetMenu(menuName = "Corebound/Gameplay/Combat/Projectile", fileName = "Projectile")]
    public sealed class ProjectileData : ScriptableObject
    {
        [SerializeField] private Sprite _sprite;
        [SerializeField] private string _projectSpritePath = string.Empty;

        [Header("Atlas Cell (used when Sprite and Project Sprite Path are both unset)")]
        [SerializeField] private string _atlasSheetPath = string.Empty;
        [SerializeField] private int _atlasColumn;
        [SerializeField] private int _atlasRow;
        [SerializeField] private int _atlasColumns = 1;
        [SerializeField] private int _atlasRows = 1;

        [SerializeField] private float _speed = 10f;
        [SerializeField] private float _lifetimeSeconds = 1.4f;
        [SerializeField] private float _radius = 0.14f;
        [SerializeField] private int _pierceCount;
        [SerializeField] private Color _debugColor = new Color(1f, 0.55f, 0.2f, 1f);

        public Sprite Sprite => _sprite;

        public string ProjectSpritePath => _projectSpritePath;

        public float Speed => _speed;

        public float LifetimeSeconds => _lifetimeSeconds;

        public float Radius => _radius;

        // Number of additional targets this projectile survives hitting before it is released
        // back to the pool. 0 = releases on first hit (Rapid/Spread's existing behavior).
        public int PierceCount => Mathf.Max(0, _pierceCount);

        public Color DebugColor => _debugColor;

        public Sprite ResolveSprite()
        {
            if (_sprite != null)
            {
                return _sprite;
            }

            if (!string.IsNullOrWhiteSpace(_projectSpritePath))
            {
                return DebugSpriteFactory.LoadProjectSprite(_projectSpritePath);
            }

            return string.IsNullOrWhiteSpace(_atlasSheetPath)
                ? null
                : DebugSpriteFactory.LoadProjectAtlasCellSprite(_atlasSheetPath, _atlasColumn, _atlasRow, _atlasColumns, _atlasRows);
        }
    }
}