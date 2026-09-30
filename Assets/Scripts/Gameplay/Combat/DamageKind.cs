namespace Corebound.Gameplay.Combat
{
    public enum DamageKind
    {
        Direct,
        Burn,
        Freeze,

        // Appended 2026-09-29 for the new card mechanics.
        Poison,

        // Arc Coil's chained hit. Its own kind so a chained hit can never chain again.
        Shock
    }
}