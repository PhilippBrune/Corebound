using Corebound.Gameplay.Combat;
using Corebound.UI;
using UnityEngine;

namespace Corebound.Gameplay.Towers
{
    [CreateAssetMenu(menuName = "Corebound/Gameplay/Towers/Tower Data", fileName = "TowerData")]
    public sealed class TowerData : ScriptableObject
    {
        [SerializeField] private string _towerId = "tower_default";
        [SerializeField] private string _displayName = "Tower";
        [SerializeField] private string _displayNameDe = string.Empty;
        [SerializeField] [TextArea(2, 3)] private string _description = string.Empty;
        [SerializeField] [TextArea(2, 3)] private string _descriptionDe = string.Empty;
        [SerializeField] private Sprite _sprite;
        [SerializeField] private int _buildCost = 12;
        [SerializeField] private float _range = 6f;
        [SerializeField] private float _fireIntervalSeconds = 0.85f;
        [SerializeField] private string _requiredUnlockId = string.Empty;
        [SerializeField] private TowerLevelData[] _upgradeLevels;
        [SerializeField] private DamageProfileData _damageProfile;
        [SerializeField] private ProjectileData _projectile;
        // Playtest 2026-09-21: "I miss e.g. information what the towers are able of and what
        // are there pro/cons." Authored per tower rather than derived, because the useful answer
        // is tactical ("good against grouped enemies") and no combination of damage and range
        // numbers conveys that.
        [SerializeField] private string _strengths = string.Empty;
        [SerializeField] private string _strengthsDe = string.Empty;
        [SerializeField] private string _weaknesses = string.Empty;
        [SerializeField] private string _weaknessesDe = string.Empty;

        [SerializeField] private Color _debugColor = new Color(1f, 0.95f, 0.55f, 1f);

        public string TowerId => _towerId;

        public string DisplayName => LocalizationService.Resolve(_displayName, _displayNameDe);

        public string Description => LocalizationService.Resolve(_description, _descriptionDe);

        public Sprite Sprite => _sprite;

        public int BuildCost => _buildCost;

        public float Range => _range;

        public float FireIntervalSeconds => _fireIntervalSeconds;

        public string Strengths => LocalizationService.Resolve(_strengths, _strengthsDe);

        public string Weaknesses => LocalizationService.Resolve(_weaknesses, _weaknessesDe);

        // Sustained single-target damage per second, the one number that actually compares two
        // towers. Damage and fire interval separately do not: a player cannot divide them at a
        // glance while deciding what to build.
        public float DamagePerSecond => _damageProfile != null && _fireIntervalSeconds > Mathf.Epsilon
            ? _damageProfile.BaseDamage / _fireIntervalSeconds
            : 0f;

        public string RequiredUnlockId => _requiredUnlockId;

        public TowerLevelData[] UpgradeLevels => _upgradeLevels;

        public DamageProfileData DamageProfile => _damageProfile;

        public ProjectileData Projectile => _projectile;

        public Color DebugColor => _debugColor;
    }
}