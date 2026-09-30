using System;

namespace Corebound.Gameplay.Combat
{
    // On-hit card mechanics rolled when a player shot is fired (NEW_CARD_MECHANICS, 2026-09-29) and
    // applied by CombatHitReceiver when it lands. Flags, because one shot can carry several.
    [Flags]
    public enum CombatHitEffects
    {
        None = 0,
        Poison = 1,
        Shock = 2,
        Vulnerable = 4
    }

    // The card values combat code needs that are not per weapon: how far Virulent Strain spreads
    // Poison, how much harder a marked enemy is hit, how much less the Core takes. CardService
    // writes these whenever its modifier snapshot is rebuilt, and resets them with it, so a new run
    // or a resumed one always starts from what its own cards say.
    public static class RunCombatEffects
    {
        // Target Painter: "marked enemies take +15% damage from every source, towers included".
        public const float VulnerableDamageBonus = 0.15f;
        public const float VulnerableSeconds = 3f;

        // Arc Coil: "arcs to 2 enemies within 2.5 units for 50% damage".
        public const int ShockTargets = 2;
        public const float ShockRadius = 2.5f;
        public const float ShockDamageFraction = 0.5f;

        public static float PoisonSpreadRadius { get; private set; }

        public static float CoreDamageTakenMultiplier { get; private set; } = 1f;

        // Core Aegis: fraction of the Core's max health restored per second during an Intermission.
        public static float CoreIntermissionRegen { get; private set; }

        // Overclock Beacon: how much faster towers near the player fire (0.2 = 20%).
        public static float TowerAuraFireRate { get; private set; }

        // Where the aura is centred - the living player - or null when there is none.
        public static UnityEngine.Transform AuraCenter { get; set; }

        public static void Set(float poisonSpreadRadius, float coreDamageTakenMultiplier, float coreIntermissionRegen, float towerAuraFireRate)
        {
            PoisonSpreadRadius = Math.Max(0f, poisonSpreadRadius);
            CoreDamageTakenMultiplier = Math.Max(0.1f, coreDamageTakenMultiplier);
            CoreIntermissionRegen = Math.Max(0f, coreIntermissionRegen);
            TowerAuraFireRate = Math.Max(0f, towerAuraFireRate);
        }

        // The divisor a tower at this position applies to its fire interval.
        public static float ResolveTowerAuraRateMultiplier(UnityEngine.Vector2 towerPosition)
        {
            if (TowerAuraFireRate <= 0f || AuraCenter == null)
            {
                return 1f;
            }

            var offset = (UnityEngine.Vector2)AuraCenter.position - towerPosition;
            return offset.sqrMagnitude <= PlayerSupportRules.TowerAuraRadius * PlayerSupportRules.TowerAuraRadius
                ? 1f + TowerAuraFireRate
                : 1f;
        }
    }

    // The fixed numbers of the four support cards and the drone, in one place so the card text
    // (CardEffectDescriber) and the code that does it read the same value.
    public static class PlayerSupportRules
    {
        // Leech Rounds: "up to 5 per wave" - five healing kills, not five health.
        public const int HealPerKillCapPerWave = 5;

        // Reactive Plating: one blocked hit, then this long to recharge. Each further copy
        // shortens it by a fifth.
        public const float ShieldRechargeSeconds = 12f;
        public const float ShieldRechargeStackFactor = 0.8f;

        // Overclock Beacon: towers this close to the player fire faster.
        public const float TowerAuraRadius = 3f;

        // Sentry Drone.
        public const float DroneDamageFraction = 0.35f;
        public const float DroneFireIntervalSeconds = 0.5f;
        public const float DroneRange = 7f;
        public const float DroneOrbitRadius = 1.1f;
        public const float DroneOrbitDegreesPerSecond = 90f;

        public static float ResolveShieldRechargeSeconds(int stacks)
        {
            return ShieldRechargeSeconds * (float)Math.Pow(ShieldRechargeStackFactor, Math.Max(0, stacks - 1));
        }
    }
}
