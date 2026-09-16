using UnityEngine;

namespace Corebound.Gameplay.Combat
{
    public readonly struct StatusAppliedEventArgs
    {
        public StatusAppliedEventArgs(GameObject target, StatusEffectType effectType, float durationSeconds)
        {
            Target = target;
            EffectType = effectType;
            DurationSeconds = durationSeconds;
        }

        public GameObject Target { get; }

        public StatusEffectType EffectType { get; }

        public float DurationSeconds { get; }
    }
}