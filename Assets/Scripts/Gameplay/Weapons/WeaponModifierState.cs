using System.Collections.Generic;
using UnityEngine;

namespace Corebound.Gameplay.Weapons
{
    // The single place a weapon's effective numbers are resolved. Before this, three of them were
    // read straight off CardService inside FireProjectile and the other four (Crit, Crit damage,
    // Multishot, Status chance) had nowhere to live at all, which is what Weapons.md's audit note
    // calls out as the spec's concretest gap.
    //
    // Deliberately a plain class, not a MonoBehaviour or ScriptableObject: Weapons.md asks for
    // "testbare Runtime-Klassen", and this one has no Unity dependencies beyond Mathf.
    public sealed class WeaponModifierState
    {
        private readonly List<WeaponModifierData> _activeModifiers = new List<WeaponModifierData>();

        public float DamageMultiplier { get; private set; } = 1f;

        public float FireIntervalMultiplier { get; private set; } = 1f;

        public float ProjectileSpeedMultiplier { get; private set; } = 1f;

        public float CriticalChance { get; private set; }

        public float CriticalDamageMultiplier { get; private set; } = 2f;

        public int AdditionalProjectiles { get; private set; }

        // Starts at 1 so a weapon whose DamageProfile applies a status keeps applying it with no
        // modifiers present. A modifier can only scale that chance down or keep it at 1 - giving a
        // status to a weapon that has none is a DamageProfile change, not a modifier.
        public float StatusEffectChance { get; private set; } = 1f;

        // WeaponLoadout.md Set Bonus rule changes (2026-09-22).
        public int AdditionalPierce { get; private set; }

        public int CriticalEveryNthShot { get; private set; }

        public bool FullRing { get; private set; }

        public bool ExplodeOnExpire { get; private set; }

        public bool ExplodeOnImpact { get; private set; }

        public bool SlowOnHit { get; private set; }

        public float HeldDamageRampPerSecond { get; private set; }

        public float SpreadAngleMultiplier { get; private set; } = 1f;

        public int MagazineBonus { get; private set; }

        public float ReloadTimeMultiplier { get; private set; } = 1f;

        // The share of DamageMultiplier / FireIntervalMultiplier that came from the loadout's Set
        // Bonuses, kept apart so the HUD can say "from your loadout" separately from "from your
        // cards" (WeaponLoadout.md Runtime).
        public float LoadoutDamageMultiplier { get; private set; } = 1f;

        public float LoadoutFireIntervalMultiplier { get; private set; } = 1f;

        public IReadOnlyList<WeaponModifierData> ActiveModifiers => _activeModifiers;

        public bool HasActiveModifiers => _activeModifiers.Count > 0;

        // Bumped whenever Rebuild actually changed something, so UI can redraw on a cheap integer
        // compare instead of diffing seven floats every frame.
        public int Version { get; private set; }

        // Returns true when the resolved values differ from the previous call, which is what
        // decides whether a WeaponModified event goes out. Reuses its own list, so calling this
        // once per frame allocates nothing.
        public bool Rebuild(
            string weaponId,
            float baseDamageMultiplier,
            float baseFireIntervalMultiplier,
            float baseProjectileSpeedMultiplier,
            IReadOnlyList<WeaponModifierData> modifiers)
        {
            return Rebuild(weaponId, baseDamageMultiplier, baseFireIntervalMultiplier, baseProjectileSpeedMultiplier, modifiers, null);
        }

        // loadoutEffects are the equipped weapon's active Set Bonus (and Arsenal) effects. They
        // compose multiplicatively with the card modifiers like any other modifier - the order of
        // a product does not matter - but are also accumulated on their own for display.
        public bool Rebuild(
            string weaponId,
            float baseDamageMultiplier,
            float baseFireIntervalMultiplier,
            float baseProjectileSpeedMultiplier,
            IReadOnlyList<WeaponModifierData> modifiers,
            IReadOnlyList<WeaponBonusEffect> loadoutEffects)
        {
            var previousLoadoutDamage = LoadoutDamageMultiplier;
            var previousLoadoutInterval = LoadoutFireIntervalMultiplier;
            var previousRuleSignature = RuleSignature();
            var previousDamage = DamageMultiplier;
            var previousFireInterval = FireIntervalMultiplier;
            var previousProjectileSpeed = ProjectileSpeedMultiplier;
            var previousCriticalChance = CriticalChance;
            var previousCriticalDamage = CriticalDamageMultiplier;
            var previousAdditionalProjectiles = AdditionalProjectiles;
            var previousStatusChance = StatusEffectChance;
            var previousModifierCount = _activeModifiers.Count;

            DamageMultiplier = baseDamageMultiplier;
            FireIntervalMultiplier = baseFireIntervalMultiplier;
            ProjectileSpeedMultiplier = baseProjectileSpeedMultiplier;
            CriticalChance = 0f;
            CriticalDamageMultiplier = 2f;
            AdditionalProjectiles = 0;
            StatusEffectChance = 1f;
            AdditionalPierce = 0;
            CriticalEveryNthShot = 0;
            FullRing = false;
            ExplodeOnExpire = false;
            ExplodeOnImpact = false;
            SlowOnHit = false;
            HeldDamageRampPerSecond = 0f;
            SpreadAngleMultiplier = 1f;
            MagazineBonus = 0;
            ReloadTimeMultiplier = 1f;
            LoadoutDamageMultiplier = 1f;
            LoadoutFireIntervalMultiplier = 1f;

            var modifiersChanged = false;
            var appliedCount = 0;

            if (modifiers != null)
            {
                for (var index = 0; index < modifiers.Count; index++)
                {
                    var modifier = modifiers[index];

                    if (modifier == null || !modifier.AppliesTo(weaponId))
                    {
                        continue;
                    }

                    if (appliedCount >= _activeModifiers.Count)
                    {
                        _activeModifiers.Add(modifier);
                        modifiersChanged = true;
                    }
                    else if (_activeModifiers[appliedCount] != modifier)
                    {
                        _activeModifiers[appliedCount] = modifier;
                        modifiersChanged = true;
                    }

                    appliedCount++;
                    Apply(modifier);
                }
            }

            if (_activeModifiers.Count > appliedCount)
            {
                _activeModifiers.RemoveRange(appliedCount, _activeModifiers.Count - appliedCount);
                modifiersChanged = true;
            }

            if (loadoutEffects != null)
            {
                for (var index = 0; index < loadoutEffects.Count; index++)
                {
                    var effect = loadoutEffects[index];

                    if (effect.Target == WeaponModifierTarget.DamageMultiplier)
                    {
                        LoadoutDamageMultiplier *= effect.Value;
                    }
                    else if (effect.Target == WeaponModifierTarget.FireIntervalMultiplier)
                    {
                        LoadoutFireIntervalMultiplier *= effect.Value;
                    }

                    Apply(effect.Target, effect.Value);
                }
            }

            CriticalChance = Mathf.Clamp01(CriticalChance);
            CriticalDamageMultiplier = Mathf.Max(1f, CriticalDamageMultiplier);
            AdditionalProjectiles = Mathf.Max(0, AdditionalProjectiles);
            StatusEffectChance = Mathf.Clamp01(StatusEffectChance);

            var changed = modifiersChanged
                || previousModifierCount != _activeModifiers.Count
                || !Mathf.Approximately(previousDamage, DamageMultiplier)
                || !Mathf.Approximately(previousFireInterval, FireIntervalMultiplier)
                || !Mathf.Approximately(previousProjectileSpeed, ProjectileSpeedMultiplier)
                || !Mathf.Approximately(previousCriticalChance, CriticalChance)
                || !Mathf.Approximately(previousCriticalDamage, CriticalDamageMultiplier)
                || previousAdditionalProjectiles != AdditionalProjectiles
                || !Mathf.Approximately(previousStatusChance, StatusEffectChance)
                || !Mathf.Approximately(previousLoadoutDamage, LoadoutDamageMultiplier)
                || !Mathf.Approximately(previousLoadoutInterval, LoadoutFireIntervalMultiplier)
                || previousRuleSignature != RuleSignature();

            if (changed)
            {
                Version++;
            }

            return changed;
        }

        // Rolls a crit for one shot. Kept here rather than in PlayerCombatController so tower and
        // any future weapon owner crit by the same rule.
        public bool RollCritical()
        {
            return CriticalChance > 0f && Random.value < CriticalChance;
        }

        public bool RollStatusEffect()
        {
            return StatusEffectChance >= 1f || Random.value < StatusEffectChance;
        }

        public float ResolveDamageMultiplier(bool isCritical)
        {
            return isCritical ? DamageMultiplier * CriticalDamageMultiplier : DamageMultiplier;
        }

        private int RuleSignature()
        {
            return AdditionalPierce
                + CriticalEveryNthShot * 16
                + (FullRing ? 1 << 10 : 0)
                + (ExplodeOnExpire ? 1 << 11 : 0)
                + (SlowOnHit ? 1 << 12 : 0)
                + (ExplodeOnImpact ? 1 << 9 : 0)
                + Mathf.RoundToInt(HeldDamageRampPerSecond * 100f) * (1 << 13)
                + Mathf.RoundToInt(SpreadAngleMultiplier * 100f) * 7
                + MagazineBonus * 131
                + Mathf.RoundToInt(ReloadTimeMultiplier * 100f) * 17;
        }

        private void Apply(WeaponModifierData modifier)
        {
            Apply(modifier.Target, modifier.Value);
        }

        private void Apply(WeaponModifierTarget target, float value)
        {
            switch (target)
            {
                case WeaponModifierTarget.DamageMultiplier:
                    DamageMultiplier *= value;
                    return;
                case WeaponModifierTarget.FireIntervalMultiplier:
                    FireIntervalMultiplier *= value;
                    return;
                case WeaponModifierTarget.ProjectileSpeedMultiplier:
                    ProjectileSpeedMultiplier *= value;
                    return;
                case WeaponModifierTarget.CriticalChance:
                    CriticalChance += value;
                    return;
                case WeaponModifierTarget.CriticalDamageMultiplier:
                    CriticalDamageMultiplier *= value;
                    return;
                case WeaponModifierTarget.AdditionalProjectiles:
                    AdditionalProjectiles += Mathf.RoundToInt(value);
                    return;
                case WeaponModifierTarget.StatusEffectChance:
                    StatusEffectChance += value;
                    return;
                case WeaponModifierTarget.AdditionalPierce:
                    AdditionalPierce += Mathf.RoundToInt(value);
                    return;
                case WeaponModifierTarget.CriticalEveryNthShot:
                    // The strongest (smallest N) wins; two sources never make it rarer.
                    var every = Mathf.RoundToInt(value);
                    CriticalEveryNthShot = CriticalEveryNthShot <= 0 ? every : Mathf.Min(CriticalEveryNthShot, every);
                    return;
                case WeaponModifierTarget.FullRing:
                    FullRing |= value > 0f;
                    return;
                case WeaponModifierTarget.ExplodeOnExpire:
                    ExplodeOnExpire |= value > 0f;
                    return;
                case WeaponModifierTarget.ExplodeOnImpact:
                    ExplodeOnImpact |= value > 0f;
                    return;
                case WeaponModifierTarget.SlowOnHit:
                    SlowOnHit |= value > 0f;
                    return;
                case WeaponModifierTarget.HeldDamageRamp:
                    HeldDamageRampPerSecond += value;
                    return;
                case WeaponModifierTarget.SpreadAngleMultiplier:
                    SpreadAngleMultiplier *= value;
                    return;
                case WeaponModifierTarget.MagazineBonus:
                    MagazineBonus += Mathf.RoundToInt(value);
                    return;
                case WeaponModifierTarget.ReloadTimeMultiplier:
                    ReloadTimeMultiplier *= value;
                    return;
            }
        }
    }
}
