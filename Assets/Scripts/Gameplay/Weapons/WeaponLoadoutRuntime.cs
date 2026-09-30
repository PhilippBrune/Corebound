using System;
using System.Collections.Generic;
using UnityEngine;

namespace Corebound.Gameplay.Weapons
{
    // One equipped copy: which weapon, its in-run tier, what it cost, and its own firing state.
    // Every copy fires on its own cooldown and magazine (WeaponLoadout.md, Firing).
    public sealed class WeaponSlotRuntime
    {
        public WeaponData Weapon { get; private set; }

        public int Tier { get; private set; }

        public int PurchasePrice { get; private set; }

        public float Cooldown { get; set; }

        public float LastFireInterval { get; set; } = 0.2f;

        public int Ammo { get; set; }

        public int MagazineSize { get; set; }

        public float ReloadTimer { get; set; }

        public float ReloadDuration { get; set; } = 1f;

        public int VolleyCount { get; set; }

        public WeaponModifierState Modifiers { get; } = new WeaponModifierState();

        public bool IsEmpty => Weapon == null;

        public bool IsReloading => ReloadTimer > 0f;

        public bool HasMagazine => MagazineSize > 0;

        public float CooldownNormalized => LastFireInterval > Mathf.Epsilon ? Mathf.Clamp01(Cooldown / LastFireInterval) : 0f;

        public float ReloadNormalized => ReloadDuration > Mathf.Epsilon ? Mathf.Clamp01(ReloadTimer / ReloadDuration) : 0f;

        public float AmmoNormalized => MagazineSize > 0 ? Mathf.Clamp01(Ammo / (float)MagazineSize) : 1f;

        public void Assign(WeaponData weapon, int tier, int purchasePrice)
        {
            Weapon = weapon;
            Tier = Mathf.Clamp(tier, 0, WeaponTierRules.MaxTier);
            PurchasePrice = Mathf.Max(0, purchasePrice);
            Cooldown = 0f;
            ReloadTimer = 0f;
            VolleyCount = 0;

            // A freshly equipped weapon arrives loaded; MagazineSize is resolved by the owner
            // (base + upgrades + skills) and written before the first shot.
            Ammo = int.MaxValue;
        }

        public void SetTier(int tier)
        {
            Tier = Mathf.Clamp(tier, 0, WeaponTierRules.MaxTier);
        }

        public void Clear()
        {
            Weapon = null;
            Tier = 0;
            PurchasePrice = 0;
            Cooldown = 0f;
            ReloadTimer = 0f;
            Ammo = 0;
            VolleyCount = 0;
        }
    }

    // WeaponLoadout.md: six slots, some locked, duplicates welcome. Owns only the slot contents and
    // the derived Set Bonus state; firing lives in PlayerCombatController, which ticks the slots.
    public sealed class WeaponLoadoutRuntime
    {
        public const int Capacity = 6;

        private readonly WeaponSlotRuntime[] _slots = new WeaponSlotRuntime[Capacity];
        private readonly WeaponData[] _equipped = new WeaponData[Capacity];
        private readonly List<ActiveSetBonus> _activeSetBonuses = new List<ActiveSetBonus>();

        public WeaponLoadoutRuntime()
        {
            for (var index = 0; index < Capacity; index++)
            {
                _slots[index] = new WeaponSlotRuntime();
            }
        }

        public event Action Changed;

        public int UnlockedSlotCount { get; private set; } = 1;

        public int Version { get; private set; }

        public bool IsArsenalActive { get; private set; }

        public IReadOnlyList<ActiveSetBonus> ActiveSetBonuses => _activeSetBonuses;

        public IReadOnlyList<WeaponData> EquippedWeapons => _equipped;

        // A weapon acquired while every unlocked slot is full waits here until the player picks a
        // slot to replace or declines (WeaponLoadout.md, Acquisition).
        public WeaponData PendingWeapon { get; private set; }

        public int PendingTier { get; private set; }

        public int PendingPrice { get; private set; }

        // What declining gives back: the price actually paid (a shop purchase), or nothing (a
        // reward or boss offer, which cost nothing).
        public int PendingRefund { get; private set; }

        public bool HasPendingWeapon => PendingWeapon != null;

        public WeaponSlotRuntime GetSlot(int index)
        {
            return index >= 0 && index < Capacity ? _slots[index] : null;
        }

        public int FilledSlotCount
        {
            get
            {
                var count = 0;

                for (var index = 0; index < Capacity; index++)
                {
                    count += _slots[index].IsEmpty ? 0 : 1;
                }

                return count;
            }
        }

        public bool IsFull => FirstEmptyUnlockedSlot() < 0;

        public void SetUnlockedSlotCount(int count)
        {
            UnlockedSlotCount = Mathf.Clamp(count, 1, Capacity);
        }

        public int FirstEmptyUnlockedSlot()
        {
            for (var index = 0; index < UnlockedSlotCount; index++)
            {
                if (_slots[index].IsEmpty)
                {
                    return index;
                }
            }

            return -1;
        }

        public int CountCopies(string weaponId)
        {
            var count = 0;

            for (var index = 0; index < Capacity; index++)
            {
                if (!_slots[index].IsEmpty && _slots[index].Weapon.WeaponId == weaponId)
                {
                    count++;
                }
            }

            return count;
        }

        public void SetSlot(int index, WeaponData weapon, int tier, int price)
        {
            if (index < 0 || index >= Capacity)
            {
                return;
            }

            if (weapon == null)
            {
                _slots[index].Clear();
            }
            else
            {
                _slots[index].Assign(weapon, tier, price);
            }

            Recalculate();
        }

        public bool TryAdd(WeaponData weapon, int tier, int price)
        {
            var slot = FirstEmptyUnlockedSlot();

            if (weapon == null || slot < 0)
            {
                return false;
            }

            SetSlot(slot, weapon, tier, price);
            return true;
        }

        public void SetPending(WeaponData weapon, int tier, int price, int refund)
        {
            PendingWeapon = weapon;
            PendingTier = tier;
            PendingPrice = price;
            PendingRefund = Mathf.Max(0, refund);
            Version++;
            Changed?.Invoke();
        }

        public void ClearPending()
        {
            PendingWeapon = null;
            PendingTier = 0;
            PendingPrice = 0;
            PendingRefund = 0;
            Version++;
            Changed?.Invoke();
        }

        // Moves slot contents without resetting either copy's tier or price.
        public void Swap(int first, int second)
        {
            if (first < 0 || second < 0 || first >= UnlockedSlotCount || second >= UnlockedSlotCount || first == second)
            {
                return;
            }

            var a = _slots[first];
            var b = _slots[second];
            var weaponA = a.Weapon;
            var tierA = a.Tier;
            var priceA = a.PurchasePrice;
            var weaponB = b.Weapon;
            var tierB = b.Tier;
            var priceB = b.PurchasePrice;

            if (weaponB == null) a.Clear(); else a.Assign(weaponB, tierB, priceB);
            if (weaponA == null) b.Clear(); else b.Assign(weaponA, tierA, priceA);
            Recalculate();
        }

        public void NotifyTierChanged()
        {
            Recalculate();
        }

        // How many Set Bonus tiers are active across the loadout (the Vanguard's damage bonus).
        public int ActiveSetBonusTierCount
        {
            get
            {
                var count = 0;

                for (var index = 0; index < _activeSetBonuses.Count; index++)
                {
                    if (_activeSetBonuses[index].Tier != null)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        // Every weapon's active Set Bonus effects, plus the Arsenal bonus when it applies.
        public void CollectLoadoutEffects(string weaponId, List<WeaponBonusEffect> result)
        {
            result.Clear();

            for (var index = 0; index < _activeSetBonuses.Count; index++)
            {
                var bonus = _activeSetBonuses[index];

                if (bonus.Tier == null || bonus.Weapon == null || bonus.Weapon.WeaponId != weaponId || bonus.Tier.Effects == null)
                {
                    continue;
                }

                result.AddRange(bonus.Tier.Effects);
            }

            if (IsArsenalActive)
            {
                result.Add(new WeaponBonusEffect(WeaponModifierTarget.DamageMultiplier, WeaponSetBonusResolver.ArsenalDamageMultiplier));
            }
        }

        private void Recalculate()
        {
            for (var index = 0; index < Capacity; index++)
            {
                _equipped[index] = _slots[index].Weapon;
            }

            IsArsenalActive = WeaponSetBonusResolver.Resolve(_equipped, _activeSetBonuses);
            StaggerDuplicates();
            Version++;
            Changed?.Invoke();
        }

        // WeaponLoadout.md readability rule 2: copies with the same fire interval spread their
        // shots across the interval instead of all firing on the same frame - which would read as
        // one heavy shot and hit the projectile budget in a single spike.
        private void StaggerDuplicates()
        {
            for (var index = 0; index < Capacity; index++)
            {
                var slot = _slots[index];

                if (slot.IsEmpty)
                {
                    continue;
                }

                var copyIndex = 0;
                var copies = 0;

                for (var other = 0; other < Capacity; other++)
                {
                    if (_slots[other].IsEmpty || _slots[other].Weapon.WeaponId != slot.Weapon.WeaponId)
                    {
                        continue;
                    }

                    if (other < index)
                    {
                        copyIndex++;
                    }

                    copies++;
                }

                if (copies > 1)
                {
                    var interval = slot.Weapon.AttackPattern != null ? slot.Weapon.AttackPattern.FireIntervalSeconds : 0.2f;
                    slot.Cooldown = interval * copyIndex / copies;
                }
            }
        }
    }
}
