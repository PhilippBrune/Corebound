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
                || !Mathf.Approximately(previousStatusChance, StatusEffectChance);

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

        private void Apply(WeaponModifierData modifier)
        {
            var value = modifier.Value;

            switch (modifier.Target)
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
            }
        }
    }
}
