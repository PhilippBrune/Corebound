namespace Corebound.Gameplay.Cards
{
    public enum CardTagType
    {
        Weapon,
        Projectile,
        Tower,
        Resource,
        StatusEffect,
        Burn,
        Freeze,
        Critical,

        // Appended 2026-09-29 (C4-1). The icon sheet grows a third row for these; until that art
        // exists CardTagIconAtlas falls back to the nearest existing icon.
        Poison,
        Shock,
        Support,
        Summon
    }
}