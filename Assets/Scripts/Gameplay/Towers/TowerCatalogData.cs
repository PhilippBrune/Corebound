using UnityEngine;

namespace Corebound.Gameplay.Towers
{
    [CreateAssetMenu(menuName = "Corebound/Gameplay/Towers/Tower Catalog Data", fileName = "TowerCatalogData")]
    public sealed class TowerCatalogData : ScriptableObject
    {
        [SerializeField] private TowerData[] _towers;

        public TowerData[] Towers => _towers;

        public TowerData FindById(string towerId)
        {
            if (string.IsNullOrWhiteSpace(towerId) || _towers == null)
            {
                return null;
            }

            for (var index = 0; index < _towers.Length; index++)
            {
                var tower = _towers[index];

                if (tower != null && tower.TowerId == towerId)
                {
                    return tower;
                }
            }

            return null;
        }
    }
}