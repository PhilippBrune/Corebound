using Corebound.Gameplay.Shared;
using UnityEngine;

namespace Corebound.Gameplay.Cards
{
    // Resolves a tag's icon from the tag sheet (SpriteSheet_CardTagIcons, referenced by the card
    // pool), whose cells run in CardTagType's declared order: Weapon, Projectile, Tower, Resource /
    // StatusEffect, Burn, Freeze, Critical / Poison, Shock, Support, Summon. The sheet asset owns
    // the grid and reads its row count from the image, so the 4 x 3 sheet of C4-1 (2026-09-29,
    // GRAPHICS_ASSET_PROMPTS Asset 108) works the moment it replaces the 4 x 2 one. Until then a
    // tag on the missing third row borrows the nearest existing icon instead of slicing garbage.
    public static class CardTagIconAtlas
    {
        public const string SheetAssetName = "SpriteSheet_CardTagIcons";

        public static Sprite ResolveIcon(CardTagType tagType, float pixelsPerUnit = 100f)
        {
            var sheet = SpriteSheetData.Find(SheetAssetName);

            if (sheet == null)
            {
                return null;
            }

            var index = (int)tagType;

            if (!sheet.HasCell(index))
            {
                index = (int)ResolveFallback(tagType);
            }

            return sheet.LoadCell(index, pixelsPerUnit);
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
