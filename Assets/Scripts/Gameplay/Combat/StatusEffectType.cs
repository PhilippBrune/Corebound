namespace Corebound.Gameplay.Combat
{
    public enum StatusEffectType
    {
        Burn,
        Freeze,

        // Appended 2026-09-29 (NEW_CARD_MECHANICS, C2-2): damage over time like Burn - one
        // non-stacking effect that re-applying refreshes - with a longer, lower tick.
        Poison
    }
}