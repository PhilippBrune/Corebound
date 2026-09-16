using System.Collections.Generic;
using UnityEngine;

namespace Corebound.Gameplay.Resources
{
    public sealed class ResourcePickupPool
    {
        private readonly Transform _parent;
        private readonly List<ResourcePickupRuntime> _instances = new List<ResourcePickupRuntime>();

        public ResourcePickupPool(Transform parent)
        {
            _parent = parent;
        }

        public IReadOnlyList<ResourcePickupRuntime> Instances => _instances;

        public ResourcePickupRuntime Get()
        {
            for (var index = 0; index < _instances.Count; index++)
            {
                var instance = _instances[index];

                if (instance != null && !instance.gameObject.activeSelf)
                {
                    return instance;
                }
            }

            var gameObject = new GameObject("ResourcePickup");
            gameObject.transform.SetParent(_parent, false);
            var created = gameObject.AddComponent<ResourcePickupRuntime>();
            _instances.Add(created);
            return created;
        }

        public void ForceCollectAll()
        {
            for (var index = 0; index < _instances.Count; index++)
            {
                var instance = _instances[index];

                if (instance != null && instance.gameObject.activeSelf)
                {
                    instance.ForceCollect();
                }
            }
        }
    }
}
