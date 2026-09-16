using System.Collections.Generic;
using UnityEngine;

namespace Corebound.Gameplay.Combat
{
    public sealed class ProjectilePool
    {
        private readonly Transform _parent;
        private readonly List<CombatProjectile> _instances = new List<CombatProjectile>();

        public ProjectilePool(Transform parent)
        {
            _parent = parent;
        }

        public CombatProjectile Get(CombatProjectile prefab)
        {
            for (var index = 0; index < _instances.Count; index++)
            {
                var instance = _instances[index];

                if (instance != null && !instance.gameObject.activeSelf)
                {
                    instance.gameObject.SetActive(true);
                    return instance;
                }
            }

            var created = Object.Instantiate(prefab, _parent);
            _instances.Add(created);
            return created;
        }
    }
}