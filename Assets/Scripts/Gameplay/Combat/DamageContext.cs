using UnityEngine;

namespace Corebound.Gameplay.Combat
{
    public readonly struct DamageContext
    {
        public DamageContext(
            DamageSourceType sourceType,
            DamageKind damageKind,
            float amount,
            Vector2 hitPoint,
            Vector2 hitDirection,
            StatusEffectType? statusEffect,
            bool isCritical = false,
            string sourceId = null,
            CombatHitEffects hitEffects = CombatHitEffects.None)
        {
            SourceType = sourceType;
            DamageKind = damageKind;
            Amount = amount;
            HitPoint = hitPoint;
            HitDirection = hitDirection;
            StatusEffect = statusEffect;
            IsCritical = isCritical;
            SourceId = sourceId;
            HitEffects = hitEffects;
        }

        public DamageSourceType SourceType { get; }

        public DamageKind DamageKind { get; }

        public float Amount { get; }

        public Vector2 HitPoint { get; }

        public Vector2 HitDirection { get; }

        public StatusEffectType? StatusEffect { get; }

        // Carried from the volley that rolled it to the hit, for crit feedback (playtest 2026-09-22).
        public bool IsCritical { get; }

        // Which weapon (WeaponData.WeaponId) dealt a player hit; null for everything else. The
        // weapon-kill quests (2026-09-29) need to know which weapon landed the killing blow, and
        // until then a hit only knew it came from "the player".
        public string SourceId { get; }

        // Card mechanics rolled when the shot was fired (Poison, Shock, Vulnerable); applied by the
        // receiver on the hit.
        public CombatHitEffects HitEffects { get; }

        // Every rebuild of a context used to repeat all seven constructor arguments by hand, which
        // is how a new field gets silently dropped at one of the six rebuild sites. These copy
        // everything and change one thing.
        public DamageContext WithAmount(float amount)
        {
            return new DamageContext(SourceType, DamageKind, amount, HitPoint, HitDirection, StatusEffect, IsCritical, SourceId, HitEffects);
        }

        public DamageContext WithHitPoint(Vector2 hitPoint)
        {
            return new DamageContext(SourceType, DamageKind, Amount, hitPoint, HitDirection, StatusEffect, IsCritical, SourceId, HitEffects);
        }
    }
}
