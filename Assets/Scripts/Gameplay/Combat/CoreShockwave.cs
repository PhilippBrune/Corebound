using UnityEngine;

namespace Corebound.Gameplay.Combat
{
    // A ring of force from the Core: damage and a shove for every enemy inside the radius. The
    // Custodian's tether release had it first; the Core Guard's counter-pulse
    // (CORE_PROTECTION_FEEL_2026-10-07, C7) is "the Custodian's shockwave without the class", so
    // both go through this one function rather than two copies that drift apart.
    public static class CoreShockwave
    {
        private static readonly Collider2D[] Buffer = new Collider2D[48];

        // Returns how many enemies it reached.
        public static int Release(GameObject source, Vector2 centre, float radius, float damage, float impulse, string sourceId)
        {
            CombatVfxSpawner.SpawnNovaRing(centre);
            var count = Physics2D.OverlapCircleNonAlloc(centre, radius, Buffer);
            var reached = 0;

            for (var index = 0; index < count; index++)
            {
                var collider = Buffer[index];

                if (collider == null)
                {
                    continue;
                }

                var owner = collider.attachedRigidbody != null ? collider.attachedRigidbody.gameObject : collider.gameObject;

                if (owner.GetComponent<Corebound.Gameplay.Enemies.EnemyRuntimeController>() == null)
                {
                    continue;
                }

                reached++;
                var direction = ((Vector2)owner.transform.position - centre).normalized;
                var receiver = owner.GetComponent<CombatHitReceiver>();

                if (receiver != null && receiver.IsAlive && damage > 0f)
                {
                    receiver.ReceiveHit(source, new DamageContext(DamageSourceType.Player, DamageKind.Direct,
                        damage, owner.transform.position, direction, null, false, sourceId));
                }

                if (collider.attachedRigidbody != null && impulse > 0f)
                {
                    collider.attachedRigidbody.AddForce(direction * impulse, ForceMode2D.Impulse);
                }
            }

            return reached;
        }
    }
}
