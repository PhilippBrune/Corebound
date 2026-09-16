namespace Corebound.Gameplay.Combat
{
    public sealed class StatusEffectRuntime
    {
        private readonly StatusEffectData _data;

        private float _remainingDuration;
        private float _tickTimer;

        public StatusEffectRuntime(StatusEffectData data)
        {
            _data = data;
            _remainingDuration = data != null ? data.DurationSeconds : 0f;
            _tickTimer = data != null ? data.TickIntervalSeconds : 0f;
        }

        public StatusEffectType EffectType => _data != null ? _data.EffectType : StatusEffectType.Burn;

        public float MoveSpeedMultiplier => _data != null ? _data.MoveSpeedMultiplier : 1f;

        public bool IsExpired => _remainingDuration <= 0f;

        public float Advance(float deltaTime)
        {
            if (_data == null || IsExpired)
            {
                return 0f;
            }

            _remainingDuration -= deltaTime;
            _tickTimer -= deltaTime;

            if (_tickTimer > 0f)
            {
                return 0f;
            }

            _tickTimer += _data.TickIntervalSeconds;
            return _data.TickDamage;
        }
    }
}