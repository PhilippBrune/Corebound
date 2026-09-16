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
            StatusEffectType? statusEffect)
        {
            SourceType = sourceType;
            DamageKind = damageKind;
            Amount = amount;
            HitPoint = hitPoint;
            HitDirection = hitDirection;
            StatusEffect = statusEffect;
        }

        public DamageSourceType SourceType { get; }

        public DamageKind DamageKind { get; }

        public float Amount { get; }

        public Vector2 HitPoint { get; }

        public Vector2 HitDirection { get; }

        public StatusEffectType? StatusEffect { get; }
    }
}