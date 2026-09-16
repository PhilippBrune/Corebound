using Corebound.Gameplay.Shared;
using UnityEngine;

namespace Corebound.Gameplay.Cards
{
    // Resolves card_tag_icon_sheet.png's 4x2 icon grid (Weapon, Projectile, Tower, Resource /
    // StatusEffect, Burn, Freeze, Critical - matching CardTagType's declared order exactly) via
    // runtime pixel-Rect slicing, per the same accepted alternative Docs/Specs/Graphics.md
    // documents for other grid-shaped atlases. The sheet was previously imported but referenced
    // by zero lines of code.
    public static class CardTagIconAtlas
    {
        private const string SheetPath = "Assets/Art/Cards/Icons/card_tag_icon_sheet.png";
        private const int Columns = 4;
        private const int Rows = 2;

        public static Sprite ResolveIcon(CardTagType tagType, float pixelsPerUnit = 100f)
        {
            var index = (int)tagType;
            var column = index % Columns;
            var row = index / Columns;
            return DebugSpriteFactory.LoadProjectAtlasCellSprite(SheetPath, column, row, Columns, Rows, pixelsPerUnit);
        }
    }
}
