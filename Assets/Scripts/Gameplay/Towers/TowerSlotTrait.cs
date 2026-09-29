using Corebound.UI;
using UnityEngine;

namespace Corebound.Gameplay.Towers
{
    // OPEN_TOPICS 3.3, owner chose option 3: give slots their own character.
    //
    // The complaint was that Stage 1's slots are the same four positions every run, so placement is
    // memorised once and never thought about again. Randomising *which* slots exist answers half of
    // that; this answers the other half, which is that every slot is otherwise identical, so even a
    // new layout is still "put towers on the pads".
    //
    // A trait makes each pad a different offer - reach, protection or price - so choosing between
    // two open slots is a real decision rather than a formality. Traits are rolled per run and per
    // stage rather than authored, because the point is that the answer changes between runs.
    public enum TowerSlotTrait
    {
        Standard = 0,

        // Reaches further, which is what makes a slot worth walking to even when it is badly placed
        // for the player's own position.
        Elevated = 1,

        // Takes no damage. Matters most on the slots nearest a spawn gate, which is exactly where a
        // player would otherwise refuse to build.
        Shielded = 2,

        // Cheaper, so an early run can afford a tower one wave sooner at the cost of a worse spot.
        Cramped = 3
    }

    public static class TowerSlotTraitRules
    {
        public const float ElevatedRangeMultiplier = 1.25f;
        public const float CrampedCostMultiplier = 0.85f;

        // Weighted so most slots stay ordinary: if every pad were special, none of them would be.
        // One in roughly three carries a trait.
        public static TowerSlotTrait Roll(System.Random random)
        {
            if (random == null)
            {
                return TowerSlotTrait.Standard;
            }

            var roll = random.Next(0, 100);

            if (roll < 66)
            {
                return TowerSlotTrait.Standard;
            }

            if (roll < 79)
            {
                return TowerSlotTrait.Elevated;
            }

            return roll < 90 ? TowerSlotTrait.Shielded : TowerSlotTrait.Cramped;
        }

        public static float RangeMultiplier(TowerSlotTrait trait)
        {
            return trait == TowerSlotTrait.Elevated ? ElevatedRangeMultiplier : 1f;
        }

        public static float CostMultiplier(TowerSlotTrait trait)
        {
            return trait == TowerSlotTrait.Cramped ? CrampedCostMultiplier : 1f;
        }

        public static bool IsImmuneToDamage(TowerSlotTrait trait)
        {
            return trait == TowerSlotTrait.Shielded;
        }

        // Tints the pad. Deliberately paired with a name in the slot summary rather than standing
        // alone, so the trait is never colour-only - the same rule SkillTree.md already imposes on
        // node states.
        public static Color Tint(TowerSlotTrait trait)
        {
            switch (trait)
            {
                case TowerSlotTrait.Elevated:
                    return new Color(0.68f, 0.95f, 0.78f, 1f);
                case TowerSlotTrait.Shielded:
                    return new Color(0.72f, 0.84f, 1f, 1f);
                case TowerSlotTrait.Cramped:
                    return new Color(1f, 0.86f, 0.62f, 1f);
                default:
                    return Color.white;
            }
        }

        public static string DisplayName(TowerSlotTrait trait)
        {
            switch (trait)
            {
                case TowerSlotTrait.Elevated:
                    return LocalizationService.Resolve("Elevated", "Erhoeht");
                case TowerSlotTrait.Shielded:
                    return LocalizationService.Resolve("Shielded", "Abgeschirmt");
                case TowerSlotTrait.Cramped:
                    return LocalizationService.Resolve("Cramped", "Beengt");
                default:
                    return string.Empty;
            }
        }

        public static string Describe(TowerSlotTrait trait)
        {
            switch (trait)
            {
                case TowerSlotTrait.Elevated:
                    return LocalizationService.Resolve(
                        "Elevated  ·  +25 % range",
                        "Erhoeht  ·  +25 % Reichweite");
                case TowerSlotTrait.Shielded:
                    return LocalizationService.Resolve(
                        "Shielded  ·  the tower here takes no damage",
                        "Abgeschirmt  ·  der Turm hier nimmt keinen Schaden");
                case TowerSlotTrait.Cramped:
                    return LocalizationService.Resolve(
                        "Cramped  ·  15 % cheaper to build",
                        "Beengt  ·  15 % guenstiger im Bau");
                default:
                    return string.Empty;
            }
        }
    }
}
