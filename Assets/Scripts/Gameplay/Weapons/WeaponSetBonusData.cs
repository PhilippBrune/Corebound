using System;
using System.Collections.Generic;
using Corebound.Gameplay.Cards;
using Corebound.UI;
using UnityEngine;

namespace Corebound.Gameplay.Weapons
{
    [Serializable]
    public struct WeaponBonusEffect
    {
        [SerializeField] private WeaponModifierTarget _target;
        [SerializeField] private float _value;

        public WeaponBonusEffect(WeaponModifierTarget target, float value)
        {
            _target = target;
            _value = value;
        }

        public WeaponModifierTarget Target => _target;

        public float Value => _value;
    }

    // WeaponLoadout.md, Synergy: one tier of a weapon's Set Bonus ladder (Paired 2 / Aligned 3 /
    // Focused 4 / Synchronised 5 / Perfected 6). The spec lists the effects as references to
    // WeaponModifierData assets; they are inline (target, value) pairs instead, because a bonus
    // effect belongs to exactly one tier of one weapon and fifteen single-number assets per weapon
    // would be noise. The targets and their stacking rules are WeaponModifierTarget's, unchanged.
    [Serializable]
    public sealed class WeaponSetBonusData
    {
        [SerializeField] private int _requiredCopies = 2;
        [SerializeField] private string _displayName = "Paired";
        [SerializeField] private string _displayNameDe = "Gepaart";
        [SerializeField] private string _description = string.Empty;
        [SerializeField] private string _descriptionDe = string.Empty;
        [SerializeField] private WeaponBonusEffect[] _effects = Array.Empty<WeaponBonusEffect>();

        public int RequiredCopies => Mathf.Clamp(_requiredCopies, 2, 6);

        public string DisplayName => LocalizationService.Resolve(_displayName, _displayNameDe);

        public string Description => LocalizationService.Resolve(_description, _descriptionDe);

        public WeaponBonusEffect[] Effects => _effects;
    }

    // Per-copy in-run tier (owner decision 2026-09-22: "each weapon needs to be upgraded
    // separately"). Reuses CardRarity so a copy's tier reads with the same colour language as
    // cards. Numbers are data-light on purpose: four fixed steps, tuned in one place.
    public static class WeaponTierRules
    {
        public const int MaxTier = (int)CardRarity.Legendary;

        private static readonly float[] DamageMultipliers = { 1f, 1.2f, 1.45f, 1.75f };
        private static readonly float[] FireIntervalMultipliers = { 1f, 0.95f, 0.9f, 0.85f };

        public static float DamageMultiplier(int tier)
        {
            return DamageMultipliers[Mathf.Clamp(tier, 0, MaxTier)];
        }

        public static float FireIntervalMultiplier(int tier)
        {
            return FireIntervalMultipliers[Mathf.Clamp(tier, 0, MaxTier)];
        }

        // Scrap to raise a copy from tier to tier + 1 (raised from 0.75x on 2026-09-22: "increase the
        // cost of upgrading weapons in a run"). Scales with the weapon's price so an
        // expensive weapon is expensive to improve, and with the tier so Legendary is a goal.
        public static int UpgradeCost(WeaponData weapon, int tier)
        {
            var basePrice = weapon != null ? weapon.BasePurchaseCost : 14;
            return Mathf.RoundToInt(basePrice * 1.25f * (Mathf.Clamp(tier, 0, MaxTier) + 1));
        }

        // WeaponLoadout.md, Selling: 50% of what the copy cost, rounded down.
        public static int SellValue(int purchasePrice)
        {
            return Mathf.Max(0, purchasePrice) / 2;
        }
    }

    public readonly struct ActiveSetBonus
    {
        public ActiveSetBonus(WeaponData weapon, int copies, WeaponSetBonusData tier, WeaponSetBonusData nextTier)
        {
            Weapon = weapon;
            Copies = copies;
            Tier = tier;
            NextTier = nextTier;
        }

        public WeaponData Weapon { get; }

        public int Copies { get; }

        public WeaponSetBonusData Tier { get; }

        public WeaponSetBonusData NextTier { get; }
    }

    // WeaponLoadout.md Runtime: "a plain class, testable - takes the six equipped WeaponData
    // references and returns the active Set Bonuses plus the Arsenal flag. Pure function of the
    // loadout." Copies are counted by WeaponId alone (owner decision 2026-09-22: different tiers
    // of one weapon count toward the same Set Bonus).
    public static class WeaponSetBonusResolver
    {
        public const int ArsenalDistinctWeapons = 6;
        public const float ArsenalDamageMultiplier = 1.12f;
        public const int ArsenalProjectileBudgetHeadroom = 1;

        public static bool Resolve(IReadOnlyList<WeaponData> equipped, List<ActiveSetBonus> result)
        {
            result.Clear();

            if (equipped == null)
            {
                return false;
            }

            var counted = new List<WeaponData>(6);
            var copies = new List<int>(6);
            var filled = 0;

            for (var index = 0; index < equipped.Count; index++)
            {
                var weapon = equipped[index];

                if (weapon == null)
                {
                    continue;
                }

                filled++;
                var found = -1;

                for (var countedIndex = 0; countedIndex < counted.Count; countedIndex++)
                {
                    if (counted[countedIndex].WeaponId == weapon.WeaponId)
                    {
                        found = countedIndex;
                        break;
                    }
                }

                if (found < 0)
                {
                    counted.Add(weapon);
                    copies.Add(1);
                }
                else
                {
                    copies[found]++;
                }
            }

            for (var index = 0; index < counted.Count; index++)
            {
                // Only the highest tier reached applies; tiers do not stack with each other.
                var tier = FindTier(counted[index], copies[index], out var nextTier);

                if (tier != null || nextTier != null)
                {
                    result.Add(new ActiveSetBonus(counted[index], copies[index], tier, nextTier));
                }
            }

            // Six distinct archetypes; mutually exclusive with every Set Bonus by construction.
            return counted.Count >= ArsenalDistinctWeapons && filled >= ArsenalDistinctWeapons;
        }

        private static WeaponSetBonusData FindTier(WeaponData weapon, int copies, out WeaponSetBonusData nextTier)
        {
            nextTier = null;
            WeaponSetBonusData best = null;
            var bonuses = weapon.SetBonuses;

            if (bonuses == null)
            {
                return null;
            }

            for (var index = 0; index < bonuses.Length; index++)
            {
                var bonus = bonuses[index];

                if (bonus == null)
                {
                    continue;
                }

                if (bonus.RequiredCopies <= copies && (best == null || bonus.RequiredCopies > best.RequiredCopies))
                {
                    best = bonus;
                }
                else if (bonus.RequiredCopies > copies && (nextTier == null || bonus.RequiredCopies < nextTier.RequiredCopies))
                {
                    nextTier = bonus;
                }
            }

            return best;
        }
    }
}
