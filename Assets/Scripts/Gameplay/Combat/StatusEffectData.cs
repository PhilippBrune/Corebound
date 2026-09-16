using UnityEngine;

namespace Corebound.Gameplay.Combat
{
    [CreateAssetMenu(menuName = "Corebound/Gameplay/Combat/Status Effect", fileName = "StatusEffect")]
    public sealed class StatusEffectData : ScriptableObject
    {
        [SerializeField] private StatusEffectType _effectType = StatusEffectType.Burn;
        [SerializeField] private float _durationSeconds = 2f;
        [SerializeField] private float _tickIntervalSeconds = 0.5f;
        [SerializeField] private float _tickDamage = 0.5f;
        [SerializeField] private float _moveSpeedMultiplier = 1f;

        public StatusEffectType EffectType => _effectType;

        public float DurationSeconds => _durationSeconds;

        public float TickIntervalSeconds => _tickIntervalSeconds;

        public float TickDamage => _tickDamage;

        public float MoveSpeedMultiplier => _moveSpeedMultiplier;

        public static StatusEffectData Find(StatusEffectData[] definitions, StatusEffectType effectType)
        {
            if (definitions == null)
            {
                return null;
            }

            for (var index = 0; index < definitions.Length; index++)
            {
                var definition = definitions[index];

                if (definition != null && definition.EffectType == effectType)
                {
                    return definition;
                }
            }

            return null;
        }
    }
}