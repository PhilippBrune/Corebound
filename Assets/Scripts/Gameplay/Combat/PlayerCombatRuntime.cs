using System.Collections.Generic;
using UnityEngine;

namespace Corebound.Gameplay.Combat
{
    public sealed class PlayerCombatRuntime
    {
        private readonly HealthRuntime _healthRuntime;
        private readonly Dictionary<StatusEffectType, StatusEffectRuntime> _activeStatuses = new Dictionary<StatusEffectType, StatusEffectRuntime>();

        private float _fireCooldown;
        private float _invulnerabilityTimer;

        public PlayerCombatRuntime(float maxHealth)
        {
            _healthRuntime = new HealthRuntime(maxHealth);
        }

        public float CurrentHealth => _healthRuntime.CurrentHealth;

        public float MaxHealth => _healthRuntime.MaxHealth;

        public bool IsAlive => _healthRuntime.IsAlive;

        public float MovementSpeedMultiplier
        {
            get
            {
                if (_activeStatuses.Count == 0)
                {
                    return 1f;
                }

                var multiplier = 1f;

                foreach (var pair in _activeStatuses)
                {
                    multiplier *= pair.Value.MoveSpeedMultiplier;
                }

                return multiplier;
            }
        }

        public void Tick(float deltaTime)
        {
            _fireCooldown = Mathf.Max(0f, _fireCooldown - deltaTime);
            _invulnerabilityTimer = Mathf.Max(0f, _invulnerabilityTimer - deltaTime);
        }

        public void ApplyStatus(StatusEffectType effectType, StatusEffectData statusData)
        {
            if (statusData == null || !IsAlive)
            {
                return;
            }

            _activeStatuses[effectType] = new StatusEffectRuntime(statusData);
        }

        public float AdvanceStatuses(float deltaTime)
        {
            if (_activeStatuses.Count == 0 || !IsAlive)
            {
                return 0f;
            }

            List<StatusEffectType> expiredEffects = null;
            var totalTickDamage = 0f;

            foreach (var pair in _activeStatuses)
            {
                totalTickDamage += pair.Value.Advance(deltaTime);

                if (!pair.Value.IsExpired)
                {
                    continue;
                }

                if (expiredEffects == null)
                {
                    expiredEffects = new List<StatusEffectType>();
                }

                expiredEffects.Add(pair.Key);
            }

            if (expiredEffects != null)
            {
                for (var index = 0; index < expiredEffects.Count; index++)
                {
                    _activeStatuses.Remove(expiredEffects[index]);
                }
            }

            return totalTickDamage;
        }

        public float ApplyStatusTickDamage(float amount)
        {
            return _healthRuntime.ApplyDamage(amount);
        }

        public bool CanFire()
        {
            return IsAlive && _fireCooldown <= 0f;
        }

        public void ConsumeFireCooldown(float fireIntervalSeconds)
        {
            _fireCooldown = Mathf.Max(0.01f, fireIntervalSeconds);
        }

        public void RestoreHealthNormalized(float normalizedHealth)
        {
            _healthRuntime.SetNormalized(normalizedHealth);
            _invulnerabilityTimer = 0f;
        }

        public bool TryReceiveHit(DamageContext damageContext, float invulnerabilitySeconds, out float remainingHealth)
        {
            if (_invulnerabilityTimer > 0f || !_healthRuntime.IsAlive)
            {
                remainingHealth = _healthRuntime.CurrentHealth;
                return false;
            }

            remainingHealth = _healthRuntime.ApplyDamage(damageContext.Amount);
            _invulnerabilityTimer = Mathf.Max(0f, invulnerabilitySeconds);
            return true;
        }
    }
}