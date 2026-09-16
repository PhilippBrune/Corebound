using UnityEngine;

namespace Corebound.Gameplay.Combat
{
    public interface ICombatHitReceiver
    {
        void ReceiveHit(GameObject source, DamageContext damageContext);
    }
}