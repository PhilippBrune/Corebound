using Corebound.Gameplay.Cards;
using UnityEngine;

namespace Corebound.MetaProgression
{
    // SkillTree.md Runtime: rebuilt from the purchase set exactly like CardModifierSnapshot, and
    // the only thing gameplay reads - never the tree, never the save. Multiplier targets start at
    // 1, additive ones at 0; a node's effect is applied once per purchased rank.
    public sealed class SkillModifierSnapshot
    {
        // Sized from the enum, not a literal. It was the literal 29 while PickupMagnetRange (29) and
        // RewardRerolls (30) were appended on 2026-09-22, so both fell outside the array, read as 0,
        // and the two tree nodes that grant them did nothing at all (found 2026-09-29).
        private static readonly int TargetCount = System.Enum.GetValues(typeof(SkillEffectTarget)).Length;

        private readonly float[] _values = new float[TargetCount];

        public SkillModifierSnapshot()
        {
            Reset();
        }

        public void Reset()
        {
            for (var index = 0; index < _values.Length; index++)
            {
                _values[index] = IsMultiplier((SkillEffectTarget)index) ? 1f : 0f;
            }
        }

        public float this[SkillEffectTarget target] => (int)target >= 0 && (int)target < _values.Length ? _values[(int)target] : 0f;

        public bool HasRule(SkillEffectTarget target)
        {
            return this[target] > Mathf.Epsilon;
        }

        public void Apply(SkillEffectData effect, int rank)
        {
            if (effect == null || rank <= 0)
            {
                return;
            }

            var index = (int)effect.Target;

            if (index < 0 || index >= _values.Length)
            {
                return;
            }

            for (var step = 0; step < rank; step++)
            {
                _values[index] = effect.Operation == CardEffectOperation.Multiply
                    ? _values[index] * effect.Value
                    : _values[index] + effect.Value;
            }
        }

        public static bool IsMultiplier(SkillEffectTarget target)
        {
            switch (target)
            {
                case SkillEffectTarget.WeaponDamageMultiplier:
                case SkillEffectTarget.WeaponFireIntervalMultiplier:
                case SkillEffectTarget.CriticalDamageMultiplier:
                case SkillEffectTarget.PlayerMaxHealthMultiplier:
                case SkillEffectTarget.PlayerMoveSpeedMultiplier:
                case SkillEffectTarget.TowerDamageMultiplier:
                case SkillEffectTarget.TowerRangeMultiplier:
                case SkillEffectTarget.TowerFireIntervalMultiplier:
                case SkillEffectTarget.CoreMaxHealthMultiplier:
                case SkillEffectTarget.ScrapGainMultiplier:
                case SkillEffectTarget.PurchaseCostMultiplier:
                case SkillEffectTarget.CoreRepairCostMultiplier:
                case SkillEffectTarget.ReloadTimeMultiplier:
                case SkillEffectTarget.ProjectileSpeedMultiplier:
                case SkillEffectTarget.IncomingDamageMultiplier:
                    return true;
                default:
                    return false;
            }
        }
    }
}
