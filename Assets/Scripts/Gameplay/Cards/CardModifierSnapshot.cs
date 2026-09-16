namespace Corebound.Gameplay.Cards
{
    public sealed class CardModifierSnapshot
    {
        public float WeaponDamageMultiplier { get; private set; } = 1f;

        public float WeaponFireIntervalMultiplier { get; private set; } = 1f;

        public float ResourceGainMultiplier { get; private set; } = 1f;

        public float TowerDamageMultiplier { get; private set; } = 1f;

        public float IncomingDamageTakenMultiplier { get; private set; } = 1f;

        public float ProjectileSpeedMultiplier { get; private set; } = 1f;

        public float PlayerMovementSpeedMultiplier { get; private set; } = 1f;

        public float PurchaseCostMultiplier { get; private set; } = 1f;

        public void Reset()
        {
            WeaponDamageMultiplier = 1f;
            WeaponFireIntervalMultiplier = 1f;
            ResourceGainMultiplier = 1f;
            TowerDamageMultiplier = 1f;
            IncomingDamageTakenMultiplier = 1f;
            ProjectileSpeedMultiplier = 1f;
            PlayerMovementSpeedMultiplier = 1f;
            PurchaseCostMultiplier = 1f;
        }

        public void Apply(CardEffectData effect)
        {
            if (effect == null)
            {
                return;
            }

            switch (effect.Target)
            {
                case CardEffectTarget.WeaponDamageMultiplier:
                    WeaponDamageMultiplier = ApplyOperation(WeaponDamageMultiplier, effect.Operation, effect.Value);
                    return;
                case CardEffectTarget.WeaponFireIntervalMultiplier:
                    WeaponFireIntervalMultiplier = ApplyOperation(WeaponFireIntervalMultiplier, effect.Operation, effect.Value);
                    return;
                case CardEffectTarget.ResourceGainMultiplier:
                    ResourceGainMultiplier = ApplyOperation(ResourceGainMultiplier, effect.Operation, effect.Value);
                    return;
                case CardEffectTarget.TowerDamageMultiplier:
                    TowerDamageMultiplier = ApplyOperation(TowerDamageMultiplier, effect.Operation, effect.Value);
                    return;
                case CardEffectTarget.IncomingDamageTakenMultiplier:
                    IncomingDamageTakenMultiplier = ApplyOperation(IncomingDamageTakenMultiplier, effect.Operation, effect.Value);
                    return;
                case CardEffectTarget.ProjectileSpeedMultiplier:
                    ProjectileSpeedMultiplier = ApplyOperation(ProjectileSpeedMultiplier, effect.Operation, effect.Value);
                    return;
                case CardEffectTarget.PlayerMovementSpeedMultiplier:
                    PlayerMovementSpeedMultiplier = ApplyOperation(PlayerMovementSpeedMultiplier, effect.Operation, effect.Value);
                    return;
                case CardEffectTarget.PurchaseCostMultiplier:
                    PurchaseCostMultiplier = ApplyOperation(PurchaseCostMultiplier, effect.Operation, effect.Value);
                    return;
            }
        }

        private static float ApplyOperation(float currentValue, CardEffectOperation operation, float effectValue)
        {
            return operation == CardEffectOperation.Add ? currentValue + effectValue : currentValue * effectValue;
        }
    }
}