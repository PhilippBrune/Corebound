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

        // NEW_CARD_MECHANICS (2026-09-29). Additive ones start at 0.
        public float PoisonSpreadRadius { get; private set; }

        public float HealOnWaveClear { get; private set; }

        public float HealPerKill { get; private set; }

        public float ShieldStacks { get; private set; }

        public float TowerAuraFireRate { get; private set; }

        public float CoreIntermissionRegen { get; private set; }

        public float CoreDamageTakenMultiplier { get; private set; } = 1f;

        public float SentryDrones { get; private set; }

        // Class cards (2026-09-30).
        public float ClassSignaturePower { get; private set; }

        public float ClassOverchargeCharges { get; private set; }

        public float ClassHoardThresholdReduction { get; private set; }

        public float ClassTetherRadius { get; private set; }

        public float ClassContagionRadius { get; private set; }

        public float ClassSetBonusFocus { get; private set; }

        public void Reset()
        {
            PoisonSpreadRadius = 0f;
            HealOnWaveClear = 0f;
            HealPerKill = 0f;
            ShieldStacks = 0f;
            TowerAuraFireRate = 0f;
            CoreIntermissionRegen = 0f;
            CoreDamageTakenMultiplier = 1f;
            SentryDrones = 0f;
            ClassSignaturePower = 0f;
            ClassOverchargeCharges = 0f;
            ClassHoardThresholdReduction = 0f;
            ClassTetherRadius = 0f;
            ClassContagionRadius = 0f;
            ClassSetBonusFocus = 0f;
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
                case CardEffectTarget.PoisonSpreadRadius:
                    // Two copies spread no further than one; the radius is a reach, not a sum.
                    PoisonSpreadRadius = UnityEngine.Mathf.Max(PoisonSpreadRadius, effect.Value);
                    return;
                case CardEffectTarget.HealOnWaveClear:
                    HealOnWaveClear = ApplyOperation(HealOnWaveClear, effect.Operation, effect.Value);
                    return;
                case CardEffectTarget.HealPerKill:
                    HealPerKill = ApplyOperation(HealPerKill, effect.Operation, effect.Value);
                    return;
                case CardEffectTarget.ShieldStacks:
                    ShieldStacks = ApplyOperation(ShieldStacks, effect.Operation, effect.Value);
                    return;
                case CardEffectTarget.TowerAuraFireRate:
                    TowerAuraFireRate = ApplyOperation(TowerAuraFireRate, effect.Operation, effect.Value);
                    return;
                case CardEffectTarget.CoreIntermissionRegen:
                    CoreIntermissionRegen = ApplyOperation(CoreIntermissionRegen, effect.Operation, effect.Value);
                    return;
                case CardEffectTarget.CoreDamageTakenMultiplier:
                    CoreDamageTakenMultiplier = ApplyOperation(CoreDamageTakenMultiplier, effect.Operation, effect.Value);
                    return;
                case CardEffectTarget.SentryDrones:
                    SentryDrones = ApplyOperation(SentryDrones, effect.Operation, effect.Value);
                    return;
                case CardEffectTarget.ClassSignaturePower:
                    ClassSignaturePower = ApplyOperation(ClassSignaturePower, effect.Operation, effect.Value);
                    return;
                case CardEffectTarget.ClassOverchargeCharges:
                    ClassOverchargeCharges = ApplyOperation(ClassOverchargeCharges, effect.Operation, effect.Value);
                    return;
                case CardEffectTarget.ClassHoardThresholdReduction:
                    ClassHoardThresholdReduction = ApplyOperation(ClassHoardThresholdReduction, effect.Operation, effect.Value);
                    return;
                case CardEffectTarget.ClassTetherRadius:
                    ClassTetherRadius = ApplyOperation(ClassTetherRadius, effect.Operation, effect.Value);
                    return;
                case CardEffectTarget.ClassContagionRadius:
                    ClassContagionRadius = ApplyOperation(ClassContagionRadius, effect.Operation, effect.Value);
                    return;
                case CardEffectTarget.ClassSetBonusFocus:
                    ClassSetBonusFocus = ApplyOperation(ClassSetBonusFocus, effect.Operation, effect.Value);
                    return;
            }
        }

        private static float ApplyOperation(float currentValue, CardEffectOperation operation, float effectValue)
        {
            return operation == CardEffectOperation.Add ? currentValue + effectValue : currentValue * effectValue;
        }
    }
}