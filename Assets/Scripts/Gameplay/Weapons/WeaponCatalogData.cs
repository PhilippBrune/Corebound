using UnityEngine;

namespace Corebound.Gameplay.Weapons
{
    [CreateAssetMenu(menuName = "Corebound/Gameplay/Weapons/Weapon Catalog Data", fileName = "WeaponCatalogData")]
    public sealed class WeaponCatalogData : ScriptableObject
    {
        [SerializeField] private WeaponData[] _weapons;

        public WeaponData[] Weapons => _weapons;

        public WeaponData FindById(string weaponId)
        {
            if (string.IsNullOrWhiteSpace(weaponId) || _weapons == null)
            {
                return null;
            }

            for (var index = 0; index < _weapons.Length; index++)
            {
                var weapon = _weapons[index];

                if (weapon != null && weapon.WeaponId == weaponId)
                {
                    return weapon;
                }
            }

            return null;
        }
    }
}