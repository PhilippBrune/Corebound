using Corebound.Gameplay.Shared;
using UnityEngine;

namespace Corebound.Gameplay.Cards
{
    // Resolves card_tag_icon_sheet.png's icon grid (Weapon, Projectile, Tower, Resource /
    // StatusEffect, Burn, Freeze, Critical / Poison, Shock, Support, Summon - matching CardTagType's
    // declared order exactly) via runtime pixel-Rect slicing, per the same accepted alternative
    // Docs/Specs/Graphics.md documents for other grid-shaped atlases.
    //
    // The delivered sheet is 4 x 2. C4-1 (2026-09-29) extends it to 4 x 3 for the new card
    // mechanics (GRAPHICS_ASSET_PROMPTS Asset 108). The row count is read from the sheet's shape
    // rather than hard-coded, so the new sheet works the moment it is dropped in, and until then a
    // tag on the missing third row borrows the nearest existing icon instead of slicing garbage.
    public static class CardTagIconAtlas
    {
        private const string SheetPath = "Assets/Art/Cards/Icons/card_tag_icon_sheet.png";
        private const int Columns = 4;

        // A 4 x 2 sheet is about 0.56 as tall as it is wide, a 4 x 3 one 0.75 or more.
        private const float ThreeRowAspectThreshold = 0.66f;

        public static Sprite ResolveIcon(CardTagType tagType, float pixelsPerUnit = 100f)
        {
            var rows = ResolveRows();
            var index = (int)tagType;

            if (index >= Columns * rows)
            {
                index = (int)ResolveFallback(tagType);
            }

            var column = index % Columns;
            var row = index / Columns;
            return DebugSpriteFactory.LoadProjectAtlasCellSprite(SheetPath, column, row, Columns, rows, pixelsPerUnit);
        }

        private static int ResolveRows()
        {
            var size = DebugSpriteFactory.ResolveProjectAtlasSize(SheetPath);

            if (size.x <= 0)
            {
                return 2;
            }

            return size.y / (float)size.x >= ThreeRowAspectThreshold ? 3 : 2;
        }

        private static CardTagType ResolveFallback(CardTagType tagType)
        {
            switch (tagType)
            {
                case CardTagType.Poison:
                case CardTagType.Shock:
                    return CardTagType.StatusEffect;
                case CardTagType.Support:
                    return CardTagType.Tower;
                case CardTagType.Summon:
                    return CardTagType.Weapon;
                default:
                    return CardTagType.StatusEffect;
            }
        }
    }
}
