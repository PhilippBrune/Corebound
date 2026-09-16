using System.Collections.Generic;
using Corebound.Gameplay.Combat;
using UnityEngine;

namespace Corebound.Gameplay.Enemies
{
    public sealed class EnemyPool
    {
        private readonly Transform _parent;
        private readonly List<EnemyRuntimeController> _instances = new List<EnemyRuntimeController>();

        public EnemyPool(Transform parent)
        {
            _parent = parent;
        }

        public EnemyRuntimeController Get()
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

            var gameObject = new GameObject("EnemyRuntime");
            gameObject.transform.SetParent(_parent, false);
            gameObject.AddComponent<CombatHitReceiver>();
            var created = gameObject.AddComponent<EnemyRuntimeController>();
            _instances.Add(created);
            return created;
        }
    }
}