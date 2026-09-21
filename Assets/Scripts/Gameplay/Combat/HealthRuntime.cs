using UnityEngine;

namespace Corebound.Gameplay.Combat
{
    public sealed class HealthRuntime
    {
        public HealthRuntime(float maxHealth)
        {
            MaxHealth = Mathf.Max(1f, maxHealth);
            CurrentHealth = MaxHealth;
        }

        public float MaxHealth { get; }

        public float CurrentHealth { get; private set; }

        public bool IsAlive => CurrentHealth > 0f;

        public void Reset()
        {
            CurrentHealth = MaxHealth;
        }

        public void SetNormalized(float normalizedHealth)
        {
            CurrentHealth = Mathf.Clamp01(normalizedHealth) * MaxHealth;
        }

        // Returns the health actually restored, which is not the amount asked for once the bar is
        // near full. CoreRepairService needs that distinction to refuse a purchase that would do
        // nothing.
        public float Heal(float amount)
        {
            if (amount <= 0f || !IsAlive)
            {
                return 0f;
            }

            var previous = CurrentHealth;
            CurrentHealth = Mathf.Min(MaxHealth, CurrentHealth + amount);
            return CurrentHealth - previous;
        }

        public float ApplyDamage(float amount)
        {
            if (!IsAlive)
            {
                return CurrentHealth;
            }

            CurrentHealth = Mathf.Max(0f, CurrentHealth - Mathf.Max(0f, amount));
            return CurrentHealth;
        }
    }
}