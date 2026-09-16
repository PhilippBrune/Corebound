using UnityEngine;

namespace Corebound.Gameplay.Towers
{
    [CreateAssetMenu(menuName = "Corebound/Gameplay/Towers/Tower Level Data", fileName = "TowerLevelData")]
    public sealed class TowerLevelData : ScriptableObject
    {
        [SerializeField] private string _label = "Upgrade";
        [SerializeField] private int _upgradeCost = 8;
        [SerializeField] private float _rangeMultiplier = 1.15f;
        [SerializeField] private float _fireIntervalMultiplier = 0.88f;
        [SerializeField] private float _damageMultiplier = 1.2f;
        [SerializeField] private Color _tint = Color.white;

        public string Label => _label;

        public int UpgradeCost => Mathf.Max(1, _upgradeCost);

        public float RangeMultiplier => Mathf.Max(0.1f, _rangeMultiplier);

        public float FireIntervalMultiplier => Mathf.Max(0.1f, _fireIntervalMultiplier);

        public float DamageMultiplier => Mathf.Max(0.1f, _damageMultiplier);

        public Color Tint => _tint;
    }
}