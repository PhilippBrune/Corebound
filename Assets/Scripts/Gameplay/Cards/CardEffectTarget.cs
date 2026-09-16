namespace Corebound.Gameplay.Cards
{
    public enum CardEffectTarget
    {
        WeaponDamageMultiplier,
        WeaponFireIntervalMultiplier,
        ResourceGainMultiplier,
        TowerDamageMultiplier,

        // Appended, not inserted, so existing serialized assets (which store this enum by
        // index) keep resolving to the same target. The first negative-effect axis: scales
        // damage the Player and Core receive, read live at hit-time by PlayerCombatController
        // and CombatHitReceiver's optional CardService source.
        IncomingDamageTakenMultiplier,

        // Scales the Player's own weapon projectile speed (CombatProjectile.Launch's optional
        // speedMultiplier parameter). Tower/Enemy/Boss projectiles are unaffected - only
        // PlayerCombatController.FireProjectile reads this.
        ProjectileSpeedMultiplier,

        // Scales the Player's own movement speed, composed multiplicatively with the existing
        // status-effect (Freeze/slow) multiplier in PlayerCombatController.MovementSpeedMultiplier.
        PlayerMovementSpeedMultiplier,

        // Economy.md's documented "standardisierte Preis-Modifikatoren aus Cards, Unlocks oder
        // Events": scales the cost EconomyService.TryPurchase charges for Shop offers and Tower
        // builds/upgrades. Read live (pull-model, same as every other multiplier here) so a
        // mid-run relic grant discounts the very next purchase.
        PurchaseCostMultiplier
    }
}