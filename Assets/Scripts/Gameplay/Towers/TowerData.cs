using Corebound.Gameplay.Combat;
using UnityEngine;

namespace Corebound.Gameplay.Towers
{
    [CreateAssetMenu(menuName = "Corebound/Gameplay/Towers/Tower Data", fileName = "TowerData")]
    public sealed class TowerData : ScriptableObject
    {
        [SerializeField] private string _towerId = "tower_default";
        [SerializeField] private string _displayName = "Tower";
        [SerializeField] [TextArea(2, 3)] private string _description = string.Empty;
        [SerializeField] private Sprite _sprite;
        [SerializeField] private int _buildCost = 12;
        [SerializeField] private float _range = 6f;
        [SerializeField] private float _fireIntervalSeconds = 0.85f;
        [SerializeField] private string _requiredUnlockId = string.Empty;
        [SerializeField] private TowerLevelData[] _upgradeLevels;
        [SerializeField] private DamageProfileData _damageProfile;
        [SerializeField] private ProjectileData _projectile;
        [SerializeField] private Color _debugColor = new Color(1f, 0.95f, 0.55f, 1f);

        public string TowerId => _towerId;

        public string DisplayName => _displayName;

        public string Description => _description;

        public Sprite Sprite => _sprite;

        public int BuildCost => _buildCost;

        public float Range => _range;

        public float FireIntervalSeconds => _fireIntervalSeconds;

        public string RequiredUnlockId => _requiredUnlockId;

        public TowerLevelData[] UpgradeLevels => _upgradeLevels;

        public DamageProfileData DamageProfile => _damageProfile;

        public ProjectileData Projectile => _projectile;

        public Color DebugColor => _debugColor;
    }
}