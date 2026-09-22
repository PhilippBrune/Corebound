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

        // WeaponLoadout.md: "The projectile budget is a data field. It will need tuning and must
        // never become a magic number." 60 is reachable by six Spread copies at Perfected and not
        // by any mixed loadout, which is the balancing rule it exists to express.
        [SerializeField] private int _projectileBudget = 60;
        [SerializeField] private int _arsenalProjectileBudgetBonus = 8;

        // Seconds after releasing the trigger before a partial magazine starts topping up.
        [SerializeField] private float _idleReloadDelaySeconds = 0.9f;

        public float MaxHealth => _maxHealth;

        public CombatBalanceData CombatBalance => _combatBalance;

        public WeaponData DefaultWeapon => _defaultWeapon;

        public WeaponCatalogData WeaponCatalog => _weaponCatalog;

        public MetaProgressionDefinitionData MetaProgressionDefinitionData => _metaProgressionDefinitionData;

        public int ProjectileBudget => Mathf.Max(1, _projectileBudget);

        public int ArsenalProjectileBudgetBonus => Mathf.Max(0, _arsenalProjectileBudgetBonus);

        public float IdleReloadDelaySeconds => Mathf.Max(0f, _idleReloadDelaySeconds);
    }
}