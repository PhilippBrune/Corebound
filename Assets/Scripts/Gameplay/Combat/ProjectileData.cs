using Corebound.Gameplay.Shared;
using UnityEngine;

namespace Corebound.Gameplay.Combat
{
    [CreateAssetMenu(menuName = "Corebound/Gameplay/Combat/Projectile", fileName = "Projectile")]
    public sealed class ProjectileData : ScriptableObject
    {
        [SerializeField] private Sprite _sprite;
        [SerializeField] private string _projectSpritePath = string.Empty;

        // A cell of a shared projectile sheet (used when Sprite and Project Sprite Path are both
        // unset). The sheet asset owns the grid and, for the hand-composed Asset 101 sheet, each
        // cell's pixel rect; a projectile only names its cell (Open Topics 4 (a), 2026-09-30).
        [Header("Sheet Cell (used when Sprite and Project Sprite Path are both unset)")]
        [SerializeField] private SpriteSheetData _sheet;
        [SerializeField] private string _sheetCell = string.Empty;

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

            return _sheet != null ? _sheet.LoadCell(_sheetCell) : null;
        }
    }
}