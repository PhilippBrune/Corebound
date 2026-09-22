using UnityEngine;

namespace Corebound.Gameplay.Weapons
{
    // The hooks Weapons.md names: "klar definierte Hooks fuer Fire Rate, Damage, Crit, Multishot
    // oder Status Effect". Before this existed, modifiers were four loose float properties on
    // CardService that PlayerCombatController read directly, which meant Crit and Multishot simply
    // had nowhere to live and no card or tower could introduce a new kind of weapon effect without
    // editing PlayerCombatController.
    public enum WeaponModifierTarget
    {
        DamageMultiplier = 0,
        FireIntervalMultiplier = 1,
        ProjectileSpeedMultiplier = 2,
        CriticalChance = 3,
        CriticalDamageMultiplier = 4,
        AdditionalProjectiles = 5,
        StatusEffectChance = 6,

        // Appended 2026-09-22 for WeaponLoadout.md's Set Bonus rule changes. Serialized by index.
        AdditionalPierce = 7,           // Add: extra enemies each projectile passes through
        CriticalEveryNthShot = 8,       // Add: every Nth volley is a guaranteed crit (0 = off)
        FullRing = 9,                   // Add: > 0 fires the volley as a full 360-degree ring
        ExplodeOnExpire = 10,           // Add: > 0 makes projectiles that hit nothing explode
        SlowOnHit = 11,                 // Add: > 0 applies Freeze (a slow) on hit
        HeldDamageRamp = 12,            // Add: damage gained per second of held fire (resets on release)
        SpreadAngleMultiplier = 13      // Multiply: < 1 narrows the spread
    }

    // Multiplicative targets compose by multiplying, additive ones by summing. Keeping this on the
    // enum rather than on each asset stops two assets disagreeing about how the same target stacks.
    public static class WeaponModifierTargetExtensions
    {
        public static bool IsMultiplicative(this WeaponModifierTarget target)
        {
            return target == WeaponModifierTarget.DamageMultiplier
                || target == WeaponModifierTarget.FireIntervalMultiplier
                || target == WeaponModifierTarget.ProjectileSpeedMultiplier
                || target == WeaponModifierTarget.CriticalDamageMultiplier
                || target == WeaponModifierTarget.SpreadAngleMultiplier;
        }
    }

    [CreateAssetMenu(menuName = "Corebound/Gameplay/Weapons/Weapon Modifier Data", fileName = "WeaponModifierData")]
    public sealed class WeaponModifierData : ScriptableObject
    {
        [SerializeField] private string _modifierId = "weapon_modifier_default";
        [SerializeField] private string _displayName = "Weapon Modifier";
        [SerializeField] [TextArea(2, 3)] private string _description = string.Empty;
        [SerializeField] private WeaponModifierTarget _target = WeaponModifierTarget.DamageMultiplier;
        [SerializeField] private float _value = 1f;

        // Empty means "applies to whatever weapon is equipped". Naming a weapon id is how a
        // modifier becomes weapon-specific (e.g. a Beam-only pierce bonus) without any code
        // knowing that weapon exists.
        [SerializeField] private string _requiredWeaponId = string.Empty;

        public string ModifierId => _modifierId;

        public string DisplayName => _displayName;

        public string Description => _description;

        public WeaponModifierTarget Target => _target;

        public float Value => _value;

        public string RequiredWeaponId => _requiredWeaponId;

        public bool AppliesTo(string weaponId)
        {
            return string.IsNullOrWhiteSpace(_requiredWeaponId)
                || string.Equals(_requiredWeaponId, weaponId, System.StringComparison.Ordinal);
        }
    }
}
