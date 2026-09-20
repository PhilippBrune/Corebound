using System;
using UnityEngine;

namespace Corebound.Gameplay.Weapons
{
    // Weapons.md, Technical Design: "Weapon-Systeme senden Events wie OnWeaponFired,
    // OnProjectileSpawned, OnWeaponModified und OnWeaponStateChanged." The names drop the "On"
    // prefix to match CombatEvents/FactoryEvents/WaveEvents, which every other raiser in this
    // project follows; the four signals themselves map one-to-one onto the spec's list.
    //
    // PlayerCombatController.WeaponFired stays where it is: TutorialService already consumes it
    // as an instance event, and a tutorial must not react to a different player's shots if a
    // second combatant ever raises these.
    public readonly struct WeaponFiredEventArgs
    {
        public WeaponFiredEventArgs(GameObject owner, WeaponData weapon, Vector2 origin, Vector2 direction, int projectileCount, bool isCritical)
        {
            Owner = owner;
            Weapon = weapon;
            Origin = origin;
            Direction = direction;
            ProjectileCount = projectileCount;
            IsCritical = isCritical;
        }

        public GameObject Owner { get; }

        public WeaponData Weapon { get; }

        public Vector2 Origin { get; }

        public Vector2 Direction { get; }

        public int ProjectileCount { get; }

        public bool IsCritical { get; }
    }

    public readonly struct ProjectileSpawnedEventArgs
    {
        public ProjectileSpawnedEventArgs(GameObject owner, WeaponData weapon, Vector2 origin, Vector2 direction, float damage, int shotIndex, bool isCritical)
        {
            Owner = owner;
            Weapon = weapon;
            Origin = origin;
            Direction = direction;
            Damage = damage;
            ShotIndex = shotIndex;
            IsCritical = isCritical;
        }

        public GameObject Owner { get; }

        public WeaponData Weapon { get; }

        public Vector2 Origin { get; }

        public Vector2 Direction { get; }

        public float Damage { get; }

        public int ShotIndex { get; }

        public bool IsCritical { get; }
    }

    public readonly struct WeaponModifiedEventArgs
    {
        public WeaponModifiedEventArgs(WeaponData weapon, WeaponModifierState modifierState)
        {
            Weapon = weapon;
            ModifierState = modifierState;
        }

        public WeaponData Weapon { get; }

        public WeaponModifierState ModifierState { get; }
    }

    public enum WeaponReadinessState
    {
        // No weapon resolved, or the owner is dead - the UI shows the weapon as unusable rather
        // than permanently "cooling".
        Unavailable = 0,
        Ready = 1,
        Cooling = 2
    }

    public readonly struct WeaponStateChangedEventArgs
    {
        public WeaponStateChangedEventArgs(WeaponData weapon, WeaponReadinessState previousState, WeaponReadinessState currentState)
        {
            Weapon = weapon;
            PreviousState = previousState;
            CurrentState = currentState;
        }

        public WeaponData Weapon { get; }

        public WeaponReadinessState PreviousState { get; }

        public WeaponReadinessState CurrentState { get; }
    }

    public static class WeaponEvents
    {
        public static event Action<WeaponFiredEventArgs> WeaponFired;

        public static event Action<ProjectileSpawnedEventArgs> ProjectileSpawned;

        public static event Action<WeaponModifiedEventArgs> WeaponModified;

        public static event Action<WeaponStateChangedEventArgs> WeaponStateChanged;

        public static void RaiseWeaponFired(WeaponFiredEventArgs eventArgs)
        {
            WeaponFired?.Invoke(eventArgs);
        }

        public static void RaiseProjectileSpawned(ProjectileSpawnedEventArgs eventArgs)
        {
            ProjectileSpawned?.Invoke(eventArgs);
        }

        public static void RaiseWeaponModified(WeaponModifiedEventArgs eventArgs)
        {
            WeaponModified?.Invoke(eventArgs);
        }

        public static void RaiseWeaponStateChanged(WeaponStateChangedEventArgs eventArgs)
        {
            WeaponStateChanged?.Invoke(eventArgs);
        }
    }
}
