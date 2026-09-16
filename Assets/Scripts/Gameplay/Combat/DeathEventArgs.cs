using UnityEngine;

namespace Corebound.Gameplay.Combat
{
    public readonly struct DeathEventArgs
    {
        public DeathEventArgs(GameObject victim, DamageContext damageContext)
        {
            Victim = victim;
            DamageContext = damageContext;
        }

        public GameObject Victim { get; }

        public DamageContext DamageContext { get; }
    }
}