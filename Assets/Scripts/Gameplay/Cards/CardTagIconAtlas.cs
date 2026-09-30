using Corebound.Gameplay.Shared;
using UnityEngine;

namespace Corebound.Gameplay.Cards
{
    // Resolves a tag's icon. The first eight tags (Weapon ... Critical) are the cells of
    // SpriteSheet_CardTagIcons in CardTagType order; the four added for the new card mechanics
    // (Poison, Shock, Support, Summon - C4-1, 2026-09-29) are named cells of
    // SpriteSheet_CardTagIcons2, which is card_tag_icon_sheet_2.png (Asset 108, delivered
    // 2026-09-30 as its own file rather than a third row). Both sheets are referenced by the card
    // pool, which registers them. A tag with no icon on either sheet borrows the nearest one
    // instead of slicing garbage.
    public static class CardTagIconAtlas
    {
        public const string SheetAssetName = "SpriteSheet_CardTagIcons";
        public const string ExtraSheetAssetName = "SpriteSheet_CardTagIcons2";

        public static Sprite ResolveIcon(CardTagType tagType, float pixelsPerUnit = 100f)
        {
            var sheet = SpriteSheetData.Find(SheetAssetName);
            var index = (int)tagType;

            if (sheet != null && sheet.HasCell(index) && index < FirstExtraTag)
            {
                return sheet.LoadCell(index, pixelsPerUnit);
            }

            var extra = SpriteSheetData.Find(ExtraSheetAssetName);
            var extraIcon = extra != null ? extra.LoadCell(ExtraCellName(tagType), pixelsPerUnit) : null;

            if (extraIcon != null)
            {
                return extraIcon;
            }

            return sheet != null ? sheet.LoadCell((int)ResolveFallback(tagType), pixelsPerUnit) : null;
        }

        private const int FirstExtraTag = (int)CardTagType.Poison;

        private static string ExtraCellName(CardTagType tagType)
        {
            return tagType.ToString().ToLowerInvariant();
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
