using Corebound.Gameplay.Weapons;
using Corebound.MetaProgression;
using UnityEngine;

namespace Corebound.Gameplay.Combat
{
    [CreateAssetMenu(menuName = "Corebound/Gameplay/Combat/Player Combat", fileName = "PlayerCombat")]
    public sealed class PlayerCombatData : ScriptableObject
    {
        [SerializeField] private float _maxHealth = 8f;
        [SerializeField] private CombatBalanceData _combatBalance;
        [SerializeField] private WeaponData _defaultWeapon;
        [SerializeField] private WeaponCatalogData _weaponCatalog;
        [SerializeField] private MetaProgressionDefinitionData _metaProgressionDefinitionData;

        public float MaxHealth => _maxHealth;

        public CombatBalanceData CombatBalance => _combatBalance;

        public WeaponData DefaultWeapon => _defaultWeapon;

        public WeaponCatalogData WeaponCatalog => _weaponCatalog;

        public MetaProgressionDefinitionData MetaProgressionDefinitionData => _metaProgressionDefinitionData;
    }
}