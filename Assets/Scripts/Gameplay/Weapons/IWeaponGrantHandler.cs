namespace Corebound.Gameplay.Weapons
{
    // Implemented by whoever owns the loadout (PlayerCombatController). CardService calls it when a
    // weapon card is picked, from the shop or the reward screen, without knowing about the player.
    public interface IWeaponGrantHandler
    {
        // purchasePrice is what was paid (0 for a reward), the basis of the copy's sell value and
        // of the refund if the player declines a weapon that did not fit.
        bool TryGrantWeapon(WeaponData weapon, int tier, int purchasePrice);

        // True while an acquired weapon is waiting for a slot. Only one can wait, so no further
        // weapon may be offered until it is placed or declined.
        bool HasPendingWeapon { get; }
    }
}
