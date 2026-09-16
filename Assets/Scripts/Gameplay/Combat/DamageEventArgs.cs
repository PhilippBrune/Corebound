using UnityEngine;

namespace Corebound.Gameplay.Combat
{
    public readonly struct DamageEventArgs
    {
        public DamageEventArgs(GameObject source, GameObject target, DamageContext damageContext, float remainingHealth)
        {
            Source = source;
            Target = target;
            DamageContext = damageContext;
            RemainingHealth = remainingHealth;
        }

        public GameObject Source { get; }

        public GameObject Target { get; }

        public DamageContext DamageContext { get; }

        public float RemainingHealth { get; }
    }
}