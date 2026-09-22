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
            bool isCritical = false)
        {
            SourceType = sourceType;
            DamageKind = damageKind;
            Amount = amount;
            HitPoint = hitPoint;
            HitDirection = hitDirection;
            StatusEffect = statusEffect;
            IsCritical = isCritical;
        }

        public DamageSourceType SourceType { get; }

        public DamageKind DamageKind { get; }

        public float Amount { get; }

        public Vector2 HitPoint { get; }

        public Vector2 HitDirection { get; }

        public StatusEffectType? StatusEffect { get; }

        // Carried from the volley that rolled it to the hit, for crit feedback (playtest 2026-09-22).
        // Every rebuild of a context must pass it on.
        public bool IsCritical { get; }
    }
}